using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class EnemyScalingTests
{
    [Test]
    public void CurrentPrefabSeedsAndLevelOneRemainUnchanged()
    {
        CheckPrefab("Assets/Prefabs/PaperBattle/Goblin2D.prefab", 250f, false);
        CheckPrefab("Assets/Prefabs/PaperBattle/Hobgoblin2D.prefab", 500f, true);
    }

    private static void CheckPrefab(string path, float seed, bool boss)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        var health = prefab.GetComponent<HealthComponent>();
        var ai = prefab.GetComponent<EnemyAI>();
        Assert.That(health.PrefabMaxLife, Is.EqualTo(seed));
        Assert.That(health.IsBoss, Is.EqualTo(boss));
        Assert.That(ai.baseSpeed, Is.EqualTo(.1f));
        var actor = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var setup = actor.GetComponent<EnemyStatSetup>();
            var stats = actor.GetComponent<StatsComponent>();
            actor.GetComponent<HealthComponent>().RestoreCheckpointLife(seed);
            setup.SetupForZone(1, boss);
            Assert.That(actor.GetComponent<HealthComponent>().MaxLife, Is.EqualTo(seed));
            Assert.That(setup.Intrinsic.DamageFactor, Is.EqualTo(1f));
            foreach (var stat in new[] { StatTypes.FlatArmour, StatTypes.FireRes, StatTypes.ColdRes,
                StatTypes.LightRes, StatTypes.VoidRes, StatTypes.LifeRegeneration })
                Assert.That(stats.GetRawStat(stat), Is.EqualTo(0f), $"{path}: {stat}");
            setup.SetupForZone(1, boss);
            Assert.That(actor.GetComponent<HealthComponent>().MaxLife, Is.EqualTo(seed));
        }
        finally { UnityEngine.Object.DestroyImmediate(actor); }
    }

    [Test]
    public void IntrinsicCurveIsFiniteMonotonicAndPostHundredSlows()
    {
        int[] levels = { -1, 1, 10, 25, 50, 75, 100, 101, 150, 200, 300, 1000 };
        var previous = EnemyScalingMath.Calculate(1);
        foreach (int level in levels)
        {
            var current = EnemyScalingMath.Calculate(level);
            Assert.That(!float.IsNaN(current.LifeFactor) && !float.IsInfinity(current.LifeFactor)
                && !float.IsNaN(current.DamageFactor) && !float.IsInfinity(current.DamageFactor), Is.True);
            Assert.That(current.LifeFactor, Is.GreaterThanOrEqualTo(previous.LifeFactor));
            Assert.That(current.DamageFactor, Is.GreaterThanOrEqualTo(previous.DamageFactor));
            Assert.That(current.Armour, Is.GreaterThanOrEqualTo(previous.Armour));
            Assert.That(current.ResistancePoints, Is.GreaterThanOrEqualTo(previous.ResistancePoints));
            previous = current;
        }
        var hundred = EnemyScalingMath.Calculate(100);
        var next = EnemyScalingMath.Calculate(101);
        Assert.That(hundred.LifeFactor, Is.EqualTo(Math.Pow(1.04, 99)).Within(.01));
        Assert.That(hundred.DamageFactor, Is.EqualTo(Math.Pow(1.03, 99)).Within(.01));
        Assert.That(next.LifeFactor / hundred.LifeFactor, Is.EqualTo(1.02f).Within(.0001f));
        Assert.That(next.DamageFactor / hundred.DamageFactor, Is.EqualTo(1.015f).Within(.0001f));
        Assert.That(hundred.Armour, Is.EqualTo(495f));
        Assert.That(EnemyScalingMath.Calculate(300).ResistancePoints, Is.EqualTo(20f));
        Assert.That(float.IsInfinity(EnemyScalingMath.Calculate(1000).ScaledLife(500f)), Is.False);
    }

    [Test]
    public void RepeatSetupReplacesOnlyOwnedScalingAndGearLifeAppliesAfterSeed()
    {
        var go = new GameObject("test enemy");
        try
        {
            var health = go.AddComponent<HealthComponent>();
            typeof(HealthComponent).GetField("maxLife", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(health, 250f);
            var setup = go.AddComponent<EnemyStatSetup>();
            var stats = go.GetComponent<StatsComponent>();
            setup.SetupForZone(50, false);
            stats.AddModifier(new StatModifier(StatTypes.Life, StatOp.Additive, 50f, this));
            stats.AddModifier(new StatModifier(StatTypes.LifePercent, StatOp.Additive, 20f, this));
            float expected = (250f * EnemyScalingMath.Calculate(50).LifeFactor + 50f) * 1.2f;
            Assert.That(health.MaxLife, Is.EqualTo(expected).Within(.001f));
            setup.SetupForZone(50, false);
            Assert.That(health.MaxLife, Is.EqualTo(expected).Within(.001f));
            Assert.That(stats.GetRawStat(StatTypes.FlatArmour), Is.EqualTo(245f));
            Assert.That(stats.GetRawStat(StatTypes.FireRes), Is.EqualTo(7.35f).Within(.0001f));
            Assert.That(stats.GetRawStat(StatTypes.PoisonRes), Is.EqualTo(0f));
            Assert.That(stats.GetRawStat(StatTypes.MaxFireRes), Is.EqualTo(0f));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void OutgoingPackageScalesOnceIncludingVoidSourceForPoison()
    {
        var ctx = new DamageContext(2);
        ctx.AddDamage(Element.Phys, 10f);
        ctx.AddDamage(Element.Void, 5f);
        EnemyScalingMath.ScaleOutgoing(ctx, EnemyScalingMath.Calculate(50).DamageFactor);
        Assert.That(ctx.Hits[0].Amount, Is.EqualTo(10f * EnemyScalingMath.Calculate(50).DamageFactor).Within(.001f));
        Assert.That(ctx.Hits[1].Amount, Is.EqualTo(5f * EnemyScalingMath.Calculate(50).DamageFactor).Within(.001f));
        var host = new GameObject("poison attacker", typeof(StatsComponent));
        var effect = ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            effect.ConfigureRuntime("Poison", StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Poison, ElementMask.Void, .1f, 2, 100,
                StatusEffects.StackPolicy.StackAndRefresh, 4);
            var baseVoid = new DamageContext(1); baseVoid.AddDamage(Element.Void, 5f);
            AilmentCalculator.ComputeAilmentFromHit(effect, baseVoid, host.GetComponent<StatsComponent>(),
                out float baseTick, out _, out _);
            var scaledVoid = new DamageContext(1); scaledVoid.AddDamage(Element.Void, 5f);
            EnemyScalingMath.ScaleOutgoing(scaledVoid, EnemyScalingMath.Calculate(50).DamageFactor);
            AilmentCalculator.ComputeAilmentFromHit(effect, scaledVoid, host.GetComponent<StatsComponent>(),
                out float scaledTick, out _, out _);
            Assert.That(scaledTick / baseTick,
                Is.EqualTo(EnemyScalingMath.Calculate(50).DamageFactor).Within(.0001f));
        }
        finally { UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(host); }
    }

    [Test]
    public void RuntimeGeneratedActorsAndOptimizerAgreeWithCanonicalScalingAtRepresentativeLevels()
    {
        var prefabs = new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaperBattle/Goblin2D.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaperBattle/Hobgoblin2D.prefab")
        };
        UnityEngine.Random.State saved = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(11012);
            foreach (int level in new[] { 1, 10, 25, 50, 75, 100 })
            foreach (GameObject prefab in prefabs)
            {
                var actor = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    var ai = actor.GetComponent<EnemyAI>();
                    var setup = actor.GetComponent<EnemyStatSetup>();
                    var stats = actor.GetComponent<StatsComponent>();
                    ai.GenerateIsolatedBuild(level, null, EnemyAI.EnemyRarity.Normal);
                    var math = EnemyScalingMath.Calculate(level);
                    Assert.That(setup.Intrinsic.LifeFactor, Is.EqualTo(math.LifeFactor));
                    Assert.That(ai.LastOptimizerBaseStats[(int)StatTypes.Life],
                        Is.EqualTo(math.ScaledLife(setup.AuthoredLevelOneLife)).Within(.001f));
                    Assert.That(ai.LastOptimizerBaseStats[(int)StatTypes.FlatArmour], Is.EqualTo(math.Armour));
                    Assert.That(ai.LastOptimizerBaseStats[(int)StatTypes.VoidRes], Is.EqualTo(math.ResistancePoints));
                    Assert.That(actor.GetComponent<HealthComponent>().MaxLife,
                        Is.GreaterThanOrEqualTo(math.ScaledLife(setup.AuthoredLevelOneLife)));
                    Assert.That(CombatCalculator.GetMaximumResistance(Element.Void, stats),
                        Is.GreaterThanOrEqualTo(CombatCalculator.BaseMaximumResistance));
                    var parity = EnemyBuildOptimizer.Evaluate(ai.EquippedItems, ai.LastOptimizerBaseStats,
                        ai.baseSpeed, ai.IntrinsicDamageFactor);
                    Assert.That(ai.LastBuildEvaluation.Offense,
                        Is.EqualTo(parity.Offense).Within(.001f));
                    Assert.That(ai.LastBuildEvaluation.Defense,
                        Is.EqualTo(parity.Defense).Within(.001f));
                    if (level > 1)
                        Assert.That(parity.Offense, Is.GreaterThan(EnemyBuildOptimizer.Evaluate(
                            ai.EquippedItems, ai.LastOptimizerBaseStats, ai.baseSpeed, 1f).Offense));
                }
                finally { UnityEngine.Object.DestroyImmediate(actor); }
            }
        }
        finally { UnityEngine.Random.state = saved; }
    }
}
