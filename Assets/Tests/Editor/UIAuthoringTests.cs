using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIAuthoringTests
{
    [Test] public void PassiveAssetsLoadWithExpectedTiersAndUniqueIds()
    {
        PassiveTreeDatabaseSO database=AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(PassiveTreeAuthoringMigration.DatabasePath);Assert.That(database,Is.Not.Null);Assert.That(database.ClassBranches.Count,Is.EqualTo(6));Assert.That(database.WeaponBranches.Count,Is.EqualTo(6));
        Assert.That(database.ClassBranches.All(x=>x!=null&&x.TierCount==10),Is.True);Assert.That(database.WeaponBranches.All(x=>x!=null&&x.TierCount==5),Is.True);
        string[] ids=database.ClassBranches.Cast<PassiveBranchDataSO>().Concat(database.WeaponBranches).SelectMany(x=>x.AllAuthoredNodes()).Where(x=>x!=null&&!string.IsNullOrEmpty(x.StableId)).Select(x=>x.StableId).ToArray();Assert.That(ids.Distinct().Count(),Is.EqualTo(ids.Length));Assert.That(UIAuthoringValidation.ValidateAll(),Is.Empty);
    }

    [Test] public void SubclassGenericIconsAndRuntimeValuesComeFromAssets()
    {
        PassiveClassBranchSO warrior=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(PassiveTreeAuthoringMigration.ClassFolder+"/SO_Warrior_Branch.asset");Assert.That(warrior,Is.Not.Null);PassiveClassTierData tier=warrior.Tiers[0];
        Assert.That(tier.Left.GenericNodes.Count(),Is.EqualTo(3));Assert.That(tier.Left.SubclassA.StableId,Is.Not.EqualTo(tier.Left.SubclassB.StableId));Assert.That(tier.Left.SubclassA.LogicalSlotId,Is.EqualTo(tier.Left.SubclassB.LogicalSlotId));
        Assert.That(PassiveTreeDefinition.Database,Is.Not.Null);int id=PassiveTreeDefinition.NodeId(tier.Spine.StableId);Assert.That(id,Is.GreaterThanOrEqualTo(0));Assert.That(PassiveTreeDefinition.Node(id).Effects[0].Amount,Is.EqualTo(tier.Spine.Effects[0].Value));
        Assert.That(PassiveTreeDefinition.Database.IconLibrary.Resolve(tier.Spine),Is.Not.Null);Assert.That(PassiveTreeDefinition.SubclassEffects(warrior.SubclassAId,PassiveTreeDefinition.Node(PassiveTreeDefinition.NodeId(tier.Left.SubclassA.LogicalSlotId))),Is.Not.Empty);
    }

    [Test] public void CustomIconOverrideWinsOverAutomaticMapping()
    {
        PassiveClassBranchSO warrior=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(PassiveTreeAuthoringMigration.ClassFolder+"/SO_Warrior_Branch.asset");PassiveAuthoredNode node=warrior.Tiers[0].Spine;Sprite automatic=PassiveTreeDefinition.Database.IconLibrary.Resolve(node);Assert.That(automatic,Is.Not.Null);
        SerializedObject serialized=new(warrior);SerializedProperty tiers=serialized.FindProperty("tiers");SerializedProperty spine=tiers.GetArrayElementAtIndex(0).FindPropertyRelative("spine");spine.FindPropertyRelative("iconMode").enumValueIndex=(int)PassiveIconMode.Custom;spine.FindPropertyRelative("iconOverride").objectReferenceValue=automatic;serialized.ApplyModifiedPropertiesWithoutUndo();
        try{Assert.That(PassiveTreeDefinition.Database.IconLibrary.Resolve(node),Is.SameAs(automatic));}
        finally{spine.FindPropertyRelative("iconMode").enumValueIndex=(int)PassiveIconMode.Auto;spine.FindPropertyRelative("iconOverride").objectReferenceValue=null;serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(warrior);}
    }

    [Test] public void MovingPassiveViewDoesNotChangeLogicalIdentityOrEffect()
    {
        GameObject root=PrefabUtility.LoadPrefabContents(PassiveTreePrefabBuilder.PrefabPath);try{PassiveNodeBinding binding=root.GetComponentsInChildren<PassiveNodeBinding>(true).First();string slot=binding.LogicalSlotId;int id=PassiveTreeDefinition.NodeId(slot);float value=PassiveTreeDefinition.Node(id).Magnitude;RectTransform rect=binding.transform as RectTransform;rect.anchoredPosition+=new Vector2(37,-19);Assert.That(binding.LogicalSlotId,Is.EqualTo(slot));Assert.That(PassiveTreeDefinition.NodeId(binding.LogicalSlotId),Is.EqualTo(id));Assert.That(PassiveTreeDefinition.Node(id).Magnitude,Is.EqualTo(value));}finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    [Test] public void ProductionPrefabsContainRequiredSerializedViews()
    {
        GameObject battle=AssetDatabase.LoadAssetAtPath<GameObject>(PersistentUIAuthoringInstaller.GameplayPrefabPath);Assert.That(battle,Is.Not.Null);PaperBattleHUD hud=battle.GetComponentInChildren<PaperBattleHUD>(true);Assert.That(hud,Is.Not.Null);Assert.That(hud.GetComponent<GameplayHUDView>(),Is.Not.Null);Assert.That(hud.GetComponent<SkillTreeUI>(),Is.Not.Null);Assert.That(battle.GetComponentInChildren<PassiveTreeView>(true),Is.Not.Null);
        GameObject tree=AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath);PassiveTreeView view=tree.GetComponent<PassiveTreeView>();Assert.That(view,Is.Not.Null);Assert.That(view.scroll,Is.Not.Null);Assert.That(view.closeButton,Is.Not.Null);Assert.That(view.AllNodes.Count(),Is.GreaterThan(750));
    }

    [Test] public void InventoryPrefabContainsAuthoredFixedLayoutAndBindings()
    {
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(InventoryAuthoringBuilder.PrefabPath);Assert.That(prefab,Is.Not.Null);InventoryView view=prefab.GetComponent<InventoryView>();Assert.That(view,Is.Not.Null);Assert.That(view.itemGridRoot,Is.Not.Null);Assert.That(view.equipmentRoot,Is.Not.Null);Assert.That(view.ordinaryCurrencyRoot,Is.Not.Null);Assert.That(view.ancientCurrencyRoot,Is.Not.Null);Assert.That(view.relicRoot,Is.Not.Null);Assert.That(view.filterRoot,Is.Not.Null);Assert.That(view.modFilterRoot,Is.Not.Null);Assert.That(view.gearToggle,Is.Not.Null);Assert.That(view.relicToggle,Is.Not.Null);Assert.That(view.equipmentSlots.Count,Is.GreaterThanOrEqualTo(8));Assert.That(view.activeRelicSlots.Count,Is.EqualTo(RelicInventory.ActiveSlotCount));Assert.That(view.currencySlots.Count,Is.EqualTo(13));Assert.That(view.fragmentLabels.Count,Is.EqualTo(2));
    }

    [Test] public void GameplayPanelsAndTooltipsAreAuthoredAndBound()
    {
        GameObject battle=AssetDatabase.LoadAssetAtPath<GameObject>(PersistentUIAuthoringInstaller.GameplayPrefabPath);Assert.That(battle,Is.Not.Null);
        Assert.That(battle.GetComponentInChildren<SkillSelectionView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<SubclassView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<ChallengeView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<EndgameCraftingView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<RebirthView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<StatsView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<EnemyInspectionView>(true),Is.Not.Null);
        Assert.That(battle.GetComponentInChildren<StatusHUDView>(true),Is.Not.Null);Assert.That(battle.GetComponentInChildren<StatusHUD>(true),Is.Not.Null);Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(StatusHUDAuthoringBuilder.PrefabPath),Is.Not.Null);Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(StatusHUDAuthoringBuilder.BadgePrefabPath),Is.Not.Null);
        string[] panels={GameplayPanelAuthoringBuilder.SkillPrefabPath,GameplayPanelAuthoringBuilder.SubclassPrefabPath,GameplayPanelAuthoringBuilder.RebirthPrefabPath,GameplayPanelAuthoringBuilder.ChallengePrefabPath,GameplayPanelAuthoringBuilder.CraftingPrefabPath,GameplayPanelAuthoringBuilder.StatsPrefabPath,GameplayPanelAuthoringBuilder.EnemyInspectionPrefabPath};foreach(string path in panels)Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path),Is.Not.Null,path);
        GameObject item=AssetDatabase.LoadAssetAtPath<GameObject>(TooltipAuthoringBuilder.ItemTooltipPrefabPath);Assert.That(item,Is.Not.Null);Assert.That(item.GetComponent<ItemTooltipUI>(),Is.Not.Null);
        GameObject currency=AssetDatabase.LoadAssetAtPath<GameObject>(TooltipAuthoringBuilder.CurrencyTooltipPrefabPath);Assert.That(currency,Is.Not.Null);Assert.That(currency.GetComponent<CurrencyTooltipUI>(),Is.Not.Null);
        GameObject relic=AssetDatabase.LoadAssetAtPath<GameObject>(TooltipAuthoringBuilder.RelicTooltipPrefabPath);Assert.That(relic,Is.Not.Null);Assert.That(relic.GetComponent<RelicTooltipUI>(),Is.Not.Null);
    }

    [Test] public void ButtonStatesPreserveAuthoredPlacement()
    {
        UIVisualLibrarySO library=AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath);GameObject eventObject=new("EventSystem",typeof(EventSystem));GameObject go=new("Button",typeof(RectTransform),typeof(Image),typeof(Button),typeof(UIButtonVisualController));try{RectTransform rect=go.transform as RectTransform;rect.anchoredPosition=new Vector2(123,456);Button button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();UIButtonVisualController controller=go.GetComponent<UIButtonVisualController>();controller.SetStyle(library.defaultButtonStyle);Vector2 authored=rect.anchoredPosition;PointerEventData pointer=new(eventObject.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left};controller.OnPointerEnter(pointer);Assert.That(controller.CurrentState,Is.EqualTo(UIButtonVisualState.Hovered));controller.OnPointerDown(pointer);Assert.That(controller.CurrentState,Is.EqualTo(UIButtonVisualState.Pressed));controller.SetSelected(true);Assert.That(controller.CurrentState,Is.EqualTo(UIButtonVisualState.Selected));button.interactable=false;controller.Refresh();Assert.That(controller.CurrentState,Is.EqualTo(UIButtonVisualState.Disabled));Assert.That(rect.anchoredPosition,Is.EqualTo(authored));}finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(eventObject);}
    }
}
