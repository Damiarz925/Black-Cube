using System.Collections.Generic;
using UnityEngine;

public class GearStatLists : MonoBehaviour
{
    public static GearStatLists Instance { get; private set; }

    private Dictionary<LootManager.GearType, List<StatTypes>> statPools;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Use the shared builder for runtime
        statPools = BuildDefaultStatPools();
    }

    public List<StatTypes> GetStatPoolForType(LootManager.GearType type)
    {
        return statPools[type];
    }

    /// <summary>
    /// Shared source of truth: defines which stats can appear on which gear types.
    /// This is used both at runtime (Awake) and in editor (ModDatabase.OnValidate).
    /// </summary>
    public static Dictionary<LootManager.GearType, List<StatTypes>> BuildDefaultStatPools()
    {
        var dict = new Dictionary<LootManager.GearType, List<StatTypes>>();

        // GLOVES
        dict[LootManager.GearType.Gloves] = new List<StatTypes>
        {
            // Damage Mods
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.PhysDmg,
            StatTypes.ColdDmg,
            StatTypes.PhysMult,
            StatTypes.ColdMult,
            StatTypes.PhysPenetration,
            StatTypes.ColdPenetration,

            // Crit Mods
            StatTypes.CritChance,
            StatTypes.CritMult,
            StatTypes.BaseCritChance,

            // DoT Mods
            StatTypes.PoisonDmg,
            StatTypes.BleedDmg,
            StatTypes.PoisonMult,
            StatTypes.BleedMult,
            StatTypes.PoisonChance,
            StatTypes.BleedChance,
            StatTypes.PoisonTickRate,
            StatTypes.BleedTickRate,
            StatTypes.PoisonDuration,
            StatTypes.BleedDuration,
            StatTypes.PoisonPenetration,
            StatTypes.BleedPenetration,

            // Defensive
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.FlatArmour,
            StatTypes.FlatEvasion,
            StatTypes.ArmourPercent,
            StatTypes.EvasionPercent,

            // Speed / Utility
            StatTypes.Accuracy,
            StatTypes.AttackSpeed,
            StatTypes.CooldownRecovery,

            // Resource
            StatTypes.Life,
            StatTypes.Mana,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity,
            StatTypes.DexterityPercent,

            // Attribute scaling
            StatTypes.AttackSpeedPerDexterity,
            StatTypes.AccuracyPerDexterity
        };

        // HELMETS
        dict[LootManager.GearType.Helmets] = new List<StatTypes>
        {
            // Damage mods
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.PhysDmg,
            StatTypes.FireDmg,
            StatTypes.PhysMult,
            StatTypes.FireMult,
            StatTypes.PhysPenetration,
            StatTypes.FirePenetration,

            // Crit chance
            StatTypes.CritChance,

            // DoTs (Bleed / Ignite)
            StatTypes.BleedDmg,
            StatTypes.IgniteDmg,
            StatTypes.BleedMult,
            StatTypes.IgniteMult,
            StatTypes.BleedChance,
            StatTypes.IgniteChance,
            StatTypes.BleedTickRate,
            StatTypes.IgniteTickRate,
            StatTypes.BleedDuration,
            StatTypes.IgniteDuration,
            StatTypes.BleedPenetration,
            StatTypes.IgnitePenetration,

            // Defenses
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.AllRes,
            StatTypes.FlatArmour,
            StatTypes.FlatEvasion,
            StatTypes.ArmourPercent,
            StatTypes.EvasionPercent,
            StatTypes.MaxColdRes,
            StatTypes.MaxFireRes,
            StatTypes.MaxLightRes,
            StatTypes.PoisonRes,
            StatTypes.IgniteRes,
            StatTypes.BleedRes,
            StatTypes.ShockRes,
            StatTypes.ChillRes,

            // Resources
            StatTypes.Life,
            StatTypes.Mana,
            StatTypes.LifeRegeneration,
            StatTypes.ManaRegeneration,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity,
            StatTypes.StrengthPercent,

            // Attribute scaling
            StatTypes.LifePerStrength,
            StatTypes.DamagePerStrength
        };

        // BODY ARMOUR
        dict[LootManager.GearType.BodyArmours] = new List<StatTypes>
        {
            // Generic dmg mods
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,

            // Defenses
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.AllRes,
            StatTypes.FlatArmour,
            StatTypes.FlatEvasion,
            StatTypes.ArmourPercent,
            StatTypes.EvasionPercent,
            StatTypes.MaxColdRes,
            StatTypes.MaxFireRes,
            StatTypes.MaxLightRes,
            StatTypes.MaxAllRes,
            StatTypes.PoisonRes,
            StatTypes.IgniteRes,
            StatTypes.BleedRes,
            StatTypes.ShockRes,
            StatTypes.ChillRes,
            StatTypes.AllAilmentRes,
            StatTypes.ChanceToBlock,

            // Resources
            StatTypes.Life,
            StatTypes.Mana,
            StatTypes.LifePercent,
            StatTypes.ManaPercent,
            StatTypes.LifeRegeneration,
            StatTypes.ManaRegeneration,
            StatTypes.LifeOnHit,
            StatTypes.ManaOnHit,
            StatTypes.LifeOnKill,
            StatTypes.ManaOnKill,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity,
            StatTypes.StrengthPercent,
            StatTypes.IntelligencePercent,
            StatTypes.DexterityPercent,

            // Attribute scaling
            StatTypes.LifePerStrength,
            StatTypes.DamagePerStrength,
            StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence,
            StatTypes.AttackSpeedPerDexterity,
            StatTypes.AccuracyPerDexterity
        };

        // BOOTS
        dict[LootManager.GearType.Boots] = new List<StatTypes>
        {
            // Damage
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.ColdDmg,
            StatTypes.LightDmg,
            StatTypes.ColdMult,
            StatTypes.LightMult,
            StatTypes.ColdPenetration,
            StatTypes.LightPenetration,
            StatTypes.CritChance,
            StatTypes.PoisonDmg,
            StatTypes.PoisonMult,
            StatTypes.PoisonChance,
            StatTypes.PoisonTickRate,
            StatTypes.PoisonDuration,
            StatTypes.PoisonPenetration,
            StatTypes.ShockChance,
            StatTypes.ShockEffect,
            StatTypes.ShockDuration,
            StatTypes.ChillChance,
            StatTypes.ChillEffect,
            StatTypes.ChillDuration,

            // Defenses
            StatTypes.FlatArmour,
            StatTypes.FlatEvasion,
            StatTypes.ArmourPercent,
            StatTypes.EvasionPercent,
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.PoisonRes,
            StatTypes.IgniteRes,
            StatTypes.BleedRes,
            StatTypes.ShockRes,
            StatTypes.ChillRes,

            // Mana / Resources
            StatTypes.Life,
            StatTypes.Mana,
            StatTypes.ManaPercent,
            StatTypes.ManaRegeneration,
            StatTypes.ManaOnHit,
            StatTypes.ManaOnKill,
            StatTypes.ManaCost,
            StatTypes.DmgPerMaxMana,
            StatTypes.DmgPerCurrentMana,

            // Utility
            StatTypes.CooldownRecovery,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity,
            StatTypes.IntelligencePercent,

            // Attribute scaling
            StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence
        };

        // RINGS
        dict[LootManager.GearType.Rings] = new List<StatTypes>
        {
            // Elements, no phys
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.ColdDmg,
            StatTypes.LightDmg,
            StatTypes.FireDmg,
            StatTypes.ColdMult,
            StatTypes.LightMult,
            StatTypes.FireMult,
            StatTypes.ColdPenetration,
            StatTypes.LightPenetration,
            StatTypes.FirePenetration,
            StatTypes.CritChance,
            StatTypes.ShockChance,
            StatTypes.ShockEffect,
            StatTypes.ShockDuration,
            StatTypes.ChillChance,
            StatTypes.ChillEffect,
            StatTypes.ChillDuration,

            // Res
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.AllRes,

            // Resources
            StatTypes.Life,
            StatTypes.Mana,
            StatTypes.ManaCost,

            // Utility
            StatTypes.CooldownRecovery,
            StatTypes.AttackSpeed,
            StatTypes.Accuracy,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity
        };

        // AMULETS
        dict[LootManager.GearType.Amulets] = new List<StatTypes>
        {
            // All damage mods
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.PhysDmg,
            StatTypes.ColdDmg,
            StatTypes.LightDmg,
            StatTypes.FireDmg,
            StatTypes.PhysMult,
            StatTypes.ColdMult,
            StatTypes.LightMult,
            StatTypes.FireMult,
            StatTypes.PhysPenetration,
            StatTypes.ColdPenetration,
            StatTypes.LightPenetration,
            StatTypes.FirePenetration,
            StatTypes.CritChance,
            StatTypes.CritMult,
            StatTypes.BaseCritChance,
            StatTypes.PoisonDmg,
            StatTypes.PoisonMult,
            StatTypes.PoisonChance,
            StatTypes.PoisonTickRate,
            StatTypes.PoisonDuration,
            StatTypes.PoisonPenetration,
            StatTypes.IgniteDmg,
            StatTypes.IgniteMult,
            StatTypes.IgniteChance,
            StatTypes.IgniteTickRate,
            StatTypes.IgniteDuration,
            StatTypes.IgnitePenetration,
            StatTypes.BleedDmg,
            StatTypes.BleedMult,
            StatTypes.BleedChance,
            StatTypes.BleedTickRate,
            StatTypes.BleedDuration,
            StatTypes.BleedPenetration,
            StatTypes.ChanceToHitTwice,

            // Resists
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,

            // Resources
            StatTypes.Life,
            StatTypes.Mana,

            // Utility + +1 skills
            StatTypes.CooldownRecovery,
            StatTypes.AttackSpeed,
            StatTypes.Accuracy,
            StatTypes.Plus1Phys,
            StatTypes.Plus1Fire,
            StatTypes.Plus1Cold,
            StatTypes.Plus1Light,
            StatTypes.Plus1Poison,
            StatTypes.Plus1Bleed,
            StatTypes.Plus1Ignite,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity
        };

        // BELTS
        dict[LootManager.GearType.Belts] = new List<StatTypes>
        {
            // Res
            StatTypes.ColdRes,
            StatTypes.LightRes,
            StatTypes.FireRes,
            StatTypes.AllRes,
            StatTypes.MaxAllRes,

            // Resources
            StatTypes.Life,
            StatTypes.Mana,

            // Attributes
            StatTypes.Strength,
            StatTypes.Intelligence,
            StatTypes.Dexterity,
            StatTypes.IntelligencePercent,
            StatTypes.DexterityPercent,
            StatTypes.StrengthPercent,

            // Attribute scaling
            StatTypes.FlatFirePerStrength,
            StatTypes.FlatLightPerIntelligence,
            StatTypes.FlatColdPerDexterity,
            StatTypes.LifePerStrength,
            StatTypes.DamagePerStrength,
            StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence,
            StatTypes.AttackSpeedPerDexterity,
            StatTypes.AccuracyPerDexterity,
            StatTypes.DmgPerLowestStat,
            StatTypes.ChanceToBlock,
            StatTypes.ChanceToHitTwice
        };

        // WEAPONS
        dict[LootManager.GearType.Weapons] = new List<StatTypes>
        {
            // All damage mods
            StatTypes.GenericDmg,
            StatTypes.GenericDotMult,
            StatTypes.GenericMult,
            StatTypes.PhysDmg,
            StatTypes.ColdDmg,
            StatTypes.LightDmg,
            StatTypes.FireDmg,
            StatTypes.PhysMult,
            StatTypes.ColdMult,
            StatTypes.LightMult,
            StatTypes.FireMult,
            StatTypes.FlatPhys,
            StatTypes.FlatCold,
            StatTypes.FlatLight,
            StatTypes.FlatFire,
            StatTypes.PhysPenetration,
            StatTypes.ColdPenetration,
            StatTypes.LightPenetration,
            StatTypes.FirePenetration,
            StatTypes.CritMult,
            StatTypes.PoisonDmg,
            StatTypes.PoisonMult,
            StatTypes.PoisonChance,
            StatTypes.PoisonTickRate,
            StatTypes.PoisonDuration,
            StatTypes.PoisonPenetration,
            StatTypes.IgniteDmg,
            StatTypes.IgniteMult,
            StatTypes.IgniteChance,
            StatTypes.IgniteTickRate,
            StatTypes.IgniteDuration,
            StatTypes.IgnitePenetration,
            StatTypes.BleedDmg,
            StatTypes.BleedMult,
            StatTypes.BleedChance,
            StatTypes.BleedTickRate,
            StatTypes.BleedDuration,
            StatTypes.BleedPenetration,
            StatTypes.ChanceToHitTwice,

            // Base weapon stats (these will be treated specially in rolling)
            StatTypes.WeaponBaseDmg,
            StatTypes.WeaponBaseAttackSpeed,
            StatTypes.WeaponBaseCrit,

            // Global versions of crit/AS
            StatTypes.CritChance,
            StatTypes.BaseCritChance,
            StatTypes.AttackSpeed
        };

        return dict;
    }
}
