using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class PassiveTreeTests
{
    [Test] public void V2HasTargetSizeUniqueStableIdsAndExpectedMacroCounts()
    {
        Assert.That(PassiveTreeDefinition.NodeCount,Is.InRange(360,380));
        Assert.That(PassiveTreeDefinition.Nodes.Select(x=>x.StableId).Distinct().Count(),Is.EqualTo(PassiveTreeDefinition.NodeCount));
        foreach(PassiveRegion region in new[]{PassiveRegion.Warrior,PassiveRegion.Ranger,PassiveRegion.Thief,PassiveRegion.Mage,PassiveRegion.Priest,PassiveRegion.Barbarian})
            Assert.That(PassiveTreeDefinition.Nodes.Count(x=>x.Region==region),Is.EqualTo(44),region.ToString());
        Assert.That(PassiveTreeDefinition.Nodes.Count(x=>x.Region==PassiveRegion.Center),Is.EqualTo(42));
        foreach(PassiveRegion region in new[]{PassiveRegion.WarriorRanger,PassiveRegion.RangerThief,PassiveRegion.ThiefMage,PassiveRegion.MagePriest,PassiveRegion.PriestBarbarian,PassiveRegion.BarbarianWarrior})
            Assert.That(PassiveTreeDefinition.Nodes.Count(x=>x.Region==region),Is.EqualTo(10),region.ToString());
    }

    [Test] public void SixClassStartsAreStableFreeAndCorrectlyOriented()
    {
        var expected=new Dictionary<string,Vector2>{{PlayerClassIds.Warrior,Vector2.up},{PlayerClassIds.Ranger,new(.866f,.5f)},{PlayerClassIds.Thief,new(.866f,-.5f)},{PlayerClassIds.Mage,Vector2.down},{PlayerClassIds.Priest,new(-.866f,-.5f)},{PlayerClassIds.Barbarian,new(-.866f,.5f)}};
        foreach(var pair in expected){var node=PassiveTreeDefinition.Node(PassiveTreeDefinition.StartNodeId(pair.Key));Assert.That(node.Kind,Is.EqualTo(PassiveNodeKind.ClassStart));Assert.That(Vector2.Dot(node.LayoutPosition.normalized,pair.Value),Is.GreaterThan(.99f));}
    }

    [Test] public void EntireGraphIsConnectedAndEveryEdgeIsUndirected()
    {
        var reached=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);
        while(queue.Count>0){int id=queue.Dequeue();foreach(int next in PassiveTreeDefinition.AdjacentNodeIds(id)){Assert.That(PassiveTreeDefinition.AdjacentNodeIds(next),Does.Contain(id));if(reached.Add(next))queue.Enqueue(next);}}
        Assert.That(reached.Count,Is.EqualTo(PassiveTreeDefinition.NodeCount));
    }

    [Test] public void WeaponNodesUseCentralPremiumAndAreConditioned()
    {
        Assert.That(PassiveTreeDefinition.WeaponSpecificEfficiencyMultiplier,Is.InRange(1.5f,1.75f));
        var weaponNodes=PassiveTreeDefinition.Nodes.Where(x=>!string.IsNullOrEmpty(x.WeaponTypeRestriction)&&x.Effects.Length>0).ToArray();
        Assert.That(weaponNodes.Length,Is.GreaterThan(60));Assert.That(weaponNodes.All(x=>WeaponTypeCatalog.IsValid(x.WeaponTypeRestriction)),Is.True);
    }

    [Test] public void EveryClassSectorHasModestBroadMechanicAccess()
    {
        var required=new[]{StatTypes.LifePercent,StatTypes.ManaPercent,StatTypes.CritChance,StatTypes.AttackSpeed,StatTypes.LifeOnHit,StatTypes.ManaOnHit,StatTypes.ArmourPercent,StatTypes.ChanceToHitTwice};
        foreach(PassiveRegion region in new[]{PassiveRegion.Warrior,PassiveRegion.Ranger,PassiveRegion.Thief,PassiveRegion.Mage,PassiveRegion.Priest,PassiveRegion.Barbarian})
        {var stats=PassiveTreeDefinition.Nodes.Where(x=>x.Region==region).SelectMany(x=>x.Effects).Select(x=>x.Stat).ToHashSet();foreach(var stat in required)Assert.That(stats,Does.Contain(stat),$"{region} lacks nearby {stat}");}
    }

    [Test] public void RageDistrictIncludesGenerationRetentionEffectAndRecovery()
    {
        var axe=PassiveTreeDefinition.Nodes.Where(x=>x.WeaponTypeRestriction==WeaponTypeIds.TwoHandedAxe).SelectMany(x=>x.Effects).Select(x=>x.Stat).ToHashSet();
        Assert.That(axe,Does.Contain(StatTypes.RageGeneration));Assert.That(axe,Does.Contain(StatTypes.RageDecayReduction));Assert.That(axe,Does.Contain(StatTypes.RageEffect));Assert.That(axe,Does.Contain(StatTypes.LifeOnHit));Assert.That(axe,Does.Contain(StatTypes.BleedChance));
    }

    [Test] public void TransformFoundationExistsWithoutProductionTransformEffects()
    {
        foreach(var node in PassiveTreeDefinition.Nodes){Assert.That(node.ExtensionMetadata,Is.Not.Null);Assert.That(node.ExtensionMetadata.TransformedEffects,Is.Empty);Assert.That(node.ExtensionMetadata.SupportsTransformation,Is.True);}
    }

    [Test] public void LevelOneAndLevelHundredPointBudgetsEqualLevel()
    {
        var go=new GameObject("progression");try{go.AddComponent<PlayerIdentityState>();var p=go.AddComponent<PlayerProgression>();Assert.That(p.AvailablePoints,Is.EqualTo(1));Assert.That(p.RestoreProgression(100,0,100,new int[PassiveTreeDefinition.NodeCount]),Is.True);Assert.That(p.AvailablePoints,Is.EqualTo(100));}finally{Object.DestroyImmediate(go);}
    }

    [Test] public void SelectedClassIsTheOnlyFreeOriginAndRefundCannotStrandNodes()
    {
        var go=new GameObject("progression");try{var identity=go.AddComponent<PlayerIdentityState>();Assert.That(identity.BeginNewGame(PlayerClassIds.Mage),Is.True);var p=go.AddComponent<PlayerProgression>();Assert.That(p.RestoreProgression(10,0,10,new int[PassiveTreeDefinition.NodeCount]),Is.True);int mage=PassiveTreeDefinition.AdjacentNodeIds(PassiveTreeDefinition.StartNodeId(PlayerClassIds.Mage)).First();int warrior=PassiveTreeDefinition.AdjacentNodeIds(PassiveTreeDefinition.StartNodeId(PlayerClassIds.Warrior)).First();Assert.That(p.CanSpend(mage),Is.True);Assert.That(p.CanSpend(warrior),Is.False);Assert.That(p.TrySpend(mage),Is.True);int child=PassiveTreeDefinition.AdjacentNodeIds(mage).First(x=>!PassiveTreeDefinition.IsClassStart(x));Assert.That(p.TrySpend(child),Is.True);Assert.That(p.CanRefund(mage),Is.False);p.RefundAll();Assert.That(p.AvailablePoints,Is.EqualTo(10));Assert.That(p.ActiveClassId,Is.EqualTo(PlayerClassIds.Mage));}finally{Object.DestroyImmediate(go);}
    }
}
