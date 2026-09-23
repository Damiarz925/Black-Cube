using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class AnalysisContext
    {
        public PlayerBuildSnapshot player=new(); public CombatLabRequest combat=new(); public OptimizationObjective objective=new();
        public int itemLevel=60,killCount=1000; public long seed=60001; public string fingerprint;
        public void Stamp() => fingerprint=ProductionBalanceAdapters.DataFingerprint();
        public bool Stale => !string.IsNullOrEmpty(fingerprint)&&fingerprint!=ProductionBalanceAdapters.DataFingerprint();
    }

    public enum SensitivityMode { Analytical, Combat }
    public enum PerturbationMode { Absolute, Relative }
    [Serializable] public sealed class SensitivityRequest
    {
        public PlayerBuildSnapshot build=new(); public CombatLabRequest combat=new(); public SensitivityMode mode;
        public PerturbationMode perturbation=PerturbationMode.Absolute; public string metric="basic_dps";
        public float amount=.10f; public List<StatTypes> stats=new(){StatTypes.PhysDmg,StatTypes.AttackSpeed,StatTypes.CritChance,StatTypes.CritMult};
    }
    [Serializable] public sealed class SensitivityRow
    {
        public StatTypes stat; public float inputDelta; public double baseline,modified,absoluteDelta,percentDelta,elasticity;
    }
    [Serializable] public sealed class SensitivityResult
    {
        public ExperimentMetadata metadata; public List<SensitivityRow> rows=new(); public string metric;
        public bool Stale=>metadata!=null&&metadata.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint();
    }
    [Serializable] public sealed class SensitivityPair
    {
        public StatTypes a,b; public double gainA,gainB,observed,interaction;
    }
    public static class SensitivityAnalyzer
    {
        public static readonly StatTypes[] Catalog={StatTypes.Strength,StatTypes.Dexterity,StatTypes.Intelligence,StatTypes.PhysDmg,StatTypes.FireDmg,StatTypes.ColdDmg,StatTypes.LightDmg,StatTypes.VoidDmg,StatTypes.AttackSpeed,StatTypes.CooldownReduction,StatTypes.CritChance,StatTypes.CritMult,StatTypes.ChanceToHitTwice,StatTypes.FlatArmour,StatTypes.Life,StatTypes.LifeRegeneration,StatTypes.LifeOnHit,StatTypes.Mana,StatTypes.ManaRegeneration,StatTypes.ManaOnHit,StatTypes.PoisonChance,StatTypes.PoisonDmg,StatTypes.PoisonSpeed,StatTypes.PoisonDuration,StatTypes.BleedChance,StatTypes.BleedDmg,StatTypes.IgniteChance,StatTypes.IgniteDmg,StatTypes.ShockChance,StatTypes.ShockEffect,StatTypes.ChillChance,StatTypes.ChillEffect,StatTypes.ProjectileDmg,StatTypes.ProjectileSpeed,StatTypes.ProjectileAmount,StatTypes.ProjectilePrecisionChance,StatTypes.ProjectilePrecisionMultiplier,StatTypes.AuraEffect,StatTypes.RageGeneration,StatTypes.RageEffect,StatTypes.PhysPenetration,StatTypes.FirePenetration,StatTypes.ColdPenetration,StatTypes.LightPenetration,StatTypes.VoidPenetration};
        static bool FlatUnit(StatTypes stat)=>stat is StatTypes.Strength or StatTypes.Dexterity or StatTypes.Intelligence or StatTypes.FlatArmour or StatTypes.Life or StatTypes.Mana or StatTypes.LifeRegeneration or StatTypes.ManaRegeneration or StatTypes.LifeOnHit or StatTypes.ManaOnHit or StatTypes.ProjectileAmount;
        // Fractional stats use 1.0 = 100 percentage points; flat stats use their displayed unit.
        public static string Unit(StatTypes stat)=>FlatUnit(stat)?"flat points":stat==StatTypes.CritMult?"multiplier points":"percentage points";
        public static float Delta(StatTypes stat,float amount,PerturbationMode mode,PlayerBuildMetrics baseline)
        {
            if(mode==PerturbationMode.Absolute)return amount;
            double basis=stat switch{StatTypes.Life=>baseline.life,StatTypes.Mana=>baseline.mana,StatTypes.FlatArmour=>baseline.armour,StatTypes.Strength=>baseline.strength,StatTypes.Dexterity=>baseline.dexterity,StatTypes.Intelligence=>baseline.intelligence,StatTypes.LifeRegeneration=>baseline.lifeRegen,StatTypes.ManaRegeneration=>baseline.manaRegen,StatTypes.CritChance=>baseline.critChance,StatTypes.CritMult=>baseline.critMultiplier,StatTypes.AttackSpeed=>baseline.attacksPerSecond, _=>1};
            return (float)(amount*basis);
        }
        public static double Measure(SensitivityRequest request,PlayerBuildSnapshot build)
        {
            if(request.mode==SensitivityMode.Analytical)return OptimizationMetricCatalog.Get(request.metric).Value(PlayerBuildEvaluator.Evaluate(build));
            var c=JsonUtility.FromJson<CombatLabRequest>(JsonUtility.ToJson(request.combat)); c.player=build;
            var result=CombatLabAdapters.Batch(c);return request.metric switch{"win_rate"=>result.winRate,"p50_ttk"=>result.duration.p50,"p90_ttk"=>result.duration.p90,"mana_starvation"=>result.manaStarvation.mean,"combat_dps"=>result.playerDps.mean,_=>result.winRate};
        }
        public static SensitivityResult Run(SensitivityRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(request.build==null)throw new ArgumentException("Select a build.");
            var result=new SensitivityResult{metadata=ExperimentMetadata.Create("Sensitivity",request.combat.seed,request.stats.Count,request.build.playerLevel,request.build.playerLevel,JsonUtility.ToJson(request)),metric=request.metric};
            double baseline=Measure(request,request.build);var bm=PlayerBuildEvaluator.Evaluate(request.build);
            for(int i=0;i<request.stats.Count;i++)
            {
                if(cancelled?.Invoke()==true)break;var stat=request.stats[i];float delta=Delta(stat,request.amount,request.perturbation,bm);var changed=request.build.Clone();changed.analysisDeltas.Add(new AnalysisStatDelta{stat=stat,amount=delta});double value=Measure(request,changed);double diff=value-baseline;
                result.rows.Add(new SensitivityRow{stat=stat,inputDelta=delta,baseline=baseline,modified=value,absoluteDelta=diff,percentDelta=Math.Abs(baseline)>1e-9?diff/baseline:0,elasticity=request.amount!=0&&Math.Abs(baseline)>1e-9?(diff/baseline)/request.amount:0});progress?.Invoke((i+1f)/request.stats.Count);
            }
            result.rows=result.rows.OrderByDescending(x=>Math.Abs(x.absoluteDelta)).ToList();return result;
        }
        public static List<CurvePoint> Curve(SensitivityRequest request,StatTypes stat,IEnumerable<float> amounts)
        {
            var baselineMetrics=PlayerBuildEvaluator.Evaluate(request.build);var result=new List<CurvePoint>();foreach(float amount in amounts){var copy=request.build.Clone();float delta=Delta(stat,amount,request.perturbation,baselineMetrics);copy.analysisDeltas.Add(new AnalysisStatDelta{stat=stat,amount=delta});result.Add(new CurvePoint{x=amount,mean=Measure(request,copy)});}return result;
        }
        public static List<SensitivityPair> Pairs(SensitivityRequest request,IReadOnlyList<StatTypes> stats,Func<bool> cancelled=null)
        {
            var result=new List<SensitivityPair>();var bm=PlayerBuildEvaluator.Evaluate(request.build);double origin=Measure(request,request.build);var singles=new Dictionary<StatTypes,double>();foreach(var stat in stats){var b=request.build.Clone();b.analysisDeltas.Add(new AnalysisStatDelta{stat=stat,amount=Delta(stat,request.amount,request.perturbation,bm)});singles[stat]=Measure(request,b)-origin;}
            for(int i=0;i<stats.Count;i++)for(int j=i+1;j<stats.Count;j++){if(cancelled?.Invoke()==true)return result;var b=request.build.Clone();foreach(var stat in new[]{stats[i],stats[j]})b.analysisDeltas.Add(new AnalysisStatDelta{stat=stat,amount=Delta(stat,request.amount,request.perturbation,bm)});double observed=Measure(request,b)-origin;result.Add(new SensitivityPair{a=stats[i],b=stats[j],gainA=singles[stats[i]],gainB=singles[stats[j]],observed=observed,interaction=observed-singles[stats[i]]-singles[stats[j]]});}return result.OrderByDescending(x=>Math.Abs(x.interaction)).ToList();
        }
    }

    public enum BreakpointOperator { Greater, GreaterOrEqual, Less, LessOrEqual, CrossUp, CrossDown }
    [Serializable] public sealed class BreakpointPoint { public int level; public double value; }
    [Serializable] public sealed class BreakpointResult { public int level;public double before,at,after,threshold,confidenceLow,confidenceHigh;public int samples;public long seed;public string metric,fingerprint;public List<BreakpointPoint> curve=new(); }
    [Serializable] public sealed class BreakpointBatchCase { public string name,classId,subclassId,weaponId,biomeId;public EnemyAI.EnemyRarity rarity;public int corruption; }
    [Serializable] public sealed class BreakpointBatchRow { public string scenario,confidence;public bool crossingFound;public int firstCrossing,allCrossings,samples;public double before,at,threshold,confidenceLow,confidenceHigh;public List<BreakpointResult> crossings=new(); }
    [Serializable] public sealed class BreakpointSampleBudget { public int initial=100,refinement=100,maximum=1000;public double confidenceZ=1.96; }
    public static class BreakpointFinder
    {
        public static (double low,double high) WilsonInterval(double proportion,int samples,double z=1.96)
        {if(samples<=0)return (0,1);double p=Math.Clamp(proportion,0,1),d=1+z*z/samples,c=(p+z*z/(2*samples))/d,h=z*Math.Sqrt(p*(1-p)/samples+z*z/(4.0*samples*samples))/d;return (Math.Max(0,c-h),Math.Min(1,c+h));}
        static bool Matches(double value,double threshold,BreakpointOperator op)=>op switch{BreakpointOperator.Greater or BreakpointOperator.CrossUp=>value>threshold,BreakpointOperator.GreaterOrEqual=>value>=threshold,BreakpointOperator.Less or BreakpointOperator.CrossDown=>value<threshold,_=>value<=threshold};
        public static List<BreakpointResult> Find(Func<int,double> evaluator,int start,int end,int increment,double threshold,BreakpointOperator op,bool all=false)
        {
            if(evaluator==null||end<start)throw new ArgumentException("Invalid breakpoint domain.");increment=Math.Max(1,increment);var cache=new Dictionary<int,double>();double At(int x){if(!cache.TryGetValue(x,out double v))cache[x]=v=evaluator(x);return v;}
            var results=new List<BreakpointResult>();bool prior=Matches(At(start),threshold,op);if(prior&&op is not (BreakpointOperator.CrossUp or BreakpointOperator.CrossDown))results.Add(new BreakpointResult{level=start,at=At(start),before=At(start),threshold=threshold});
            for(int coarse=start+increment;coarse<=end+increment;coarse+=increment){int high=Math.Min(end,coarse);if(high<=start)break;int low=Math.Max(start,coarse-increment);for(int level=low+1;level<=high;level++){bool current=Matches(At(level),threshold,op);bool crossed=op is BreakpointOperator.CrossUp or BreakpointOperator.CrossDown ? !prior&&current : !prior&&current;if(crossed){results.Add(new BreakpointResult{level=level,before=At(level-1),at=At(level),after=At(Math.Min(end,level+1)),threshold=threshold});if(!all)goto Done;}prior=current;}if(high==end)break;}
            Done:var points=cache.OrderBy(x=>x.Key).Select(x=>new BreakpointPoint{level=x.Key,value=x.Value}).ToList();foreach(var r in results)r.curve=points;return results;
        }
        public static List<BreakpointBatchRow> FindBatch(IReadOnlyList<BreakpointBatchCase> cases,Func<BreakpointBatchCase,int,double> evaluator,int start,int end,int increment,double threshold,BreakpointOperator op,BreakpointSampleBudget budget=null,Func<BreakpointBatchCase,int,int,double> sampledEvaluator=null,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(cases==null||evaluator==null)throw new ArgumentException("Batch cases and evaluator are required.");
            var rows=new List<BreakpointBatchRow>();
            for(int i=0;i<cases.Count;i++)
            {
                if(cancelled?.Invoke()==true)break;var c=cases[i];var points=Find(level=>sampledEvaluator==null?evaluator(c,level):sampledEvaluator(c,level,Math.Max(1,budget?.initial??100)),start,end,increment,threshold,op,true);
                var row=new BreakpointBatchRow{scenario=c.name,threshold=threshold,crossingFound=points.Count>0,firstCrossing=points.FirstOrDefault()?.level??0,allCrossings=points.Count,crossings=points,samples=sampledEvaluator==null?1:Math.Max(1,budget?.initial??100),confidence="DETERMINISTIC"};
                if(points.Count>0){row.before=points[0].before;row.at=points[0].at;}
                if(sampledEvaluator!=null&&points.Count>0)
                {
                    int max=Math.Max(row.samples,budget?.maximum??row.samples),step=Math.Max(1,budget?.refinement??row.samples);double z=budget?.confidenceZ??1.96;
                    while(true)
                    {
                        var ci=WilsonInterval(row.at,row.samples,z);row.confidenceLow=ci.low;row.confidenceHigh=ci.high;
                        bool ambiguous=ci.low<=threshold&&ci.high>=threshold;row.confidence=ambiguous?"AMBIGUOUS / CI OVERLAPS THRESHOLD":Matches(row.at,threshold,op)?"CLEAR CROSSING":"REFINEMENT DID NOT CONFIRM CROSSING";
                        if(!ambiguous||row.samples>=max||cancelled?.Invoke()==true)break;
                        row.samples=Math.Min(max,row.samples+step);row.at=sampledEvaluator(c,row.firstCrossing,row.samples);
                    }
                }
                rows.Add(row);progress?.Invoke((i+1f)/Math.Max(1,cases.Count));
            }
            return rows.OrderBy(x=>x.crossingFound?x.firstCrossing:int.MaxValue).ThenBy(x=>x.scenario,StringComparer.Ordinal).ToList();
        }
    }

    public enum BalanceWidgetKind { SingleMetric, Curve, Distribution, Matrix, Ranking, Breakpoint }
    [Serializable] public sealed class BalanceScenario { public string name,source="Player Analytical",metric="basic_dps";public BalanceWidgetKind widgetKind;public int startLevel=1,endLevel=100,increment=10,sampleCount=100;public double threshold=.7;public BreakpointOperator breakpointOperator=BreakpointOperator.Less;public List<PlayerBuildSnapshot> matrixBuilds=new();public OptimizationObjective objective=new();public ItemLabRequest item=new();public PlayerBuildSnapshot build=new();public CombatLabRequest combat=new();public DropLabRequest drop=new();public SensitivityRequest sensitivity=new();public AffixAnalysisRequest affix=new();public LootProgressionRequest loot=new();public CraftingSimulationRequest crafting=new(); }
    [Serializable] public sealed class BalanceSuite { public string name="Example / user-editable";public List<BalanceScenario> scenarios=new(); }
    [Serializable] public sealed class BalanceMetricValue { public string scenario,source,metric;public double value; }
    [Serializable] public sealed class BalanceSnapshot { public string name,description,timestampUtc,gitCommit,fingerprint;public BalanceSuite suite;public List<BalanceMetricValue> metrics=new();public List<SnapshotWidgetData> widgets=new(); }
    [Serializable] public sealed class BalanceDifference { public string scenario,source,metric,status;public double before,after,delta,percent;public bool highlighted; }
    public static class BalanceSnapshotService
    {
        public static BalanceSnapshot Capture(BalanceSuite suite,string name,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var snap=new BalanceSnapshot{name=name,timestampUtc=DateTime.UtcNow.ToString("O"),gitCommit=ProductionBalanceAdapters.GitCommit(),fingerprint=ProductionBalanceAdapters.DataFingerprint(),suite=JsonUtility.FromJson<BalanceSuite>(JsonUtility.ToJson(suite))};for(int i=0;i<suite.scenarios.Count;i++){if(cancelled?.Invoke()==true)break;var s=suite.scenarios[i];try{if(s.widgetKind==BalanceWidgetKind.SingleMetric){double value=s.source switch{"Combat"=>CombatValue(s),"Drops"=>DropValue(s),"Enemy"=>ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{level=s.combat.enemyLevel,sampleCount=32,seed=s.combat.seed,archetypeId=s.combat.enemyArchetypeId,rarity=s.combat.rarity}).dps.p50,"Sensitivity"=>SensitivityAnalyzer.Run(s.sensitivity).rows.Select(x=>Math.Abs(x.absoluteDelta)).DefaultIfEmpty(0).Max(),"Affix"=>AffixAnalyzer.Run(s.affix).rows.Select(x=>x.objectiveDelta).DefaultIfEmpty(0).Max(),"Loot Progression"=>LootProgressionAnalyzer.Run(s.loot).upgrades.Count/(double)Math.Max(1,s.loot.kills)*100,"Crafting"=>CraftingSuccess(s.crafting),_=>OptimizationMetricCatalog.Get(s.metric).Value(PlayerBuildEvaluator.Evaluate(s.build))};snap.metrics.Add(new BalanceMetricValue{scenario=s.name,source=s.source,metric=s.metric,value=value});}else snap.widgets.Add(BalanceSnapshotWidgets.Capture(s,cancelled));}catch(Exception ex){snap.widgets.Add(new SnapshotWidgetData{scenario=s.name,kind=s.widgetKind,source=s.source,status="FAILED",error=ex.Message});}progress?.Invoke((i+1f)/suite.scenarios.Count);}return snap;
        }
        static double CraftingSuccess(CraftingSimulationRequest request){var r=CraftingSimulator.Run(request);return r.trials>0?r.successes/(double)r.trials:0;}
        static double CombatValue(BalanceScenario s){var r=CombatLabAdapters.Batch(s.combat);return s.metric switch{"p50_ttk"=>r.duration.p50,"p90_ttk"=>r.duration.p90,"mana_starvation"=>r.manaStarvation.mean,_=>r.winRate};}
        static double DropValue(BalanceScenario s){var r=ProductionBalanceAdapters.RunDrops(s.drop);return s.metric=="gear_per_kill"?r.averageGearItems:r.averageCurrencyRolls;}
        public static List<BalanceDifference> Compare(BalanceSnapshot a,BalanceSnapshot b,double highlightPercent)
        {
            var result=new List<BalanceDifference>();var keys=a.metrics.Select(x=>$"{x.scenario}|{x.source}|{x.metric}").Union(b.metrics.Select(x=>$"{x.scenario}|{x.source}|{x.metric}"));foreach(string key in keys){var x=a.metrics.FirstOrDefault(v=>$"{v.scenario}|{v.source}|{v.metric}"==key);var y=b.metrics.FirstOrDefault(v=>$"{v.scenario}|{v.source}|{v.metric}"==key);double delta=x!=null&&y!=null?y.value-x.value:0,percent=x!=null&&y!=null&&Math.Abs(x.value)>1e-9?delta/x.value:0;result.Add(new BalanceDifference{scenario=y?.scenario??x?.scenario,source=y?.source??x?.source,metric=y?.metric??x?.metric,before=x?.value??0,after=y?.value??0,delta=delta,percent=percent,highlighted=x!=null&&y!=null&&Math.Abs(percent)>=highlightPercent,status=x==null?"MISSING FROM A":y==null?"MISSING FROM B":a.fingerprint!=b.fingerprint?"STALE":Math.Abs(delta)>1e-9?"CHANGED":"UNCHANGED"});}return result;
        }
        public static string ExportMarkdown(BalanceSnapshot snapshot,IReadOnlyList<BalanceDifference> differences=null)
        {
            Directory.CreateDirectory(WorkbenchExports.Root);string stem="balance_report_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");string path=Path.Combine(WorkbenchExports.Root,stem+".md"),csv=Path.Combine(WorkbenchExports.Root,stem+".csv");var lines=new List<string>{"# "+snapshot.name,"","Captured: "+snapshot.timestampUtc,"Git: "+snapshot.gitCommit,"Production fingerprint: "+snapshot.fingerprint,"","[Metric data]("+Path.GetFileName(csv)+")","","| Scenario | Source | Metric | Value |","|---|---|---|---:|"};lines.AddRange(snapshot.metrics.Select(x=>$"| {x.scenario} | {x.source} | {x.metric} | {x.value:0.####} |"));File.WriteAllLines(csv,new[]{"Scenario,Source,Metric,Value"}.Concat(snapshot.metrics.Select(x=>$"\"{x.scenario.Replace("\"","\"\"")}\",\"{x.source}\",\"{x.metric}\",{x.value:R}")));
            foreach(var group in snapshot.metrics.GroupBy(x=>x.metric))
            {
                var points=group.Select((x,i)=>new CurvePoint{x=i+1,mean=x.value}).ToList();if(points.Count==1)points.Insert(0,new CurvePoint{x=0,mean=0});var series=new CurveSeries{name=group.Key,color=Color.cyan,points=points};string png=new BalanceWorkbenchChart().ExportPng(new[]{series},stem+"_"+group.Key);lines.AddRange(new[]{"","### "+group.Key,"","!["+group.Key+"]("+Path.GetFileName(png)+")"});
            }
            if(differences!=null){lines.AddRange(new[]{"","## Regression comparison","","| Scenario | Metric | Before | After | Delta | Change |","|---|---|---:|---:|---:|---:|"});lines.AddRange(differences.Select(x=>$"| {x.scenario} | {x.metric} | {x.before:0.####} | {x.after:0.####} | {x.delta:+0.####;-0.####;0} | {x.percent:+0.##%;-0.##%;0%} |"));}
            var highlighted=differences?.Where(x=>x.highlighted).ToList()??new List<BalanceDifference>();var invalid=snapshot.metrics.Where(x=>!double.IsFinite(x.value)).ToList();if(highlighted.Count>0||invalid.Count>0){lines.AddRange(new[]{"","## Warnings",""});lines.AddRange(highlighted.Select(x=>"- User threshold crossed: "+x.scenario+" / "+x.metric+" changed "+x.percent.ToString("+0.##%;-0.##%;0%")));lines.AddRange(invalid.Select(x=>"- Technical validity: non-finite result for "+x.scenario+" / "+x.metric));}
            File.WriteAllLines(path,lines);AssetDatabase.Refresh();return path;
        }
    }
}
