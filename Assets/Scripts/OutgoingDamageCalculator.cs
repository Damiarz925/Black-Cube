/// <summary>Shared pre-crit, pre-mitigation outgoing hit scaling for players and enemies.</summary>
public static class OutgoingDamageCalculator
{
    public static float Calculate(float baseAmount, float flatAmount, Element element, StatsComponent stats)
    {
        if (stats == null) return baseAmount + flatAmount;

        float increased = stats.GetStat(StatMappings.GetIncDamageStat(element))
                        + stats.GetStat(StatTypes.GenericDmg);

        // Each applicable roll remains an independent factor, including rolls of
        // the same stat from different equipment sources.
        float more = stats.GetMoreDamageMultiplier(StatMappings.GetMoreDamageStat(element))
                   * stats.GetMoreDamageMultiplier(StatTypes.GenericMult);

        return (baseAmount + flatAmount) * (1f + increased) * more;
    }
}
