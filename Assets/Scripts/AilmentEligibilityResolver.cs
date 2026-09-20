// Central authority for which portions of a hit may apply each ailment.
using UnityEngine;

public static class SubclassEffectIds
{
    public const string RangerAllDamagePoison="subclass.ranger.poison.all_damage";
    public const string MageAllDamageShock="subclass.mage.storm.core";
    public const string DarkPriestVoidAilments="subclass.priest.dark.void_ailments";
}

public static class AilmentEligibilityResolver
{
    public static ElementMask DefaultMask(StatusEffects effect)
    {
        if(effect==null)return ElementMask.None;
        if(effect._StatusType==StatusEffects.StatusType.Shock)return ElementMask.Light;
        if(effect._StatusType==StatusEffects.StatusType.Chill)return ElementMask.Cold;
        return effect.Ailment switch
        {
            StatusEffects.AilmentKind.Poison=>ElementMask.Phys|ElementMask.Void,
            StatusEffects.AilmentKind.Bleed=>ElementMask.Phys,
            StatusEffects.AilmentKind.Ignite=>ElementMask.Fire,
            _=>effect.Elements
        };
    }

    public static ElementMask ResolveMask(StatusEffects effect,StatsComponent attacker)
    {
        ElementMask mask=DefaultMask(effect);
        SubclassCombatState state=attacker!=null?attacker.GetComponent<SubclassCombatState>():null;
        if(state==null)return mask;
        if(effect!=null&&effect.Ailment==StatusEffects.AilmentKind.Poison&&state.HasEffect(SubclassEffectIds.RangerAllDamagePoison))
            return ElementMask.Phys|ElementMask.Fire|ElementMask.Cold|ElementMask.Light|ElementMask.Void;
        if(effect!=null&&effect._StatusType==StatusEffects.StatusType.Shock&&state.HasEffect(SubclassEffectIds.MageAllDamageShock))
            return ElementMask.Phys|ElementMask.Fire|ElementMask.Cold|ElementMask.Light|ElementMask.Void;
        if(state.HasEffect(SubclassEffectIds.DarkPriestVoidAilments))mask|=ElementMask.Void;
        return mask;
    }

    public static ElementMask ResolveMask(StatusEffects effect,DamageContext context,StatsComponent attacker)
        =>(context.EventTags&CombatEventTags.FullAilmentBasis)!=0
            ?ElementMask.Phys|ElementMask.Fire|ElementMask.Cold|ElementMask.Light|ElementMask.Void
            :ResolveMask(effect,attacker);

    public static float EligibleRawDamage(StatusEffects effect,DamageContext context,StatsComponent attacker)
    {
        if(effect==null||context.Hits==null)return 0f;
        ElementMask mask=ResolveMask(effect,context,attacker);float total=0f;
        foreach(ElementalHit hit in context.Hits)
            if(hit.Amount>0f&&(mask&Mask(hit.Element))!=0)total+=hit.Amount;
        return total;
    }

    public static DamageContext Filter(StatusEffects effect,DamageContext source,StatsComponent attacker)
    {
        var result=new DamageContext(source.Hits?.Count??0){IsCrit=source.IsCrit,CritMultiplier=source.CritMultiplier,Scopes=source.Scopes,IsPrecision=source.IsPrecision,PrecisionMultiplier=source.PrecisionMultiplier,WeaponMechanicsApplied=source.WeaponMechanicsApplied,EventTags=source.EventTags};
        if(source.Hits==null)return result;ElementMask mask=ResolveMask(effect,source,attacker);
        foreach(ElementalHit hit in source.Hits)if(hit.Amount>0f&&(mask&Mask(hit.Element))!=0)result.AddDamage(hit.Element,hit.Amount);
        return result;
    }

    static ElementMask Mask(Element element)=>element==Element.Poison?ElementMask.Void:(ElementMask)(1<<(int)element);
}
