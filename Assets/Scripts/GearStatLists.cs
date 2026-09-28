// Authoritative ordinary affix pools. Deprecated stat IDs remain serialized but
// are deliberately absent here, so neither natural drops nor crafts generate them.
using System.Collections.Generic;
using UnityEngine;

public class GearStatLists : MonoBehaviour
{
    public static GearStatLists Instance { get; private set; }
    private Dictionary<LootManager.GearType, List<StatTypes>> statPools;
    private static Dictionary<LootManager.GearType, List<StatTypes>> canonicalPools;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        statPools = BuildDefaultStatPools();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
    public List<StatTypes> GetStatPoolForType(LootManager.GearType type) => statPools[type];
    public static List<StatTypes> GetCanonicalStatPoolForType(LootManager.GearType type)
    {
        canonicalPools ??= BuildDefaultStatPools();
        return canonicalPools[type];
    }

    static List<StatTypes> Pool(params StatTypes[] stats) => new(stats);

    public static Dictionary<LootManager.GearType, List<StatTypes>> BuildDefaultStatPools() => new()
    {
        [LootManager.GearType.Weapons] = Pool(
            // Shared: 2 prefixes, 6 suffixes.
            StatTypes.GenericDmg, StatTypes.PlusAllSkills,
            StatTypes.GenericDotMult, StatTypes.BaseCritChance, StatTypes.CritChance,
            StatTypes.CritMult, StatTypes.AttackSpeed, StatTypes.ChanceToHitTwice,
            StatTypes.ProjectileAmount,StatTypes.CooldownReduction,StatTypes.AuraEffect,
            StatTypes.AxePhysicalRage,StatTypes.CullingStrike,
            // Element-specific additions are filtered by ModManager.
            StatTypes.FlatPhys, StatTypes.PhysDmg, StatTypes.PhysPenetration, StatTypes.BleedDmg,
            StatTypes.BleedChance, StatTypes.BleedDuration, StatTypes.BleedTickRate,
            StatTypes.FlatFire, StatTypes.FireDmg, StatTypes.FirePenetration, StatTypes.IgniteDmg,
            StatTypes.IgniteChance, StatTypes.IgniteDuration, StatTypes.IgniteTickRate,
            StatTypes.FlatCold, StatTypes.ColdDmg, StatTypes.ColdPenetration,
            StatTypes.ChillChance, StatTypes.ChillDuration, StatTypes.ChillEffect,
            StatTypes.FlatLight, StatTypes.LightDmg, StatTypes.LightPenetration,
            StatTypes.ShockChance, StatTypes.ShockDuration, StatTypes.ShockEffect,
            StatTypes.FlatVoid, StatTypes.VoidDmg, StatTypes.VoidPenetration, StatTypes.PoisonDmg,
            StatTypes.PoisonChance, StatTypes.PoisonDuration, StatTypes.PoisonTickRate,
            // Guaranteed weapon bases, not ordinary prefix/suffix rolls.
            StatTypes.WeaponBaseDmg, StatTypes.WeaponBaseAttackSpeed, StatTypes.WeaponBaseCrit),

        [LootManager.GearType.Amulets] = Pool(
            StatTypes.CullingStrikeChance,
            StatTypes.GrantsPhysicalAura, StatTypes.GrantsFireAura, StatTypes.GrantsColdAura,
            StatTypes.GrantsLightningAura, StatTypes.GrantsVoidAura,
            StatTypes.GenericDmg, StatTypes.PhysDmg, StatTypes.FireDmg, StatTypes.ColdDmg,
            StatTypes.LightDmg, StatTypes.VoidDmg,
            StatTypes.PhysPenetration, StatTypes.FirePenetration, StatTypes.ColdPenetration,
            StatTypes.LightPenetration, StatTypes.VoidPenetration,
            StatTypes.PoisonDmg, StatTypes.BleedDmg, StatTypes.IgniteDmg,
            StatTypes.Life, StatTypes.Mana, StatTypes.PlusAllSkills,
            StatTypes.FlatFirePerStrength, StatTypes.LifePerStrength, StatTypes.DamagePerStrength,
            StatTypes.FlatLightPerIntelligence, StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence, StatTypes.FlatColdPerDexterity,
            StatTypes.AttackSpeedPerDexterity, StatTypes.DmgPerLowestStat,
            StatTypes.BaseCritChance, StatTypes.CritChance, StatTypes.CritMult,
            StatTypes.GenericDotMult, StatTypes.PoisonTickRate, StatTypes.BleedTickRate,
            StatTypes.IgniteTickRate, StatTypes.PoisonChance, StatTypes.BleedChance,
            StatTypes.IgniteChance, StatTypes.ShockChance, StatTypes.ChillChance,
            StatTypes.PoisonDuration, StatTypes.BleedDuration, StatTypes.IgniteDuration,
            StatTypes.ShockEffect, StatTypes.ChillEffect,
            StatTypes.ChanceToHitTwice, StatTypes.AttackSpeed,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.StrengthPercent, StatTypes.IntelligencePercent, StatTypes.DexterityPercent),

        [LootManager.GearType.Belts] = Pool(
            StatTypes.CullingStrikeChance,
            StatTypes.RageGeneration, StatTypes.RageDecayReduction,
            StatTypes.GenericDmg, StatTypes.Life, StatTypes.Mana,
            StatTypes.FlatFirePerStrength, StatTypes.LifePerStrength, StatTypes.DamagePerStrength,
            StatTypes.FlatLightPerIntelligence, StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence, StatTypes.FlatColdPerDexterity,
            StatTypes.AttackSpeedPerDexterity, StatTypes.DmgPerLowestStat,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.AllRes, StatTypes.MaxAllRes,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.StrengthPercent, StatTypes.IntelligencePercent, StatTypes.DexterityPercent,
            StatTypes.ChanceToHitTwice, StatTypes.AttackSpeed,
            StatTypes.LifeRegeneration, StatTypes.ManaRegeneration,
            StatTypes.LifeOnHit, StatTypes.ManaOnHit),

        [LootManager.GearType.Gloves] = Pool(
            StatTypes.CullingStrikeChance,
            StatTypes.GenericDmg, StatTypes.PhysDmg, StatTypes.FireDmg, StatTypes.ColdDmg,
            StatTypes.LightDmg, StatTypes.VoidDmg,
            StatTypes.PhysPenetration, StatTypes.FirePenetration, StatTypes.ColdPenetration,
            StatTypes.LightPenetration, StatTypes.VoidPenetration,
            StatTypes.PoisonDmg, StatTypes.BleedDmg, StatTypes.IgniteDmg,
            StatTypes.FlatArmour, StatTypes.ArmourPercent, StatTypes.Life, StatTypes.Mana,
            StatTypes.FlatColdPerDexterity, StatTypes.AttackSpeedPerDexterity,
            StatTypes.BaseCritChance, StatTypes.CritChance, StatTypes.CritMult,
            StatTypes.GenericDotMult, StatTypes.PoisonTickRate, StatTypes.BleedTickRate,
            StatTypes.IgniteTickRate, StatTypes.ShockEffect, StatTypes.ChillEffect,
            StatTypes.PoisonChance, StatTypes.BleedChance, StatTypes.IgniteChance,
            StatTypes.ShockChance, StatTypes.ChillChance,
            StatTypes.PoisonDuration, StatTypes.BleedDuration, StatTypes.IgniteDuration,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.AttackSpeed, StatTypes.ChanceToHitTwice,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.DexterityPercent),

        [LootManager.GearType.Rings] = Pool(
            StatTypes.CullingStrikeChance,
            StatTypes.RageGeneration, StatTypes.RageDecayReduction,
            StatTypes.GenericDmg, StatTypes.PhysDmg, StatTypes.FireDmg, StatTypes.ColdDmg,
            StatTypes.LightDmg, StatTypes.VoidDmg,
            StatTypes.PhysPenetration, StatTypes.FirePenetration, StatTypes.ColdPenetration,
            StatTypes.LightPenetration, StatTypes.VoidPenetration, StatTypes.Life, StatTypes.Mana,
            StatTypes.FlatFirePerStrength, StatTypes.DamagePerStrength,
            StatTypes.FlatLightPerIntelligence, StatTypes.DoTMultPerIntelligence,
            StatTypes.FlatColdPerDexterity, StatTypes.AttackSpeedPerDexterity,
            StatTypes.GenericDotMult, StatTypes.CritChance, StatTypes.CritMult,
            StatTypes.IgniteChance, StatTypes.ShockChance, StatTypes.ChillChance,
            StatTypes.ShockEffect, StatTypes.ChillEffect,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.AllRes, StatTypes.AttackSpeed, StatTypes.ChanceToHitTwice,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity),

        [LootManager.GearType.Helmets] = Pool(
            StatTypes.GenericDmg, StatTypes.PhysDmg, StatTypes.FireDmg, StatTypes.ColdDmg,
            StatTypes.LightDmg, StatTypes.VoidDmg,
            StatTypes.PhysPenetration, StatTypes.FirePenetration, StatTypes.ColdPenetration,
            StatTypes.LightPenetration, StatTypes.VoidPenetration,
            StatTypes.PoisonDmg, StatTypes.BleedDmg, StatTypes.IgniteDmg,
            StatTypes.FlatArmour, StatTypes.ArmourPercent, StatTypes.PhysicalDamageReduction,
            StatTypes.ReducedShockEffect, StatTypes.ReducedChillEffect,
            StatTypes.Life, StatTypes.Mana, StatTypes.PlusAllSkills,
            StatTypes.FlatFirePerStrength, StatTypes.LifePerStrength, StatTypes.DamagePerStrength,
            StatTypes.GenericDotMult, StatTypes.PoisonTickRate, StatTypes.BleedTickRate,
            StatTypes.IgniteTickRate, StatTypes.ShockEffect, StatTypes.ChillEffect,
            StatTypes.CritChance, StatTypes.PoisonChance, StatTypes.BleedChance,
            StatTypes.IgniteChance, StatTypes.ShockChance, StatTypes.ChillChance,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.AllRes, StatTypes.MaxFireRes, StatTypes.MaxColdRes,
            StatTypes.MaxLightRes, StatTypes.MaxVoidRes,
            StatTypes.LifeRegeneration, StatTypes.ManaRegeneration,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.StrengthPercent),

        [LootManager.GearType.Boots] = Pool(
            StatTypes.ColdDmg, StatTypes.LightDmg, StatTypes.VoidDmg, StatTypes.PoisonDmg,
            StatTypes.FlatArmour, StatTypes.ArmourPercent,
            StatTypes.ReducedShockEffect, StatTypes.ReducedChillEffect,
            StatTypes.Life, StatTypes.Mana, StatTypes.ManaPercent,
            StatTypes.DmgPerMaxMana, StatTypes.DmgPerCurrentMana,
            StatTypes.FlatLightPerIntelligence, StatTypes.ManaPerIntelligence,
            StatTypes.DoTMultPerIntelligence,
            StatTypes.CritChance, StatTypes.PoisonChance, StatTypes.BleedChance,
            StatTypes.IgniteChance, StatTypes.ShockChance, StatTypes.ChillChance,
            StatTypes.ShockEffect, StatTypes.ChillEffect,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.ManaRegeneration, StatTypes.ManaOnHit, StatTypes.ManaOnKill,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.IntelligencePercent),

        [LootManager.GearType.BodyArmours] = Pool(
            StatTypes.GenericDmg, StatTypes.PhysDmg, StatTypes.FireDmg, StatTypes.ColdDmg,
            StatTypes.LightDmg, StatTypes.VoidDmg,
            StatTypes.FlatArmour, StatTypes.ArmourPercent, StatTypes.PhysicalDamageReduction,
            StatTypes.ReducedShockEffect, StatTypes.ReducedChillEffect,
            StatTypes.Life, StatTypes.Mana, StatTypes.LifePercent, StatTypes.ManaPercent,
            StatTypes.GenericDotMult,
            StatTypes.FireRes, StatTypes.ColdRes, StatTypes.LightRes, StatTypes.VoidRes,
            StatTypes.AllRes, StatTypes.MaxFireRes, StatTypes.MaxColdRes,
            StatTypes.MaxLightRes, StatTypes.MaxVoidRes, StatTypes.MaxAllRes,
            StatTypes.LifeRegeneration, StatTypes.ManaRegeneration,
            StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity,
            StatTypes.StrengthPercent, StatTypes.IntelligencePercent, StatTypes.DexterityPercent)
    };
}
