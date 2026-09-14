// Developer map: Bridges player or explicit enemy gear to the shared read-only equipped-item tooltip, which disables dismantling.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Read-only hover target; equipment has no click/unequip gesture.</summary>
public sealed class EquippedItemHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public LootManager.GearType Type;
    public bool UseExplicitItem;
    public Gear Item;
    InventoryUI inventory;
    public void SetItem(bool useExplicitItem, Gear item)
    {
        if (UseExplicitItem != useExplicitItem || Item != item)
            HideTooltip();
        UseExplicitItem = useExplicitItem;
        Item = item;
    }
    public void OnPointerEnter(PointerEventData data)
    {
        var item = UseExplicitItem ? Item : EquipmentManager.Instance?.GetEquipped(Type);
        if (item == null) return;
        inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventory?.ShowTooltip(item, (RectTransform)transform, true, data, !UseExplicitItem);
    }
    public void OnPointerExit(PointerEventData data) => inventory?.Tooltip?.Leave((RectTransform)transform, data);
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        Activate();
    }
    public void Activate()
    {
        if (UseExplicitItem) return;
        var item = EquipmentManager.Instance?.GetEquipped(Type);
        if (item != null && CurrencyInventory.Instance != null && CurrencyInventory.Instance.ArmedCurrency.HasValue)
            CurrencyInventory.Instance.TryApplyArmedToGear(item);
    }
    private void OnDisable() => HideTooltip();
    private void HideTooltip() => inventory?.Tooltip?.HideFor((RectTransform)transform);
}
