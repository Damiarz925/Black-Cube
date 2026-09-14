using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemyBuildOptimizerTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < objects.Count; i++)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [TestCase(1, 1)]
    [TestCase(10, 1)]
    [TestCase(11, 2)]
    [TestCase(20, 2)]
    [TestCase(21, 3)]
    [TestCase(90, 9)]
    [TestCase(91, 10)]
    [TestCase(100, 10)]
    public void CandidateCount_UsesTenLevelBands(int level, int expected)
    {
        Assert.That(EnemyBuildOptimizer.CandidateCountForLevel(level), Is.EqualTo(expected));
    }

    [Test]
    public void OneCandidatePerSlot_HasNoOptimizationChoiceOrExtraRoll()
    {
        Gear weapon = Weapon("only-weapon", Element.Phys, 100f);
        Gear helmet = Item("only-helmet", LootManager.GearType.Helmets, StatTypes.Life, 50f);
        var slots = new List<EnemyBuildOptimizer.CandidateSlot>
        {
            Slot(LootManager.GearType.Weapons, weapon),
            Slot(LootManager.GearType.Helmets, helmet)
        };

        EnemyBuildOptimizer.BuildResult result = EnemyBuildOptimizer.SelectBestBuild(slots, BaseStats(), .5f);

        Assert.That(result.Items, Is.EqualTo(new[] { weapon, helmet }));
        Assert.That(result.CandidateIndices, Is.EqualTo(new[] { 0, 0 }));
        Assert.That(slots[0].Candidates.Count, Is.EqualTo(1));
        Assert.That(slots[1].Candidates.Count, Is.EqualTo(1));
    }

    [Test]
    public void LowerStandaloneWeapon_WinsWithMatchingLaterSlotSynergy()
    {
        Gear strongerAlone = Weapon("strong-physical", Element.Phys, 140f);
        Gear synergistic = Weapon("lower-fire", Element.Fire, 100f);
        Gear fireRing = Item("fire-ring", LootManager.GearType.Rings, StatTypes.FireDmg, 140f);

        float[] baseline = BaseStats();
        Assert.That(EnemyBuildOptimizer.Evaluate(new[] { strongerAlone }, baseline, .5f).Offense,
            Is.GreaterThan(EnemyBuildOptimizer.Evaluate(new[] { synergistic }, baseline, .5f).Offense));

        EnemyBuildOptimizer.BuildResult result = EnemyBuildOptimizer.SelectBestBuild(
            new[]
            {
                Slot(LootManager.GearType.Weapons, strongerAlone, synergistic),
                Slot(LootManager.GearType.Rings, fireRing)
            }, baseline, .5f);

        Assert.That(result.Items[0], Is.SameAs(synergistic));
    }

    [Test]
    public void ArchetypeQuota_PreservesInitiallyWeakerFirePathUntilLateSynergy()
    {
        List<Gear> weapons = new List<Gear>();
        for (int i = 0; i < 9; i++) weapons.Add(Weapon("physical-" + i, Element.Phys, 130f - i));
        Gear fire = Weapon("fire-path", Element.Fire, 60f);
        weapons.Add(fire);

        Gear[] middle = new Gear[5];
        for (int i = 0; i < middle.Length; i++)
            middle[i] = Item("middle-" + i, LootManager.GearType.Helmets, StatTypes.Life, 10f + i);
        Gear lateFire = Item("late-fire", LootManager.GearType.Amulets, StatTypes.FireDmg, 500f);

        EnemyBuildOptimizer.BuildResult result = EnemyBuildOptimizer.SelectBestBuild(
            new[]
            {
                new EnemyBuildOptimizer.CandidateSlot(LootManager.GearType.Weapons, weapons),
                new EnemyBuildOptimizer.CandidateSlot(LootManager.GearType.Helmets, middle),
                Slot(LootManager.GearType.Amulets, lateFire)
            }, BaseStats(), .5f);

        Assert.That(result.Items[0], Is.SameAs(fire));
    }

    [Test]
    public void EqualBuilds_UseStableCandidateOrderTieBreak()
    {
        Gear first = Weapon("first", Element.Phys, 100f);
        Gear second = Weapon("second", Element.Phys, 100f);
        var slots = new[] { Slot(LootManager.GearType.Weapons, first, second) };

        EnemyBuildOptimizer.BuildResult a = EnemyBuildOptimizer.SelectBestBuild(slots, BaseStats(), .5f);
        EnemyBuildOptimizer.BuildResult b = EnemyBuildOptimizer.SelectBestBuild(slots, BaseStats(), .5f);

        Assert.That(a.Items[0], Is.SameAs(first));
        Assert.That(b.CandidateIndices, Is.EqualTo(a.CandidateIndices));
    }

    [Test]
    public void Selection_DoesNotConsumeRandomNumbersOrGenerateCandidates()
    {
        Gear first = Weapon("seed-first", Element.Phys, 100f);
        Gear second = Weapon("seed-second", Element.Fire, 100f);
        var slots = new[] { Slot(LootManager.GearType.Weapons, first, second) };

        UnityEngine.Random.InitState(9182);
        float expectedNext = UnityEngine.Random.value;
        UnityEngine.Random.InitState(9182);
        EnemyBuildOptimizer.SelectBestBuild(slots, BaseStats(), .5f);
        float actualNext = UnityEngine.Random.value;

        Assert.That(actualNext, Is.EqualTo(expectedNext));
        Assert.That(slots[0].Candidates.Count, Is.EqualTo(2));
    }

    [Test]
    public void Ranking_IsSeventyFiveTwentyFiveLogNormalized()
    {
        EnemyBuildOptimizer.Evaluation evaluation = EnemyBuildOptimizer.Evaluate(
            new[] { Weapon("ranking", Element.Phys, 100f) }, BaseStats(), .5f);
        float expected = .75f * Mathf.Log(evaluation.Offense) + .25f * Mathf.Log(evaluation.Defense);
        Assert.That(evaluation.Score, Is.EqualTo(expected).Within(.0001f));
    }

    [Test]
    public void DormantRolls_ContributeNoInventedPower()
    {
        Gear weapon = Weapon("dead-roll-weapon", Element.Phys, 100f);
        Gear blank = Item("blank", LootManager.GearType.Rings);
        Gear dormant = Item("dormant", LootManager.GearType.Rings, StatTypes.ManaCost, 999f);
        float[] baseline = BaseStats();

        EnemyBuildOptimizer.Evaluation blankEvaluation = EnemyBuildOptimizer.Evaluate(new[] { weapon, blank }, baseline, .5f);
        EnemyBuildOptimizer.Evaluation dormantEvaluation = EnemyBuildOptimizer.Evaluate(new[] { weapon, dormant }, baseline, .5f);

        Assert.That(dormantEvaluation.Score, Is.EqualTo(blankEvaluation.Score).Within(.0001f));
        Assert.That(dormantEvaluation.Offense, Is.EqualTo(blankEvaluation.Offense).Within(.0001f));
        Assert.That(dormantEvaluation.Defense, Is.EqualTo(blankEvaluation.Defense).Within(.0001f));
    }

    [Test]
    public void ActualStatsAndIndependentMoreRolls_AffectEvaluation()
    {
        Gear weapon = Weapon("fire", Element.Fire, 100f);
        Gear genericMore = Item("generic-more", LootManager.GearType.Rings, StatTypes.GenericMult, 20f);
        Gear fireMore = Item("fire-more", LootManager.GearType.Amulets, StatTypes.FireMult, 20f);
        float[] baseline = BaseStats();

        float baseOffense = EnemyBuildOptimizer.Evaluate(new[] { weapon }, baseline, .5f).Offense;
        float scaledOffense = EnemyBuildOptimizer.Evaluate(new[] { weapon, genericMore, fireMore }, baseline, .5f).Offense;

        Assert.That(scaledOffense, Is.GreaterThan(baseOffense * 1.43f));
    }

    [Test]
    public void PhysicalPenetration_UsesSharedArmourCalculation()
    {
        Gear weapon = Weapon("physical-penetration", Element.Phys, 100f);
        Gear penetration = Item("penetration", LootManager.GearType.Rings, StatTypes.PhysPenetration, 20f);
        float[] baseline = BaseStats();

        float without = EnemyBuildOptimizer.Evaluate(new[] { weapon }, baseline, .5f).PhysicalHit;
        float with = EnemyBuildOptimizer.Evaluate(new[] { weapon, penetration }, baseline, .5f).PhysicalHit;

        Assert.That(with, Is.GreaterThan(without));
        Assert.That(CombatCalculator.ApplyArmourValue(100f, EnemyBuildOptimizer.ReferencePhysicalArmour, .2f),
            Is.EqualTo(110.909f).Within(.001f));
    }

    [Test]
    public void AilmentChanceAboveOneHundred_HasExpectedAdditionalStacks()
    {
        Gear weapon = Weapon("poison-source", Element.Phys, 100f);
        Gear oneHundred = Item("poison-100", LootManager.GearType.Rings, StatTypes.PoisonChance, 100f);
        Gear twoHundred = Item("poison-200", LootManager.GearType.Rings, StatTypes.PoisonChance, 200f);
        float[] baseline = BaseStats();

        float one = EnemyBuildOptimizer.Evaluate(new[] { weapon, oneHundred }, baseline, .5f).Poison;
        float two = EnemyBuildOptimizer.Evaluate(new[] { weapon, twoHundred }, baseline, .5f).Poison;

        Assert.That(two, Is.GreaterThan(one * 1.9f));
    }

    [Test]
    public void LifeRegeneration_UsesImplementedFlatPerSecondInDefenseWindow()
    {
        Gear weapon = Weapon("regen-weapon", Element.Phys, 100f);
        Gear regeneration = Item("regen", LootManager.GearType.Belts, StatTypes.LifeRegeneration, 3.34f);
        float[] baseline = BaseStats();

        float without = EnemyBuildOptimizer.Evaluate(new[] { weapon }, baseline, .5f).Defense;
        float with = EnemyBuildOptimizer.Evaluate(new[] { weapon, regeneration }, baseline, .5f).Defense;

        Assert.That(with - without, Is.EqualTo(33.4f).Within(.001f));
    }

    [Test]
    public void WinningResultContainsActualCandidateReferencesForEquipmentApplication()
    {
        Gear weak = Weapon("weak", Element.Phys, 50f);
        Gear strong = Weapon("strong", Element.Phys, 100f);
        Gear belt = Item("belt", LootManager.GearType.Belts, StatTypes.Life, 25f);

        EnemyBuildOptimizer.BuildResult result = EnemyBuildOptimizer.SelectBestBuild(
            new[] { Slot(LootManager.GearType.Weapons, weak, strong), Slot(LootManager.GearType.Belts, belt) },
            BaseStats(), .5f);

        Assert.That(result.Items[0], Is.SameAs(strong));
        Assert.That(result.Items[1], Is.SameAs(belt));
    }

    private EnemyBuildOptimizer.CandidateSlot Slot(LootManager.GearType type, params Gear[] candidates) =>
        new EnemyBuildOptimizer.CandidateSlot(type, candidates);

    private Gear Weapon(string name, Element element, float damage)
    {
        Gear gear = Item(name, LootManager.GearType.Weapons);
        gear.BaseElement = element;
        gear.BaseDamage = damage;
        gear.BaseAttackSpeed = 1f;
        gear.BaseCritChance = .05f;
        return gear;
    }

    private Gear Item(string name, LootManager.GearType type, StatTypes? stat = null, float value = 0f)
    {
        GameObject go = new GameObject(name);
        objects.Add(go);
        Gear gear = go.AddComponent<Gear>();
        UnityEngine.Random.State state = UnityEngine.Random.state;
        UnityEngine.Random.InitState(1234);
        gear.Initialize(type, LootManager.GearRarity.Normal, 1, Element.Phys);
        UnityEngine.Random.state = state;
        if (stat.HasValue) gear.ApplyMods(new List<RolledMod> { new RolledMod(stat.Value, 1, value) });
        return gear;
    }

    private static float[] BaseStats()
    {
        float[] stats = new float[(int)StatTypes.UnarmedDamage + 1];
        stats[(int)StatTypes.Life] = 100f;
        return stats;
    }
}
