using System.Collections.Generic;
using UnityEngine;

public class Gear : MonoBehaviour
{
    [SerializeField] private LootManager.GearType itemType; //Field for the item type
    [SerializeField] private LootManager.GearRarity itemRarity; //Field for the item rarity
    [SerializeField] private int itemLevel; //Field for the item level of the gear

    private int modNumber;  //Integer variable used to store the number of mods that the item can have (set on initialize)

    public List<RolledMod> rolledMods = new List<RolledMod>();  //Optional list for storing all rolled mods (not currently populated here)
    public List<RolledMod> globalRolledMods = new List<RolledMod>();    //List used to store only the global mods rolled on the item (excludes local mods like base weapon damage)

    public Element BaseElement; //Variable of type Element (enum) that will store the base element of the weapon
    public float BaseDamage;    //Variable to store the weapon's base damage
    public float BaseAttackSpeed;   //Variable to store a weapon's base attack speed
    public float BaseCritChance;    //Variable to store a weapon's base crit chance

    // Local weapon-only modifiers
    public float LocalFlatDamage;
    public float LocalIncDamage;
    public float LocalBaseCrit;
    public float LocalIncCrit;
    public float LocalIncAttackSpeed;

    //These 4 lines define getters for the private variables, itemType, itemRarity, itemLevel, and modNumber
    public LootManager.GearType ItemType => itemType;
    public LootManager.GearRarity ItemRarity => itemRarity;
    public int ItemLevel => itemLevel;
    public int ModCount => modNumber;

    //Initialize method to be called when creating a new gear object, sets the item type, rarity, ilvl and number of rolled mods (by calling RollModNumber) and using passed in values for the other variables
    //Assumes this gear instance is fresh or cleared before reinitializing
    public void Initialize(LootManager.GearType type, LootManager.GearRarity rarity, int level, Element element)
    {
        itemType = type;
        itemRarity = rarity;
        itemLevel = level;
        modNumber = RollModNumber();
        BaseElement = element;
    }

    //Rolls the number of modifiers on the items, by referencing the rarity to determine the range, and rolling within that range, if the item is legendary, it rolls a range of 1-6 modifiers instead.
    public int RollModNumber()
    {
        if (itemRarity == LootManager.GearRarity.Normal) return 0;
        if (itemRarity == LootManager.GearRarity.Magic) return Random.Range(1, 3);
        if (itemRarity == LootManager.GearRarity.Rare) return Random.Range(1, 5);
        return Random.Range(1, 7);
    }

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
            default:
                return false;
        }
    }

    //Function used to apply modifiers to the item.
    public void ApplyMods(List<RolledMod> rolledMods)
    {
        foreach (var mod in rolledMods)
        {
            switch (mod.statType)
            {

                //Base Damage Stats
                case StatTypes.WeaponBaseDmg:
                    BaseDamage = mod.value;
                    break;
                case StatTypes.WeaponBaseAttackSpeed:
                    BaseAttackSpeed = mod.value;
                    break;
                case StatTypes.WeaponBaseCrit:
                    BaseCritChance = mod.value;
                    break;
                // FLAT / INC DAMAGE (If any of these cases are true, perform if, so if the mod is any "Dmg" mod or "Flat" mod)
                case StatTypes.PhysDmg:
                case StatTypes.ColdDmg:
                case StatTypes.LightDmg:
                case StatTypes.FireDmg:
                case StatTypes.FlatPhys:
                case StatTypes.FlatCold:
                case StatTypes.FlatLight:
                case StatTypes.FlatFire:
                    {
                        if (itemType == LootManager.GearType.Weapons && MatchesBaseElement(mod.statType)) //If the itemtype is a weapon and the base element matches this mod's type enter the if statement
                        {
                            //If it is a flat modifier, add it's value to the local flat damage of the weapon
                            if (mod.statType is StatTypes.FlatPhys or StatTypes.FlatCold or StatTypes.FlatLight or StatTypes.FlatFire)
                                LocalFlatDamage += mod.value;
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
        float damageWithFlat = BaseDamage + LocalFlatDamage;
        return damageWithFlat * (1f + LocalIncDamage);
    }

    //Finds the effective base damage by adding the base crit chance to the local base crit and multiplying it by 1 + the local increased crit chance (local inc crit should be stored as a decimal)
    public float GetEffectiveBaseCrit()
    {
        float critWithBase = BaseCritChance + LocalBaseCrit;
        return critWithBase * (1f + LocalIncCrit);
    }

    //Finds the effective attack speed by multiplying the base attack speed by 1 + the local increased attack speed.
    public float GetEffectiveAttackSpeed()
    {
        return BaseAttackSpeed * (1f + LocalIncAttackSpeed);
    }
}
