using UnityEngine;

// Inert in normal players. These values are transient and never serialized into character saves.
public static class DevelopmentOverrides
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static float experience=1,gearDrops=1,currencyDrops=1,enemyRarity=0,itemRarity=0;
    public static string enemyId="";
    public static LootManager.GearType itemType=LootManager.GearType.Weapons;
    public static bool restrictItemType;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset(){experience=gearDrops=currencyDrops=1;enemyRarity=itemRarity=0;enemyId="";restrictItemType=false;}
    public static float Experience=>Mathf.Max(0,experience);
    public static float GearDrops(string id)=>string.IsNullOrEmpty(enemyId)||enemyId==id?Mathf.Max(0,gearDrops):1;
    public static float CurrencyDrops(string id)=>string.IsNullOrEmpty(enemyId)||enemyId==id?Mathf.Max(0,currencyDrops):1;
    public static float EnemyRarity(string id)=>string.IsNullOrEmpty(enemyId)||enemyId==id?Mathf.Max(0,enemyRarity):0;
    public static float ItemRarity(LootManager.GearType? type=null)=>!restrictItemType||type==itemType?Mathf.Max(0,itemRarity):0;
#else
    public static float Experience=>1;
    public static float GearDrops(string id)=>1;
    public static float CurrencyDrops(string id)=>1;
    public static float EnemyRarity(string id)=>0;
    public static float ItemRarity(LootManager.GearType? type=null)=>0;
#endif
}
