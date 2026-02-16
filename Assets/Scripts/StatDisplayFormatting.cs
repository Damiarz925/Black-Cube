using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class StatDisplayFormatting
{
    // Optional: override names that look bad when auto-split
    private static readonly Dictionary<StatTypes, string> Overrides = new()
    {
        { StatTypes.FlatPhys, "Flat Physical Damage" },
        { StatTypes.FlatFire, "Flat Fire Damage" },
        { StatTypes.FlatCold, "Flat Cold Damage" },
        { StatTypes.FlatLight, "Flat Lightning Damage" },
        { StatTypes.GenericDmg, "Increased Damage" },
        { StatTypes.GenericMult, "More Damage" },
        { StatTypes.GenericDotMult, "More DoT Damage" },
        { StatTypes.CritMult, "Critical Damage Multiplier" },
        { StatTypes.AllRes, "All Elemental Resist" },
        { StatTypes.AllAilmentRes, "All Ailment Resist" },
        { StatTypes.ManaCost, "Mana Cost" },
        { StatTypes.ChanceToHitTwice, "Chance to Hit Twice" },
    };

    public static string ToFriendlyName(StatTypes t)
    {
        if (Overrides.TryGetValue(t, out var s))
            return s;

        // Split PascalCase: MaxFireRes -> "Max Fire Res"
        var name = t.ToString();
        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        // Small Cleanup
        name = name.Replace("Dmg", "Damage");
        name = name.Replace("Mult", "Multiplier");
        name = name.Replace("Phys", "Physical");
        name = name.Replace("Light", "Lightning");

        return name;
    }

    /// <summary>
    /// Uses RAW values so percent stats show as 0-100 numbers
    /// </summary>
    
    public static string FormatValue(StatsComponent stats, StatTypes type)
    {
        float raw = stats.GetRawStat(type);

        // Hide near-zero from floats
        if (System.MathF.Abs(raw) < 0.0001f)
            return "0";

        bool isPercent = StatsComponent.IsPercentStat(type);

        if (isPercent)
        {
            // Raw is already 0-100 for percent stats
            return $"{raw:0.##}%";
        }

        // Flat stats
        return raw.ToString("0.##");
    }

    public static bool ShouldDisplay (StatsComponent stats, StatTypes type, float epsilon = 0.0001f)
    {
        float raw = stats.GetRawStat(type);
        return System.MathF.Abs(raw) > epsilon;
    }
}