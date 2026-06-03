using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class DamageReceiver : MonoBehaviour
{
    [SerializeField] private Transform popupAnchor;
    private Transform PopupTarget => popupAnchor != null ? popupAnchor : transform;
    private HealthComponent health;
    private DamagePopup damagePopup;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        damagePopup = FindFirstObjectByType<DamagePopup>();
    }

    public void TakeDamage(float damage, Element element = Element.Phys, StatusEffects effect = null)
    {
        if (damage <= 0f)
            return;

        if (health != null)
            health.LoseLife(damage);

        SpawnDamagePopup(damage, element, effect);
    }

    public void TakeDamage(float damage, DamageContext context, StatusEffects effect = null)
    {
        TakeDamage(damage, GetPrimaryElement(context), effect);
    }

    private void SpawnDamagePopup(float damage, Element element, StatusEffects effect)
    {
        if (damagePopup == null)
            damagePopup = FindFirstObjectByType<DamagePopup>();

        if (damagePopup == null)
            return;

        if (effect != null)
            damagePopup.Spawn(damage, PopupTarget, effect);
        else
            damagePopup.Spawn(damage, PopupTarget, element);
    }

    private Element GetPrimaryElement(DamageContext context)
    {
        if (context.Hits == null || context.Hits.Count == 0)
            return Element.Phys;

        Element primaryElement = context.Hits[0].Element;
        float highestAmount = context.Hits[0].Amount;

        for (int i = 1; i < context.Hits.Count; i++)
        {
            if (context.Hits[i].Amount <= highestAmount)
                continue;

            highestAmount = context.Hits[i].Amount;
            primaryElement = context.Hits[i].Element;
        }

        return primaryElement;
    }
}
