using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BlackCube;

public sealed class BalanceSimulationTests
{
    [Test]
    public void ShivKeepsAHitButUsesDedicatedBleedWeightedCoefficients()
    {
        var shiv=PlayerSkillDefinition.CreateDefaults()
            .Single(skill=>skill.id==PlayerSkillId.Shiv);
        Assert.That(shiv.hitDamageMultiplier,Is.EqualTo(.5f));
        Assert.That(shiv.ailmentBasisMultiplier,Is.EqualTo(20f));
        Assert.That(shiv.specializedAilment,Is.EqualTo(StatusEffects.AilmentKind.Bleed));
        Assert.That(shiv.guaranteedAilmentApplications,Is.EqualTo(1));
        var catalog=Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        Assert.That(catalog,Is.Not.Null);
        var production=catalog.skills.Single(skill=>skill.id==PlayerSkillId.Shiv);
        Assert.That(production.hitDamageMultiplier,Is.EqualTo(shiv.hitDamageMultiplier));
        Assert.That(production.ailmentBasisMultiplier,Is.EqualTo(shiv.ailmentBasisMultiplier));
    }

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
    public void ExistingGameplaySingletonsCannotInfluenceIsolatedBaseline()
    {
        var config = new BalanceSimulationConfig { seed = 11012, sampleCount = 1, levels = new[] { 1 } };
        BalanceResult before = BalanceSimulationRunner.Run(config);
        object previousMod = ModManager.Instance;
        object previousPools = GearStatLists.Instance;
        object previousRelic = RelicInventory.Instance;
        var fakeMod = new GameObject("live mods", typeof(ModManager));
        var fakePools = new GameObject("live pools", typeof(GearStatLists));
        var fakeRelic = new GameObject("live relics", typeof(RelicInventory));
        try
        {
            SetInstance(typeof(ModManager), fakeMod.GetComponent<ModManager>());
            SetInstance(typeof(GearStatLists), fakePools.GetComponent<GearStatLists>());
            SetInstance(typeof(RelicInventory), fakeRelic.GetComponent<RelicInventory>());
            BalanceResult during = BalanceSimulationRunner.Run(config);
            for (int i = 0; i < before.rows.Length; i++)
                Assert.That(JsonUtility.ToJson(during.rows[i]), Is.EqualTo(JsonUtility.ToJson(before.rows[i])));
            Assert.That(ModManager.Instance, Is.EqualTo(fakeMod.GetComponent<ModManager>()));
            Assert.That(GearStatLists.Instance, Is.EqualTo(fakePools.GetComponent<GearStatLists>()));
            Assert.That(RelicInventory.Instance, Is.EqualTo(fakeRelic.GetComponent<RelicInventory>()));
        }
        finally
        {
            SetInstance(typeof(ModManager), previousMod);
            SetInstance(typeof(GearStatLists), previousPools);
            SetInstance(typeof(RelicInventory), previousRelic);
            UnityEngine.Object.DestroyImmediate(fakeMod);
            UnityEngine.Object.DestroyImmediate(fakePools);
            UnityEngine.Object.DestroyImmediate(fakeRelic);
        }
    }

    private static void SetInstance(Type type, object value) => type.GetProperty("Instance",
        BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { value });

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
    public void EveryReferenceReportFormatCarriesExactSyntheticPlayerWarning()
    {
        var result = new BalanceResult
        {
            config = new BalanceSimulationConfig { sampleCount = 1, levels = new[] { 1 } },
            archetypes = new[] { "test" },
            rows = new[] { new BalanceRow { archetype = "test", level = 1,
                referencePlayerLife = 1000f, referencePlayerHit = 80f } }
        };
        string csv = (string)typeof(BalanceReportWriter).GetMethod("Csv",
            BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { result });
        Assert.That(csv, Does.Contain("referenceWarning"));
        Assert.That(csv, Does.Contain(result.warning));
        Assert.That(JsonUtility.ToJson(result), Does.Contain(result.warning));
        Assert.That(BalanceReportWriter.Markdown(result), Does.Contain(result.warning));
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

    [Test]
    public void SnapshotDuelUsesSyntheticDefenseAndExactShockThresholdDamage()
    {
        var offenseHost = new GameObject("player offense", typeof(StatsComponent));
        var defenseHost = new GameObject("synthetic defense", typeof(StatsComponent));
        var enemyHost = new GameObject("enemy", typeof(StatsComponent));
        try
        {
            var playerOffense = offenseHost.GetComponent<StatsComponent>();
            var playerDefense = defenseHost.GetComponent<StatsComponent>();
            var enemy = enemyHost.GetComponent<StatsComponent>();
            playerDefense.SetBaseStat(StatTypes.FlatArmour, 1000f);
            var idle = new DamageContext(1);
            var physical = new DamageContext(1); physical.AddDamage(Element.Phys, 100f);
            var armored = BalanceCombatSimulator.Simulate(idle, playerOffense, playerDefense,
                1000f, .0001f, 0f, physical, enemy, enemy, 1000f, 1f, 0f,
                Array.Empty<StatusEffects>(), 42, 1.1f);
            Assert.That(armored.PlayerRemainingLife,
                Is.EqualTo(1000f - CombatCalculator.ApplyArmourValue(100f, 1000f, 0f)).Within(.0001f));

            playerOffense.SetBaseStat(StatTypes.ShockChance, 100f);
            var lightning = new DamageContext(1); lightning.AddDamage(Element.Light, 10f);
            var shocked = BalanceCombatSimulator.Simulate(lightning, playerOffense, playerDefense,
                1000f, 1f, 0f, idle, enemy, enemy, 1000f, .0001f, 0f,
                Array.Empty<StatusEffects>(), 42, 5.1f);
            Assert.That(shocked.ShockTriggers, Is.EqualTo(1));
            Assert.That(shocked.EnemyRemainingLife, Is.EqualTo(945f).Within(.0001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(offenseHost);
            UnityEngine.Object.DestroyImmediate(defenseHost);
            UnityEngine.Object.DestroyImmediate(enemyHost);
        }
    }

    [Test]
    public void SnapshotDuelUsesProductionCriticalBaseAndTurnOwnedDamagingTicks()
    {
        var sourceHost = new GameObject("balance crit source", typeof(StatsComponent));
        var targetHost = new GameObject("balance ailment target", typeof(StatsComponent));
        var bleed = ScriptableObject.CreateInstance<StatusEffects>();
        var poison = ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            var source = sourceHost.GetComponent<StatsComponent>();
            var target = targetHost.GetComponent<StatsComponent>();
            var physical = new DamageContext(1); physical.AddDamage(Element.Phys, 100f);
            var idle = new DamageContext(1);
            var critical = BalanceCombatSimulator.Simulate(physical, source, 1000f, 1f, 1f,
                idle, target, 1000f, .0001f, 0f, Array.Empty<StatusEffects>(), 51, 1.1f);
            Assert.That(critical.EnemyRemainingLife, Is.EqualTo(850f).Within(.0001f));

            source.SetBaseStat(StatTypes.BleedChance, 100f);
            bleed.ConfigureRuntime("Bleed", StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Bleed, ElementMask.Phys, .1f, 5, 5,
                StatusEffects.StackPolicy.StackIndependently, 2);
            var attackerOnlyTurns = BalanceCombatSimulator.Simulate(physical, source, 1000f, 1f, 0f,
                idle, target, 1000f, .0001f, 0f, new[] { bleed }, 52, 3.1f);
            var afflictedTurns = BalanceCombatSimulator.Simulate(physical, source, 1000f, 1f, 0f,
                idle, target, 1000f, 1f, 0f, new[] { bleed }, 52, 3.1f);
            Assert.That(attackerOnlyTurns.AilmentTicks, Is.Zero);
            Assert.That(afflictedTurns.AilmentTicks, Is.GreaterThan(0));

            source.SetBaseStat(StatTypes.BleedChance, 0f);
            source.SetBaseStat(StatTypes.PoisonChance, 100f);
            poison.ConfigureRuntime("Poison", StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Poison, ElementMask.Phys, .1f, 4, 100,
                StatusEffects.StackPolicy.StackIndependently, 2);
            var globalPoison = BalanceCombatSimulator.Simulate(physical, source, 1000f, 1f, 0f,
                idle, target, 1000f, .0001f, 0f, new[] { poison }, 53, 3.1f);
            Assert.That(globalPoison.AilmentTicks, Is.GreaterThan(0));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bleed);
            UnityEngine.Object.DestroyImmediate(poison);
            UnityEngine.Object.DestroyImmediate(sourceHost);
            UnityEngine.Object.DestroyImmediate(targetHost);
        }
    }

    [Test]
    public void SnapshotDuelRollsSeededWeaponEndpointsForEachRealStrike()
    {
        var sourceHost = new GameObject("balance ranged source", typeof(StatsComponent));
        var targetHost = new GameObject("balance ranged target", typeof(StatsComponent));
        try
        {
            var source = sourceHost.GetComponent<StatsComponent>();
            var target = targetHost.GetComponent<StatsComponent>();
            var average = new DamageContext(1); average.AddDamage(Element.Phys, 100f);
            var low = new DamageContext(1); low.AddDamage(Element.Phys, 50f);
            var high = new DamageContext(1); high.AddDamage(Element.Phys, 150f);
            var idle = new DamageContext(1);
            const int seed = 1212;
            var random = new System.Random(seed);
            float first = 50f + 100f * (float)random.NextDouble();
            var outcome = BalanceCombatSimulator.Simulate(average, source, 1000f, 1f, 0f,
                idle, target, 1000f, .0001f, 0f, Array.Empty<StatusEffects>(), seed, 1.1f,
                low, high);
            Assert.That(outcome.EnemyRemainingLife, Is.EqualTo(1000f-first).Within(.0001f));
            Assert.That(outcome.EnemyRemainingLife, Is.Not.EqualTo(900f).Within(.0001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceHost);
            UnityEngine.Object.DestroyImmediate(targetHost);
        }
    }

    [Test]
    public void EngagedDuelSchedulesInstantSkillsAgainstRealManaRecoveryWithoutStoppingAutoAttacks()
    {
        var playerHost=new GameObject("active player",typeof(StatsComponent));
        var enemyHost=new GameObject("active enemy",typeof(StatsComponent));
        try
        {
            var direct=new DamageContext(1);direct.AddDamage(Element.Phys,100f);
            var idle=new DamageContext(1);
            var skill=new PlayerSkillDefinition{id=PlayerSkillId.HeavyStrike,
                manaCost=20f,hitDamageMultiplier=1f};
            var plan=new BalanceCombatSimulator.ActiveSkillPlan(skill,direct,direct,20f,20f,10f,
                castSpacing:1.5f);
            var result=BalanceCombatSimulator.SimulateWithSkill(idle,playerHost.GetComponent<StatsComponent>(),
                1000f,.0001f,0f,idle,enemyHost.GetComponent<StatsComponent>(),
                350f,.0001f,0f,Array.Empty<StatusEffects>(),plan,13001,7f);
            Assert.That(result.Winner,Is.EqualTo(1));
            Assert.That(result.SkillCasts,Is.EqualTo(4));
            Assert.That(result.Seconds,Is.EqualTo(6f).Within(.01f));
            Assert.That(result.DirectEnemyDamage,Is.EqualTo(350f).Within(.01f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerHost);
            UnityEngine.Object.DestroyImmediate(enemyHost);
        }
    }

    [Test]
    public void EngagedEnvenomGuaranteedPoisonUsesSpecializedBasisDespiteZeroDirectHitAndChance()
    {
        var playerHost=new GameObject("poison player",typeof(StatsComponent));
        var enemyHost=new GameObject("poison enemy",typeof(StatsComponent));
        var poison=ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            poison.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Poison,ElementMask.Phys,.1f,4,0,
                StatusEffects.StackPolicy.StackIndependently,2);
            var empty=new DamageContext(1);
            var basis=new DamageContext(1);basis.AddDamage(Element.Phys,100f);
            var skill=new PlayerSkillDefinition{id=PlayerSkillId.Envenom,
                specializedAilment=StatusEffects.AilmentKind.Poison,
                guaranteedAilmentApplications=1,suppressDirectDamage=true};
            var plan=new BalanceCombatSimulator.ActiveSkillPlan(skill,empty,basis,0f,100f,0f);
            var result=BalanceCombatSimulator.SimulateWithSkill(empty,playerHost.GetComponent<StatsComponent>(),
                1000f,1f,0f,empty,enemyHost.GetComponent<StatsComponent>(),
                500f,1f,0f,new[]{poison},plan,13002,4.1f);
            Assert.That(result.SkillCasts,Is.GreaterThan(0));
            Assert.That(result.DirectEnemyDamage,Is.Zero);
            Assert.That(result.AilmentTicks,Is.GreaterThan(0));
            Assert.That(result.AilmentEnemyDamage,Is.GreaterThan(0f));
            skill.guaranteedAilmentApplications=0;
            var noGuarantee=BalanceCombatSimulator.SimulateWithSkill(empty,playerHost.GetComponent<StatsComponent>(),
                1000f,1f,0f,empty,enemyHost.GetComponent<StatsComponent>(),
                500f,1f,0f,new[]{poison},plan,13002,4.1f);
            Assert.That(noGuarantee.AilmentTicks,Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(poison);
            UnityEngine.Object.DestroyImmediate(playerHost);
            UnityEngine.Object.DestroyImmediate(enemyHost);
        }
    }
}
