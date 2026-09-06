using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the manager's actual equipped items, including empty categories.</summary>
public class EquipmentStatsUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public LootManager.GearType type;
        public EquipmentGlyph glyph;
        public TMP_Text label;
        public TMP_Text detail;
        public Image border;
    }
    public Slot[] slots;
    private EquipmentManager manager;
    private void OnEnable()
    {
        manager = EquipmentManager.Instance;
        if (manager != null) manager.EquipmentChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (manager != null) manager.EquipmentChanged -= Refresh;
        manager = null;
    }
    public void Refresh()
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            var item = manager != null ? manager.GetEquipped(slot.type) : null;
            var tint = item != null ? ItemSlotUI.RarityColor(item.ItemRarity) : new Color(.29f,.31f,.35f);
            slot.glyph.gearType = slot.type;
            slot.glyph.color = tint;
            slot.glyph.SetVerticesDirty();
            slot.border.color = tint;
            slot.label.text = ItemSlotUI.DisplayType(slot.type);
            slot.detail.text = item != null ? $"{item.ItemRarity}  /  LV {item.ItemLevel}" : "EMPTY";
            slot.detail.color = item != null ? tint : new Color(.48f,.5f,.54f);
        }
        var stats = GetComponent<PlayerStatsPanelUI>();
        if (stats != null) stats.Refresh();
    }
}
