using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public enum RealisticCraftingSearch { Fast, Serious, Deep }

    [Serializable] public sealed class CraftabilityRow
    {
        public int inventoryIndex;
        public string item;
        public double score,currentBuildScore,expectedImprovement;
        public CraftingCurrencyType action;
        public StatTypes targetStat;
        public bool focused;
        public AffixSide focusedSide;
        public int potential;
    }

    [Serializable] public sealed class RealisticCraftingAction
    {
        public int number,inventoryIndex,potentialBefore,potentialAfter;
        public CraftingCurrencyType currency;
        public StatTypes targetStat;
        public bool focused;
        public AffixSide focusedSide;
        public double expectedImprovement;
        public string before,after;
    }

    [Serializable] public sealed class RealisticCraftingResult
    {
        public RealisticInventoryResult inventory;
        public List<CraftabilityRow> initialCraftability=new();
        public List<RealisticCraftingAction> actions=new();
        public List<ProgressionCurrencyCount> spent=new(),remaining=new();
        public int projectsReevaluated;
        public string stoppingReason;
    }

    // A bounded, stochastic look-ahead over actual production crafting actions.
    // Every original item remains in the inventory; Craftability only decides
    // where the one shared currency budget should be tried next.
    public static class RealisticCraftingOptimizer
    {
        sealed class Proposal
        {
            public int index;
            public CraftingCurrencyType currency;
            public AffixSide? side;
            public StatTypes? targetStat;
            public double gain,score;
        }

        public static RealisticCraftingResult Run(PlayerBuildSnapshot source,
            RealisticInventoryResult ground,OptimizationObjective objective,
            OptimizationConstraints floors,RealisticCraftingSearch search=RealisticCraftingSearch.Fast,
            int maximumActions=30,long seed=61001,Func<bool> cancelled=null,
            Action<float> progress=null)
        {
            if(source==null||ground==null||objective==null)throw new ArgumentNullException();
            floors??=new OptimizationConstraints();
            maximumActions=Math.Clamp(maximumActions,0,500);
            var result=new RealisticCraftingResult{inventory=new RealisticInventoryResult
            {
                seed=ground.seed,observedDrops=ground.observedDrops,generatedItems=ground.generatedItems,
                items=(ground.items??new()).Select(Clone).ToList(),
                currencies=(ground.currencies??new()).Select(x=>new ProgressionCurrencyCount{currency=x.currency,count=x.count}).ToList(),
                warning="Ground loot plus production crafting; all original item identities are retained."
            }};
            var stock=(ground.currencies??new()).GroupBy(x=>x.currency)
                .ToDictionary(x=>x.Key,x=>Math.Max(0,(int)Math.Floor(x.Sum(v=>v.count))));
            var initial=new Dictionary<CraftingCurrencyType,int>(stock);
            var bestGround=RealisticGearsetOptimizer.Optimize(source,ground,objective,floors,
                DefenseAdherence.Soft,12,24);
            if(bestGround.build==null)
            {
                result.stoppingReason="A complete starting gearset is unavailable: "+bestGround.warning;
                Finish();return result;
            }
            var reference=bestGround.build.Clone();
            using var session=new WorkbenchSession();
            var root=new GameObject("Realistic crafting search"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                for(int step=0;step<maximumActions;step++)
                {
                    progress?.Invoke(step/(float)Math.Max(1,maximumActions));
                    if(cancelled?.Invoke()==true){result.stoppingReason="Cancelled after the last completed action.";break;}
                    var baseline=PlayerBuildEvaluator.Evaluate(reference);
                    double baselineScore=RealisticGearsetOptimizer.Score(baseline,baseline,objective,floors);
                    var shortlist=Shortlist(result.inventory.items,reference,objective,floors,baseline,search);
                    Proposal best=null;
                    var bestByItem=new Dictionary<int,Proposal>();
                    foreach(int index in shortlist)
                    {
                        var snapshot=result.inventory.items[index];
                        double beforeScore=ItemScore(reference,snapshot,baseline,objective,floors);
                        foreach(var currency in CraftingSimulator.OrdinaryActions.Concat(
                            new[]{CraftingCurrencyType.EmpowermentCatalyst}))
                        {
                            if(stock.GetValueOrDefault(currency)<=0)continue;
                            foreach(AffixSide? side in Sides(currency,stock))
                            foreach(StatTypes? targetStat in Targets(snapshot,currency))
                            {
                                double gain=ExpectedGain(snapshot,index,currency,side,targetStat,reference,
                                    baseline,objective,floors,Math.Max(beforeScore,baselineScore),
                                    session,root.transform,search,seed,step);
                                if(gain<=0)continue;
                                double score=gain; // Currency has already been constrained by its shared stock.
                                if(!bestByItem.TryGetValue(index,out var itemBest)||score>itemBest.score)
                                    bestByItem[index]=new Proposal{index=index,currency=currency,side=side,targetStat=targetStat,gain=gain,score=score};
                                if(best==null||score>best.score)
                                    best=new Proposal{index=index,currency=currency,side=side,targetStat=targetStat,gain=gain,score=score};
                            }
                        }
                        result.projectsReevaluated++;
                    }
                    if(step==0)
                    {
                        // The first pass records every evaluated project, not only
                        // the eventual winning action. The full inventory is never pruned.
                        foreach(int index in shortlist)
                        {
                            var item=result.inventory.items[index];
                            bestByItem.TryGetValue(index,out var project);
                            result.initialCraftability.Add(new CraftabilityRow
                            {
                                inventoryIndex=index,item=item.Description,
                                score=project?.gain??0,
                                currentBuildScore=ItemScore(reference,item,baseline,objective,floors),
                                expectedImprovement=project?.gain??0,
                                action=project?.currency??default,
                                targetStat=project?.targetStat??default,
                                focused=project?.side.HasValue??false,
                                focusedSide=project?.side??default,
                                potential=item.currentPotential
                            });
                        }
                    }
                    if(best==null)
                    {
                        result.stoppingReason="No remaining legal craft has positive sampled expected build value with the shared currency and Crafting Potential budget.";
                        break;
                    }
                    var target=result.inventory.items[best.index];
                    var itemObject=target.Materialize(root.transform,"Selected historical craft");
                    bool applied;
                    GearSnapshot after;
                    try
                    {
                        var rng=new SeededSimulationRandomSource(seed+step*1000003L+best.index*7919L);
                        applied=Apply(best.currency,itemObject,best.side,best.targetStat,
                            reference.combatLevel,session,rng);
                        after=applied?GearSnapshot.Capture(itemObject):null;
                    }
                    finally{UnityEngine.Object.DestroyImmediate(itemObject.gameObject);}
                    if(!applied)
                    {
                        result.stoppingReason="The selected production craft rejected its roll without spending currency; no item was lost.";
                        break;
                    }
                    result.inventory.items[best.index]=after;
                    stock[best.currency]--;
                    if(best.side.HasValue)stock[CraftingCurrencyType.AffixFocus]--;
                    result.actions.Add(new RealisticCraftingAction
                    {
                        number=step+1,inventoryIndex=best.index,currency=best.currency,
                        targetStat=best.targetStat??default,
                        focused=best.side.HasValue,focusedSide=best.side??default,
                        potentialBefore=target.currentPotential,potentialAfter=after.currentPotential,
                        expectedImprovement=best.gain,before=target.Description,after=after.Description
                    });
                    if(ItemScore(reference,after,baseline,objective,floors)>baselineScore)
                    {
                        reference.equipment.RemoveAll(x=>x.slot==after.slot);
                        reference.equipment.Add(after);
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            if(string.IsNullOrEmpty(result.stoppingReason))result.stoppingReason=
                $"Stopped at the configured {maximumActions}-action search budget.";
            Finish();return result;

            void Finish()
            {
                result.spent=initial.Where(x=>x.Value-stock.GetValueOrDefault(x.Key)>0)
                    .Select(x=>new ProgressionCurrencyCount{currency=x.Key,
                        count=x.Value-stock.GetValueOrDefault(x.Key)}).ToList();
                result.remaining=stock.OrderBy(x=>x.Key)
                    .Select(x=>new ProgressionCurrencyCount{currency=x.Key,count=x.Value}).ToList();
                result.inventory.currencies=result.remaining.Select(x=>new ProgressionCurrencyCount
                    {currency=x.currency,count=x.count}).ToList();
            }
        }

        static GearSnapshot Clone(GearSnapshot item)=>JsonUtility.FromJson<GearSnapshot>(JsonUtility.ToJson(item));

        static IEnumerable<AffixSide?> Sides(CraftingCurrencyType currency,
            IReadOnlyDictionary<CraftingCurrencyType,int> stock)
        {
            yield return null;
            if(stock.GetValueOrDefault(CraftingCurrencyType.AffixFocus)<=0||
                !EquipmentCrafting.SupportsFocus(currency))yield break;
            yield return AffixSide.Prefix;
            yield return AffixSide.Suffix;
        }

        static IEnumerable<StatTypes?> Targets(GearSnapshot item,CraftingCurrencyType currency)
        {
            if(currency!=CraftingCurrencyType.EmpowermentCatalyst)
            {yield return null;yield break;}
            foreach(var stat in item.mods.Where(x=>x!=null&&!x.implicitMod&&!x.empowered&&
                !x.bossSpecial&&x.tier==1).Select(x=>x.stat).Distinct())
                yield return stat;
        }

        static bool Apply(CraftingCurrencyType currency,Gear gear,AffixSide? side,
            StatTypes? targetStat,int combatLevel,WorkbenchSession session,
            ILootRandomSource rng)
        {
            if(currency!=CraftingCurrencyType.EmpowermentCatalyst)
                return EquipmentCrafting.TryApply(currency,gear,session.Roller,rng,side);
            var mod=gear.rolledMods.FirstOrDefault(x=>x!=null&&x.statType==targetStat);
            return mod!=null&&EmpowermentCrafting.TryApply(gear,mod,combatLevel,
                rng.Value(),rng.Value(),session.Roller.Database);
        }

        static List<int> Shortlist(List<GearSnapshot> items,PlayerBuildSnapshot build,
            OptimizationObjective objective,OptimizationConstraints floors,
            PlayerBuildMetrics baseline,RealisticCraftingSearch search)
        {
            int perSlot=search switch{RealisticCraftingSearch.Fast=>8,
                RealisticCraftingSearch.Serious=>24,_=>int.MaxValue};
            var shortlist=new List<int>();
            foreach(LootManager.GearType slot in Enum.GetValues(typeof(LootManager.GearType)))
            {
                var candidates=Enumerable.Range(0,items.Count)
                    .Where(i=>items[i]!=null&&items[i].slot==slot&&
                        (slot!=LootManager.GearType.Weapons||items[i].weaponTypeId==build.weaponTypeId))
                    .Select(i=>(index:i,score:ItemScore(build,items[i],baseline,objective,floors)))
                    .OrderByDescending(x=>x.score).ThenBy(x=>x.index).Take(perSlot);
                shortlist.AddRange(candidates.Select(x=>x.index));
            }
            return shortlist;
        }

        static double ItemScore(PlayerBuildSnapshot reference,GearSnapshot item,
            PlayerBuildMetrics baseline,OptimizationObjective objective,
            OptimizationConstraints floors)
        {
            var candidate=reference.Clone();
            candidate.equipment.RemoveAll(x=>x.slot==item.slot);
            candidate.equipment.Add(item);
            var metrics=PlayerBuildEvaluator.Evaluate(candidate);
            return RealisticGearsetOptimizer.Score(metrics,baseline,objective,floors);
        }

        static double ExpectedGain(GearSnapshot item,int index,CraftingCurrencyType currency,
            AffixSide? side,StatTypes? targetStat,PlayerBuildSnapshot reference,PlayerBuildMetrics baseline,
            OptimizationObjective objective,OptimizationConstraints floors,double threshold,
            WorkbenchSession session,Transform parent,RealisticCraftingSearch search,
            long seed,int step)
        {
            int samples=search switch{RealisticCraftingSearch.Fast=>2,
                RealisticCraftingSearch.Serious=>6,_=>16};
            double sum=0;
            for(int sample=0;sample<samples;sample++)
            {
                var gear=item.Materialize(parent,"Craftability sample");
                try
                {
                    if(currency!=CraftingCurrencyType.EmpowermentCatalyst&&
                        !EquipmentCrafting.CanApply(currency,gear,side))return 0;
                    var rng=new SeededSimulationRandomSource(seed+step*1000003L+
                        index*7919L+(int)currency*131L+(side.HasValue?(int)side.Value+1:0)*17L+
                        (targetStat.HasValue?(int)targetStat.Value:0)*11L+sample);
                    if(!Apply(currency,gear,side,targetStat,reference.combatLevel,session,rng))return 0;
                    sum+=ItemScore(reference,GearSnapshot.Capture(gear),baseline,objective,floors)-threshold;
                }
                finally{UnityEngine.Object.DestroyImmediate(gear.gameObject);}
            }
            return sum/samples;
        }
    }
}
