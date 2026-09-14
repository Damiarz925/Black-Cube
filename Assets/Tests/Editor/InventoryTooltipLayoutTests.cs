using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InventoryTooltipLayoutTests
{
    readonly List<GameObject> cleanup=new();
    [TearDown] public void TearDown(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)Object.DestroyImmediate(cleanup[i]);cleanup.Clear();}

    [Test]
    public void WeaponFormatter_UsesHeadlineOrderDividerAndStableModifierOrder()
    {
        Gear gear=Weapon();
        gear.rolledMods.Add(new RolledMod(StatTypes.Strength,1,8,false));
        gear.rolledMods.Add(new RolledMod(StatTypes.FireRes,2,12,false));
        gear.rolledMods.Add(new RolledMod(StatTypes.AttackSpeed,3,7,false));
        gear.rolledMods.Add(new RolledMod(StatTypes.FireDmg,1,14,true));
        string first=ItemTooltipFormatter.DescribeGear(gear);
        gear.rolledMods.Reverse();
        string shuffled=ItemTooltipFormatter.DescribeGear(gear);
        Assert.That(shuffled,Is.EqualTo(first));
        Assert.That(first.IndexOf("Fire Damage:"),Is.LessThan(first.IndexOf("Crit Chance:")));
        Assert.That(first.IndexOf("Crit Chance:"),Is.LessThan(first.IndexOf("Attacks Per Second:")));
        Assert.That(first.IndexOf("Attacks Per Second:"),Is.LessThan(first.IndexOf(ItemTooltipFormatter.Divider)));
        Assert.That(first,Does.Contain("Attacks Per Second: </b>1.2"));
        Assert.That(first,Does.Not.Contain("Base damage:"));
        Assert.That(first,Does.Contain("◆ LOCKED"));
        Assert.That(first.IndexOf("Fire Damage",System.StringComparison.Ordinal),Is.LessThan(first.IndexOf("Attack Speed",System.StringComparison.Ordinal)));
        Assert.That(first.IndexOf("Attack Speed",System.StringComparison.Ordinal),Is.LessThan(first.IndexOf("Fire Resistance",System.StringComparison.Ordinal)));
        Assert.That(first.IndexOf("Fire Resistance",System.StringComparison.Ordinal),Is.LessThan(first.IndexOf("Strength",System.StringComparison.Ordinal)));
    }

    [Test]
    public void NonWeaponAndRelicFormattersPreserveAllModsAndPutProgressionLast()
    {
        Gear armour=Track(new GameObject("armour")).AddComponent<Gear>();armour.Initialize(LootManager.GearType.BodyArmours,LootManager.GearRarity.Rare,42,Element.Phys);
        armour.rolledMods.Add(new RolledMod(StatTypes.LifeRegeneration,1,3,false));armour.rolledMods.Add(new RolledMod(StatTypes.FlatArmour,1,25,true));
        string item=ItemTooltipFormatter.DescribeGear(armour);Assert.That(item,Does.Contain("ITEM LEVEL 42"));Assert.That(item,Does.Contain(ItemTooltipFormatter.Divider));Assert.That(item,Does.Contain("◆ LOCKED"));
        var relic=new RelicData{id="r",cycle=2,rarity=LootManager.GearRarity.Legendary,craftableThisCycle=true,modifiers=new List<RelicModifier>{new(RelicModifierType.IncreasedExperience,8,false),new(RelicModifierType.MoreDamage,7,true),new(RelicModifierType.MoreAttackSpeed,4,false)}};
        string text=ItemTooltipFormatter.DescribeRelic(relic);Assert.That(text.IndexOf("More Damage"),Is.LessThan(text.IndexOf("More Attack Speed")));Assert.That(text.IndexOf("More Attack Speed"),Is.LessThan(text.IndexOf("Increased Experience")));Assert.That(text,Does.Contain("CURRENT CYCLE / CRAFTABLE"));
    }

    [Test]
    public void VisibleTooltipRefreshesInPlaceOnSuccessfulCraftAndClosesWhenTargetLeavesInventory()
    {
        var inventoryHost=Track(new GameObject("inventory"));var inventory=inventoryHost.AddComponent<Inventory>();SetInstance(typeof(Inventory),inventory);var currency=inventoryHost.AddComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),currency);
        ModManager manager=CreateModManager();Gear gear=Weapon();gear.rolledMods.Add(new RolledMod(StatTypes.FireDmg,1,10,true));gear.RebuildMods();inventory.Add(gear);
        var canvas=Track(new GameObject("canvas",typeof(RectTransform),typeof(Canvas)));var anchor=Track(new GameObject("anchor",typeof(RectTransform)));anchor.transform.SetParent(canvas.transform,false);
        ItemTooltipUI tooltip=ItemTooltipUI.Create(canvas.transform);typeof(ItemTooltipUI).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tooltip,null);tooltip.Show(gear,(RectTransform)anchor.transform,false);string before=tooltip.BodyText;
        currency.Add(CraftingCurrencyType.NormalToMagic);currency.Arm(CraftingCurrencyType.NormalToMagic);
        Assert.That(currency.TryApplyArmedToGear(gear),Is.True);Assert.That(tooltip.gameObject.activeSelf,Is.True);Assert.That(tooltip.BodyText,Is.Not.EqualTo(before));Assert.That(tooltip.BodyText,Does.Contain("◇ UNLOCKED"));
        inventory.Remove(gear);Assert.That(tooltip.gameObject.activeSelf,Is.False);
    }

    [Test]
    public void FailedCraftDoesNotRaiseGearRefreshOrConsumeCurrency()
    {
        Gear gear=Weapon();int events=0;System.Action<Gear> handler=_=>events++;CurrencyInventory.GearChanged+=handler;
        try{var host=Track(new GameObject("currency"));var currency=host.AddComponent<CurrencyInventory>();currency.Add(CraftingCurrencyType.MagicToRare);currency.Arm(CraftingCurrencyType.MagicToRare);Assert.That(currency.TryApplyArmedToGear(gear),Is.False);Assert.That(events,Is.Zero);Assert.That(currency.Count(CraftingCurrencyType.MagicToRare),Is.EqualTo(1));}
        finally{CurrencyInventory.GearChanged-=handler;}
    }

    [Test]
    public void CurrencyPresentation_UsesRequestedSixMappingsAndAncientDistinction()
    {
        Assert.That(CurrencyPresentation.Symbol(CraftingCurrencyType.AddRareModifier),Does.Contain("+"));Assert.That(CurrencyPresentation.Symbol(CraftingCurrencyType.RemoveRareModifier),Does.Contain("−"));
        Assert.That(CurrencyPresentation.Description(CraftingCurrencyType.RerollRareModifier),Does.Contain("Rare or Legendary"));Assert.That(CurrencyPresentation.PrimaryColor(CraftingCurrencyType.RemoveRareModifier).r,Is.GreaterThan(.7f));
        Assert.That(CurrencyPresentation.PrimaryColor(CraftingCurrencyType.AncientReroll),Is.Not.EqualTo(CurrencyPresentation.PrimaryColor(CraftingCurrencyType.RerollRareModifier)));
    }

    [Test]
    public void RareCurrenciesAcceptLegendaryWhilePreservingSixModifierCap()
    {
        var gear=Track(new GameObject("legendary")).AddComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Legendary,80,Element.Phys);
        gear.rolledMods.Add(new RolledMod(StatTypes.Life,1,10,true));gear.rolledMods.Add(new RolledMod(StatTypes.FireRes,1,10,false));gear.rolledMods.Add(new RolledMod(StatTypes.ColdRes,1,10,false));gear.rolledMods.Add(new RolledMod(StatTypes.LightRes,1,10,false));gear.rolledMods.Add(new RolledMod(StatTypes.FlatArmour,1,10,false));
        Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RerollRareModifier,gear),Is.True);Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RemoveRareModifier,gear),Is.True);Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.AddRareModifier,gear),Is.True);
        gear.rolledMods.Add(new RolledMod(StatTypes.AllRes,1,10,false));Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.AddRareModifier,gear),Is.False);
    }

    [Test]
    public void DefaultEnemyDropTable_RollsEquipmentAtHalfAndEveryBasicCurrencyIndependentlyAtTenPercent()
    {
        var table=new EnemyDropTable();int calls=0;
        float[] rolls={.49f,.09f,.11f,.01f,.9f,.099f,.1f};
        EnemyDropResult result=table.Roll(()=>rolls[calls++]);
        Assert.That(calls,Is.EqualTo(7));Assert.That(table.equipmentChance,Is.EqualTo(.5f));Assert.That(result.equipment,Is.True);
        Assert.That(table.CurrencyRules.Count,Is.EqualTo(6));foreach(var rule in table.CurrencyRules)Assert.That(rule.chance,Is.EqualTo(.1f));
        Assert.That(result.currencies,Is.EqualTo(new[]{CraftingCurrencyType.NormalToMagic,CraftingCurrencyType.MagicToRare,CraftingCurrencyType.AddRareModifier}));
        EnemyDropResult miss=table.Roll(()=>.5f);Assert.That(miss.equipment,Is.False);Assert.That(miss.currencies,Is.Empty);
    }

    [Test]
    public void CurrencyRewardClaim_IsExactlyOnceAndBasicIconMappingsAreStable()
    {
        var claim=new CurrencyRewardClaim(CraftingCurrencyType.NormalToMagic);Assert.That(claim.TryClaim(),Is.True);Assert.That(claim.TryClaim(),Is.False);
        var expected=new Dictionary<CraftingCurrencyType,string>{{CraftingCurrencyType.NormalToMagic,"whiteToBlue"},{CraftingCurrencyType.RerollMagic,"RerollBlue"},{CraftingCurrencyType.MagicToRare,"BlueToYellow"},{CraftingCurrencyType.RerollRareModifier,"RerollYellow"},{CraftingCurrencyType.AddRareModifier,"AddYellow"},{CraftingCurrencyType.RemoveRareModifier,"Remove"}};
        foreach(var pair in expected){Assert.That(InventoryArtCatalog.ResourcePath(pair.Key),Does.EndWith(pair.Value));Assert.That(CurrencyPresentation.ValidTarget(pair.Key),Is.Not.Empty);Assert.That(CurrencyPresentation.FailureConditions(pair.Key),Is.Not.Empty);}
        Assert.That(InventoryArtCatalog.ResourcePath(CraftingCurrencyType.AncientReroll),Is.Null);
    }

    [Test]
    public void RelicSlotMigration_PreservesOldTwoSlotSaveAndAddsTwoEmptySlots()
    {
        var host=Track(new GameObject("relic migration"));var inventory=host.AddComponent<RelicInventory>();
        var relics=new List<RelicData>{new(){id="a"},new(){id="b"}};inventory.Restore(relics,3,new[]{0,1});
        Assert.That(RelicInventory.ActiveSlotCount,Is.EqualTo(4));Assert.That(inventory.CopyActiveIndices(),Is.EqualTo(new[]{0,1,-1,-1}));Assert.That(inventory.Active(0),Is.SameAs(relics[0]));Assert.That(inventory.Active(1),Is.SameAs(relics[1]));
    }

    [Test]
    public void SuppliedInventoryAndCurrencyArt_ImportAsUncompressedUiSprites()
    {
        string[] paths={"Assets/Resources/UI/Inventory/InventoryLayout.png","Assets/Resources/UI/Currency/whiteToBlue.png","Assets/Resources/UI/Currency/RerollBlue.png","Assets/Resources/UI/Currency/BlueToYellow.png","Assets/Resources/UI/Currency/RerollYellow.png","Assets/Resources/UI/Currency/AddYellow.png","Assets/Resources/UI/Currency/Remove.png"};
        foreach(string path in paths){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Assert.That(sprite,Is.Not.Null,path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite),path);Assert.That(importer.mipmapEnabled,Is.False,path);Assert.That(importer.alphaIsTransparency,Is.True,path);}
        var layout=AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);Assert.That(layout.width,Is.EqualTo(960));Assert.That(layout.height,Is.EqualTo(1052));
    }

    [Test]
    public void ArtworkLayout_UsesMeasuredRectanglesForEveryBakedTarget()
    {
        Assert.That(InventoryArtLayout.EquipmentSlots.Length,Is.EqualTo(8));Assert.That(InventoryArtLayout.RelicSlots.Length,Is.EqualTo(4));Assert.That(InventoryArtLayout.OrdinaryCurrencySlots.Length,Is.EqualTo(6));Assert.That(InventoryArtLayout.AncientCurrencySlots.Length,Is.EqualTo(6));
        float[] equipmentCenters={112.5f,216.5f,322.5f,428.5f,534.5f,640f,746f,852f};
        for(int i=0;i<equipmentCenters.Length;i++)Assert.That(InventoryArtLayout.EquipmentSlots[i].center.x*InventoryArtLayout.Width,Is.EqualTo(equipmentCenters[i]).Within(.01f),"equipment "+i);
        float[] relicCenters={213.5f,387f,571.5f,748f};for(int i=0;i<relicCenters.Length;i++)Assert.That(InventoryArtLayout.RelicSlots[i].center.x*InventoryArtLayout.Width,Is.EqualTo(relicCenters[i]).Within(.01f),"relic "+i);
        float[] currencyCenters={120.5f,265f,408.5f,553f,697f,841f};for(int i=0;i<currencyCenters.Length;i++){Assert.That(InventoryArtLayout.OrdinaryCurrencySlots[i].center.x*InventoryArtLayout.Width,Is.EqualTo(currencyCenters[i]).Within(.01f));Assert.That(InventoryArtLayout.AncientCurrencySlots[i].center.x*InventoryArtLayout.Width,Is.EqualTo(currencyCenters[i]).Within(.01f));Assert.That(InventoryArtLayout.OrdinaryCountBoxes[i].xMax,Is.LessThanOrEqualTo(InventoryArtLayout.OrdinaryCurrencySlots[i].xMax));Assert.That(InventoryArtLayout.AncientCountBoxes[i].xMax,Is.LessThanOrEqualTo(InventoryArtLayout.AncientCurrencySlots[i].xMax));}
        Assert.That(InventoryArtLayout.DismantleButton,Is.EqualTo(InventoryArtLayout.P(568,88,724,130)));Assert.That(InventoryArtLayout.HighlightButton,Is.EqualTo(InventoryArtLayout.P(735,88,891,130)));Assert.That(InventoryArtLayout.CloseButton,Is.EqualTo(InventoryArtLayout.P(833,37,910,86)));
        Assert.That(InventoryArtLayout.InventoryBounds,Is.EqualTo(InventoryArtLayout.P(72,302,893,477)));
    }

    [Test]
    public void BakedButtonTargets_HideLegacyVisualsAndKeepExactHitRectangles()
    {
        var go=Track(new GameObject("legacy button",typeof(RectTransform),typeof(Image),typeof(Button)));
        var label=Track(new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)));label.transform.SetParent(go.transform,false);label.GetComponent<TextMeshProUGUI>().text="DUPLICATE";
        var button=go.GetComponent<Button>();BakedInventoryButton.Configure(button,InventoryArtLayout.DismantleButton);
        var rect=(RectTransform)go.transform;Assert.That(rect.anchorMin,Is.EqualTo(InventoryArtLayout.DismantleButton.min));Assert.That(rect.anchorMax,Is.EqualTo(InventoryArtLayout.DismantleButton.max));
        Assert.That(label.activeSelf,Is.False);Assert.That(button.transition,Is.EqualTo(Selectable.Transition.None));Assert.That(go.GetComponent<Image>().color.a,Is.LessThan(.01f));Assert.That(go.GetComponent<BakedInventoryButtonFeedback>(),Is.Not.Null);Assert.That(go.GetComponent<InventoryArtworkHotspot>(),Is.Not.Null);
    }

    [Test]
    public void EquipmentPanel_LocksArtworkAspectAndUsesPerSlotAnchors()
    {
        var panel=Track(new GameObject("inventory panel",typeof(RectTransform),typeof(Image),typeof(InventoryEquipmentPanelUI)));panel.GetComponent<InventoryEquipmentPanelUI>().Build(null);
        var rect=(RectTransform)panel.transform;Assert.That(rect.sizeDelta.x,Is.EqualTo(690f));Assert.That(rect.sizeDelta.y/rect.sizeDelta.x,Is.EqualTo(InventoryArtLayout.Height/InventoryArtLayout.Width).Within(.0001f));Assert.That(rect.anchorMin,Is.EqualTo(new Vector2(0,1)));Assert.That(rect.anchorMax,Is.EqualTo(new Vector2(0,1)));
        Transform equipment=panel.transform.Find("Equipped gear layout");Assert.That(equipment.childCount,Is.EqualTo(8));for(int i=0;i<8;i++){var slot=(RectTransform)equipment.GetChild(i);Assert.That(slot.anchorMin,Is.EqualTo(InventoryArtLayout.EquipmentSlots[i].min));Assert.That(slot.anchorMax,Is.EqualTo(InventoryArtLayout.EquipmentSlots[i].max));var interior=(RectTransform)slot.Find("Occupied slot interior");Assert.That(interior.anchorMin,Is.EqualTo(new Vector2(.12f,.13f)));Assert.That(interior.anchorMax,Is.EqualTo(new Vector2(.88f,.87f)));}
        foreach(var slot in equipment.GetComponent<EquipmentStatsUI>().slots){Assert.That(slot.elementBadge.rectTransform.anchorMin,Is.EqualTo(new Vector2(.82f,.18f)));Assert.That(slot.elementBadge.rectTransform.sizeDelta,Is.EqualTo(new Vector2(4,4)));}
    }

    [Test]
    public void InventorySlots_UseEightColumnsAspectFitMaskingAndNoRarityBar()
    {
        Assert.That(InventoryUI.GridColumnCount,Is.EqualTo(8));Assert.That(InventoryUI.GridCellSize,Is.EqualTo(new Vector2(59,59)));
        var root=Track(new GameObject("slot",typeof(RectTransform),typeof(Image),typeof(ItemSlotUI)));
        var icon=Track(new GameObject("icon",typeof(RectTransform),typeof(Image)));icon.transform.SetParent(root.transform,false);
        var rarity=Track(new GameObject("rarity",typeof(RectTransform),typeof(Image)));rarity.transform.SetParent(root.transform,false);
        var glyph=Track(new GameObject("old silhouette",typeof(RectTransform),typeof(EquipmentGlyph)));glyph.transform.SetParent(root.transform,false);
        var slot=root.GetComponent<ItemSlotUI>();SetField(slot,"iconImage",icon.GetComponent<Image>());SetField(slot,"backgroundImage",root.GetComponent<Image>());SetField(slot,"rarityOverlay",rarity.GetComponent<Image>());
        var texture=new Texture2D(64,32);var sprite=Sprite.Create(texture,new Rect(0,0,64,32),new Vector2(.5f,.5f));
        var gear=Weapon();gear.SetRarity(LootManager.GearRarity.Rare);slot.Setup(gear,sprite);
        Assert.That(icon.GetComponent<Image>().preserveAspect,Is.True);Assert.That(icon.GetComponent<Image>().color,Is.EqualTo(Color.white));Assert.That(root.GetComponent<RectMask2D>(),Is.Not.Null);
        Assert.That(rarity.activeSelf,Is.False);Assert.That(glyph.GetComponent<EquipmentGlyph>().enabled,Is.False);Assert.That(slot.BackgroundImage.color,Is.EqualTo(ItemSlotUI.RarityBackground(LootManager.GearRarity.Rare)));Assert.That(root.GetComponent<Image>().color.a,Is.LessThan(.01f));
        Assert.That(slot.BackgroundImage.rectTransform.anchorMin,Is.EqualTo(new Vector2(.1f,.12f)));Assert.That(slot.BackgroundImage.rectTransform.anchorMax,Is.EqualTo(new Vector2(.9f,.9f)));
        Assert.That(slot.ElementBadge.rectTransform.anchorMin,Is.EqualTo(new Vector2(.82f,.18f)));Assert.That(slot.ElementBadge.rectTransform.anchorMax,Is.EqualTo(new Vector2(.82f,.18f)));Assert.That(slot.ElementBadge.rectTransform.sizeDelta,Is.EqualTo(new Vector2(4,4)));
        Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);
    }

    [Test]
    public void CurrencyCounts_ArePlainNumbersInsideBakedCountBox()
    {
        var host=Track(new GameObject("currency",typeof(CurrencyInventory)));var currency=host.GetComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),currency);currency.Add(CraftingCurrencyType.NormalToMagic);
        var go=Track(new GameObject("currency slot",typeof(RectTransform),typeof(Image),typeof(Outline),typeof(CurrencySlotUI)));
        var labelObject=Track(new GameObject("count",typeof(RectTransform),typeof(TextMeshProUGUI)));labelObject.transform.SetParent(go.transform,false);
        var rect=(RectTransform)labelObject.transform;InventoryArtLayout.Apply(rect,InventoryArtLayout.Relative(InventoryArtLayout.OrdinaryCountBoxes[0],InventoryArtLayout.OrdinaryCurrencySlots[0]));
        var slot=go.GetComponent<CurrencySlotUI>();slot.Initialize(CraftingCurrencyType.NormalToMagic,null,labelObject.GetComponent<TextMeshProUGUI>(),go.GetComponent<Outline>());
        Assert.That(slot.DisplayText,Is.EqualTo("1"));Assert.That(slot.DisplayText,Does.Not.Contain("x"));Assert.That(rect.anchorMin.x,Is.GreaterThan(.5f));Assert.That(rect.anchorMax.x,Is.LessThanOrEqualTo(1f));Assert.That(rect.anchorMax.y,Is.LessThan(.3f));
        currency.Add(CraftingCurrencyType.AncientReroll);slot.Initialize(CraftingCurrencyType.AncientReroll,null,labelObject.GetComponent<TextMeshProUGUI>(),go.GetComponent<Outline>());Assert.That(slot.DisplayText,Is.EqualTo("1"));
        var panelObject=Track(new GameObject("currency panel",typeof(RectTransform),typeof(CurrencyInventoryPanel)));panelObject.GetComponent<CurrencyInventoryPanel>().Initialize(null,null);
        var entries=panelObject.GetComponentsInChildren<CurrencySlotUI>(true);Assert.That(entries.Length,Is.EqualTo(12));foreach(var entry in entries){var images=entry.GetComponentsInChildren<Image>(true);Assert.That(images.Length,Is.EqualTo(2),entry.name);Assert.That(entry.GetComponent<Image>().color.a,Is.LessThan(.01f),entry.name);Assert.That(entry.SelectionOverlay.raycastTarget,Is.False);int index=CurrencyInventory.IsAncient(entry.Type)?(int)entry.Type-(int)CraftingCurrencyType.AncientNormalToMagic:System.Array.IndexOf(EnemyDropTable.OrdinaryTypes(),entry.Type);Rect expected=(CurrencyInventory.IsAncient(entry.Type)?InventoryArtLayout.AncientCurrencySlots:InventoryArtLayout.OrdinaryCurrencySlots)[index];Assert.That(((RectTransform)entry.transform).anchorMin,Is.EqualTo(expected.min));Assert.That(((RectTransform)entry.transform).anchorMax,Is.EqualTo(expected.max));var count=entry.GetComponentInChildren<TMP_Text>(true).rectTransform;Assert.That(count.anchorMin.x,Is.GreaterThan(.55f));Assert.That(count.anchorMax.x,Is.LessThanOrEqualTo(1f));}
    }

    [Test]
    public void RelicSlot_ShowsOneEmptyWordAndOccupiedStateReplacesIt()
    {
        var inventoryHost=Track(new GameObject("relic inventory",typeof(RelicInventory)));RelicData relic=RelicInventory.Instance.BeginNewCycle();
        var slotObject=Track(new GameObject("relic slot",typeof(RectTransform),typeof(Image),typeof(ActiveRelicSlotUI)));
        var labelObject=Track(new GameObject("label",typeof(RectTransform),typeof(TextMeshProUGUI)));labelObject.transform.SetParent(slotObject.transform,false);
        var slot=slotObject.GetComponent<ActiveRelicSlotUI>();slot.Initialize(0,labelObject.GetComponent<TextMeshProUGUI>());Assert.That(slot.DisplayText,Is.EqualTo("Empty"));
        RelicInventory.Instance.Equip(relic,0);slot.Refresh();Assert.That(slot.DisplayText,Does.Not.Contain("Empty"));Assert.That(slot.DisplayText,Does.Not.Contain("RELIC SLOT"));Assert.That(slotObject.GetComponent<Image>().color.a,Is.EqualTo(1f));
    }

    Gear Weapon(){var gear=Track(new GameObject("weapon")).AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,20,Element.Fire);gear.BaseDamage=12;gear.BaseAttackSpeed=1.2f;gear.BaseCritChance=.05f;return gear;}
    ModManager CreateModManager(){var lists=Track(new GameObject("stat lists")).AddComponent<GearStatLists>();SetInstance(typeof(GearStatLists),lists);typeof(GearStatLists).GetField("statPools",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(lists,GearStatLists.BuildDefaultStatPools());var manager=Track(new GameObject("mods")).AddComponent<ModManager>();SetInstance(typeof(ModManager),manager);var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");database.Initialize();typeof(ModManager).GetField("modDatabase",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,database);return manager;}
    GameObject Track(GameObject value){cleanup.Add(value);return value;}
    static void SetField(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void SetInstance(System.Type type,object value)=>type.GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
}
