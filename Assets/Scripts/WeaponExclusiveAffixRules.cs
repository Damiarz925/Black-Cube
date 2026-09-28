using System.Collections.Generic;
using UnityEngine;

// One authority for weapon eligibility and level-gated exclusive suffix ladders.
public static class WeaponExclusiveAffixRules
{
    public static string Weapon(StatTypes stat)=>stat switch
    {
        StatTypes.ChanceToHitTwice=>WeaponTypeIds.Sword,
        StatTypes.ProjectileAmount=>WeaponTypeIds.Bow,
        StatTypes.CooldownReduction=>WeaponTypeIds.Staff,
        StatTypes.AuraEffect=>WeaponTypeIds.Sceptre,
        StatTypes.AxePhysicalRage=>WeaponTypeIds.TwoHandedAxe,
        StatTypes.CullingStrike=>WeaponTypeIds.Dagger,
        _=>null
    };
    public static bool Allows(StatTypes stat,string weapon)=>Weapon(stat)==null||Weapon(stat)==weapon;
    public static bool TryGet(StatTypes stat,LootManager.GearType slot,string weapon,out List<AffixTier> tiers)
    {
        tiers=null;if(slot!=LootManager.GearType.Weapons||Weapon(stat)==null)return false;
        tiers=new();if(!Allows(stat,weapon))return true;
        float[] values=stat switch
        {
            StatTypes.ChanceToHitTwice=>new[]{5f,8f,11f,14f,18f},
            StatTypes.ProjectileAmount=>new[]{1f,1f,1f,1f,2f},
            StatTypes.CooldownReduction=>new[]{8f,12f,16f,20f,25f},
            StatTypes.AuraEffect=>new[]{10f,20f,30f,40f,50f},
            StatTypes.AxePhysicalRage=>new[]{5f,7.5f,10f,12.5f,15f},
            _=>new[]{5f,7.5f,10f,12.5f,15f}
        };
        for(int i=0;i<values.Length;i++)tiers.Add(new AffixTier{tierIndex=values.Length-i,minItemLevel=50+i*10,weight=100,
            minValue=stat==StatTypes.ProjectileAmount?values[i]:values[i]*.8f,maxValue=values[i],
            pairedDamage=stat==StatTypes.AxePhysicalRage,minHighValue=stat==StatTypes.AxePhysicalRage?(10+i*5)*.8f:0,maxHighValue=stat==StatTypes.AxePhysicalRage?10+i*5:0});
        return true;
    }
}
