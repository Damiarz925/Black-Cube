// Encounter-scoped Step 18 subclass authority. None of this state is persisted.
using UnityEngine;
using System.Collections.Generic;

public enum HealingSource{Generic,Regeneration,WeaponSkill,LifeOnHit,LifeOnKill,SubclassDamage}

[RequireComponent(typeof(PlayerController),typeof(StatsComponent))]
public sealed class SubclassCombatState:MonoBehaviour
{
    public int HitsSinceEnemySuccessfulAttack{get;private set;}
    public bool EnemyHasActed{get;private set;}
    public int QueuedRepeats{get;private set;}
    readonly float[] auraDamage=new float[4];
    readonly HashSet<string> externallyGrantedEffects=new();
    PlayerIdentityState Identity=>GameManager.Instance!=null?GameManager.Instance.GetComponent<PlayerIdentityState>():null;
    public string SelectedSubclassId=>Identity?.SelectedSubclassId??string.Empty;
    public bool Has(string id)=>SelectedSubclassId==id;
    public bool HasEffect(string effectId)
    {
        if(string.IsNullOrEmpty(effectId))return false;
        if(externallyGrantedEffects.Contains(effectId))return true;
        return SubclassEffectCatalog.TryGet(effectId,out var effect)&&effect.SubclassId==SelectedSubclassId;
    }
    public bool GrantExternalEffect(string effectId){if(!SubclassEffectCatalog.TryGet(effectId,out _))return false;return externallyGrantedEffects.Add(effectId);}
    public bool RevokeExternalEffect(string effectId)=>externallyGrantedEffects.Remove(effectId);
    public void ResetForEnemy(){HitsSinceEnemySuccessfulAttack=0;EnemyHasActed=false;QueuedRepeats=0;for(int i=0;i<auraDamage.Length;i++)auraDamage[i]=0;}
    public float BeforePlayerHitMultiplier(bool targetFullLife,bool playerInjured,bool bossLowLife=false)
    {
        float value=1;
        if(Has(SubclassIds.WarriorMultihit))value*=SubclassBalanceProfile.ComboMultiplier(HitsSinceEnemySuccessfulAttack);
        if(Has(SubclassIds.BarbarianBigHit)){if(targetFullLife)value*=1+SubclassBalanceProfile.FullLifeMore;if(playerInjured)value*=1+SubclassBalanceProfile.InjuredMore;}
        if(Has(SubclassIds.ThiefAssassin)&&!EnemyHasActed)value*=1.5f;
        if(Has(SubclassIds.ThiefAssassin)&&bossLowLife)value*=1.5f;
        if(Has(SubclassIds.PriestLight))value*=1+TotalAuraGlobalDamage;
        return value;
    }
    public void PlayerHit(){HitsSinceEnemySuccessfulAttack++;}
    public void EnemySuccessfulAttack(){EnemyHasActed=true;HitsSinceEnemySuccessfulAttack=0;}
    public bool TryQueueRepeat()
    {
        if(!Has(SubclassIds.MageCooldown)||QueuedRepeats>=SubclassBalanceProfile.QueuedRepeatMaximum||Random.value>=SubclassBalanceProfile.CooldownIgnoreChance){QueuedRepeats=0;return false;}
        QueuedRepeats++;return true;
    }
    public void ResetQueuedRepeats()=>QueuedRepeats=0;
    public void RecordTypedDamage(DamageContext context,float actualDamage,float enemyMaximumLife)
    {
        if(!Has(SubclassIds.PriestLight)||context.Hits==null||actualDamage<=0)return;float raw=0;foreach(var hit in context.Hits)raw+=hit.Amount;if(raw<=0)return;
        foreach(var hit in context.Hits){int index=hit.Element switch{Element.Phys=>0,Element.Fire=>1,Element.Cold=>2,Element.Light=>3,_=>-1};if(index>=0)auraDamage[index]+=actualDamage*hit.Amount/raw;}
    }
    public float AuraIntensity(int index,float enemyMaximumLife)=>index>=0&&index<4?SubclassBalanceProfile.AuraIntensity(auraDamage[index],enemyMaximumLife):0;
    public float TotalAuraGlobalDamage{get{var enemy=BattleManager.Instance?.CurrentEnemyAI?.GetComponent<HealthComponent>();float maximum=enemy!=null?enemy.MaxLife:0;float effect=GetComponent<StatsComponent>().GetStat(StatTypes.AuraEffect);float sum=0;for(int i=0;i<4;i++)sum+=SubclassBalanceProfile.FinalAuraBonus(.20f,AuraIntensity(i,maximum),effect);return sum;}}
    public float AuraSecondary(int index,float baseValue){var enemy=BattleManager.Instance?.CurrentEnemyAI?.GetComponent<HealthComponent>();return SubclassBalanceProfile.FinalAuraBonus(baseValue,AuraIntensity(index,enemy!=null?enemy.MaxLife:0),GetComponent<StatsComponent>().GetStat(StatTypes.AuraEffect));}
    public int FinalProjectileCount(int wouldBeCount,out float damageMultiplier)
    {
        damageMultiplier=1;if(!Has(SubclassIds.RangerProjectile)||Identity.ProjectileMode!=SubclassProjectileMode.Focused)return wouldBeCount;
        damageMultiplier=SubclassBalanceProfile.FocusedMultiplier(wouldBeCount);return 1;
    }
    public bool RollCooldownBypass()=>Has(SubclassIds.MageCooldown)&&Random.value<SubclassBalanceProfile.CooldownIgnoreChance;
    public int MaximumShockInstances=>Has(SubclassIds.MageStorm)?SubclassBalanceProfile.StormShockMaximum:1;
}
