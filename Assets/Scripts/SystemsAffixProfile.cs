using System.Collections.Generic;
using UnityEngine;

// Explicit first-pass ladders; overrides only these new/redesigned families.
public static class SystemsAffixProfile
{
    public static bool TryTiers(StatTypes stat,LootManager.GearType slot,out List<AffixTier> tiers)
    {
        tiers=null;
        if(stat is StatTypes.PoisonDuration or StatTypes.BleedDuration or StatTypes.IgniteDuration
            or StatTypes.PoisonTickRate or StatTypes.BleedTickRate or StatTypes.IgniteTickRate)
        {tiers=Ladder(new[]{3f,6,10,15,20},new[]{5f,9,14,19,25});return true;}
        if((stat is StatTypes.RageGeneration or StatTypes.RageDecayReduction)&&(slot is LootManager.GearType.Rings or LootManager.GearType.Belts))
        {
            float premium=slot==LootManager.GearType.Belts?1.5f:1f;
            tiers=Ladder(stat==StatTypes.RageGeneration?new[]{10f,20,30,40,50}:new[]{3f,6,9,12,15},stat==StatTypes.RageGeneration?new[]{20f,30,40,50,70}:new[]{5f,9,12,15,20},premium);return true;
        }
        if(stat==StatTypes.CullingStrikeChance&&(slot is LootManager.GearType.Rings or LootManager.GearType.Belts or LootManager.GearType.Amulets or LootManager.GearType.Gloves))
        {tiers=Ladder(new[]{3f,6,10,15,20},new[]{5f,9,14,19,25});return true;}
        return false;
    }
    static List<AffixTier> Ladder(float[] min,float[] max,float factor=1)
    {var result=new List<AffixTier>();for(int i=0;i<5;i++)result.Add(new(){tierIndex=5-i,minItemLevel=new[]{1,20,40,60,80}[i],weight=new[]{50,35,20,10,5}[i],minValue=min[i]*factor,maxValue=max[i]*factor});return result;}
    public static AffixDefinitions Definition(StatTypes stat)
    {
        if(stat!=StatTypes.CullingStrikeChance)return null;
        TryTiers(stat,LootManager.GearType.Rings,out var tiers);
        return new(){statType=stat,displayName="Culling Strike Chance",side=AffixSide.Suffix,tiers=tiers,allowedSlots=new[]{LootManager.GearType.Rings,LootManager.GearType.Belts,LootManager.GearType.Amulets,LootManager.GearType.Gloves},allowedWeaponTypeIds=System.Array.Empty<string>()};
    }
}
