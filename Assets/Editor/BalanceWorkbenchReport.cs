using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public enum ReportWidgetType { TextNotes,SingleMetric,MetricTable,CurveChart,DistributionHistogram,MatchupMatrix,SensitivityRanking,AffixRanking,PassiveTopN,LootUpgradeTable,CurrencyTable,CraftingSummary,BreakpointList,SnapshotRegressionSummary }
    public enum ReportDataSource { LiveCurrentResults,SavedSnapshotResults }
    [Serializable] public sealed class ReportWidgetPreset { public string title="Widget",scenarioName,notes;public ReportWidgetType type;public ReportDataSource dataSource; }
    [Serializable] public sealed class ReportSectionPreset { public string title="Section",notes;public List<ReportWidgetPreset> widgets=new(); }
    [Serializable] public sealed class BalanceReportPreset { public string title="Black-Cube Balance Report",designerNotes;public List<ReportSectionPreset> sections=new(); }
    [Serializable] public sealed class ReportWidgetResult { public int sectionOrder;public string section,sectionNotes,title,scenarioName,source,status,error,notes;public ReportWidgetType type;public BalanceMetricValue metric;public SnapshotWidgetData widget;public List<BalanceDifference> scalarRegression=new();public List<SnapshotWidgetDifference> widgetRegression=new(); }
    [Serializable] public sealed class BalanceReportRun { public string title,timestampUtc,fingerprint,gitCommit,designerNotes;public bool cancelled;public List<ReportWidgetResult> widgets=new(); }
    public static class BalanceReportService
    {
        static string Safe(string input)
        {var clean=new string((input??"report").Select(x=>char.IsLetterOrDigit(x)||x is '-' or '_'?x:'_').ToArray()).Trim('_');return string.IsNullOrEmpty(clean)?"report":clean.Substring(0,Math.Min(60,clean.Length));}
        static string Csv(string x)=>"\""+(x??"").Replace("\"","\"\"")+"\"";
        public static BalanceReportRun Run(BalanceReportPreset preset,BalanceSuite suite,BalanceSnapshot saved=null,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(preset==null||suite==null)throw new ArgumentException("Choose a report preset and scenario suite.");
            var run=new BalanceReportRun{title=preset.title,designerNotes=preset.designerNotes,timestampUtc=DateTime.UtcNow.ToString("O"),fingerprint=ProductionBalanceAdapters.DataFingerprint(),gitCommit=ProductionBalanceAdapters.GitCommit()};
            int total=preset.sections.Sum(x=>x.widgets.Count),index=0;
            for(int sectionIndex=0;sectionIndex<preset.sections.Count;sectionIndex++)foreach(var selected in preset.sections[sectionIndex].widgets)
            {
                if(cancelled?.Invoke()==true){run.cancelled=true;return run;}
                var section=preset.sections[sectionIndex];var result=new ReportWidgetResult{sectionOrder=sectionIndex,section=section.title,sectionNotes=section.notes,title=selected.title,scenarioName=selected.scenarioName,source=selected.dataSource.ToString(),type=selected.type,notes=selected.notes,status="OK"};
                if(selected.type==ReportWidgetType.SnapshotRegressionSummary)
                {
                    if(saved==null){result.status="MISSING SNAPSHOT";result.error="Capture or load a Before snapshot for regression.";}
                    else
                    {
                        var current=BalanceSnapshotService.Capture(saved.suite,"Report current",null,cancelled);
                        result.scalarRegression=BalanceSnapshotService.Compare(saved,current,.1);
                        result.widgetRegression=BalanceSnapshotWidgets.Compare(saved,current);
                        if(saved.fingerprint!=run.fingerprint)result.status="STALE";
                    }
                }
                else if(selected.type!=ReportWidgetType.TextNotes)
                {
                    var scenario=suite.scenarios.FirstOrDefault(x=>x.name==selected.scenarioName);
                    if(scenario==null){result.status="MISSING SCENARIO";result.error="No saved scenario with that name.";}
                    else
                    {
                        BalanceSnapshot snapshot;
                        if(selected.dataSource==ReportDataSource.SavedSnapshotResults)snapshot=saved;
                        else snapshot=BalanceSnapshotService.Capture(new BalanceSuite{name=suite.name,scenarios=new(){scenario}},"Report widget",null,cancelled);
                        if(snapshot==null){result.status="MISSING SNAPSHOT";result.error="Select a captured snapshot.";}
                        else
                        {
                            result.metric=snapshot.metrics.FirstOrDefault(x=>x.scenario==selected.scenarioName);
                            result.widget=snapshot.widgets.FirstOrDefault(x=>x.scenario==selected.scenarioName);
                            if(result.metric==null&&result.widget==null){result.status="MISSING RESULT";result.error="Scenario was not captured in the selected source.";}
                            else if(result.widget?.status=="FAILED"){result.status="FAILED";result.error=result.widget.error;}
                            else if(selected.dataSource==ReportDataSource.SavedSnapshotResults&&snapshot.fingerprint!=run.fingerprint)result.status="STALE";
                            if(result.status=="OK"&&!Compatible(selected.type,result)){result.status="INCOMPATIBLE SCENARIO";result.error="Choose a saved scenario with the data shape required by this widget type.";}
                        }
                    }
                }
                run.widgets.Add(result);progress?.Invoke(++index/(float)Math.Max(1,total));
            }
            return run;
        }
        static bool Compatible(ReportWidgetType type,ReportWidgetResult result)
        {
            var w=result.widget;return type switch
            {
                ReportWidgetType.SingleMetric or ReportWidgetType.MetricTable=>result.metric!=null,
                ReportWidgetType.CurveChart=>w?.kind==BalanceWidgetKind.Curve,
                ReportWidgetType.DistributionHistogram=>w?.kind==BalanceWidgetKind.Distribution,
                ReportWidgetType.MatchupMatrix=>w?.kind==BalanceWidgetKind.Matrix,
                ReportWidgetType.SensitivityRanking=>w?.kind==BalanceWidgetKind.Ranking&&w.source=="Sensitivity",
                ReportWidgetType.AffixRanking=>w?.kind==BalanceWidgetKind.Ranking&&w.source=="Affix",
                ReportWidgetType.PassiveTopN=>w?.kind==BalanceWidgetKind.Ranking&&w.source=="Passive",
                ReportWidgetType.LootUpgradeTable or ReportWidgetType.CurrencyTable=>w?.kind==BalanceWidgetKind.Ranking&&w.source=="Loot Progression",
                ReportWidgetType.CraftingSummary=>w?.kind==BalanceWidgetKind.Ranking&&w.source=="Crafting",
                ReportWidgetType.BreakpointList=>w?.kind==BalanceWidgetKind.Breakpoint,
                _=>true
            };
        }
        public static string ExportBundle(BalanceReportRun run)
        {
            if(run==null)throw new ArgumentNullException(nameof(run));
            string folder=Path.Combine(WorkbenchExports.Root,Safe(run.title)+"_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+"_"+Guid.NewGuid().ToString("N").Substring(0,6));string chartDir=Path.Combine(folder,"Charts"),dataDir=Path.Combine(folder,"Data");Directory.CreateDirectory(chartDir);Directory.CreateDirectory(dataDir);
            var md=new List<string>{"# "+run.title,"","Captured: "+run.timestampUtc,"Git: "+run.gitCommit,"Production fingerprint: "+run.fingerprint,"Status: "+(run.cancelled?"CANCELLED / PARTIAL":"COMPLETE"),"",run.designerNotes??""};
            int number=0;foreach(var section in run.widgets.GroupBy(x=>x.sectionOrder).OrderBy(x=>x.Key))
            {
                md.AddRange(new[]{"","## "+section.First().section,""});if(!string.IsNullOrWhiteSpace(section.First().sectionNotes))md.Add(section.First().sectionNotes);
                foreach(var entry in section)
                {
                    string stem=Safe(entry.title)+"_"+(++number).ToString("D2");md.AddRange(new[]{"","### "+entry.title,"","Source: "+entry.source+" · Scenario: "+entry.scenarioName+" · Status: "+entry.status,""});
                    if(!string.IsNullOrWhiteSpace(entry.notes))md.Add(entry.notes);
                    if(!string.IsNullOrWhiteSpace(entry.error)){md.Add("Warning: "+entry.error);continue;}
                    if(entry.type==ReportWidgetType.SnapshotRegressionSummary)
                    {
                        md.Add("| Scenario | Status | Delta |\n|---|---|---:|");md.AddRange(entry.scalarRegression.Select(x=>$"| {x.scenario} / {x.metric} | {x.status} | {x.delta:0.####} |"));md.AddRange(entry.widgetRegression.Select(x=>$"| {x.scenario} / {x.kind} | {x.status} | {x.maximumAbsoluteDelta:0.####} |"));
                        File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Scenario,Kind,Status,Delta"}.Concat(entry.scalarRegression.Select(x=>$"{Csv(x.scenario)},{Csv(x.metric)},{Csv(x.status)},{x.delta:R}")).Concat(entry.widgetRegression.Select(x=>$"{Csv(x.scenario)},{x.kind},{Csv(x.status)},{x.maximumAbsoluteDelta:R}")));
                        continue;
                    }
                    if(entry.metric!=null){md.Add($"| Metric | Value |\n|---|---:|\n| {entry.metric.metric} | {entry.metric.value:0.####} |");File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Metric,Value",$"{Csv(entry.metric.metric)},{entry.metric.value:R}"});}
                    var w=entry.widget;if(w==null)continue;
                    if(w.curve.Count>0)
                    {
                        var series=new CurveSeries{name=w.metric??entry.title,color=Color.cyan,points=w.curve.Select(x=>new CurvePoint{x=x.level,mean=x.value}).ToList()};string png=new BalanceWorkbenchChart().ExportPng(new[]{series},Path.Combine(Path.GetFileName(folder),"Charts",stem));
                        md.Add($"![{entry.title}](Charts/{Path.GetFileName(png)})");
                        File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Level,Value"}.Concat(w.curve.Select(x=>$"{x.level},{x.value:R}")));
                    }
                    else if(w.histogram.Count>0){md.Add($"Distribution: mean {w.distribution.mean:0.###}, median {w.distribution.p50:0.###}, P90 {w.distribution.p90:0.###}, P99 {w.distribution.p99:0.###}.");var series=new CurveSeries{name="Frequency",color=Color.yellow,points=w.histogram.Select(x=>new CurvePoint{x=(x.minimum+x.maximum)*.5,mean=x.count}).ToList()};string png=new BalanceWorkbenchChart().ExportPng(new[]{series},Path.Combine(Path.GetFileName(folder),"Charts",stem));md.Add($"![{entry.title} histogram](Charts/{Path.GetFileName(png)})");File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Minimum,Maximum,Count"}.Concat(w.histogram.Select(x=>$"{x.minimum:R},{x.maximum:R},{x.count}")));}
                    else if(w.matrix.Count>0){md.Add("| Row | Column | Value |\n|---|---|---:|");md.AddRange(w.matrix.Select(x=>$"| {x.row} | {x.column} | {x.value:0.####} |"));File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Row,Column,Value"}.Concat(w.matrix.Select(x=>$"{Csv(x.row)},{Csv(x.column)},{x.value:R}")));}
                    else if(w.ranking.Count>0){md.Add("| Rank | Name | Value |\n|---:|---|---:|");md.AddRange(w.ranking.Select((x,i)=>$"| {i+1} | {x.name} | {x.value:0.####} |"));File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Rank,Name,Value"}.Concat(w.ranking.Select((x,i)=>$"{i+1},{Csv(x.name)},{x.value:R}")));}
                    else if(w.breakpoints.Count>0){md.Add("| Crossing level | Before | At | Threshold |\n|---:|---:|---:|---:|");md.AddRange(w.breakpoints.Select(x=>$"| {x.level} | {x.before:0.####} | {x.at:0.####} | {x.threshold:0.####} |"));File.WriteAllLines(Path.Combine(dataDir,stem+".csv"),new[]{"Level,Before,At,Threshold"}.Concat(w.breakpoints.Select(x=>$"{x.level},{x.before:R},{x.at:R},{x.threshold:R}")));}
                }
            }
            string report=Path.Combine(folder,"Report.md");File.WriteAllLines(report,md);File.WriteAllText(Path.Combine(folder,"metadata.json"),JsonUtility.ToJson(run,true));AssetDatabase.Refresh();return report;
        }
    }
}
