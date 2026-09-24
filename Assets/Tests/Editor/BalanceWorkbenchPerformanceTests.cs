using System;
using System.Collections.Generic;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class BalanceWorkbenchPerformanceTests
{
    [Test] public void CachedMidGearEvaluation_MatchesDirectAndIsStable()
    {
        var objective=new OptimizationObjective{primary="physical",secondary="attack_speed",primaryWeight=.70f,mode=OptimizationMode.Weighted};
        var build=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,seed=41001};
        var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset");
        var geared=PlayerGearsetOptimizer.Optimize(build,profile,objective,false).build;
        string directA,directB;using(var evaluation=new PlayerBuildEvaluation(geared))directA=UnityEngine.JsonUtility.ToJson(evaluation.Metrics);
        using(var evaluation=new PlayerBuildEvaluation(geared))directB=UnityEngine.JsonUtility.ToJson(evaluation.Metrics);
        Assert.That(directB,Is.EqualTo(directA),"Direct production evaluations must repeat.");
        Assert.That(UnityEngine.JsonUtility.ToJson(PlayerBuildEvaluator.Evaluate(geared)),Is.EqualTo(directA));
    }
    [Test] public void ReusedPassiveActor_MatchesFreshProductionActorAcrossChangingAllocations()
    {
        var objective=new OptimizationObjective{primary="physical",secondary="attack_speed",primaryWeight=.70f,mode=OptimizationMode.Weighted};
        var baseBuild=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,seed=41001};
        var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>("Assets/Balance/Profiles/SO_PlayerGearProfile_Mid.asset");
        var build=PlayerGearsetOptimizer.Optimize(baseBuild,profile,objective,false).build;
        using var reused=new PlayerBuildEvaluation(build);var ranks=build.AllocationRanks();var random=new Random(41001);
        for(int i=0;i<15;i++)
        {
            var legal=PassiveTreeOptimizer.LegalNext(ranks,build.classId,build.subclassId);Assert.That(legal,Is.Not.Empty);
            ranks[legal[random.Next(legal.Count)]]=1;build.passiveStableIds=PassiveTreeDefinition.Nodes.Where(n=>ranks[n.Id]!=0).Select(n=>n.StableId).ToList();
            string actual=UnityEngine.JsonUtility.ToJson(reused.ReevaluatePassives(build));
            using var direct=new PlayerBuildEvaluation(build);
            Assert.That(actual,Is.EqualTo(UnityEngine.JsonUtility.ToJson(direct.Metrics)),"allocation "+i);
        }
    }
    [Test] public void ReusedPassiveActor_MatchesFreshActorForEveryClassWeapon()
    {
        var combinations=new[]{(PlayerClassIds.Warrior,WeaponTypeIds.Sword),(PlayerClassIds.Mage,WeaponTypeIds.Staff),(PlayerClassIds.Ranger,WeaponTypeIds.Bow),(PlayerClassIds.Thief,WeaponTypeIds.Dagger),(PlayerClassIds.Priest,WeaponTypeIds.Sceptre),(PlayerClassIds.Barbarian,WeaponTypeIds.TwoHandedAxe)};
        foreach(var (classId,weaponId) in combinations)
        {
            var build=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=classId,weaponTypeId=weaponId,seed=41001};
            using var reused=new PlayerBuildEvaluation(build);var ranks=build.AllocationRanks();
            for(int i=0;i<5;i++)
            {
                var legal=PassiveTreeOptimizer.LegalNext(ranks,classId,"");ranks[legal[0]]=1;
                build.passiveStableIds=PassiveTreeDefinition.Nodes.Where(n=>ranks[n.Id]!=0).Select(n=>n.StableId).ToList();
                string actual=UnityEngine.JsonUtility.ToJson(reused.ReevaluatePassives(build));
                using var direct=new PlayerBuildEvaluation(build);
                Assert.That(actual,Is.EqualTo(UnityEngine.JsonUtility.ToJson(direct.Metrics)),classId+" point "+i);
            }
        }
    }
    [Test] public void CachedPlayerEvaluation_MatchesProductionPipelineForAllSixClassWeapons()
    {
        var combinations=new[]{(PlayerClassIds.Warrior,WeaponTypeIds.Sword),(PlayerClassIds.Mage,WeaponTypeIds.Staff),(PlayerClassIds.Ranger,WeaponTypeIds.Bow),(PlayerClassIds.Thief,WeaponTypeIds.Dagger),(PlayerClassIds.Priest,WeaponTypeIds.Sceptre),(PlayerClassIds.Barbarian,WeaponTypeIds.TwoHandedAxe)};
        foreach(var (classId,weaponId) in combinations)
        {
            var build=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=classId,weaponTypeId=weaponId,seed=41001};
            string expected;using(var direct=new PlayerBuildEvaluation(build))expected=UnityEngine.JsonUtility.ToJson(direct.Metrics);
            Assert.That(UnityEngine.JsonUtility.ToJson(PlayerBuildEvaluator.Evaluate(build)),Is.EqualTo(expected),classId);
            var result=PlayerBuildEvaluator.Evaluate(build);result.life=-999;
            Assert.That(UnityEngine.JsonUtility.ToJson(PlayerBuildEvaluator.Evaluate(build)),Is.EqualTo(expected),classId+" cache isolation");
        }
    }
    // Frozen reference of the pre-performance-pass legal-node discovery. This is
    // deliberately slow and used only by parity tests, never the Workbench.
    static List<int> ReferenceLegalNext(int[] ranks,string classId,string subclassId,OptimizationConstraints constraints)
    {
        var excluded=new HashSet<string>(constraints.excludedPassiveIds??new());var result=new List<int>();
        for(int id=0;id<ranks.Length;id++)
        {
            var node=PassiveTreeDefinition.Node(id);if(ranks[id]!=0||excluded.Contains(node.StableId))continue;
            var copy=(int[])ranks.Clone();copy[id]=1;
            if(constraints.maximumOffClassPoints>=0)
            {
                int off=0;foreach(var n in PassiveTreeDefinition.Nodes)
                    if(copy[n.Id]!=0&&(n.IsClassRoute&&n.RouteClassId!=classId||n.IsWeaponRoute&&PassiveTreeDefinition.WeaponClass(n.RouteWeaponId)!=classId))off++;
                if(off>constraints.maximumOffClassPoints)continue;
            }
            if(PlayerProgression.ValidateAllocationState(copy,classId,subclassId))result.Add(id);
        }
        return result;
    }

    [Test] public void IncrementalLegalNodes_MatchFullProductionValidatorAcrossClassesAndBudgets()
    {
        var random=new Random(41001);
        foreach(var classId in PlayerClassCatalog.All.Select(x=>x.Id))
        foreach(int budget in new[]{-1,3})
        {
            var ranks=new int[PassiveTreeDefinition.NodeCount];var constraints=new OptimizationConstraints{maximumOffClassPoints=budget};
            for(int step=0;step<5;step++)
            {
                Assert.That(PlayerProgression.ValidateAllocationState(ranks,classId,""),Is.True);
                var expected=ReferenceLegalNext(ranks,classId,"",constraints);
                var actual=PassiveTreeOptimizer.LegalNext(ranks,classId,"",constraints);
                Assert.That(actual,Is.EqualTo(expected),classId+" budget "+budget+" step "+step);
                if(actual.Count==0)break;ranks[actual[random.Next(actual.Count)]]=1;
            }
        }
    }
}
