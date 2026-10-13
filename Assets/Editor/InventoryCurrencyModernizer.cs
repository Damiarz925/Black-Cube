using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryCurrencyModernizer
{
    static readonly (string name,CraftingCurrencyType type)[] AncientSlots={
        ("AncientNormalToMagic",CraftingCurrencyType.AncientNormalToMagic),("AncientRerollMagic",CraftingCurrencyType.AncientRerollMagic),("AncientAddMagic",CraftingCurrencyType.AncientAddMagicModifier),
        ("AncientMagicToRare",CraftingCurrencyType.AncientMagicToRare),("AncientRerollRare",CraftingCurrencyType.AncientRerollRareModifier),("AncientAddRare",CraftingCurrencyType.AncientAddRareModifier),
        ("AncientRemoveModifier",CraftingCurrencyType.AncientRemoveModifier),("AncientFocusPrefix",CraftingCurrencyType.AncientPrefixFocus),("AncientFocusSuffix",CraftingCurrencyType.AncientSuffixFocus)};
    static readonly (string name,CraftingCurrencyType type)[] OrdinarySlots={
        ("NormalToMagic",CraftingCurrencyType.NormalToMagic),("RerollMagic",CraftingCurrencyType.RerollMagic),("MagicAdd",CraftingCurrencyType.AddMagicModifier),
        ("MagicToRare",CraftingCurrencyType.MagicToRare),("RareReroll",CraftingCurrencyType.RerollRareModifier),("RareAdd",CraftingCurrencyType.AddRareModifier),
        ("Remove",CraftingCurrencyType.RemoveRareModifier),("PrefixFocus",CraftingCurrencyType.PrefixFocus),("SuffixFocus",CraftingCurrencyType.SuffixFocus)};

    [MenuItem("Black-Cube/UI Authoring/Modernize Inventory Currency Grid")]
    public static void RunFromCommandLine()
    {
        GameObject root=PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            InventoryUI inventory=root.GetComponentInChildren<InventoryUI>(true);if(inventory==null)throw new InvalidOperationException("Production InventoryUI missing.");
            Modernize(inventory);PrefabUtility.SaveAsPrefabAsset(inventory.gameObject,InventoryAuthoringBuilder.PrefabPath);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("INVENTORY MODERNIZER: production inventory updated.");
    }

    static void Modernize(InventoryUI inventory)
    {
        InventoryView view=inventory.GetComponent<InventoryView>()??inventory.gameObject.AddComponent<InventoryView>();view.authoredRoot=(RectTransform)inventory.transform;view.background=inventory.GetComponent<Image>();
        if(view.background!=null){view.background.sprite=InventoryArtCatalog.Layout;view.background.color=Color.white;view.background.preserveAspect=true;}
        view.equipmentRoot=Find<RectTransform>(inventory.transform,"Equipped gear layout");view.ordinaryCurrencyRoot=Find<RectTransform>(inventory.transform,"Ordinary currency");view.ancientCurrencyRoot=Find<RectTransform>(inventory.transform,"Ancient relic currency");
        view.relicRoot=Find<RectTransform>(inventory.transform,"Relic inventory");
        if(view.ordinaryCurrencyRoot==null||view.ancientCurrencyRoot==null)throw new InvalidOperationException("Currency roots missing.");
        BindCurrencies(view);ConfigureGrid(view,inventory);ConfigureFilters(view,inventory);ConfigureEquipment(view,inventory);EditorUtility.SetDirty(view);EditorUtility.SetDirty(inventory);
    }

    static void BindCurrencies(InventoryView view)
    {
        var slots=new List<CurrencySlotUI>();var ancientBorders=new List<Transform>(9);
        for(int i=0;i<9;i++)
        {
            Transform ancientIcon=FindTransform(view.ancientCurrencyRoot,AncientSlots[i].name);if(ancientIcon==null)throw new InvalidOperationException("Missing "+AncientSlots[i].name);Transform ancientBorder=ancientIcon.parent;
            ancientBorders.Add(ancientBorder);ConfigureCurrency(ancientIcon,ancientBorder,AncientSlots[i].type,i,view.ancientCurrencyRoot,slots);
        }
        foreach(Transform child in view.ordinaryCurrencyRoot.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        for(int i=0;i<9;i++)
        {
            GameObject clone=UnityEngine.Object.Instantiate(ancientBorders[i].gameObject,view.ordinaryCurrencyRoot);clone.name=OrdinarySlots[i].name+"Border";
            CurrencySlotUI clonedSlot=clone.GetComponentInChildren<CurrencySlotUI>(true);if(clonedSlot==null)throw new InvalidOperationException("Cloned currency slot is missing for "+OrdinarySlots[i].name);
            Transform ordinaryIcon=clonedSlot.transform;ordinaryIcon.name=OrdinarySlots[i].name;ConfigureCurrency(ordinaryIcon,clone.transform,OrdinarySlots[i].type,i,view.ordinaryCurrencyRoot,slots);
        }
        foreach(var element in view.ancientCurrencyRoot.GetComponentsInChildren<UIAuthoringElement>(true).Concat(view.ordinaryCurrencyRoot.GetComponentsInChildren<UIAuthoringElement>(true)).ToArray())UnityEngine.Object.DestroyImmediate(element);
        foreach(var slot in slots){var element=slot.gameObject.AddComponent<UIAuthoringElement>();element.Configure("inventory.currency."+slot.Type.ToString().ToLowerInvariant(),slot.Type+" Currency",slot.transform as RectTransform,slot.GetComponent<Image>(),slot.GetComponentInChildren<TMP_Text>(true));}
        view.currencySlots=slots;
    }

    static void ConfigureCurrency(Transform icon,Transform border,CraftingCurrencyType type,int index,RectTransform root,List<CurrencySlotUI> slots)
    {
        border.SetParent(root,false);Rect cell=InventoryArtLayout.CurrencyRowForEditor(index);InventoryArtLayout.Apply((RectTransform)border,cell);
        Image borderImage=border.GetComponent<Image>()??border.gameObject.AddComponent<Image>();borderImage.sprite=InventoryArtCatalog.CurrencyBorderNormal;borderImage.color=Color.white;borderImage.type=Image.Type.Simple;
        RectTransform iconRect=(RectTransform)icon;iconRect.anchorMin=Vector2.zero;iconRect.anchorMax=Vector2.one;iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;
        foreach(Transform child in icon.GetComponentsInChildren<Transform>(true).Where(x=>x!=icon&&x.name=="Selected currency overlay").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        Image image=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();Sprite sprite=InventoryArtCatalog.Currency(type);if(sprite!=null)image.sprite=sprite;image.preserveAspect=true;image.color=Color.white;
        CurrencySlotUI slot=icon.GetComponent<CurrencySlotUI>()??icon.gameObject.AddComponent<CurrencySlotUI>();TMP_Text label=icon.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if(label==null){var go=new GameObject("Count",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(icon,false);label=go.GetComponent<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;}
        InventoryArtLayout.Apply(label.rectTransform,InventoryArtLayout.Relative(InventoryArtLayout.CountRowForEditor(index),cell));slot.Initialize(type,null,label,icon.GetComponent<Outline>());slot.ConfigureBorders(borderImage,InventoryArtCatalog.CurrencyBorderNormal,InventoryArtCatalog.CurrencyBorderHovered,InventoryArtCatalog.CurrencyBorderSelected);slots.Add(slot);EditorUtility.SetDirty(slot);
    }

    static void ConfigureGrid(InventoryView view,InventoryUI inventory)
    {
        view.itemGridRoot=Find<RectTransform>(inventory.transform,"Compact inventory grid")??view.itemGridRoot;if(view.itemGridRoot==null)throw new InvalidOperationException("Inventory grid missing.");
        GridLayoutGroup layout=view.itemGridRoot.GetComponent<GridLayoutGroup>();if(layout!=null)UnityEngine.Object.DestroyImmediate(layout);ContentSizeFitter fitter=view.itemGridRoot.GetComponent<ContentSizeFitter>();if(fitter!=null)UnityEngine.Object.DestroyImmediate(fitter);
        ScrollRect scroll=view.itemGridRoot.GetComponentInParent<ScrollRect>();view.inventoryScroll=scroll;if(scroll==null)return;scroll.horizontal=false;scroll.content=view.itemGridRoot;InventoryArtLayout.Apply((RectTransform)scroll.transform,InventoryArtLayout.InventoryBounds);
        Scrollbar bar=scroll.verticalScrollbar;
        if(bar==null){var go=new GameObject("Inventory Scrollbar",typeof(RectTransform),typeof(Image),typeof(Scrollbar));go.transform.SetParent(scroll.transform,false);var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(.982f,0);rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;Image track=go.GetComponent<Image>();track.color=new Color(.08f,.08f,.08f,.9f);bar=go.GetComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(go.transform,false);var hr=(RectTransform)handle.transform;hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=new Vector2(1,1);hr.offsetMax=new Vector2(-1,-1);handle.GetComponent<Image>().color=new Color(.8f,.45f,.08f,.9f);bar.handleRect=hr;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;}
        view.inventoryScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    static void ConfigureFilters(InventoryView view,InventoryUI inventory)
    {
        InventoryTypeFilterUI controller=inventory.GetComponent<InventoryTypeFilterUI>()??inventory.gameObject.AddComponent<InventoryTypeFilterUI>();Transform old=inventory.transform.Find("Inventory type filters");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Inventory type filters",typeof(RectTransform));root.transform.SetParent(inventory.transform,false);InventoryArtLayout.Apply((RectTransform)root.transform,InventoryArtLayout.P(45,1075,1155,1148));
        var bindings=new List<InventoryTypeFilterUI.Binding>();var types=(LootManager.GearType[])Enum.GetValues(typeof(LootManager.GearType));
        for(int i=0;i<types.Length;i++){float gap=.008f,width=(1f-gap*8)/9f,x=i*(width+gap);Button button=MakeButton(root.transform,ItemSlotUI.DisplayType(types[i]).ToUpperInvariant());RectTransform rect=(RectTransform)button.transform;rect.anchorMin=new Vector2(x,0);rect.anchorMax=new Vector2(x+width,1);rect.offsetMin=rect.offsetMax=Vector2.zero;bindings.Add(new InventoryTypeFilterUI.Binding{type=types[i],button=button});}
        controller.SetBindings(bindings);view.itemTypeFilters=bindings.Select(x=>x.button).ToList();CurrencyInventoryPanel panel=inventory.GetComponent<CurrencyInventoryPanel>();view.gearToggle=panel?.GearTab??view.gearToggle;view.relicToggle=panel?.RelicTab??view.relicToggle;
        if(view.gearToggle!=null)InventoryArtLayout.Apply((RectTransform)view.gearToggle.transform,InventoryArtLayout.P(455,1025,585,1068));if(view.relicToggle!=null)InventoryArtLayout.Apply((RectTransform)view.relicToggle.transform,InventoryArtLayout.P(615,1025,745,1068));
    }

    static void ConfigureEquipment(InventoryView view,InventoryUI inventory)
    {
        EquipmentStatsUI equipment=inventory.GetComponentInChildren<EquipmentStatsUI>(true);if(equipment==null||equipment.slots==null)return;var list=equipment.slots.Where(x=>x!=null).ToList();EquipmentStatsUI.Slot weapon=list.FirstOrDefault(x=>x.type==LootManager.GearType.Weapons);
        if(list.All(x=>x.type!=LootManager.GearType.OffHands)&&weapon?.border!=null){GameObject clone=UnityEngine.Object.Instantiate(weapon.border.gameObject,weapon.border.transform.parent);clone.name="Off-Hand";TMP_Text[] labels=clone.GetComponentsInChildren<TMP_Text>(true);list.Add(new EquipmentStatsUI.Slot{type=LootManager.GearType.OffHands,border=clone.GetComponent<Image>(),glyph=clone.GetComponentInChildren<EquipmentGlyph>(true),label=labels.FirstOrDefault(),detail=labels.Skip(1).FirstOrDefault()});}
        foreach(var slot in list){int index=(int)slot.type;if(index<InventoryArtLayout.EquipmentSlots.Length&&slot.border!=null)InventoryArtLayout.Apply(slot.border.rectTransform,InventoryArtLayout.EquipmentSlots[index]);if(slot.glyph!=null)slot.glyph.gearType=slot.type;}
        equipment.slots=list.OrderBy(x=>(int)x.type).ToArray();view.equipmentSlots=equipment.slots.Where(x=>x.border!=null).Select(x=>x.border.rectTransform).ToList();view.activeRelicSlots=inventory.GetComponentsInChildren<ActiveRelicSlotUI>(true).Select(x=>(RectTransform)x.transform).ToList();EditorUtility.SetDirty(equipment);
    }

    static Button MakeButton(Transform parent,string text)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Image image=go.GetComponent<Image>();image.color=new Color(.12f,.12f,.14f,.95f);Button button=go.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.SpriteSwap;
        var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);RectTransform r=(RectTransform)label.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;TMP_Text tmp=label.GetComponent<TMP_Text>();tmp.text=text;tmp.fontSize=10;tmp.enableAutoSizing=true;tmp.fontSizeMin=6;tmp.fontSizeMax=11;tmp.alignment=TextAlignmentOptions.Center;tmp.raycastTarget=false;return button;
    }
    static Transform FindTransform(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.Equals(name,StringComparison.OrdinalIgnoreCase));
    static T Find<T>(Transform root,string name) where T:Component{Transform t=FindTransform(root,name);return t!=null?t.GetComponent<T>():null;}
}
