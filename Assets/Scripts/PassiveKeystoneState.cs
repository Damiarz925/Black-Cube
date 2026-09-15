// Runtime projection of allocated inner keystones onto the player's combat/resource pipelines.
using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
public sealed class PassiveKeystoneState : MonoBehaviour
{
    readonly bool[] active = new bool[System.Enum.GetValues(typeof(PassiveKeystone)).Length];

    public void Apply(PlayerProgression progression)
    {
        for (int i = 0; i < active.Length; i++) active[i] = false;
        if (progression == null) return;
        for (int i = 1; i < active.Length; i++) active[i] = progression.HasKeystone((PassiveKeystone)i);
    }

    public bool Has(PassiveKeystone keystone) => keystone != PassiveKeystone.None && active[(int)keystone];

    public float MaximumLifeMultiplier
    {
        get
        {
            float factor = 1f;
            if (Has(PassiveKeystone.LivingFortress)) factor *= 1.5f;
            if (Has(PassiveKeystone.ManaShield)) factor *= .75f;
            if (Has(PassiveKeystone.IronBastion)) factor *= .7f;
            if (Has(PassiveKeystone.UndyingFlesh)) factor *= .75f;
            return factor;
        }
    }

    public float DefenseMultiplier
    {
        get
        {
            float factor = 1f;
            if (Has(PassiveKeystone.LivingFortress)) factor *= .5f;
            if (Has(PassiveKeystone.IronBastion)) factor *= 1.75f;
            return factor;
        }
    }

    public float MaximumManaMultiplier => Has(PassiveKeystone.EndlessCurrent) ? .75f : 1f;
    public float AttackSpeedMultiplier => (Has(PassiveKeystone.BruteForce) ? .75f : 1f)
        * (Has(PassiveKeystone.Frenzy) ? 1.5f : 1f);
    public float ManaCostMultiplier => Has(PassiveKeystone.ArcaneOverload) ? 2f : 1f;
    public bool TransmutesHitsToPoison => Has(PassiveKeystone.VenomousTransmutation);
    // Deep Freeze is the finalized +10 percentage-point slow-cap specialization.
    public float DeepFreezeMaximumEffectIncrease => Has(PassiveKeystone.DeepFreeze) ? .10f : 0f;
    public float ChillEffectMultiplier => Has(PassiveKeystone.DeepFreeze) ? .75f : 1f;
    public float ShockStackRequirementMultiplier => Has(PassiveKeystone.Overcharged) ? .5f : 1f;
    public float ShockTriggeredHitMultiplier => Has(PassiveKeystone.Overcharged) ? .65f : 1f;
    public float LifeRegenerationMultiplier => Has(PassiveKeystone.UndyingFlesh) ? 2f : 1f;
    public float ManaRegenerationMultiplier => Has(PassiveKeystone.EndlessCurrent) ? 2f : 1f;
    public int ProjectileAmountBonus => Has(PassiveKeystone.BulletHell) ? 2 : 0;

    public float ChanceMultiplier(StatTypes stat) => stat switch
    {
        StatTypes.BleedChance => Has(PassiveKeystone.OpenWounds) ? 2f : 1f,
        StatTypes.IgniteChance => Has(PassiveKeystone.Wildfire) ? 2f : 1f,
        StatTypes.ChillChance => Has(PassiveKeystone.DeepFreeze) ? 2f : 1f,
        StatTypes.ShockChance => Has(PassiveKeystone.Overcharged) ? 2f : 1f,
        StatTypes.PoisonChance => Has(PassiveKeystone.ToxicSaturation) ? 2f : 1f,
        StatTypes.ChanceToHitTwice => Has(PassiveKeystone.EchoingStrikes) ? 2f : 1f,
        _ => 1f
    };

    public float AilmentDamageMultiplier(StatusEffects.AilmentKind ailment) => ailment switch
    {
        StatusEffects.AilmentKind.Bleed => Has(PassiveKeystone.OpenWounds) ? .65f : 1f,
        StatusEffects.AilmentKind.Ignite => Has(PassiveKeystone.Wildfire) ? .7f : 1f,
        StatusEffects.AilmentKind.Poison => Has(PassiveKeystone.ToxicSaturation) ? .6f : 1f,
        _ => 1f
    };

    public int EffectiveAilmentStackCap(StatusEffects effect)
    {
        if (effect == null || effect.MaxStacks <= 0) return effect != null ? effect.MaxStacks : 0;
        if (effect.Ailment == StatusEffects.AilmentKind.Bleed && Has(PassiveKeystone.OpenWounds)) return effect.MaxStacks * 2;
        if (effect.Ailment == StatusEffects.AilmentKind.Ignite && Has(PassiveKeystone.Wildfire)) return effect.MaxStacks + 1;
        return effect.MaxStacks;
    }

    public DamageContext TransformOutgoing(DamageContext source)
    {
        var result = Copy(source);
        Element? exclusive = null;
        if (Has(PassiveKeystone.BruteForce)) exclusive = Element.Phys;
        else if (Has(PassiveKeystone.AbsoluteZero)) exclusive = Element.Cold;
        else if (Has(PassiveKeystone.InfernalConversion)) exclusive = Element.Fire;
        else if (Has(PassiveKeystone.LivingCurrent)) exclusive = Element.Light;

        if (exclusive.HasValue)
        {
            float allowed = 0f;
            float converted = 0f;
            if (source.Hits != null)
            {
                foreach (var hit in source.Hits)
                    if (hit.Element == exclusive.Value) allowed += hit.Amount;
                    else if (!Has(PassiveKeystone.BruteForce)) converted += hit.Amount * .5f;
            }
            result.Hits.Clear();
            result.AddDamage(exclusive.Value, allowed + converted);
        }

        float factor = 1f;
        if (Has(PassiveKeystone.BruteForce)) factor *= 2f;
        else if (exclusive.HasValue) factor *= 1.25f;
        if (Has(PassiveKeystone.ArcaneOverload) && (source.Scopes & DamageScope.Magic) != 0) factor *= 1.4f;
        factor *= Has(PassiveKeystone.BallisticBarrage)
            ? (source.Scopes & DamageScope.Projectile) != 0 ? 1.5f : .5f
            : 1f;
        if (Has(PassiveKeystone.Frenzy)) factor *= .7f;
        if (Has(PassiveKeystone.EchoingStrikes)) factor *= .75f;
        if (Has(PassiveKeystone.BulletHell) && (source.Scopes & DamageScope.Projectile) != 0) factor *= .65f;
        if (!Mathf.Approximately(factor, 1f))
            for (int i = 0; i < result.Hits.Count; i++)
            {
                var hit = result.Hits[i];
                hit.Amount *= factor;
                result.Hits[i] = hit;
            }
        return result;
    }

    public static DamageContext AsPoisonBasis(DamageContext source)
    {
        var result = new DamageContext(1)
        {
            IsCrit = source.IsCrit, CritMultiplier = source.CritMultiplier, Scopes = source.Scopes
        };
        float total = 0f;
        if (source.Hits != null) foreach (var hit in source.Hits) total += Mathf.Max(0f, hit.Amount);
        result.AddDamage(Element.Void, total);
        return result;
    }

    public float RedirectDamageToMana(float damage)
    {
        if (!Has(PassiveKeystone.ManaShield) || damage <= 0f) return damage;
        var mana = GetComponent<ManaComponent>();
        if (mana == null) return damage;
        float requested = damage * .5f;
        float absorbed = mana.SpendUpTo(requested);
        return damage - absorbed;
    }

    static DamageContext Copy(DamageContext source)
    {
        var result = new DamageContext(source.Hits != null ? source.Hits.Count : 1)
        {
            IsCrit = source.IsCrit,
            CritMultiplier = source.CritMultiplier,
            Scopes = source.Scopes
        };
        if (source.Hits != null)
            foreach (var hit in source.Hits) result.AddDamage(hit.Element, hit.Amount);
        return result;
    }
}
