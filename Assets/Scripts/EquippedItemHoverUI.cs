// Developer map: Bridges player or explicit enemy gear to the shared read-only equipped-item tooltip, which disables dismantling.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Hover and click target for equipped gear. Currency application takes priority over unequip.</summary>
public sealed class EquippedItemHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public LootManager.GearType Type;
    public bool UseExplicitItem;
    public Gear Item;
    InventoryUI inventory;
    bool hovered;
    public void SetItem(bool useExplicitItem, Gear item)
    {
        if (UseExplicitItem != useExplicitItem || Item != item)
            HideTooltip();
        UseExplicitItem = useExplicitItem;
        Item = item;
    }
    public void OnPointerEnter(PointerEventData data)
    {
        hovered=true;
        var item = UseExplicitItem ? Item : EquipmentManager.Instance?.GetEquipped(Type);
        if (item == null) return;
        inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventory?.ShowTooltip(item, (RectTransform)transform, true, data, !UseExplicitItem);
    }
    public void OnPointerExit(PointerEventData data) {hovered=false;inventory?.Tooltip?.Leave((RectTransform)transform,data);}
    void Update()
    {
        if(!hovered||UseExplicitItem||Keyboard.current?.lKey.wasPressedThisFrame!=true)return;
        EquipmentManager.Instance?.GetEquipped(Type)?.ToggleLock();HideTooltip();
    }
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
        {
            CurrencyInventory.Instance.TryApplyArmedToGear(item);
            return;
        }
        if(item!=null){HideTooltip();EquipmentManager.Instance?.Unequip(Type);}
    }
    private void OnDisable() {hovered=false;HideTooltip();}
    private void HideTooltip() => inventory?.Tooltip?.HideFor((RectTransform)transform);
}
