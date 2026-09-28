using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BlackCube.CombatSimulation;

public sealed class GenericClassPassiveReworkTests
{
    [TestCase(PlayerClassIds.Barbarian)] [TestCase(PlayerClassIds.Ranger)] [TestCase(PlayerClassIds.Mage)] [TestCase(PlayerClassIds.Priest)] [TestCase(PlayerClassIds.Thief)]
    public void AuthoredChoicesAndSpinesMatchRotation(string id)
    {
        var branch=GenericClassPassiveReauthoring.Branch(id);var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        Assert.That(branch.Tiers.Count,Is.EqualTo(10));
        for(int t=0;t<10;t++)
        {
            var expectedSpine=GenericClassPassiveReauthoring.Spine(id);var spine=branch.Tiers[t].Spine;
            Assert.That(spine.Effects.Select(x=>x.Stat),Is.EqualTo(expectedSpine.Select(x=>x.Stat)));
            Assert.That(spine.Effects.Select(x=>x.Value),Is.EqualTo(expectedSpine.Select(x=>x.Amount)));
            var keys=GenericClassPassiveReauthoring.Rotations[id][t].Split(' ');var nodes=branch.Tiers[t].Left.GenericNodes.Concat(branch.Tiers[t].Right.GenericNodes).ToArray();
            for(int i=0;i<6;i++){var effects=GenericClassPassiveReauthoring.Effects(db,keys[i]);Assert.That(nodes[i].Effects.Select(x=>x.Stat),Is.EqualTo(effects.Select(x=>x.Stat)));Assert.That(nodes[i].Effects.Select(x=>x.Value),Is.EqualTo(effects.Select(x=>x.Amount)));Assert.That(nodes[i].Keystone,Is.EqualTo(PassiveKeystone.None));Assert.That(nodes[i].IconOverride,Is.Not.Null);}
        }
    }
    [TestCase(PlayerClassIds.Barbarian)] [TestCase(PlayerClassIds.Ranger)] [TestCase(PlayerClassIds.Mage)] [TestCase(PlayerClassIds.Priest)] [TestCase(PlayerClassIds.Thief)]
    public void ProductionClassHasTenSpinesAndTwentyCircularSlots(string id)
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath);var branch=root.GetComponent<PassiveTreeView>().branches.First(x=>x.RouteId==id);
        Assert.That(branch.AllNodes().Count(),Is.EqualTo(10));var slots=branch.GetComponentsInChildren<PassiveChoiceSlotView>(true);Assert.That(slots.Length,Is.EqualTo(21));
        foreach(var slot in slots){Assert.That(slot.icon.transform.parent.GetComponent<Mask>(),Is.Not.Null);Assert.That(slot.emptyGlow,Is.Not.Null);Assert.That(PassiveTreeDefinition.ChoiceNodes(slot.choiceGroupId).Count(),Is.EqualTo(slot==branch.keystoneSlot?3:4));}
    }
    [Test] public void SpecialSupplyTotalsAreDeliberate()
    {
        float Total(string id,StatTypes stat)=>GenericClassPassiveReauthoring.Branch(id).Tiers.SelectMany(t=>t.Left.GenericNodes.Concat(t.Right.GenericNodes)).SelectMany(n=>n.Effects).Where(e=>e.Stat==stat).Sum(e=>e.Value);
        Assert.That(Total(PlayerClassIds.Ranger,StatTypes.ProjectilePrecisionChance),Is.EqualTo(40));Assert.That(Total(PlayerClassIds.Mage,StatTypes.CooldownReduction),Is.EqualTo(50));Assert.That(Total(PlayerClassIds.Priest,StatTypes.AuraEffect),Is.EqualTo(50));Assert.That(Total(PlayerClassIds.Mage,StatTypes.ManaOnHit),Is.Zero);
    }
    [TestCase(WeaponTypeIds.Sword,true)] [TestCase(WeaponTypeIds.TwoHandedAxe,true)] [TestCase(WeaponTypeIds.Dagger,true)] [TestCase(WeaponTypeIds.Sceptre,true)] [TestCase(WeaponTypeIds.Bow,false)] [TestCase(WeaponTypeIds.Staff,false)]
    public void MultistrikeIsMeleeOnly(string weapon,bool expected)=>Assert.That(GenericPassiveMechanics.SupportsMultistrike(weapon),Is.EqualTo(expected));
    [Test] public void RevengeAccumulatesHitsAndConsumesOnce()
    {
        var root=new GameObject("Revenge test");try{var stats=root.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.RevengeEffect,10);var state=root.AddComponent<RevengeState>();state.RecordHit(30,100);state.RecordHit(10,100);Assert.That(state.StoredFraction,Is.EqualTo(.4f).Within(.0001));Assert.That(state.ConsumeAttack(),Is.EqualTo(1.88f).Within(.0001));Assert.That(state.ConsumeAttack(),Is.EqualTo(1));Assert.That(GenericPassiveMechanics.RevengeMultiplier(.5f),Is.EqualTo(2));}finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void BundledDamagingChanceDoesNotIncludeShockOrChill()
    {var root=new GameObject("Chance test");try{var stats=root.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.AllDamagingAilmentChance,10);foreach(var stat in new[]{StatTypes.PoisonChance,StatTypes.BleedChance,StatTypes.IgniteChance})Assert.That(stats.GetStat(stat),Is.EqualTo(.1f));Assert.That(stats.GetStat(StatTypes.ShockChance),Is.Zero);Assert.That(stats.GetStat(StatTypes.ChillChance),Is.Zero);}finally{UnityEngine.Object.DestroyImmediate(root);}}
    [Test] public void RecoveryAndPoisonLeechUseFractionUnits()
    {Assert.That(GenericPassiveMechanics.Recovery(20,.1f),Is.EqualTo(22));Assert.That(GenericPassiveMechanics.PoisonLeech(30,.02f),Is.EqualTo(.6f).Within(.0001));}
    [Test] public void HealthRecoveryScalesLifeButNotMana()
    {var root=new GameObject("Recovery test");try{var stats=root.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,100);stats.SetBaseStat(StatTypes.Mana,100);stats.SetBaseStat(StatTypes.LifeRecoveryEffect,50);var health=root.AddComponent<HealthComponent>();health.ConfigureIsolatedStats(stats);var mana=root.AddComponent<ManaComponent>();mana.ConfigureIsolatedStats(stats);health.LoseLife(50);health.RestoreLife(10);Assert.That(health.CurrentLife,Is.EqualTo(65));Assert.That(mana.MaxMana,Is.EqualTo(100));}finally{UnityEngine.Object.DestroyImmediate(root);}}
    [TestCase(WeaponTypeIds.Sword,true)] [TestCase(WeaponTypeIds.Staff,false)] [TestCase(WeaponTypeIds.Bow,false)]
    public void LaboratoryUsesTheSamePlayerMultistrikeRestriction(string weapon,bool melee)
    {
        var player=new CombatantSnapshot{id="player",player=true,weaponTypeId=weapon,maximumLife=1000,maximumMana=100,attackSpeed=1,hitTwiceChance=1,basicDamage=new CombatDamageSnapshot{physical=1}};
        var enemy=new CombatantSnapshot{id="enemy",maximumLife=1000,attackSpeed=.1f,basicDamage=new CombatDamageSnapshot{physical=1}};
        var result=HeadlessCombatSimulator.Run(player,enemy,new CombatSimulationConfig{maximumDuration=3,actionPolicy=PlayerActionPolicy.BasicOnly});Assert.That(result.error,Is.Null.Or.Empty);Assert.That(result.hitTwiceCount,melee?Is.GreaterThan(0):Is.Zero);
    }
    [Test] public void AurasUseLatestTypedHitAndExpireAfterTwoTurns()
    {
        var a=new FiveAuraState();a.RecordTypedHit(0,100,1000,false);Assert.That(a.Intensity(0),Is.Zero);a.RecordTypedHit(0,50,1000,true);Assert.That(a.Intensity(0),Is.EqualTo(.5f));a.RecordTypedHit(0,10,1000,true);Assert.That(a.Intensity(0),Is.EqualTo(.1f));Assert.That(a.DamageMultiplier(Element.Fire,0),Is.EqualTo(1));a.Tick();Assert.That(a.Intensity(0),Is.EqualTo(.1f));a.Tick();Assert.That(a.Intensity(0),Is.Zero);
    }
    [Test] public void AuraScalingHasNoTwoHundredPercentCapAndVoidRequiresSameHit()
    {
        var a=new FiveAuraState();a.RecordTypedHit(1,100,1000,true);Assert.That(a.DamageMultiplier(Element.Fire,3),Is.EqualTo(1.8f).Within(.0001));Assert.That(a.ExtraIgniteTicks(3),Is.EqualTo(4));a.RecordDamagingAilments(true,false,true,true);Assert.That(a.Intensity(4),Is.Zero);a.RecordDamagingAilments(false,true,false,true);Assert.That(a.Intensity(4),Is.Zero);a.RecordDamagingAilments(true,true,true,true);Assert.That(a.Intensity(4),Is.EqualTo(1));Assert.That(a.Bonus(4,.2f,.5f),Is.EqualTo(.3f).Within(.0001));
    }
    [Test] public void LineAttachmentsRespectOffCenterPivotsAndOffsets()
    {
        var root=new GameObject("Line geometry",typeof(RectTransform));try{var parent=(RectTransform)root.transform;parent.sizeDelta=new Vector2(800,600);parent.pivot=new Vector2(.2f,.8f);RectTransform Child(string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}var from=Child("From");var to=Child("To");from.sizeDelta=to.sizeDelta=Vector2.one*100;from.anchoredPosition=new Vector2(-150,80);to.anchoredPosition=new Vector2(220,-60);from.pivot=new Vector2(.1f,.8f);to.pivot=new Vector2(.8f,.2f);var lineRect=Child("Line");var line=lineRect.gameObject.AddComponent<PassiveConnectionBinding>();line.Configure(from,to,lineRect);line.FromAnchor=new Vector2(1,.5f);line.ToAnchor=new Vector2(0,.5f);line.FromOffset=new Vector2(4,8);line.ToOffset=new Vector2(-6,3);line.UpdateGeometry();Assert.That(Vector3.Distance(lineRect.TransformPoint(new Vector3(-lineRect.rect.width*.5f,0,0)),line.FromWorldPoint),Is.LessThan(.01));Assert.That(Vector3.Distance(lineRect.TransformPoint(new Vector3(lineRect.rect.width*.5f,0,0)),line.ToWorldPoint),Is.LessThan(.01));}finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void LayoutDraggingDoesNotWriteProductionPrefab()
    {
        string before=File.ReadAllText(PassiveTreePrefabBuilder.PrefabPath);var window=ScriptableObject.CreateInstance<PassiveTreeLayoutWindow>();try{window.LoadEditingCopy();var rect=(RectTransform)window.EditingRoot.GetComponent<PassiveTreeView>().branches[0].Tiers[0].spine.transform;window.MoveNode(rect,rect.anchoredPosition+new Vector2(50,30));Assert.That(window.HasUnsavedChanges,Is.True);Assert.That(File.ReadAllText(PassiveTreePrefabBuilder.PrefabPath),Is.EqualTo(before));Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(File.ReadAllText(PassiveTreePrefabBuilder.PrefabPath),Is.EqualTo(before));}finally{window.Discard();UnityEngine.Object.DestroyImmediate(window);}
    }
    [Test] public void StagedSavePersistsNodePositionAndConnectionOffsets()
    {
        string testPath=AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/Editor/PassiveLayoutSaveTest.prefab");
        Assert.That(AssetDatabase.CopyAsset(PassiveTreePrefabBuilder.PrefabPath,testPath),Is.True);
        var window=ScriptableObject.CreateInstance<PassiveTreeLayoutWindow>();
        try
        {
            window.LoadEditingCopy(testPath);
            var rect=(RectTransform)window.EditingRoot.GetComponent<PassiveTreeView>().branches[0].Tiers[0].spine.transform;
            Vector2 expected=rect.anchoredPosition+new Vector2(37,19);
            var line=window.EditingRoot.GetComponentsInChildren<PassiveConnectionBinding>(true).First(x=>x.From!=null&&x.To!=null);
            string lineName=line.name;line.FromOffset=new Vector2(7,9);line.ToAnchor=new Vector2(.7f,.4f);
            window.MoveNode(rect,expected);window.Save();Assert.That(window.HasUnsavedChanges,Is.False);window.Discard();
            window.LoadEditingCopy(testPath);
            var restored=(RectTransform)window.EditingRoot.GetComponent<PassiveTreeView>().branches[0].Tiers[0].spine.transform;
            Assert.That(restored.anchoredPosition,Is.EqualTo(expected));
            var restoredLine=window.EditingRoot.GetComponentsInChildren<PassiveConnectionBinding>(true).First(x=>x.name==lineName);
            Assert.That(restoredLine.FromOffset,Is.EqualTo(new Vector2(7,9)));
            Assert.That(restoredLine.ToAnchor,Is.EqualTo(new Vector2(.7f,.4f)));
        }
        finally{window.Discard();UnityEngine.Object.DestroyImmediate(window);AssetDatabase.DeleteAsset(testPath);}
    }
    [Test] public void AuraAmuletFamiliesArePrefixesAndNotEmpowerable()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");for(int i=0;i<5;i++){var def=db.GetDefinition(GenericPassiveMechanics.AuraGrant(i));Assert.That(def.side,Is.EqualTo(AffixSide.Prefix));Assert.That(def.empowerable,Is.False);Assert.That(def.allowedSlots,Is.EqualTo(new[]{LootManager.GearType.Amulets}));Assert.That(def.tiers.Count,Is.EqualTo(1));Assert.That(def.tiers[0].minItemLevel,Is.EqualTo(70));Assert.That(def.tiers[0].minValue,Is.EqualTo(1));Assert.That(def.tiers[0].maxValue,Is.EqualTo(1));}
    }
}
