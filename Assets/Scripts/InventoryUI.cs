using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class GearIconEntry
{
    public LootManager.GearType gearType;
    public Sprite icon;
}

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Icons")]
    [SerializeField] private List<GearIconEntry> gearIcons = new List<GearIconEntry>();

    private Dictionary<LootManager.GearType, Sprite> iconLookup;

    private void Awake()
    {
        iconLookup = new Dictionary<LootManager.GearType, Sprite>();

        foreach (var entry in gearIcons)
        {
            if (entry == null || entry.icon == null) continue;
            iconLookup[entry.gearType] = entry.icon;
        }
    }

    private Sprite GetIconForType(LootManager.GearType type)
    {
        if (iconLookup != null && iconLookup.TryGetValue(type, out var sprite))
            return sprite;
        return null;
    }

    public void AddItem(Gear gear)
    {
        if (gear == null || contentParent == null || itemSlotPrefab == null)
        {
            Debug.LogWarning("InventoryUI.AddItem: missing gear/contentParent/itemSlotPrefab");
            return;
        }

        GameObject slotGO = Instantiate(itemSlotPrefab, contentParent);
        var slotUI = slotGO.GetComponent<ItemSlotUI>();

        if (slotUI == null)
        {
            Debug.LogError("ItemSlot prefab is missing ItemSlotUI component.");
            return;
        }

        Sprite icon = GetIconForType(gear.ItemType);
        slotUI.Setup(gear, icon);
    }

    private void OnEnable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (contentParent == null || itemSlotPrefab == null || Inventory.Instance == null)
            return;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var gear in Inventory.Instance.Items)
        {
            var go = Instantiate(itemSlotPrefab, contentParent);
            var slot = go.GetComponent<ItemSlotUI>();

            Sprite icon = GetIconForType(gear.ItemType);
            slot.Setup(gear, icon);
        }
    }
}
