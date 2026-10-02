// Developer map: Pure hit and DOT mitigation shared by both sides. Evasion has no hit roll here; physical damage uses armour and physical penetration rather than an elemental resistance.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public static class CombatCalculator
{
    // Base crit is real damage even before any increased critical multiplier affix.
    public const float BaseCriticalMultiplier = 1.5f;
    public const float BaseMaximumResistance = .75f;
    public const float HardMaximumResistance = .90f;

    public static float ScaleOutgoingDamage(float baseAmount, float genericIncreased,
        float matchingIncreased, float genericMore, float matchingMore)
    {
        return baseAmount * (1f + genericIncreased + matchingIncreased)
            * (1f + genericMore) * (1f + matchingMore);
    }

    public static float ApplyResistanceValue(float damage, float resistance, float penetration)
    {
        return ApplyResistanceValue(damage, resistance, penetration, HardMaximumResistance);
    }

    public static float ApplyResistanceValue(float damage, float resistance, float penetration, float maximumResistance)
    {
        maximumResistance = Mathf.Clamp(maximumResistance, 0f, HardMaximumResistance);
        float cappedResistance = Mathf.Min(resistance, maximumResistance);
        float reduction = Mathf.Clamp(cappedResistance - penetration, -HardMaximumResistance, maximumResistance);
        return damage * (1f - reduction);
    }

    public static float GetMaximumResistance(Element element, StatsComponent defender)
    {
        float bonus = defender != null && (element is Element.Fire or Element.Cold or Element.Light)
            ? defender.GetStat(StatTypes.MaxAllRes) : 0f;
        if (defender != null)
        {
            bonus += element switch
            {
                Element.Fire => defender.GetStat(StatTypes.MaxFireRes),
                Element.Cold => defender.GetStat(StatTypes.MaxColdRes),
                Element.Light => defender.GetStat(StatTypes.MaxLightRes),
                Element.Void or Element.Poison => defender.GetStat(StatTypes.MaxVoidRes),
                _ => 0f
            };
        }
        return Mathf.Clamp(BaseMaximumResistance + bonus, 0f, HardMaximumResistance);
    }

    public static float ApplyArmourValue(float damage, float armour, float physicalPenetration)
        => ApplyArmourValue(damage, armour, 0f, physicalPenetration);

    // Explicit PDR adds to the final Armour-derived reduction before ordinary
    // Physical Penetration is applied once.
    public static float ApplyArmourValue(float damage, float armour, float explicitReduction,
        float physicalPenetration)
    {
        if (damage <= 0f) return 0f;
        armour = Mathf.Max(0f, armour);
        float reduction = Mathf.Clamp01(armour / (armour + 10f * damage));
        reduction = Mathf.Clamp(reduction + explicitReduction - physicalPenetration, -0.9f, 0.9f);
        return damage * (1f - reduction);
    }

    //Function for calculating actual hit damage based on player/enemy stats
    public static float CalculateFinalDamage(DamageContext ctx, StatsComponent attacker, StatsComponent defender)
    {
        float totalDamageTaken = 0f;

        foreach (var hit in ctx.Hits)   //Loop through each "hit" (damage element) in context, and apply the armour using the applyArmour method, and apply resistances and penetration, then return the total
        {
            float d = CalculateFinalHitComponent(hit, ctx.Scopes, attacker, defender);
            if (d > 0f)
                totalDamageTaken += d;      //If D is greater than 0 after accounting for armour and resistances, add it to the total damage taken and return it
        }

        return totalDamageTaken;
    }

    public static float CalculateFinalElementDamage(DamageContext ctx, Element element,
        StatsComponent attacker, StatsComponent defender)
    {
        float total = 0f;
        if (ctx.Hits == null) return total;
        foreach (ElementalHit hit in ctx.Hits)
            if (ResolvedElement(hit.Element,attacker) == element)
                total += Mathf.Max(0f, CalculateFinalHitComponent(hit, ctx.Scopes, attacker, defender));
        return total;
    }

    private static float CalculateFinalHitComponent(ElementalHit hit, DamageScope scopes,
        StatsComponent attacker, StatsComponent defender)
    {
        if(hit.Element==Element.True)return Mathf.Max(0f,hit.Amount);
        float damage = hit.Amount * ScopedDamageMultiplier(scopes, attacker);
        if(attacker!=null)damage*=attacker.GetComponent<SubclassCombatState>()?.AuraDamageMultiplier(hit.Element)??1;
        var keys=attacker!=null?attacker.GetComponent<PassiveKeystoneState>():null;
        Element resolved=ResolvedElement(hit.Element,attacker);
        if(keys!=null)
        {
            if(keys.Has(PassiveKeystone.MageFire))
            {if(resolved!=Element.Fire)return 0;damage*=PassiveKeystoneState.Value(PassiveKeystone.MageFire);}
            damage*=keys.GenericMoreMultiplier;
            if(keys.Has(PassiveKeystone.ThiefOpener))
            {var health=defender!=null?defender.GetComponent<HealthComponent>():null;if(health!=null)damage*=ClassKeystoneMechanics.TargetLifeMultiplier(health.CurrentLife>=health.MaxLife);}
        }
        if(defender!=null&&defender.GetComponent<StatusController>()?.IsFractured==true)
            damage*=ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary;
        if(resolved==Element.Fire&&defender?.GetComponent<EnemyAI>()!=null)damage*=attacker?.GetComponent<UniqueCombatRuntime>()?.FireTakenMultiplier??1;
        float mitigated;
        if(resolved==Element.Phys)mitigated=ApplyArmourAndPenetration(damage,attacker,defender);
        else
        {
            if(defender!=null&&(resolved is Element.Fire or Element.Cold or Element.Light))
            {
                float fraction=defender.GetStat(StatTypes.ElementalPlating);
                if(fraction>0)damage=ApplyArmourValue(damage,TotalArmour(defender)*Mathf.Clamp01(fraction),0,0);
            }
            mitigated=ApplyResistancesAndPenetration(damage,resolved,attacker,defender);
        }
        return mitigated*(1+(defender?.GetComponent<StatusController>()?.CombinedShockEffect??0));
    }

    public static Element ResolvedElement(Element source,StatsComponent attacker)
        =>source==Element.Phys&&attacker!=null&&attacker.GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.BarbarianFire)==true?Element.Fire:source;

    /// <summary>One additive increased-damage bucket for every explicit scope on the source.</summary>
    public static float ScopedDamageMultiplier(DamageScope scopes, StatsComponent attacker)
    {
        if (attacker == null || scopes == DamageScope.None) return 1f;
        float increased = 0f;
        if ((scopes & DamageScope.Magic) != 0) increased += attacker.GetStat(StatTypes.MagicDmg);
        if ((scopes & DamageScope.Projectile) != 0)
            increased += attacker.GetStat(StatTypes.ProjectileDmg) + DerivedStatCalculator.ProjectileIncreasedDamage(attacker);
        if ((scopes & DamageScope.Minion) != 0) increased += attacker.GetStat(StatTypes.MinionDmg);
        return Mathf.Max(0f, 1f + increased);
    }

    // ----------------- EXISTING HIT DEFENCES -----------------

    //Function for applying resistances to the damage
    static float ApplyResistancesAndPenetration(float damage, Element element, StatsComponent attacker, StatsComponent defender)
    {
        if (damage <= 0f || defender == null)       //if damage or defender is 0/null, return
            return 0f;

        StatTypes resType = StatMappings.GetResistStat(element);        //Grab resistance stat for the passed in element
        float res = defender.GetStat(resType);      //Get the actual resistance amount for that element.

        float allRes = defender.GetStat(StatTypes.AllRes); //Get the actual all res stat

        float totalRes = res;
        if (element == Element.Fire || element == Element.Cold || element == Element.Light)
            totalRes += allRes;

        float pen = 0f;
        if (attacker != null)       //If attacker isn't null, find their penetration amount for the given element
        {
            StatTypes penType = StatMappings.GetPenetrationStat(element);
            pen = attacker.GetStat(penType);
        }

        return ApplyResistanceValue(damage, totalRes, pen, GetMaximumResistance(element, defender));
    }

    // Applies the existing armour formula, then subtracts physical penetration
    // from that percentage reduction. The final reduction uses the same +/-90%
    // bounds as elemental resistance and penetration.
    static float ApplyArmourAndPenetration(float physDamage, StatsComponent attacker, StatsComponent defender)
    {
        if (physDamage <= 0f || defender == null)       //If physical damage or defender is 0/null, return
            return 0f;

        float totalArmour=TotalArmour(defender);

        float penetration = attacker != null
            ? attacker.GetStat(StatTypes.PhysPenetration)
            : 0f;
        return ApplyArmourValue(physDamage, totalArmour,
            defender.GetStat(StatTypes.PhysicalDamageReduction), penetration);
    }

    public static float TotalArmour(StatsComponent defender)
    {
        if(defender==null)return 0;
        float total=defender.GetStat(StatTypes.FlatArmour)*(1f+defender.GetStat(StatTypes.ArmourPercent));
        var keys=defender.GetComponent<PassiveKeystoneState>();
        return Mathf.Max(0,total*(keys!=null?keys.DefenseMultiplier:1f));
    }

    // ----------------- NEW: DOT DEFENCES -----------------

    /// <summary>
    /// Damaging ailments inherit their ordinary damage type's mitigation and penetration.
    /// </summary>
    public static float CalculateAilmentTickDamage(float baseTickDamage, StatusEffects effect, StatsComponent attacker, StatsComponent defender)   // required for resists
    {
        if (baseTickDamage <= 0f || effect == null || defender == null)     //If effect has no dmg, is null, or defender is null, return
            return 0f;

        var keys=attacker!=null?attacker.GetComponent<PassiveKeystoneState>():null;
        if(keys?.Has(PassiveKeystone.MageFire)==true)
        {if(effect.Ailment!=StatusEffects.AilmentKind.Ignite)return 0;baseTickDamage*=PassiveKeystoneState.Value(PassiveKeystone.MageFire);}
        if(keys!=null)baseTickDamage*=keys.GenericMoreMultiplier;
        if(defender.GetComponent<StatusController>()?.IsFractured==true)baseTickDamage*=ClassKeystoneCatalog.Get(PassiveKeystone.PriestFracture).secondary;
        if(keys?.Has(PassiveKeystone.ThiefOpener)==true)
        {var health=defender.GetComponent<HealthComponent>();if(health!=null)baseTickDamage*=ClassKeystoneMechanics.TargetLifeMultiplier(health.CurrentLife>=health.MaxLife);}

        if(effect.Ailment==StatusEffects.AilmentKind.Ignite)
            baseTickDamage*=UniqueCombatRuntime.For(attacker)?.FireTakenMultiplier??1;
        // Damage-taken conversion precedes mitigation for damaging ailments too.
        // Ignite is already Fire, so it must not be converted a second time.
        float takenAsFire=effect.Ailment==StatusEffects.AilmentKind.Ignite?0f:
            Mathf.Clamp01(defender.GetComponent<SubclassCombatState>()?.Has(SubclassIds.BarbarianFire)==true
                ?defender.GetStat(StatTypes.DamageTakenAsFire):0f);
        float firePortion=baseTickDamage*takenAsFire;
        baseTickDamage-=firePortion;
        float mitigated=effect.Ailment switch
        {
            StatusEffects.AilmentKind.Bleed => ApplyArmourAndPenetration(baseTickDamage, attacker, defender),
            StatusEffects.AilmentKind.Ignite => ApplyResistancesAndPenetration(baseTickDamage, Element.Fire, attacker, defender),
            StatusEffects.AilmentKind.Poison => ApplyResistancesAndPenetration(baseTickDamage, Element.Void, attacker, defender),
            _ => baseTickDamage
        };
        if(firePortion>0f)mitigated+=ApplyResistancesAndPenetration(firePortion,Element.Fire,attacker,defender);
        return mitigated*(1+(defender.GetComponent<StatusController>()?.CombinedShockEffect??0));
    }
}
