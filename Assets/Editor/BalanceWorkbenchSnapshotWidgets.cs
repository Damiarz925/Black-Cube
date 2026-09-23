using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class SnapshotNamedValue { public string name;public double value; }
    [Serializable] public sealed class SnapshotMatrixCell { public string row,column;public double value; }
    [Serializable] public sealed class SnapshotWidgetData
    {
        public string scenario,source,metric,status="OK",error,config,fingerprint;
        public BalanceWidgetKind kind;public long seed;
        public List<BreakpointPoint> curve=new();public MetricSummary distribution=new();public List<HistogramBin> histogram=new();
        public List<SnapshotMatrixCell> matrix=new();public List<SnapshotNamedValue> ranking=new();public List<BreakpointResult> breakpoints=new();
    }
    [Serializable] public sealed class SnapshotCurveDelta { public int level;public double before,after,delta,percent; }
    [Serializable] public sealed class SnapshotCellDelta { public string row,column;public double before,after,delta; }
    [Serializable] public sealed class SnapshotWidgetDifference
    {
        public string scenario,status;public BalanceWidgetKind kind;public double maximumAbsoluteDelta,maximumPercentDelta,averageDelta,areaDelta;public int largestChangeLevel;
        public MetricSummary beforeDistribution,afterDistribution;public List<SnapshotCurveDelta> curve=new();public List<SnapshotCellDelta> matrix=new();
        public List<SnapshotNamedValue> rankingChanges=new();public int beforeBreakpointCount,afterBreakpointCount;
    }
    [Serializable] public sealed class SnapshotWidgetComparisonExport { public List<SnapshotWidgetDifference> rows=new(); }
    public static class BalanceSnapshotWidgets
    {
        static readonly Dictionary<string,SnapshotWidgetData> cache=new();
        public static SnapshotWidgetData Capture(BalanceScenario scenario,Func<bool> cancelled=null)
        {
            string config=JsonUtility.ToJson(scenario),fingerprint=ProductionBalanceAdapters.DataFingerprint(),key=fingerprint+":"+config;
            if(cache.TryGetValue(key,out var prior))return JsonUtility.FromJson<SnapshotWidgetData>(JsonUtility.ToJson(prior));
            var widget=new SnapshotWidgetData{scenario=scenario.name,source=scenario.source,metric=scenario.metric,kind=scenario.widgetKind,config=config,fingerprint=fingerprint,seed=scenario.combat.seed};
            switch(scenario.widgetKind)
            {
                case BalanceWidgetKind.Curve:
                    for(int level=Math.Max(1,scenario.startLevel);level<=Math.Max(scenario.startLevel,scenario.endLevel);level+=Math.Max(1,scenario.increment))
                    {if(cancelled?.Invoke()==true){widget.status="FAILED";widget.error="Cancelled before complete curve";break;}widget.curve.Add(new BreakpointPoint{level=level,value=Measure(scenario,level)});}break;
                case BalanceWidgetKind.Distribution:
                    IEnumerable<double> values=scenario.source switch
                    {
                        "Combat"=>CombatLabAdapters.Batch(scenario.combat).durationSamples,
                        "Items"=>ProductionBalanceAdapters.RunItems(scenario.item).samples.Select(x=>x.weaponDps),
                        "Loot Progression"=>LootProgressionAnalyzer.RunFirstUpgradeTrials(scenario.loot,Math.Max(1,scenario.sampleCount),Math.Max(1,scenario.endLevel),null,null,cancelled).observations.Where(x=>!x.censored).Select(x=>(double)x.kills),
                        "Crafting"=>CraftingSimulator.Run(scenario.crafting,null,cancelled).actionSamples.Select(x=>(double)x),
                        _=>throw new InvalidOperationException("Distribution source must be Combat, Items, Loot Progression, or Crafting.")
                    };
                    var data=values.ToArray();widget.distribution=MetricSummary.From(data);widget.histogram=WorkbenchStatistics.Histogram(data);break;
                case BalanceWidgetKind.Matrix:
                    var builds=scenario.matrixBuilds?.Count>0?scenario.matrixBuilds:new List<PlayerBuildSnapshot>{scenario.build};
                    foreach(var build in builds)foreach(EnemyAI.EnemyRarity rarity in Enum.GetValues(typeof(EnemyAI.EnemyRarity)))
                    {if(cancelled?.Invoke()==true){widget.status="FAILED";widget.error="Cancelled before complete matrix";return widget;}var combat=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(scenario.combat));combat.player=build.Clone();combat.rarity=rarity;var r=CombatLabAdapters.Batch(combat);widget.matrix.Add(new SnapshotMatrixCell{row=build.classId,column=rarity.ToString(),value=r.winRate});}break;
                case BalanceWidgetKind.Ranking:
                    if(scenario.source=="Sensitivity")widget.ranking=SensitivityAnalyzer.Run(scenario.sensitivity,null,cancelled).rows.Select(x=>new SnapshotNamedValue{name=x.stat.ToString(),value=x.absoluteDelta}).ToList();
                    else if(scenario.source=="Affix")widget.ranking=AffixAnalyzer.Run(scenario.affix,cancelled).rows.Take(100).Select(x=>new SnapshotNamedValue{name=x.name+" T"+x.tier,value=x.objectiveDelta}).ToList();
                    else if(scenario.source=="Passive")widget.ranking=PassiveTreeOptimizer.Analyze(scenario.build,scenario.objective).OrderByDescending(x=>x.objectiveDelta).Take(100).Select(x=>new SnapshotNamedValue{name=x.name,value=x.objectiveDelta}).ToList();
                    else if(scenario.source=="Loot Progression")
                    {
                        var loot=LootProgressionAnalyzer.Run(scenario.loot,null,cancelled);widget.ranking.Add(new SnapshotNamedValue{name="Gear per kill",value=loot.drops/(double)Math.Max(1,loot.kills)});widget.ranking.Add(new SnapshotNamedValue{name="Upgrades per kill",value=loot.upgrades.Count/(double)Math.Max(1,loot.kills)});
                        widget.ranking.AddRange(loot.upgrades.GroupBy(x=>x.slot).Select(x=>new SnapshotNamedValue{name="Slot/"+x.Key,value=x.Count()}));widget.ranking.AddRange(loot.upgrades.GroupBy(x=>x.rarity).Select(x=>new SnapshotNamedValue{name="Item rarity/"+x.Key,value=x.Count()}));widget.ranking.AddRange(loot.currency.Select(x=>new SnapshotNamedValue{name="Currency/"+x.type,value=x.amount}));
                    }
                    else if(scenario.source=="Crafting")
                    {
                        var craft=CraftingSimulator.Run(scenario.crafting,null,cancelled);widget.ranking.Add(new SnapshotNamedValue{name="Success rate",value=craft.trials==0?0:craft.successes/(double)craft.trials});widget.ranking.Add(new SnapshotNamedValue{name="Potential failures",value=craft.potentialFailures});widget.ranking.Add(new SnapshotNamedValue{name="Resource failures",value=craft.budgetFailures});widget.ranking.Add(new SnapshotNamedValue{name="Median actions",value=craft.actions.p50});widget.ranking.AddRange(craft.resourceCosts.Select(x=>new SnapshotNamedValue{name="Resource/"+x.resourceId,value=x.amount}));
                    }
                    else throw new InvalidOperationException("Ranking/table source must be Sensitivity, Affix, Passive, Loot Progression, or Crafting.");break;
                case BalanceWidgetKind.Breakpoint:
                    widget.breakpoints=BreakpointFinder.Find(level=>Measure(scenario,level),scenario.startLevel,scenario.endLevel,scenario.increment,scenario.threshold,scenario.breakpointOperator,true);break;
                default:throw new InvalidOperationException("Choose a non-scalar snapshot widget.");
            }
            if(widget.status=="OK"){if(cache.Count>=128)cache.Clear();cache[key]=JsonUtility.FromJson<SnapshotWidgetData>(JsonUtility.ToJson(widget));}
            return widget;
        }
        public static double Measure(BalanceScenario scenario,int level)
        {
            switch(scenario.source)
            {
                case "Enemy":{var r=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{level=level,sampleCount=32,seed=scenario.combat.seed,archetypeId=scenario.combat.enemyArchetypeId,rarity=scenario.combat.rarity});return scenario.metric=="life_p50"?r.life.p50:r.dps.p50;}
                case "Drops":{var request=JsonUtility.FromJson<DropLabRequest>(JsonUtility.ToJson(scenario.drop));request.level=level;var r=ProductionBalanceAdapters.RunDrops(request);return scenario.metric=="gear_per_kill"?r.averageGearItems:r.averageCurrencyRolls;}
                case "Combat":{var request=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(scenario.combat));request.enemyLevel=level;var r=CombatLabAdapters.Batch(request);return scenario.metric switch{"p50_ttk"=>r.duration.p50,"p90_ttk"=>r.duration.p90,_=>r.winRate};}
                case "Loot Progression":{var request=JsonUtility.FromJson<LootProgressionRequest>(JsonUtility.ToJson(scenario.loot));request.combatLevel=level;var r=LootProgressionAnalyzer.Run(request);return r.upgrades.Count/(double)Math.Max(1,r.kills);}
                default:{var build=scenario.build.Clone();build.playerLevel=level;return OptimizationMetricCatalog.Get(scenario.metric).Value(PlayerBuildEvaluator.Evaluate(build));}
            }
        }
        public static List<SnapshotWidgetDifference> Compare(BalanceSnapshot before,BalanceSnapshot after)
        {
            var rows=new List<SnapshotWidgetDifference>();var a=before.widgets??new();var b=after.widgets??new();
            foreach(var key in a.Select(x=>x.scenario).Union(b.Select(x=>x.scenario)).Distinct())
            {
                var left=a.FirstOrDefault(x=>x.scenario==key);var right=b.FirstOrDefault(x=>x.scenario==key);var row=new SnapshotWidgetDifference{scenario=key,kind=right?.kind??left?.kind??BalanceWidgetKind.SingleMetric};
                if(left==null)row.status="MISSING FROM A";else if(right==null)row.status="MISSING FROM B";else if(left.status=="FAILED"||right.status=="FAILED")row.status="FAILED";else if(left.fingerprint!=before.fingerprint||right.fingerprint!=after.fingerprint)row.status="STALE";
                else if(left.kind!=right.kind||left.kind==BalanceWidgetKind.Curve&&!left.curve.Select(x=>x.level).OrderBy(x=>x).SequenceEqual(right.curve.Select(x=>x.level).OrderBy(x=>x))||left.kind==BalanceWidgetKind.Matrix&&!left.matrix.Select(x=>x.row+"|"+x.column).OrderBy(x=>x).SequenceEqual(right.matrix.Select(x=>x.row+"|"+x.column).OrderBy(x=>x))||left.kind==BalanceWidgetKind.Ranking&&!left.ranking.Select(x=>x.name).OrderBy(x=>x).SequenceEqual(right.ranking.Select(x=>x.name).OrderBy(x=>x)))row.status="FAILED";
                else
                {
                    var deltas=new List<double>();var percentages=new List<double>();
                    switch(row.kind)
                    {
                        case BalanceWidgetKind.Curve:
                            foreach(var point in left.curve){var match=right.curve.FirstOrDefault(x=>x.level==point.level);if(match==null)continue;double d=match.value-point.value,p=Math.Abs(point.value)>1e-9?d/point.value:0;row.curve.Add(new SnapshotCurveDelta{level=point.level,before=point.value,after=match.value,delta=d,percent=p});deltas.Add(d);percentages.Add(p);}
                            if(row.curve.Count>0){var largest=row.curve.OrderByDescending(x=>Math.Abs(x.delta)).First();row.largestChangeLevel=largest.level;row.areaDelta=row.curve.Sum(x=>x.delta);}break;
                        case BalanceWidgetKind.Distribution:
                            row.beforeDistribution=left.distribution;row.afterDistribution=right.distribution;
                            foreach(var pair in new[]{(left.distribution.mean,right.distribution.mean),(left.distribution.p50,right.distribution.p50),(left.distribution.p10,right.distribution.p10),(left.distribution.p90,right.distribution.p90),(left.distribution.p99,right.distribution.p99),(left.distribution.standardDeviation,right.distribution.standardDeviation)}){deltas.Add(pair.Item2-pair.Item1);percentages.Add(Math.Abs(pair.Item1)>1e-9?(pair.Item2-pair.Item1)/pair.Item1:0);}break;
                        case BalanceWidgetKind.Matrix:
                            foreach(var cell in left.matrix){var match=right.matrix.FirstOrDefault(x=>x.row==cell.row&&x.column==cell.column);if(match==null)continue;double d=match.value-cell.value;row.matrix.Add(new SnapshotCellDelta{row=cell.row,column=cell.column,before=cell.value,after=match.value,delta=d});deltas.Add(d);percentages.Add(Math.Abs(cell.value)>1e-9?d/cell.value:0);}break;
                        case BalanceWidgetKind.Ranking:
                            foreach(var value in left.ranking){var match=right.ranking.FirstOrDefault(x=>x.name==value.name);if(match==null)continue;double d=match.value-value.value;row.rankingChanges.Add(new SnapshotNamedValue{name=value.name,value=d});deltas.Add(d);percentages.Add(Math.Abs(value.value)>1e-9?d/value.value:0);}break;
                        case BalanceWidgetKind.Breakpoint:
                            row.beforeBreakpointCount=left.breakpoints.Count;row.afterBreakpointCount=right.breakpoints.Count;int count=Math.Min(left.breakpoints.Count,right.breakpoints.Count);for(int i=0;i<count;i++)deltas.Add(right.breakpoints[i].level-left.breakpoints[i].level);if(left.breakpoints.Count!=right.breakpoints.Count)deltas.Add(Math.Abs(left.breakpoints.Count-right.breakpoints.Count));break;
                    }
                    row.maximumAbsoluteDelta=deltas.Count==0?0:deltas.Max(x=>Math.Abs(x));row.maximumPercentDelta=percentages.Count==0?0:percentages.Max(x=>Math.Abs(x));row.averageDelta=deltas.Count==0?0:deltas.Average();row.status=deltas.Any(x=>Math.Abs(x)>1e-9)?"CHANGED":"UNCHANGED";
                }
                rows.Add(row);
            }
            return rows;
        }
    }
}
