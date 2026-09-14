// Developer map: Converts eligible pre-defense hit components into per-tick strength, tick count and global-turn interval. Generic hit scaling is already in the source hit and must not be applied twice.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
        out int effectiveInterval,
        float magnitudeOverride = -1f)
    {
        damagePerTick = 0f;         //Create variables for damage per tick, tick count, and interval
        tickCount = 0;
        effectiveInterval = 1;

        if (effect == null || attacker == null)         //If there is no effect or attacker, return
            return;

        float sourceHitDamage = GetSourceHitDamage(effect, ctx)
                                * CombatCalculator.ScopedDamageMultiplier(ctx.Scopes, attacker);
        if (sourceHitDamage <= 0f)      //If it's 0, return.
            return;

        //Calculate the base ailment damage by multiplying source hit by the effect's set magnitude
        float baseAilmentDamage = sourceHitDamage * (magnitudeOverride >= 0f ? magnitudeOverride : effect.Magnitude);

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
        float moreFactor = 1f + attacker.GetStat(StatTypes.GenericDotMult);

        switch (effect.Ailment)
        {
            case StatusEffects.AilmentKind.Poison:
                moreFactor *= 1f + attacker.GetStat(StatTypes.PoisonMult);
                break;

            case StatusEffects.AilmentKind.Bleed:
                moreFactor *= 1f + attacker.GetStat(StatTypes.BleedMult);
                break;

            case StatusEffects.AilmentKind.Ignite:
                moreFactor *= 1f + attacker.GetStat(StatTypes.IgniteMult);
                break;
        }

        // Each more stat compounds its own rolls. Multiply DOT and matching ailment
        // factors only: hit increased/more scaling is already in the source hit.
        float totalAilmentDamage = baseAilmentDamage * (1f + incTotal) * moreFactor;
        var keystones = attacker.GetComponent<PassiveKeystoneState>();
        if (keystones != null) totalAilmentDamage *= keystones.AilmentDamageMultiplier(effect.Ailment);

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

        // Preserve the configured base-duration coefficient. Extra duration adds
        // equally strong ticks; it must not dilute or multiply individual ticks.
        damagePerTick = totalAilmentDamage / baseTicks;

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
    public static float GetSourceHitDamage(StatusEffects effect, DamageContext ctx)
    {
        if (effect == null || ctx.Hits == null || ctx.Hits.Count == 0)  //If effect is null or context has no hits, return
            return 0f;

        float total = 0f;

        // One eligibility rule shared by chance rolls and ailment strength calculation.
        ElementMask eligible = effect._StatusType switch
        {
            StatusEffects.StatusType.Chill => ElementMask.Cold,
            StatusEffects.StatusType.Shock => ElementMask.Light,
            _ => effect.Ailment switch
            {
                StatusEffects.AilmentKind.Poison => ElementMask.Phys | ElementMask.Poison,
                StatusEffects.AilmentKind.Bleed => ElementMask.Phys,
                StatusEffects.AilmentKind.Ignite => ElementMask.Fire,
                _ => effect.Elements
            }
        };
        foreach (var hit in ctx.Hits)
        {
            if (hit.Amount > 0f && (eligible & (ElementMask)(1 << (int)hit.Element)) != 0)
                total += hit.Amount;
        }

        return total;   //After calculating the total amount of dmg to apply to the hit based on the dmg types of the context, return that total
    }
}
