using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ClassKeystoneProgressionTests
{
    static PlayerProgression Create(int level,out GameObject root)
    {
        root=new GameObject("Keystone progression test");root.AddComponent<PlayerIdentityState>().BeginNewGame(PlayerClassIds.Warrior);
        var progression=root.GetComponent<PlayerProgression>()??root.AddComponent<PlayerProgression>();
        Assert.That(progression.RestoreProgression(level,0,level,new int[PassiveTreeDefinition.NodeCount]),Is.True);return progression;
    }
    public static void AllocateThirty(PlayerProgression p,string route)
    {
        for(int tier=1;tier<=10;tier++)
        {
            Assert.That(p.TrySpend(PassiveTreeDefinition.ClassSpineNode(route,tier)),Is.True);
            foreach(string side in new[]{"left","right"})Assert.That(p.TrySpend(PassiveTreeDefinition.NodeId($"tree.v3.{route}.t{tier:00}.{side}.a")),Is.True);
        }
    }
    [Test] public void TwentyNineNodesLockKeyAndThirtyMakeItEligibleButDoNotGrantExtraPoint()
    {
        var p=Create(30,out var root);try
        {
            AllocateThirty(p,PlayerClassIds.Warrior);int key=ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First();
            Assert.That(ClassPassiveProgressionRules.KeystoneEligible(PlayerClassIds.Warrior,p.IsAllocated),Is.True);
            Assert.That(p.CanSpend(key),Is.False);Assert.That(p.CanSelectWeaponTree,Is.False);
            int side=PassiveTreeDefinition.NodeId("tree.v3.class.warrior.t10.right.a");Assert.That(p.TryRefund(side),Is.True);
            Assert.That(ClassPassiveProgressionRules.KeystoneEligible(PlayerClassIds.Warrior,p.IsAllocated),Is.False);Assert.That(p.CanSpend(key),Is.False);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void KeystoneUnlocksIndependentWeaponChoiceAndRejectsSecondWeaponUntilFullRespec()
    {
        var p=Create(100,out var root);try
        {
            AllocateThirty(p,PlayerClassIds.Warrior);int key=ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First();Assert.That(p.TrySpend(key),Is.True);
            Assert.That(p.NativeClassComplete,Is.True);Assert.That(p.AvailablePoints,Is.EqualTo(69));
            Assert.That(p.TrySelectWeaponTree(WeaponTypeIds.Bow),Is.True);Assert.That(p.TrySelectWeaponTree(WeaponTypeIds.Sword),Is.False);
            Assert.That(p.TrySpend(PassiveTreeDefinition.WeaponSpineNode(WeaponTypeIds.Bow,1)),Is.True);
            Assert.That(p.CanSpend(PassiveTreeDefinition.WeaponSpineNode(WeaponTypeIds.Sword,1)),Is.False);
            Assert.That(p.CanRefund(key),Is.False);Assert.That(p.CanRefund(PassiveTreeDefinition.NodeId("tree.v3.class.warrior.t01.left.a")),Is.False);
            p.RefundAll();Assert.That(p.AvailablePoints,Is.EqualTo(100));Assert.That(p.SelectedWeaponTreeId,Is.Empty);Assert.That(p.SelectedClassRoutes,Is.Empty);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void OffClassTravelRequiresSpineButItsKeystoneRequiresEverySide()
    {
        var p=Create(100,out var root);try
        {
            AllocateThirty(p,PlayerClassIds.Warrior);Assert.That(p.TrySpend(ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First()),Is.True);
            Assert.That(p.TrySelectClassRoute(PlayerClassIds.Barbarian),Is.True);Assert.That(p.TrySelectClassRoute(PlayerClassIds.Mage),Is.False);
            for(int t=1;t<=10;t++)Assert.That(p.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Barbarian,t)),Is.True);
            Assert.That(p.CanSelectAdditionalClass,Is.True);int key=ClassPassiveProgressionRules.Keystones(PlayerClassIds.Barbarian).First();Assert.That(p.CanSpend(key),Is.False);
            Assert.That(p.TrySelectClassRoute(PlayerClassIds.Mage),Is.True);
            foreach(int id in PassiveTreeDefinition.RouteNodes(PlayerClassIds.Barbarian).Where(id=>PassiveTreeDefinition.Node(id).Kind==PassiveNodeKind.Choice&&PassiveTreeDefinition.Node(id).StableId.EndsWith(".a")))Assert.That(p.TrySpend(id),Is.True);
            Assert.That(p.CanSpend(key),Is.True);Assert.That(p.TrySpend(key),Is.True);
            Assert.That(p.CanRefund(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Barbarian,10)),Is.False);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void AuthoringHasSingleKeystoneSlotAndSevenWeaponTiers()
    {
        Assert.That(PassiveTreeDefinition.Nodes.Count,Is.EqualTo(852));Assert.That(PassiveTreeDefinition.Edges.Count,Is.EqualTo(840));
        var view=AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath).GetComponent<PassiveTreeView>();
        foreach(var branch in view.branches)
        {
            if(branch.Data is PassiveClassBranchSO cls){Assert.That(cls.Keystones.Count,Is.EqualTo(3));Assert.That(branch.keystoneSlot,Is.Not.Null);}
            else{Assert.That(branch.Tiers.Count,Is.EqualTo(7));foreach(var tier in branch.Tiers){Assert.That(tier.leftSlot,Is.Not.Null);Assert.That(tier.rightSlot,Is.Not.Null);Assert.That(PassiveTreeDefinition.ChoiceNodes(tier.leftSlot.choiceGroupId).Count(),Is.EqualTo(3));}}
        }
        Assert.That(view.GetComponent<PassiveTreeNavigation>(),Is.Not.Null);
    }
    [Test] public void NewSaveLegalityRequiresExplicitChosenRoutesAndWeapon()
    {
        var p=Create(100,out var root);try
        {
            AllocateThirty(p,PlayerClassIds.Warrior);p.TrySpend(ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First());p.TrySelectWeaponTree(WeaponTypeIds.Bow);p.TrySpend(PassiveTreeDefinition.WeaponSpineNode(WeaponTypeIds.Bow,1));
            Assert.That(PlayerProgression.ValidateAllocationState(p.CopyRanks(),p.ActiveClassId,null,p.SelectedClassRoutes,p.SelectedWeaponTreeId),Is.True);
            Assert.That(PlayerProgression.ValidateAllocationState(p.CopyRanks(),p.ActiveClassId,null),Is.False);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
