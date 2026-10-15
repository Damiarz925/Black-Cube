// Developer map: Maintains Gear-to-slot views and a shared root-canvas tooltip, with a responsive scrolling grid. Subscribes to inventory changes while enabled and preserves unchanged slot objects.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

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
    private readonly Dictionary<Gear, ItemSlotUI> slots = new();
    public ItemTooltipUI Tooltip { get; private set; }

    public void ShowTooltip(ItemSlotUI slot)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.Create(GetComponentInParent<Canvas>().rootCanvas.transform);
        Tooltip.Show(slot);
    }
    public void ShowTooltip(Gear gear, RectTransform anchor, bool equipped, UnityEngine.EventSystems.PointerEventData data)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.Create(GetComponentInParent<Canvas>().rootCanvas.transform);
        Tooltip.Show(gear, anchor, equipped, data);
    }

    private void Awake()
    {
        ConfigureGrid();
        gameObject.AddComponent<InventoryFilterUI>();
        iconLookup = new Dictionary<LootManager.GearType, Sprite>();

        foreach (var entry in gearIcons)
        {
            if (entry == null || entry.icon == null) continue;
            iconLookup[entry.gearType] = entry.icon;
        }
    }

    private void ConfigureGrid()
    {
        if (contentParent == null) return;
        var old = (RectTransform)contentParent;
        var scroll = old.GetComponentInParent<ScrollRect>();
        var go = new GameObject("Compact inventory grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        var rect = (RectTransform)go.transform; rect.SetParent(old.parent, false);
        rect.anchorMin = old.anchorMin; rect.anchorMax = old.anchorMax; rect.pivot = old.pivot;
        rect.offsetMin = old.offsetMin; rect.offsetMax = old.offsetMax;
        var grid = go.GetComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(78,96); grid.spacing = new Vector2(8,8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentParent = rect;
        if (scroll != null) { scroll.content = rect; scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.onValueChanged.AddListener(_ => Tooltip?.Hide()); }
        old.gameObject.SetActive(false); Destroy(old.gameObject);
    }

    private void LateUpdate()
    {
        if (contentParent == null) return;
        var grid = contentParent.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((((RectTransform)contentParent).rect.width + 8) / 86));
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
        if (Tooltip != null) Tooltip.Hide();
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (contentParent == null || itemSlotPrefab == null || Inventory.Instance == null)
            return;

        var removed = new List<Gear>();
        foreach (var pair in slots)
            if (!Contains(pair.Key)) { if(pair.Value != null) { pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); } removed.Add(pair.Key); }
        foreach (var item in removed) slots.Remove(item);
        foreach (var gear in Inventory.Instance.Items)
        {
            if (gear == null) continue;
            if (!slots.TryGetValue(gear, out var slot) || slot == null)
            {
                slot = Instantiate(itemSlotPrefab, contentParent).GetComponent<ItemSlotUI>();
                slots[gear] = slot;
            }
            Sprite icon = GetIconForType(gear.ItemType);
            slot.Setup(gear, icon);
            slot.transform.SetAsLastSibling();
        }
    }
    private bool Contains(Gear item)
    { foreach(var gear in Inventory.Instance.Items) if(gear == item) return true; return false; }
    private void OnDestroy() { if(Tooltip != null) Destroy(Tooltip.gameObject); }
}
