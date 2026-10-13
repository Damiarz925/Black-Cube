using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryView : MonoBehaviour
{
    [Header("Roots")] public RectTransform authoredRoot, equipmentRoot, itemGridRoot, currencyTray, ordinaryCurrencyRoot, ancientCurrencyRoot, relicRoot, tooltipHost;
    [Header("Equipment")] public List<RectTransform> equipmentSlots = new(); public List<RectTransform> activeRelicSlots = new();
    [Header("Currencies")] public List<CurrencySlotUI> currencySlots = new();
    [Header("Controls")] public Button gearToggle, relicToggle; public List<Button> itemTypeFilters = new();
    [Header("Inventory Grid")] public Image gridBackground; public Sprite gridCellSprite; public ScrollRect inventoryScroll; public Scrollbar inventoryScrollbar;
    [Header("Presentation")] public Image background; public ItemSlotUI itemSlotPrefab; public ItemTooltipUI itemTooltipPrefab; public RelicTooltipUI relicTooltipPrefab; public CurrencyTooltipUI currencyTooltipPrefab;
}
