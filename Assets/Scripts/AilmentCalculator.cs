// Developer map: Converts eligible pre-defense hit components into per-tick strength, tick count and global-turn interval. Generic hit scaling is already in the source hit and must not be applied twice.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

//This class is used to calculate ailment damage so that it can be dealt to a target
public static class AilmentCalculator
{
    public const float MinimumPoisonTickInterval=.10f;
    public static float PoisonTickInterval(float baseInterval,float poisonSpeed)=>Mathf.Max(MinimumPoisonTickInterval,Mathf.Max(MinimumPoisonTickInterval,baseInterval)/(1+Mathf.Max(0,poisonSpeed)));
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

        float sourceHitDamage = (effect.Ailment == StatusEffects.AilmentKind.Poison
                ? GetPoisonVoidScaledSource(effect,ctx,attacker)
                : GetSourceHitDamage(effect, ctx,attacker))
                                * CombatCalculator.ScopedDamageMultiplier(ctx.Scopes, attacker);
        if (sourceHitDamage <= 0f)      //If it's 0, return.
            return;

        //Calculate the base ailment damage by multiplying source hit by the effect's set magnitude
        float baseAilmentDamage = sourceHitDamage * (magnitudeOverride >= 0f ? magnitudeOverride : (AilmentTimingRules.IsDamaging(effect.Ailment)?AilmentTimingRules.Coefficient(effect.Ailment):effect.Magnitude));

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
        float moreFactor = (1f + attacker.GetStat(StatTypes.GenericDotMult))
            * (1f + attacker.GetStat(StatTypes.WarriorDotMultiplier))
            * (1f + DerivedStatCalculator.DotMoreDamage(attacker));

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
        if(attacker.GetComponent<PlayerController>()!=null)
        {
            if(effect.Ailment==StatusEffects.AilmentKind.Poison&&BossSpecialEffectRuntime.PlayerHas("corrosive-void"))
            {bool hasVoid=false;foreach(var hit in ctx.Hits)if((hit.Element==Element.Void||hit.Element==Element.Poison)&&hit.Amount>0){hasVoid=true;break;}if(hasVoid)moreFactor*=1.30f;}
            if(effect.Ailment==StatusEffects.AilmentKind.Ignite&&BossSpecialEffectRuntime.PlayerHas("rapid-burn"))moreFactor*=1.25f;
        }

        // Each more stat compounds its own rolls. Multiply DOT and matching ailment
        // factors only: hit increased/more scaling is already in the source hit.
        float totalAilmentDamage = baseAilmentDamage * (1f + incTotal) * moreFactor * UniqueCatalog.AilmentMultiplier(attacker);
        var keystones = attacker.GetComponent<PassiveKeystoneState>();
        if (keystones != null) totalAilmentDamage *= keystones.AilmentDamageMultiplier(effect.Ailment);

        if(AilmentTimingRules.IsDamaging(effect.Ailment))
        {
            AilmentTimingRules.Timing(effect.Ailment,attacker,out float duration,out float interval);
            tickCount=AilmentTimingRules.TickCount(duration,interval);
            damagePerTick=totalAilmentDamage/AilmentTimingRules.BaseTicks(effect.Ailment);
            effectiveInterval=1; // Legacy signature retained; real-time scheduling owns fractional seconds.
        }
        else {tickCount=Mathf.Max(1,effect.TickDuration);damagePerTick=totalAilmentDamage/tickCount;effectiveInterval=effect.BaseTurnInterval;}

    }

    /// <summary>
    /// Poison is an ailment whose payload is Void. Non-Void source components gain
    /// Void increased/more scaling here; a Void source already received those
    /// modifiers while its hit was built, so it is not scaled a second time.
    /// </summary>
    public static float GetPoisonVoidScaledSource(StatusEffects effect,DamageContext ctx, StatsComponent attacker)
    {
        if (ctx.Hits == null || attacker == null) return 0f;
        ElementMask eligible=AilmentEligibilityResolver.ResolveMask(effect,ctx,attacker);
        float voidFactor = (1f + attacker.GetStat(StatTypes.VoidDmg)
                + DerivedStatCalculator.ElementIncreasedDamage(attacker, Element.Void))
            * (1f + attacker.GetStat(StatTypes.VoidMult));
        float total = 0f;
        foreach (ElementalHit hit in ctx.Hits)
        {
            Element element=hit.Element==Element.Poison?Element.Void:hit.Element;
            if (hit.Amount <= 0f || (eligible&(ElementMask)(1<<(int)element))==0) continue;
            total += element == Element.Void ? hit.Amount : hit.Amount * voidFactor;
        }
        return total;
    }

    //Used to grab the damage of the source hit to be used when applying ailments
    public static float GetSourceHitDamage(StatusEffects effect, DamageContext ctx)
    {
        if (effect == null || ctx.Hits == null || ctx.Hits.Count == 0)  //If effect is null or context has no hits, return
            return 0f;

        return AilmentEligibilityResolver.EligibleRawDamage(effect,ctx,null);
    }
    public static float GetSourceHitDamage(StatusEffects effect,DamageContext ctx,StatsComponent attacker)
    {
        return AilmentEligibilityResolver.EligibleRawDamage(effect,ctx,attacker);
    }
}
