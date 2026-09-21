// Developer map: Replaces HUD, panel and button background Images with six ZoneManager-driven corruption stages while preserving live text and controls.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CorruptionUITheme : MonoBehaviour
{
    private static readonly int[] Percentages = { 0, 20, 40, 60, 80, 100 };
    private readonly Dictionary<string, Sprite> sprites = new();
    private readonly List<CorruptionUIButtonSkin> buttonSkins = new();
    private PaperBattleHUD hud;
    private ZoneManager zone;
    private Image toolbarSurface;
    private Image inventorySurface;
    private Image statsSurface;
    private Image enemySurface;
    private int appliedStage = -1;
    private float nextButtonScan;

    public void Initialize(PaperBattleHUD owner, EnemyInspectionPanelUI enemyInspection)
    {
        if (owner == null) return;
        hud = owner;
        // The exact supplied TopHUDBarEmpty art owns the toolbar permanently.
        // Corruption stages still skin the inventory/stat surfaces, never the new HUD or its state sheets.
        toolbarSurface = null;
        inventorySurface = Surface(owner.inventoryPanel);
        statsSurface = Surface(owner.statsPanel);
        enemySurface = Surface(enemyInspection != null ? enemyInspection.Panel : null);

        EnsureButtonSkins();
        ApplyCurrentStage(force: true);
    }

    private void Update()
    {
        bool addedButton = false;
        if (Time.unscaledTime >= nextButtonScan)
        {
            nextButtonScan = Time.unscaledTime + .5f;
            addedButton = EnsureButtonSkins();
        }
        ApplyCurrentStage(force: addedButton);
    }

    private void ApplyCurrentStage(bool force = false)
    {
        if (hud == null) return;
        if (zone == null) zone = FindFirstObjectByType<ZoneManager>();
        int stage = zone != null ? ZoneManager.ForestBackgroundIndex(zone.zoneLevel) : 0;
        stage = Mathf.Clamp(stage, 0, Percentages.Length - 1);
        if (!force && stage == appliedStage) return;
        appliedStage = stage;

        Set(toolbarSurface, Load(stage, "toolbar"), Image.Type.Simple);
        Set(inventorySurface, InventoryArtCatalog.Layout, Image.Type.Simple);
        if(inventorySurface!=null)inventorySurface.preserveAspect=true;
        Set(statsSurface, Load(stage, "stats"), Image.Type.Simple);
        Set(enemySurface, Load(stage, "stats"), Image.Type.Simple);
        foreach (var skin in buttonSkins)
        {
            if (skin == null) continue;
            string prefix = skin.IsCompact ? "button_compact_" : "button_";
            skin.SetStageSprites(Load(stage, prefix + "idle"), Load(stage, prefix + "active"));
        }
    }

    private Sprite Load(int stage, string part)
    {
        string key = Percentages[stage] + "_" + part;
        if (sprites.TryGetValue(key, out Sprite sprite)) return sprite;
        Texture2D texture = Resources.Load<Texture2D>("UI/Corruption/" + key);
        if (texture == null)
        {
            Debug.LogWarning("CorruptionUITheme: missing UI art " + key, this);
            sprites[key] = null;
            return null;
        }
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f);
        sprite.name = key;
        sprites[key] = sprite;
        return sprite;
    }

    private bool EnsureButtonSkins()
    {
        if (hud == null) return false;
        bool added = false;
        buttonSkins.RemoveAll(skin => skin == null);
        Canvas canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            if (!ShouldSkin(button)) continue;
            var skin = CorruptionUIButtonSkin.Ensure(button);
            if (buttonSkins.Contains(skin)) continue;
            buttonSkins.Add(skin);
            added = true;
        }
        return added;
    }

    private static bool ShouldSkin(Button button)
    {
        if (button == null) return false;
        // Inventory/equipment slots communicate item rarity and occupancy through
        // their own surfaces. Passive nodes likewise own their complete circular
        // state artwork; the generic button skin would replace that sprite and add
        // a rectangular Frame overlay on hover.
        return button.GetComponent<ItemSlotUI>() == null
            && button.GetComponent<HUDSpriteState>() == null
            && button.GetComponent<PassiveNodeView>() == null
            && button.GetComponent<CurrencySlotUI>() == null
            && button.GetComponent<RelicSlotUI>() == null
            && button.GetComponent<ActiveRelicSlotUI>() == null
            && button.GetComponent<EquippedItemHoverUI>() == null
            && button.GetComponent<InventoryArtworkHotspot>() == null;
    }

    private static Image Surface(GameObject target)
    {
        if (target == null) return null;
        Transform legacyOverlay = target.transform.Find("Corruption art");
        if (legacyOverlay != null) legacyOverlay.gameObject.SetActive(false);
        Image image = target.GetComponent<Image>();
        if (image == null) image = target.AddComponent<Image>();
        return image;
    }

    private static void Set(Image image, Sprite sprite, Image.Type type)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.type = type;
        image.color = Color.white;
        image.preserveAspect = false;
        image.enabled = sprite != null;
    }
}

public sealed partial class CorruptionUIButtonSkin : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button button;
    private Image surface;
    private Sprite idle;
    private Sprite active;
    private bool hovered;
    private bool pressed;
    private bool selected;
    private CorruptionButtonFrame frame;
    public bool IsCompact => name == "Pause Button" || name == "Play Button";

    public static CorruptionUIButtonSkin Ensure(Button owner)
    {
        if (owner == null) return null;
        var skin = owner.GetComponent<CorruptionUIButtonSkin>();
        if (skin == null) skin = owner.gameObject.AddComponent<CorruptionUIButtonSkin>();
        if (skin.button != owner) skin.Initialize(owner);
        return skin;
    }

    public void Initialize(Button owner)
    {
        button = owner;
        surface = button != null ? button.targetGraphic as Image : GetComponent<Image>();
        if (surface == null) surface = gameObject.AddComponent<Image>();
        if (button != null)
        {
            button.targetGraphic = surface;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(.82f, .82f, .82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.55f, .55f, .55f, 1f);
            button.colors = colors;
        }
        Transform legacyOverlay = transform.Find("Corruption button art");
        if (legacyOverlay != null) legacyOverlay.gameObject.SetActive(false);
        if (idle == null)
        {
            string prefix = IsCompact ? "button_compact_" : "button_";
            idle = LoadFallback(prefix + "idle");
            active = LoadFallback(prefix + "active");
        }
        EnsureFrame();
        ConfigureLabel();
        Refresh();
    }

    public void SetStageSprites(Sprite idleSprite, Sprite activeSprite)
    {
        idle = idleSprite;
        active = activeSprite;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; Refresh(); }
    private void OnDisable() { hovered = false; pressed = false; Refresh(); }
    public void SetSelected(bool value) { if (selected == value) return; selected = value; Refresh(); }

    private void Refresh()
    {
        if (surface == null) return;
        bool highlighted = button != null && button.IsInteractable() && (selected || hovered || pressed);
        // The textured idle art remains the button surface. A separate frame keeps the
        // neutral outline intact while drawing the hover signal inside it.
        surface.sprite = idle;
        surface.type = Image.Type.Simple;
        surface.color = Color.white;
        surface.preserveAspect = false;
        surface.enabled = surface.sprite != null;
        if (frame != null) frame.SetHighlighted(highlighted);
    }

    private void EnsureFrame()
    {
        Transform existing = transform.Find("Frame overlay");
        if (existing == null)
        {
            var go = new GameObject(
                "Frame overlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CorruptionButtonFrame));
            go.transform.SetParent(transform, false);
            existing = go.transform;
        }
        // MaskableGraphic needs this while RectMask2D evaluates clipping. Repair
        // overlays that may already exist from an earlier version of the skin.
        if (existing.GetComponent<CanvasRenderer>() == null)
            existing.gameObject.AddComponent<CanvasRenderer>();
        RectTransform rect = (RectTransform)existing;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        existing.SetAsLastSibling();
        frame = existing.GetComponent<CorruptionButtonFrame>();
        if (frame == null)
            frame = existing.gameObject.AddComponent<CorruptionButtonFrame>();
        frame.raycastTarget = false;
    }

    private void ConfigureLabel()
    {
        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.raycastTarget = false;
        if (!IsToolbarButton()) return;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(7, 3);
        rect.offsetMax = new Vector2(-7, -3);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 7f;
        label.fontSizeMax = 11f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private bool IsToolbarButton()
    {
        return name.EndsWith(" Button", System.StringComparison.Ordinal);
    }

    private static readonly Dictionary<string, Sprite> FallbackSprites = new();
    private static Sprite LoadFallback(string part)
    {
        if (FallbackSprites.TryGetValue(part, out Sprite cached)) return cached;
        Texture2D texture = Resources.Load<Texture2D>("UI/Corruption/0_" + part);
        if (texture == null) return null;
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f);
        sprite.name = "0_" + part;
        FallbackSprites[part] = sprite;
        return sprite;
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed partial class CorruptionButtonFrame : MaskableGraphic
{
    private bool highlighted;
    private static readonly Color Outer = new Color(.38f, .40f, .43f, 1f);
    private static readonly Color Inner = new Color(1f, .31f, 0f, 1f);

    public void SetHighlighted(bool value)
    {
        if (highlighted == value) return;
        highlighted = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        AddChamferedFrame(vertexHelper, rect, 2f, 6f, Outer);
        if (highlighted)
        {
            rect.xMin += 4f; rect.xMax -= 4f;
            rect.yMin += 4f; rect.yMax -= 4f;
            if (rect.width > 0f && rect.height > 0f) AddChamferedFrame(vertexHelper, rect, 1.5f, 4f, Inner);
        }
    }

    private static void AddChamferedFrame(VertexHelper vh, Rect rect, float thickness, float chamfer, Color color)
    {
        float safeChamfer = Mathf.Min(chamfer, Mathf.Min(rect.width, rect.height) * .25f);
        Rect innerRect = new Rect(rect.xMin + thickness, rect.yMin + thickness,
            rect.width - thickness * 2f, rect.height - thickness * 2f);
        float innerChamfer = Mathf.Max(0f, safeChamfer - thickness);
        Vector2[] outer = Corners(rect, safeChamfer);
        Vector2[] inner = Corners(innerRect, innerChamfer);
        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        for (int i = 0; i < 8; i++) { vertex.position = outer[i]; vh.AddVert(vertex); }
        for (int i = 0; i < 8; i++) { vertex.position = inner[i]; vh.AddVert(vertex); }
        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            vh.AddTriangle(start + i, start + next, start + 8 + next);
            vh.AddTriangle(start + 8 + next, start + 8 + i, start + i);
        }
    }

    private static Vector2[] Corners(Rect rect, float chamfer)
    {
        return new[]
        {
            new Vector2(rect.xMin + chamfer, rect.yMin),
            new Vector2(rect.xMax - chamfer, rect.yMin),
            new Vector2(rect.xMax, rect.yMin + chamfer),
            new Vector2(rect.xMax, rect.yMax - chamfer),
            new Vector2(rect.xMax - chamfer, rect.yMax),
            new Vector2(rect.xMin + chamfer, rect.yMax),
            new Vector2(rect.xMin, rect.yMax - chamfer),
            new Vector2(rect.xMin, rect.yMin + chamfer)
        };
    }
}
