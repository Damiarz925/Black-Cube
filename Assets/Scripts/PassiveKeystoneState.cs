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
    public void ApplyAllocatedNodes(System.Collections.Generic.IEnumerable<PassiveNodeDefinition> nodes)
    {
        System.Array.Clear(active,0,active.Length);
        if(nodes==null)return;
        foreach(var node in nodes)if(node.Keystone!=PassiveKeystone.None)active[(int)node.Keystone]=true;
    }
    public float MaximumLifeMultiplier
    {
        get
        {
            float factor = 1f;
            if (Has(PassiveKeystone.ManaShield)) factor *= .75f;
            return factor;
        }
    }

    public float DefenseMultiplier
    {
        get
        {
            float factor = 1f;
            return factor;
        }
    }

    public float MaximumManaMultiplier => 1f;
    public float AttackSpeedMultiplier => (Has(PassiveKeystone.BruteForce) ? .75f : 1f)
        * (Has(PassiveKeystone.WarriorTempo) ? Value(PassiveKeystone.WarriorTempo) : 1f);
    public float ManaCostMultiplier => 1f;
    public bool TransmutesHitsToPoison => Has(PassiveKeystone.VenomousTransmutation);
    // Deep Freeze is the finalized +10 percentage-point slow-cap specialization.
    public float DeepFreezeMaximumEffectIncrease => 0f;
    public float ChillEffectMultiplier => 1f;
    public float ShockStackRequirementMultiplier => 1f;
    public float ShockTriggeredHitMultiplier => 1f;
    public float LifeRegenerationMultiplier => Has(PassiveKeystone.BarbarianRecovery) ? 0f : 1f;
    public float ManaRegenerationMultiplier => 1f;
    public int ProjectileAmountBonus => 0;

    public float ChanceMultiplier(StatTypes stat) => 1f;

    public float AilmentDamageMultiplier(StatusEffects.AilmentKind ailment)
    {
        if(ailment==StatusEffects.AilmentKind.Bleed&&Has(PassiveKeystone.WarriorBleed))return Value(PassiveKeystone.WarriorBleed);
        if(ailment==StatusEffects.AilmentKind.Poison&&Has(PassiveKeystone.RangerEndlessPoison))
            return Value(PassiveKeystone.RangerEndlessPoison)*(1+Mathf.Max(0,GetComponent<StatsComponent>().GetStat(StatTypes.PoisonDuration))*ClassKeystoneCatalog.Get(PassiveKeystone.RangerEndlessPoison).secondary);
        return 1f;
    }

    public static float Value(PassiveKeystone key) => ClassKeystoneCatalog.Get(key)?.primary ?? 1f;
    public float DamageRecoveryFraction => Has(PassiveKeystone.BarbarianRecovery)
        ? Mathf.Max(0,GetComponent<StatsComponent>().GetStat(StatTypes.LifeRegeneration))*Value(PassiveKeystone.BarbarianRecovery) : 0;
    public float GenericMoreMultiplier => (Has(PassiveKeystone.BarbarianRecovery)?ClassKeystoneCatalog.Get(PassiveKeystone.BarbarianRecovery).secondary:1)
        *(Has(PassiveKeystone.ThiefStealth)?Value(PassiveKeystone.ThiefStealth):1);

    public int EffectiveAilmentStackCap(StatusEffects effect)
    {
        if(effect==null)return 0;
        if(effect.Ailment==StatusEffects.AilmentKind.Ignite)return Has(PassiveKeystone.BarbarianFire)?2:1;
        if(effect.MaxStacks<=0)return effect.MaxStacks;
        return effect.Ailment==StatusEffects.AilmentKind.Bleed&&Has(PassiveKeystone.WarriorBleed)
            ? effect.MaxStacks*Mathf.RoundToInt(ClassKeystoneCatalog.Get(PassiveKeystone.WarriorBleed).secondary):effect.MaxStacks;
    }

    public DamageContext TransformOutgoing(DamageContext source)
    {
        var result = Copy(source);
        Element? exclusive = null;
        if (Has(PassiveKeystone.BruteForce)) exclusive = Element.Phys;
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
            IsCrit = source.IsCrit, CritMultiplier = source.CritMultiplier, Scopes = source.Scopes,
            IsPrecision=source.IsPrecision,PrecisionMultiplier=source.PrecisionMultiplier,WeaponMechanicsApplied=source.WeaponMechanicsApplied
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
            ,IsPrecision=source.IsPrecision,PrecisionMultiplier=source.PrecisionMultiplier,WeaponMechanicsApplied=source.WeaponMechanicsApplied
        };
        if (source.Hits != null)
            foreach (var hit in source.Hits) result.AddDamage(hit.Element, hit.Amount);
        return result;
    }
}
