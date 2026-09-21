using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CraftingCurrencyInteractionTests
{
    readonly List<GameObject> cleanup=new();

    [TearDown]
    public void TearDown()
    {
        for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)Object.DestroyImmediate(cleanup[i]);
        cleanup.Clear();SetInstance(typeof(CurrencyInventory),null);SetInstance(typeof(ModManager),null);SetInstance(typeof(GearStatLists),null);PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
    }

    [Test]
    public void Selection_ShowsYellowSourceOverlayAndRaycastTransparentCursorIcon()
    {
        CurrencyInventory inventory=Currency(2);CurrencySlotUI slot=Slot();
        slot.ToggleArmed();slot.RefreshPresentation();
        var canvas=Track(new GameObject("canvas",typeof(RectTransform),typeof(Canvas))).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        CraftingCurrencyCursorUI cursor=CraftingCurrencyCursorUI.Ensure(canvas);cursor.RefreshPresentation();
        Assert.That(inventory.ArmedCurrency,Is.EqualTo(CraftingCurrencyType.NormalToMagic));
        Assert.That(slot.IsSelected,Is.True);Assert.That(slot.SelectionOverlay.color.r,Is.GreaterThan(.9f));Assert.That(slot.SelectionOverlay.color.a,Is.GreaterThan(.2f));Assert.That(slot.SelectionOverlay.raycastTarget,Is.False);
        Assert.That(cursor.IsShowing,Is.True);Assert.That(cursor.Icon.sprite,Is.SameAs(InventoryArtCatalog.Currency(CraftingCurrencyType.NormalToMagic)));Assert.That(cursor.Icon.raycastTarget,Is.False);Assert.That(cursor.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
        cursor.HandleClickTarget(slot.SelectionOverlay.gameObject);Assert.That(inventory.ArmedCurrency,Is.Not.Null,"The initiating/source currency click must not be treated as click-away");
    }

    [Test]
    public void SuccessfulDefaultUse_SpendsOnceAndClearsSelection()
    {
        CreateModManager();CurrencyInventory inventory=Currency(2);Gear gear=NormalGear();inventory.Arm(CraftingCurrencyType.NormalToMagic);
        Assert.That(inventory.TryApplyArmedToGear(gear),Is.True);Assert.That(inventory.Count(CraftingCurrencyType.NormalToMagic),Is.EqualTo(1));Assert.That(inventory.ArmedCurrency,Is.Null);
    }

    [Test]
    public void SuccessfulShiftUse_RetainsSelectionWhileSupplyRemains()
    {
        CreateModManager();CurrencyInventory inventory=Currency(2);Gear gear=NormalGear();inventory.Arm(CraftingCurrencyType.NormalToMagic);
        Assert.That(inventory.TryApplyArmedToGear(gear,true),Is.True);Assert.That(inventory.Count(CraftingCurrencyType.NormalToMagic),Is.EqualTo(1));Assert.That(inventory.ArmedCurrency,Is.EqualTo(CraftingCurrencyType.NormalToMagic));
    }

    [Test]
    public void SuccessfulShiftUse_ClearsSelectionWhenLastUnitIsSpent()
    {
        CreateModManager();CurrencyInventory inventory=Currency(1);Gear gear=NormalGear();inventory.Arm(CraftingCurrencyType.NormalToMagic);
        Assert.That(inventory.TryApplyArmedToGear(gear,true),Is.True);Assert.That(inventory.Count(CraftingCurrencyType.NormalToMagic),Is.Zero);Assert.That(inventory.ArmedCurrency,Is.Null);
    }

    [Test]
    public void ClickAway_CancelsCursorAndSourceSelection()
    {
        CurrencyInventory inventory=Currency(2);CurrencySlotUI slot=Slot();slot.ToggleArmed();
        var canvas=Track(new GameObject("canvas",typeof(RectTransform),typeof(Canvas))).GetComponent<Canvas>();var cursor=CraftingCurrencyCursorUI.Ensure(canvas);cursor.RefreshPresentation();
        cursor.HandleClickTarget(Track(new GameObject("inventory background")));
        slot.RefreshPresentation();cursor.RefreshPresentation();Assert.That(inventory.ArmedCurrency,Is.Null);Assert.That(slot.IsSelected,Is.False);Assert.That(cursor.IsShowing,Is.False);
    }

    [Test]
    public void InvalidGear_CancelsWithoutConsumptionOrMutation()
    {
        CreateModManager();CurrencyInventory inventory=Currency(1,CraftingCurrencyType.MagicToRare);Gear gear=NormalGear();string before=JsonUtility.ToJson(gear);inventory.Arm(CraftingCurrencyType.MagicToRare);
        Assert.That(inventory.TryApplyArmedToGear(gear,true),Is.False);Assert.That(inventory.Count(CraftingCurrencyType.MagicToRare),Is.EqualTo(1));Assert.That(inventory.ArmedCurrency,Is.Null);Assert.That(JsonUtility.ToJson(gear),Is.EqualTo(before));
    }

    [Test]
    public void PanelDisable_ClearsHeldCurrencyState()
    {
        CurrencyInventory inventory=Currency(1);inventory.Arm(CraftingCurrencyType.NormalToMagic);
        var panel=Track(new GameObject("currency panel",typeof(RectTransform),typeof(CurrencyInventoryPanel))).GetComponent<CurrencyInventoryPanel>();panel.BuildAuthoring(null,null);panel.Initialize(null,null);Invoke(panel,"OnDisable");
        Assert.That(inventory.ArmedCurrency,Is.Null);
    }

    CurrencyInventory Currency(int amount,CraftingCurrencyType type=CraftingCurrencyType.NormalToMagic)
    {
        var inventory=Track(new GameObject("currencies",typeof(CurrencyInventory))).GetComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),inventory);inventory.Add(type,amount);return inventory;
    }
    CurrencySlotUI Slot()
    {
        var go=Track(new GameObject("currency slot",typeof(RectTransform),typeof(Image),typeof(Outline),typeof(CurrencySlotUI)));
        var label=Track(new GameObject("count",typeof(RectTransform),typeof(TextMeshProUGUI)));label.transform.SetParent(go.transform,false);
        var slot=go.GetComponent<CurrencySlotUI>();slot.Initialize(CraftingCurrencyType.NormalToMagic,null,label.GetComponent<TextMeshProUGUI>(),go.GetComponent<Outline>());return slot;
    }
    Gear NormalGear()
    {
        var gear=Track(new GameObject("gear",typeof(Gear))).GetComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Normal,40,Element.Phys);
        gear.ApplyMods(new List<RolledMod>{new(StatTypes.Life,1,10,true)});return gear;
    }
    void CreateModManager()
    {
        var statLists=Track(new GameObject("stat lists",typeof(GearStatLists))).GetComponent<GearStatLists>();SetInstance(typeof(GearStatLists),statLists);typeof(GearStatLists).GetField("statPools",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(statLists,GearStatLists.BuildDefaultStatPools());var manager=Track(new GameObject("mods",typeof(ModManager))).GetComponent<ModManager>();
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");database.Initialize();typeof(ModManager).GetField("modDatabase",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,database);SetInstance(typeof(ModManager),manager);
    }
    GameObject Track(GameObject value){cleanup.Add(value);return value;}
    static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void SetInstance(System.Type type,object value)=>type.GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
}
