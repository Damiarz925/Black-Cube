// Developer map: Maps damage elements to stat keys for attacker scaling and defender mitigation. Physical hits use armour/physical penetration; unsupported elements retain legacy fallback keys.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public static class StatMappings
{
    /// <summary>
    /// Rolled values remain in their authored raw units inside each stat channel.
    /// Gameplay consumers decide how those channels combine; StatValue handles
    /// independent compounding for the dedicated *Mult damage channels.
    /// </summary>
    public static StatOp GetRolledModifierOperation(StatTypes stat)
    {
        return StatOp.Additive;
    }

    /// <summary>
    /// Global flat damage per element function used to grab the flat dmg stats
    /// </summary>
    public static StatTypes GetFlatDamageStat(Element element)
    {
        switch (element)
        {
            case Element.Phys:
                return StatTypes.FlatPhys;
            case Element.Fire:
                return StatTypes.FlatFire;
            case Element.Cold:
                return StatTypes.FlatCold;
            case Element.Light:
                return StatTypes.FlatLight;
            default:
                return StatTypes.FlatPhys;
        }
    }

    /// <summary>
    /// % increased damage stat for this element.
    /// Function to grab % inc damage for each element
    /// </summary>
    public static StatTypes GetIncDamageStat(Element element)
    {
        switch (element)
        {
            case Element.Phys:
                return StatTypes.PhysDmg;
            case Element.Fire:
                return StatTypes.FireDmg;
            case Element.Cold:
                return StatTypes.ColdDmg;
            case Element.Light:
                return StatTypes.LightDmg;
            default:
                return StatTypes.PhysDmg;
        }
    }

    /// <summary>
    /// % more damage stat for this element.
    /// Function used to grab the % More damage for each element
    /// </summary>
    public static StatTypes GetMoreDamageStat(Element element)
    {
        switch (element)
        {
            case Element.Phys:
                return StatTypes.PhysMult;
            case Element.Fire:
                return StatTypes.FireMult;
            case Element.Cold:
                return StatTypes.ColdMult;
            case Element.Light:
                return StatTypes.LightMult;
            default:
                return StatTypes.PhysMult;
        }
    }

    /// <summary>
    /// Elemental resistance stat.
    /// </summary>
    public static StatTypes GetResistStat(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                return StatTypes.FireRes;
            case Element.Cold:
                return StatTypes.ColdRes;
            case Element.Light:
                return StatTypes.LightRes;
            case Element.Phys:
                throw new System.ArgumentOutOfRangeException(
                    nameof(element), element, "Physical damage uses armour, not a resistance stat.");
            default:
                // Preserve the existing fallback for currently unsupported elements.
                return StatTypes.FireRes;
        }
    }

    /// <summary>
    /// Elemental penetration stat (used when applying damage to enemies).
    /// </summary>
    public static StatTypes GetPenetrationStat(Element element)
    {
        switch (element)
        {
            case Element.Phys:
                return StatTypes.PhysPenetration;
            case Element.Fire:
                return StatTypes.FirePenetration;
            case Element.Cold:
                return StatTypes.ColdPenetration;
            case Element.Light:
                return StatTypes.LightPenetration;
            default:
                return StatTypes.PhysPenetration;
        }
    }
}
