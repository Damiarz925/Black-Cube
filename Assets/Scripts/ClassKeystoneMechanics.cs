using UnityEngine;

// Shared, unit-explicit keystone rules. Runtime and laboratory use the same math.
public static class ClassKeystoneMechanics
{
    public static float FullRageMultiplier(float rage,float effect,bool crown)
    {
        float factor=1+Mathf.Max(0,effect);
        return (1+Mathf.Clamp(rage,0,100)*WeaponMechanicProfile.RageDamagePerPoint*factor)
            *(crown&&rage>=RageState.MaximumRage?PassiveKeystoneState.Value(PassiveKeystone.BarbarianFullRage):1);
    }
    public static float ConsolidatedMultiplier(int extraStrikes)=>1+Mathf.Max(0,extraStrikes)*PassiveKeystoneState.Value(PassiveKeystone.WarriorConsolidation);
    public static float EndlessPoisonMultiplier(int durationTicks)=>PassiveKeystoneState.Value(PassiveKeystone.RangerEndlessPoison)
        *(1+Mathf.Max(0,durationTicks)*ClassKeystoneCatalog.Get(PassiveKeystone.RangerEndlessPoison).secondary);
    public static float TargetLifeMultiplier(bool full)=>full?PassiveKeystoneState.Value(PassiveKeystone.ThiefOpener):ClassKeystoneCatalog.Get(PassiveKeystone.ThiefOpener).secondary;
    public static float AilmentBaseCrit(int distinct)=>Mathf.Clamp(distinct,0,5)*PassiveKeystoneState.Value(PassiveKeystone.ThiefAilmentCrit);
    public static float UncappedChill(float coldDamage,float maxLife,float increased,float effectiveness=1)
        =>coldDamage<=0||maxLife<=0?0:Mathf.Clamp(.05f+coldDamage/maxLife,.05f,.3f)*Mathf.Max(0,1+increased)*Mathf.Max(0,effectiveness);
    public static bool Cull(float life,float maxLife,float threshold)=>life>0&&maxLife>0&&threshold>0&&life<=maxLife*Mathf.Clamp01(threshold);
}
