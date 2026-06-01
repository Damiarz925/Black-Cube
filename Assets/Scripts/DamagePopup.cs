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
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    public static DamagePopup Instance;

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
        }

        var instance = go.AddComponent<DamagePopupInstance>();
        instance.Initialize(target, canvas, floatSpeed, lifetime, worldOffset);
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
    private float yOffset;

    public void Initialize(Transform target, Canvas canvas, float floatSpeed, float lifetime, Vector3 worldOffset)
    {
        this.target = target;
        this.floatSpeed = floatSpeed;
        this.lifetime = lifetime;
        this.worldOffset = worldOffset;

        rectTransform = transform as RectTransform;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + worldOffset);

        yOffset += floatSpeed * Time.deltaTime;
        screenPos.y += yOffset;

        rectTransform.position = screenPos;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
