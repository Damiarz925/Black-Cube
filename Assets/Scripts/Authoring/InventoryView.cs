using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryView : MonoBehaviour
{
    [Header("Roots")] public RectTransform authoredRoot, equipmentRoot, itemGridRoot, currencyTray, ordinaryCurrencyRoot, ancientCurrencyRoot, relicRoot, tooltipHost, filterRoot, modFilterRoot;
    [Header("Equipment")] public List<RectTransform> equipmentSlots = new(); public List<RectTransform> activeRelicSlots = new();
    [Header("Currencies")] public List<CurrencySlotUI> currencySlots = new(); public List<TMP_Text> fragmentLabels = new();
    [Header("Controls")] public Button gearToggle, relicToggle;
    [Header("Presentation")] public Image background; public ItemSlotUI itemSlotPrefab; public ItemTooltipUI itemTooltipPrefab; public RelicTooltipUI relicTooltipPrefab; public CurrencyTooltipUI currencyTooltipPrefab;
}
