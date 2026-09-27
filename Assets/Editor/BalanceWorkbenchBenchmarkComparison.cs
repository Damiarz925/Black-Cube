using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public static class WorkbenchBenchmarkComparison
    {
        public static bool Equivalent(WorkbenchPerformanceRow before,WorkbenchPerformanceRow after)=>
            before!=null&&after!=null&&string.IsNullOrEmpty(before.error)&&
            string.IsNullOrEmpty(after.error)&&!string.IsNullOrEmpty(before.resultHash)&&
            before.resultHash==after.resultHash&&before.evaluations==after.evaluations;

        public static WorkbenchPerformanceReport Read(string phase)
        {
            string path=$"Logs/BalanceWorkbenchPerformance/{phase}.json";
            return File.Exists(path)?JsonUtility.FromJson<WorkbenchPerformanceReport>(File.ReadAllText(path)):null;
        }

        public static void CaptureCurrentAsBaseline()
        {
            string directory="Logs/BalanceWorkbenchPerformance";
            foreach(string segment in new[]{"core","passive","quick_core","quick_passive"})
            {
                string source=Path.Combine(directory,"after_"+segment+".json");
                if(!File.Exists(source))continue;
                var report=JsonUtility.FromJson<WorkbenchPerformanceReport>(File.ReadAllText(source));
                if(report?.rows==null||report.rows.Count==0)continue;
                report.phase="baseline_"+segment;
                File.WriteAllText(Path.Combine(directory,report.phase+".json"),JsonUtility.ToJson(report,true));
            }
        }
    }

    public sealed partial class BalanceWorkbenchWindow
    {
        bool benchmarkComparisonLoaded;
        WorkbenchPerformanceReport benchmarkBeforeCore,benchmarkBeforePassive,
            benchmarkAfterCore,benchmarkAfterPassive,benchmarkBeforeQuickCore,
            benchmarkBeforeQuickPassive,benchmarkAfterQuickCore,benchmarkAfterQuickPassive;
        Vector2 benchmarkComparisonScroll;

        void RefreshBenchmarkComparison()
        {
            benchmarkBeforeCore=WorkbenchBenchmarkComparison.Read("baseline_core")??
                WorkbenchBenchmarkComparison.Read("trust_before_core");
            benchmarkBeforePassive=WorkbenchBenchmarkComparison.Read("baseline_passive")??
                WorkbenchBenchmarkComparison.Read("trust_before_passive");
            benchmarkAfterCore=WorkbenchBenchmarkComparison.Read("after_core");
            benchmarkAfterPassive=WorkbenchBenchmarkComparison.Read("after_passive");
            benchmarkBeforeQuickCore=WorkbenchBenchmarkComparison.Read("baseline_quick_core");
            benchmarkBeforeQuickPassive=WorkbenchBenchmarkComparison.Read("baseline_quick_passive");
            benchmarkAfterQuickCore=WorkbenchBenchmarkComparison.Read("after_quick_core");
            benchmarkAfterQuickPassive=WorkbenchBenchmarkComparison.Read("after_quick_passive");
            benchmarkComparisonLoaded=true;
        }

        void DrawBenchmarkComparison()
        {
            if(!benchmarkComparisonLoaded)RefreshBenchmarkComparison();
            Heading("HASH-GATED BENCHMARK COMPARISON");
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("RUN QUICK BENCHMARKS",GUILayout.Width(205)))
                    Run("Quick Workbench Benchmarks",_=>
                    {WorkbenchPerformanceBenchmarks.EditorQuick();RefreshBenchmarkComparison();});
                if(GUILayout.Button("RUN FULL BENCHMARKS",GUILayout.Width(195))&&
                    EditorUtility.DisplayDialog("Run full benchmarks?",
                        "This includes 1,000 production enemy samples and Beam 100 / 50. It may occupy the Editor for several minutes.","Run","Cancel"))
                    Run("Full Workbench Benchmarks",_=>
                    {WorkbenchPerformanceBenchmarks.EditorFull();RefreshBenchmarkComparison();});
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("REFRESH REPORTS",GUILayout.Width(170)))RefreshBenchmarkComparison();
                if(GUILayout.Button("CAPTURE CURRENT AS BASELINE",GUILayout.Width(235)))
                {
                    if(EditorUtility.DisplayDialog("Capture benchmark baseline?",
                        "This explicitly replaces saved local baseline reports with the latest after-run reports. No production data changes.","Capture","Cancel"))
                    {WorkbenchBenchmarkComparison.CaptureCurrentAsBaseline();RefreshBenchmarkComparison();}
                }
            }
            EditorGUILayout.HelpBox("Quick and Full write separate local reports. Full may take several minutes. Equivalent hashes and counts are required before any speed comparison; changed hashes are NOT an equivalence benchmark.",MessageType.Info);
            DrawBenchmarkGroup("CORE / ENEMY / CACHE — FULL",benchmarkBeforeCore,benchmarkAfterCore);
            DrawBenchmarkGroup("PASSIVE — FULL",benchmarkBeforePassive,benchmarkAfterPassive);
            DrawBenchmarkGroup("CORE / ENEMY / CACHE — QUICK",benchmarkBeforeQuickCore,benchmarkAfterQuickCore);
            DrawBenchmarkGroup("PASSIVE — QUICK",benchmarkBeforeQuickPassive,benchmarkAfterQuickPassive);
        }

        void DrawBenchmarkGroup(string name,WorkbenchPerformanceReport before,
            WorkbenchPerformanceReport after)
        {
            Heading(name);
            if(after?.rows==null){EditorGUILayout.HelpBox("No current report loaded.",MessageType.Info);return;}
            EditorGUILayout.LabelField("Current context",
                $"{after.utc} · {after.commit} · {after.machine} · Unity {after.unityVersion}");
            if(before!=null&&before.fingerprint!=after.fingerprint)
                EditorGUILayout.HelpBox("Baseline and current production-data fingerprints differ. Timings are diagnostic only until the data context matches.",MessageType.Warning);
            var earlier=before?.rows?.ToDictionary(x=>x.operation,StringComparer.Ordinal)??
                new Dictionary<string,WorkbenchPerformanceRow>(StringComparer.Ordinal);
            benchmarkComparisonScroll=EditorGUILayout.BeginScrollView(benchmarkComparisonScroll,
                GUILayout.MinHeight(100),GUILayout.MaxHeight(340));
            using(new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Benchmark",GUILayout.Width(255));GUILayout.Label("Before ms",GUILayout.Width(90));
                GUILayout.Label("Current ms",GUILayout.Width(90));GUILayout.Label("Count",GUILayout.Width(70));
                GUILayout.Label("ms / op",GUILayout.Width(75));GUILayout.Label("Result hash",GUILayout.Width(190));
                GUILayout.Label("Alloc MB",GUILayout.Width(75));GUILayout.Label("GC 0/1/2",GUILayout.Width(70));
                GUILayout.Label("Equivalence / change",GUILayout.Width(210));
            }
            foreach(var row in after.rows)
            {
                earlier.TryGetValue(row.operation,out var old);
                bool sameContext=before!=null&&before.fingerprint==after.fingerprint;
                bool equivalent=sameContext&&WorkbenchBenchmarkComparison.Equivalent(old,row);
                string status=old==null?"No baseline":!sameContext?"DIFFERENT DATA CONTEXT":equivalent?
                    $"MATCH · {(row.elapsedMs<old.elapsedMs?"faster":"slower")} {Math.Abs(row.elapsedMs-old.elapsedMs):0.###} ms":
                    "NON-EQUIVALENT HASH/COUNT";
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent(row.operation,row.operation),GUILayout.Width(255));
                    GUILayout.Label(old==null?"—":old.elapsedMs.ToString("0.###"),GUILayout.Width(90));
                    GUILayout.Label(row.elapsedMs.ToString("0.###"),GUILayout.Width(90));
                    GUILayout.Label(row.evaluations.ToString(),GUILayout.Width(70));
                    GUILayout.Label((row.elapsedMs/Math.Max(1,row.evaluations)).ToString("0.####"),GUILayout.Width(75));
                    GUILayout.Label(new GUIContent(row.resultHash??"ERROR",row.error??row.resultHash),GUILayout.Width(190));
                    GUILayout.Label(row.allocatedMb.ToString("0.##"),GUILayout.Width(75));
                    GUILayout.Label($"{row.gen0}/{row.gen1}/{row.gen2}",GUILayout.Width(70));
                    GUILayout.Label(status,GUILayout.Width(210));
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
