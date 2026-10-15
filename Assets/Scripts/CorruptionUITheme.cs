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

    public void Initialize(PaperBattleHUD owner, EnemyInspectionPanelUI enemyInspection)
    {
        if (owner == null) return;
        hud = owner;
        toolbarSurface = Surface(owner.gameObject);
        inventorySurface = Surface(owner.inventoryPanel);
        statsSurface = Surface(owner.statsPanel);
        enemySurface = Surface(enemyInspection != null ? enemyInspection.Panel : null);

        EnsureButtonSkins();
        ApplyCurrentStage(force: true);
    }

    private void Update()
    {
        bool addedButton = EnsureButtonSkins();
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
        Set(inventorySurface, Load(stage, "inventory"), Image.Type.Simple);
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
        foreach (string name in new[] { "INVENTORY", "STATS", "ENEMY", "PAUSE", "PLAY", "Skills" })
        {
            Transform target = hud.transform.Find(name);
            if (target == null) continue;
            var skin = target.GetComponent<CorruptionUIButtonSkin>();
            if (skin == null) skin = target.gameObject.AddComponent<CorruptionUIButtonSkin>();
            if (buttonSkins.Contains(skin)) continue;
            skin.Initialize(target.GetComponent<Button>());
            buttonSkins.Add(skin);
            added = true;
        }
        return added;
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

public sealed class CorruptionUIButtonSkin : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button button;
    private Image surface;
    private Sprite idle;
    private Sprite active;
    private bool hovered;
    private bool pressed;
    private bool selected;
    public bool IsCompact => name == "PAUSE" || name == "PLAY";

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
        surface.sprite = highlighted && active != null ? active : idle;
        surface.type = Image.Type.Simple;
        surface.color = Color.white;
        surface.preserveAspect = false;
        surface.enabled = surface.sprite != null;
    }

    private void ConfigureLabel()
    {
        TMP_Text label = GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
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
        label.raycastTarget = false;
    }
}
