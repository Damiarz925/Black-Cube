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
    [SerializeField] private float horizontalSpacing = 34f;
    [SerializeField] private float verticalSpacing = 2f;

    [Header("Damage Colors")]
    [SerializeField] private Color physicalColor = Color.black;
    [SerializeField] private Color fireColor = new Color(1f, 0.45f, 0f);
    [SerializeField] private Color coldColor = new Color(0.15f, 0.55f, 1f);
    [SerializeField] private Color lightningColor = Color.yellow;
    [SerializeField] private Color poisonColor = new Color(0.1f, 0.8f, 0.25f);
    [SerializeField] private Color bleedColor = new Color(0.85f, 0.05f, 0.05f);
    [SerializeField] private Color defaultColor = Color.white;

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
        Spawn(damage, target, GetColorForElement(element));
    }

    public void Spawn(float damage, Transform target, StatusEffects effect)
    {
        Spawn(damage, target, GetColorForStatus(effect));
    }

    public void Spawn(float damage, Transform target, Color color)
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

        int column = (index % 5) - 2;
        int row = index / 5;

        return new Vector2(column * horizontalSpacing, row * verticalSpacing);
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
    private Transform target;
    private RectTransform rectTransform;
    private float floatSpeed;
    private float lifetime;
    private float timer;
    private Vector3 worldOffset;
    private Vector2 screenOffset;
    private float yOffset;

    public void Initialize(Transform target, Canvas canvas, float floatSpeed, float lifetime, Vector3 worldOffset, Vector2 screenOffset)
    {
        this.target = target;
        this.floatSpeed = floatSpeed;
        this.lifetime = lifetime;
        this.worldOffset = worldOffset;
        this.screenOffset = screenOffset;

        rectTransform = transform as RectTransform;
    }

    private void Update()
    {
        if (target == null || rectTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position + worldOffset);

        yOffset += floatSpeed * Time.deltaTime;
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        screenPos.y += yOffset;

        rectTransform.position = screenPos;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
