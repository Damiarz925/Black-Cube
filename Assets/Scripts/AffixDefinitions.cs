// Authored, variable-length affix ladders. T1 is always strongest for a family/slot.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum AffixSide { Prefix, Suffix }

[Serializable]
public class AffixTier
{
    [HideInInspector] public int tierIndex;
    public float minValue;
    public float maxValue;
    public int minItemLevel;
    public int weight;
    public bool pairedDamage;
    public float minHighValue;
    public float maxHighValue;
}

[Serializable]
public class AffixDefinitions
{
    public StatTypes statType;
    public string displayName;
    public LootManager.GearType[] allowedSlots;
    public string[] groups;
    public AffixSide side;
    public List<AffixTier> tiers = new();
    // Historical authoring fields stay serialized for existing assets but no
    // longer generate or truncate a universal five-tier ladder.
    public float tierTopMultiplier = 6f;
    [Range(0f,.8f)] public float tierSpread = .35f;

    public int Id => (int)statType;
    public override string ToString() => $"{statType} ({Id})";

    public void EnsureTiersGenerated()
    {
        side = AffixPolicy.Side(statType);
        tiers ??= new List<AffixTier>();
        if (statType is >= StatTypes.Plus1Phys and <= StatTypes.Plus1Ignite && tiers.Count > 1)
            tiers.RemoveRange(1, tiers.Count - 1);
        for (int i=0;i<tiers.Count;i++) tiers[i].tierIndex=tiers.Count-i;
    }
}
