using UnityEngine;

//This class is used to calculate ailment damage so that it can be dealt to a target
public static class AilmentCalculator
{
    //ComputeAilmentFromHit does exactly that, computes the ailment based on the hit that applies it
    public static void ComputeAilmentFromHit(
        StatusEffects effect,
        DamageContext ctx,
        StatsComponent attacker,
        out float damagePerTick,
        out int tickCount,
        out int effectiveInterval)
    {
        damagePerTick = 0f;         //Create variables for damage per tick, tick count, and interval
        tickCount = 0;
        effectiveInterval = 1;

        if (effect == null || attacker == null)         //If there is no effect or attacker, return
            return;

        float sourceHitDamage = GetSourceHitDamage(effect, ctx);    //Grab the damage of the source hit that is allowed to apply the the ailment
        if (sourceHitDamage <= 0f)      //If it's 0, return.
            return;

        //Calculate the base ailment damage by multiplying source hit by the effect's set magnitude
        float baseAilmentDamage = sourceHitDamage * effect.Magnitude;

        //Define variable for total increased damage
        float incTotal = 0f;

        //As of now, we aren't going to add the increased generic, because it has already been added to the hit.
        //incTotal += attacker.GetStat(StatTypes.GenericDmg);

        //This switch statement adds the appropriate increased damage modifiers to the incTotal.
        //For now, I am not including phys or fire damage in the ailment calculations as they were already used to calculate the base hit. May change later.
        switch (effect.Ailment)
        {
            case StatusEffects.AilmentKind.Poison:
                //incTotal += attacker.GetStat(StatTypes.PhysDmg);
                incTotal += attacker.GetStat(StatTypes.PoisonDmg);
                break;

            case StatusEffects.AilmentKind.Bleed:
                //incTotal += attacker.GetStat(StatTypes.PhysDmg);
                incTotal += attacker.GetStat(StatTypes.BleedDmg);
                break;

            case StatusEffects.AilmentKind.Ignite:
                //incTotal += attacker.GetStat(StatTypes.FireDmg);
                incTotal += attacker.GetStat(StatTypes.IgniteDmg);
                break;

            default:
                break;
        }

        // 4)Calculate the appropriate DoT multipliers to apply to the ailment damage
        float moreTotal = 0f;
        moreTotal += attacker.GetStat(StatTypes.GenericDotMult);

        switch (effect.Ailment)
        {
            case StatusEffects.AilmentKind.Poison:
                moreTotal += attacker.GetStat(StatTypes.PoisonMult);
                break;

            case StatusEffects.AilmentKind.Bleed:
                moreTotal += attacker.GetStat(StatTypes.BleedMult);
                break;

            case StatusEffects.AilmentKind.Ignite:
                moreTotal += attacker.GetStat(StatTypes.IgniteMult);
                break;
        }

        //Calculate the total ailment damage as base damage * (1+increased/100) * (1+more/100). This is because they are whole numbers that must be converted to be used. 
        float totalAilmentDamage = baseAilmentDamage * (1f + (incTotal/100f)) * (1f + (moreTotal/100f));

        // 5)Grab the base tick duration of the ailment
        int baseTicks = Mathf.Max(1, effect.TickDuration);
        int extraTicks = 0;

        //Grab the attacker's duration stat for the ailment
        switch (effect.Ailment)
        {
            case StatusEffects.AilmentKind.Poison:
                extraTicks += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.PoisonDuration));
                break;
            case StatusEffects.AilmentKind.Bleed:
                extraTicks += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.BleedDuration));
                break;
            case StatusEffects.AilmentKind.Ignite:
                extraTicks += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.IgniteDuration));
                break;
        }

        //Set the tick count to the largest number between 1 and baseTicks + extraTicks
        tickCount = Mathf.Max(1, baseTicks + extraTicks);

        //Set damage per tick as the total damage divided by the tick count
        damagePerTick = totalAilmentDamage / tickCount;

        //Set base interval to the effects base turn interval (how many turns between the effect applying)
        int baseInterval = effect.BaseTurnInterval;
        int tickRateFlat = 0;

        //Grab the attacker's tick rate modifier and add it to tickrateflat
        switch (effect.Ailment)
        {
            case StatusEffects.AilmentKind.Poison:
                tickRateFlat += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.PoisonTickRate));
                break;
            case StatusEffects.AilmentKind.Bleed:
                tickRateFlat += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.BleedTickRate));
                break;
            case StatusEffects.AilmentKind.Ignite:
                tickRateFlat += Mathf.RoundToInt(attacker.GetRawStat(StatTypes.IgniteTickRate));
                break;
        }

        //Calculate effective interval as base interval - tickrateflat. As in, a positive interval is multiple turns between application, 0 would be every turn and a negative number would be multiple per turn.
        effectiveInterval = baseInterval - tickRateFlat;
    }

    //Used to grab the damage of the source hit to be used when applying ailments
    private static float GetSourceHitDamage(StatusEffects effect, DamageContext ctx)
    {
        if (effect == null || ctx.Hits == null || ctx.Hits.Count == 0)  //If effect is null or context has no hits, return
            return 0f;

        float total = 0f;

        switch (effect.Ailment)     //Decide which damage types to apply. Currently Physical damage affects both poison and bleed, while fire damage affects ignite.
        {
            case StatusEffects.AilmentKind.Poison:
                foreach (var hit in ctx.Hits)
                    if (hit.Element == Element.Phys)
                        total += hit.Amount;
                break;
            case StatusEffects.AilmentKind.Bleed:
                foreach (var hit in ctx.Hits)
                    if (hit.Element == Element.Phys)
                        total += hit.Amount;
                break;

            case StatusEffects.AilmentKind.Ignite:
                foreach (var hit in ctx.Hits)
                    if (hit.Element == Element.Fire)
                        total += hit.Amount;
                break;

            default:
                foreach (var hit in ctx.Hits)
                    total += hit.Amount;
                break;
        }

        return total;   //After calculating the total amount of dmg to apply to the hit based on the dmg types of the context, return that total
    }
}
