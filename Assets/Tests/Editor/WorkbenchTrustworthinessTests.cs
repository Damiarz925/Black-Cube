using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using BlackCube.CombatSimulation;
using NUnit.Framework;
using UnityEngine;

public sealed class WorkbenchTrustworthinessTests
{
    [TestCase(0f,0,0)]
    [TestCase(.5f,0,1)]
    [TestCase(1f,1,1)]
    [TestCase(1.5f,1,2)]
    [TestCase(2.51f,2,3)]
    [TestCase(3f,3,3)]
    [TestCase(3.5f,3,4)]
    public void AilmentOverflowUsesGuaranteedPlusFractionalApplications(
        float chance,int minimum,int maximum)
    {
        Assert.That(CombatDeterministicRules.RollOverflowApplications(chance,()=>.999f),
            Is.EqualTo(minimum));
        Assert.That(CombatDeterministicRules.RollOverflowApplications(chance,()=>0f),
            Is.EqualTo(maximum));
    }

    [TestCase(.5f)]
    [TestCase(1f)]
    [TestCase(1.5f)]
    [TestCase(2.5f)]
    [TestCase(2.51f)]
    [TestCase(3.5f)]
    public void SeededOverflowApplicationMeanMatchesAnalyticalChance(float chance)
    {
        var rng=new SeededSimulationRandomSource(61001);
        const int samples=100000;
        long applications=0;
        for(int i=0;i<samples;i++)
            applications+=CombatDeterministicRules.RollOverflowApplications(chance,rng.Value);
        Assert.That(applications/(double)samples,Is.EqualTo(chance).Within(.01));
    }

    [Test]
    public void AnalyticalAilmentChanceRetainsOverflowRatherThanClamping()
    {
        var build=Fixture(20);
        build.analysisDeltas.Add(new AnalysisStatDelta{stat=StatTypes.BleedChance,amount=251f});
        build.analysisDeltas.Add(new AnalysisStatDelta{stat=StatTypes.PoisonChance,amount=150f});
        build.analysisDeltas.Add(new AnalysisStatDelta{stat=StatTypes.IgniteChance,amount=350f});
        var metrics=PlayerBuildEvaluator.Evaluate(build);
        Assert.That(metrics.bleedChance,Is.EqualTo(2.51).Within(.0001));
        Assert.That(metrics.poisonChance,Is.EqualTo(1.5).Within(.0001));
        Assert.That(metrics.igniteChance,Is.EqualTo(3.5).Within(.0001));
        Assert.That(metrics.bleedDps,Is.GreaterThan(0));
    }

    [Test]
    public void ResistancePressureUsesLegalProductionSuffixesAndExcludesVoidFromAllElemental()
    {
        using var session=new WorkbenchSession();
        var report=ResistancePressureAnalyzer.Analyze(session.Roller.Database);
        Assert.That(report.reachesTarget,Is.True);
        Assert.That(report.t1Options.Select(x=>x.stat).Distinct(),
            Does.Contain(StatTypes.VoidRes));
        Assert.That(report.referenceInvestment.GroupBy(x=>x.slot)
            .All(group=>group.Count()<=AffixPolicy.MaximumOnSide(LootManager.GearRarity.Rare)),
            Is.True);
        Assert.That(report.referenceInvestment.GroupBy(x=>new{x.slot,x.stat})
            .All(group=>group.Count()==1),Is.True);
        Assert.That(report.referenceInvestment.Any(x=>x.stat==StatTypes.VoidRes),Is.True,
            "All elemental suffixes cannot cover Void.");
        Assert.That(report.earlierTiers.All(x=>x.fire<=report.target&&
            x.cold<=report.target&&x.lightning<=report.target&&
            x.voidResistance<=report.target),Is.True);
    }

    [Test]
    public void BenchmarkComparisonRequiresBothResultHashAndEvaluationCount()
    {
        var before=new WorkbenchPerformanceRow{resultHash="same",evaluations=100,elapsedMs=100};
        var after=new WorkbenchPerformanceRow{resultHash="same",evaluations=100,elapsedMs=50};
        Assert.That(WorkbenchBenchmarkComparison.Equivalent(before,after),Is.True);
        after.resultHash="different";
        Assert.That(WorkbenchBenchmarkComparison.Equivalent(before,after),Is.False);
        after.resultHash="same";after.evaluations=99;
        Assert.That(WorkbenchBenchmarkComparison.Equivalent(before,after),Is.False);
    }

    [Test]
    public void PlayerEvaluationCacheKeysExactBuildAndReturnsDetachedMetrics()
    {
        var build=Fixture(33);build.seed=887766;
        long beforeMiss=PlayerBuildEvaluator.CacheMisses;
        var first=PlayerBuildEvaluator.Evaluate(build);
        Assert.That(PlayerBuildEvaluator.CacheMisses,Is.GreaterThan(beforeMiss));
        long beforeHit=PlayerBuildEvaluator.CacheHits;
        var repeat=PlayerBuildEvaluator.Evaluate(build);
        Assert.That(PlayerBuildEvaluator.CacheHits,Is.GreaterThan(beforeHit));
        repeat.basicDps=-123;
        Assert.That(PlayerBuildEvaluator.Evaluate(build).basicDps,
            Is.EqualTo(first.basicDps));
        build.playerLevel++;
        beforeMiss=PlayerBuildEvaluator.CacheMisses;
        PlayerBuildEvaluator.Evaluate(build);
        Assert.That(PlayerBuildEvaluator.CacheMisses,Is.GreaterThan(beforeMiss));
        build.passiveStableIds.Add(PassiveTreeDefinition.Node(
            PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,1)).StableId);
        beforeMiss=PlayerBuildEvaluator.CacheMisses;
        PlayerBuildEvaluator.Evaluate(build);
        Assert.That(PlayerBuildEvaluator.CacheMisses,Is.GreaterThan(beforeMiss));
    }

    [Test]
    public void CombatTransferPreservesSelectedScenarioBuildAndPolicyContext()
    {
        var build=Fixture(25);build.playerLevel=30;build.combatLevel=70;
        build.passiveStableIds.Add(PassiveTreeDefinition.Node(
            PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,1)).StableId);
        var point=Point(build,new OptimizationObjective(),"Mid");
        var inspected=ScenarioResultIdentity.Inspect(point);
        var request=new CombatLabRequest();
        CombatLabTransfer.SetPlayer(request,inspected.build,inspected.skillPolicy);
        Assert.That(request.player.playerLevel,Is.EqualTo(30));
        Assert.That(request.player.combatLevel,Is.EqualTo(70));
        Assert.That(ScenarioResultIdentity.GearHash(request.player),Is.EqualTo(point.gearHash));
        Assert.That(ScenarioResultIdentity.PassiveHash(request.player),Is.EqualTo(point.passiveHash));
        Assert.That(request.analyticalSelectedSkillPolicy,Is.EqualTo(inspected.skillPolicy));
        inspected.build.equipment[0].baseMin=999;
        Assert.That(ScenarioResultIdentity.GearHash(request.player),Is.EqualTo(point.gearHash));
    }

    [Test]
    public void CombatLevelPoliciesHaveExplicitMappings()
    {
        int[] levels={10,20,30,40,50};
        CollectionAssert.AreEqual(levels,levels.Select(x=>ScenarioLevelPolicy.CombatLevel(
            CombatLevelSweepPolicy.MatchPlayerLevel,x,10,50,0)).ToArray());
        CollectionAssert.AreEqual(new[]{50,50,50,50,50},levels.Select(x=>
            ScenarioLevelPolicy.CombatLevel(CombatLevelSweepPolicy.Fixed,x,10,50,0)).ToArray());
        CollectionAssert.AreEqual(new[]{15,25,35,45,55},levels.Select(x=>
            ScenarioLevelPolicy.CombatLevel(CombatLevelSweepPolicy.OffsetFromPlayerLevel,x,10,50,5)).ToArray());
        CollectionAssert.AreEqual(new[]{50,60,70,80,90},levels.Select(x=>
            ScenarioLevelPolicy.CombatLevel(CombatLevelSweepPolicy.AdvanceFromStart,x,10,50,0)).ToArray());
    }

    [Test]
    public void ScenarioInspectionReevaluatesItsOwnBuildAndObjective()
    {
        var objective=new OptimizationObjective{primary="basic_dps",secondary="life"};
        var weak=Fixture(10);
        var strong=Fixture(90);
        string passive=PassiveTreeDefinition.Node(
            PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,1)).StableId;
        strong.passiveStableIds.Add(passive);
        var a=Point(weak,objective,"Low");
        var b=Point(strong,objective,"Mid");
        var first=ScenarioResultIdentity.Inspect(a);
        var second=ScenarioResultIdentity.Inspect(b);
        Assert.That(second.metrics.basicDps,Is.GreaterThan(first.metrics.basicDps));
        Assert.That(first.contributions.Single(x=>x.source=="Passive Tree").primary,
            Is.EqualTo(0).Within(1e-8));
        Assert.That(second.contributions.Single(x=>x.source=="Passive Tree").primary,
            Is.GreaterThan(0));
        Assert.That(second.contributions.Single(x=>x.source=="Gear").primary,
            Is.Not.EqualTo(first.contributions.Single(x=>x.source=="Gear").primary));
        Assert.That(ScenarioResultIdentity.Inspect(a).resultId,Is.EqualTo(first.resultId));
        var restored=ScenarioResultIdentity.Inspect(a);
        Assert.That(restored.gearHash,Is.EqualTo(first.gearHash));
        Assert.That(restored.passiveHash,Is.EqualTo(first.passiveHash));
        Assert.That(restored.evaluationHash,Is.EqualTo(first.evaluationHash));
        Assert.That(restored.metrics.basicDps,Is.EqualTo(first.metrics.basicDps));
        Assert.That(restored.contributions.Single(x=>x.source=="Gear").primary,
            Is.EqualTo(first.contributions.Single(x=>x.source=="Gear").primary));
        second.build.equipment[0].baseMin=9999;
        Assert.That(ScenarioResultIdentity.Inspect(b).resultId,Is.EqualTo(b.resultId),
            "An inspected copy must not mutate its stored point.");
    }

    [Test]
    public void ScenarioInspectionRejectsTamperedGearAndMetrics()
    {
        var point=Point(Fixture(20),new OptimizationObjective(),"Low");
        point.build.equipment[0].baseMax+=10;
        Assert.Throws<InvalidOperationException>(()=>ScenarioResultIdentity.Inspect(point));
        point=Point(Fixture(20),new OptimizationObjective(),"Low");
        point.metrics.basicDps+=1;
        Assert.Throws<InvalidOperationException>(()=>ScenarioResultIdentity.Inspect(point));
    }

    [Test]
    public void ScenarioInspectionUsesCapturedObjectiveAfterCallerChangesItsOwnObjective()
    {
        var build=Fixture(50);
        var objective=new OptimizationObjective{primary="basic_dps",secondary="life"};
        var point=Point(build,objective,"Mid");
        objective.primary="life";
        var first=ScenarioResultIdentity.Inspect(point);
        Assert.That(first.objective.primary,Is.EqualTo("basic_dps"));
        double damageContribution=first.contributions.Single(x=>x.source=="Gear").primary;
        var lifeObjective=new OptimizationObjective{primary="life",secondary="basic_dps"};
        var lifePoint=Point(build,lifeObjective,"Mid");
        double lifeContribution=ScenarioResultIdentity.Inspect(lifePoint)
            .contributions.Single(x=>x.source=="Gear").primary;
        Assert.That(damageContribution,Is.Not.EqualTo(lifeContribution));
        Assert.That(ScenarioResultIdentity.Inspect(point).objective.primary,
            Is.EqualTo("basic_dps"));
    }

    [Test]
    public void ScenarioSweepStoresAtLevelSnapshotsWithoutAliasingInputs()
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>(
            "Assets/Balance/Profiles/SO_PlayerGearProfile_Low.asset");
        Assert.That(source,Is.Not.Null);
        var profile=UnityEngine.Object.Instantiate(source);
        try
        {
            profile.name=source.name;
            profile.candidatesPerSlot=1;
            profile.candidateRetention=1;
            profile.gearsetBeamWidth=1;
            var template=Fixture(10);template.playerLevel=50;template.combatLevel=50;
            var objective=new OptimizationObjective{primary="basic_dps",secondary="life"};
            var sweep=PlayerScenarioSweep.Run(template,new[]{profile},objective,10,20,10,
                false,true,true,1);
            CollectionAssert.AreEqual(new[]{10,20},sweep.points.Select(x=>x.playerLevel).ToArray());
            CollectionAssert.AreEqual(new[]{10,20},sweep.points.Select(x=>x.combatLevel).ToArray());
            Assert.That(sweep.points.All(x=>x.samplesPerLevel==1&&x.resultId!=null),Is.True);
            string firstHash=sweep.points[0].buildHash;
            template.combatLevel=300;
            objective.primary="life";
            Assert.That(sweep.points[0].buildHash,Is.EqualTo(firstHash));
            Assert.That(ScenarioResultIdentity.Inspect(sweep.points[0]).build.combatLevel,
                Is.EqualTo(10));
        }
        finally{UnityEngine.Object.DestroyImmediate(profile);}
    }

    [Test]
    public void ProgressiveWithoutFreeRespecCarriesEarlierPassiveAllocation()
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>(
            "Assets/Balance/Profiles/SO_PlayerGearProfile_Low.asset");
        var profile=UnityEngine.Object.Instantiate(source);
        try
        {
            profile.candidatesPerSlot=1;profile.candidateRetention=1;profile.gearsetBeamWidth=1;
            var sweep=PlayerScenarioSweep.Run(Fixture(10),new[]{profile},
                new OptimizationObjective{primary="basic_dps",secondary="life"},
                1,2,1,true,true,false,1);
            Assert.That(sweep.points,Has.Count.EqualTo(2));
            Assert.That(sweep.points[0].build.passiveStableIds.Count,Is.EqualTo(1));
            Assert.That(sweep.points[1].build.passiveStableIds,
                Does.Contain(sweep.points[0].build.passiveStableIds[0]));
            Assert.That(sweep.points[1].build.passiveStableIds.Count,Is.EqualTo(2));
        }
        finally{UnityEngine.Object.DestroyImmediate(profile);}
    }

    static PlayerBuildSnapshot Fixture(float damage)=>new()
    {
        playerLevel=30,combatLevel=30,classId=PlayerClassIds.Warrior,
        weaponTypeId=WeaponTypeIds.Sword,seed=41001,
        equipment=new()
        {
            new GearSnapshot
            {
                slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Rare,
                itemLevel=30,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,
                baseMin=damage,baseMax=damage*1.5f,baseSpeed=1.2f,baseCrit=.05f
            }
        }
    };

    static PlayerCurvePoint Point(PlayerBuildSnapshot build,OptimizationObjective objective,
        string profile)
    {
        var point=new PlayerCurvePoint
        {
            playerLevel=build.playerLevel,combatLevel=build.combatLevel,
            profile=profile,profileVersion=1,dataFingerprint=ProductionBalanceAdapters.DataFingerprint(),
            build=build.Clone(),metrics=PlayerBuildEvaluator.Evaluate(build),
            objective=JsonUtility.FromJson<OptimizationObjective>(JsonUtility.ToJson(objective)),
            seed=build.seed
        };
        ScenarioResultIdentity.Stamp(point);
        return point;
    }
}
