using UnityEngine;

// Shared production / laboratory authority. Percentage inputs are fractions.
public static class ShockRules
{
    public const float BaseEffect=.20f, BaseMaximumEffect=1f, BaseDuration=3f;
    public static float Effect(float increased,float maximum=BaseMaximumEffect,float reduced=0)
        => Mathf.Min(Mathf.Max(0,maximum),BaseEffect*Mathf.Max(0,1+increased))*(1-Mathf.Clamp01(reduced));
    public static float Duration(float increased)=>Mathf.Max(.01f,BaseDuration*Mathf.Max(0,1+increased));
    public static int BarrageHits(float sum)=>1+Mathf.FloorToInt(Mathf.Max(0,sum)/.20f+.00001f);
}

public static class ManaBeforeLifeRules
{
    public const string Description="A percentage of post-mitigation damage is taken from Mana before Life. Damage that cannot be absorbed because Mana is empty is taken from Life.";
    public static float Fraction(StatsComponent stats)=>Mathf.Clamp01((stats?.GetStat(StatTypes.DamageTakenFromManaBeforeLife)??0)
        +(stats?.GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.ManaShield)==true?.5f:0));
    public static float LifeDamage(float finalDamage,float fraction,float availableMana,out float absorbed)
    {
        finalDamage=Mathf.Max(0,finalDamage);
        absorbed=Mathf.Min(Mathf.Max(0,availableMana),finalDamage*Mathf.Clamp01(fraction));
        return finalDamage-absorbed;
    }
    public static float Apply(Component target,float damage)
    {
        var mana=target.GetComponent<ManaComponent>();
        if(mana==null)return damage;
        return damage-mana.SpendUpTo(Mathf.Max(0,damage)*Fraction(target.GetComponent<StatsComponent>()));
    }
}
