using System.Collections.Generic;
using UnityEngine;

public partial class StatusController
{
    readonly Dictionary<StatusEffects,float> averagedTicks = new();
    public struct Summary
    {
        public StatusEffects Effect;
        public int Count, MinTurns, MaxTurns;
        public float DamagePerTick, DamagePerTurn, Magnitude;
        public bool MixedIntervals;
        public int Interval;
        public string DisplayName => Effect._StatusType != StatusEffects.StatusType.DamageOverTime
            ? Effect._StatusType.ToString() : Effect.Ailment == StatusEffects.AilmentKind.Ignite ? "Burn" : Effect.Ailment.ToString();
        public string Tooltip => Effect._StatusType == StatusEffects.StatusType.DamageOverTime
            ? $"{DisplayName} / {Count} stacks\n{DamagePerTick:0.##} damage per stack per tick\n{DamagePerTurn:0.##} total damage per global turn (average)\n" +
              (MixedIntervals ? "Mixed tick speeds; frequency-weighted mean.\n" : Interval > 0 ? $"Ticks every {Interval} global turn(s).\n" : $"{1-Interval} ticks per global turn.\n") +
              $"Expires in {MinTurns}-{MaxTurns} global turns.\nDamage includes current ailment resistance."
            : $"{DisplayName} / {Count} stacks\nAverage strength: {Magnitude:0.##}\nExpires in {MinTurns}-{MaxTurns} global turns.\nNon-damaging effect; gameplay response is currently a placeholder.";
    }
    public void ClearStatuses()
    { StatusDictionary.Clear(); IndependentDictionary.Clear(); StopAllCoroutines(); }
    public void RemoveStatus(StatusEffects effect)
    { StatusDictionary.Remove(effect); IndependentDictionary.Remove(effect); }
    public List<Summary> GetStatusSummaries()
    {
        var groups = new Dictionary<StatusEffects,List<StatusInstance>>();
        foreach (var pair in StatusDictionary) groups[pair.Key] = new List<StatusInstance>{pair.Value};
        foreach (var pair in IndependentDictionary)
        {
            if (!groups.TryGetValue(pair.Key,out var list)) groups[pair.Key]=list=new List<StatusInstance>();
            list.AddRange(pair.Value);
        }
        var result = new List<Summary>();
        foreach (var pair in groups)
        {
            if (pair.Key == null) continue;
            var s = new Summary { Effect=pair.Key, MinTurns=int.MaxValue };
            float frequency=0, magnitude=0;
            foreach (var instance in pair.Value)
            {
                if (instance.stacks<=0 || instance.remainingTicks<=0) continue;
                if (s.Count==0) s.Interval=instance.effectiveInterval;
                else if(s.Interval!=instance.effectiveInterval) s.MixedIntervals=true;
                float rate=instance.effectiveInterval>0 ? 1f/instance.effectiveInterval : 1-instance.effectiveInterval;
                float damage=CombatCalculator.CalculateAilmentTickDamage(instance.damagePerTick,pair.Key,instance.sourceStats,stats);
                s.Count+=instance.stacks; frequency+=rate*instance.stacks;
                s.DamagePerTurn+=damage*rate*instance.stacks; magnitude+=instance.damagePerTick*instance.stacks;
                int turns=instance.effectiveInterval>0
                    ? Mathf.Max(1,instance.turnsUntilNextTick)+(instance.remainingTicks-1)*instance.effectiveInterval
                    : Mathf.CeilToInt(instance.remainingTicks/rate);
                s.MinTurns=Mathf.Min(s.MinTurns,turns);s.MaxTurns=Mathf.Max(s.MaxTurns,turns);
            }
            if(s.Count==0) continue;
            s.DamagePerTick=frequency>0?s.DamagePerTurn/frequency:0;s.Magnitude=magnitude/s.Count;
            result.Add(s);
        }
        result.Sort((a,b)=>string.CompareOrdinal(a.DisplayName,b.DisplayName));
        return result;
    }
}
