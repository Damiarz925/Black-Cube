// Step 18 production subclass data and reusable effect/transform formulas.
// Effect IDs are source-agnostic so a future item can grant one effect without
// pretending the character selected the owning subclass.
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SubclassEffectDefinition
{
    public readonly string Id,SubclassId,Description;
    public SubclassEffectDefinition(string id,string subclassId,string description){Id=id;SubclassId=subclassId;Description=description;}
}

public static class SubclassEffectCatalog
{
    static readonly SubclassEffectDefinition[] all={
        E("subclass.warrior.bleed.core",SubclassIds.WarriorBleed,"+20pp Bleed Chance and +40% Bleed damage"),E("subclass.warrior.bleed.rupture",SubclassIds.WarriorBleed,"15% Bleed rupture"),
        E("subclass.warrior.multihit.core",SubclassIds.WarriorMultihit,"+10% Attack Speed and +10pp Hit Twice"),E("subclass.warrior.multihit.combo",SubclassIds.WarriorMultihit,"5% more per prior hit, maximum 10"),
        E("subclass.barbarian.big_hit.core",SubclassIds.BarbarianBigHit,"25% less Attack Speed and 60% more Physical hit damage"),E("subclass.barbarian.big_hit.full_life",SubclassIds.BarbarianBigHit,"75% more against full-Life enemies; 35% more while injured"),
        E("subclass.barbarian.fire.added",SubclassIds.BarbarianFire,"40% base Physical gained as Fire"),E("subclass.barbarian.fire.eruption",SubclassIds.BarbarianFire,"20% chance for a 75% Fire eruption"),
        E("subclass.ranger.poison.all_damage",SubclassIds.RangerPoison,"All damage can Poison"),E("subclass.ranger.poison.core",SubclassIds.RangerPoison,"Poison chance, damage, duration and speed"),
        E("subclass.ranger.projectile.core",SubclassIds.RangerProjectile,"Additional projectile, speed and Precision"),E("subclass.ranger.projectile.focused",SubclassIds.RangerProjectile,"Collapse projectiles for 75% more per sacrifice"),
        E("subclass.mage.cooldown.core",SubclassIds.MageCooldown,"20% Cooldown Reduction"),E("subclass.mage.cooldown.ignore",SubclassIds.MageCooldown,"20% cooldown bypass / queued repeat"),
        E("subclass.mage.storm.core",SubclassIds.MageStorm,"All damage can Shock and +50% Shock Effect"),E("subclass.mage.storm.multi_shock",SubclassIds.MageStorm,"Three multiplicatively combined Shocks"),
        E("subclass.priest.dark.heal_to_harm",SubclassIds.PriestDark,"Non-regeneration healing becomes triggerless Void damage"),E("subclass.priest.dark.void_ailments",SubclassIds.PriestDark,"Void can apply all ailments"),E("subclass.priest.dark.corruption",SubclassIds.PriestDark,"0.20% more Void per corruption point"),
        E("subclass.priest.light.no_void",SubclassIds.PriestLight,"Cannot deal Void damage"),E("subclass.priest.light.damage_heal",SubclassIds.PriestLight,"Direct damage heals 10%"),E("subclass.priest.light.auras",SubclassIds.PriestLight,"Four damage-built auras"),
        E("subclass.thief.assassin.first_strike",SubclassIds.ThiefAssassin,"First attack event and opener damage"),E("subclass.thief.assassin.execution",SubclassIds.ThiefAssassin,"10% non-boss execution"),E("subclass.thief.assassin.deadly_patience",SubclassIds.ThiefAssassin,"Bonus Attack Speed becomes Critical Multiplier"),
        E("subclass.thief.ailment_crit.scaling",SubclassIds.ThiefAilmentCrit,"Excess Crit Multiplier scales damaging ailments"),E("subclass.thief.ailment_crit.critical_ailment",SubclassIds.ThiefAilmentCrit,"Poison, Bleed and Ignite can crit once at creation")};
    static SubclassEffectDefinition E(string id,string sub,string text)=>new(id,sub,text);
    public static IReadOnlyList<SubclassEffectDefinition> All=>all;
    public static bool TryGet(string id,out SubclassEffectDefinition value){foreach(var x in all)if(x.Id==id){value=x;return true;}value=null;return false;}
    public static IEnumerable<SubclassEffectDefinition> ForSubclass(string id){foreach(var x in all)if(x.SubclassId==id)yield return x;}
}

public static class SubclassBalanceProfile
{
    public const float RuptureChance=.15f,ComboMorePerPriorHit=.05f;public const int ComboMaximum=10;
    public const float BigHitAttackSpeedLess=.25f,BigHitPhysicalMore=.60f,FullLifeMore=.75f,InjuredMore=.35f;
    public const float AddedFireFromPhysical=.40f,EruptionChance=.20f,EruptionMagnitude=.75f;
    public const float FocusedMorePerSacrifice=.75f,CooldownIgnoreChance=.20f;public const int QueuedRepeatMaximum=3;
    public const int StormShockMaximum=3;public const float AuraFullThreshold=.10f;
    public static float ComboMultiplier(int priorHits)=>1+Mathf.Min(ComboMaximum,Mathf.Max(0,priorHits))*ComboMorePerPriorHit;
    public static float FocusedMultiplier(int wouldBeCount)=>1+Mathf.Max(0,wouldBeCount-1)*FocusedMorePerSacrifice;
    public static float AuraIntensity(float damage,float enemyMaxLife)=>enemyMaxLife<=0?0:Mathf.Clamp01((damage/enemyMaxLife)/AuraFullThreshold);
    public static float FinalAuraBonus(float baseBonus,float intensity,float auraEffect)=>baseBonus*Mathf.Clamp01(intensity)*(1+Mathf.Max(0,auraEffect));
    public static float AilmentExtraMore(float playerCritMultiplier)=>1+Mathf.Max(0,playerCritMultiplier-CombatCalculator.BaseCriticalMultiplier)*.5f;
    public static float CriticalAilmentMultiplier(float playerCritMultiplier)=>1+Mathf.Max(0,playerCritMultiplier-1)*.5f;
    [Obsolete("Use the explicit extra-scaling or critical-ailment formula.")]
    public static float AilmentCritMore(float critMultiplier)=>CriticalAilmentMultiplier(critMultiplier);
}

/// <summary>Authoritative static stat package granted by selecting a subclass.</summary>
public static class SubclassStatPackage
{
    public static void Apply(string subclassId,Action<StatTypes,float> add)
    {
        if(add==null)return;
        switch(subclassId)
        {
            case SubclassIds.WarriorBleed:add(StatTypes.BleedChance,20);add(StatTypes.BleedDmg,40);break;
            case SubclassIds.WarriorMultihit:add(StatTypes.AttackSpeed,10);add(StatTypes.ChanceToHitTwice,10);break;
            case SubclassIds.BarbarianBigHit:add(StatTypes.AttackSpeed,-25);add(StatTypes.PhysMult,60);break;
            case SubclassIds.RangerPoison:add(StatTypes.PoisonChance,20);add(StatTypes.PoisonDmg,40);add(StatTypes.PoisonDuration,25);add(StatTypes.PoisonSpeed,25);break;
            case SubclassIds.RangerProjectile:add(StatTypes.ProjectileAmount,1);add(StatTypes.ProjectileSpeed,20);add(StatTypes.ProjectilePrecisionChance,15);break;
            case SubclassIds.MageCooldown:add(StatTypes.CooldownReduction,20);break;
            case SubclassIds.MageStorm:add(StatTypes.ShockEffect,50);break;
            case SubclassIds.ThiefAssassin:add(StatTypes.CritChance,10);add(StatTypes.CritMult,50);break;
        }
    }
}
