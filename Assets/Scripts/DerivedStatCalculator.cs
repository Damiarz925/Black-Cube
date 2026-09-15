// Developer map: Query-time projection for attributes and resource-dependent damage.
// No projected value is written back into StatsComponent, so equipment rebinds cannot
// duplicate derived modifiers and the formulas cannot recurse.
using UnityEngine;

public static class DerivedStatCalculator
{
    public static float Attribute(StatsComponent stats, StatTypes attribute)
    {
        if (stats == null) return 0f;
        StatTypes percent = attribute switch
        {
            StatTypes.Strength => StatTypes.StrengthPercent,
            StatTypes.Dexterity => StatTypes.DexterityPercent,
            StatTypes.Intelligence => StatTypes.IntelligencePercent,
            _ => attribute
        };
        if (percent == attribute) return 0f;
        return FinalAttribute(stats.GetRawStat(attribute), stats.GetStat(percent));
    }

    public static float FinalAttribute(float flat, float increased) =>
        Mathf.Max(0f, flat * (1f + increased));

    public static float Strength(StatsComponent stats) => Attribute(stats, StatTypes.Strength);
    public static float Dexterity(StatsComponent stats) => Attribute(stats, StatTypes.Dexterity);
    public static float Intelligence(StatsComponent stats) => Attribute(stats, StatTypes.Intelligence);

    // Inherent attribute effects: one percentage point per ten final attribute.
    public static float StrengthIncreased(StatsComponent stats) => Strength(stats) / 1000f;
    public static float DexterityIncreased(StatsComponent stats) => Dexterity(stats) / 1000f;
    public static float IntelligenceIncreased(StatsComponent stats) => Intelligence(stats) / 1000f;

    public static float AddedLife(StatsComponent stats) =>
        stats == null ? 0f : stats.GetRawStat(StatTypes.LifePerStrength) * Strength(stats);

    public static float AddedMana(StatsComponent stats) =>
        stats == null ? 0f : stats.GetRawStat(StatTypes.ManaPerIntelligence) * Intelligence(stats);

    public static float AddedFlatDamage(StatsComponent stats, Element element)
    {
        if (stats == null) return 0f;
        return element switch
        {
            Element.Fire => stats.GetRawStat(StatTypes.FlatFirePerStrength) * Strength(stats),
            Element.Cold => stats.GetRawStat(StatTypes.FlatColdPerDexterity) * Dexterity(stats),
            Element.Light => stats.GetRawStat(StatTypes.FlatLightPerIntelligence) * Intelligence(stats),
            _ => 0f
        };
    }

    public static float GlobalIncreasedDamage(StatsComponent stats, ManaComponent mana = null)
    {
        if (stats == null) return 0f;
        float strengthGroups = Strength(stats) / 10f;
        float lowestGroups = Mathf.Min(Strength(stats), Mathf.Min(Dexterity(stats), Intelligence(stats))) / 10f;
        float result = stats.GetStat(StatTypes.DamagePerStrength) * strengthGroups
            + stats.GetStat(StatTypes.DmgPerLowestStat) * lowestGroups;
        if (mana != null)
        {
            result += stats.GetStat(StatTypes.DmgPerMaxMana) * (mana.MaxMana / 100f);
            result += stats.GetStat(StatTypes.DmgPerCurrentMana) * (mana.CurrentMana / 100f);
        }
        return result;
    }

    public static float ElementIncreasedDamage(StatsComponent stats, Element element)
    {
        if (element == Element.Phys) return StrengthIncreased(stats);
        return element is Element.Fire or Element.Cold or Element.Light or Element.Void or Element.Poison
            ? IntelligenceIncreased(stats) : 0f;
    }

    public static float AttackSpeedIncreased(StatsComponent stats) => stats == null ? 0f
        : DexterityIncreased(stats) + stats.GetStat(StatTypes.AttackSpeedPerDexterity) * Dexterity(stats);

    public static float ProjectileIncreasedDamage(StatsComponent stats) => DexterityIncreased(stats);

    public static float DotMoreDamage(StatsComponent stats) => stats == null ? 0f
        : stats.GetStat(StatTypes.DoTMultPerIntelligence) * Intelligence(stats);
}
