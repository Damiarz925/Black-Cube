// Developer map: Runtime item state, scrap flags, local weapon values and global rolled modifiers. ApplyMods accumulates once during generation; local percentages are fractions, global rolls remain raw percentage points.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections.Generic;
using UnityEngine;

public class Gear : MonoBehaviour
{
    [SerializeField] private string persistentId;
    public string PersistentId => persistentId;
    // Historical schema-2/PlayerPrefs rolls retain their exact values and
    // tier numbering; new items are validated against the current catalog.
    public bool LegacyAffixRules { get; private set; }
    public void RestoreLegacyAffixRules(bool value) => LegacyAffixRules=value;
    private void Awake() => EnsurePersistentId();
    public string EnsurePersistentId()
    {
        if (string.IsNullOrWhiteSpace(persistentId)) persistentId = Guid.NewGuid().ToString("N");
        return persistentId;
    }
    public void RestorePersistentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Gear persistent ID is required.", nameof(value));
        persistentId = value;
    }
    [SerializeField] private bool isScrap;
    [SerializeField] private int stackCount = 1;
    public bool IsScrap => isScrap;
    public int StackCount => stackCount;
    internal bool PickupClaimed { get; set; }
    internal bool Dismantled { get; set; }
    internal void InitializeScrap(int count) { isScrap = true; stackCount = count; }
    internal void AddScrap(int count) { stackCount = checked(stackCount + count); }
    [SerializeField] private LootManager.GearType itemType; //Field for the item type
    [SerializeField] private LootManager.GearRarity itemRarity; //Field for the item rarity
    [SerializeField] private int itemLevel; //Field for the item level of the gear
    [SerializeField] private PaperWeaponVisual weaponVisual;
    public PaperWeaponVisual WeaponVisual => weaponVisual;
    public void SetWeaponVisual(PaperWeaponVisual visual) { weaponVisual = visual; }

    private int modNumber;  //Integer variable used to store the number of mods that the item can have (set on initialize)

    public List<RolledMod> rolledMods = new List<RolledMod>();  //Optional list for storing all rolled mods (not currently populated here)
    public List<RolledMod> globalRolledMods = new List<RolledMod>();    //List used to store only the global mods rolled on the item (excludes local mods like base weapon damage)

    public Element BaseElement; //Variable of type Element (enum) that will store the base element of the weapon
    public float BaseDamage;    //Variable to store the weapon's base damage
    public float BaseDamageMin;
    public float BaseDamageMax;
    public float BaseAttackSpeed;   //Variable to store a weapon's base attack speed
    public float BaseCritChance;    //Variable to store a weapon's base crit chance

    // Local weapon-only modifiers
    public float LocalFlatDamage;
    public float LocalFlatDamageMax;
    public float LocalIncDamage;
    public float LocalBaseCrit;
    public float LocalIncCrit;
    public float LocalIncAttackSpeed;

    //These 4 lines define getters for the private variables, itemType, itemRarity, itemLevel, and modNumber
    public LootManager.GearType ItemType => itemType;
    public LootManager.GearRarity ItemRarity => itemRarity;
    public int ItemLevel => itemLevel;
    public int ModCount => modNumber;
    public RolledMod ImplicitMod
    {
        get
        {
            foreach (var mod in rolledMods)
                if (mod != null && !IsWeaponBaseStat(mod.statType) && mod.lockedOriginal) return mod;
            return null;
        }
    }

    public int CraftingModCount
    {
        get
        {
            int count = 0;
            foreach (var mod in rolledMods)
                if (mod != null && !IsWeaponBaseStat(mod.statType) && !mod.lockedOriginal) count++;
            return count;
        }
    }

    //Initialize method to be called when creating a new gear object, sets the item type, rarity, ilvl and number of rolled mods (by calling RollModNumber) and using passed in values for the other variables
    //Assumes this gear instance is fresh or cleared before reinitializing
    public void Initialize(LootManager.GearType type, LootManager.GearRarity rarity, int level, Element element)
    {
        EnsurePersistentId();
        itemType = type;
        itemRarity = rarity;
        itemLevel = level;
        modNumber = RollModNumber();
        BaseElement = element == Element.Poison ? Element.Void : element;
    }

    public static bool IsWeaponBaseStat(StatTypes stat) => stat is StatTypes.WeaponBaseDmg
        or StatTypes.WeaponBaseAttackSpeed or StatTypes.WeaponBaseCrit;

    public void SetRarity(LootManager.GearRarity rarity)
    {
        itemRarity = rarity;
        modNumber = CraftingModCount;
    }

    public void EnsureOriginalModifierLocked()
    {
        RolledMod first = null;
        RolledMod implicitMod = null;
        foreach (var mod in rolledMods)
        {
            if (mod == null || IsWeaponBaseStat(mod.statType)) continue;
            if (first == null) first = mod;
            if (mod.lockedOriginal && implicitMod == null) implicitMod = mod;
        }
        implicitMod ??= first;
        foreach (var mod in rolledMods)
            if (mod != null && !IsWeaponBaseStat(mod.statType)) mod.lockedOriginal = ReferenceEquals(mod, implicitMod);
    }

    /// <summary>Recalculates all derived local/global fields after a crafting mutation.</summary>
    public void RebuildMods()
    {
        var copy = new List<RolledMod>(rolledMods);
        float fallbackDamage = BaseDamage, fallbackMin = BaseDamageMin, fallbackMax = BaseDamageMax;
        float fallbackSpeed = BaseAttackSpeed, fallbackCrit = BaseCritChance;
        bool hasDamageBase = copy.Exists(m => m != null && m.statType == StatTypes.WeaponBaseDmg);
        bool hasSpeedBase = copy.Exists(m => m != null && m.statType == StatTypes.WeaponBaseAttackSpeed);
        bool hasCritBase = copy.Exists(m => m != null && m.statType == StatTypes.WeaponBaseCrit);
        BaseDamage = BaseDamageMin = BaseDamageMax = BaseAttackSpeed = BaseCritChance = 0f;
        LocalFlatDamage = LocalFlatDamageMax = LocalIncDamage = LocalBaseCrit = LocalIncCrit = LocalIncAttackSpeed = 0f;
        globalRolledMods.Clear();
        ApplyMods(copy);
        if (!hasDamageBase) { BaseDamage = fallbackDamage; BaseDamageMin = fallbackMin; BaseDamageMax = fallbackMax; }
        if (!hasSpeedBase) BaseAttackSpeed = fallbackSpeed;
        if (!hasCritBase) BaseCritChance = fallbackCrit;
        modNumber = CraftingModCount;
    }

    // One permanent implicit plus a full 0/2/4/6 explicit set on natural equipment.
    public int RollModNumber()
    {
        if (itemRarity == LootManager.GearRarity.Normal) return 1;
        if (itemRarity == LootManager.GearRarity.Magic) return 3;
        if (itemRarity == LootManager.GearRarity.Rare) return 5;
        return 7;
    }

    // Enemy intrinsic equipment retains the pre-12.5G modifier counts. The
    // equipment-implicit follow-up does not rebalance enemy scaling.
    public static int RollEnemyModNumber(LootManager.GearRarity rarity) => rarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 2,
        LootManager.GearRarity.Rare => UnityEngine.Random.Range(3,5),
        _ => UnityEngine.Random.Range(5,7)
    };

    //Function used to check if a rolled modifier matches a weapon's base element, if so that modifier will be applied as a local modifier to weapon damage, instead of global.
    private bool MatchesBaseElement(StatTypes stat)
    {
        switch (BaseElement)
        {
            case Element.Phys:
                return stat == StatTypes.PhysDmg || stat == StatTypes.FlatPhys;
            case Element.Fire:
                return stat == StatTypes.FireDmg || stat == StatTypes.FlatFire;
            case Element.Cold:
                return stat == StatTypes.ColdDmg || stat == StatTypes.FlatCold;
            case Element.Light:
                return stat == StatTypes.LightDmg || stat == StatTypes.FlatLight;
            case Element.Void:
            case Element.Poison:
                return stat == StatTypes.VoidDmg || stat == StatTypes.FlatVoid;
            default:
                return false;
        }
    }

    //Function used to apply modifiers to the item.
    public void ApplyMods(List<RolledMod> rolledMods)
    {
        if (rolledMods == null)
            return;

        // Keep the complete roll list for item inspection/filtering. Some weapon
        // affixes are applied locally and therefore never enter globalRolledMods.
        // Copy first in case a caller passes this instance's own list.
        var incomingMods = new List<RolledMod>(rolledMods);
        this.rolledMods.Clear();
        this.rolledMods.AddRange(incomingMods);
        EnsureOriginalModifierLocked();

        foreach (var mod in incomingMods)
        {
            if (mod == null) continue;
            switch (mod.statType)
            {

                //Base Damage Stats
                case StatTypes.WeaponBaseDmg:
                    BaseDamageMin = mod.value;
                    BaseDamageMax = mod.HighValue;
                    BaseDamage = (BaseDamageMin + BaseDamageMax) * .5f;
                    break;
                case StatTypes.WeaponBaseAttackSpeed:
                    BaseAttackSpeed = mod.value;
                    break;
                case StatTypes.WeaponBaseCrit:
                    // Rolled values are percentage points; runtime weapon fields are fractions.
                    BaseCritChance = mod.value / 100f;
                    break;
                // FLAT / INC DAMAGE (If any of these cases are true, perform if, so if the mod is any "Dmg" mod or "Flat" mod)
                case StatTypes.PhysDmg:
                case StatTypes.ColdDmg:
                case StatTypes.LightDmg:
                case StatTypes.FireDmg:
                case StatTypes.VoidDmg:
                case StatTypes.FlatPhys:
                case StatTypes.FlatCold:
                case StatTypes.FlatLight:
                case StatTypes.FlatFire:
                case StatTypes.FlatVoid:
                    {
                        if (itemType == LootManager.GearType.Weapons && MatchesBaseElement(mod.statType)) //If the itemtype is a weapon and the base element matches this mod's type enter the if statement
                        {
                            //If it is a flat modifier, add it's value to the local flat damage of the weapon
                            if (mod.statType is StatTypes.FlatPhys or StatTypes.FlatCold or StatTypes.FlatLight or StatTypes.FlatFire or StatTypes.FlatVoid)
                            {
                                LocalFlatDamage += mod.value;
                                LocalFlatDamageMax += mod.HighValue;
                            }
                            //Otherwise, add it's value to the local increased damage of the weapon
                            else
                                LocalIncDamage += (mod.value/100);
                        }
                        else
                        {
                            //If we don't enter the if statement, we can safely add the modifier to global rolled mods
                            globalRolledMods.Add(mod);
                        }
                        break;
                    }

                // BASE CRIT (local only on weapons)
                //If the stat is base crit, check if the item is a weapon, if so add it to the local base crit (might change this to always apply to weapon's local base crit, because that's what the mod does no matter what slot
                case StatTypes.BaseCritChance:
                    if (itemType == LootManager.GearType.Weapons)
                        LocalBaseCrit += (mod.value/100);
                    else
                        globalRolledMods.Add(mod);
                    break;

                // % CRIT CHANCE (local only on weapons)
                //If the stat is increased crit chance, check if it's on a weapon, if so it's local, if not it's global
                case StatTypes.CritChance:
                    if (itemType == LootManager.GearType.Weapons)
                        LocalIncCrit += (mod.value/100);
                    else
                        globalRolledMods.Add(mod);
                    break;

                // ATTACK SPEED (local only on weapons)
                //If the stat is attack speed, check if it's on a weapon, if so it's local, if not it's global.
                case StatTypes.AttackSpeed:
                    if (itemType == LootManager.GearType.Weapons)
                        LocalIncAttackSpeed += (mod.value/100);
                    else
                        globalRolledMods.Add(mod);
                    break;

                //If none of the cases are true, add the modifier to the global mod list.
                default:
                    globalRolledMods.Add(mod);
                    break;
            }
        }
    }

    //Finds the effective base damage by adding the local flat to the base damage and multiplying it by  1 + the local increased damage (local inc dmg should be stored as a decimal)
    public float GetEffectiveBaseDamage()
    {
        GetEffectiveBaseDamageRange(out float minimum, out float maximum);
        return (minimum + maximum) * .5f;
    }

    public bool IsLocalAffix(StatTypes stat) => itemType == LootManager.GearType.Weapons
        && (MatchesBaseElement(stat) || stat is StatTypes.AttackSpeed or StatTypes.CritChance
            or StatTypes.BaseCritChance);

    public void GetEffectiveBaseDamageRange(out float minimum, out float maximum)
    {
        float baseMin = BaseDamageMin > 0f || BaseDamageMax > 0f ? BaseDamageMin : BaseDamage;
        float baseMax = BaseDamageMin > 0f || BaseDamageMax > 0f ? BaseDamageMax : BaseDamage;
        minimum = Mathf.Max(0f, (baseMin + LocalFlatDamage) * (1f + LocalIncDamage));
        maximum = Mathf.Max(minimum, (baseMax + LocalFlatDamageMax) * (1f + LocalIncDamage));
    }

    public float RollEffectiveBaseDamage()
    {
        GetEffectiveBaseDamageRange(out float minimum, out float maximum);
        return UnityEngine.Random.Range(minimum, maximum);
    }

    //Finds the effective base damage by adding the base crit chance to the local base crit and multiplying it by 1 + the local increased crit chance (local inc crit should be stored as a decimal)
    public float GetEffectiveBaseCrit(float extraBaseCrit = 0f)
    {
        float critWithBase = BaseCritChance + LocalBaseCrit + extraBaseCrit;
        return critWithBase * (1f + LocalIncCrit);
    }

    //Finds the effective attack speed by multiplying the base attack speed by 1 + the local increased attack speed.
    public float GetEffectiveAttackSpeed()
    {
        return BaseAttackSpeed * (1f + LocalIncAttackSpeed);
    }
}
