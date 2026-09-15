using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AilmentFoundationTests
{
    readonly List<Object> cleanup=new();
    [TearDown]
    public void TearDown(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)Object.DestroyImmediate(cleanup[i]);cleanup.Clear();}

    [Test]
    public void AuthoredAilmentAssetsMatchRuntimeCadenceAndIndependentCaps()
    {
        var poison=AssetDatabase.LoadAssetAtPath<PoisonStatusEffect>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset");
        var bleed=AssetDatabase.LoadAssetAtPath<BleedStatusEffect>("Assets/Prefabs/Scriptable Objects/BleedStatus.asset");
        var ignite=AssetDatabase.LoadAssetAtPath<IgniteStatusEffect>("Assets/Prefabs/Scriptable Objects/IgniteStatus.asset");
        Assert.That(poison,Is.Not.Null);Assert.That(bleed,Is.Not.Null);Assert.That(ignite,Is.Not.Null);
        Assert.That((poison.TickDuration,poison.BaseTurnInterval,poison.MaxStacks,poison._StackPolicy),
            Is.EqualTo((4,2,0,StatusEffects.StackPolicy.StackIndependently)));
        Assert.That((bleed.TickDuration,bleed.BaseTurnInterval,bleed.MaxStacks,bleed._StackPolicy),
            Is.EqualTo((5,2,5,StatusEffects.StackPolicy.StackIndependently)));
        Assert.That((ignite.TickDuration,ignite.BaseTurnInterval,ignite.MaxStacks,ignite._StackPolicy),
            Is.EqualTo((2,2,1,StatusEffects.StackPolicy.ReplaceIfStronger)));
    }

    [Test]
    public void IgniteTicksOnSecondAndFourthAfflictedTurnsAndUsesRemainingTotalReplacement()
    {
        var (controller,source)=Actor();var effect=Effect(StatusEffects.AilmentKind.Ignite);
        controller.ApplyStatus(effect,1,100,2,source,2);
        controller.TickStatuses(false);controller.TickStatuses(false);
        Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(2),"Attacker turns cannot age Ignite.");
        controller.TickStatuses(true);Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(2));
        controller.TickStatuses(true);Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(1));
        controller.ApplyStatus(effect,1,70,2,source,2);
        Assert.That(Stacks(controller,effect)[0].damagePerTick,Is.EqualTo(70),"A full-duration 140 Ignite replaces an aged 100 remaining-total Ignite.");
        controller.ApplyStatus(effect,1,50,2,source,2);
        Assert.That(Stacks(controller,effect)[0].damagePerTick,Is.EqualTo(70),"A weaker incoming total cannot replace it.");
        for(int turn=0;turn<4;turn++)controller.TickStatuses(true);
        Assert.That(Stacks(controller,effect).Count,Is.Zero,"Four afflicted turns give two base ticks and expiration.");
    }

    [Test]
    public void BleedStacksIndependentlyCapsAtFiveAndSixthUsesAgedRemainingTotal()
    {
        var (controller,source)=Actor();var effect=Effect(StatusEffects.AilmentKind.Bleed);
        controller.ApplyStatus(effect,5,100,5,source,2);
        Assert.That(Stacks(controller,effect).Count,Is.EqualTo(5));
        for(int turn=0;turn<8;turn++)controller.TickStatuses(true);
        Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(1));
        controller.ApplyStatus(effect,1,60,5,source,2);
        Assert.That(Stacks(controller,effect).Count,Is.EqualTo(5));
        Assert.That(Stacks(controller,effect).Exists(x=>Mathf.Approximately(x.damagePerTick,60)),Is.True,"A new 300-total Bleed can replace an aged 100-total stack.");
        controller.ApplyStatus(effect,1,5,5,source,2);
        Assert.That(Stacks(controller,effect).Exists(x=>Mathf.Approximately(x.damagePerTick,5)),Is.False);
        Assert.That(controller.GetStatusSummaries()[0].MaximumStackCount,Is.EqualTo(5));
    }

    [Test]
    public void PoisonHasNoCapAndEachStackExpiresIndependentlyEveryTwoGlobalTurns()
    {
        var (controller,source)=Actor();var effect=Effect(StatusEffects.AilmentKind.Poison);
        controller.ApplyStatus(effect,8,10,4,source,2);
        Assert.That(Stacks(controller,effect).Count,Is.EqualTo(8));
        controller.TickStatuses(false);controller.TickStatuses(false);
        Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(3));
        controller.ApplyStatus(effect,1,20,4,source,2);
        for(int turn=0;turn<6;turn++)controller.TickStatuses(false);
        Assert.That(Stacks(controller,effect).Count,Is.EqualTo(1),"Old stacks expired after eight global turns; the later application survived.");
        Assert.That(Stacks(controller,effect)[0].remainingTicks,Is.EqualTo(1));
        Assert.That(controller.GetStatusSummaries()[0].MaximumStackCount,Is.Zero);
    }

    [Test]
    public void OffensivelyScaledPreDefenseHitBasisMitigatesOnlyItsOwnAilmentTick()
    {
        var (controller,attacker)=Actor();var effect=Effect(StatusEffects.AilmentKind.Ignite);
        var hit=new DamageContext(1);hit.AddDamage(Element.Fire,100);
        AilmentCalculator.ComputeAilmentFromHit(effect,hit,attacker,out var baseTick,out var ticks,out var interval);
        Assert.That(baseTick,Is.EqualTo(50).Within(.001));Assert.That(ticks,Is.EqualTo(2));Assert.That(interval,Is.EqualTo(2));
        hit.Hits[0]=new ElementalHit(Element.Fire,200);
        AilmentCalculator.ComputeAilmentFromHit(effect,hit,attacker,out var scaledTick,out _,out _);
        Assert.That(scaledTick,Is.EqualTo(baseTick*2).Within(.001),"A hit increased/More/crit/range result is already in the pre-defense hit context.");
        controller.GetComponent<StatsComponent>().SetBaseStat(StatTypes.IgniteRes,50);
        float mitigated=CombatCalculator.CalculateAilmentTickDamage(scaledTick,effect,attacker,controller.GetComponent<StatsComponent>());
        Assert.That(mitigated,Is.EqualTo(scaledTick*.5f).Within(.001));
    }

    (StatusController,StatsComponent) Actor()
    {
        var go=new GameObject("ailment fixture",typeof(StatsComponent),typeof(StatusController));cleanup.Add(go);
        var stats=go.GetComponent<StatsComponent>();var status=go.GetComponent<StatusController>();
        typeof(StatusController).GetField("stats",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(status,stats);
        return (status,stats);
    }
    StatusEffects Effect(StatusEffects.AilmentKind kind)
    {
        var effect=ScriptableObject.CreateInstance<StatusEffects>();cleanup.Add(effect);
        var mask=kind==StatusEffects.AilmentKind.Bleed?ElementMask.Phys:kind==StatusEffects.AilmentKind.Ignite?ElementMask.Fire:ElementMask.All;
        effect.ConfigureRuntime(kind.ToString(),StatusEffects.StatusType.DamageOverTime,kind,mask,1,2,1,StatusEffects.StackPolicy.StackIndependently,2);
        return effect;
    }
    static List<StatusInstance> Stacks(StatusController controller,StatusEffects effect)
    {
        var dictionary=(Dictionary<StatusEffects,List<StatusInstance>>)typeof(StatusController)
            .GetField("IndependentDictionary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
        return dictionary.TryGetValue(effect,out var stacks)?stacks:new List<StatusInstance>();
    }
}
