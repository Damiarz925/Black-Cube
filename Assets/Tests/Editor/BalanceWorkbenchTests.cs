using System;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BalanceWorkbenchTests
{
    [Test] public void SeededSimulationRandomSource_IsExactAndDoesNotReplaceProductionRng()
    {
        var a=new SeededSimulationRandomSource(9182);var b=new SeededSimulationRandomSource(9182);var c=new SeededSimulationRandomSource(9183);float[] av=Enumerable.Range(0,64).Select(_=>a.Value()).ToArray();float[] bv=Enumerable.Range(0,64).Select(_=>b.Value()).ToArray();float[] cv=Enumerable.Range(0,64).Select(_=>c.Value()).ToArray();
        Assert.That(av,Is.EqualTo(bv));Assert.That(cv,Is.Not.EqualTo(av));Assert.That(LootRandomSourceFactory.CreateProduction(),Is.TypeOf<ProductionLootRandomSource>());
    }

    [Test] public void Statistics_UsesDocumentedR7PercentilesAndFiniteHistogram()
    {
        var s=MetricSummary.From(Enumerable.Range(1,100).Select(x=>(double)x));Assert.That(s.count,Is.EqualTo(100));Assert.That(s.median,Is.EqualTo(50.5).Within(1e-8));Assert.That(s.p90,Is.EqualTo(90.1).Within(1e-8));Assert.That(WorkbenchStatistics.Histogram(new[]{1d,2,3,4},2).Sum(x=>x.count),Is.EqualTo(4));
    }

    [Test] public void WeaponLab_IsDeterministicAndUsesProductionAffixPipeline()
    {
        var r=new ItemLabRequest{mode=ItemLabMode.Weapon,weaponTypeId=WeaponTypeIds.Dagger,itemLevel=50,rarity=LootManager.GearRarity.Rare,sampleCount=20,seed=77};var a=ProductionBalanceAdapters.RunItems(r);var b=ProductionBalanceAdapters.RunItems(r);
        Assert.That(a.samples.Select(x=>x.detail),Is.EqualTo(b.samples.Select(x=>x.detail)));Assert.That(a.samples.All(x=>x.affixCount==4&&x.weaponDps>0&&x.weaponType==WeaponTypeIds.Dagger),Is.True);r.seed++;var c=ProductionBalanceAdapters.RunItems(r);Assert.That(c.samples.Select(x=>x.detail),Is.Not.EqualTo(a.samples.Select(x=>x.detail)));
    }

    [Test] public void DropSimulator_IsDeterministicAndReadsAuthoritativeProfile()
    {
        var request=new DropLabRequest{level=150,rarity=EnemyAI.EnemyRarity.Rare,sampleCount=2000,seed=81,fixedGearQuality=1.25f};var profile=LootBalanceProfileSO.Current;float before=profile.currencyRollCoefficient;var a=ProductionBalanceAdapters.RunDrops(request);var b=ProductionBalanceAdapters.RunDrops(request);
        Assert.That(a.averageGearItems,Is.EqualTo(b.averageGearItems));Assert.That(a.averageCurrencyRolls,Is.EqualTo(b.averageCurrencyRolls));Assert.That(a.currencies.Select(x=>x.averagePerKill),Is.EqualTo(b.currencies.Select(x=>x.averagePerKill)));Assert.That(profile.currencyRollCoefficient,Is.EqualTo(before));Assert.That(a.currencies.Count,Is.EqualTo(profile.currencies.Count));Assert.That(a.averageGearItems,Is.GreaterThanOrEqualTo(1));
    }

    [Test] public void DropSimulator_ActualGearModeUsesProductionEnemySamples()
    {
        var request=new DropLabRequest{level=40,rarity=EnemyAI.EnemyRarity.Magic,sampleCount=2,seed=88,generateActualEnemyGear=true};var result=ProductionBalanceAdapters.RunDrops(request);Assert.That(result.metadata.sampleCount,Is.EqualTo(2));Assert.That(result.averageGearItems,Is.GreaterThanOrEqualTo(1));
    }

    [Test] public void EnemyGearLab_UsesProductionCandidatePolicyAndForcedDamageIsAnOverride()
    {
        var request=new EnemyLabRequest{level=50,rarity=EnemyAI.EnemyRarity.Rare,sampleCount=2,seed=93,forcePrimaryDamage=true,primaryDamage=Element.Light};var a=ProductionBalanceAdapters.RunEnemies(request);var b=ProductionBalanceAdapters.RunEnemies(request);
        Assert.That(a.samples.Select(x=>x.equipment),Is.EqualTo(b.samples.Select(x=>x.equipment)));Assert.That(a.samples.All(x=>x.candidateCount==EnemyBuildOptimizer.CandidateCountForLevel(50)),Is.True);Assert.That(a.samples.All(x=>x.slotCount>0&&x.gearScore>0),Is.True);
    }

    [TestCase(1)][TestCase(10)][TestCase(20)][TestCase(50)][TestCase(100)][TestCase(200)][TestCase(360)]
    public void EnemyCandidateCounts_AreProductionOwned(int level)
    {Assert.That(EnemyBuildOptimizer.CandidateCountForLevel(level),Is.EqualTo(Mathf.Clamp(((Mathf.Max(1,level)-1)/10)+1,1,10)));}

    [Test] public void Curves_AreFiniteThroughLevel360()
    {var curves=ProductionBalanceAdapters.IntrinsicCurves(1,360,1);Assert.That(curves.Count,Is.EqualTo(4));Assert.That(curves.All(x=>x.points.Count==360));Assert.That(curves.SelectMany(x=>x.points).All(x=>double.IsFinite(x.mean)),Is.True);}

    [Test] public void Validation_ResolvesEveryProductionAdapter()
    {Assert.That(BalanceWorkbenchValidation.Validate(),Is.Empty);Assert.That(AssetDatabase.LoadAssetAtPath<LootBalanceProfileSO>(BalanceWorkbenchAssets.LootProfilePath),Is.Not.Null);}

    [Test] public void Chart_ExportsPngForMultipleSeries()
    {
        string path=new BalanceWorkbenchChart().ExportPng(ProductionBalanceAdapters.IntrinsicCurves(1,20,1),"test_curve",320,180);try{Assert.That(File.Exists(path),Is.True);Assert.That(new FileInfo(path).Length,Is.GreaterThan(100));}finally{if(File.Exists(path))File.Delete(path);AssetDatabase.Refresh();}
    }
}
