// Developer map: Produces per-effect, frequency-weighted summaries for status UI and the shared per-stack tick calculation. This partial class participates in damage math as well as display.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

public partial class StatusController
{
    public struct Summary
    {
        public StatusEffects Effect;
        public int Count, MinTurns, MaxTurns, Threshold, MaximumStackCount;
        public float DamagePerTick, DamagePerTurn, Magnitude, RemainingTotalDamage;
        public bool MixedIntervals;
        public int Interval;
        public string DisplayName => Effect._StatusType != StatusEffects.StatusType.DamageOverTime
            ? Effect._StatusType.ToString() : Effect.Ailment == StatusEffects.AilmentKind.Ignite ? "Burn" : Effect.Ailment.ToString();
        public string Tooltip => Effect._StatusType == StatusEffects.StatusType.DamageOverTime
            ? $"{DisplayName} / {(Effect.Ailment==StatusEffects.AilmentKind.Poison?Count.ToString():Count+"/"+MaximumStackCount)} stacks\n"+
              $"{DamagePerTick:0.##} average damage per active stack tick\n{RemainingTotalDamage:0.##} approximate remaining total damage\n"+
              (MixedIntervals ? "Mixed tick speeds.\n" : Interval > 0 ? $"Ticks every {Interval} {(Effect.Ailment==StatusEffects.AilmentKind.Poison?"global":"afflicted-actor")} turn(s).\n" : $"{1-Interval} ticks per qualifying turn.\n") +
              $"Remaining duration: {MinTurns}-{MaxTurns} qualifying turns.\nDamage includes current ailment resistance."
            : Effect._StatusType == StatusEffects.StatusType.Shock
                ? $"Shock / {Count}/{Threshold} stacks\nTriggered Lightning coefficient: {Magnitude:P0}\nExpires in {MinTurns}-{MaxTurns} global turns."
                : $"Chill / {Magnitude:P1} attack-speed slow\nExpires in {MinTurns}-{MaxTurns} global turns.";
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
                float damage=pair.Key._StatusType == StatusEffects.StatusType.DamageOverTime
                    ? CombatCalculator.CalculateAilmentTickDamage(instance.damagePerTick,pair.Key,instance.sourceStats,stats)
                    : 0f;
                if(s.MaximumStackCount==0)s.MaximumStackCount=EffectiveStackCap(pair.Key,instance.sourceStats);
                s.Count+=instance.stacks; s.Threshold=Mathf.Max(1,instance.threshold); frequency+=rate*instance.stacks;
                s.DamagePerTurn+=damage*rate*instance.stacks; magnitude+=instance.damagePerTick*instance.stacks;
                s.RemainingTotalDamage+=damage*instance.remainingTicks*instance.stacks;
                int turns=pair.Key._StatusType==StatusEffects.StatusType.DamageOverTime
                    ? instance.remainingDurationTurns : instance.remainingTicks;
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
