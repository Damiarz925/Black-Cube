using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class CraftTarget
    {public StatTypes stat;public int maximumTier=2;public float minimumRoll;}
    public enum CraftOperation { Ordinary, Empowerment, BossInfusion, ImplicitReforge }
    [Serializable] public sealed class CraftPolicyStep
    {public CraftOperation operation;public CraftingCurrencyType action;public int repeats=1;public StatTypes targetStat;public string challengeId;}
    [Serializable] public sealed class SimulatedResourceQuantity
    {public string id;public int startingQuantity;public bool unlimited;}
    public enum CraftTargetGroupMode { AllRequired, AnyOf, AtLeastN }
    [Serializable] public sealed class CraftingSimulationRequest
    {
        public GearSnapshot item;public List<CraftTarget> targets=new();public int requiredMatches=1;public List<CraftPolicyStep> policy=new();public int trials=1000,maxActions=100;public long seed=80001;
        public List<CurrencyStackData> budgets=new();public List<SimulatedResourceQuantity> resources=new();public int combatLevel=100,minimumEmpowered,maximumUndesirable=int.MaxValue,maximumImplicitTier=int.MaxValue;public float minimumImplicitRoll;public double minimumObjectiveImprovement;public PlayerBuildSnapshot objectiveBuild;public OptimizationObjective objective=new();public bool requireSpecificEmpowered;public StatTypes targetEmpoweredStat;public string targetBossSpecialId,targetBossPoolId,targetImplicitStat;public CraftTargetGroupMode targetGroupMode=CraftTargetGroupMode.AtLeastN;
    }
    [Serializable] public sealed class CraftTraceStep
    {public int number;public CraftingCurrencyType action;public CraftOperation operation;public bool applied;public int potentialBefore,potentialAfter;public string item,resourceId,failureReason;}
    [Serializable] public sealed class CraftAttempt
    {public bool success,potentialFailure,budgetFailure,progressionFailure;public int actions,matches,remainingPotential;public GearSnapshot finalItem;public List<CurrencyStackData> spent=new();public List<EndgameResourceStack> resourcesSpent=new();public List<CraftTraceStep> trace=new();}
    [Serializable] public sealed class CurrencyCostSummary
    {public CraftingCurrencyType currency;public double mean,p50,p90,p95,p99;}
    [Serializable] public sealed class CraftingSimulationResult
    {
        public ExperimentMetadata metadata;public int trials,successes,potentialFailures,budgetFailures,progressionFailures;public MetricSummary actions,finalMatches,failurePotential;public List<int> actionSamples=new();public List<CurrencyCostSummary> costs=new();public CraftAttempt replay,bestFailure;public List<EndgameResourceStack> resourceCosts=new();public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();
    }
    public static class CraftingSimulator
    {
        public static readonly CraftingCurrencyType[] OrdinaryActions={CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.RerollMagic,CraftingCurrencyType.MagicToRare,CraftingCurrencyType.AddRareModifier,CraftingCurrencyType.RerollRareModifier,CraftingCurrencyType.RemoveRareModifier};
        static int Matches(Gear gear,CraftingSimulationRequest request)=>request.targets.Count(t=>gear.rolledMods.Any(m=>m!=null&&!m.lockedOriginal&&m.statType==t.stat&&m.tierIndex<=t.maximumTier&&m.value>=t.minimumRoll));
        static bool TargetMet(Gear gear,CraftingSimulationRequest request,int matches)
        {
            int required=request.targetGroupMode switch{CraftTargetGroupMode.AllRequired=>request.targets.Count,CraftTargetGroupMode.AnyOf=>1,_=>request.requiredMatches};
            if(matches<required||gear.EmpoweredModifierCount<request.minimumEmpowered)return false;
            if(request.requireSpecificEmpowered&&!gear.rolledMods.Any(x=>x!=null&&x.isEmpowered&&x.statType==request.targetEmpoweredStat))return false;
            if(!string.IsNullOrEmpty(request.targetBossSpecialId)&&!gear.rolledMods.Any(x=>x!=null&&x.isBossSpecial&&x.specialModifierId==request.targetBossSpecialId))return false;
            if(!string.IsNullOrEmpty(request.targetBossPoolId)&&!gear.rolledMods.Any(x=>x!=null&&x.isBossSpecial&&x.specialPoolId==request.targetBossPoolId))return false;
            if(!string.IsNullOrEmpty(request.targetImplicitStat)&&gear.ImplicitMod?.statType.ToString()!=request.targetImplicitStat)return false;
            if((request.maximumImplicitTier<int.MaxValue||request.minimumImplicitRoll>0)&&(gear.ImplicitMod==null||gear.ImplicitMod.tierIndex>request.maximumImplicitTier||gear.ImplicitMod.value<request.minimumImplicitRoll))return false;
            int undesirable=gear.rolledMods.Count(x=>x!=null&&!x.lockedOriginal&&!x.isBossSpecial&&!request.targets.Any(t=>t.stat==x.statType));if(undesirable>request.maximumUndesirable)return false;
            if(request.minimumObjectiveImprovement>0)
            {
                if(request.objectiveBuild==null)throw new ArgumentException("Objective-improvement target requires a reference build.");
                var before=PlayerBuildEvaluator.Evaluate(request.objectiveBuild);var candidate=request.objectiveBuild.Clone();candidate.equipment.RemoveAll(x=>x.slot==gear.ItemType);candidate.equipment.Add(GearSnapshot.Capture(gear));var after=PlayerBuildEvaluator.Evaluate(candidate);
                if(OptimizationMetricCatalog.Score(after,before,request.objective)<request.minimumObjectiveImprovement)return false;
            }
            return true;
        }
        static string ResourceFor(CraftPolicyStep step,ChallengeEncounterDefinition challenge)=>step.operation switch{CraftOperation.Empowerment=>CraftingCurrencyType.EmpowermentCatalyst.ToString(),CraftOperation.BossInfusion=>challenge?.rewardResourceId,CraftOperation.ImplicitReforge=>EndgameResourceIds.ImplicitReforger,_=>step.action.ToString()};
        static bool Apply(CraftPolicyStep step,Gear item,CraftingSimulationRequest request,WorkbenchSession session,ILootRandomSource rng,ChallengeEncounterDefinition challenge)
        {
            switch(step.operation)
            {
                case CraftOperation.Empowerment:{var mod=item.rolledMods.FirstOrDefault(x=>x!=null&&x.statType==step.targetStat&&EmpowermentCrafting.IsEligible(item,x,request.combatLevel,session.Roller.Database));return mod!=null&&EmpowermentCrafting.TryApply(item,mod,request.combatLevel,rng.Value(),rng.Value(),session.Roller.Database);}
                case CraftOperation.BossInfusion:{var mod=item.rolledMods.FirstOrDefault(x=>x!=null&&x.statType==step.targetStat);return challenge!=null&&BossSpecialCrafting.TryReplace(item,mod,WorldContentCatalog.Reference.ChallengeSpecialPool(challenge.specialAffixPoolId),request.combatLevel,rng);}
                case CraftOperation.ImplicitReforge:return EndgameCraftingService.TryReforgeImplicitCore(item,session.Roller,rng);
                default:return EquipmentCrafting.TryApply(step.action,item,session.Roller,rng);
            }
        }
        public static CraftingSimulationResult Run(CraftingSimulationRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request.item==null)throw new ArgumentException("Select a real starting item.");
            if(request.policy==null||request.policy.Count==0)throw new ArgumentException("Add at least one production crafting action.");
            if(request.policy.Any(x=>x.operation==CraftOperation.Ordinary&&!OrdinaryActions.Contains(x.action)))throw new ArgumentException("Policy includes an unsupported ordinary-equipment action.");
            var result=new CraftingSimulationResult{metadata=ExperimentMetadata.Create("Crafting Simulator",request.seed,request.trials,request.item.itemLevel,request.item.itemLevel,JsonUtility.ToJson(request))};
            var costs=new Dictionary<CraftingCurrencyType,List<double>>();var actionCounts=new List<double>();var matchCounts=new List<double>();var failedPotential=new List<double>();var resourceTotals=new Dictionary<string,int>();
            using var session=new WorkbenchSession();var root=new GameObject("Crafting simulations"){hideFlags=HideFlags.HideAndDontSave};try
            {
                for(int i=0;i<request.trials;i++)
                {
                    if(cancelled?.Invoke()==true)break;
                    var rng=new SeededSimulationRandomSource(request.seed+i*7919L);var item=request.item.Materialize(root.transform,"Craft attempt "+i);
                    var spent=new Dictionary<CraftingCurrencyType,int>();var resourceSpent=new Dictionary<string,int>();var attempt=new CraftAttempt();try
                    {
                        attempt.matches=Matches(item,request);attempt.success=TargetMet(item,request,attempt.matches);
                        for(int cycle=0;cycle<request.maxActions&&!attempt.success;cycle++)
                        {
                            bool any=false;foreach(var step in request.policy)
                            {
                                if(attempt.success||attempt.actions>=request.maxActions)break;for(int repeat=0;repeat<Math.Max(1,step.repeats);repeat++)
                                {
                                    if(attempt.success||attempt.actions>=request.maxActions)break;
                                    var challenge=step.operation==CraftOperation.BossInfusion?WorldContentCatalog.Reference?.Challenge(step.challengeId):null;
                                    string resourceId=ResourceFor(step,challenge);
                                    if(step.operation==CraftOperation.BossInfusion&&challenge==null){attempt.progressionFailure=true;continue;}
                                    var quantity=request.resources?.FirstOrDefault(x=>x.id==resourceId);bool limited=step.operation!=CraftOperation.Ordinary?quantity==null||!quantity.unlimited:quantity!=null&&!quantity.unlimited;
                                    int ordinaryBudget=step.operation==CraftOperation.Ordinary?request.budgets?.FirstOrDefault(x=>x.type==step.action).amount??0:0;
                                    if((limited&&resourceSpent.GetValueOrDefault(resourceId)>=(quantity?.startingQuantity??0))||(ordinaryBudget>0&&spent.GetValueOrDefault(step.action)>=ordinaryBudget))
                                    {attempt.budgetFailure=true;attempt.trace.Add(new CraftTraceStep{number=attempt.actions+1,action=step.action,operation=step.operation,resourceId=resourceId,failureReason="Missing simulated resource",potentialBefore=item.CurrentCraftingPotential,potentialAfter=item.CurrentCraftingPotential});continue;}
                                    int cost=step.operation switch{CraftOperation.Ordinary=>CraftingPotentialProfile.OrdinaryCost(step.action),CraftOperation.BossInfusion=>CraftingPotentialProfile.BossSpecialReplacementCost,_=>0};
                                    if(item.CurrentCraftingPotential<cost){attempt.potentialFailure=true;continue;}
                                    if(step.operation==CraftOperation.Empowerment&&EmpowermentProgressionProfile.MaximumEmpoweredModifiers(request.combatLevel)<=item.EmpoweredModifierCount){attempt.progressionFailure=true;continue;}
                                    int before=item.CurrentCraftingPotential;bool applied=Apply(step,item,request,session,rng,challenge);
                                    if(!applied){attempt.trace.Add(new CraftTraceStep{number=attempt.actions+1,action=step.action,operation=step.operation,resourceId=resourceId,failureReason="Production eligibility or roll rejected",potentialBefore=before,potentialAfter=item.CurrentCraftingPotential});continue;}
                                    any=true;attempt.actions++;resourceSpent[resourceId]=resourceSpent.GetValueOrDefault(resourceId)+1;
                                    if(step.operation==CraftOperation.Ordinary)spent[step.action]=spent.GetValueOrDefault(step.action)+1;
                                    else if(step.operation==CraftOperation.Empowerment)spent[CraftingCurrencyType.EmpowermentCatalyst]=spent.GetValueOrDefault(CraftingCurrencyType.EmpowermentCatalyst)+1;
                                    attempt.matches=Matches(item,request);attempt.success=TargetMet(item,request,attempt.matches);
                                    attempt.trace.Add(new CraftTraceStep{number=attempt.actions,action=step.action,operation=step.operation,resourceId=resourceId,applied=true,potentialBefore=before,potentialAfter=item.CurrentCraftingPotential,item=GearSnapshot.Capture(item).Description});
                                }
                            }
                            if(!any)break;
                        }
                        attempt.remainingPotential=item.CurrentCraftingPotential;attempt.finalItem=GearSnapshot.Capture(item);
                    }finally{UnityEngine.Object.DestroyImmediate(item.gameObject);}
                    attempt.spent=spent.Select(x=>new CurrencyStackData(x.Key,x.Value)).ToList();attempt.resourcesSpent=resourceSpent.Select(x=>new EndgameResourceStack{resourceId=x.Key,amount=x.Value}).ToList();
                    if(attempt.success)result.successes++;else{failedPotential.Add(attempt.remainingPotential);if(result.bestFailure==null||attempt.matches>result.bestFailure.matches||attempt.matches==result.bestFailure.matches&&attempt.remainingPotential>result.bestFailure.remainingPotential)result.bestFailure=attempt;}
                    if(attempt.potentialFailure)result.potentialFailures++;if(attempt.budgetFailure)result.budgetFailures++;if(attempt.progressionFailure)result.progressionFailures++;
                    actionCounts.Add(attempt.actions);result.actionSamples.Add(attempt.actions);matchCounts.Add(attempt.matches);foreach(CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType))){if(!costs.TryGetValue(type,out var list))costs[type]=list=new List<double>();list.Add(spent.GetValueOrDefault(type));}
                    foreach(var pair in resourceSpent)resourceTotals[pair.Key]=resourceTotals.GetValueOrDefault(pair.Key)+pair.Value;
                    if(result.replay==null||!attempt.success&&result.replay.success)result.replay=attempt;result.trials++;if((i&63)==0)progress?.Invoke((i+1f)/request.trials);
                }
            }finally{UnityEngine.Object.DestroyImmediate(root);}
            result.actions=MetricSummary.From(actionCounts);result.finalMatches=MetricSummary.From(matchCounts);result.failurePotential=MetricSummary.From(failedPotential);
            foreach(var pair in costs.Where(x=>x.Value.Any(v=>v>0))){var m=MetricSummary.From(pair.Value);result.costs.Add(new CurrencyCostSummary{currency=pair.Key,mean=m.mean,p50=m.p50,p90=m.p90,p95=m.p95,p99=m.p99});}
            result.resourceCosts=resourceTotals.Select(x=>new EndgameResourceStack{resourceId=x.Key,amount=x.Value}).ToList();result.metadata.sampleCount=result.trials;return result;
        }
    }
}
