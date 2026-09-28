using UnityEngine;

public static class CullingRules
{
    public const float BaseThreshold=.10f;
    public static float Threshold(float ordinaryThreshold,bool lastBreath,int poisonStacks,int bleedStacks)
        =>lastBreath?Mathf.Max(0,poisonStacks)*.002f+Mathf.Max(0,bleedStacks)*.01f:Mathf.Max(BaseThreshold,ordinaryThreshold);
    public static bool Qualifies(float life,float maximum,float threshold,float chance,float roll)
        =>life>0&&maximum>0&&threshold>0&&life/maximum<=threshold&&roll<Mathf.Clamp01(chance);
    public static void TryExecute(HealthComponent target,StatsComponent attacker)
    {
        if(target==null||attacker?.GetComponent<PlayerController>()==null||target.GetComponent<PlayerController>()!=null)return;
        float chance=attacker.GetStat(StatTypes.CullingStrikeChance);if(chance<=0)return;
        var statuses=target.GetComponent<StatusController>();
        float threshold=Threshold(attacker.GetStat(StatTypes.CullingStrike),UniqueCatalog.Power(attacker,UniquePower.LastBreath)>0,statuses?.AilmentStackCount(StatusEffects.AilmentKind.Poison)??0,statuses?.AilmentStackCount(StatusEffects.AilmentKind.Bleed)??0);
        if(target.CurrentLife<=0||target.MaxLife<=0||target.CurrentLife/target.MaxLife>threshold)return;
        if(Qualifies(target.CurrentLife,target.MaxLife,threshold,chance,Random.value))target.LoseLife(target.CurrentLife);
    }
}
