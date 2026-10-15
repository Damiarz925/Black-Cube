// Developer map: Bridges an equipped slot to the shared InventoryUI tooltip. Equipped items use the protected tooltip mode that disables dismantling.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Read-only hover target; equipment has no click/unequip gesture.</summary>
public sealed class EquippedItemHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public LootManager.GearType Type;
    InventoryUI inventory;
    public void OnPointerEnter(PointerEventData data)
    {
        var item = EquipmentManager.Instance?.GetEquipped(Type);
        if (item == null) return;
        inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventory?.ShowTooltip(item, (RectTransform)transform, true, data);
    }
    public void OnPointerExit(PointerEventData data) => inventory?.Tooltip?.Leave((RectTransform)transform, data);
}
