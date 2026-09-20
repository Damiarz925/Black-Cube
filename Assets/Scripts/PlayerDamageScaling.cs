// Central additive player-damage contributions from weapon attribute identity and level.
using UnityEngine;

public readonly struct WeaponAttributeScalingDefinition
{
    public readonly string WeaponTypeId,DisplayName;
    public readonly StatTypes Primary,Secondary;
    public readonly float PrimaryPerPoint,SecondaryPerPoint;
    public bool IsDual=>Secondary!=Primary;
    public WeaponAttributeScalingDefinition(string weapon,string name,StatTypes primary,float primaryRate,StatTypes secondary,float secondaryRate)
    {WeaponTypeId=weapon;DisplayName=name;Primary=primary;Secondary=secondary;PrimaryPerPoint=primaryRate;SecondaryPerPoint=secondaryRate;}
}

public static class WeaponAttributeScalingProfile
{
    static readonly WeaponAttributeScalingDefinition[] definitions={
        new(WeaponTypeIds.Sword,"Strength + Dexterity",StatTypes.Strength,.0025f,StatTypes.Dexterity,.0025f),
        new(WeaponTypeIds.TwoHandedAxe,"Strength",StatTypes.Strength,.005f,StatTypes.Strength,0),
        new(WeaponTypeIds.Bow,"Dexterity",StatTypes.Dexterity,.005f,StatTypes.Dexterity,0),
        new(WeaponTypeIds.Staff,"Intelligence",StatTypes.Intelligence,.005f,StatTypes.Intelligence,0),
        new(WeaponTypeIds.Dagger,"Dexterity + Intelligence",StatTypes.Dexterity,.0025f,StatTypes.Intelligence,.0025f),
        new(WeaponTypeIds.Sceptre,"Strength + Intelligence",StatTypes.Strength,.0025f,StatTypes.Intelligence,.0025f)};
    public static WeaponAttributeScalingDefinition Get(string weaponTypeId)
    {
        foreach(var definition in definitions)if(definition.WeaponTypeId==weaponTypeId)return definition;
        return new WeaponAttributeScalingDefinition(weaponTypeId??string.Empty,"None",StatTypes.Strength,0,StatTypes.Strength,0);
    }
    public static float IncreasedDamage(string weaponTypeId,StatsComponent stats)
    {
        if(stats==null)return 0f;var definition=Get(weaponTypeId);
        float result=Attribute(stats,definition.Primary)*definition.PrimaryPerPoint;
        if(definition.IsDual)result+=Attribute(stats,definition.Secondary)*definition.SecondaryPerPoint;
        return Mathf.Max(0,result);
    }
    public static string ScalingAttributes(string weaponTypeId)=>Get(weaponTypeId).DisplayName;
    static float Attribute(StatsComponent stats,StatTypes type)=>type switch
    {
        StatTypes.Strength=>DerivedStatCalculator.Strength(stats),
        StatTypes.Dexterity=>DerivedStatCalculator.Dexterity(stats),
        StatTypes.Intelligence=>DerivedStatCalculator.Intelligence(stats),
        _=>0f
    };
}

public static class PlayerLevelDamageProfile
{
    public const float IncreasedDamagePerLevelAboveOne=.0025f;
    public static int CappedLevel(int level)=>Mathf.Clamp(level,1,100);
    public static float IncreasedDamage(int level)=>(CappedLevel(level)-1)*IncreasedDamagePerLevelAboveOne;
}
