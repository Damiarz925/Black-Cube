using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Editor-only regression coverage for the phase-one modifier tier data and runtime mappings.
public class ModifierTierDataTests
{
    private const string DatabasePath = "Assets/Prefabs/Scriptable Objects/ModDatabase.asset";
    private static readonly int[] ExpectedLevels = { 1, 20, 40, 60, 75 };
    private static readonly int[] ExpectedWeights = { 50, 35, 20, 10, 5 };

    [Test]
    public void SuppliedRanges_AreExactAndComplete()
    {
        ModDatabase db = LoadDatabase();

        Expect(db, S(StatTypes.WeaponBaseDmg), "26,54;51,106;101,209;198,412;390,810");
        Expect(db, S(StatTypes.WeaponBaseAttackSpeed), ".45,.75;.60,.90;.75,1.05;.90,1.20;1.05,1.40");
        Expect(db, S(StatTypes.WeaponBaseCrit), "5,10;5,10;5,10;5,10;5,10");
        Expect(db, S(StatTypes.ChanceToBlock), "5,10;7,15;11,23;16,34;24,51");
        Expect(db, S(StatTypes.FlatPhys, StatTypes.FlatCold, StatTypes.FlatLight, StatTypes.FlatFire), "5,11;10,21;20,42;40,82;78,162");
        Expect(db, S(StatTypes.FlatVoid), "5,11;10,21;20,42;40,82;78,162");
        Expect(db, S(StatTypes.GenericDmg, StatTypes.CritChance, StatTypes.PhysDmg, StatTypes.ColdDmg, StatTypes.LightDmg, StatTypes.FireDmg, StatTypes.PoisonDmg, StatTypes.IgniteDmg, StatTypes.BleedDmg), "7,14;11,24;19,40;32,67;55,113");
        Expect(db, S(StatTypes.VoidDmg), "7,14;11,24;19,40;32,67;55,113");
        Expect(db, S(StatTypes.GenericMult), "1,3;2,4;3,6;5,10;7,15");
        Expect(db, S(StatTypes.PhysMult, StatTypes.ColdMult, StatTypes.LightMult, StatTypes.FireMult, StatTypes.PoisonMult, StatTypes.IgniteMult, StatTypes.BleedMult), "2,4;3,6;4,9;6,13;10,20");
        Expect(db, S(StatTypes.VoidMult), "2,4;3,6;4,9;6,13;10,20");
        Expect(db, S(StatTypes.GenericDotMult), "3,7;5,11;8,17;12,26;20,40");
        Expect(db, S(StatTypes.CritMult), "8,18;13,28;20,43;30,65;50,100");
        Expect(db, S(StatTypes.BaseCritChance), "1,1;1,2;1,2;2,3;2,4");
        Expect(db, S(StatTypes.PhysPenetration, StatTypes.LightPenetration, StatTypes.FirePenetration, StatTypes.PoisonPenetration, StatTypes.BleedPenetration), "2,4;3,6;5,9;8,13;12,19");
        Expect(db, S(StatTypes.VoidPenetration), "2,4;3,6;5,9;8,13;12,19");
        Expect(db, S(StatTypes.ColdPenetration), "2,3;3,5;4,7;6,10;10,15");
        Expect(db, S(StatTypes.IgnitePenetration), "3,5;4,8;7,12;10,17;16,25");
        Expect(db, S(StatTypes.PoisonChance, StatTypes.IgniteChance, StatTypes.BleedChance), "14,28;21,44;33,69;52,109;82,170");
        // Intentionally preserved legacy flat-turn ranges; these are not percentages.
        Expect(db, S(StatTypes.PoisonTickRate, StatTypes.IgniteTickRate, StatTypes.BleedTickRate), "1,1;1,2;1,3;2,5;3,7");
        Expect(db, S(StatTypes.PoisonDuration, StatTypes.IgniteDuration, StatTypes.BleedDuration), "1,1;1,1;1,1;1,2;1,2");
        Expect(db, S(StatTypes.ShockChance, StatTypes.ChillChance), "7,14;10,20;14,28;19,40;27,57");
        Expect(db, S(StatTypes.ShockEffect, StatTypes.ChillEffect), "3,7;5,10;6,13;9,19;13,27");
        Expect(db, S(StatTypes.ShockDuration, StatTypes.ChillDuration), "1,1;1,1;1,1;1,2;1,2");
        Expect(db, S(StatTypes.FlatArmour, StatTypes.FlatEvasion), "26,54;41,85;64,132;100,207;156,324");
        Expect(db, S(StatTypes.ArmourPercent, StatTypes.EvasionPercent), "8,17;11,24;16,34;23,48;32,67");
        Expect(db, S(StatTypes.ColdRes, StatTypes.LightRes, StatTypes.FireRes, StatTypes.PoisonRes, StatTypes.IgniteRes, StatTypes.BleedRes, StatTypes.ShockRes, StatTypes.ChillRes), "3,7;5,11;8,17;12,26;20,40");
        Expect(db, S(StatTypes.VoidRes), "3,7;5,11;8,17;12,26;20,40");
        Expect(db, S(StatTypes.AllRes), "2,4;3,6;4,8;6,11;8,16");
        Expect(db, S(StatTypes.MaxColdRes, StatTypes.MaxLightRes, StatTypes.MaxFireRes), "1,1;1,2;2,3;2,5;4,8");
        Expect(db, S(StatTypes.MaxVoidRes), "1,1;1,2;2,3;2,5;4,8");
        Expect(db, S(StatTypes.MaxAllRes), "1,1;1,2;1,3;2,4;3,5");
        Expect(db, S(StatTypes.AllAilmentRes), "2,5;3,7;5,9;6,13;9,19");
        Expect(db, S(StatTypes.Life), "120,250;237,492;466,967;917,1904;1804,3746");
        Expect(db, S(StatTypes.Mana), "58,122;115,239;227,471;446,926;878,1822");
        Expect(db, S(StatTypes.LifePercent), "3,7;5,11;8,17;12,26;20,40");
        Expect(db, S(StatTypes.ManaPercent), "4,8;6,13;9,19;14,29;21,44");
        Expect(db, S(StatTypes.LifeRegeneration, StatTypes.ManaRegeneration), "2,4;3,7;5,10;8,16;12,25");
        Expect(db, S(StatTypes.LifeOnHit), "8,17;13,26;20,41;31,64;48,100");
        Expect(db, S(StatTypes.ManaOnHit), "4,8;6,13;10,21;16,32;24,50");
        Expect(db, S(StatTypes.LifeOnKill), "15,31;24,49;38,79;59,123;93,193");
        Expect(db, S(StatTypes.ManaOnKill), "7,15;12,24;19,39;29,61;46,95");
        Expect(db, S(StatTypes.AttackSpeed), "2,5;4,8;6,12;9,18;13,27");
        Expect(db, S(StatTypes.Accuracy), "7,14;11,24;19,40;32,67;55,113");
        Expect(db, S(StatTypes.ChanceToHitTwice), "1,2;2,4;3,6;5,9;7,13");
        Expect(db, S(StatTypes.Strength, StatTypes.Intelligence, StatTypes.Dexterity), "3,6;6,12;11,22;20,40;36,72");
        Expect(db, S(StatTypes.StrengthPercent, StatTypes.IntelligencePercent, StatTypes.DexterityPercent), "2,4;3,6;5,9;7,13;10,19");
        Expect(db, S(StatTypes.LifePerStrength, StatTypes.ManaPerIntelligence), "1,2;2,3;3,5;4,7;6,10");
        Expect(db, S(StatTypes.DamagePerStrength), ".10,.20;.15,.30;.25,.45;.35,.65;.50,.90");
        Expect(db, S(StatTypes.DoTMultPerIntelligence), ".05,.10;.08,.15;.12,.22;.18,.32;.25,.45");
        Expect(db, S(StatTypes.AttackSpeedPerDexterity), ".03,.06;.05,.09;.07,.13;.10,.18;.15,.25");
        Expect(db, S(StatTypes.AccuracyPerDexterity), ".25,.50;.40,.80;.60,1.20;.90,1.80;1.30,2.70");
        Expect(db, S(StatTypes.FlatFirePerStrength, StatTypes.FlatLightPerIntelligence, StatTypes.FlatColdPerDexterity, StatTypes.DmgPerLowestStat), ".05,.10;.08,.16;.13,.25;.20,.40;.30,.60");
    }

    [Test]
    public void SuppliedStats_HaveNamesSlotsAndExpectedTierEligibility()
    {
        ModDatabase db = LoadDatabase();
        StatTypes[] supplied = SuppliedStats();
        Dictionary<LootManager.GearType, List<StatTypes>> pools = GearStatLists.BuildDefaultStatPools();

        foreach (StatTypes stat in supplied)
        {
            AffixDefinitions def = db.GetDefinition(stat);
            bool specialSkillTier = stat is >= StatTypes.Plus1Phys and <= StatTypes.Plus1Ignite;
            Assert.That(def, Is.Not.Null, stat.ToString());
            Assert.That(def.displayName, Is.Not.Empty, stat.ToString());
            Assert.That(def.tiers.Count, Is.EqualTo(specialSkillTier ? 1 : 5), stat.ToString());
            Assert.That(def.allowedSlots, Is.Not.Empty, stat.ToString());
            Assert.That(pools.Any(p => p.Value.Contains(stat)), Is.True, stat + " is not generation-eligible in any slot");
            Assert.That(AvailableTierCount(def, 1), Is.EqualTo(specialSkillTier ? 0 : 1), stat.ToString());
            Assert.That(AvailableTierCount(def, 19), Is.EqualTo(specialSkillTier ? 0 : 1), stat.ToString());
            Assert.That(AvailableTierCount(def, 20), Is.EqualTo(specialSkillTier ? 0 : 2), stat.ToString());
            Assert.That(AvailableTierCount(def, 40), Is.EqualTo(specialSkillTier ? 1 : 3), stat.ToString());
            Assert.That(AvailableTierCount(def, 60), Is.EqualTo(specialSkillTier ? 1 : 4), stat.ToString());
            Assert.That(AvailableTierCount(def, 75), Is.EqualTo(specialSkillTier ? 1 : 5), stat.ToString());
        }
    }

    [Test]
    public void TierGenerator_PopulatesCanonicalMetadata()
    {
        var def = new AffixDefinitions
        {
            tiers = new List<AffixTier> { new AffixTier { minValue = 10f, maxValue = 20f } }
        };

        def.EnsureTiersGenerated();

        Assert.That(def.tiers.Count, Is.EqualTo(5));
        for (int i = 0; i < 5; i++)
        {
            Assert.That(def.tiers[i].tierIndex, Is.EqualTo(i + 1));
            Assert.That(def.tiers[i].minItemLevel, Is.EqualTo(ExpectedLevels[i]));
            Assert.That(def.tiers[i].weight, Is.EqualTo(ExpectedWeights[i]));
            Assert.That(def.tiers[i].minValue, Is.LessThanOrEqualTo(def.tiers[i].maxValue));
        }
    }

    [Test]
    public void PercentUnitsAndRepresentativeRuntimeApplication_AreConsistent()
    {
        var attackerObject = new GameObject("modifier-tier-attacker");
        var defenderObject = new GameObject("modifier-tier-defender");
        var weaponObject = new GameObject("modifier-tier-weapon");
        try
        {
            StatsComponent attacker = attackerObject.AddComponent<StatsComponent>();
            StatsComponent defender = defenderObject.AddComponent<StatsComponent>();
            attacker.AddModifier(new StatModifier(StatTypes.FirePenetration, StatMappings.GetRolledModifierOperation(StatTypes.FirePenetration), 20f));
            attacker.AddModifier(new StatModifier(StatTypes.GenericMult, StatMappings.GetRolledModifierOperation(StatTypes.GenericMult), 15f));
            defender.AddModifier(new StatModifier(StatTypes.FireRes, StatOp.Additive, 50f));

            Assert.That(attacker.GetStat(StatTypes.FirePenetration), Is.EqualTo(.2f).Within(.0001f));
            Assert.That(attacker.GetStat(StatTypes.GenericMult), Is.EqualTo(.15f).Within(.0001f));
            Assert.That(StatDisplayFormatting.FormatValue(attacker, StatTypes.FirePenetration), Is.EqualTo("20%"));
            Assert.That(StatsComponent.IsPercentStat(StatTypes.PoisonTickRate), Is.False);

            var context = new DamageContext(4);
            context.AddDamage(Element.Fire, 100f);
            Assert.That(CombatCalculator.CalculateFinalDamage(context, attacker, defender), Is.EqualTo(70f).Within(.001f));

            Gear weapon = weaponObject.AddComponent<Gear>();
            weapon.Initialize(LootManager.GearType.Weapons, LootManager.GearRarity.Magic, 1, Element.Phys);
            weapon.ApplyMods(new List<RolledMod> { new RolledMod(StatTypes.WeaponBaseCrit, 1, 7.5f) });
            Assert.That(weapon.BaseCritChance, Is.EqualTo(.075f).Within(.0001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackerObject);
            UnityEngine.Object.DestroyImmediate(defenderObject);
            UnityEngine.Object.DestroyImmediate(weaponObject);
        }
    }

    [Test]
    public void RegenerationStats_UseFlatUnitsPerSecond()
    {
        var playerObject = new GameObject("life-regeneration-player");
        var enemyObject = new GameObject("life-regeneration-enemy");
        try
        {
            StatsComponent playerStats = playerObject.AddComponent<StatsComponent>();
            StatsComponent enemyStats = enemyObject.AddComponent<StatsComponent>();
            playerStats.AddModifier(new StatModifier(StatTypes.LifeRegeneration, StatOp.Additive, 3.34f));
            enemyStats.AddModifier(new StatModifier(StatTypes.LifeRegeneration, StatOp.Additive, 3.34f));

            Assert.That(playerStats.GetRawStat(StatTypes.LifeRegeneration), Is.EqualTo(3.34f).Within(.0001f));
            Assert.That(playerStats.GetStat(StatTypes.LifeRegeneration), Is.EqualTo(3.34f).Within(.0001f));
            Assert.That(enemyStats.GetStat(StatTypes.LifeRegeneration), Is.EqualTo(3.34f).Within(.0001f));
            Assert.That(StatsComponent.IsPercentStat(StatTypes.LifeRegeneration), Is.False);
            Assert.That(StatsComponent.IsPercentStat(StatTypes.ManaRegeneration), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void LegacyAilmentSpeedRanges_ProduceDefinedMultiTickIntervals()
    {
        var attackerObject = new GameObject("modifier-tier-ailment-attacker");
        try
        {
            StatsComponent attacker = attackerObject.AddComponent<StatsComponent>();
            AssertAilmentInterval(attacker, "PoisonStatus.asset", StatTypes.PoisonTickRate, Element.Phys, 7f, -5);
            AssertAilmentInterval(attacker, "IgniteStatus.asset", StatTypes.IgniteTickRate, Element.Fire, 7f, -5);
            AssertAilmentInterval(attacker, "BleedStatus.asset", StatTypes.BleedTickRate, Element.Phys, 7f, -5);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackerObject);
        }
    }

    [Test]
    public void PhysicalDamage_UsesArmourReductionMinusPhysicalPenetration()
    {
        var attackerObject = new GameObject("physical-rule-attacker");
        var defenderObject = new GameObject("physical-rule-defender");
        try
        {
            StatsComponent attacker = attackerObject.AddComponent<StatsComponent>();
            StatsComponent defender = defenderObject.AddComponent<StatsComponent>();
            defender.SetBaseStat(StatTypes.FlatArmour, 100f);

            const float hit = 100f;
            float armourReduction = 100f / (100f + 10f * hit);
            Assert.That(PhysicalDamage(hit, attacker, defender),
                Is.EqualTo(hit * (1f - armourReduction)).Within(.001f));

            attacker.SetBaseStat(StatTypes.PhysPenetration, 20f);
            Assert.That(PhysicalDamage(hit, attacker, defender),
                Is.EqualTo(hit * (1f - (armourReduction - .2f))).Within(.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackerObject);
            UnityEngine.Object.DestroyImmediate(defenderObject);
        }
    }

    [Test]
    public void PhysicalDamage_IgnoresFireResistanceAndSupportsZeroArmour()
    {
        var attackerObject = new GameObject("physical-rule-zero-attacker");
        var defenderObject = new GameObject("physical-rule-zero-defender");
        try
        {
            StatsComponent attacker = attackerObject.AddComponent<StatsComponent>();
            StatsComponent defender = defenderObject.AddComponent<StatsComponent>();
            defender.SetBaseStat(StatTypes.FireRes, 90f);

            Assert.That(PhysicalDamage(100f, attacker, defender), Is.EqualTo(100f).Within(.001f));

            attacker.SetBaseStat(StatTypes.PhysPenetration, 20f);
            Assert.That(PhysicalDamage(100f, attacker, defender), Is.EqualTo(120f).Within(.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackerObject);
            UnityEngine.Object.DestroyImmediate(defenderObject);
        }
    }

    private static ModDatabase LoadDatabase()
    {
        ModDatabase db = AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
        Assert.That(db, Is.Not.Null);
        db.Initialize();
        return db;
    }

    private static StatTypes[] S(params StatTypes[] stats) => stats;

    private static void Expect(ModDatabase db, IEnumerable<StatTypes> stats, string encodedRanges)
    {
        float[][] expected = encodedRanges.Split(';')
            .Select(pair => pair.Split(',').Select(value => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray())
            .ToArray();

        foreach (StatTypes stat in stats)
        {
            AffixDefinitions def = db.GetDefinition(stat);
            Assert.That(def, Is.Not.Null, stat.ToString());
            Assert.That(def.tiers.Count, Is.EqualTo(5), stat.ToString());
            for (int i = 0; i < 5; i++)
            {
                Assert.That(def.tiers[i].minValue, Is.EqualTo(expected[i][0]).Within(.0001f), stat + " T" + (i + 1) + " min");
                Assert.That(def.tiers[i].maxValue, Is.EqualTo(expected[i][1]).Within(.0001f), stat + " T" + (i + 1) + " max");
                Assert.That(def.tiers[i].tierIndex, Is.EqualTo(i + 1), stat.ToString());
                Assert.That(def.tiers[i].minItemLevel, Is.EqualTo(ExpectedLevels[i]), stat.ToString());
                Assert.That(def.tiers[i].weight, Is.EqualTo(ExpectedWeights[i]), stat.ToString());
            }
        }
    }

    private static int AvailableTierCount(AffixDefinitions def, int itemLevel)
    {
        return def.tiers.Count(t => t.minItemLevel <= itemLevel && t.weight > 0);
    }

    private static void AssertAilmentInterval(
        StatsComponent attacker,
        string assetName,
        StatTypes speedStat,
        Element sourceElement,
        float speed,
        int expectedInterval)
    {
        StatusEffects effect = AssetDatabase.LoadAssetAtPath<StatusEffects>(
            "Assets/Prefabs/Scriptable Objects/" + assetName);
        Assert.That(effect, Is.Not.Null);
        attacker.SetBaseStat(speedStat, speed);
        var context = new DamageContext(1);
        context.AddDamage(sourceElement, 100f);

        AilmentCalculator.ComputeAilmentFromHit(
            effect, context, attacker, out float damagePerTick, out int tickCount, out int effectiveInterval);

        Assert.That(damagePerTick, Is.GreaterThan(0f));
        Assert.That(tickCount, Is.GreaterThan(0));
        Assert.That(effectiveInterval, Is.EqualTo(expectedInterval));
        Assert.That(1 - effectiveInterval, Is.GreaterThan(0), "Non-positive intervals map to a positive ticks-per-turn count");
    }

    private static float PhysicalDamage(float amount, StatsComponent attacker, StatsComponent defender)
    {
        var context = new DamageContext(1);
        context.AddDamage(Element.Phys, amount);
        return CombatCalculator.CalculateFinalDamage(context, attacker, defender);
    }

    private static StatTypes[] SuppliedStats()
    {
        return GearStatLists.BuildDefaultStatPools().Values.SelectMany(values => values).Distinct().ToArray();
    }

    private static void AddRange(List<StatTypes> stats, int first, int last)
    {
        for (int id = first; id <= last; id++)
            stats.Add((StatTypes)id);
    }
}
