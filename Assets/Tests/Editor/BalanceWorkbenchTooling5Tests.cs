using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class BalanceWorkbenchTooling5Tests
{
    static PlayerBuildSnapshot Build()=>new(){playerLevel=60,combatLevel=60,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Rare,itemLevel=60,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=40,baseMax=60,baseSpeed=1.2f,baseCrit=.05f}}};

    [Test] public void BreakpointFinder_FindsExactIntegerAndAllNonMonotonicCrossings()
    {
        var first=BreakpointFinder.Find(level=>level,1,100,10,37,BreakpointOperator.GreaterOrEqual);
        Assert.That(first.Single().level,Is.EqualTo(37));
        var all=BreakpointFinder.Find(level=>Math.Sin(level),1,30,7,0,BreakpointOperator.CrossUp,true);
        Assert.That(all.Count,Is.GreaterThan(1));
        Assert.That(all.All(x=>x.before<=0&&x.at>0),Is.True);
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
}
