using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryAuthoringBuilder
{
    public const string PrefabPath = "Assets/Prefabs/UI/InventoryPanel.prefab";

    [MenuItem("Black-Cube/UI Authoring/Build Inventory")]
    public static void Build()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            InventoryUI inventory = root.GetComponentInChildren<InventoryUI>(true);
            if (inventory == null) throw new InvalidOperationException("Production PaperBattle prefab has no InventoryUI.");
            InventoryView existing = inventory.GetComponent<InventoryView>();
            InventoryEquipmentPanelUI equipment = inventory.GetComponent<InventoryEquipmentPanelUI>();
            CurrencyInventoryPanel currency = inventory.GetComponent<CurrencyInventoryPanel>();
            InventoryFilterUI filter = inventory.GetComponent<InventoryFilterUI>();
            InventoryModHighlightUI modFilter = inventory.GetComponent<InventoryModHighlightUI>();
            if (existing != null && existing.itemGridRoot != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Debug.Log("INVENTORY AUTHORING: Existing authored layout preserved.");
            }
            else
            {
                inventory.BuildAuthoringView();
                if (equipment == null) equipment = inventory.gameObject.AddComponent<InventoryEquipmentPanelUI>();
                equipment.BuildAuthoring(Find<RectTransform>(inventory.transform, "Compact inventory grid"));
                if (currency == null) currency = inventory.gameObject.AddComponent<CurrencyInventoryPanel>();
                currency.BuildAuthoring(inventory, Find<RectTransform>(inventory.transform, "Compact inventory grid"));
                if (filter == null) filter = inventory.gameObject.AddComponent<InventoryFilterUI>();
                filter.BuildAuthoring();
                if (modFilter == null) modFilter = inventory.gameObject.AddComponent<InventoryModHighlightUI>();
                modFilter.BuildAuthoring();
            }

            InventoryView view = inventory.GetComponent<InventoryView>();
            view.authoredRoot = inventory.transform as RectTransform;
            view.itemGridRoot = Find<RectTransform>(inventory.transform, "Compact inventory grid");
            view.equipmentRoot = Find<RectTransform>(inventory.transform, "Equipped gear layout");
            view.ordinaryCurrencyRoot = Find<RectTransform>(inventory.transform, "Ordinary currency");
            view.ancientCurrencyRoot = Find<RectTransform>(inventory.transform, "Ancient relic currency");
            view.currencyTray = view.ordinaryCurrencyRoot;
            view.relicRoot = Find<RectTransform>(inventory.transform, "Relic inventory");
            view.filterRoot = Find<RectTransform>(inventory.transform, "Auto-dismantle filter");
            view.modFilterRoot = Find<RectTransform>(inventory.transform, "Mod highlight filter");
            view.tooltipHost = inventory.GetComponentInParent<Canvas>(true)?.transform as RectTransform;
            view.background = inventory.GetComponent<Image>();
            view.currencySlots = inventory.GetComponentsInChildren<CurrencySlotUI>(true).OrderBy(x => (int)x.Type).ToList();
            view.fragmentLabels = inventory.GetComponentsInChildren<TMP_Text>(true).Where(x => x.name.EndsWith("fragments", StringComparison.OrdinalIgnoreCase)).ToList();
            view.gearToggle = currency.GearTab; view.relicToggle = currency.RelicTab;
            EquipmentStatsUI equipmentStats = inventory.GetComponentInChildren<EquipmentStatsUI>(true);
            view.equipmentSlots = equipmentStats != null && equipmentStats.slots != null ? equipmentStats.slots.Where(x => x != null && x.border != null).Select(x => x.border.rectTransform).ToList() : view.equipmentSlots;
            view.activeRelicSlots = inventory.GetComponentsInChildren<ActiveRelicSlotUI>(true).Select(x => (RectTransform)x.transform).ToList();

            ConfigureElement(inventory.gameObject, "inventory.root", "Inventory Root");
            ConfigureElement(view.itemGridRoot.gameObject, "inventory.item-grid", "Inventory Item Grid");
            ConfigureElement(view.gearToggle.gameObject, "inventory.tab.gear", "Gear Tab");
            ConfigureElement(view.relicToggle.gameObject, "inventory.tab.relic", "Relic Tab");
            foreach (CurrencySlotUI slot in view.currencySlots) ConfigureElement(slot.gameObject, "inventory.currency." + slot.Type.ToString().ToLowerInvariant(), slot.Type + " Currency");
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(inventory); EditorUtility.SetDirty(equipment); EditorUtility.SetDirty(currency); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(modFilter);
            PrefabUtility.SaveAsPrefabAsset(inventory.gameObject, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("INVENTORY AUTHORING: Built persistent production inventory and reference prefab " + PrefabPath + ".");
    }

    static T Find<T>(Transform root, string name) where T : Component
    {
        Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
        return found != null ? found.GetComponent<T>() : null;
    }

    static void ConfigureElement(GameObject go, string id, string label)
    {
        if (go == null) return;
        UIAuthoringElement element = go.GetComponent<UIAuthoringElement>() ?? go.AddComponent<UIAuthoringElement>();
        Button button = go.GetComponent<Button>(); Image image = go.GetComponent<Image>();
        element.Configure(id, label, go.transform as RectTransform, image, go.GetComponentInChildren<TMP_Text>(true));
    }
}
