using System.Collections.Generic;
using UnityEngine;

// Encounter-independent transient effects. Nothing here is restored mid-combat.
[RequireComponent(typeof(StatsComponent))]
public sealed class UniqueCombatRuntime:MonoBehaviour
{
    StatsComponent stats;StatusController enemyStatus;HealthComponent enemy;
    float poolUntil,poolStrength,regenerationUntil,regenerationRate,voidClock,combatClock;
    readonly List<StatusInstance> preserved=new();
    void Awake()=>stats=GetComponent<StatsComponent>();
    // Statuses can outlive the enemy that applied them. Unity's destroyed-object
    // sentinel is not respected by C#'s ?. operator, so never dereference a
    // source StatsComponent without the Unity lifetime check.
    public static UniqueCombatRuntime For(StatsComponent actor)
        => actor != null && actor.GetComponent<PlayerController>() != null
            ? (actor.GetComponent<UniqueCombatRuntime>() ?? actor.gameObject.AddComponent<UniqueCombatRuntime>())
            : null;
    public void BindEnemy(HealthComponent target)
    {
        if(enemyStatus!=null)enemyStatus.AilmentExpired-=Expired;
        enemy=target;enemyStatus=target?.GetComponent<StatusController>();
        if(enemyStatus==null)return;
        enemyStatus.AilmentExpired+=Expired;
        foreach(var instance in preserved)enemyStatus.RestorePreservedPoison(instance);
        preserved.Clear();
    }
    public void EnemyDied(StatusController controller)
    {
        preserved.Clear();int count=Mathf.FloorToInt(UniqueCatalog.Power(stats,UniquePower.PreservePoison));
        if(count>0&&controller!=null)preserved.AddRange(controller.HighestRemainingPoison(count));
        if(enemyStatus!=null)enemyStatus.AilmentExpired-=Expired;
        enemy=null;enemyStatus=null;
    }
    void Expired(StatusInstance instance)
    {
        if(instance.effect?.Ailment!=StatusEffects.AilmentKind.Ignite||instance.sourceStats!=stats)return;
        float value=UniqueCatalog.Power(stats,UniquePower.LavaFireTaken);if(value<=0)return;
        poolStrength=Mathf.Max(poolUntil>combatClock?poolStrength:0,value);poolUntil=combatClock+2;
    }
    public float FireTakenMultiplier=>combatClock<poolUntil?1+poolStrength:1;
    public void RecordVoidDamage(float actualDamage)
    {
        float conversion=UniqueCatalog.Power(stats,UniquePower.VoidToRegeneration);if(conversion<=0||actualDamage<=0)return;
        regenerationRate=Mathf.Max(regenerationUntil>combatClock?regenerationRate:0,actualDamage*conversion);
        regenerationUntil=combatClock+2;
    }
    public void Rupture(HealthComponent target,StatusController status)
    {
        if(UniqueCatalog.Power(stats,UniquePower.Rupture)<=0||target==null||status==null)return;
        float damage=status.ConsumeRemainingAilmentDamage(StatusEffects.AilmentKind.Bleed);if(damage<=0)return;
        var context=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.Rupture|CombatEventTags.NoSecondaryTriggers};context.AddDamage(Element.Phys,damage);
        target.GetComponent<DamageReceiver>()?.TakeDamage(damage,context,attacker:stats);
    }
    public void EchoSelfHit(float rawSpellDamage)
    {
        float coefficient=UniqueCatalog.Power(stats,UniquePower.EchoSelfDamage);if(coefficient<=0)return;
        var context=new DamageContext(1){IncomingSelfHit=true,EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers};context.AddDamage(Element.Light,rawSpellDamage*coefficient);
        var status=GetComponent<StatusController>();status?.AddShockInstance(ShockRules.Effect(stats.GetStat(StatTypes.ShockEffect),1+(RelicInventory.Instance?.MaximumShockEffectIncrease??0),stats.GetStat(StatTypes.ReducedShockEffect)),ShockRules.Duration(stats.GetStat(StatTypes.ShockDuration)));
        float final=CombatCalculator.CalculateFinalDamage(context,null,stats);GetComponent<DamageReceiver>()?.TakeDamage(final,context);
    }
    void Update()
    {
        if(Time.deltaTime<=0||SkillTreeUI.PausesGameplay||PlayerSkillMenuUI.IsOpen)return;
        combatClock+=Time.deltaTime;
        var health=GetComponent<HealthComponent>();if(health==null||health.CurrentLife<=0)return;
        float temporary=combatClock<regenerationUntil?regenerationRate:0;
        float allowed=GetComponent<PassiveKeystoneState>()?.LifeRegenerationMultiplier??1;
        if(temporary>0&&allowed>0)health.RestoreLife(temporary*allowed*Time.deltaTime,HealingSource.Regeneration);
        float duplicate=UniqueCatalog.Power(stats,UniquePower.RegenerationToVoid);if(duplicate<=0||enemy==null||enemy.CurrentLife<=0){voidClock=0;return;}
        voidClock+=Time.deltaTime;if(voidClock<.25f)return;
        float elapsed=voidClock;voidClock=0;
        float rate=(health.MaxLife*stats.GetStat(StatTypes.LifeRegeneration)+temporary)*allowed;
        var context=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers};
        context.AddDamage(Element.Void,rate*duplicate*elapsed*(1+stats.GetStat(StatTypes.VoidDmg))*(1+stats.GetStat(StatTypes.VoidMult)));
        float damage=CombatCalculator.CalculateFinalDamage(context,stats,enemy.GetComponent<StatsComponent>());enemy.GetComponent<DamageReceiver>()?.TakeDamage(damage,context,attacker:stats);
    }
    public void ResetTransient()
    {
        if(enemyStatus!=null)enemyStatus.AilmentExpired-=Expired;
        enemy=null;enemyStatus=null;preserved.Clear();poolUntil=poolStrength=regenerationUntil=regenerationRate=voidClock=combatClock=0;
    }
    void OnDestroy(){if(enemyStatus!=null)enemyStatus.AilmentExpired-=Expired;}
}
