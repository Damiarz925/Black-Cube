using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using UnityEditor;
using UnityEngine;

public static class Tooling5EditorSmokeRunner
{
    [MenuItem("Black-Cube/Validation/Tooling 5 Editor Workflow Smoke")]
    public static void Run()
    {
        try
        {
            var coverage=BalanceWorkbenchWindow.RunTooling5ControllerSmoke();
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/Tooling5EditorSmoke.txt",coverage);
            Debug.Log(string.Join("\n",coverage));
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}

namespace BlackCube.BalanceWorkbench
{
    public sealed partial class BalanceWorkbenchWindow
    {
        // Drives the same request/result fields consumed by all seven Editor tabs. GUI button clicks
        // remain manual-only; the log states this boundary explicitly.
        public static List<string> RunTooling5ControllerSmoke()
        {
            var lines=new List<string>{"TOOLING 5 EDITOR WORKFLOW SMOKE", "Coverage: EditorWindow opened and seven tab controllers/results exercised; native IMGUI mouse clicks not automated."};
            var window=GetWindow<BalanceWorkbenchWindow>("Balance Workbench");
            window.playerBuild=new PlayerBuildSnapshot{playerLevel=60,combatLevel=60,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Rare,itemLevel=60,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=40,baseMax=60,baseSpeed=1.2f,baseCrit=.05f}}};
            window.tab=Tab.Sensitivity;window.sensitivity=new SensitivityRequest{build=window.playerBuild.Clone(),stats=new(){StatTypes.AttackSpeed,StatTypes.CritChance}};window.sensitivityResult=SensitivityAnalyzer.Run(window.sensitivity);if(window.sensitivityResult.rows.Count!=2)throw new Exception("Sensitivity table empty.");lines.Add("PASS Sensitivity tab / assigned build / populated result table");
            window.tab=Tab.AffixAnalyzer;window.affixRequest=new AffixAnalysisRequest{build=window.playerBuild.Clone(),itemLevel=60,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword};window.affixResult=AffixAnalyzer.Run(window.affixRequest);if(window.affixResult.rows.Count==0)throw new Exception("Affix table empty.");lines.Add("PASS Affix tab / selected weapon slot / populated ranking");
            window.tab=Tab.LootProgression;window.lootRequest=new LootProgressionRequest{build=window.playerBuild.Clone(),combatLevel=60,kills=10,seed=65101,generateActualEnemyGear=false};window.firstUpgradeResult=LootProgressionAnalyzer.RunFirstUpgradeTrials(window.lootRequest,3,10);if(window.firstUpgradeResult.trials!=3)throw new Exception("Independent trials missing.");window.lootRequest.sourceMode=LootSourceMode.FullCombatLevelLoop;window.lootResult=LootProgressionAnalyzer.Run(window.lootRequest);if(window.lootResult.sources.Count==0||!window.lootResult.sources.Any(x=>x.stage==10&&x.boss))throw new Exception("World-stage mixture missing boss stage.");lines.Add("PASS Loot tab / independent trials / production world-stage mixture");
            window.tab=Tab.CraftingSimulator;var craftItem=window.playerBuild.equipment[0];window.craftingRequest=new CraftingSimulationRequest{item=craftItem,targets=new(){new CraftTarget{stat=StatTypes.CritChance}},policy=new(){new CraftPolicyStep{action=CraftingCurrencyType.RerollRareModifier}},trials=4,maxActions=2,seed=65102};window.craftingResult=CraftingSimulator.Run(window.craftingRequest);if(window.craftingResult.trials!=4||window.craftingResult.replay==null)throw new Exception("Craft trial/trace missing.");window.craftSearch=new CraftPolicySearchRequest{simulation=window.craftingRequest,allowedActions=window.craftingRequest.policy,maximumStates=2,candidateOutcomesPerAction=2,maxCraftActions=2};window.craftSearchResult=CraftingPolicySearcher.Search(window.craftSearch);if(window.craftSearchResult.evaluatedStates==0)throw new Exception("Craft policy search empty.");lines.Add("PASS Crafting tab / Monte Carlo / bounded search / trace");
            window.tab=Tab.BalanceSnapshots;window.balanceSuite=new BalanceSuite{scenarios=new(){new BalanceScenario{name="Smoked build",build=window.playerBuild.Clone(),metric="basic_dps"},new BalanceScenario{name="Smoked curve",build=window.playerBuild.Clone(),metric="basic_dps",widgetKind=BalanceWidgetKind.Curve,startLevel=59,endLevel=61,increment=1}}};window.beforeSnapshot=BalanceSnapshotService.Capture(window.balanceSuite,"Smoke before");window.afterSnapshot=BalanceSnapshotService.Capture(window.balanceSuite,"Smoke after");window.differences=BalanceSnapshotService.Compare(window.beforeSnapshot,window.afterSnapshot,.1);window.widgetDifferences=BalanceSnapshotWidgets.Compare(window.beforeSnapshot,window.afterSnapshot);if(window.widgetDifferences.Count!=1)throw new Exception("Widget snapshot comparison missing.");lines.Add("PASS Snapshot tab / capture / scalar and curve comparison");
            window.tab=Tab.BreakpointFinder;window.breakResults=BreakpointFinder.Find(x=>x,1,10,2,5,BreakpointOperator.GreaterOrEqual);var cases=new[]{new BreakpointBatchCase{name="A"},new BreakpointBatchCase{name="B"}};window.breakBatchResults=BreakpointFinder.FindBatch(cases,(c,l)=>c.name=="A"?l-5:l-7,1,10,2,0,BreakpointOperator.GreaterOrEqual);if(window.breakResults.Single().level!=5||window.breakBatchResults.Count!=2)throw new Exception("Breakpoint controller failed.");lines.Add("PASS Breakpoint tab / single and batch results");
            window.tab=Tab.BalanceReport;window.reportPreset=new BalanceReportPreset{title="Tooling5 Editor Smoke",sections=new(){new ReportSectionPreset{title="Progression",widgets=new(){new ReportWidgetPreset{title="Build DPS",scenarioName="Smoked build",type=ReportWidgetType.SingleMetric,dataSource=ReportDataSource.SavedSnapshotResults},new ReportWidgetPreset{title="DPS Curve",scenarioName="Smoked curve",type=ReportWidgetType.CurveChart,dataSource=ReportDataSource.SavedSnapshotResults}}}}};window.reportRun=BalanceReportService.Run(window.reportPreset,window.balanceSuite,window.beforeSnapshot);window.reportPath=BalanceReportService.ExportBundle(window.reportRun);if(!File.Exists(window.reportPath))throw new Exception("Report bundle missing.");lines.Add("PASS Report tab / widget preset / run / Markdown-CSV-PNG export");
            lines.Add("MANUAL REMAINING: verify IMGUI mouse selection, scroll, button operation, and visible chart/table layout in a foreground Unity Editor session.");
            window.Repaint();return lines;
        }
    }
}
