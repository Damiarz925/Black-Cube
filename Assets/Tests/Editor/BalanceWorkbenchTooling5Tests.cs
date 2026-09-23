using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class BalanceWorkbenchTooling5Tests
{
    static PlayerBuildSnapshot Build()=>new(){playerLevel=60,combatLevel=60,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Rare,itemLevel=60,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=40,baseMax=60,baseSpeed=1.2f,baseCrit=.05f}}};
    static GearSnapshot EndgameItem()=>new(){slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Legendary,originRarity=LootManager.GearRarity.Legendary,itemLevel=100,currentPotential=14,maximumPotential=14,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=40,baseMax=60,baseSpeed=1.2f,baseCrit=.05f,mods=new(){new RolledModSnapshot{stat=StatTypes.Life,tier=1,value=10,implicitMod=true},new RolledModSnapshot{stat=StatTypes.GenericDmg,tier=1,value=.1f}}};

    [Test] public void BreakpointFinder_FindsExactIntegerAndAllNonMonotonicCrossings()
    {
        var first=BreakpointFinder.Find(level=>level,1,100,10,37,BreakpointOperator.GreaterOrEqual);
        Assert.That(first.Single().level,Is.EqualTo(37));
        var all=BreakpointFinder.Find(level=>Math.Sin(level),1,30,7,0,BreakpointOperator.CrossUp,true);
        Assert.That(all.Count,Is.GreaterThan(1));
        Assert.That(all.All(x=>x.before<=0&&x.at>0),Is.True);
        var ci=BreakpointFinder.WilsonInterval(.5,100);
        Assert.That(ci.low,Is.LessThan(.5));Assert.That(ci.high,Is.GreaterThan(.5));
    }

    [Test] public void BatchBreakpoint_RecordsCrossingsAndExplicitNoCrossing()
    {
        var cases=new[]{new BreakpointBatchCase{name="A"},new BreakpointBatchCase{name="B"},new BreakpointBatchCase{name="C"}};
        var results=BreakpointFinder.FindBatch(cases,(c,level)=>c.name=="A"?level-30:c.name=="B"?level-50:-1,1,60,10,0,BreakpointOperator.GreaterOrEqual);
        Assert.That(results.Single(x=>x.scenario=="A").firstCrossing,Is.EqualTo(30));
        Assert.That(results.Single(x=>x.scenario=="B").firstCrossing,Is.EqualTo(50));
        Assert.That(results.Single(x=>x.scenario=="C").crossingFound,Is.False);
    }

    [Test] public void SampledBreakpoint_RefinesAmbiguousWilsonIntervalWithinBudget()
    {
        int maxObserved=0;var cases=new[]{new BreakpointBatchCase{name="sampled"}};
        var budget=new BreakpointSampleBudget{initial=20,refinement=20,maximum=60};
        var rows=BreakpointFinder.FindBatch(cases,(c,l)=>l<5?.8:.6,1,8,2,.7,BreakpointOperator.Less,budget,(c,l,n)=>{maxObserved=Math.Max(n,maxObserved);return l<5?.8:.6;});
        Assert.That(rows.Single().firstCrossing,Is.EqualTo(5));
        Assert.That(rows.Single().samples,Is.LessThanOrEqualTo(60));
        Assert.That(maxObserved,Is.GreaterThanOrEqualTo(20));
        Assert.That(rows.Single().confidenceLow,Is.LessThanOrEqualTo(rows.Single().at));
    }

    [Test] public void CraftPolicySearch_RespectsConfiguredStateCap()
    {
        var request=new CraftPolicySearchRequest{simulation=new CraftingSimulationRequest{item=Build().equipment.Single(),targets=new(){new CraftTarget{stat=StatTypes.PhysDmg}},requiredMatches=1},allowedActions=new(){new CraftPolicyStep{action=CraftingCurrencyType.RerollRareModifier},new CraftPolicyStep{action=CraftingCurrencyType.RemoveRareModifier}},beamWidth=2,maxCraftActions=3,candidateOutcomesPerAction=2,maximumStates=4};
        var result=CraftingPolicySearcher.Search(request);
        Assert.That(result.evaluatedStates,Is.LessThanOrEqualTo(4));
        Assert.That(result.label,Does.Contain("not proven optimal"));
    }

    [Test] public void CraftingSimulator_BossInfusionMatchesProductionMutationWithSameSeed()
    {
        var challenge=WorldContentCatalog.Reference.challengeEncounters[0];var snapshot=EndgameItem();const long seed=17403;
        var go=new UnityEngine.GameObject("Direct boss infusion");try
        {
            var direct=snapshot.Materialize(go.transform,"direct");var target=direct.rolledMods.Single(x=>x.statType==StatTypes.GenericDmg&&!x.lockedOriginal);
            bool expected=BossSpecialCrafting.TryReplace(direct,target,WorldContentCatalog.Reference.ChallengeSpecialPool(challenge.specialAffixPoolId),360,new SeededSimulationRandomSource(seed));
            var request=new CraftingSimulationRequest{item=snapshot,combatLevel=360,trials=1,maxActions=1,seed=seed,targets=new(){new CraftTarget{stat=StatTypes.PhysDmg}},policy=new(){new CraftPolicyStep{operation=CraftOperation.BossInfusion,targetStat=StatTypes.GenericDmg,challengeId=challenge.stableContentId}},resources=new(){new SimulatedResourceQuantity{id=challenge.rewardResourceId,startingQuantity=1}}};
            var actual=CraftingSimulator.Run(request);Assert.That(expected,Is.True);Assert.That(actual.replay.actions,Is.EqualTo(1));
            Assert.That(actual.replay.finalItem.mods.Single(x=>x.bossSpecial).id,Is.EqualTo(GearSnapshot.Capture(direct).mods.Single(x=>x.bossSpecial).id));
            Assert.That(actual.replay.finalItem.currentPotential,Is.EqualTo(direct.CurrentCraftingPotential));
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void CraftingSimulator_EmpowermentMatchesProductionMutationWithSameSeed()
    {
        var snapshot=EndgameItem();const long seed=39114;
        using var session=new WorkbenchSession();var go=new UnityEngine.GameObject("Direct empowerment");try
        {
            var direct=snapshot.Materialize(go.transform,"direct");var target=direct.rolledMods.Single(x=>x.statType==StatTypes.GenericDmg&&!x.lockedOriginal);
            var random=new SeededSimulationRandomSource(seed);
            bool expected=EmpowermentCrafting.TryApply(direct,target,360,random.Value(),random.Value(),session.Roller.Database);
            var request=new CraftingSimulationRequest{item=snapshot,combatLevel=360,trials=1,maxActions=1,seed=seed,targets=new(){new CraftTarget{stat=StatTypes.PhysDmg}},policy=new(){new CraftPolicyStep{operation=CraftOperation.Empowerment,targetStat=StatTypes.GenericDmg}},resources=new(){new SimulatedResourceQuantity{id=CraftingCurrencyType.EmpowermentCatalyst.ToString(),startingQuantity=1}}};
            var actual=CraftingSimulator.Run(request);Assert.That(expected,Is.True);Assert.That(actual.replay.actions,Is.EqualTo(1));
            Assert.That(actual.replay.finalItem.mods.Single(x=>x.empowered).value,Is.EqualTo(GearSnapshot.Capture(direct).mods.Single(x=>x.empowered).value));
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void CraftingSimulator_ImplicitReforgeMatchesProductionMutationWithSameSeed()
    {
        var snapshot=EndgameItem();const long seed=51008;
        using var session=new WorkbenchSession();var go=new UnityEngine.GameObject("Direct implicit reforge");try
        {
            var direct=snapshot.Materialize(go.transform,"direct");
            bool expected=EndgameCraftingService.TryReforgeImplicitCore(direct,session.Roller,new SeededSimulationRandomSource(seed));
            var request=new CraftingSimulationRequest{item=snapshot,combatLevel=360,trials=1,maxActions=1,seed=seed,targets=new(){new CraftTarget{stat=StatTypes.PhysDmg}},policy=new(){new CraftPolicyStep{operation=CraftOperation.ImplicitReforge}},resources=new(){new SimulatedResourceQuantity{id=EndgameResourceIds.ImplicitReforger,startingQuantity=1}}};
            var actual=CraftingSimulator.Run(request);Assert.That(expected,Is.True);Assert.That(actual.replay.actions,Is.EqualTo(1));
            var final=actual.replay.finalItem.mods.Single(x=>x.implicitMod);var baseline=GearSnapshot.Capture(direct).mods.Single(x=>x.implicitMod);
            Assert.That(final.stat,Is.EqualTo(baseline.stat));Assert.That(final.tier,Is.EqualTo(baseline.tier));Assert.That(final.value,Is.EqualTo(baseline.value));
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void CraftingSimulator_EnforcesEndgameResourcesAndProgression()
    {
        var snapshot=EndgameItem();var request=new CraftingSimulationRequest{item=snapshot,combatLevel=119,trials=1,maxActions=1,targets=new(){new CraftTarget{stat=StatTypes.PhysDmg}},policy=new(){new CraftPolicyStep{operation=CraftOperation.Empowerment,targetStat=StatTypes.GenericDmg}},resources=new(){new SimulatedResourceQuantity{id=CraftingCurrencyType.EmpowermentCatalyst.ToString(),startingQuantity=0}}};
        var blocked=CraftingSimulator.Run(request);Assert.That(blocked.budgetFailures,Is.EqualTo(1));Assert.That(blocked.replay.actions,Is.Zero);
        request.resources[0].startingQuantity=1;var gated=CraftingSimulator.Run(request);Assert.That(gated.progressionFailures,Is.EqualTo(1));Assert.That(gated.replay.actions,Is.Zero);
    }

    [Test] public void CraftingTarget_ImplicitFamilyTierAndRollAreEnforced()
    {
        var request=new CraftingSimulationRequest{item=EndgameItem(),targets=new(),requiredMatches=0,targetImplicitStat=StatTypes.Life.ToString(),maximumImplicitTier=1,minimumImplicitRoll=10,trials=1,maxActions=1,policy=new(){new CraftPolicyStep{action=CraftingCurrencyType.RerollRareModifier}}};
        Assert.That(CraftingSimulator.Run(request).successes,Is.EqualTo(1));
        request.minimumImplicitRoll=11;
        Assert.That(CraftingSimulator.Run(request).successes,Is.EqualTo(0));
    }

    [Test] public void Sensitivity_UsesProductionEvaluatorAndReproduces()
    {
        var request=new SensitivityRequest{build=Build(),stats=new(){StatTypes.AttackSpeed,StatTypes.CritChance},amount=.10f};
        var a=SensitivityAnalyzer.Run(request);var b=SensitivityAnalyzer.Run(request);
        Assert.That(a.rows.Count,Is.EqualTo(2));
        Assert.That(a.rows.Zip(b.rows,(x,y)=>x.stat==y.stat&&x.modified==y.modified).All(x=>x),Is.True);
        Assert.That(a.rows.Single(x=>x.stat==StatTypes.AttackSpeed).modified,Is.GreaterThan(a.rows.Single(x=>x.stat==StatTypes.AttackSpeed).baseline));
    }

    [Test] public void SensitivityPair_ReportsIndependentInteraction()
    {
        var request=new SensitivityRequest{build=Build(),stats=new(){StatTypes.CritChance,StatTypes.CritMult},amount=.10f};
        var pair=SensitivityAnalyzer.Pairs(request,request.stats).Single();
        Assert.That(pair.interaction,Is.EqualTo(pair.observed-pair.gainA-pair.gainB).Within(1e-9));
    }

    [Test] public void AffixAnalyzer_OnlyExposesLegalItemLevels()
    {
        var request=new AffixAnalysisRequest{build=Build(),slot=LootManager.GearType.Weapons,itemLevel=30,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,allTiers=true};
        var result=AffixAnalyzer.Run(request);
        Assert.That(result.rows,Is.Not.Empty);
        Assert.That(result.rows.All(x=>x.itemLevel<=30),Is.True);
    }

    [Test] public void LootProgression_ReplaysExactSeed()
    {
        var request=new LootProgressionRequest{build=Build(),kills=12,combatLevel=60,seed=91002};
        var a=LootProgressionAnalyzer.Run(request);var b=LootProgressionAnalyzer.Run(request);
        Assert.That(a.kills,Is.EqualTo(12));Assert.That(a.drops,Is.EqualTo(b.drops));
        Assert.That(a.upgrades.Select(x=>x.item.Description),Is.EqualTo(b.upgrades.Select(x=>x.item.Description)));
        Assert.That(a.sources.Sum(x=>x.slots.Sum(s=>s.count)),Is.EqualTo(a.drops));
        foreach(var currency in a.currency)Assert.That(a.sources.Sum(x=>x.currency.Where(c=>c.type==currency.type).Sum(c=>c.amount)),Is.EqualTo(currency.amount));
    }

    [Test] public void FirstUpgradeDistribution_AccountsForCensoringWithoutTreatingItAsSuccess()
    {
        var distribution=new FirstUpgradeDistribution{maxKills=10,observations=new()
        {new FirstUpgradeTrial{trial=1,kills=3},new FirstUpgradeTrial{trial=2,kills=7},new FirstUpgradeTrial{trial=3,kills=10,censored=true}}};
        LootProgressionAnalyzer.SummarizeFirstUpgradeObservations(distribution,new[]{3,7});
        Assert.That(distribution.successes,Is.EqualTo(2));Assert.That(distribution.censored,Is.EqualTo(1));
        Assert.That(distribution.successfulKills.p50,Is.EqualTo(5));
        Assert.That(distribution.curve.Single(x=>x.kills==3).probabilityFound,Is.EqualTo(1d/3).Within(1e-9));
        Assert.That(distribution.curve.Single(x=>x.kills==7).probabilityFound,Is.EqualTo(2d/3).Within(1e-9));
    }

    [Test] public void ProgressiveIntervals_UseIndependentTrialSeedsAndReportReachCounts()
    {
        var request=new LootProgressionRequest{build=Build(),kills=8,combatLevel=60,seed=7741};
        var first=LootProgressionAnalyzer.RunProgressiveIntervalTrials(request,2,8);
        var repeat=LootProgressionAnalyzer.RunProgressiveIntervalTrials(request,2,8);
        Assert.That(first.trials,Is.EqualTo(2));Assert.That(first.firstReached,Is.GreaterThanOrEqualTo(first.secondReached));
        Assert.That(first.secondReached,Is.GreaterThanOrEqualTo(first.thirdReached));
        Assert.That(first.first.mean,Is.EqualTo(repeat.first.mean));Assert.That(first.laterCount,Is.EqualTo(repeat.laterCount));
    }

    [Test] public void WorldLootSource_UsesTheProductionEncounterResolver()
    {
        var request=new LootProgressionRequest{sourceMode=LootSourceMode.FullCombatLevelLoop,combatLevel=80,seed=77};
        for(int kill=1;kill<=20;kill++)
        {
            var actual=LootProgressionAnalyzer.ResolveWorldSource(request,kill,77);
            var expected=WorldProgression.Resolve(80,1+(kill-1)%10,WorldContentCatalog.Reference,77+(kill-1)/10);
            Assert.That(actual.Encounter?.enemyArchetypeId,Is.EqualTo(expected.Encounter?.enemyArchetypeId));
            Assert.That(actual.Encounter?.bossId,Is.EqualTo(expected.Encounter?.bossId));
        }
    }

    [Test] public void WorldLootBossQuality_UsesTheAuthoredBossPrefabBuild()
    {
        var world=WorldProgression.Resolve(80,10,WorldContentCatalog.Reference,77);
        var result=ProductionBalanceAdapters.RunEnemies(new EnemyLabRequest{useBoss=true,bossId=world.Encounter.bossId,level=80,sampleCount=1,rarity=EnemyAI.EnemyRarity.Rare,seed=775});
        Assert.That(result.samples,Has.Count.EqualTo(1));Assert.That(result.samples[0].slotCount,Is.GreaterThan(0));Assert.That(result.samples[0].gearScore,Is.GreaterThan(0));
    }

    [Test] public void Snapshot_UnchangedSuiteReproducesAndControlledChangeShowsDelta()
    {
        var build=Build();var suite=new BalanceSuite{scenarios=new(){new BalanceScenario{name="Warrior",build=build,metric="basic_dps"}}};
        var before=BalanceSnapshotService.Capture(suite,"Before");var repeat=BalanceSnapshotService.Capture(suite,"Repeat");
        Assert.That(BalanceSnapshotService.Compare(before,repeat,.1).Single().delta,Is.Zero);
        suite.scenarios[0].build.analysisDeltas.Add(new AnalysisStatDelta{stat=StatTypes.AttackSpeed,amount=.1f});
        var after=BalanceSnapshotService.Capture(suite,"After");
        Assert.That(BalanceSnapshotService.Compare(before,after,.1).Single().delta,Is.GreaterThan(0));
    }

    [Test] public void SnapshotCurve_TracksLargestChangedLevel()
    {
        var a=new BalanceSnapshot{fingerprint="fixture",widgets=new(){new SnapshotWidgetData{scenario="curve",kind=BalanceWidgetKind.Curve,fingerprint="fixture",curve=new(){new BreakpointPoint{level=10,value=2},new BreakpointPoint{level=20,value=4}}}}};
        var b=new BalanceSnapshot{fingerprint="fixture",widgets=new(){new SnapshotWidgetData{scenario="curve",kind=BalanceWidgetKind.Curve,fingerprint="fixture",curve=new(){new BreakpointPoint{level=10,value=2},new BreakpointPoint{level=20,value=7}}}}};
        var diff=BalanceSnapshotWidgets.Compare(a,b).Single();Assert.That(diff.status,Is.EqualTo("CHANGED"));Assert.That(diff.largestChangeLevel,Is.EqualTo(20));Assert.That(diff.maximumAbsoluteDelta,Is.EqualTo(3));
    }

    [Test] public void SnapshotMatrix_ChangesOnlyOneCell()
    {
        var a=new BalanceSnapshot{fingerprint="fixture",widgets=new(){new SnapshotWidgetData{scenario="matrix",kind=BalanceWidgetKind.Matrix,fingerprint="fixture",matrix=new(){new SnapshotMatrixCell{row="A",column="X",value=.5},new SnapshotMatrixCell{row="A",column="Y",value=.6},new SnapshotMatrixCell{row="B",column="X",value=.7},new SnapshotMatrixCell{row="B",column="Y",value=.8}}}}};
        var b=UnityEngine.JsonUtility.FromJson<BalanceSnapshot>(UnityEngine.JsonUtility.ToJson(a));b.widgets[0].matrix[2].value=.4;
        var diff=BalanceSnapshotWidgets.Compare(a,b).Single();Assert.That(diff.matrix.Count(x=>Math.Abs(x.delta)>1e-9),Is.EqualTo(1));Assert.That(diff.matrix.Single(x=>Math.Abs(x.delta)>1e-9).row,Is.EqualTo("B"));
    }

    [Test] public void SnapshotMatrix_IncompleteComparisonIsFailedNotUnchanged()
    {
        var a=new BalanceSnapshot{fingerprint="fixture",widgets=new(){new SnapshotWidgetData{scenario="matrix",kind=BalanceWidgetKind.Matrix,fingerprint="fixture",matrix=new(){new SnapshotMatrixCell{row="A",column="X",value=.5},new SnapshotMatrixCell{row="B",column="X",value=.7}}}}};
        var b=UnityEngine.JsonUtility.FromJson<BalanceSnapshot>(UnityEngine.JsonUtility.ToJson(a));b.widgets[0].matrix.RemoveAt(1);
        Assert.That(BalanceSnapshotWidgets.Compare(a,b).Single().status,Is.EqualTo("FAILED"));
    }

    [Test] public void ReportBundle_WritesMarkdownCsvAndMetadata()
    {
        var run=new BalanceReportRun{title="Fixture Report",timestampUtc="fixture",fingerprint="fixture",gitCommit="fixture",widgets=new(){new ReportWidgetResult{section="Scaling",title="Curve",scenarioName="curve",source="SavedSnapshotResults",status="OK",type=ReportWidgetType.CurveChart,widget=new SnapshotWidgetData{metric="dps",kind=BalanceWidgetKind.Curve,curve=new(){new BreakpointPoint{level=1,value=2},new BreakpointPoint{level=2,value=3}}}}}};
        var path=BalanceReportService.ExportBundle(run);var folder=System.IO.Path.GetDirectoryName(path);
        Assert.That(System.IO.File.Exists(path),Is.True);Assert.That(System.IO.File.Exists(System.IO.Path.Combine(folder,"metadata.json")),Is.True);Assert.That(System.IO.Directory.GetFiles(System.IO.Path.Combine(folder,"Data"),"*.csv").Length,Is.GreaterThan(0));Assert.That(System.IO.Directory.GetFiles(System.IO.Path.Combine(folder,"Charts"),"*.png").Length,Is.GreaterThan(0));
    }

    [Test] public void ReportRegressionWidget_ComparesBeforeSnapshotAndExportsTable()
    {
        var suite=new BalanceSuite{scenarios=new(){new BalanceScenario{name="Warrior",build=Build(),metric="basic_dps"}}};
        var before=BalanceSnapshotService.Capture(suite,"Before");
        var preset=new BalanceReportPreset{sections=new(){new ReportSectionPreset{title="Regression",widgets=new(){new ReportWidgetPreset{title="Before versus current",type=ReportWidgetType.SnapshotRegressionSummary}}}}};
        var report=BalanceReportService.Run(preset,suite,before);
        Assert.That(report.widgets.Single().scalarRegression,Has.Count.EqualTo(1));
        Assert.That(report.widgets.Single().scalarRegression.Single().status,Is.EqualTo("UNCHANGED"));
        var path=BalanceReportService.ExportBundle(report);var folder=System.IO.Path.GetDirectoryName(path);
        Assert.That(System.IO.File.ReadAllText(path),Does.Contain("Before versus current"));
        Assert.That(System.IO.Directory.GetFiles(System.IO.Path.Combine(folder,"Data"),"*.csv"),Has.Length.EqualTo(1));
    }
}
