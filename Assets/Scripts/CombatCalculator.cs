using UnityEngine;

public static class CombatCalculator
{
    //Function for calculating actual hit damage based on player/enemy stats
    public static float CalculateFinalDamage(DamageContext ctx, StatsComponent attacker, StatsComponent defender)
    {
        float totalDamageTaken = 0f;

        foreach (var hit in ctx.Hits)   //Loop through each "hit" (damage element) in context, and apply the armour using the applyArmour method, and apply resistances and penetration, then return the total
        {
            float d = hit.Amount;

            // Physical: armour first
            if (hit.Element == Element.Phys)
                d = ApplyArmour(d, defender);   //Apply the armour to the hit (d)

            // Then elemental/Phys res (your existing hit pipeline)
            d = ApplyResistancesAndPenetration(d, hit.Element, attacker, defender);     //Calculate and apply resistances and penetration to the hit

            if (d > 0f)
                totalDamageTaken += d;      //If D is greater than 0 after accounting for armour and resistances, add it to the total damage taken and return it
        }

        return totalDamageTaken;
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
        if (element == Element.Fire || element == Element.Cold || element == Element.Light)     //If the current element isn't phys, add its resistance to all resistance
            totalRes += allRes;

        float pen = 0f;
        if (attacker != null)       //If attacker isn't null, find their penetration amount for the given element
        {
            StatTypes penType = StatMappings.GetPenetrationStat(element);
            pen = attacker.GetStat(penType);
        }

        totalRes -= pen;    //Subtract the attacker's penetration stat for the given element from the defender's resistance

        totalRes = Mathf.Clamp(totalRes, -0.9f, 0.9f);      //Clamp the resistance at -90% to 90% resistance

        return damage * (1f - totalRes);        //Convert the number and return the damage amount after resistance and penetration has been applied
    }

    //Function for applying armour to the physical damage hit
    static float ApplyArmour(float physDamage, StatsComponent defender)
    {
        if (physDamage <= 0f || defender == null)       //If physical damage or defender is 0/null, return
            return 0f;

        float flatArmour = defender.GetStat(StatTypes.FlatArmour);
        float percentArmour = defender.GetStat(StatTypes.ArmourPercent);
        float totalArmour = flatArmour * (1f + percentArmour);  //Grab the flat and percent armour values and multiply them for the total armour.

        float reduction = totalArmour / (totalArmour + 10f * physDamage);
        reduction = Mathf.Clamp01(reduction);

        return physDamage * (1f - reduction);
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

        float resAilment = 0f;
        float resAllAil = defender.GetStat(StatTypes.AllAilmentRes);        //Grab the all ailment rest stat
        float penAilment = 0f;

        switch (effect.Ailment)     //Check which ailment effect was passed in then grab the defender's res stat and attacker's pen stat for that ailment
        {
            case StatusEffects.AilmentKind.Poison:
                resAilment = defender.GetStat(StatTypes.PoisonRes);
                if (attacker != null)
                    penAilment = attacker.GetStat(StatTypes.PoisonPenetration);
                break;

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

        return baseTickDamage * (1f - totalRes);        //Calculate tick damage as base tick damage * 1 + totalres / 100 to convert to a decimal.
    }
}