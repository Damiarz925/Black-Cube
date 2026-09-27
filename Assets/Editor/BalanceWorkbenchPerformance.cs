using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class WorkbenchPerformanceRow
    {
        public string operation,resultHash,error;public double elapsedMs,allocatedMb;public int gen0,gen1,gen2,states,evaluations;
    }
    [Serializable] public sealed class WorkbenchPerformanceReport
    {
        public string phase,utc,commit,fingerprint,machine,unityVersion,runtimeVersion;public List<WorkbenchPerformanceRow> rows=new();
    }
    public static class WorkbenchPerformanceBenchmarks
    {
        static readonly OptimizationObjective Objective=new(){primary="physical",secondary="attack_speed",primaryWeight=.70f,mode=OptimizationMode.Weighted};
        static PlayerBuildSnapshot Fixture()
        {
            var build=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,seed=41001};
            var profile=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset");
            if(profile==null)throw new InvalidOperationException("Mid player gear profile is missing.");
            return PlayerGearsetOptimizer.Optimize(build,profile,Objective,false).build;
        }
        static string Hash(string value)
        {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value??""))).Replace("-",string.Empty).Substring(0,24);}
        static string StableRows<T>(IEnumerable<T> rows)=>string.Join("\n",rows.Select(x=>JsonUtility.ToJson(x)));
        static void Measure(WorkbenchPerformanceReport report,string name,Func<(string result,int states,int evaluations)> run)
        {
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();long allocation=GC.GetAllocatedBytesForCurrentThread();int g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);var clock=Stopwatch.StartNew();var row=new WorkbenchPerformanceRow{operation=name};
            try{var result=run();row.resultHash=Hash(result.result);row.states=result.states;row.evaluations=result.evaluations;}
            catch(Exception ex){row.error=ex.ToString();}
            finally{clock.Stop();row.elapsedMs=clock.Elapsed.TotalMilliseconds;row.allocatedMb=(GC.GetAllocatedBytesForCurrentThread()-allocation)/(1024.0*1024);row.gen0=GC.CollectionCount(0)-g0;row.gen1=GC.CollectionCount(1)-g1;row.gen2=GC.CollectionCount(2)-g2;report.rows.Add(row);Save(report);UnityEngine.Debug.Log($"PERF {name}: {row.elapsedMs:0.###} ms, {row.allocatedMb:0.###} MB, hash {row.resultHash}, error {row.error}");}
        }
        static void RecordPhase(WorkbenchPerformanceReport report,string name,double ms,int count,string resultHash)
        {
            report.rows.Add(new WorkbenchPerformanceRow{operation=name,elapsedMs=ms,
                evaluations=count,resultHash=resultHash});
            Save(report);
        }
        static void Save(WorkbenchPerformanceReport report)
        {
            Directory.CreateDirectory("Logs/BalanceWorkbenchPerformance");string stem=report.phase;File.WriteAllText($"Logs/BalanceWorkbenchPerformance/{stem}.json",JsonUtility.ToJson(report,true));
            File.WriteAllLines($"Logs/BalanceWorkbenchPerformance/{stem}.csv",new[]{"Operation,ElapsedMs,AllocatedMB,Gen0,Gen1,Gen2,States,Evaluations,ResultHash,Error"}.Concat(report.rows.Select(x=>$"\"{x.operation}\",{x.elapsedMs:R},{x.allocatedMb:R},{x.gen0},{x.gen1},{x.gen2},{x.states},{x.evaluations},{x.resultHash},\"{(x.error??"").Replace("\"","\"\"")}\"")));
        }
        static void Execute(string phase,string segment,bool exitEditor=true,bool quick=false)
        {
            var report=new WorkbenchPerformanceReport{phase=phase+"_"+segment,utc=DateTime.UtcNow.ToString("O"),commit=ProductionBalanceAdapters.GitCommit(),fingerprint=ProductionBalanceAdapters.DataFingerprint(),machine=Environment.MachineName,unityVersion=Application.unityVersion,runtimeVersion=Environment.Version.ToString()};
            try
            {
                var build=Fixture();Measure(report,"Fixture Mid gear",()=> (JsonUtility.ToJson(build),0,0));
                if(segment=="passive")
                {
                    var searches=quick?
                        new[]{(name:"Greedy 50",beam:false,width:1),(name:"Beam 50 / 50",beam:true,width:50)}:
                        new[]{(name:"Greedy 50",beam:false,width:1),(name:"Beam 50 / 50",beam:true,width:50),(name:"Beam 100 / 50",beam:true,width:100)};
                    foreach(var entry in searches)
                    {
                        PassiveOptimizationResult measured=null;
                        Measure(report,entry.name,()=>{measured=PassiveTreeOptimizer.Optimize(build,50,Objective,entry.beam,entry.width);return (JsonUtility.ToJson(measured.build)+"|"+measured.score.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+string.Join(",",measured.sequence.Select(x=>x.stableId)),measured.debug.Sum(x=>x.candidateStates),measured.debug.Sum(x=>x.statesEvaluated));});
                        if(measured==null)continue;
                        string hash=report.rows[^1].resultHash;
                        RecordPhase(report,entry.name+" · legal next",measured.profile.legalNextMs,measured.profile.legalCalls,hash);
                        RecordPhase(report,entry.name+" · build evaluation",measured.profile.buildEvaluationMs,measured.profile.evaluations,hash);
                        RecordPhase(report,entry.name+" · retain",measured.profile.retainMs,measured.profile.evaluations,hash);
                        RecordPhase(report,entry.name+" · other",measured.profile.otherMs,measured.profile.evaluations,hash);
                    }
                }
                else
                {
                    Measure(report,"Player evaluation single",()=> (JsonUtility.ToJson(PlayerBuildEvaluator.Evaluate(build)),0,1));
                    int itemCount=quick?100:1000,enemyCount=quick?100:1000,dropCount=quick?1000:10000;
                    Measure(report,$"Player evaluation x{itemCount}",()=>{PlayerBuildMetrics last=null;for(int i=0;i<itemCount;i++)last=PlayerBuildEvaluator.Evaluate(build);return (JsonUtility.ToJson(last),0,itemCount);});
                    Measure(report,$"Items x{itemCount}",()=>{var r=ProductionBalanceAdapters.RunItems(new ItemLabRequest{itemLevel=50,sampleCount=itemCount,rarity=LootManager.GearRarity.Rare,weaponTypeId=WeaponTypeIds.Sword,seed=41001});return (StableRows(r.samples),0,itemCount);});
                    string enemyId=WorldContentCatalog.Reference.enemyArchetypes.First().stableId;
                    EnemyLabResult enemyProfile=null;
                    Measure(report,$"Enemies x{enemyCount}",()=>{enemyProfile=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{archetypeId=enemyId,level=50,sampleCount=enemyCount,rarity=EnemyAI.EnemyRarity.Rare,seed=41001});return (StableRows(enemyProfile.samples),0,enemyCount);});
                    if(enemyProfile!=null)
                    {
                        string hash=report.rows[^1].resultHash;int count=enemyProfile.samples.Count;var t=enemyProfile.timing;
                        RecordPhase(report,$"Enemy instantiate x{count}",t.instantiateMs,count,hash);
                        RecordPhase(report,$"Enemy configure x{count}",t.configureMs,count,hash);
                        RecordPhase(report,$"Enemy generate production x{count}",t.generateMs,count,hash);
                        RecordPhase(report,$"Enemy candidate gear x{count}",t.candidateGearMs,count,hash);
                        RecordPhase(report,$"Enemy build optimizer x{count}",t.buildOptimizerMs,count,hash);
                        RecordPhase(report,$"Enemy evaluate generated x{count}",t.captureMs,count,hash);
                        RecordPhase(report,$"Enemy power score x{count}",t.powerScoreMs,count,hash);
                        RecordPhase(report,$"Enemy dispose x{count}",t.disposeMs,count,hash);
                    }
                    Measure(report,$"Drops x{dropCount}",()=>{var r=ProductionBalanceAdapters.RunDrops(new DropLabRequest{level=50,rarity=EnemyAI.EnemyRarity.Rare,sampleCount=dropCount,seed=41001,generateActualEnemyGear=false});return ($"{r.averageGearItems:R}|{r.averageCurrencyRolls:R}|{r.chanceAnyCurrency:R}|{StableRows(r.currencies)}",0,dropCount);});
                    var combat=new CombatLabRequest{player=build,enemyArchetypeId=enemyId,enemyLevel=50,rarity=EnemyAI.EnemyRarity.Rare,seed=41001};
                    foreach(int count in quick?new[]{1000}:new[]{1000,10000})Measure(report,$"Pure combats x{count}",()=>{combat.fightCount=count;var r=CombatLabAdapters.Batch(combat);return (JsonUtility.ToJson(r),0,count);});
                    Measure(report,"Sensitivity analytical",()=>{var r=SensitivityAnalyzer.Run(new SensitivityRequest{build=build,stats=new(){StatTypes.AttackSpeed,StatTypes.CritChance,StatTypes.CritMult,StatTypes.PhysDmg},amount=.1f});return (StableRows(r.rows),0,r.rows.Count);});
                    Measure(report,"Fingerprint x100",()=>{string last=null;for(int i=0;i<100;i++)last=ProductionBalanceAdapters.DataFingerprint();return (last,0,100);});
                    var mid=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>(
                        "Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset");
                    Measure(report,"Scenario Mid L10–50 gear only",()=>
                    {
                        var sweep=PlayerScenarioSweep.Run(build,new[]{mid},Objective,10,50,10,
                            false,false,true,1);
                        return (StableRows(sweep.points),
                            sweep.points.Sum(x=>x.gearGenerations),
                            sweep.points.Sum(x=>x.gearEvaluationRequests));
                    });
                }
            }
            finally{Save(report);if(exitEditor)EditorApplication.Exit(report.rows.Any(x=>x.error!=null)?1:0);}
            if(!exitEditor&&report.rows.Any(x=>x.error!=null))
                throw new InvalidOperationException("Benchmark failed: "+report.rows.First(x=>x.error!=null).error);
        }
        public static void BaselineCore()=>Execute("baseline","core");
        public static void BaselinePassive()=>Execute("baseline","passive");
        public static void AfterCore()=>Execute("after","core");
        public static void AfterPassive()=>Execute("after","passive");
        public static void TrustBeforeCore()=>Execute("trust_before","core");
        public static void TrustBeforePassive()=>Execute("trust_before","passive");
        public static void EditorQuick(){Execute("after_quick","core",false,true);Execute("after_quick","passive",false,true);}
        public static void EditorFull(){Execute("after","core",false);Execute("after","passive",false);}
    }

    public static class WorkbenchPerformanceTestRunner
    {
        static Callbacks callbacks;
        public static void Focused()=>Run("BalanceWorkbenchPerformanceTests","Logs/BalanceWorkbenchPerformance/focused_result.txt");
        public static void AllEditMode()=>Run(null,"Logs/BalanceWorkbenchPerformance/all_editmode_result.txt");
        static void Run(string name,string path)
        {
            Directory.CreateDirectory("Logs/BalanceWorkbenchPerformance");callbacks=new Callbacks(path);
            var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(callbacks);
            var filter=new Filter{testMode=TestMode.EditMode};if(name!=null)filter.testNames=new[]{name};
            api.Execute(new ExecutionSettings(filter));
        }
        sealed class Callbacks:ICallbacks
        {
            readonly string path;readonly List<string> failures=new();public Callbacks(string path){this.path=path;}
            public void RunStarted(ITestAdaptor test){}public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){if(result.FailCount>0)failures.Add(result.Name+": "+result.Message);}
            public void RunFinished(ITestResultAdaptor result)
            {File.WriteAllText(path,$"result={result.TestStatus}\npassed={result.PassCount}\nfailed={result.FailCount}\nskipped={result.SkipCount}\nduration={result.Duration:0.###}\n"+string.Join("\n",failures));EditorApplication.Exit(result.FailCount==0?0:1);}
        }
    }
    public static class WorkbenchPerformanceBuildRunner
    {
        public static void BuildWindows()
        {
            try
            {
                string output=Path.GetFullPath("Builds/BalanceWorkbenchPerformanceWindows/BlackCube.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
                string[] scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray();
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
                Directory.CreateDirectory("Logs/BalanceWorkbenchPerformance");
                File.WriteAllText("Logs/BalanceWorkbenchPerformance/windows_build.txt",$"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\noutput={output}\nscenes={string.Join(",",scenes)}\n");
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
            }
            catch(Exception error){UnityEngine.Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
