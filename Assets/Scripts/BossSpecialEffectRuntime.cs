// Stateful modular execution for boss-special affixes. One component owns all per-attack/target guards.
using System.Collections.Generic;
using UnityEngine;

public sealed class BossSpecialEffectRuntime:MonoBehaviour
{
    readonly Dictionary<Element,float> recentTypes=new();
    HealthComponent target;float previousAttackTime=-999f,reprisalUntil,bloodReturnReady;int staticEchoHits;long attackEvent,corruptedSustenanceAttack=-1,bloodReturnAttack=-1;
    public void BeginAttackEvent(){attackEvent++;}
    public void ResetForTarget(HealthComponent value){if(target==value)return;target=value;staticEchoHits=0;recentTypes.Clear();}
    public void NotifyPlayerTookDirectHit(){if(Has("reprisal"))reprisalUntil=Time.time+4f;}
    public void OnFreezeConsumed(HealthComponent health,ManaComponent mana)
    {if(!Has("frozen-recovery"))return;health?.RestoreLife(health.MaxLife*.02f,HealingSource.Generic);mana?.Restore(mana.MaxMana*.03f);}
    public DamageContext BeforeHit(DamageContext source,StatusController statuses,HealthComponent currentTarget)
    {
        ResetForTarget(currentTarget);float now=Time.time,multiplier=1f;
        if(Has("overwhelming-blow")&&now-previousAttackTime>=1.5f)multiplier*=1.4f;
        if(Has("reprisal")&&now<=reprisalUntil){multiplier*=1.3f;reprisalUntil=0;}
        if(Has("afflicted-dominion"))multiplier*=1f+.08f*Mathf.Clamp(statuses?.DistinctAilmentCount()??0,0,5);
        if(Has("corruption-mastery")){int corruption=FindFirstObjectByType<ZoneManager>()?.CorruptionPercentage??0;multiplier*=1f+Mathf.Clamp(corruption,0,100)*.001f;}
        foreach(var hit in source.Hits)if(hit.Amount>0)recentTypes[hit.Element]=now;
        var expired=new List<Element>();foreach(var pair in recentTypes)if(now-pair.Value>4f)expired.Add(pair.Key);foreach(var type in expired)recentTypes.Remove(type);
        if(Has("confluence"))multiplier*=1f+.08f*Mathf.Clamp(recentTypes.Count,0,5);
        DamageContext result=Scale(source,multiplier);
        if(Has("prismatic-core")&&(source.EventTags&(CombatEventTags.TriggeredDamage|CombatEventTags.PrismaticGain))==0)
        {float physical=0;foreach(var hit in source.Hits)if(hit.Element==Element.Phys)physical+=hit.Amount;if(physical>0){result.EventTags|=CombatEventTags.PrismaticGain;foreach(var type in new[]{Element.Fire,Element.Cold,Element.Light,Element.Void})result.AddDamage(type,physical*.08f);}}
        previousAttackTime=now;return result;
    }
    public void AfterHit(DamageContext source,float rawMagnitude,StatusController statuses,HealthComponent enemy,DamageReceiver receiver,StatsComponent attacker,StatsComponent defender,ILootRandomSource random=null)
    {
        if(enemy==null||enemy.CurrentLife<=0||receiver==null)return;random??=LootRandomSourceFactory.CreateProduction();bool secondary=(source.EventTags&(CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers))!=0;
        if(!secondary&&Has("eruption")&&random.Value()<.12f)Trigger(Element.Fire,rawMagnitude*.35f,CombatEventTags.Eruption,receiver,attacker,defender);
        if(!secondary&&Has("static-echo")&&(statuses?.CombinedShockEffect??0)>0&&++staticEchoHits>=5){staticEchoHits=0;Trigger(Element.Light,rawMagnitude*.5f,CombatEventTags.StaticEcho,receiver,attacker,defender);}
        if(!secondary&&Has("partial-rupture")&&statuses?.HasAilment(StatusEffects.AilmentKind.Bleed)==true&&random.Value()<.08f)Trigger(Element.Phys,statuses.RemainingAilmentDamage(StatusEffects.AilmentKind.Bleed)*.25f,CombatEventTags.Rupture,receiver,attacker,defender);
        if(Has("blood-return")&&statuses?.HasAilment(StatusEffects.AilmentKind.Bleed)==true&&Time.time>=bloodReturnReady&&bloodReturnAttack!=attackEvent){bloodReturnAttack=attackEvent;var health=GetComponent<HealthComponent>();health?.RestoreLife((health.MaxLife-health.CurrentLife)*.02f,HealingSource.LifeOnHit);bloodReturnReady=Time.time+1f;}
        if(Has("corrupted-sustenance")&&statuses?.HasAilment(StatusEffects.AilmentKind.Poison)==true&&corruptedSustenanceAttack!=attackEvent){corruptedSustenanceAttack=attackEvent;var health=GetComponent<HealthComponent>();health?.RestoreLife(health.MaxLife*.01f,HealingSource.LifeOnHit);var mana=GetComponent<ManaComponent>();mana?.Restore(mana.MaxMana*.02f);}
        if(Has("freezing-edge")&&Sum(source,Element.Cold)>0&&(statuses?.CurrentChillSlow??0)>=.20f&&random.Value()<.10f)statuses.ApplyFreeze(statuses.CurrentChillSlow);
    }
    void Trigger(Element element,float raw,CombatEventTags tag,DamageReceiver receiver,StatsComponent attacker,StatsComponent defender)
    {if(raw<=0)return;var ctx=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.SpecialAffixProc|CombatEventTags.NoSecondaryTriggers|tag};ctx.AddDamage(element,raw);float final=CombatCalculator.CalculateFinalDamage(ctx,attacker,defender);if(final>0)receiver.TakeDamage(final,ctx);}
    bool Has(string suffix)=>PlayerHas(suffix);
    public static bool PlayerHas(string suffix)
    {
        if(EquipmentManager.Instance==null)return false;foreach(var pair in EquipmentManager.Instance.EquippedItems)if(pair.Value!=null)foreach(var mod in pair.Value.rolledMods)
            if(mod?.isBossSpecial==true&&mod.specialModifierId!=null&&mod.specialModifierId.EndsWith("."+suffix,System.StringComparison.Ordinal))return true;return false;
    }
    static float Sum(DamageContext ctx,Element type){float n=0;if(ctx.Hits!=null)foreach(var h in ctx.Hits)if(h.Element==type)n+=h.Amount;return n;}
    static DamageContext Scale(DamageContext source,float multiplier){if(Mathf.Approximately(multiplier,1))return source;var result=new DamageContext(source.Hits?.Count??0){IsCrit=source.IsCrit,CritMultiplier=source.CritMultiplier,Scopes=source.Scopes,IsPrecision=source.IsPrecision,PrecisionMultiplier=source.PrecisionMultiplier,WeaponMechanicsApplied=source.WeaponMechanicsApplied,EventTags=source.EventTags};if(source.Hits!=null)foreach(var hit in source.Hits)result.AddDamage(hit.Element,hit.Amount*multiplier);return result;}
}
