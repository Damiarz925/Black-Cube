using UnityEngine;

public static class ItemArmourProfile
{
    // Documented level-100 Rare reference. This is a hit-size comparison, not
    // a claim that the Cinder Wastes' Fire damage is mitigated by Armour.
    public const float ReferenceHit = 2053.714f;
    public static bool IsArmour(LootManager.GearType slot) => slot is LootManager.GearType.BodyArmours
        or LootManager.GearType.Helmets or LootManager.GearType.Gloves or LootManager.GearType.Boots;
    public static float Base(LootManager.GearType slot,int level)
    {
        float maximum=slot switch{LootManager.GearType.BodyArmours=>4454f,LootManager.GearType.Helmets=>2475f,
            LootManager.GearType.Gloves or LootManager.GearType.Boots=>1485f,_=>0};
        return Mathf.Round(maximum*(.02f+.98f*Mathf.Pow(Mathf.Clamp(level,1,100)/100f,1.65f)));
    }
    public static float Item(float basis,float flat,float localPercent)=>Mathf.Max(0,basis+flat)*Mathf.Max(0,1+localPercent);
    public static float Final(StatsComponent stats)=>Mathf.Max(0,stats.GetStat(StatTypes.FlatArmour))*Mathf.Max(0,1+stats.GetStat(StatTypes.ArmourPercent));
    public static float RawReduction(float armour,float hit,float explicitReduction=0)=>Mathf.Max(0,armour)/(Mathf.Max(0,armour)+10*Mathf.Max(.001f,hit))+explicitReduction;
    public static string Diagnose(Gear item)=>$"Base {item.BaseArmour:0}; local flat {item.LocalFlatArmour:0}; local increased {item.LocalArmourPercent:P1}; item {item.FinalItemArmour:0}";
}
