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
        float bonus = defender != null ? defender.GetStat(StatTypes.MaxAllRes) : 0f;
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
    {
        if (damage <= 0f) return 0f;
        armour = Mathf.Max(0f, armour);
        float reduction = Mathf.Clamp01(armour / (armour + 10f * damage));
        reduction = Mathf.Clamp(reduction - physicalPenetration, -0.9f, 0.9f);
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
            if (hit.Element == element)
                total += Mathf.Max(0f, CalculateFinalHitComponent(hit, ctx.Scopes, attacker, defender));
        return total;
    }

    private static float CalculateFinalHitComponent(ElementalHit hit, DamageScope scopes,
        StatsComponent attacker, StatsComponent defender)
    {
        float damage = hit.Amount * ScopedDamageMultiplier(scopes, attacker);
        return hit.Element == Element.Phys
            ? ApplyArmourAndPenetration(damage, attacker, defender)
            : ApplyResistancesAndPenetration(damage, hit.Element, attacker, defender);
    }

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
        if (element == Element.Fire || element == Element.Cold || element == Element.Light
            || element == Element.Void || element == Element.Poison)     //AllRes applies to every core non-Physical hit; Poison is legacy Void.
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

        float flatArmour = defender.GetStat(StatTypes.FlatArmour);
        float percentArmour = defender.GetStat(StatTypes.ArmourPercent);
        float totalArmour = flatArmour * (1f + percentArmour);  //Grab the flat and percent armour values and multiply them for the total armour.
        var keystones = defender.GetComponent<PassiveKeystoneState>();
        if (keystones != null) totalArmour *= keystones.DefenseMultiplier;

        float penetration = attacker != null
            ? attacker.GetStat(StatTypes.PhysPenetration)
            : 0f;
        return ApplyArmourValue(physDamage, totalArmour, penetration);
    }

    // ----------------- NEW: DOT DEFENCES -----------------

    /// <summary>
    /// Applies ailment-specific resists and penetration to DOT damage.
    /// Uses PoisonRes/BleedRes/IgniteRes + AllAilmentRes, minus PoisonPen/BleedPen/IgnitePen.
    /// </summary>
    public static float CalculateAilmentTickDamage(float baseTickDamage, StatusEffects effect, StatsComponent attacker, StatsComponent defender)   // required for resists
    {
        if (baseTickDamage <= 0f || effect == null || defender == null)     //If effect has no dmg, is null, or defender is null, return
            return 0f;

        if (effect.Ailment == StatusEffects.AilmentKind.Poison)
        {
            float voidResistance = defender.GetStat(StatTypes.VoidRes) + defender.GetStat(StatTypes.AllRes);
            float voidPenetration = attacker != null ? attacker.GetStat(StatTypes.VoidPenetration) : 0f;
            return ApplyResistanceValue(baseTickDamage, voidResistance, voidPenetration,
                GetMaximumResistance(Element.Void, defender));
        }

        float resAilment = 0f;
        float resAllAil = defender.GetStat(StatTypes.AllAilmentRes);        //Grab the all ailment rest stat
        float penAilment = 0f;

        switch (effect.Ailment)     //Check which ailment effect was passed in then grab the defender's res stat and attacker's pen stat for that ailment
        {
            case StatusEffects.AilmentKind.Bleed:
                resAilment = defender.GetStat(StatTypes.BleedRes);
                if (attacker != null)
                    penAilment = attacker.GetStat(StatTypes.BleedPenetration);
                break;

            case StatusEffects.AilmentKind.Ignite:
                resAilment = defender.GetStat(StatTypes.IgniteRes);
                if (attacker != null)
                    penAilment = attacker.GetStat(StatTypes.IgnitePenetration);
                break;

            default:
                break;
        }

        float totalRes = resAilment + resAllAil - penAilment;       //Calculate the total res by adding the specific ailment res to all ailment res and subtracting the attacker's pen
        totalRes = Mathf.Clamp(totalRes, -0.9f, 0.9f);      //Clamp it at -90% to 90%

        return baseTickDamage * (1f - totalRes);        // Resistance is already a fraction; do not divide by 100 again.
    }
}
