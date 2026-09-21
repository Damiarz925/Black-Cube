using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using UnityEditor;
using UnityEngine;

public static class Tooling2ValidationRunner
{
    public static void RunSmoke()=>Run(() =>
    {
        Directory.CreateDirectory("Logs");
        var lines=new List<string>{"TOOLING 2 EDITOR WORKFLOW SMOKE"};
        // The same commands are exposed by BalanceWorkbenchWindow; headless smoke invokes
        // their production-backed services directly because batchmode has no graphics device.
        var objective=new OptimizationObjective{primary="physical",secondary="attack_speed",primaryWeight=.7f,mode=OptimizationMode.Weighted};
        var template=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,subclassId=string.Empty,weaponTypeId=WeaponTypeIds.Sword,seed=22001};
        var optimized=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Optimized.asset");
        if(optimized==null)throw new InvalidOperationException("Optimized gear profile is missing.");

        var gear=PlayerGearsetOptimizer.Optimize(template,optimized,objective,true);
        var repeat=PlayerGearsetOptimizer.Optimize(template,optimized,objective,true);
        string[] first=gear.build.equipment.OrderBy(x=>x.slot).Select(x=>x.Description).ToArray();
        string[] second=repeat.build.equipment.OrderBy(x=>x.slot).Select(x=>x.Description).ToArray();
        if(!first.SequenceEqual(second))throw new InvalidOperationException("Same-seed optimized gear rerun was not exact.");
        if(gear.build.equipment.Count!=Enum.GetValues(typeof(LootManager.GearType)).Length)throw new InvalidOperationException("Optimized gearset did not fill every slot.");
        lines.Add($"PASS Player Build: L50 Warrior/Sword optimized gear; DPS={gear.metrics.basicDps:0.###}, APS={gear.metrics.attacksPerSecond:0.###}, score={gear.score:0.######}");
        lines.Add($"PASS exact rerun: seed={template.seed}, candidates={gear.debug.generated}, states={gear.debug.statesEvaluated}, beam={gear.debug.beamWidth}");

        var greedy=PassiveTreeOptimizer.Optimize(gear.build,50,objective,false);
        var beam=PassiveTreeOptimizer.Optimize(gear.build,50,objective,true,4);
        ValidatePassive(greedy,template.classId,template.subclassId,"Greedy");
        ValidatePassive(beam,template.classId,template.subclassId,"Beam");
        var marginal=PassiveTreeOptimizer.Analyze(beam.build,objective,null,false);
        lines.Add($"PASS Passives: greedy={greedy.score:0.######}, beam={beam.score:0.######}, beamWidth=4, legalNext={marginal.Count}");
        lines.Add("PASS heatmap data: authored layout positions="+PassiveTreeDefinition.Nodes.Select(x=>x.LayoutPosition).Distinct().Count());
        if(beam.sequence.Count>0)
        {
            var top=beam.sequence.OrderByDescending(x=>x.scoreDelta).First();
            lines.Add($"PASS Why This Node: point={top.point}, id={top.stableId}, objectiveDelta={top.scoreDelta:0.######}");
        }

        var smokeProfiles=new List<PlayerGearProfileSO>();
        try
        {
            foreach(string name in new[]{"Low","Mid","Optimized"})
            {
                var source=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>($"Assets/Balance/Profiles/SO_PlayerGearProfile_{name}.asset");
                if(source==null)throw new InvalidOperationException("Missing gear profile: "+name);
                var copy=UnityEngine.Object.Instantiate(source);copy.name=source.name;copy.candidatesPerSlot=1;copy.candidateRetention=1;copy.gearsetBeamWidth=1;smokeProfiles.Add(copy);
            }
            var sweepTemplate=template.Clone();sweepTemplate.playerLevel=10;sweepTemplate.combatLevel=10;sweepTemplate.seed=23001;
            var sweep=PlayerScenarioSweep.Run(sweepTemplate,smokeProfiles,objective,10,100,10,false,false,true,1);
            if(sweep.points.Count!=30)throw new InvalidOperationException("Expected 30 Low/Mid/Optimized curve points, got "+sweep.points.Count);
            var opened=sweep.points.Single(x=>x.playerLevel==60&&x.profile.Contains("Optimized"));
            if(opened.build==null||opened.metrics==null)throw new InvalidOperationException("Level-60 click-through build was not retained.");
            lines.Add("PASS Gear Curves / Scenario Sweep: 30 exact snapshots (levels 10-100, Low/Mid/Optimized); L60 click-through retained");

            var enemies=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{level=60,sampleCount=20,productionRarity=true,seed=24001});
            var ordered=enemies.samples.OrderBy(x=>x.gearScore).ToList();
            EnemySample p50=ordered[(int)Math.Round((ordered.Count-1)*.5)],p90=ordered[(int)Math.Round((ordered.Count-1)*.9)];
            double ttk50=PlayerScenarioSweep.AnalyticalTtk(opened.metrics,p50),ttk90=PlayerScenarioSweep.AnalyticalTtk(opened.metrics,p90);
            double ttd50=PlayerScenarioSweep.AnalyticalTtd(opened.metrics,p50),ttd90=PlayerScenarioSweep.AnalyticalTtd(opened.metrics,p90);
            if(!double.IsFinite(ttk50)||!double.IsFinite(ttk90)||!double.IsFinite(ttd50)||!double.IsFinite(ttd90))throw new InvalidOperationException("Analytical P50/P90 comparison was not finite.");
            lines.Add($"PASS Player vs Enemy analytical estimates: P50 TTK={ttk50:0.###}, P90 TTK={ttk90:0.###}, P50 TTD={ttd50:0.###}, P90 TTD={ttd90:0.###}");

            string buildCsv=WorkbenchExports.SaveBuildCsv(gear.build,gear.metrics);
            string passiveCsv=WorkbenchExports.SavePassiveCsv(beam);
            string sweepCsv=WorkbenchExports.SaveSweepCsv(sweep,objective);
            string png=new BalanceWorkbenchChart().ExportPng(sweep.Curves("basic_dps"),"tooling2_smoke_curve",640,360);
            foreach(string path in new[]{buildCsv,passiveCsv,sweepCsv,png})if(!File.Exists(path)||new FileInfo(path).Length==0)throw new InvalidOperationException("Export missing or empty: "+path);
            lines.Add($"PASS exports: {buildCsv}, {passiveCsv}, {sweepCsv}, {png}");
        }
        finally
        {
            foreach(var profile in smokeProfiles)if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);
        }
        File.WriteAllLines("Logs/Tooling2EditorSmoke.txt",lines);
        Debug.Log(string.Join("\n",lines));
    });

    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildTooling2Windows);

    static void ValidatePassive(PassiveOptimizationResult result,string classId,string subclassId,string label)
    {
        if(result.build.passiveStableIds.Count!=50)throw new InvalidOperationException(label+" optimizer did not spend 50 points.");
        if(!PlayerProgression.ValidateAllocationState(result.build.AllocationRanks(),classId,subclassId))throw new InvalidOperationException(label+" optimizer returned an invalid production allocation.");
    }

    static void Run(Action action)
    {
        try{action();EditorApplication.Exit(0);}
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}
