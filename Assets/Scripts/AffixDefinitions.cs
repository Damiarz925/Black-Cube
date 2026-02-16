using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class AffixTier      //affix tier class, stores tier index, min value, max value, min item level, and weight
{
    [HideInInspector] public int tierIndex;
    public float minValue;
    public float maxValue;
    public int minItemLevel;
    public int weight;
}

[Serializable]
public class AffixDefinitions   //Affix definition class, has a stat type, displayname, allowed gear slots, list of groups, list of tiers, a set multiplier for how much stronger T5 is than T1, and a spread multiplier for the difference between each tier
{
    [Tooltip("Stat that this affix modifies.")]
    public StatTypes statType;

    [Tooltip("Display name used in tooltips, etc")]
    public string displayName;

    [Tooltip("Which gear slots this stat can appear on.")]
    public LootManager.GearType[] allowedSlots;

    [Tooltip("Groups mods together to avoid duplicate mods.")]
    public string[] groups;

    [Tooltip("Tier data for the stat (T1, T2, T3, etc.)")]
    public List<AffixTier> tiers = new();

    [Header("Tier Generation")]
    [Tooltip("How much stronger T5 is than T1 (mid-value).")]
    public float tierTopMultiplier = 6f;

    [Tooltip("Spread factor for min/max around the tier mid-value.")]
    [Range(0f, 0.8f)]
    public float tierSpread = 0.35f;

    // Runtime helper
    public int Id => (int)statType;     //public Id variable for the stat type
    public override string ToString() => $"{statType} ({Id})";      //string for the stat and its id

    /// <summary>
    /// Ensures we have 5 tiers generated based on Tier 1 and the global rules.
    /// Called from ModDatabase.OnValidate.
    /// </summary>
    public void EnsureTiersGenerated()
    {
        const int TOTAL_TIERS = 5;      //number of total tiers, currently hard coded as 5
        if (tiers == null || tiers.Count == 0)      //if tiers is null, or the count is 0, return
            return;

        if (tiers.Count >= TOTAL_TIERS &&       //if the count of tiers, is greater than or equal to total tiers, and tiers at the index 4 has a minilvl greater than 0, and weight greater than 0
            tiers[TOTAL_TIERS - 1].minItemLevel > 0 &&
            tiers[TOTAL_TIERS - 1].weight > 0)
        {
            for (int i = 0; i < tiers.Count; i++)   //for each item in tiers, set the tier index at tiers[i] to i + 1.
                tiers[i].tierIndex = i + 1;
            return;
        }

        int[] minLevels = { 1, 20, 40, 60, 75 };        //hard coded min ilvl for each tier, 1 being T1, 75 being T5
        int[] weights = { 50, 35, 20, 10, 5 };          //hard coded weights for each tier, 50 being T1, 5 being T5

        var t1 = tiers[0];          //t1 defined as first index in tiers, should be t1
        float mid1 = (t1.minValue + t1.maxValue) * 0.5f;        //mid1 is t1's min value plus t1's max value times 0.5. This is the mid point of the T1 roll range

        if (mid1 <= 0f)     //if mid1 is less than or equal to 0
        {
            mid1 = Mathf.Max(Mathf.Abs(t1.minValue), Mathf.Abs(t1.maxValue));       //mid1 is equal to the max between the absolute value of t1's min and the absolute value of t1's max
            if (mid1 <= 0f) mid1 = 1f;      //if mid1 is still less than or equal to 0, mid1 is equal to 1f.
        }

        float topMult = Mathf.Max(1f, tierTopMultiplier);       //top mult is the max between 1 and the tiertopmultiplier
        float r = Mathf.Pow(topMult, 1f / (TOTAL_TIERS - 1));       //r is the result of topmult to the power of 1 / total tiers - 1 (which is 4). So top mult is hardcoded as 6f, but can be changed in inspector. At default it is 6^(1/5-1)-> 6^0.25. (~1.6)
        float spread = Mathf.Clamp01(tierSpread);       //spread if tierspread (0.35 by default) clamped between 0 and 1

        while (tiers.Count < TOTAL_TIERS)       //while the count of tiers is less than total tiers, add a new affix tier
            tiers.Add(new AffixTier());
        if (tiers.Count > TOTAL_TIERS)      //if tiers.count is greater than total tiers, remove the last tier from the list.
            tiers.RemoveRange(TOTAL_TIERS, tiers.Count - TOTAL_TIERS);

        for (int i = 0; i < TOTAL_TIERS; i++)       //loop through the number of total tiers
        {
            float mid = mid1 * Mathf.Pow(r, i); //mid is mid1 * r to the power of i, this would be at default for t1, ((t1min + t1max) * 0.5) * (~1.6^0). t2 would be 1.6^1, and so on. t5 would be 1.6^4. This results in evenly distributed mod values.
            var tier = tiers[i];        //tier is the current tier

            // 🔥 ROUNDING APPLIED HERE
            tier.minValue = Mathf.Round(mid * (1f - spread));       //set the min value for the tier using the calculated mid point multiplied by 1 - the spread
            tier.maxValue = Mathf.Round(mid * (1f + spread));       //set the max value for the tier using the calculated mid point multiplied by 1 + the spread

            tier.minItemLevel = minLevels[i];       //set the min item level using the min levels array
            tier.weight = weights[i];               //set the weight using the weights array
            tier.tierIndex = i + 1;                 //set the tier index to i + 1 (IE. i = 0 would be the first tier, T1 and so on)

            tiers[i] = tier;        //update the list
        }
    }
}
