// Developer map: Session-only inventory highlight criteria. Simple mode matches broad
// affix families; advanced mode matches exact rollable StatTypes. Each affix counts once.
using System;
using System.Collections.Generic;
using System.Linq;

[Flags]
public enum ModFilterCategory : long
{
    None = 0,
    Damage = 1L << 0,
    Physical = 1L << 1,
    Fire = 1L << 2,
    Cold = 1L << 3,
    Lightning = 1L << 4,
    Poison = 1L << 5,
    Bleed = 1L << 6,
    Ignite = 1L << 7,
    Shock = 1L << 8,
    Chill = 1L << 9,
    Critical = 1L << 10,
    Armour = 1L << 11,
    Evasion = 1L << 12,
    Block = 1L << 13,
    ElementalResistance = 1L << 14,
    AilmentResistance = 1L << 15,
    Life = 1L << 16,
    Mana = 1L << 17,
    Speed = 1L << 18,
    Accuracy = 1L << 19,
    Cooldown = 1L << 20,
    Attributes = 1L << 21,
    SkillLevels = 1L << 22,
    Utility = 1L << 23
}

public enum ModFilterMode { Simple, Advanced }

public sealed class InventoryModFilter
{
    public static readonly (ModFilterCategory Category, string Label)[] SimpleCategories =
    {
        (ModFilterCategory.Damage, "Generic Damage"),
        (ModFilterCategory.Physical, "Physical"),
        (ModFilterCategory.Fire, "Fire"),
        (ModFilterCategory.Cold, "Cold"),
        (ModFilterCategory.Lightning, "Lightning"),
        (ModFilterCategory.Poison, "Poison"),
        (ModFilterCategory.Bleed, "Bleed"),
        (ModFilterCategory.Ignite, "Ignite"),
        (ModFilterCategory.Shock, "Shock"),
        (ModFilterCategory.Chill, "Chill"),
        (ModFilterCategory.Critical, "Critical"),
        (ModFilterCategory.Armour, "Armour"),
        (ModFilterCategory.Evasion, "Evasion"),
        (ModFilterCategory.Block, "Block"),
        (ModFilterCategory.ElementalResistance, "Elemental Res"),
        (ModFilterCategory.AilmentResistance, "Ailment Res"),
        (ModFilterCategory.Life, "Life"),
        (ModFilterCategory.Mana, "Mana"),
        (ModFilterCategory.Speed, "Speed"),
        (ModFilterCategory.Accuracy, "Accuracy"),
        (ModFilterCategory.Cooldown, "Cooldown"),
        (ModFilterCategory.Attributes, "Attributes"),
        (ModFilterCategory.SkillLevels, "+ Skill Levels"),
        (ModFilterCategory.Utility, "Utility")
    };

    private readonly HashSet<StatTypes> advancedStats = new();
    private static IReadOnlyList<StatTypes> selectableStats;

    public event Action Changed;
    public ModFilterMode Mode { get; private set; }
    public ModFilterCategory SimpleSelection { get; private set; }
    public IReadOnlyCollection<StatTypes> AdvancedSelection => advancedStats;
    public int RequiredMatches { get; private set; } = 1;
    public bool HasSelection => Mode == ModFilterMode.Simple
        ? SimpleSelection != ModFilterCategory.None
        : advancedStats.Count > 0;

    public static IReadOnlyList<StatTypes> SelectableStats => selectableStats ??=
        GearStatLists.BuildDefaultStatPools().Values
            .SelectMany(stats => stats)
            .Where(stat => !IsIntrinsicWeaponStat(stat))
            .Distinct()
            .OrderBy(AdvancedGroupOrder)
            .ThenBy(ElementOrder)
            .ThenBy(StatDisplayFormatting.ToFriendlyName)
            .ToArray();

    public void SetMode(ModFilterMode value)
    {
        if (Mode == value) return;
        // Modes are intentionally exclusive. UI callers are responsible for any
        // confirmation before reaching this destructive switch.
        SimpleSelection = ModFilterCategory.None;
        advancedStats.Clear();
        Mode = value;
        Changed?.Invoke();
    }

    public void Toggle(ModFilterCategory category)
    {
        SimpleSelection ^= category;
        Changed?.Invoke();
    }

    public void Toggle(StatTypes stat)
    {
        if (!advancedStats.Add(stat)) advancedStats.Remove(stat);
        Changed?.Invoke();
    }

    public bool IsSelected(ModFilterCategory category) => (SimpleSelection & category) != 0;
    public bool IsSelected(StatTypes stat) => advancedStats.Contains(stat);

    public void SetRequiredMatches(int value)
    {
        value = Math.Max(1, Math.Min(6, value));
        if (RequiredMatches == value) return;
        RequiredMatches = value;
        Changed?.Invoke();
    }

    public void ClearCurrentMode()
    {
        if (Mode == ModFilterMode.Simple)
        {
            if (SimpleSelection == ModFilterCategory.None) return;
            SimpleSelection = ModFilterCategory.None;
        }
        else
        {
            if (advancedStats.Count == 0) return;
            advancedStats.Clear();
        }
        Changed?.Invoke();
    }

    public bool Matches(Gear gear) => HasSelection && CountMatches(gear) >= RequiredMatches;

    public int CountMatches(Gear gear)
    {
        if (gear == null || gear.IsScrap || !HasSelection) return 0;
        int count = 0;
        IEnumerable<RolledMod> mods = gear.rolledMods != null && gear.rolledMods.Count > 0
            ? gear.rolledMods
            : gear.globalRolledMods;
        foreach (RolledMod mod in mods)
        {
            if (mod == null || IsIntrinsicWeaponStat(mod.statType)) continue;
            bool match = Mode == ModFilterMode.Simple
                ? (CategoriesFor(mod.statType) & SimpleSelection) != 0
                : advancedStats.Contains(mod.statType);
            if (match) count++;
        }
        return count;
    }

    public static string AdvancedGroup(StatTypes stat)
    {
        if (IsSkillLevel(stat)) return "Skill Levels";
        if (IsCritical(stat)) return "Critical";
        if (IsResistance(stat)) return "Resistances";
        if (stat is StatTypes.FlatArmour or StatTypes.FlatEvasion or StatTypes.ArmourPercent or StatTypes.EvasionPercent or StatTypes.ChanceToBlock)
            return "Armour / Evasion / Block";
        return StatCategoryMapping.GetHeaderName(StatCategoryMapping.GetCategory(stat));
    }

    private static int AdvancedGroupOrder(StatTypes stat) => AdvancedGroup(stat) switch
    {
        "Flat Damage" => 0,
        "Increased Damage" => 1,
        "More Damage" => 2,
        "Penetration" => 3,
        "Damage Over Time" => 4,
        "Ailments" => 5,
        "Critical" => 6,
        "Armour / Evasion / Block" => 7,
        "Resistances" => 8,
        "Resources" => 9,
        "Utility" => 10,
        "Attributes" => 11,
        "Skill Levels" => 12,
        _ => 13
    };

    private static int ElementOrder(StatTypes stat)
    {
        ModFilterCategory tags = CategoriesFor(stat);
        ModFilterCategory[] order = { ModFilterCategory.Physical, ModFilterCategory.Fire, ModFilterCategory.Cold,
            ModFilterCategory.Lightning, ModFilterCategory.Poison, ModFilterCategory.Bleed,
            ModFilterCategory.Ignite, ModFilterCategory.Shock, ModFilterCategory.Chill };
        for (int i = 0; i < order.Length; i++) if ((tags & order[i]) != 0) return i;
        return order.Length;
    }

    private static bool IsIntrinsicWeaponStat(StatTypes stat) => stat is
        StatTypes.WeaponBaseDmg or StatTypes.WeaponBaseAttackSpeed or StatTypes.WeaponBaseCrit;
    private static bool IsSkillLevel(StatTypes stat) => stat is >= StatTypes.Plus1Phys and <= StatTypes.Plus1Ignite;
    private static bool IsCritical(StatTypes stat) => stat is StatTypes.CritChance or StatTypes.CritMult or StatTypes.BaseCritChance;
    private static bool IsResistance(StatTypes stat) => stat is >= StatTypes.ColdRes and <= StatTypes.AllAilmentRes;

    public static ModFilterCategory CategoriesFor(StatTypes stat) => stat switch
    {
        StatTypes.FlatPhys or StatTypes.PhysDmg or StatTypes.PhysMult or StatTypes.PhysPenetration or StatTypes.UnarmedDamage
            => ModFilterCategory.Physical,
        StatTypes.FlatFire or StatTypes.FireDmg or StatTypes.FireMult or StatTypes.FirePenetration
            => ModFilterCategory.Fire,
        StatTypes.FlatCold or StatTypes.ColdDmg or StatTypes.ColdMult or StatTypes.ColdPenetration
            => ModFilterCategory.Cold,
        StatTypes.FlatLight or StatTypes.LightDmg or StatTypes.LightMult or StatTypes.LightPenetration
            => ModFilterCategory.Lightning,
        StatTypes.GenericDmg or StatTypes.GenericMult or StatTypes.GenericDotMult
            => ModFilterCategory.Damage,
        StatTypes.CritChance or StatTypes.CritMult or StatTypes.BaseCritChance
            => ModFilterCategory.Critical,
        StatTypes.PoisonDmg or StatTypes.PoisonMult or StatTypes.PoisonChance or StatTypes.PoisonTickRate or
            StatTypes.PoisonDuration or StatTypes.PoisonPenetration => ModFilterCategory.Poison,
        StatTypes.BleedDmg or StatTypes.BleedMult or StatTypes.BleedChance or StatTypes.BleedTickRate or
            StatTypes.BleedDuration or StatTypes.BleedPenetration => ModFilterCategory.Bleed,
        StatTypes.IgniteDmg or StatTypes.IgniteMult or StatTypes.IgniteChance or StatTypes.IgniteTickRate or
            StatTypes.IgniteDuration or StatTypes.IgnitePenetration => ModFilterCategory.Ignite | ModFilterCategory.Fire,
        StatTypes.ShockChance or StatTypes.ShockEffect or StatTypes.ShockDuration => ModFilterCategory.Shock | ModFilterCategory.Lightning,
        StatTypes.ChillChance or StatTypes.ChillEffect or StatTypes.ChillDuration => ModFilterCategory.Chill | ModFilterCategory.Cold,
        StatTypes.FlatArmour or StatTypes.ArmourPercent => ModFilterCategory.Armour,
        StatTypes.FlatEvasion or StatTypes.EvasionPercent => ModFilterCategory.Evasion,
        StatTypes.ChanceToBlock => ModFilterCategory.Block,
        StatTypes.FireRes or StatTypes.MaxFireRes => ModFilterCategory.Fire | ModFilterCategory.ElementalResistance,
        StatTypes.ColdRes or StatTypes.MaxColdRes => ModFilterCategory.Cold | ModFilterCategory.ElementalResistance,
        StatTypes.LightRes or StatTypes.MaxLightRes => ModFilterCategory.Lightning | ModFilterCategory.ElementalResistance,
        StatTypes.AllRes or StatTypes.MaxAllRes => ModFilterCategory.ElementalResistance,
        StatTypes.PoisonRes => ModFilterCategory.Poison | ModFilterCategory.AilmentResistance,
        StatTypes.BleedRes => ModFilterCategory.Bleed | ModFilterCategory.AilmentResistance,
        StatTypes.IgniteRes => ModFilterCategory.Ignite | ModFilterCategory.Fire | ModFilterCategory.AilmentResistance,
        StatTypes.ShockRes => ModFilterCategory.Shock | ModFilterCategory.Lightning | ModFilterCategory.AilmentResistance,
        StatTypes.ChillRes => ModFilterCategory.Chill | ModFilterCategory.Cold | ModFilterCategory.AilmentResistance,
        StatTypes.AllAilmentRes => ModFilterCategory.AilmentResistance,
        StatTypes.Life or StatTypes.LifePercent or StatTypes.LifeRegeneration or StatTypes.LifeOnHit or StatTypes.LifeOnKill
            => ModFilterCategory.Life,
        StatTypes.Mana or StatTypes.ManaPercent or StatTypes.ManaRegeneration or StatTypes.ManaOnHit or StatTypes.ManaOnKill or
            StatTypes.ManaCost or StatTypes.DmgPerMaxMana or StatTypes.DmgPerCurrentMana => ModFilterCategory.Mana,
        StatTypes.AttackSpeed => ModFilterCategory.Speed,
        StatTypes.Accuracy => ModFilterCategory.Accuracy,
        StatTypes.CooldownRecovery => ModFilterCategory.Cooldown,
        StatTypes.ChanceToHitTwice => ModFilterCategory.Utility,
        StatTypes.Plus1Phys => ModFilterCategory.SkillLevels | ModFilterCategory.Physical,
        StatTypes.Plus1Fire => ModFilterCategory.SkillLevels | ModFilterCategory.Fire,
        StatTypes.Plus1Cold => ModFilterCategory.SkillLevels | ModFilterCategory.Cold,
        StatTypes.Plus1Light => ModFilterCategory.SkillLevels | ModFilterCategory.Lightning,
        StatTypes.Plus1Poison => ModFilterCategory.SkillLevels | ModFilterCategory.Poison,
        StatTypes.Plus1Bleed => ModFilterCategory.SkillLevels | ModFilterCategory.Bleed,
        StatTypes.Plus1Ignite => ModFilterCategory.SkillLevels | ModFilterCategory.Ignite | ModFilterCategory.Fire,
        StatTypes.Strength or StatTypes.Intelligence or StatTypes.Dexterity or StatTypes.StrengthPercent or
            StatTypes.IntelligencePercent or StatTypes.DexterityPercent or StatTypes.DmgPerLowestStat
            => ModFilterCategory.Attributes,
        StatTypes.LifePerStrength => ModFilterCategory.Attributes | ModFilterCategory.Life,
        StatTypes.DamagePerStrength or StatTypes.DoTMultPerIntelligence => ModFilterCategory.Attributes | ModFilterCategory.Damage,
        StatTypes.ManaPerIntelligence => ModFilterCategory.Attributes | ModFilterCategory.Mana,
        StatTypes.AttackSpeedPerDexterity => ModFilterCategory.Attributes | ModFilterCategory.Speed,
        StatTypes.AccuracyPerDexterity => ModFilterCategory.Attributes | ModFilterCategory.Accuracy,
        StatTypes.FlatFirePerStrength => ModFilterCategory.Attributes | ModFilterCategory.Fire,
        StatTypes.FlatLightPerIntelligence => ModFilterCategory.Attributes | ModFilterCategory.Lightning,
        StatTypes.FlatColdPerDexterity => ModFilterCategory.Attributes | ModFilterCategory.Cold,
        _ => ModFilterCategory.None
    };
}
