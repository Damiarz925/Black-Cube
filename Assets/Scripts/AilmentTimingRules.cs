using UnityEngine;

// Damaging ailments use scaled game seconds, never attack/global-turn counts.
public static class AilmentTimingRules
{
    public static bool IsDamaging(StatusEffects.AilmentKind kind)=>kind is StatusEffects.AilmentKind.Poison or StatusEffects.AilmentKind.Bleed or StatusEffects.AilmentKind.Ignite;
    public static float BaseDuration(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Poison?1f:4f;
    public static float BaseInterval(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Poison?.5f:kind==StatusEffects.AilmentKind.Bleed?1f:2f;
    public static float Coefficient(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Poison?.05f:kind==StatusEffects.AilmentKind.Bleed?.20f:.50f;
    public static int BaseTicks(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Bleed?4:2;
    public static StatTypes DurationStat(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Poison?StatTypes.PoisonDuration:kind==StatusEffects.AilmentKind.Bleed?StatTypes.BleedDuration:StatTypes.IgniteDuration;
    public static StatTypes SpeedStat(StatusEffects.AilmentKind kind)=>kind==StatusEffects.AilmentKind.Poison?StatTypes.PoisonTickRate:kind==StatusEffects.AilmentKind.Bleed?StatTypes.BleedTickRate:StatTypes.IgniteTickRate;
    public static float Duration(StatusEffects.AilmentKind kind,float increased)=>BaseDuration(kind)*Mathf.Max(.01f,1+increased);
    public static float Interval(StatusEffects.AilmentKind kind,float increased)=>Mathf.Max(.02f,BaseInterval(kind)/Mathf.Max(.01f,1+increased));
    public static int TickCount(float duration,float interval)=>Mathf.Max(0,Mathf.FloorToInt((duration+.00001f)/interval));
    public static void Timing(StatusEffects.AilmentKind kind,StatsComponent stats,out float duration,out float interval)
    {
        duration=Duration(kind,stats!=null?stats.GetStat(DurationStat(kind)):0);
        if(kind==StatusEffects.AilmentKind.Bleed&&UniqueCatalog.Power(stats,UniquePower.Rupture)>0)duration*=.5f;
        float speed=stats!=null?stats.GetStat(SpeedStat(kind)):0;
        if(kind==StatusEffects.AilmentKind.Poison&&stats!=null)speed+=stats.GetStat(StatTypes.PoisonSpeed);
        interval=Interval(kind,speed);
        if(kind==StatusEffects.AilmentKind.Ignite&&stats!=null)duration+=(stats.GetComponent<SubclassCombatState>()?.AuraIgniteTicks??0)*BaseInterval(kind);
    }
}
