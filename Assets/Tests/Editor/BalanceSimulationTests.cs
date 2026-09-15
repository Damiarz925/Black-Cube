using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using BlackCube;

public sealed class BalanceSimulationTests
{
    [Test]
    public void CurrentEnemyDiscoveryAndSeededSamplingAreDeterministicAndIsolated()
    {
        string[] names = BalanceSimulationRunner.DiscoverEnemies().Select(prefab => prefab.name).ToArray();
        Assert.That(names, Does.Contain("Goblin2D"));
        Assert.That(names, Does.Contain("Hobgoblin2D"));
        var config = new BalanceSimulationConfig { seed = 11012, sampleCount = 2, levels = new[] { 1 } };
        UnityEngine.Random.State original = UnityEngine.Random.state;
        float expected = UnityEngine.Random.value;
        UnityEngine.Random.state = original;
        string prefsBefore = PlayerPrefs.GetString("BlackCube_BalanceIsolationSentinel", "not-set");
        BalanceResult first = BalanceSimulationRunner.Run(config);
        BalanceResult second = BalanceSimulationRunner.Run(config);
        Assert.That(first.rows.Length, Is.EqualTo(4));
        for (int i = 0; i < first.rows.Length; i++)
            Assert.That(JsonUtility.ToJson(first.rows[i]), Is.EqualTo(JsonUtility.ToJson(second.rows[i])));
        Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
        Assert.That(PlayerPrefs.GetString("BlackCube_BalanceIsolationSentinel", "not-set"), Is.EqualTo(prefsBefore));
        Assert.That(first.rows.All(row => row.level == 1 && row.lifeFactor == 1f && row.damageFactor == 1f), Is.True);
    }

    [Test]
    public void DifferentSeedsCanChangeProductionGeneratedBuilds()
    {
        var first = BalanceSimulationRunner.Run(new BalanceSimulationConfig
            { seed = 11012, sampleCount = 5, levels = new[] { 10 } });
        var second = BalanceSimulationRunner.Run(new BalanceSimulationConfig
            { seed = 55555, sampleCount = 5, levels = new[] { 10 } });
        Assert.That(first.rows.Zip(second.rows, (a, b) => a.equipmentSignature + a.affixSignature
            != b.equipmentSignature + b.affixSignature).Any(changed => changed), Is.True);
    }

    [Test]
    public void ReferenceCurveAndMitigationUseProductionMath()
    {
        Assert.That(BalanceSimulationRunner.ReferenceFactor(1), Is.EqualTo(1f));
        Assert.That(BalanceSimulationRunner.ReferenceFactor(100),
            Is.EqualTo(Math.Pow(1.035, 99)).Within(.01));
        Assert.That(BalanceSimulationRunner.ReferenceFactor(101) / BalanceSimulationRunner.ReferenceFactor(100),
            Is.EqualTo(1.018f).Within(.0001f));
        var go = new GameObject("balance parity", typeof(StatsComponent));
        try
        {
            var defense = go.GetComponent<StatsComponent>();
            defense.SetBaseStat(StatTypes.FlatArmour, 245f);
            defense.SetBaseStat(StatTypes.VoidRes, 20f);
            var phys = new DamageContext(1); phys.AddDamage(Element.Phys, 100f);
            var voidHit = new DamageContext(1); voidHit.AddDamage(Element.Void, 100f);
            Assert.That(CombatCalculator.CalculateFinalDamage(phys, null, defense),
                Is.EqualTo(CombatCalculator.ApplyArmourValue(100f, 245f, 0f)));
            Assert.That(CombatCalculator.CalculateFinalDamage(voidHit, null, defense),
                Is.EqualTo(CombatCalculator.ApplyResistanceValue(100f, .2f, 0f,
                    CombatCalculator.GetMaximumResistance(Element.Void, defense))));
            var poison = ScriptableObject.CreateInstance<StatusEffects>();
            try
            {
                poison.ConfigureRuntime("Poison", StatusEffects.StatusType.DamageOverTime,
                    StatusEffects.AilmentKind.Poison, ElementMask.Void, .1f, 2, 100,
                    StatusEffects.StackPolicy.StackAndRefresh, 4);
                var attacker = new GameObject("attacker", typeof(StatsComponent));
                try
                {
                    AilmentCalculator.ComputeAilmentFromHit(poison, voidHit,
                        attacker.GetComponent<StatsComponent>(), out float tick, out _, out _);
                    Assert.That(CombatCalculator.CalculateAilmentTickDamage(tick, poison,
                        attacker.GetComponent<StatsComponent>(), defense),
                        Is.EqualTo(CombatCalculator.ApplyResistanceValue(tick, .2f, 0f,
                            CombatCalculator.GetMaximumResistance(Element.Void, defense))));
                }
                finally { UnityEngine.Object.DestroyImmediate(attacker); }
            }
            finally { UnityEngine.Object.DestroyImmediate(poison); }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void MonteCarloIsSeededAndExercisesHitTwicePoisonShockAndChill()
    {
        var a = new GameObject("attacker", typeof(StatsComponent));
        var b = new GameObject("target", typeof(StatsComponent));
        var poison = ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            var attacker = a.GetComponent<StatsComponent>();
            var target = b.GetComponent<StatsComponent>();
            attacker.SetBaseStat(StatTypes.ChanceToHitTwice, 100f);
            attacker.SetBaseStat(StatTypes.PoisonChance, 100f);
            attacker.SetBaseStat(StatTypes.ShockChance, 100f);
            attacker.SetBaseStat(StatTypes.ChillChance, 100f);
            poison.ConfigureRuntime("Poison", StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Poison, ElementMask.All, .1f, 2, 100,
                StatusEffects.StackPolicy.StackAndRefresh, 4);
            var attack = new DamageContext(4);
            attack.AddDamage(Element.Void, 10f);
            attack.AddDamage(Element.Light, 10f);
            attack.AddDamage(Element.Cold, 10f);
            var idle = new DamageContext(1);
            var effects = new[] { poison };
            var first = BalanceCombatSimulator.Simulate(attack, attacker, 1000f, 1f, 0f,
                idle, target, 1000f, .0001f, 0f, effects, 42, 5f);
            var again = BalanceCombatSimulator.Simulate(attack, attacker, 1000f, 1f, 0f,
                idle, target, 1000f, .0001f, 0f, effects, 42, 5f);
            Assert.That(first.Hits, Is.EqualTo(again.Hits));
            Assert.That(first.AilmentTicks, Is.EqualTo(again.AilmentTicks));
            Assert.That(first.Hits, Is.GreaterThan(5));
            Assert.That(first.AilmentTicks, Is.GreaterThan(0));
            Assert.That(first.ShockTriggers, Is.GreaterThan(0));
            Assert.That(first.ChillApplications, Is.GreaterThan(0));
            Assert.That(BattleManager.CalculateChillSlow(100f, 1000f, 0f), Is.GreaterThan(0f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(poison);
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }
    }
}
