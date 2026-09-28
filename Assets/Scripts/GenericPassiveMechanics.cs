using UnityEngine;

public static class GenericPassiveMechanics
{
    // Sceptre is a non-ranged production weapon. Staff is the explicit spell exception.
    public static bool SupportsMultistrike(string weapon) => weapon is WeaponTypeIds.Sword or WeaponTypeIds.TwoHandedAxe or WeaponTypeIds.Dagger or WeaponTypeIds.Sceptre;
    public static float RevengeMultiplier(float fraction,float increasedEffect=0) => 1+2*Mathf.Clamp01(fraction)*(1+Mathf.Max(0,increasedEffect));
    public static float Recovery(float amount,float increasedEffect) => Mathf.Max(0,amount)*(1+Mathf.Max(0,increasedEffect));
    public static float PoisonLeech(float actualPoisonLifeLoss,float leech) => Mathf.Max(0,actualPoisonLifeLoss)*Mathf.Max(0,leech);
    public static StatTypes AuraGrant(int index) => (StatTypes)((int)StatTypes.GrantsPhysicalAura+index);
    public static int AuraIndex(Element element) => element switch {Element.Phys=>0,Element.Fire=>1,Element.Cold=>2,Element.Light=>3,Element.Void=>4,_=>-1};
}

// Shared production/laboratory state. Duration advances once per global combat turn.
public sealed class FiveAuraState
{
    readonly float[] intensities=new float[5];
    readonly int[] turns=new int[5];
    public float Intensity(int index) => index>=0&&index<5&&turns[index]>0?intensities[index]:0;
    public int RemainingTurns(int index) => index>=0&&index<5?turns[index]:0;
    public void Clear(){System.Array.Clear(intensities,0,5);System.Array.Clear(turns,0,5);}
    public void Tick(){for(int i=0;i<5;i++)if(turns[i]>0&&--turns[i]==0)intensities[i]=0;}
    public void RecordTypedHit(int index,float typedDamage,float enemyMaximumLife,bool access)
    {if(!access||index<0||index>=4||typedDamage<=0)return;Refresh(index,SubclassBalanceProfile.AuraIntensity(typedDamage,enemyMaximumLife));}
    public void RecordDamagingAilments(bool bleed,bool ignite,bool poison,bool voidAccess)
    {if(voidAccess&&bleed&&ignite&&poison)Refresh(4,1);}
    void Refresh(int index,float intensity){intensities[index]=Mathf.Clamp01(intensity);turns[index]=2;}
    public float Bonus(int index,float baseValue,float auraEffect) => SubclassBalanceProfile.FinalAuraBonus(baseValue,Intensity(index),auraEffect);
    public float DamageMultiplier(Element element,float auraEffect){int index=GenericPassiveMechanics.AuraIndex(element);return index<0?1:1+Bonus(index,.2f,auraEffect);}
    // Fractional-strength fire auras grant whole extra ticks conservatively.
    public int ExtraIgniteTicks(float auraEffect) => Mathf.FloorToInt(Intensity(1)*(1+Mathf.FloorToInt(Mathf.Max(0,auraEffect)))+.00001f);
}

[RequireComponent(typeof(StatsComponent))]
public sealed class RevengeState:MonoBehaviour
{
    float lastFraction;
    public float StoredFraction=>lastFraction;
    public void RecordHit(float lifeLost,float maximumLife)
    {if(lifeLost>0&&maximumLife>0)lastFraction+=lifeLost/maximumLife;}
    public float ConsumeAttack()
    {float fraction=lastFraction;lastFraction=0;var stats=GetComponent<StatsComponent>();return stats.GetStat(StatTypes.RevengeEffect)>0?GenericPassiveMechanics.RevengeMultiplier(fraction,stats.GetStat(StatTypes.RevengeEffect)):1;}
    public void Clear()=>lastFraction=0;
}
