using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float lifetime = 0.75f;

    /// <summary>
    /// Spawns a damage number at the given world position.
    /// popupPrefab should have a TextMeshProUGUI somewhere in its children.
    /// </summary>
    public void Spawn(float damage, Transform worldPosition)
    {
        if (popupPrefab == null)
        {
            Debug.LogWarning("DamagePopup: popupPrefab is not assigned.");
            return;
        }

        GameObject go = Instantiate(popupPrefab, worldPosition.position, Quaternion.identity);
        var text = go.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            text.text = Mathf.RoundToInt(damage).ToString();
        }

        var instance = go.AddComponent<DamagePopupInstance>();
        instance.Initialize(floatSpeed, lifetime);
    }
}

class DamagePopupInstance : MonoBehaviour
{
    private float floatSpeed;
    private float lifetime;
    private float timer;

    public void Initialize(float floatSpeed, float lifetime)
    {
        this.floatSpeed = floatSpeed;
        this.lifetime = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
