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
    public const int GridColumnCount = 8;
    public static readonly Vector2 GridCellSize = new Vector2(59, 59);
    public static readonly Vector2 GridSpacing = new Vector2(17, 9);
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Icons")]
    [SerializeField] private List<GearIconEntry> gearIcons = new List<GearIconEntry>();

    private Dictionary<LootManager.GearType, Sprite> iconLookup;
    private readonly Dictionary<Gear, ItemSlotUI> slots = new();
    private ZoneManager zone;
    private int displayedCorruptionStage = -1;
    public ItemTooltipUI Tooltip { get; private set; }
    private CurrencyInventoryPanel currencyPanel;
    public void ShowEquipmentView() => currencyPanel?.ShowEquipment();
    public void ShowCurrencyView() => currencyPanel?.ShowCurrency();
    public void ShowRelicView() => currencyPanel?.ShowRelics();
    public void ShowEquipmentViewForCurrency() => currencyPanel?.ShowEquipmentForCurrency();
    public void ShowRelicViewForCurrency() => currencyPanel?.ShowRelicsForCurrency();

    public void ShowTooltip(ItemSlotUI slot)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.Create(GetComponentInParent<Canvas>().rootCanvas.transform);
        Tooltip.Show(slot);
    }
    public void ShowTooltip(Gear gear, RectTransform anchor, bool equipped, UnityEngine.EventSystems.PointerEventData data,
        bool validatePlayerEquipment = true)
    {
        if (Tooltip == null) Tooltip = ItemTooltipUI.Create(GetComponentInParent<Canvas>().rootCanvas.transform);
        Tooltip.Show(gear, anchor, equipped, data, validatePlayerEquipment);
    }

    private void Awake()
    {
        ConfigureGrid();
        if (GetComponent<InventoryFilterUI>() == null) gameObject.AddComponent<InventoryFilterUI>();
        if (GetComponent<InventoryModHighlightUI>() == null) gameObject.AddComponent<InventoryModHighlightUI>();
        InventoryEquipmentPanelUI.Ensure(this, contentParent);
        currencyPanel = GetComponent<CurrencyInventoryPanel>();
        if (currencyPanel == null) currencyPanel = gameObject.AddComponent<CurrencyInventoryPanel>();
        currencyPanel.Initialize(this, contentParent);
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
        var grid = go.GetComponent<GridLayoutGroup>(); grid.cellSize = GridCellSize; grid.spacing = GridSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = GridColumnCount;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentParent = rect;
        if (scroll != null) { scroll.content = rect; scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.onValueChanged.AddListener(_ => Tooltip?.Hide()); }
        old.gameObject.SetActive(false); Destroy(old.gameObject);
        ConfigureGridMetrics();
    }

    private void LateUpdate()
    {
        if (contentParent == null) return;
        var grid = contentParent.GetComponent<GridLayoutGroup>();
        if (grid != null) { grid.constraintCount = GridColumnCount; ConfigureGridMetrics(); }
        int stage = CurrentCorruptionStage();
        if (stage != displayedCorruptionStage) RefreshAreaCorruptionBorders(stage);
    }

    private void ConfigureGridMetrics()
    {
        if(contentParent==null)return;var grid=contentParent.GetComponent<GridLayoutGroup>();if(grid==null)return;
        float width=((RectTransform)contentParent).rect.width;if(width<=0f)return;
        float sourceWidth=InventoryArtLayout.InventoryBounds.width*InventoryArtLayout.Width;
        float scale=width/sourceWidth;float cell=82f*scale;float gap=Mathf.Max(0f,(width-cell*GridColumnCount)/(GridColumnCount-1));
        grid.cellSize=new Vector2(cell,cell);grid.spacing=new Vector2(gap,12f*scale);
    }

    private Sprite GetIconForGear(Gear gear)
    {
        Sprite themed = ItemIconCatalog.Get(gear);
        if (themed != null) return themed;
        if (gear != null && iconLookup != null && iconLookup.TryGetValue(gear.ItemType, out var sprite))
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

        Sprite icon = GetIconForGear(gear);
        slotUI.Setup(gear, icon);
        slotUI.SetAreaCorruption(CurrentCorruptionStage());
    }

    private void OnEnable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += Refresh;
            Inventory.Instance.OnModFilterChanged += RefreshModHighlights;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (Tooltip != null) Tooltip.Hide();
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= Refresh;
            Inventory.Instance.OnModFilterChanged -= RefreshModHighlights;
        }
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
            Sprite icon = GetIconForGear(gear);
            slot.Setup(gear, icon);
            slot.SetAreaCorruption(CurrentCorruptionStage());
            slot.transform.SetAsLastSibling();
        }
        RefreshModHighlights();
    }

    public void RefreshModHighlights()
    {
        InventoryModFilter filter = Inventory.Instance != null ? Inventory.Instance.ModHighlightFilter : null;
        foreach (var pair in slots)
        {
            if (pair.Value == null) continue;
            int matches = filter != null ? filter.CountMatches(pair.Key) : 0;
            pair.Value.SetModHighlight(filter != null && filter.HasSelection && matches >= filter.RequiredMatches, matches);
        }
    }

    private int CurrentCorruptionStage()
    {
        if (zone == null) zone = FindAnyObjectByType<ZoneManager>();
        return zone != null ? ZoneManager.ForestBackgroundIndex(zone.zoneLevel) : 0;
    }

    private void RefreshAreaCorruptionBorders(int stage)
    {
        displayedCorruptionStage = stage;
        foreach (var pair in slots)
            if (pair.Value != null) pair.Value.SetAreaCorruption(stage);
    }
    private bool Contains(Gear item)
    { foreach(var gear in Inventory.Instance.Items) if(gear == item) return true; return false; }
    private void OnDestroy() { if(Tooltip != null) Destroy(Tooltip.gameObject); }
}
