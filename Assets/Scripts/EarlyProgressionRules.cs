using System;

// Requirements change; enemy rewards deliberately retain the pre-pass curve.
public static class EarlyProgressionRules
{
    static readonly double[] multipliers={.125,.20,.28,.38,.50,.62,.74,.85,.94,1};
    public static double LegacyRequirement(int level)=>Math.Max(1,Math.Round(300*Math.Pow(Math.Pow(619d/300,1d/6),level-10)));
    public static double Requirement(int level)=>level>=100?0:Math.Max(1,Math.Round(LegacyRequirement(level)*multipliers[Math.Min(9,Math.Max(0,level-1))]));
}
