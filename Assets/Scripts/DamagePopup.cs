using TMPro;
using UnityEngine;

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
    [SerializeField] private Color bleedColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color defaultColor = Color.white;
    [Header("Number Materials: physical, fire, cold, lightning, poison, bleed, void")]
    [SerializeField] private Material[] numberMaterials;

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

    /// <summary>
    /// Spawns a damage number at the given world position.
    /// popupPrefab should have a TextMeshProUGUI somewhere in its children.
    /// </summary>
    public void Spawn(float damage, Transform target)
    {
        Spawn(damage, target, defaultColor);
    }

    public void Spawn(float damage, Transform target, Element element)
    {
        int index=element switch {Element.Phys=>0,Element.Fire=>1,Element.Cold=>2,Element.Light=>3,Element.Poison=>4,_=>6};
        SpawnStyled(damage, target, GetColorForElement(element), GetMaterial(index),index);
    }

    public void Spawn(float damage, Transform target, StatusEffects effect)
    {
        int index=effect==null?6:effect.Ailment switch {StatusEffects.AilmentKind.Poison=>4,StatusEffects.AilmentKind.Bleed=>5,StatusEffects.AilmentKind.Ignite=>1,_=>6};
        SpawnStyled(damage, target, GetColorForStatus(effect), GetMaterial(index),index);
    }

    public void Spawn(float damage, Transform target, Color color)
    {
        SpawnStyled(damage,target,color,null);
    }

    private Material GetMaterial(int index) => numberMaterials!=null && index<numberMaterials.Length ? numberMaterials[index] : null;

    private void SpawnStyled(float damage, Transform target, Color color, Material material,int styleIndex=-1)
    {
        if (popupPrefab == null || popupRoot == null || canvas == null || target == null)
        {
            Debug.LogWarning("DamagePopup: Missing necessary component.");
            return;
        }

        GameObject go = Instantiate(popupPrefab, popupRoot);
        var text = go.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = Mathf.RoundToInt(damage).ToString();
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
