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
    readonly FiveAuraState auras=new();
    readonly HashSet<string> externallyGrantedEffects=new();
    SubclassCombatState previewSource;
    public void CopyPreviewFrom(SubclassCombatState source)=>previewSource=source;
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
    public void ResetForEnemy(){HitsSinceEnemySuccessfulAttack=0;EnemyHasActed=false;QueuedRepeats=0;auras.Clear();GetComponent<RevengeState>()?.Clear();}
    public float BeforePlayerHitMultiplier(bool targetFullLife,bool playerInjured,bool bossLowLife=false)
    {
        float value=1;
        if(Has(SubclassIds.WarriorMultihit))value*=SubclassBalanceProfile.ComboMultiplier(HitsSinceEnemySuccessfulAttack);
        if(Has(SubclassIds.BarbarianBigHit)){if(targetFullLife)value*=1+SubclassBalanceProfile.FullLifeMore;if(playerInjured)value*=1+SubclassBalanceProfile.InjuredMore;}
        if(Has(SubclassIds.ThiefAssassin)&&!EnemyHasActed)value*=1.5f;
        if(Has(SubclassIds.ThiefAssassin)&&bossLowLife)value*=1.5f;
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
        if(context.Hits==null||actualDamage<=0)return;float raw=0;foreach(var hit in context.Hits)raw+=hit.Amount;if(raw<=0)return;
        var typed=new float[4];foreach(var hit in context.Hits){int index=GenericPassiveMechanics.AuraIndex(hit.Element);if(index>=0&&index<4)typed[index]+=actualDamage*hit.Amount/raw;}
        for(int i=0;i<4;i++)auras.RecordTypedHit(i,typed[i],enemyMaximumLife,HasAuraAccess(i));
    }
    public void RecordMitigatedTypedDamage(DamageContext context,StatsComponent defender,float enemyMaximumLife)
    {var stats=GetComponent<StatsComponent>();for(int i=0;i<4;i++){Element element=i switch{0=>Element.Phys,1=>Element.Fire,2=>Element.Cold,_=>Element.Light};auras.RecordTypedHit(i,CombatCalculator.CalculateFinalElementDamage(context,element,stats,defender),enemyMaximumLife,HasAuraAccess(i));}}
    public bool HasAuraAccess(int index)=>GetComponent<StatsComponent>().GetStat(GenericPassiveMechanics.AuraGrant(index))>0 || (index<4&&Has(SubclassIds.PriestLight)) || (index==4&&Has(SubclassIds.PriestDark));
    public void RecordDamagingAilments(bool bleed,bool ignite,bool poison)=>auras.RecordDamagingAilments(bleed,ignite,poison,HasAuraAccess(4));
    public void TickAuras()=>auras.Tick();
    public float AuraIntensity(int index,float enemyMaximumLife)=>auras.Intensity(index);
    public float TotalAuraGlobalDamage=>0; // Auras now apply only to their matching damage type, never a global sum.
    public int ActiveAuraCount { get { int count=0;for(int i=0;i<5;i++)if(auras.Intensity(i)>0)count++;return count; } }
    public float EffectiveAuraEffect => GetComponent<StatsComponent>().GetStat(StatTypes.AuraEffect)
        +(GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.PriestAura)==true?ActiveAuraCount*PassiveKeystoneState.Value(PassiveKeystone.PriestAura):0);
    public float AuraDamageMultiplier(Element element)=>previewSource!=null?previewSource.AuraDamageMultiplier(element):auras.DamageMultiplier(element,EffectiveAuraEffect);
    public float AuraSecondary(int index,float baseValue)=>previewSource!=null?previewSource.AuraSecondary(index,baseValue):auras.Bonus(index,baseValue,EffectiveAuraEffect);
    public int AuraIgniteTicks=>auras.ExtraIgniteTicks(EffectiveAuraEffect);
    public int FinalProjectileCount(int wouldBeCount,out float damageMultiplier)
    {
        damageMultiplier=1;if(!Has(SubclassIds.RangerProjectile)||Identity.ProjectileMode!=SubclassProjectileMode.Focused)return wouldBeCount;
        damageMultiplier=SubclassBalanceProfile.FocusedMultiplier(wouldBeCount);return 1;
    }
    public bool RollCooldownBypass()=>Has(SubclassIds.MageCooldown)&&Random.value<SubclassBalanceProfile.CooldownIgnoreChance;
    public int MaximumShockInstances=>Has(SubclassIds.MageStorm)?SubclassBalanceProfile.StormShockMaximum:1;
}
