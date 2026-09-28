using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WarriorPassiveReauthoringTests
{
    PassiveClassBranchSO Warrior=>AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(WarriorPassiveReauthoring.BranchPath);
    [Test] public void SpineIsExactlyFiftyStrengthAndFiftyDexterity()
    {
        Assert.That(Warrior.Tiers.Count,Is.EqualTo(10));
        foreach(var tier in Warrior.Tiers){Assert.That(tier.Spine.Effects.Count,Is.EqualTo(2));Assert.That(tier.Spine.Effects.Single(x=>x.Stat==StatTypes.Strength).Value,Is.EqualTo(5));Assert.That(tier.Spine.Effects.Single(x=>x.Stat==StatTypes.Dexterity).Value,Is.EqualTo(5));}
        Assert.That(Warrior.Tiers.SelectMany(x=>x.Spine.Effects).Where(x=>x.Stat==StatTypes.Strength).Sum(x=>x.Value),Is.EqualTo(50));
        Assert.That(Warrior.Tiers.SelectMany(x=>x.Spine.Effects).Where(x=>x.Stat==StatTypes.Dexterity).Sum(x=>x.Value),Is.EqualTo(50));
    }
    [Test] public void AllSixtyChoicesMatchAuthoritativeRotationAndT1Values()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        for(int t=0;t<10;t++){var nodes=Warrior.Tiers[t].Left.GenericNodes.Concat(Warrior.Tiers[t].Right.GenericNodes).ToArray();for(int i=0;i<6;i++){var stat=WarriorPassiveReauthoring.Choices[t,i];Assert.That(nodes[i].Effects.Count,Is.EqualTo(1));Assert.That(nodes[i].Effects[0].Stat,Is.EqualTo(stat));Assert.That(nodes[i].Effects[0].Value,Is.EqualTo(WarriorPassiveReauthoring.Value(db,stat)));Assert.That(nodes[i].IconOverride,Is.EqualTo(WarriorPassiveReauthoring.Sprite(WarriorPassiveReauthoring.Icons[stat])));Assert.That(nodes[i].Keystone,Is.EqualTo(PassiveKeystone.None));}}
        Assert.That(Warrior.Tiers.SelectMany(x=>new[]{x.Spine}.Concat(x.Left.GenericNodes).Concat(x.Right.GenericNodes)).SelectMany(x=>x.Effects).Any(x=>x.Stat==StatTypes.GenericMult||x.Stat==StatTypes.ManaPercent),Is.False);
    }
    [Test] public void SubclassFourthChoicesAreNumericallyUnchanged()
    {
        StatTypes[] a={StatTypes.PhysDmg,StatTypes.LifeOnHit,StatTypes.BleedChance},b={StatTypes.ChanceToHitTwice,StatTypes.CritMult,StatTypes.AttackSpeed};
        for(int i=0;i<10;i++)foreach(var side in new[]{Warrior.Tiers[i].Left,Warrior.Tiers[i].Right})
        {float amount=9+.2f*i;foreach(var pair in new[]{(side.SubclassA,a[i%3]),(side.SubclassB,b[i%3])}){Assert.That(pair.Item1.Effects.Count,Is.EqualTo(1));Assert.That(pair.Item1.Effects[0].Stat,Is.EqualTo(pair.Item2));float factor=pair.Item2==StatTypes.LifeOnHit?.4f:pair.Item2==StatTypes.ChanceToHitTwice?.6f:pair.Item2==StatTypes.CritMult?1.6f:1;Assert.That(pair.Item1.Effects[0].Value,Is.EqualTo(amount*factor).Within(.0001f));}}
    }
    [Test] public void AllFourteenSpritesAndImportSettingsAreValid()
    {
        foreach(var name in WarriorPassiveReauthoring.Icons.Values.Concat(new[]{"PlayerHub","ClassBadge"}))
        {string path=WarriorPassiveReauthoring.ArtPath+"Warrior_"+name+".png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Assert.That(sprite,Is.Not.Null,path);Assert.That(sprite.rect.size,Is.EqualTo(Vector2.one*640));var t=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(t.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(t.mipmapEnabled,Is.False);Assert.That(t.alphaIsTransparency,Is.True);Assert.That(t.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));}
        var icons=PassiveTreeDefinition.Database.IconLibrary;Assert.That(icons.Resolve(Warrior.Tiers[0].Spine).name,Does.StartWith("Strength-"));Assert.That(icons.Resolve(Warrior.Tiers[0].Spine),Is.Not.EqualTo(WarriorPassiveReauthoring.Sprite("IncreasedStrengthPercent")));
    }
    [Test] public void MultistrikePreservesSerializedIdentity()
    {Assert.That((int)StatTypes.ChanceToHitTwice,Is.EqualTo(83));Assert.That(StatDisplayFormatting.ToFriendlyName(StatTypes.ChanceToHitTwice),Is.EqualTo("Multistrike Chance"));Assert.That(StatDisplayFormatting.PlayerFacingText("Sword Hit Twice"),Is.EqualTo("Sword Multistrike"));}
    [Test] public void AllElementalResistanceDoesNotMitigateVoid()
    {
        var go=new GameObject("Resistance test");try{var stats=go.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.AllRes,50);var method=typeof(CombatCalculator).GetMethod("ApplyResistancesAndPenetration",BindingFlags.Static|BindingFlags.NonPublic);float Hit(Element element)=>(float)method.Invoke(null,new object[]{100f,element,null,stats});Assert.That(Hit(Element.Fire),Is.EqualTo(50));Assert.That(Hit(Element.Cold),Is.EqualTo(50));Assert.That(Hit(Element.Light),Is.EqualTo(50));Assert.That(Hit(Element.Void),Is.EqualTo(100));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void PrefabHasThirtyWarriorPositionsAndNoOldChoiceObjects()
    {
        var view=AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath).GetComponent<PassiveTreeView>();var branch=view.branches.First(x=>x.RouteId==PlayerClassIds.Warrior);
        Assert.That(branch.AllNodes().Count(),Is.EqualTo(10));Assert.That(branch.GetComponentsInChildren<PassiveChoiceSlotView>(true).Length,Is.EqualTo(21));Assert.That(branch.GetComponentsInChildren<PassiveNodeBinding>(true).Length,Is.EqualTo(10));
        foreach(var tier in branch.Tiers){Assert.That(tier.leftSlot.choiceGroupId,Does.EndWith(".left"));Assert.That(tier.rightSlot.choiceGroupId,Does.EndWith(".right"));Assert.That(PassiveTreeDefinition.ChoiceNodes(tier.leftSlot.choiceGroupId).Count(),Is.EqualTo(4));foreach(var slot in new[]{tier.leftSlot,tier.rightSlot}){Assert.That(view.connections.Any(x=>x.To==slot.transform),Is.True);Assert.That(slot.emptyGlow,Is.Not.Null);}}
        Assert.That(view.choicePopup.options.Count,Is.EqualTo(4));Assert.That(view.playerHub.GetComponent<UnityEngine.UI.Image>().sprite,Is.EqualTo(WarriorPassiveReauthoring.Sprite("PlayerHub")));Assert.That(view.layout.Badge(PlayerClassIds.Warrior),Is.EqualTo(WarriorPassiveReauthoring.Sprite("ClassBadge")));Assert.That(PassiveTreeV3Validation.Validate(),Is.Empty);Assert.That(UIAuthoringValidation.ValidateAll(),Is.Empty);
    }
    [Test] public void OneClassBoundsIgnoreHiddenBranchesAndWeaponUnlockIsPreserved()
    {
        WithView((view,p,presentation)=>{Assert.That(presentation.Refresh(),Is.True);Assert.That(view.branches.Count(x=>x.gameObject.activeSelf),Is.EqualTo(1));Assert.That(view.content.sizeDelta.x,Is.LessThan(1100));Assert.That(view.content.sizeDelta.y,Is.LessThan(2700));foreach(string id in PassiveTreeDefinition.ClassIds)Assert.That(view.branches.First(x=>x.RouteId==id).gameObject.activeSelf,Is.EqualTo(id==PlayerClassIds.Warrior));ClassKeystoneProgressionTests.AllocateThirty(p,PlayerClassIds.Warrior);Assert.That(p.TrySpend(ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First()),Is.True);Assert.That(p.TrySelectWeaponTree(WeaponTypeIds.Sword),Is.True);Assert.That(presentation.Refresh(),Is.True);Assert.That(view.branches.First(x=>x.RouteId==WeaponTypeIds.Sword).gameObject.activeSelf,Is.True);Assert.That(view.branches.Count(x=>x.gameObject.activeSelf),Is.EqualTo(2));Assert.That(presentation.WeaponFocused,Is.False);});
    }
    [Test] public void FitZoomDependsOnVisibleBounds()
    {Assert.That(PassiveTreePresentation.CalculateFit(new Vector2(1000,600),new Vector2(900,2200),1.35f),Is.EqualTo(600f/2200).Within(.0001f));Assert.That(PassiveTreePresentation.CalculateFit(new Vector2(1000,600),new Vector2(900,4000),1.35f),Is.LessThan(600f/2200));}
    [Test] public void SlotChoiceUsesExistingRanksExclusivityAndRefund()
    {
        WithView((view,p,presentation)=>{int spine=PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,1);var slot=view.branches.First(x=>x.RouteId==PlayerClassIds.Warrior).Tiers[0].leftSlot;var options=PassiveTreeDefinition.ChoiceNodes(slot.choiceGroupId).ToArray();Assert.That(p.TrySpend(options[0]),Is.False);Assert.That(p.TrySpend(spine),Is.True);Assert.That(p.TrySpend(options[0]),Is.True);Assert.That(p.TrySpend(options[1]),Is.False);Assert.That(p.CopyRanks()[options[0]],Is.EqualTo(1));Assert.That(p.TryRefund(options[0]),Is.True);Assert.That(p.TrySpend(options[1]),Is.True);});
    }
    [Test] public void ExistingSaveAllocationsRoundTripWithoutSchemaChange()
    {
        string path=Path.Combine(Path.GetTempPath(),"WarriorSave-"+Guid.NewGuid().ToString("N")+".json");try{var payload=new GameStatePayload{playerLevel=20,availablePassivePoints=10,encounterStartLife=100,encounterStartMana=100};for(int t=1;t<=10;t++)payload.passiveRanks.Add(new PassiveRankData(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,t),1));for(int i=0;i<RelicInventory.ActiveSlotCount;i++)payload.activeRelicIds.Add(string.Empty);var e=new SaveEnvelope{runId="warrior",runSeed=17,savedAtUtc=DateTime.UtcNow.ToString("O"),payload=payload};File.WriteAllText(path,JsonUtility.ToJson(e));Assert.That(GamePersistence.TryReadFile(path,out var loaded,out var error),Is.True,error);Assert.That(loaded.schemaVersion,Is.EqualTo(13));Assert.That(loaded.payload.passiveRanks.Select(x=>x.stableNodeId),Is.EqualTo(payload.passiveRanks.Select(x=>x.stableNodeId)));}finally{if(File.Exists(path))File.Delete(path);}
    }
    [Test] public void ActualPopupAllocatesFillsAndRefundsSlot()
    {
        WithView((view,p,presentation)=>
        {
            var host=new GameObject("Slot controller test");try
            {
                var owner=host.AddComponent<SkillTreeUI>();void Set(string name,object value)=>typeof(SkillTreeUI).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
                Set("authoredView",view);Set("panel",view.gameObject);Set("points",view.points);Set("xp",view.progression);Set("details",view.details);Set("scroll",view.scroll);Set("progression",p);Set("presentation",presentation);
                void Call(string name)=>typeof(SkillTreeUI).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,null);
                typeof(SkillTreeUI).GetProperty("IsOpen").SetValue(null,true);Call("BindAuthoredTree");Call("Refresh");
                var slot=view.branches.First(x=>x.RouteId==PlayerClassIds.Warrior).Tiers[0].leftSlot;
                slot.button.onClick.Invoke();Assert.That(view.choicePopup.gameObject.activeSelf,Is.True);Assert.That(view.choicePopup.options[0].interactable,Is.False);
                view.choicePopup.gameObject.SetActive(false);p.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,1));Call("Refresh");slot.button.onClick.Invoke();Assert.That(view.choicePopup.options.Take(3).All(x=>x.interactable),Is.True);Assert.That(view.choicePopup.options[3].interactable,Is.False);
                view.choicePopup.options[0].onClick.Invoke();Assert.That(view.choicePopup.gameObject.activeSelf,Is.False);Assert.That(slot.icon.sprite,Is.EqualTo(WarriorPassiveReauthoring.Sprite("Life")));Assert.That(slot.label.text,Is.EqualTo("Maximum Life"));
                owner.RefundSlot(slot);Assert.That(slot.icon.sprite,Is.EqualTo(slot.emptyIcon));Assert.That(slot.label.text,Is.EqualTo("CHOOSE"));
                slot.button.onClick.Invoke();view.choicePopup.options[1].onClick.Invoke();
                var connection=view.connections.First(x=>x.To==slot.transform);
                Assert.That(connection.Line.GetComponent<UnityEngine.UI.Image>().color,Is.EqualTo(new Color(.12f,.48f,.9f,1)),"A shared slot line must light for B/C/subclass choices too.");
                owner.RefundSlot(slot);
                var hidden=view.branches.First(x=>x.RouteId==PlayerClassIds.Mage);Assert.That(hidden.gameObject.activeSelf,Is.False);
            }finally{typeof(SkillTreeUI).GetProperty("IsOpen").SetValue(null,false);UnityEngine.Object.DestroyImmediate(host);}
        });
    }
    [Test] public void PositionEditUndoAndLinesFollowEndpoints()
    {
        var root=PrefabUtility.LoadPrefabContents(PassiveTreePrefabBuilder.PrefabPath);try
        {
            var view=root.GetComponent<PassiveTreeView>();var slot=view.branches.First(x=>x.RouteId==PlayerClassIds.Warrior).Tiers[0].leftSlot;var rect=(RectTransform)slot.transform;Vector2 original=rect.anchoredPosition;
            var line=view.connections.First(x=>x.To==rect);Undo.RecordObject(rect,"Test passive layout move");rect.anchoredPosition+=new Vector2(50,25);Undo.FlushUndoRecordObjects();line.UpdateGeometry();var lineParent=(RectTransform)line.Line.parent;Vector2 a=lineParent.InverseTransformPoint(line.From.position),b=lineParent.InverseTransformPoint(rect.position);Assert.That(line.Line.sizeDelta.x,Is.EqualTo(Vector2.Distance(a,b)).Within(.01f));
            Undo.PerformUndo();Assert.That(rect.anchoredPosition,Is.EqualTo(original));Undo.PerformRedo();Assert.That(rect.anchoredPosition,Is.EqualTo(original+new Vector2(50,25)));Undo.PerformUndo();Assert.That(rect.anchoredPosition,Is.EqualTo(original));
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void WithView(Action<PassiveTreeView,PlayerProgression,PassiveTreePresentation> body)
    {var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath));var player=new GameObject("Warrior state");try{root.SetActive(true);var identity=player.AddComponent<PlayerIdentityState>();identity.BeginNewGame(PlayerClassIds.Warrior);var p=player.AddComponent<PlayerProgression>();Assert.That(p.RestoreProgression(100,0,100,new int[PassiveTreeDefinition.NodeCount]),Is.True);var view=root.GetComponent<PassiveTreeView>();var presentation=root.AddComponent<PassiveTreePresentation>();presentation.Initialize(view,p);body(view,p,presentation);}finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(player);}}
}
