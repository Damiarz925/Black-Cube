// Developer map: Single entry point for already-mitigated damage and its popup. The strongest raw component chooses a mixed hit color; it does not split the life deduction into multiple hits.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class DamageReceiver : MonoBehaviour
{
    [SerializeField] private Transform popupAnchor;
    private Transform PopupTarget => popupAnchor != null ? popupAnchor : transform;
    private HealthComponent health;
    private DamagePopup damagePopup;
    private PaperSpriteActor paperSprite;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        paperSprite = GetComponent<PaperSpriteActor>();
    }

    private void Start()
    {
        damagePopup = FindFirstObjectByType<DamagePopup>();
    }

    public void TakeDamage(float damage, Element element = Element.Phys, StatusEffects effect = null)
    {
        if (damage <= 0f)
            return;

        var keystones = GetComponent<PassiveKeystoneState>();
        if (keystones != null) damage = keystones.RedirectDamageToMana(damage);
        if (damage <= 0f) return;

        if (health != null)
        {
            health.LoseLife(damage);
            // Presentation-only notification after damage resolves. Lethal hits
            // skip the flinch so death/replacement always supersedes it.
            if (health.CurrentLife > 0f)
                paperSprite?.PlayHitReaction();
        }

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
