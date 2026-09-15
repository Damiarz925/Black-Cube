using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Step10MechanicsTests
{
    readonly List<UnityEngine.Object> created = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();
    }

    [Test]
    public void V1PoolsExcludeDeprecatedAndAmbiguousAccuracyStats()
    {
        var forbidden = new HashSet<StatTypes>
        {
            StatTypes.Accuracy, StatTypes.FlatEvasion, StatTypes.EvasionPercent,
            StatTypes.ChanceToBlock, StatTypes.CooldownRecovery, StatTypes.ManaCost,
            StatTypes.AccuracyPerDexterity
        };
        foreach (var pair in GearStatLists.BuildDefaultStatPools())
            foreach (StatTypes stat in pair.Value)
                Assert.That(forbidden.Contains(stat), Is.False, $"{pair.Key} still exposes {stat}");
    }

    [Test]
    public void EnemyAffixPolicyExcludesPlayerOnlyResourcesSkillsAndKillRecovery()
    {
        StatTypes[] excluded =
        {
            StatTypes.Mana, StatTypes.ManaPercent, StatTypes.ManaRegeneration, StatTypes.ManaOnHit,
            StatTypes.ManaOnKill, StatTypes.DmgPerMaxMana, StatTypes.DmgPerCurrentMana,
            StatTypes.ManaPerIntelligence, StatTypes.LifeOnKill,
            StatTypes.Plus1Phys, StatTypes.Plus1Fire, StatTypes.Plus1Cold, StatTypes.Plus1Light,
            StatTypes.Plus1Poison, StatTypes.Plus1Bleed, StatTypes.Plus1Ignite
        };
        foreach (StatTypes stat in excluded)
            Assert.That(ModManager.IsPlayerOnlyAffix(stat), Is.True, stat.ToString());
        Assert.That(ModManager.IsPlayerOnlyAffix(StatTypes.Life), Is.False);
        Assert.That(ModManager.IsPlayerOnlyAffix(StatTypes.ChanceToHitTwice), Is.False);
    }

    [Test]
    public void MaximumResistanceUsesBaseIndividualAllAndHardCeiling()
    {
        var go = NewGameObject("max-res");
        var stats = go.AddComponent<StatsComponent>();
        Assert.That(CombatCalculator.GetMaximumResistance(Element.Fire, stats), Is.EqualTo(.75f).Within(.0001f));
        stats.AddModifier(new StatModifier(StatTypes.MaxFireRes, StatOp.Additive, 5f, this));
        stats.AddModifier(new StatModifier(StatTypes.MaxAllRes, StatOp.Additive, 4f, this));
        Assert.That(CombatCalculator.GetMaximumResistance(Element.Fire, stats), Is.EqualTo(.84f).Within(.0001f));
        stats.AddModifier(new StatModifier(StatTypes.MaxFireRes, StatOp.Additive, 50f, new object()));
        Assert.That(CombatCalculator.GetMaximumResistance(Element.Fire, stats), Is.EqualTo(.9f).Within(.0001f));
        Assert.That(CombatCalculator.ApplyResistanceValue(100f, 1f, .1f, .8f), Is.EqualTo(30f).Within(.001f));
    }

    [TestCase(Element.Fire, StatTypes.MaxFireRes)]
    [TestCase(Element.Cold, StatTypes.MaxColdRes)]
    [TestCase(Element.Light, StatTypes.MaxLightRes)]
    [TestCase(Element.Void, StatTypes.MaxVoidRes)]
    public void MaximumResistanceAppliesOnlyMatchingCapThenMaximumAll(Element element, StatTypes capStat)
    {
        var stats = NewGameObject("matching-cap").AddComponent<StatsComponent>();
        stats.SetBaseStat(capStat, 5f);
        stats.SetBaseStat(StatTypes.MaxAllRes, 4f);
        Assert.That(CombatCalculator.GetMaximumResistance(element, stats), Is.EqualTo(.84f).Within(.0001f));
        foreach (Element other in new[] { Element.Fire, Element.Cold, Element.Light, Element.Void })
            if (other != element)
                Assert.That(CombatCalculator.GetMaximumResistance(other, stats), Is.EqualTo(.79f).Within(.0001f));
    }

    [Test]
    public void CreditedBossDeathCanBeClaimedOnlyOnce()
    {
        var health = NewGameObject("claimed-boss").AddComponent<HealthComponent>();
        health.SetEnemyRole(true);
        health.ReviveToFullLife(); // Explicit lifecycle initialization for EditMode-created actors.
        Assert.That(health.TryClaimEnemyDeath(), Is.False, "Nonlethal actors grant no reward");
        // Die schedules a delayed GameObject.Destroy in PlayMode; avoid that
        // editor-only side effect while testing the independent claim guard.
        typeof(HealthComponent).GetField("isDead", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(health, true);
        Assert.That(health.IsBoss, Is.True);
        Assert.That(health.TryClaimEnemyDeath(), Is.True);
        Assert.That(health.TryClaimEnemyDeath(), Is.False, "Duplicate death cannot grant recovery");
    }

    [Test]
    public void ShockChanceOverflowGuaranteesWholeHundredsAndRollsRemainder()
    {
        UnityEngine.Random.State prior = UnityEngine.Random.state;
        try
        {
            Assert.That(BattleManager.RollOverflowApplications(0f), Is.EqualTo(0));
            // Combat receives GetStat fractions: 100 percentage points = 1f.
            Assert.That(BattleManager.RollOverflowApplications(1f), Is.EqualTo(1));
            for (int i = 0; i < 20; i++)
                Assert.That(BattleManager.RollOverflowApplications(2.5f), Is.InRange(2, 3));
        }
        finally { UnityEngine.Random.state = prior; }
    }

    [TestCase(0f, 0, 1)]
    [TestCase(.99f, 0, 1)]
    [TestCase(1f, 0, 2)]
    [TestCase(2.75f, 0, 3)]
    [TestCase(0f, 2, 3)]
    public void ProjectileCountUsesWholePassiveAdditions(float raw, int keystone, int expected)
    {
        Assert.That(BattleManager.CalculateProjectileCount(raw, keystone), Is.EqualTo(expected));
    }

    [Test]
    public void EnemyMaximumLifeConsumesFlatAndPercentStatsWithoutIntrinsicScaling()
    {
        var go = NewGameObject("enemy-life");
        var stats = go.AddComponent<StatsComponent>();
        var health = go.AddComponent<HealthComponent>();
        // EditMode AddComponent does not execute MonoBehaviour.Awake.
        health.ReviveToFullLife();
        stats.SetBaseStat(StatTypes.Life, 100f);
        Assert.That(stats.GetStat(StatTypes.Life), Is.EqualTo(100f));
        Assert.That(health.MaxLife, Is.EqualTo(100f));
        Assert.That(health.CurrentLife, Is.EqualTo(100f));
        health.UseStatsForMaximumLife(stats);
        stats.AddModifier(new StatModifier(StatTypes.Life, StatOp.Additive, 50f, this));
        stats.AddModifier(new StatModifier(StatTypes.LifePercent, StatOp.Additive, 20f, this));
        Assert.That(health.MaxLife, Is.EqualTo(180f).Within(.001f));
        health.RestoreFullLife();
        Assert.That(health.CurrentLife, Is.EqualTo(180f).Within(.001f));
    }

    [Test]
    public void OptimizerValuesRealEnemyLifeAndHitTwice()
    {
        float[] baseline = new float[Enum.GetValues(typeof(StatTypes)).Length];
        baseline[(int)StatTypes.Life] = 100f;
        var weapon = GearWith(StatTypes.WeaponBaseDmg, 100f, LootManager.GearType.Weapons);
        weapon.RebuildMods();
        weapon.BaseAttackSpeed = 1f;
        var life = GearWith(StatTypes.Life, 100f, LootManager.GearType.Helmets);
        var twice = GearWith(StatTypes.ChanceToHitTwice, 100f, LootManager.GearType.Amulets);
        var baseEval = EnemyBuildOptimizer.Evaluate(new[] { weapon }, baseline, 1f);
        Assert.That(EnemyBuildOptimizer.Evaluate(new[] { weapon, life }, baseline, 1f).Defense, Is.GreaterThan(baseEval.Defense));
        Assert.That(EnemyBuildOptimizer.Evaluate(new[] { weapon, twice }, baseline, 1f).Offense, Is.GreaterThan(baseEval.Offense));
    }

    [Test]
    public void VoidIsAppendOnlyAndLegacyPoisonNormalizesToVoid()
    {
        Assert.That((int)StatTypes.FlatVoid, Is.EqualTo(114));
        Assert.That((int)StatTypes.MaxVoidRes, Is.EqualTo(119));
        Assert.That((int)Element.Poison, Is.EqualTo(4));
        Assert.That((int)Element.Void, Is.EqualTo(5));
        var context = new DamageContext(1);
        context.AddDamage(Element.Poison, 25f);
        Assert.That(context.Hits[0].Element, Is.EqualTo(Element.Void));
    }

    [Test]
    public void VoidDamageResistancePenetrationAndMaximumResistanceUseCorePipeline()
    {
        var attacker = NewGameObject("void-attacker").AddComponent<StatsComponent>();
        var defender = NewGameObject("void-defender").AddComponent<StatsComponent>();
        attacker.SetBaseStat(StatTypes.VoidPenetration, 10f);
        defender.SetBaseStat(StatTypes.VoidRes, 100f);
        defender.SetBaseStat(StatTypes.MaxVoidRes, 5f);
        var hit = new DamageContext(1);
        hit.AddDamage(Element.Void, 100f);
        Assert.That(CombatCalculator.CalculateFinalDamage(hit, attacker, defender), Is.EqualTo(30f).Within(.001f));
    }

    [Test]
    public void PoisonUsesPoisonAndVoidScalingOnceAndVoidMitigation()
    {
        var attacker = NewGameObject("poison-attacker").AddComponent<StatsComponent>();
        var defender = NewGameObject("poison-defender").AddComponent<StatsComponent>();
        var effect = NewEffect("Poison", StatusEffects.StatusType.DamageOverTime,
            StatusEffects.AilmentKind.Poison, ElementMask.Phys | ElementMask.Void, .1f, 2);
        attacker.SetBaseStat(StatTypes.PoisonDmg, 50f);
        attacker.SetBaseStat(StatTypes.VoidDmg, 50f);

        var physical = new DamageContext(1); physical.AddDamage(Element.Phys, 100f);
        AilmentCalculator.ComputeAilmentFromHit(effect, physical, attacker,
            out float physicalTick, out _, out _);
        Assert.That(physicalTick, Is.EqualTo(11.25f).Within(.001f));

        var alreadyScaledVoid = new DamageContext(1); alreadyScaledVoid.AddDamage(Element.Void, 150f);
        AilmentCalculator.ComputeAilmentFromHit(effect, alreadyScaledVoid, attacker,
            out float voidTick, out _, out _);
        Assert.That(voidTick, Is.EqualTo(11.25f).Within(.001f), "Void increased damage must not double dip");

        defender.SetBaseStat(StatTypes.VoidRes, 50f);
        defender.SetBaseStat(StatTypes.PoisonRes, 90f);
        Assert.That(CombatCalculator.CalculateAilmentTickDamage(physicalTick, effect, attacker, defender),
            Is.EqualTo(physicalTick * .5f).Within(.001f));
    }

    [Test]
    public void AttributesProjectFractionalInherentAndExplicitEffects()
    {
        var stats = NewGameObject("attributes").AddComponent<StatsComponent>();
        stats.SetBaseStat(StatTypes.Strength, 25f);
        stats.SetBaseStat(StatTypes.StrengthPercent, 20f);
        stats.SetBaseStat(StatTypes.Dexterity, 40f);
        stats.SetBaseStat(StatTypes.Intelligence, 50f);
        stats.SetBaseStat(StatTypes.DamagePerStrength, 2f);
        stats.SetBaseStat(StatTypes.LifePerStrength, 1f);
        Assert.That(DerivedStatCalculator.Strength(stats), Is.EqualTo(30f).Within(.001f));
        Assert.That(DerivedStatCalculator.StrengthIncreased(stats), Is.EqualTo(.03f).Within(.001f));
        Assert.That(DerivedStatCalculator.DexterityIncreased(stats), Is.EqualTo(.04f).Within(.001f));
        Assert.That(DerivedStatCalculator.IntelligenceIncreased(stats), Is.EqualTo(.05f).Within(.001f));
        Assert.That(DerivedStatCalculator.AddedLife(stats), Is.EqualTo(30f).Within(.001f));
        Assert.That(DerivedStatCalculator.GlobalIncreasedDamage(stats), Is.EqualTo(.06f).Within(.001f));
    }

    [Test]
    public void SkillLevelsUseLockedCapDamageAndManaFactors()
    {
        Assert.That(PlayerSkillController.CalculateEffectiveSkillLevel(0f), Is.EqualTo(1));
        Assert.That(PlayerSkillController.CalculateEffectiveSkillLevel(1f), Is.EqualTo(2));
        Assert.That(PlayerSkillController.CalculateEffectiveSkillLevel(99f), Is.EqualTo(20));
        Assert.That(PlayerSkillController.SkillDamageLevelFactor(10), Is.EqualTo(1.45f).Within(.001f));
        Assert.That(PlayerSkillController.ManaCostLevelFactor(20), Is.EqualTo(1.38f).Within(.001f));
        foreach (PlayerSkillId id in Enum.GetValues(typeof(PlayerSkillId)))
            Assert.That(PlayerSkillController.SkillLevelStat(id), Is.InRange(StatTypes.Plus1Phys, StatTypes.Plus1Ignite));
    }

    [Test]
    public void ManaScalingUsesPercentagePointUnitsAndCurrentSnapshot()
    {
        var go = NewGameObject("mana-scaling");
        var stats = go.AddComponent<StatsComponent>();
        stats.SetBaseStat(StatTypes.Mana, 500f);
        stats.SetBaseStat(StatTypes.DmgPerMaxMana, 3f);
        stats.SetBaseStat(StatTypes.DmgPerCurrentMana, 6f);
        var mana = go.AddComponent<ManaComponent>();
        mana.RestoreFull();
        Assert.That(stats.GetStat(StatTypes.Mana), Is.EqualTo(500f));
        Assert.That(mana.MaxMana, Is.EqualTo(500f));
        Assert.That(mana.CurrentMana, Is.EqualTo(500f));
        Assert.That(DerivedStatCalculator.GlobalIncreasedDamage(stats, mana), Is.EqualTo(.45f).Within(.001f));
        mana.TrySpend(200f);
        Assert.That(DerivedStatCalculator.GlobalIncreasedDamage(stats, mana), Is.EqualTo(.33f).Within(.001f));
    }

    [Test]
    public void ShockConsumesThresholdPreservesOverflowAndExpires()
    {
        var target = NewGameObject("shock-target");
        target.AddComponent<StatsComponent>();
        var statuses = target.AddComponent<StatusController>();
        var effect = NewEffect("Shock", StatusEffects.StatusType.Shock,
            StatusEffects.AilmentKind.None, ElementMask.Light, 1f, 5);
        Assert.That(statuses.AddShockStacks(effect, 12, 5, .75f), Is.EqualTo(2));
        var summary = statuses.GetStatusSummaries()[0];
        Assert.That(summary.Count, Is.EqualTo(2));
        Assert.That(summary.Threshold, Is.EqualTo(5));
        Assert.That(summary.Magnitude, Is.EqualTo(.75f).Within(.001f));
        for (int i = 0; i < 5; i++) statuses.TickStatuses();
        Assert.That(statuses.GetStatusSummaries(), Is.Empty);
    }

    [TestCase(1f, 100f, .06f)]
    [TestCase(10f, 100f, .15f)]
    [TestCase(25f, 100f, .30f)]
    [TestCase(100f, 100f, .30f)]
    public void ChillStrengthUsesActualColdHitRatio(float cold, float life, float expected)
    {
        Assert.That(BattleManager.CalculateChillSlow(cold, life, 0f), Is.EqualTo(expected).Within(.001f));
    }

    [Test]
    public void ChillReplacesOnlyWithEqualOrStrongerAndSlowsDynamically()
    {
        var target = NewGameObject("chill-target");
        target.AddComponent<StatsComponent>();
        var statuses = target.AddComponent<StatusController>();
        var effect = NewEffect("Chill", StatusEffects.StatusType.Chill,
            StatusEffects.AilmentKind.None, ElementMask.Cold, 1f, 4);
        Assert.That(statuses.ApplyChill(effect, .15f, 4), Is.True);
        statuses.TickStatuses();
        Assert.That(statuses.ApplyChill(effect, .10f, 4), Is.False);
        Assert.That(statuses.CurrentChillSlow, Is.EqualTo(.15f).Within(.001f));
        Assert.That(statuses.ApplyChill(effect, .15f, 4), Is.True, "Equal Chill refreshes duration");
        Assert.That(statuses.GetStatusSummaries()[0].MaxTurns, Is.EqualTo(4));
        Assert.That(statuses.ApplyChill(effect, .30f, 4), Is.True);
        Assert.That(statuses.CurrentChillSlow, Is.EqualTo(.30f).Within(.001f));
    }

    [Test]
    public void ActiveDatabaseHasSpecialSkillAndManaTiers()
    {
        var db = AssetDatabase.LoadAssetAtPath<ModDatabase>(
            "Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        Assert.That(db, Is.Not.Null);
        db.Initialize();
        foreach (StatTypes stat in new[] { StatTypes.Plus1Phys, StatTypes.Plus1Fire, StatTypes.Plus1Cold,
                     StatTypes.Plus1Light, StatTypes.Plus1Poison, StatTypes.Plus1Bleed, StatTypes.Plus1Ignite })
        {
            var def = db.GetDefinition(stat);
            Assert.That(def.tiers.Count, Is.EqualTo(1), stat.ToString());
            Assert.That(def.tiers[0].minValue, Is.EqualTo(1f));
            Assert.That(def.tiers[0].minItemLevel, Is.EqualTo(40));
            Assert.That(def.tiers[0].weight, Is.EqualTo(5));
        }
        Assert.That(db.GetDefinition(StatTypes.DmgPerMaxMana).tiers.Select(t => t.minValue),
            Is.EqualTo(new[] { 1f, 1.5f, 2f, 2.5f, 3f }));
        Assert.That(db.GetDefinition(StatTypes.DmgPerCurrentMana).tiers.Select(t => t.minValue),
            Is.EqualTo(new[] { 2f, 3f, 4f, 5f, 6f }));
    }

    StatusEffects NewEffect(string name, StatusEffects.StatusType type,
        StatusEffects.AilmentKind ailment, ElementMask elements, float magnitude, int duration)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffects>();
        effect.ConfigureRuntime(name, type, ailment, elements, magnitude, duration, 100,
            StatusEffects.StackPolicy.StackAndRefresh, 1);
        created.Add(effect);
        return effect;
    }

    Gear GearWith(StatTypes stat, float value, LootManager.GearType type)
    {
        var go = NewGameObject(stat.ToString());
        var gear = go.AddComponent<Gear>();
        gear.Initialize(type, LootManager.GearRarity.Magic, 80, Element.Phys);
        gear.ApplyMods(new List<RolledMod> { new(stat, 1, value) });
        return gear;
    }

    GameObject NewGameObject(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }
}
