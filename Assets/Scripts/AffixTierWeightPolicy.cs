// Central quality-bias policy shared by equipment and relic tier rolling.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AffixTierWeightPolicy
{
    public const int ReferenceMaximumLevel = 100;
    public const float QualityBiasAtMaximumLevel = 4f;

    public static float Weight(int authoredWeight, int tierIndex, int weakestTierIndex, int level)
    {
        int qualitySteps = Mathf.Max(0, weakestTierIndex - tierIndex); // T1 is strongest.
        float progress = Mathf.Clamp01((Mathf.Max(1, level) - 1f) / (ReferenceMaximumLevel - 1f));
        return Mathf.Max(1, authoredWeight) * (1f + qualitySteps * QualityBiasAtMaximumLevel * progress);
    }

    public static T Choose<T>(IReadOnlyList<T> eligible, Func<T, int> tierIndex,
        Func<T, int> authoredWeight, int level, float unitRoll)
    {
        if (eligible == null || eligible.Count == 0) return default;
        int weakest = 1;
        for (int i = 0; i < eligible.Count; i++) weakest = Mathf.Max(weakest, tierIndex(eligible[i]));
        float total = 0f;
        for (int i = 0; i < eligible.Count; i++)
            total += Weight(authoredWeight(eligible[i]), tierIndex(eligible[i]), weakest, level);
        float choice = Mathf.Clamp01(unitRoll) * total;
        for (int i = 0; i < eligible.Count; i++)
        {
            choice -= Weight(authoredWeight(eligible[i]), tierIndex(eligible[i]), weakest, level);
            if (choice <= 0f) return eligible[i];
        }
        return eligible[eligible.Count - 1];
    }
}
