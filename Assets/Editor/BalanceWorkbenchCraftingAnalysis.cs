using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class CraftTarget
    {public StatTypes stat;public int maximumTier=2;public float minimumRoll;}
    [Serializable] public sealed class CraftPolicyStep
    {public CraftingCurrencyType action;public int repeats=1;}
    [Serializable] public sealed class CraftingSimulationRequest
    {
        public GearSnapshot item;public List<CraftTarget> targets=new();public int requiredMatches=1;public List<CraftPolicyStep> policy=new();public int trials=1000,maxActions=100;public long seed=80001;
        public List<CurrencyStackData> budgets=new();
    }
    [Serializable] public sealed class CraftTraceStep
    {public int number;public CraftingCurrencyType action;public bool applied;public int potentialBefore,potentialAfter;public string item;}
    [Serializable] public sealed class CraftAttempt
    {public bool success,potentialFailure,budgetFailure;public int actions,matches;public List<CurrencyStackData> spent=new();public List<CraftTraceStep> trace=new();}
    [Serializable] public sealed class CurrencyCostSummary
    {public CraftingCurrencyType currency;public double mean,p50,p90,p95,p99;}
    [Serializable] public sealed class CraftingSimulationResult
    {
        public ExperimentMetadata metadata;public int trials,successes,potentialFailures,budgetFailures;public MetricSummary actions,finalMatches;public List<CurrencyCostSummary> costs=new();public CraftAttempt replay;public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();
    }
    public static class CraftingSimulator
    {
        public static readonly CraftingCurrencyType[] OrdinaryActions={CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.RerollMagic,CraftingCurrencyType.MagicToRare,CraftingCurrencyType.AddRareModifier,CraftingCurrencyType.RerollRareModifier,CraftingCurrencyType.RemoveRareModifier};
        static int Matches(Gear gear,CraftingSimulationRequest request)=>request.targets.Count(t=>gear.rolledMods.Any(m=>m!=null&&m.statType==t.stat&&m.tierIndex<=t.maximumTier&&m.value>=t.minimumRoll));
        public static CraftingSimulationResult Run(CraftingSimulationRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request.item==null)throw new ArgumentException("Select a real starting item.");if(request.policy.Count==0)throw new ArgumentException("Add at least one production crafting action.");
            if(request.policy.Any(x=>!OrdinaryActions.Contains(x.action)))throw new ArgumentException("Policy includes an action not supported by the isolated ordinary-equipment simulator.");
            var result=new CraftingSimulationResult{metadata=ExperimentMetadata.Create("Crafting Simulator",request.seed,request.trials,request.item.itemLevel,request.item.itemLevel,JsonUtility.ToJson(request))};var costs=new Dictionary<CraftingCurrencyType,List<double>>();var actionCounts=new List<double>();var matchCounts=new List<double>();using var session=new WorkbenchSession();var root=new GameObject("Crafting simulations"){hideFlags=HideFlags.HideAndDontSave};try
            {
                for(int i=0;i<request.trials;i++)
                {
                    if(cancelled?.Invoke()==true)break;var rng=new SeededSimulationRandomSource(request.seed+i*7919L);var item=request.item.Materialize(root.transform,"Craft attempt "+i);var spent=new Dictionary<CraftingCurrencyType,int>();var attempt=new CraftAttempt();try
                    {
                        if(Matches(item,request)>=request.requiredMatches)attempt.success=true;
                        for(int cycle=0;cycle<request.maxActions&&!attempt.success;cycle++)
                        {
                            bool any=false;foreach(var step in request.policy)
                            {
                                if(attempt.success||attempt.actions>=request.maxActions)break;for(int repeat=0;repeat<Math.Max(1,step.repeats);repeat++)
                                {
                                    if(attempt.success||attempt.actions>=request.maxActions)break;int budget=request.budgets.FirstOrDefault(x=>x.type==step.action).amount;if(budget>0&&spent.GetValueOrDefault(step.action)>=budget){attempt.budgetFailure=true;continue;}
                                    if(!EquipmentCrafting.CanApply(step.action,item)){if(item.CurrentCraftingPotential<CraftingPotentialProfile.OrdinaryCost(step.action))attempt.potentialFailure=true;continue;}
                                    int before=item.CurrentCraftingPotential;bool applied=EquipmentCrafting.TryApply(step.action,item,session.Roller,rng);if(!applied)continue;any=true;attempt.actions++;spent[step.action]=spent.GetValueOrDefault(step.action)+1;attempt.matches=Matches(item,request);attempt.success=attempt.matches>=request.requiredMatches;
                                    if(result.replay==null||result.replay.success)attempt.trace.Add(new CraftTraceStep{number=attempt.actions,action=step.action,applied=true,potentialBefore=before,potentialAfter=item.CurrentCraftingPotential,item=GearSnapshot.Capture(item).Description});
                                }
                            }
                            if(!any)break;
                        }
                    }finally{UnityEngine.Object.DestroyImmediate(item.gameObject);}
                    attempt.spent=spent.Select(x=>new CurrencyStackData(x.Key,x.Value)).ToList();if(attempt.success)result.successes++;if(attempt.potentialFailure)result.potentialFailures++;if(attempt.budgetFailure)result.budgetFailures++;actionCounts.Add(attempt.actions);matchCounts.Add(attempt.matches);foreach(CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType))){if(!costs.TryGetValue(type,out var list))costs[type]=list=new List<double>();list.Add(spent.GetValueOrDefault(type));}if(result.replay==null||!attempt.success&&result.replay.success)result.replay=attempt;result.trials++;if((i&63)==0)progress?.Invoke((i+1f)/request.trials);
                }
            }finally{UnityEngine.Object.DestroyImmediate(root);}
            result.actions=MetricSummary.From(actionCounts);result.finalMatches=MetricSummary.From(matchCounts);foreach(var pair in costs.Where(x=>x.Value.Any(v=>v>0))){var m=MetricSummary.From(pair.Value);result.costs.Add(new CurrencyCostSummary{currency=pair.Key,mean=m.mean,p50=m.p50,p90=m.p90,p95=m.p95,p99=m.p99});}result.metadata.sampleCount=result.trials;return result;
        }
    }
}
