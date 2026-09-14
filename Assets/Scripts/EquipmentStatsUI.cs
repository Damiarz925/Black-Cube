// Developer map: Refreshes read-only equipped-slot glyphs for either EquipmentManager or one inspected enemy. This view never mutates equipment or modifiers.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
        [System.NonSerialized] public Image occupiedSurface;
        [System.NonSerialized] public Image icon;
        [System.NonSerialized] public Image elementBadge;
        [System.NonSerialized] public Image corruptionBorder;
    }
    public Slot[] slots;
    private EquipmentManager manager;
    private EnemyAI enemyTarget;
    private bool inspectEnemy;
    private ZoneManager zone;
    private int displayedCorruptionStage = -1;

    public void SetEnemyTarget(EnemyAI target)
    {
        if (manager != null)
            manager.EquipmentChanged -= Refresh;
        manager = null;
        inspectEnemy = true;
        enemyTarget = target;
        Refresh();
    }

    private void OnEnable()
    {
        if (!inspectEnemy)
        {
            manager = EquipmentManager.Instance;
            if (manager != null) manager.EquipmentChanged += Refresh;
        }
        Refresh();
    }
    private void OnDisable()
    {
        if (manager != null) manager.EquipmentChanged -= Refresh;
        manager = null;
    }
    private void Update()
    {
        int stage = CurrentCorruptionStage();
        if (stage != displayedCorruptionStage) Refresh();
    }
    public void Refresh()
    {
        if (slots == null) return;
        displayedCorruptionStage = CurrentCorruptionStage();
        foreach (var slot in slots)
        {
            var target = slot.glyph.transform.parent.gameObject;
            var hover = target.GetComponent<EquippedItemHoverUI>();
            if (hover == null) hover = target.AddComponent<EquippedItemHoverUI>();
            hover.Type = slot.type;
            var button=target.GetComponent<Button>();if(button==null)button=target.AddComponent<Button>();button.targetGraphic=target.GetComponent<Image>();button.onClick.RemoveAllListeners();if(!inspectEnemy)button.onClick.AddListener(hover.Activate);button.interactable=!inspectEnemy;
            var item = inspectEnemy ? enemyTarget?.GetEquippedGear(slot.type) : manager?.GetEquipped(slot.type);
            hover.SetItem(inspectEnemy, item);
            var tint = item != null ? ItemSlotUI.RarityColor(item.ItemRarity) : new Color(.29f,.31f,.35f);
            if (slot.occupiedSurface != null)
                slot.occupiedSurface.color = item != null
                    ? ItemSlotUI.RarityBackground(item.ItemRarity)
                    : new Color(1f, 1f, 1f, .001f);
            slot.glyph.gearType = slot.type;
            slot.glyph.enabled = false;
            Sprite iconSprite = ItemIconCatalog.Get(item);
            if (iconSprite != null && slot.icon == null)
            {
                var iconObject = new GameObject("Themed equipped item icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(slot.glyph.transform.parent, false);
                RectTransform iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin=new Vector2(.015f,0f);iconRect.anchorMax=new Vector2(.985f,1f);
                iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;
                slot.icon=iconObject.GetComponent<Image>();slot.icon.raycastTarget=false;slot.icon.preserveAspect=true;
                slot.icon.transform.SetAsLastSibling();
            }
            if (slot.icon != null)
            {
                slot.icon.sprite=iconSprite;slot.icon.color=Color.white;slot.icon.enabled=iconSprite!=null;
            }
            if (slot.elementBadge == null)
            {
                var markerObject = new GameObject("Base element marker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(slot.border.transform, false);
                var markerRect = (RectTransform)markerObject.transform;
                markerRect.anchorMin=markerRect.anchorMax=new Vector2(.82f,.18f);markerRect.sizeDelta=new Vector2(4,4);markerRect.anchoredPosition=Vector2.zero;markerRect.localRotation=Quaternion.Euler(0,0,45);
                slot.elementBadge=markerObject.GetComponent<Image>();slot.elementBadge.raycastTarget=false;
            }
            slot.elementBadge.enabled=item!=null&&item.ItemType==LootManager.GearType.Weapons;
            if(slot.elementBadge.enabled)slot.elementBadge.color=ItemTooltipUI.ElementTint(item.BaseElement);
            if (item != null && slot.corruptionBorder == null)
            {
                var borderObject = new GameObject("Area corruption border", typeof(RectTransform), typeof(Image));
                borderObject.transform.SetParent(slot.glyph.transform.parent, false);
                RectTransform source = slot.glyph.rectTransform;
                RectTransform borderRect = (RectTransform)borderObject.transform;
                borderRect.anchorMin=source.anchorMin;borderRect.anchorMax=source.anchorMax;borderRect.pivot=source.pivot;
                borderRect.anchoredPosition=source.anchoredPosition;borderRect.sizeDelta=source.sizeDelta;
                slot.corruptionBorder=borderObject.GetComponent<Image>();
                slot.corruptionBorder.raycastTarget=false;slot.corruptionBorder.preserveAspect=true;
            }
            if (slot.corruptionBorder != null)
            {
                slot.corruptionBorder.sprite=ItemIconCatalog.GetCorruptionBorder(displayedCorruptionStage);
                slot.corruptionBorder.color=Color.white;
                slot.corruptionBorder.enabled=item!=null&&slot.corruptionBorder.sprite!=null;
                int borderIndex = slot.icon != null
                    ? slot.icon.transform.GetSiblingIndex()+1
                    : slot.glyph.transform.GetSiblingIndex()+1;
                slot.corruptionBorder.transform.SetSiblingIndex(borderIndex);
            }
            slot.glyph.enabled = false;
            slot.border.color = slot.border.GetComponent<InventoryArtworkHotspot>() != null ? Color.clear : tint;
            slot.label.text = ItemSlotUI.DisplayType(slot.type);
            slot.detail.text = item != null ? $"{item.ItemRarity}  /  LV {item.ItemLevel}" : "EMPTY";
            slot.detail.color = item != null ? tint : new Color(.48f,.5f,.54f);
        }
        var stats = GetComponent<PlayerStatsPanelUI>();
        if (stats != null) stats.Refresh();
    }

    private int CurrentCorruptionStage()
    {
        if (zone == null) zone = FindAnyObjectByType<ZoneManager>();
        return zone != null ? ZoneManager.ForestBackgroundIndex(zone.zoneLevel) : 0;
    }
}
