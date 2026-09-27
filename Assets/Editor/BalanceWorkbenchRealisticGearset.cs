using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackCube.BalanceWorkbench
{
    public enum DefenseAdherence { Soft, Strict }

    [Serializable] public sealed class RealisticGearsetResult
    {
        public PlayerBuildSnapshot build;
        public PlayerBuildMetrics metrics;
        public int inventorySize,candidatesEvaluated,shortlisted,completeSetsEvaluated;
        public bool satisfiesEnabledFloors;
        public string warning;
    }

    // Every historical item is evaluated, then a bounded diverse shortlist is
    // searched as complete sets. Defense is scored from the current full-set
    // deficit rather than reserving particular modifier slots.
    public static class RealisticGearsetOptimizer
    {
        static readonly LootManager.GearType[] Slots=(LootManager.GearType[])
            Enum.GetValues(typeof(LootManager.GearType));
        sealed class State
        {
            public List<GearSnapshot> items;
            public double score;
            public string key;
        }

        public static RealisticGearsetResult Optimize(PlayerBuildSnapshot source,
            RealisticInventoryResult inventory,OptimizationObjective objective,
            OptimizationConstraints floors,DefenseAdherence adherence=DefenseAdherence.Soft,
            int shortlistPerSlot=20,int beamWidth=48)
        {
            if(source==null||inventory==null||objective==null)throw new ArgumentNullException();
            floors??=new OptimizationConstraints();
            shortlistPerSlot=Math.Clamp(shortlistPerSlot,8,100);
            beamWidth=Math.Clamp(beamWidth,1,500);
            var result=new RealisticGearsetResult{inventorySize=inventory.items?.Count??0};
            var baselineBuild=source.Clone();baselineBuild.equipment=new();
            var baseline=PlayerBuildEvaluator.Evaluate(baselineBuild);
            var candidates=new Dictionary<LootManager.GearType,List<GearSnapshot>>();
            foreach(var slot in Slots)
            {
                var items=(inventory.items??new()).Where(x=>x!=null&&x.slot==slot&&
                    (slot!=LootManager.GearType.Weapons||x.weaponTypeId==source.weaponTypeId)).ToList();
                items.AddRange(source.equipment?.Where(x=>x!=null&&x.slot==slot)??
                    Enumerable.Empty<GearSnapshot>());
                if(items.Count==0)
                {
                    result.warning=$"No historical {slot} candidate for {source.weaponTypeId}.";
                    return result;
                }
                var ranked=new List<(GearSnapshot item,PlayerBuildMetrics metrics,double score,int index)>();
                for(int i=0;i<items.Count;i++)
                {
                    var build=baselineBuild.Clone();build.equipment.Add(items[i]);
                    var metrics=PlayerBuildEvaluator.Evaluate(build);
                    ranked.Add((items[i],metrics,Score(metrics,baseline,objective,floors),i));
                    result.candidatesEvaluated++;
                }
                var selected=new HashSet<int>();
                void Take(IEnumerable<(GearSnapshot item,PlayerBuildMetrics metrics,double score,int index)> ordered,int count)
                {foreach(var entry in ordered.Take(count))selected.Add(entry.index);}
                Take(ranked.OrderByDescending(x=>x.score),shortlistPerSlot);
                Take(ranked.OrderByDescending(x=>x.metrics.life),3);
                Take(ranked.OrderByDescending(x=>x.metrics.armour),3);
                Take(ranked.OrderByDescending(x=>x.metrics.fireResistance),3);
                Take(ranked.OrderByDescending(x=>x.metrics.coldResistance),3);
                Take(ranked.OrderByDescending(x=>x.metrics.lightningResistance),3);
                Take(ranked.OrderByDescending(x=>x.metrics.voidResistance),3);
                Take(ranked.OrderByDescending(x=>x.metrics.manaRegen),3);
                candidates[slot]=selected.OrderBy(x=>x).Select(x=>items[x]).ToList();
                result.shortlisted+=candidates[slot].Count;
            }
            var states=new List<State>{new(){items=new List<GearSnapshot>(),score=0,key=""}};
            foreach(var slot in Slots)
            {
                var expanded=new List<State>();
                foreach(var state in states)
                    foreach(var item in candidates[slot])
                    {
                        var selected=new List<GearSnapshot>(state.items){item};
                        var build=source.Clone();build.equipment=selected;
                        var metrics=PlayerBuildEvaluator.Evaluate(build);
                        result.completeSetsEvaluated++;
                        expanded.Add(new State{items=selected,
                            score=Score(metrics,baseline,objective,floors),
                            key=string.Join("|",selected.Select(x=>x.Description))});
                    }
                states=expanded.OrderByDescending(x=>x.score)
                    .ThenBy(x=>x.key,StringComparer.Ordinal).Take(beamWidth).ToList();
            }
            var finalists=states.Select(x=>
            {
                var build=source.Clone();build.equipment=x.items;
                return (build,metrics:PlayerBuildEvaluator.Evaluate(build),x.score);
            }).ToList();
            var eligible=adherence==DefenseAdherence.Strict?
                finalists.Where(x=>floors.Accept(x.metrics)).ToList():finalists;
            if(eligible.Count==0)
            {
                result.warning="STRICT floors could not be satisfied by retained historical gearsets.";
                return result;
            }
            var best=eligible.OrderByDescending(x=>x.score).First();
            result.build=best.build;result.metrics=best.metrics;
            result.satisfiesEnabledFloors=floors.Accept(best.metrics);
            if(!result.satisfiesEnabledFloors)result.warning="SOFT defense profile accepted unmet floors; inspect actual deficits.";
            return result;
        }

        internal static double Score(PlayerBuildMetrics value,PlayerBuildMetrics baseline,
            OptimizationObjective objective,OptimizationConstraints floors)
        {
            double offense=OptimizationMetricCatalog.Score(value,baseline,objective);
            double penalty=0;
            void Add(bool enabled,double actual,double target)
            {
                if(!enabled||target<=0)return;
                double deficit=Math.Clamp((target-actual)/target,0,2);
                penalty+=deficit*deficit;
            }
            Add(floors.minimumLifeEnabled,value.life,floors.minimumLife);
            Add(floors.minimumArmourEnabled,value.armour,floors.minimumArmour);
            Add(floors.minimumFireResistanceEnabled,value.fireResistance,floors.minimumFireResistance);
            Add(floors.minimumColdResistanceEnabled,value.coldResistance,floors.minimumColdResistance);
            Add(floors.minimumLightningResistanceEnabled,value.lightningResistance,floors.minimumLightningResistance);
            Add(floors.minimumVoidResistanceEnabled,value.voidResistance,floors.minimumVoidResistance);
            return offense-penalty;
        }
    }
}
