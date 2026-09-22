using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EnemyBehaviorCondition
{
    Always,
    FirstAction,
    EveryNAttacks,
    AfterNAttacks,
    SelfLifeBelow,
    SelfLifeAbove,
    PlayerLifeBelow,
    PlayerLifeAbove,
    PlayerHasAilment,
    SelfHasAilment,
    PlayerDoesNotHaveAilment,
    CooldownReady,
    CorruptionAtLeast,
    BossPhase,
    AfterTakingNHits,
    AfterDealingNHits,
    OncePerEncounter,
    WeightedRandom
}

[Flags] public enum EnemyBehaviorAilment
{ None=0,Bleed=1,Poison=2,Ignite=4,Shock=8,Chill=16,Freeze=32 }

[Serializable]
public sealed class EnemyActionRule
{
    public string stableId;
    public string skillId;
    public EnemyBehaviorCondition condition=EnemyBehaviorCondition.Always;
    public int priority;
    [Min(.0001f)] public float weight=1f;
    [Min(1)] public int everyNAttacks=1;
    [Min(0)] public int afterNAttacks;
    [Range(0f,1f)] public float threshold=.5f;
    [Range(0,100)] public int corruptionAtLeast;
    public string bossPhaseId;
    public EnemyBehaviorAilment ailment;
    [Min(0)] public int hitCount;
    [Min(0)] public int cooldownTurns;
    [Min(0)] public int minimumAttacksSinceLastUse;
    public bool oncePerEncounter;
    [Min(0)] public int maximumUses;
    public bool fallbackEligible=true;
}

[Serializable]
public sealed class EnemyBehaviorProfileDefinition
{
    public string stableId;
    public string displayName;
    public string sourceLoadoutId;
    [Tooltip("Preserves the exact Step 19 rotating cadence resolver while making cadence values authorable.")]
    public bool preserveLegacyRotatingCadence=true;
    public List<EnemyActionRule> rules=new();
}

public sealed class EnemyBehaviorRuntimeState
{
    public int completedAttacks;
    public float selfLifeFraction=1f;
    public float playerLifeFraction=1f;
    public int corruption;
    public string bossPhaseId;
    public EnemyBehaviorAilment playerAilments,selfAilments;
    public int hitsTaken,hitsDealt;
    public float random01=.5f;
    public readonly Dictionary<string,int> uses=new();
    public readonly Dictionary<string,int> lastUseAttack=new();
}

[Serializable]
public sealed class EnemyBehaviorRuleTrace
{
    public string ruleId,skillId,reason;
    public bool eligible;
    public int priority;
    public float weight,randomRoll;
}

[Serializable]
public sealed class EnemyBehaviorDecision
{
    public string profileId,selectedRuleId,selectedSkillId,reason;
    public List<EnemyBehaviorRuleTrace> traces=new();
}

public static class EnemyBehaviorResolver
{
    public static EnemyBehaviorDecision Resolve(WorldContentDatabase db,EnemyBehaviorProfileDefinition profile,EnemyBehaviorRuntimeState state)
    {
        state??=new EnemyBehaviorRuntimeState();
        var decision=new EnemyBehaviorDecision{profileId=profile?.stableId};
        if(profile?.rules==null||profile.rules.Count==0)return decision;
        if(profile.preserveLegacyRotatingCadence)return ResolveLegacy(db,profile,state,decision);
        var eligible=new List<EnemyActionRule>();
        foreach(var rule in profile.rules)
        {
            string reason;bool ok=Eligible(db,rule,state,out reason);
            decision.traces.Add(new EnemyBehaviorRuleTrace{ruleId=rule?.stableId,skillId=rule?.skillId,eligible=ok,priority=rule?.priority??0,weight=rule?.weight??0,randomRoll=state.random01,reason=reason});
            if(ok)eligible.Add(rule);
        }
        if(eligible.Count==0)eligible.AddRange(profile.rules.Where(x=>x!=null&&x.fallbackEligible&&db?.EnemySkill(x.skillId)!=null));
        if(eligible.Count==0)return decision;
        int top=eligible.Max(x=>x.priority);var group=eligible.Where(x=>x.priority==top).ToList();
        EnemyActionRule selected=group[0];
        if(group.Count>1)
        {
            float total=group.Sum(x=>Mathf.Max(.0001f,x.weight));float roll=Mathf.Clamp01(state.random01)*total;
            foreach(var candidate in group){roll-=Mathf.Max(.0001f,candidate.weight);if(roll<=0){selected=candidate;break;}}
        }
        Record(selected,state);decision.selectedRuleId=selected.stableId;decision.selectedSkillId=selected.skillId;decision.reason=$"Highest eligible priority {top}";return decision;
    }

    static EnemyBehaviorDecision ResolveLegacy(WorldContentDatabase db,EnemyBehaviorProfileDefinition profile,EnemyBehaviorRuntimeState state,EnemyBehaviorDecision decision)
    {
        int turn=Mathf.Max(0,state.completedAttacks),count=profile.rules.Count;
        for(int offset=0;offset<count;offset++)
        {
            var rule=profile.rules[(turn+offset)%count];var skill=db?.EnemySkill(rule?.skillId);int cadence=Mathf.Max(1,rule?.everyNAttacks??1);bool ok=skill!=null&&(turn+1)%cadence==0;
            decision.traces.Add(new EnemyBehaviorRuleTrace{ruleId=rule?.stableId,skillId=rule?.skillId,eligible=ok,priority=rule?.priority??0,weight=rule?.weight??0,reason=skill==null?"Missing skill":ok?$"Action {turn+1} matches every {cadence}":$"Action {turn+1} does not match every {cadence}"});
            if(!ok)continue;Record(rule,state);decision.selectedRuleId=rule.stableId;decision.selectedSkillId=rule.skillId;decision.reason="Legacy rotating cadence matched";return decision;
        }
        var fallback=profile.rules[turn%count];Record(fallback,state);decision.selectedRuleId=fallback.stableId;decision.selectedSkillId=fallback.skillId;decision.reason="No cadence matched; rotating fallback";return decision;
    }

    static bool Eligible(WorldContentDatabase db,EnemyActionRule rule,EnemyBehaviorRuntimeState state,out string reason)
    {
        if(rule==null||db?.EnemySkill(rule.skillId)==null){reason="Missing rule or skill";return false;}
        int uses=state.uses.TryGetValue(rule.stableId,out int u)?u:0;
        if((rule.oncePerEncounter||rule.condition==EnemyBehaviorCondition.OncePerEncounter)&&uses>0){reason="Already used once";return false;}
        if(rule.maximumUses>0&&uses>=rule.maximumUses){reason="Maximum uses reached";return false;}
        if(state.lastUseAttack.TryGetValue(rule.stableId,out int last)&&state.completedAttacks-last<Mathf.Max(rule.cooldownTurns,rule.minimumAttacksSinceLastUse)){reason="Cooldown/cadence not ready";return false;}
        bool ok=rule.condition switch
        {
            EnemyBehaviorCondition.FirstAction=>state.completedAttacks==0,
            EnemyBehaviorCondition.EveryNAttacks=>(state.completedAttacks+1)%Mathf.Max(1,rule.everyNAttacks)==0,
            EnemyBehaviorCondition.AfterNAttacks=>state.completedAttacks>=rule.afterNAttacks,
            EnemyBehaviorCondition.SelfLifeBelow=>state.selfLifeFraction<=rule.threshold,
            EnemyBehaviorCondition.SelfLifeAbove=>state.selfLifeFraction>=rule.threshold,
            EnemyBehaviorCondition.PlayerLifeBelow=>state.playerLifeFraction<=rule.threshold,
            EnemyBehaviorCondition.PlayerLifeAbove=>state.playerLifeFraction>=rule.threshold,
            EnemyBehaviorCondition.PlayerHasAilment=>(state.playerAilments&rule.ailment)!=0,
            EnemyBehaviorCondition.SelfHasAilment=>(state.selfAilments&rule.ailment)!=0,
            EnemyBehaviorCondition.PlayerDoesNotHaveAilment=>(state.playerAilments&rule.ailment)==0,
            EnemyBehaviorCondition.CooldownReady=>!state.lastUseAttack.TryGetValue(rule.stableId,out int readyLast)||state.completedAttacks-readyLast>=Mathf.Max(rule.cooldownTurns,rule.minimumAttacksSinceLastUse),
            EnemyBehaviorCondition.CorruptionAtLeast=>state.corruption>=rule.corruptionAtLeast,
            EnemyBehaviorCondition.BossPhase=>state.bossPhaseId==rule.bossPhaseId,
            EnemyBehaviorCondition.AfterTakingNHits=>state.hitsTaken>=rule.hitCount,
            EnemyBehaviorCondition.AfterDealingNHits=>state.hitsDealt>=rule.hitCount,
            _=>true
        };
        reason=ok?rule.condition.ToString():$"{rule.condition} condition rejected";return ok;
    }
    static void Record(EnemyActionRule rule,EnemyBehaviorRuntimeState state){if(rule==null)return;state.uses[rule.stableId]=state.uses.TryGetValue(rule.stableId,out int n)?n+1:1;state.lastUseAttack[rule.stableId]=state.completedAttacks;}
}

public static class EnemyBehaviorValidation
{
    public static List<string> Validate(WorldContentDatabase db,EnemyBehaviorProfileDefinition profile)
    {
        var errors=new List<string>();if(profile==null){errors.Add("Missing behavior profile.");return errors;}
        if(string.IsNullOrWhiteSpace(profile.stableId))errors.Add("Behavior profile stable ID is missing.");
        var ids=new HashSet<string>();foreach(var rule in profile.rules??new())
        {if(rule==null){errors.Add("Null rule.");continue;}if(string.IsNullOrWhiteSpace(rule.stableId)||!ids.Add(rule.stableId))errors.Add("Missing or duplicate rule ID: "+rule.stableId);if(db?.EnemySkill(rule.skillId)==null)errors.Add(rule.stableId+": missing skill "+rule.skillId);if(rule.weight<=0)errors.Add(rule.stableId+": weight must be positive.");if(rule.cooldownTurns<0)errors.Add(rule.stableId+": cooldown cannot be negative.");if(rule.threshold<0||rule.threshold>1)errors.Add(rule.stableId+": threshold must be 0–1.");if(rule.corruptionAtLeast<0||rule.corruptionAtLeast>100)errors.Add(rule.stableId+": corruption requirement must be 0–100.");if(rule.condition is EnemyBehaviorCondition.PlayerHasAilment or EnemyBehaviorCondition.SelfHasAilment or EnemyBehaviorCondition.PlayerDoesNotHaveAilment&&rule.ailment==EnemyBehaviorAilment.None)errors.Add(rule.stableId+": ailment condition requires an ailment.");if(rule.condition==EnemyBehaviorCondition.BossPhase&&string.IsNullOrWhiteSpace(rule.bossPhaseId))errors.Add(rule.stableId+": boss-phase condition requires a phase ID.");if(rule.condition is EnemyBehaviorCondition.AfterTakingNHits or EnemyBehaviorCondition.AfterDealingNHits&&rule.hitCount<=0)errors.Add(rule.stableId+": hit condition requires a positive count.");}
        if(!(profile.rules??new()).Any(x=>x!=null&&x.fallbackEligible))errors.Add("No fallback-eligible rule.");return errors;
    }
}
