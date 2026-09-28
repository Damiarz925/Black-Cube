// Developer map: Deduct each logical hit once, then fan out its mitigated typed
// components into styled popups. Presentation never generates extra on-hit events.
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

        float actualLifeLoss = damage;
        if (health != null)
        {
            float before = health.CurrentLife;
            RecordIncoming(Mathf.Min(damage,before),new DamageContext(1),null,element,effect);
            health.LoseLife(damage);
            actualLifeLoss = Mathf.Max(0f, before - health.CurrentLife);
            if(GetComponent<PlayerController>()!=null)
                (GetComponent<RevengeState>()??gameObject.AddComponent<RevengeState>()).RecordHit(actualLifeLoss,health.MaxLife);
            // Presentation-only notification after damage resolves. Lethal hits
            // skip the flinch so death/replacement always supersedes it.
            if (health.CurrentLife > 0f)
                paperSprite?.PlayHitReaction();
        }

        SpawnDamagePopup(actualLifeLoss, element, effect, false);
    }

    public void TakeDamage(float damage, DamageContext context, StatusEffects effect = null, StatsComponent attacker = null)
    {
        if (damage <= 0f) return;
        // Capture the typed, mitigated proportions before life loss changes target-
        // conditional modifiers (e.g. full-Life bonuses). Deduct life exactly once.
        var components=new DamageContext(5);
        if(effect==null)
            foreach(Element element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void})
            {
                float amount=CombatCalculator.CalculateFinalElementDamage(context,element,attacker,GetComponent<StatsComponent>());
                if(amount>0)components.AddDamage(element,amount);
            }
        var keystones = GetComponent<PassiveKeystoneState>();
        if (keystones != null) damage = keystones.RedirectDamageToMana(damage);
        if (damage <= 0f) return;
        float before = health != null ? health.CurrentLife : damage;
        var recap=components.Hits.Count>0?components:context;
        recap.IsCrit=context.IsCrit;recap.EventTags=context.EventTags;
        RecordIncoming(Mathf.Min(damage,before),recap,attacker,GetPrimaryElement(context),effect);
        health?.LoseLife(damage);
        float actualLifeLoss = health != null ? Mathf.Max(0f, before - health.CurrentLife) : damage;
        if(health!=null&&GetComponent<PlayerController>()!=null)
            (GetComponent<RevengeState>()??gameObject.AddComponent<RevengeState>()).RecordHit(actualLifeLoss,health.MaxLife);
        if (health != null && health.CurrentLife > 0f) paperSprite?.PlayHitReaction();
        if(effect!=null||components.Hits.Count==0)SpawnDamagePopup(actualLifeLoss, GetPrimaryElement(context), effect, context.IsCrit);
        else
        {
            if(damagePopup==null)damagePopup=DamagePopup.Instance;
            float total=0;foreach(var hit in components.Hits)total+=hit.Amount;
            foreach(var hit in components.Hits)damagePopup?.Spawn(actualLifeLoss*hit.Amount/total,PopupTarget,hit.Element,context.IsCrit,context.IsPrecision);
        }
    }

    private void SpawnDamagePopup(float damage, Element element, StatusEffects effect, bool critical)
    {
        if (damagePopup == null)
            damagePopup = FindFirstObjectByType<DamagePopup>();

        if (damagePopup == null)
            return;

        if (effect != null)
            damagePopup.Spawn(damage, PopupTarget, effect);
        else
            damagePopup.Spawn(damage, PopupTarget, element, critical);
    }

    void RecordIncoming(float amount,DamageContext context,StatsComponent attacker,Element fallback=Element.Phys,StatusEffects ailment=null)
    {
        if(GetComponent<PlayerController>()==null)return;
        if(context.Hits.Count==0)context.AddDamage(fallback,amount);
        (GetComponent<IncomingDamageHistory>()??gameObject.AddComponent<IncomingDamageHistory>()).Record(amount,context,attacker,GetComponent<StatsComponent>(),ailment);
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
