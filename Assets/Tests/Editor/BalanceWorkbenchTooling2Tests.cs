using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BalanceWorkbenchTooling2Tests
{
    static PlayerBuildSnapshot Build(int level=20)=>new(){playerLevel=level,combatLevel=level,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,seed=17002};

    [Test] public void Evaluator_UsesProductionLevelAndPlayerPipelines()
    {
        var low=PlayerBuildEvaluator.Evaluate(Build(1));var high=PlayerBuildEvaluator.Evaluate(Build(60));
        Assert.That(low.life,Is.EqualTo(1000).Within(.01));Assert.That(high.life,Is.GreaterThan(low.life));
        Assert.That(double.IsFinite(high.basicDps));Assert.That(high.skills.Count,Is.EqualTo(2));
        Assert.That(high.assumptions,Is.Not.Empty);
    }

    [Test] public void ObjectiveNormalization_IsFiniteAtZeroAndSymmetricInLogSpace()
    {
        Assert.That(double.IsFinite(OptimizationMetricCatalog.Normalized(10,0)),Is.True);
        double up=OptimizationMetricCatalog.Normalized(20,10),down=OptimizationMetricCatalog.Normalized(10,20);
        Assert.That(up,Is.GreaterThan(0));Assert.That(down,Is.LessThan(0));
    }

    [Test] public void PassiveOptimizer_OnlyReturnsProductionLegalStates()
    {
        var build=Build(4);var objective=new OptimizationObjective{primary="life",secondary="basic_dps"};var result=PassiveTreeOptimizer.Optimize(build,4,objective,false);
        Assert.That(result.build.passiveStableIds.Count,Is.EqualTo(4));
        Assert.That(PlayerProgression.ValidateAllocationState(result.build.AllocationRanks(),build.classId,build.subclassId),Is.True);
        Assert.That(result.sequence.Select(x=>x.stableId).Distinct().Count(),Is.EqualTo(4));
    }

    [Test] public void MinimumPathPackage_UsesRealValidator()
    {
        var build=Build();int target=PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Mage,1);var package=PassiveTreeOptimizer.MinimumLegalPackage(build.AllocationRanks(),target,build.classId,build.subclassId);
        Assert.That(package,Is.Not.Null);var ranks=build.AllocationRanks();foreach(int id in package)ranks[id]=1;
        Assert.That(PlayerProgression.ValidateAllocationState(ranks,build.classId,build.subclassId),Is.True);
        Assert.That(package.Count,Is.EqualTo(11));
    }

    [Test] public void GearOptimizer_IsDeterministicAndUsesLegalGeneratedItems()
    {
        var profile=ScriptableObject.CreateInstance<PlayerGearProfileSO>();try{profile.candidatesPerSlot=2;profile.candidateRetention=1;profile.gearsetBeamWidth=1;profile.useNaturalRarity=false;profile.minimumRarity=profile.maximumRarity=LootManager.GearRarity.Magic;var objective=new OptimizationObjective();var a=PlayerGearsetOptimizer.Optimize(Build(),profile,objective,false);var b=PlayerGearsetOptimizer.Optimize(Build(),profile,objective,false);Assert.That(a.build.equipment.Count,Is.EqualTo(Enum.GetValues(typeof(LootManager.GearType)).Length));Assert.That(a.build.equipment.Select(x=>x.Description),Is.EqualTo(b.build.equipment.Select(x=>x.Description)));Assert.That(a.debug.generated,Is.EqualTo(16));}finally{UnityEngine.Object.DestroyImmediate(profile);}
    }

    [Test] public void AuthoredProfilesAndTreeLayout_AreAvailable()
    {
        foreach(string name in new[]{"Low","Mid","Optimized"}){var p=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>($"Assets/Balance/Profiles/SO_PlayerGearProfile_{name}.asset");Assert.That(p,Is.Not.Null);Assert.That(p.allowLegalCrafting,Is.False);Assert.That(p.simulationNotice,Does.Contain("NOT GAME BALANCE"));}
        Assert.That(PassiveTreeDefinition.Nodes.Count,Is.EqualTo(750));Assert.That(PassiveTreeDefinition.Nodes.Select(x=>x.LayoutPosition).Distinct().Count(),Is.EqualTo(750));
    }
}
