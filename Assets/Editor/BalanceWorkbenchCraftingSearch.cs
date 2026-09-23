using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class CraftPolicySearchRequest
    {
        public CraftingSimulationRequest simulation=new();
        public List<CraftPolicyStep> allowedActions=new();
        public int beamWidth=4,maxCraftActions=4,candidateOutcomesPerAction=8,maximumStates=40;
        public long seed=91207;
    }

    [Serializable] public sealed class CraftDiscoveredPolicy
    {
        public List<CraftPolicyStep> steps=new();
        public double score,estimatedSuccessRate,medianCost,p90Cost,potentialFailureRate,medianActions;
        public int evaluationTrials;
    }

    [Serializable] public sealed class CraftPolicySearchResult
    {
        public ExperimentMetadata metadata;
        public int evaluatedStates,stateCap;
        public List<CraftDiscoveredPolicy> topPolicies=new();
        public string label="BEST DISCOVERED POLICY — bounded search, not proven optimal";
    }

    public static class CraftingPolicySearcher
    {
        static CraftPolicyStep Clone(CraftPolicyStep step)=>JsonUtility.FromJson<CraftPolicyStep>(JsonUtility.ToJson(step));
        static string Key(IEnumerable<CraftPolicyStep> policy)=>string.Join("|",policy.Select(x=>$"{x.operation}:{x.action}:{x.targetStat}:{x.challengeId}"));
        public static CraftPolicySearchResult Search(CraftPolicySearchRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request?.simulation?.item==null)throw new ArgumentException("Select a starting item before searching.");
            int cap=Math.Clamp(request.maximumStates,1,10000),width=Math.Clamp(request.beamWidth,1,100),depth=Math.Clamp(request.maxCraftActions,1,100),outcomes=Math.Clamp(request.candidateOutcomesPerAction,1,1000);
            var actions=(request.allowedActions?.Count>0?request.allowedActions:CraftingSimulator.OrdinaryActions.Select(x=>new CraftPolicyStep{action=x}).ToList()).Where(x=>x!=null).GroupBy(x=>Key(new[]{x})).Select(x=>Clone(x.First())).ToList();
            if(actions.Count==0)throw new ArgumentException("Select at least one allowed production action.");
            var result=new CraftPolicySearchResult{stateCap=cap,metadata=ExperimentMetadata.Create("Crafting Policy Search",request.seed,cap,request.simulation.item.itemLevel,request.simulation.item.itemLevel,JsonUtility.ToJson(request))};
            var frontier=new List<List<CraftPolicyStep>>{new()};var seen=new HashSet<string>();
            for(int d=1;d<=depth&&result.evaluatedStates<cap;d++)
            {
                var next=new List<CraftDiscoveredPolicy>();
                foreach(var prefix in frontier)
                foreach(var action in actions)
                {
                    if(result.evaluatedStates>=cap||cancelled?.Invoke()==true)break;
                    var policy=new List<CraftPolicyStep>(prefix.Select(Clone)){Clone(action)};string key=Key(policy);if(!seen.Add(key))continue;
                    var simulation=JsonUtility.FromJson<CraftingSimulationRequest>(JsonUtility.ToJson(request.simulation));simulation.policy=policy;simulation.maxActions=policy.Count;simulation.trials=outcomes;simulation.seed=unchecked(request.seed+result.evaluatedStates*7919L);
                    var sample=CraftingSimulator.Run(simulation,null,cancelled);if(sample.trials==0)break;
                    var discovered=new CraftDiscoveredPolicy{steps=policy,evaluationTrials=sample.trials,estimatedSuccessRate=sample.successes/(double)sample.trials,medianCost=sample.actions.p50,p90Cost=sample.actions.p90,potentialFailureRate=sample.potentialFailures/(double)sample.trials,medianActions=sample.actions.p50};
                    // Target completion dominates; partial target progress and retained potential break ties.
                    discovered.score=discovered.estimatedSuccessRate*1000+sample.finalMatches.mean*10+sample.failurePotential.mean*.01-discovered.potentialFailureRate*2-discovered.medianCost*.001;
                    next.Add(discovered);result.topPolicies.Add(discovered);result.evaluatedStates++;progress?.Invoke(result.evaluatedStates/(float)cap);
                }
                if(cancelled?.Invoke()==true)break;
                frontier=next.OrderByDescending(x=>x.score).Take(width).Select(x=>x.steps).ToList();if(frontier.Count==0)break;
            }
            result.topPolicies=result.topPolicies.OrderByDescending(x=>x.score).Take(20).ToList();result.metadata.sampleCount=result.evaluatedStates;return result;
        }
        public static CraftingSimulationResult Validate(CraftPolicySearchRequest request,CraftDiscoveredPolicy policy,int independentTrials,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(policy==null)throw new ArgumentNullException(nameof(policy));
            var simulation=JsonUtility.FromJson<CraftingSimulationRequest>(JsonUtility.ToJson(request.simulation));simulation.policy=policy.steps.Select(Clone).ToList();simulation.maxActions=Math.Max(simulation.maxActions,simulation.policy.Count);simulation.trials=Math.Max(1,independentTrials);simulation.seed=unchecked(request.seed+971393L);
            return CraftingSimulator.Run(simulation,progress,cancelled);
        }
    }
}
