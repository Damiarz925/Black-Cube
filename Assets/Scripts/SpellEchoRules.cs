using UnityEngine;

// Shared by live combat, tooltips and headless combat. Chance uses fractional gameplay units.
public static class SpellEchoRules
{
    public const float MaximumChance = .60f;
    public const float AdditionalManaPerEcho = .25f;
    // Emergency guard for zero-cost spells and pathological random providers, not a balance cap.
    public const int SafetyLimit = 256;
    public static float EffectiveChance(float chance) => Mathf.Clamp(chance,0,MaximumChance);
    public static float ManaCost(float baseCost,int echoIndex) => Mathf.Max(0,baseCost)*(1+AdditionalManaPerEcho*Mathf.Max(0,echoIndex));
    public static int CastEchoes(float chance,float baseCost,System.Func<float> roll,System.Func<float,bool> tryCastWithCost)
    {
        chance=EffectiveChance(chance);int casts=0;
        if(chance<=0||roll==null||tryCastWithCost==null)return casts;
        for(int index=1;index<=SafetyLimit&&roll()<chance;index++)
        {if(!tryCastWithCost(ManaCost(baseCost,index)))break;casts++;}
        return casts;
    }
}
