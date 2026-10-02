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
        E("subclass.warrior.bleed.rupture",SubclassIds.WarriorBleed,"Rupture capped Bleeds once per player attack opportunity"),
        E("subclass.warrior.multihit.conversion",SubclassIds.WarriorMultihit,"20% less damage; pre-conversion Attack Speed and Multistrike grant each other bonuses"),
        E("subclass.barbarian.big_hit.core",SubclassIds.BarbarianBigHit,"30% less Attack Speed and 60% more Physical hit damage"),E("subclass.barbarian.big_hit.full_life",SubclassIds.BarbarianBigHit,"Revenge, full-Life, repeated-hit and Rage regeneration passives"),
        E("subclass.barbarian.fire.added",SubclassIds.BarbarianFire,"Physical-to-Fire conversion through selected passives"),E("subclass.barbarian.fire.eruption",SubclassIds.BarbarianFire,"Every successful hit causes a separate Fire eruption"),
        E("subclass.ranger.poison.all_damage",SubclassIds.RangerPoison,"All hit damage types can Poison"),E("subclass.ranger.poison.core",SubclassIds.RangerPoison,"Poison speed, duration, leech and suppression passives"),
        E("subclass.ranger.projectile.core",SubclassIds.RangerProjectile,"Projectile Speed grants more damage"),E("subclass.ranger.projectile.focused",SubclassIds.RangerProjectile,"Precision, projectile count, guard and Dodge passives"),
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
    // Warrior's former chance-based Rupture and hit-count combo are retired.
    public const float BigHitAttackSpeedLess=.30f,BigHitPhysicalMore=.60f;
    public const float EruptionMagnitude=.25f;
    public const float FocusedMorePerSacrifice=.75f,CooldownIgnoreChance=.20f;public const int QueuedRepeatMaximum=3;
    public const int StormShockMaximum=3;public const float AuraFullThreshold=.10f;
    public static float FocusedMultiplier(int wouldBeCount)=>1+Mathf.Max(0,wouldBeCount-1)*FocusedMorePerSacrifice;
    public static float AuraIntensity(float damage,float enemyMaxLife)=>enemyMaxLife<=0?0:Mathf.Clamp01((damage/enemyMaxLife)/AuraFullThreshold);
    public static float FinalAuraBonus(float baseBonus,float intensity,float auraEffect)=>baseBonus*Mathf.Clamp01(intensity)*(Mathf.Max(0,1+auraEffect));
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
            case SubclassIds.MageCooldown:add(StatTypes.CooldownReduction,20);break;
            case SubclassIds.MageStorm:add(StatTypes.ShockEffect,50);break;
            case SubclassIds.ThiefAssassin:add(StatTypes.CritChance,10);add(StatTypes.CritMult,50);break;
        }
    }
}
