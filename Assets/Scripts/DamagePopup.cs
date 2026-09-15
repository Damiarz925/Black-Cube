// Developer map: Creates styled screen-space numbers from world-space targets; popup instances snapshot positions so target destruction does not move them. DamageReceiver has already applied life loss.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float floatSpeed = 50.0f;
    [SerializeField] private float lifetime = 0.75f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, 0f);

    [Header("Popup Spread")]
    [SerializeField] private float sameTargetResetTime = 0.08f;
    [SerializeField] private float verticalSpacing = 2f;

    [Header("Damage Colors")]
    [SerializeField] private Color physicalColor = Color.black;
    [SerializeField] private Color fireColor = new Color(1f, 0.45f, 0f);
    [SerializeField] private Color coldColor = new Color(0.15f, 0.55f, 1f);
    [SerializeField] private Color lightningColor = Color.yellow;
    [SerializeField] private Color poisonColor = new Color(0.1f, 0.8f, 0.25f);
    [SerializeField] private Color voidColor = new Color(0.65f, 0.25f, 0.9f);
    [SerializeField] private Color bleedColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color defaultColor = Color.white;
    [Header("Number Materials: physical, fire, cold, lightning, poison, bleed, void")]
    [SerializeField] private Material[] numberMaterials;
    [Header("Digit Atlases: physical, fire, cold, lightning, poison, bleed, ignite")]
    [SerializeField] private Texture2D[] numberAtlases;
    [SerializeField, Min(16f)] private float digitHeight = 52f;
    [SerializeField] private float digitSpacing = -2f;
    [SerializeField, Min(48f)] private float maxNumberWidth = 180f;

    public static DamagePopup Instance;

    private readonly System.Collections.Generic.Dictionary<Transform, PopupSequence> popupSequences = new();

    private struct PopupSequence
    {
        public int Count;
        public float LastSpawnTime;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>
    /// Spawns a damage number at the given world position.
    /// popupPrefab should have a TextMeshProUGUI somewhere in its children.
    /// </summary>
    public void Spawn(float damage, Transform target)
    {
        Spawn(damage, target, defaultColor);
    }

    public void Spawn(float damage, Transform target, Element element, bool critical = false)
    {
        int index=element switch {Element.Phys=>0,Element.Fire=>1,Element.Cold=>2,Element.Light=>3,Element.Poison=>4,Element.Void=>6,_=>-1};
        // Atlas slot 6 remains the authored Ignite digit atlas. Void can use its
        // dedicated material without accidentally borrowing Ignite's atlas.
        int atlasIndex = element == Element.Void ? -1 : index;
        SpawnStyled(damage, target, GetColorForElement(element), GetMaterial(index),atlasIndex,critical);
    }

    public void Spawn(float damage, Transform target, StatusEffects effect)
    {
        int index=effect==null?-1:effect.Ailment switch {StatusEffects.AilmentKind.Poison=>4,StatusEffects.AilmentKind.Bleed=>5,StatusEffects.AilmentKind.Ignite=>6,_=>-1};
        int materialIndex=effect!=null && effect.Ailment==StatusEffects.AilmentKind.Ignite ? 1 : index;
        SpawnStyled(damage, target, GetColorForStatus(effect), GetMaterial(materialIndex),index);
    }

    public void Spawn(float damage, Transform target, Color color)
    {
        SpawnStyled(damage,target,color,null);
    }

    private Material GetMaterial(int index) => numberMaterials!=null && index>=0 && index<numberMaterials.Length ? numberMaterials[index] : null;
    private Texture2D GetAtlas(int index) => numberAtlases!=null && index>=0 && index<numberAtlases.Length ? numberAtlases[index] : null;

    private void SpawnStyled(float damage, Transform target, Color color, Material material,int styleIndex=-1,bool critical=false)
    {
        if (popupPrefab == null || popupRoot == null || canvas == null || target == null)
        {
            Debug.LogWarning("DamagePopup: Missing necessary component.");
            return;
        }

        GameObject go = Instantiate(popupPrefab, popupRoot);
        if (critical) go.transform.localScale *= 1.2f;
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        string value = Mathf.RoundToInt(damage).ToString();
        var atlas = GetAtlas(styleIndex);

        if (atlas != null)
        {
            if (text != null) text.enabled = false;
            var display = go.AddComponent<DamageDigitDisplay>();
            display.SetValue(atlas, value, digitHeight, digitSpacing, maxNumberWidth);
        }
        else if (text != null)
        {
            text.text = value;
            text.color = color;
            if(material != null)
            {
                text.fontSharedMaterial=material;
                text.color=Color.white;
                text.fontStyle=FontStyles.Bold;
                text.extraPadding=true;
                text.UpdateMeshPadding();
            }
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            if(material!=null)DamageNumberAccent.Attach(text,styleIndex,material.GetColor("_OutlineColor"));
        }

        if (critical)
        {
            float numberWidth = atlas != null || text == null
                ? ((RectTransform)go.transform).rect.width
                : text.GetPreferredValues(value).x;
            CritPopupMarker.Attach(go, numberWidth);
        }

        var instance = go.AddComponent<DamagePopupInstance>();
        instance.Initialize(target, canvas, floatSpeed, lifetime, worldOffset, GetPopupOffset(target));
    }

    private Vector2 GetPopupOffset(Transform target)
    {
        if (!popupSequences.TryGetValue(target, out var sequence) ||
            Time.unscaledTime - sequence.LastSpawnTime > sameTargetResetTime)
        {
            sequence.Count = 0;
        }

        int index = sequence.Count++;
        sequence.LastSpawnTime = Time.unscaledTime;
        popupSequences[target] = sequence;

        // Keep hits centered, stacking simultaneous numbers vertically.
        return new Vector2(0f, index * Mathf.Max(verticalSpacing, 28f));
    }

    private Color GetColorForElement(Element element)
    {
        return element switch
        {
            Element.Phys => physicalColor,
            Element.Fire => fireColor,
            Element.Cold => coldColor,
            Element.Light => lightningColor,
            Element.Poison => poisonColor,
            Element.Void => voidColor,
            _ => defaultColor
        };
    }

    private Color GetColorForStatus(StatusEffects effect)
    {
        if (effect == null)
            return defaultColor;

        return effect.Ailment switch
        {
            StatusEffects.AilmentKind.Poison => poisonColor,
            StatusEffects.AilmentKind.Bleed => bleedColor,
            StatusEffects.AilmentKind.Ignite => fireColor,
            _ => effect.DamageColor
        };
    }
}

/// <summary>A compact star owned by one popup, including atlas-backed numbers.</summary>
public static class CritPopupMarker
{
    static Sprite sprite;
    public static void Attach(GameObject popup, float numberWidth)
    {
        if (popup == null || popup.transform is not RectTransform number) return;
        var marker = new GameObject("Critical hit marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        marker.transform.SetParent(number, false);
        var rect = (RectTransform)marker.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(20f, 20f);
        rect.anchoredPosition = new Vector2(-Mathf.Max(16f,numberWidth) * .5f - 14f, 2f);
        var image = marker.GetComponent<Image>();
        image.sprite = sprite ??= CreateStar();
        image.raycastTarget = false;
    }

    static Sprite CreateStar()
    {
        const int size = 24;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "Critical hit star", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            int dx = Mathf.Abs(x - 12), dy = Mathf.Abs(y - 12);
            bool vertical = dx <= 2 && dy <= 10 - dx;
            bool horizontal = dy <= 2 && dx <= 10 - dy;
            bool diagonal = dx + dy <= 7;
            if (!vertical && !horizontal && !diagonal) continue;
            bool edge = dx + dy >= 6 || dx == 2 || dy == 2;
            pixels[y * size + x] = edge ? new Color32(82, 42, 6, 255) : new Color32(255, 225, 82, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
    }
}

public class DamagePopupInstance : MonoBehaviour
{
    private Vector3 worldPosition;
    private Canvas canvas;
    private RectTransform rectTransform;
    private float floatSpeed;
    private float lifetime;
    private float timer;
    private Vector2 screenOffset;
    private float yOffset;

    public void Initialize(Transform target, Canvas canvas, float floatSpeed, float lifetime, Vector3 worldOffset, Vector2 screenOffset)
    {
        this.canvas = canvas;
        var actor = target.GetComponentInParent<PaperSpriteActor>();
        // Snapshot the stable head anchor. Lethal hits survive recipient destruction.
        worldPosition = actor != null ? actor.DamagePopupPosition : target.position + worldOffset;
        this.floatSpeed = floatSpeed;
        this.lifetime = lifetime;
        this.screenOffset = screenOffset;

        rectTransform = transform as RectTransform;
        if (rectTransform != null && rectTransform.parent is RectTransform parent)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = parent.pivot;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            PositionPopup();
        }
    }

    private void Update()
    {
        if (rectTransform == null || canvas == null)
        {
            Destroy(gameObject);
            return;
        }

        yOffset += floatSpeed * Time.deltaTime;
        PositionPopup();
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    private void PositionPopup()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Destroy(gameObject);
            return;
        }

        var parent = rectTransform.parent as RectTransform;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        // CanvasScaler-safe conversion; do not assign raw screen pixels to UI positions.
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, mainCamera.WorldToScreenPoint(worldPosition), uiCamera, out var localPoint))
        {
            rectTransform.anchoredPosition = localPoint + screenOffset + Vector2.up * yOffset;
        }
    }
}
