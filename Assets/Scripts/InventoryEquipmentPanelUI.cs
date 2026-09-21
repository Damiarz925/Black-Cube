// Developer map: Owns the responsive ARPG-style inventory composition: equipped gear and relics above, recovered items below, currencies at the bottom.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryEquipmentPanelUI : MonoBehaviour
{
    static readonly Color Ink = new Color(.027f, .03f, .037f, .98f);
    static readonly Color Muted = new Color(.48f, .5f, .54f);
    static readonly Color Red = new Color(.78f, .12f, .16f);
    [SerializeField] EquipmentStatsUI equipmentView;
    [SerializeField] GameObject equipmentRoot;
    [SerializeField] GameObject relicHost;
    [SerializeField] GameObject itemScroll;

    public static InventoryEquipmentPanelUI Ensure(InventoryUI owner, Transform itemContent)
    {
        if (owner == null) return null;
        var layout = owner.GetComponent<InventoryEquipmentPanelUI>();
        if (layout == null) { Debug.LogError("InventoryUI is missing its authored InventoryEquipmentPanelUI.", owner); return null; }
        layout.Bind(itemContent);
        return layout;
    }

    public void Bind(Transform itemContent)
    {
        if (equipmentView == null && equipmentRoot != null) equipmentView = equipmentRoot.GetComponent<EquipmentStatsUI>();
        if (itemScroll == null && itemContent != null)
        {
            ScrollRect scroll = itemContent.GetComponentInParent<ScrollRect>();
            if (scroll != null) itemScroll = scroll.gameObject;
        }
        if (equipmentView == null) Debug.LogError("Inventory equipment layout is not authored. Run the Inventory authoring builder.", this);
    }

#if UNITY_EDITOR
    public void BuildAuthoring(Transform itemContent) => Build(itemContent);
#endif

    public void Build(Transform itemContent)
    {
        if (equipmentView != null) return;
        var panel = (RectTransform)transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(24, -120);
        panel.sizeDelta = new Vector2(690, 690f * InventoryArtLayout.Height / InventoryArtLayout.Width);
        var background = GetComponent<Image>();
        if (background != null) { background.sprite=InventoryArtCatalog.Layout;background.type=Image.Type.Simple;background.preserveAspect=true;background.color=Color.white; }

        TMP_Text title = FindText("FIELD INVENTORY");
        if (title != null) title.gameObject.SetActive(false);
        TMP_Text subtitle = FindText("RECOVERED EQUIPMENT");
        if (subtitle != null) subtitle.gameObject.SetActive(false);
        foreach(var button in GetComponentsInChildren<Button>(true))if(button.name=="X")BakedInventoryButton.Configure(button,InventoryArtLayout.CloseButton);

        Transform oldEquipment = transform.Find("Equipped gear layout");
        if (oldEquipment != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(oldEquipment.gameObject); else Destroy(oldEquipment.gameObject);
#else
            Destroy(oldEquipment.gameObject);
#endif
        }
        equipmentRoot = new GameObject("Equipped gear layout", typeof(RectTransform), typeof(EquipmentStatsUI));
        equipmentRoot.transform.SetParent(transform, false);
        Place((RectTransform)equipmentRoot.transform, 0, 0, 1, 1);

        equipmentView = equipmentRoot.GetComponent<EquipmentStatsUI>();
        var types = (LootManager.GearType[])System.Enum.GetValues(typeof(LootManager.GearType));
        equipmentView.slots = new EquipmentStatsUI.Slot[types.Length];
        for (int i = 0; i < types.Length; i++)
            equipmentView.slots[i] = CreateEquipmentSlot(equipmentRoot.transform, types[i], InventoryArtLayout.EquipmentSlots[i]);
        equipmentView.Refresh();

        relicHost = new GameObject("Equipped relic layout", typeof(RectTransform), typeof(RelicEquipmentUI));
        relicHost.transform.SetParent(transform, false);
        Place((RectTransform)relicHost.transform, 0, 0, 1, 1);
        #if UNITY_EDITOR
        relicHost.GetComponent<RelicEquipmentUI>().Build();
        #endif

        if (itemContent != null)
        {
            ScrollRect scroll = itemContent.GetComponentInParent<ScrollRect>();
            if (scroll != null) { itemScroll = scroll.gameObject; InventoryArtLayout.Apply((RectTransform)scroll.transform, InventoryArtLayout.InventoryBounds); }
        }
    }

    public void SetRelicMode(bool relicMode)
    {
        if (equipmentRoot != null) equipmentRoot.SetActive(!relicMode);
        if (relicHost != null) relicHost.SetActive(!relicMode);
        if (itemScroll != null) itemScroll.SetActive(!relicMode);
        foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            if (text.text is "EQUIPPED GEAR" or "ACTIVE RELICS" or "RECOVERED ITEMS") text.gameObject.SetActive(!relicMode);
    }

    public static void SeparateStats(GameObject panel)
    {
        if (panel == null) return;
        var equipment = panel.GetComponent<EquipmentStatsUI>();
        if (equipment != null)
        {
            if (equipment.slots != null)
                foreach (var slot in equipment.slots)
                    if (slot != null && slot.border != null) slot.border.gameObject.SetActive(false);
            equipment.enabled = false;
        }
        Transform relics = panel.transform.Find("Active Relics");
        if (relics != null) relics.gameObject.SetActive(false);
        foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text == "EQUIPPED GEAR") text.gameObject.SetActive(false);
            if (text.text == "ACTIVE STATS") Place(text.rectTransform, .05f, .84f, .8f, .89f);
        }
        Transform scroll = panel.transform.Find("Stats scroll");
        if (scroll is RectTransform rect) Place(rect, .05f, .055f, .95f, .83f);
    }

    EquipmentStatsUI.Slot CreateEquipmentSlot(Transform parent, LootManager.GearType type, Rect artworkRect)
    {
        var go = new GameObject(ItemSlotUI.DisplayType(type), typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        go.transform.SetParent(parent, false);
        InventoryArtLayout.Apply((RectTransform)go.transform,artworkRect);
        go.AddComponent<InventoryArtworkHotspot>();
        var border = go.GetComponent<Image>(); border.color = Color.clear;
        var surface = new GameObject("Occupied slot interior", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        surface.transform.SetParent(go.transform, false); Place((RectTransform)surface.transform, .12f, .13f, .88f, .87f);
        surface.GetComponent<Image>().color = new Color(1,1,1,0.001f);
        var glyphObject = new GameObject("Category icon", typeof(RectTransform), typeof(EquipmentGlyph));
        glyphObject.transform.SetParent(surface.transform, false);
        var glyphRect = (RectTransform)glyphObject.transform; glyphRect.anchorMin = new Vector2(.5f, .5f); glyphRect.anchorMax = new Vector2(.5f, .5f); glyphRect.sizeDelta = new Vector2(44, 44); glyphRect.anchoredPosition = Vector2.zero;
        var glyph = glyphObject.GetComponent<EquipmentGlyph>(); glyph.gearType = type; glyph.color = Color.clear; glyph.raycastTarget = false; glyph.enabled = false;
        return new EquipmentStatsUI.Slot
        {
            type = type,
            glyph = glyph,
            border = border,
            occupiedSurface = surface.GetComponent<Image>(),
            label = HiddenLabel(surface.transform, ItemSlotUI.DisplayType(type)),
            detail = HiddenLabel(surface.transform, "EMPTY")
        };
    }

    static TMP_Text HiddenLabel(Transform parent,string value){var label=Label(parent,value,1,0,0,0,0,Color.clear);label.gameObject.SetActive(false);return label;}

    TMP_Text FindText(string value)
    {
        foreach (var text in GetComponentsInChildren<TMP_Text>(true)) if (text.text == value) return text;
        return null;
    }

    static TMP_Text Label(Transform parent, string value, int size, float x0, float y0, float x1, float y1, Color color)
    {
        var go = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, x0, y0, x1, y1);
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        return text;
    }

    static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
