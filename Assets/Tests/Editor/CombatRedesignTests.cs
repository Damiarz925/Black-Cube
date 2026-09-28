using NUnit.Framework;
using UnityEngine;

public sealed class CombatRedesignTests
{
    [Test] public void AilmentBaselinesAndFractionalModifiersMatchContract()
    {
        foreach(var kind in new[]{StatusEffects.AilmentKind.Poison,StatusEffects.AilmentKind.Bleed,StatusEffects.AilmentKind.Ignite})
        {Assert.That(AilmentTimingRules.TickCount(AilmentTimingRules.BaseDuration(kind),AilmentTimingRules.BaseInterval(kind)),Is.EqualTo(AilmentTimingRules.BaseTicks(kind)));Assert.That(AilmentTimingRules.Duration(kind,1),Is.EqualTo(AilmentTimingRules.BaseDuration(kind)*2));Assert.That(AilmentTimingRules.Interval(kind,1),Is.EqualTo(AilmentTimingRules.BaseInterval(kind)/2));}
        Assert.That(AilmentTimingRules.Coefficient(StatusEffects.AilmentKind.Poison),Is.EqualTo(.05f));Assert.That(AilmentTimingRules.Coefficient(StatusEffects.AilmentKind.Bleed),Is.EqualTo(.20f));Assert.That(AilmentTimingRules.Coefficient(StatusEffects.AilmentKind.Ignite),Is.EqualTo(.50f));
    }
    [Test] public void LiveBleedWaitsForSecondsAndNeverAdvancesOnAttackTurns()
    {
        var source=new GameObject("source",typeof(StatsComponent));var target=new GameObject("target",typeof(StatsComponent),typeof(HealthComponent),typeof(DamageReceiver),typeof(StatusController));
        var effect=ScriptableObject.CreateInstance<StatusEffects>();effect.ConfigureRuntime("Bleed",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Bleed,ElementMask.Phys,.20f,4,5,StatusEffects.StackPolicy.StackIndependently);
        try
        {
            var health=target.GetComponent<HealthComponent>();var stats=target.GetComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,1000);health.ConfigureIsolatedStats(stats);
            var controller=target.GetComponent<StatusController>();var hit=new DamageContext(1);hit.AddDamage(Element.Phys,100);controller.ApplyAilmentFromHit(effect,hit,source.GetComponent<StatsComponent>());
            for(int i=0;i<10;i++)controller.TickStatuses();Assert.That(health.CurrentLife,Is.EqualTo(1000));
            controller.TickRealtime(.99f);Assert.That(health.CurrentLife,Is.EqualTo(1000));controller.TickRealtime(.01f);Assert.That(health.CurrentLife,Is.EqualTo(995).Within(.001));
            controller.TickRealtime(3);Assert.That(health.CurrentLife,Is.EqualTo(980).Within(.001));Assert.That(controller.HasAilment(StatusEffects.AilmentKind.Bleed),Is.False);
        }
        finally{Object.DestroyImmediate(source);Object.DestroyImmediate(target);Object.DestroyImmediate(effect);}
    }
    [Test] public void RageGenerationAndDecayAreSimultaneousWithoutGracePeriod()
    {Assert.That(WeaponMechanicProfile.AdvanceRage(0,18,2,1,0,false),Is.EqualTo(8));Assert.That(WeaponMechanicProfile.AdvanceRage(0,18,2,3,0,false),Is.EqualTo(6));Assert.That(WeaponMechanicProfile.AdvanceRage(50,0,0,1,0,false),Is.EqualTo(40));Assert.That(WeaponMechanicProfile.DecayRate(0,true),Is.EqualTo(30));Assert.That(ClassKeystoneMechanics.FullRageMultiplier(50,0,false),Is.EqualTo(1.1f));Assert.That(ClassKeystoneMechanics.FullRageMultiplier(100,0,false),Is.EqualTo(1.2f));Assert.That(ClassKeystoneMechanics.FullRageMultiplier(100,0,true),Is.EqualTo(1.56f).Within(.001));}
    [Test] public void CullChanceAndUncappedLastBreathThresholdAreIndependent()
    {Assert.That(CullingRules.Threshold(.15f,true,500,10),Is.EqualTo(1.1f).Within(.001));Assert.That(CullingRules.Qualifies(5,100,.1f,0,0),Is.False);Assert.That(CullingRules.Qualifies(5,100,.1f,.02f,.01f),Is.True);Assert.That(CullingRules.Qualifies(100,100,1.1f,1,.99f),Is.True);}
    [Test] public void RageAffixesArePrefixesAndBeltPremiumIsCentralized()
    {Assert.That(AffixPolicy.Side(StatTypes.RageGeneration),Is.EqualTo(AffixSide.Prefix));Assert.That(SystemsAffixProfile.TryTiers(StatTypes.RageGeneration,LootManager.GearType.Rings,out var ring));Assert.That(SystemsAffixProfile.TryTiers(StatTypes.RageGeneration,LootManager.GearType.Belts,out var belt));Assert.That(belt[4].maxValue,Is.EqualTo(ring[4].maxValue*1.5f));Assert.That(AffixPolicy.Side(StatTypes.CullingStrikeChance),Is.EqualTo(AffixSide.Suffix));}
}
