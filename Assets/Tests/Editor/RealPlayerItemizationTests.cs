using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RealPlayerItemizationTests
{
    [Test]
    public void OrdinaryPoolsExcludeDeprecatedMoreAilmentPenetrationAndResistanceFamilies()
    {
        var forbidden=new HashSet<StatTypes>
        {
            StatTypes.GenericMult,StatTypes.PhysMult,StatTypes.FireMult,StatTypes.ColdMult,
            StatTypes.LightMult,StatTypes.VoidMult,StatTypes.PoisonMult,StatTypes.BleedMult,
            StatTypes.IgniteMult,StatTypes.PoisonPenetration,StatTypes.BleedPenetration,
            StatTypes.IgnitePenetration,StatTypes.PoisonRes,StatTypes.BleedRes,
            StatTypes.IgniteRes,StatTypes.ShockRes,StatTypes.ChillRes,StatTypes.AllAilmentRes
        };
        foreach(var pair in GearStatLists.BuildDefaultStatPools())
            foreach(var stat in pair.Value)
                Assert.That(forbidden.Contains(stat),Is.False,$"{pair.Key} generates deprecated {stat}");
    }

    [Test]
    public void SkillLevelAndPhysicalReductionUseStableNewIdsAndIntendedSlots()
    {
        var pools=GearStatLists.BuildDefaultStatPools();
        Assert.That((int)StatTypes.PlusAllSkills,Is.EqualTo(132));
        Assert.That((int)StatTypes.PhysicalDamageReduction,Is.EqualTo(129));
        foreach(var slot in new[]{LootManager.GearType.Weapons,LootManager.GearType.Amulets,LootManager.GearType.Helmets})
            Assert.That(pools[slot],Does.Contain(StatTypes.PlusAllSkills));
        foreach(var slot in new[]{LootManager.GearType.Helmets,LootManager.GearType.BodyArmours})
            Assert.That(pools[slot],Does.Contain(StatTypes.PhysicalDamageReduction));
        Assert.That(AffixPolicy.Side(StatTypes.PhysicalDamageReduction),Is.EqualTo(AffixSide.Prefix));
        Assert.That(AffixPolicy.Side(StatTypes.AllRes),Is.EqualTo(AffixSide.Suffix));
        Assert.That(AffixPolicy.Side(StatTypes.GenericDotMult),Is.EqualTo(AffixSide.Suffix));
    }

    [Test]
    public void WeaponElementRestrictsPoisonAndOtherTypedAffixes()
    {
        Assert.That(ModManager.IsWeaponAffixEligible(StatTypes.PoisonChance,Element.Phys),Is.False);
        Assert.That(ModManager.IsWeaponAffixEligible(StatTypes.PoisonChance,Element.Void),Is.True);
        Assert.That(ModManager.IsWeaponAffixEligible(StatTypes.BleedChance,Element.Phys),Is.True);
        Assert.That(ModManager.IsWeaponAffixEligible(StatTypes.FireDmg,Element.Cold),Is.False);
        Assert.That(ModManager.IsWeaponAffixEligible(StatTypes.GenericDotMult,Element.Cold),Is.True);
    }

    [Test]
    public void PhysicalReductionAddsAfterArmourAndPenetrationAppliesOnce()
    {
        // 1000 Armour against a 100 hit reduces 50%; +8% PDR and 10% penetration leave 48%.
        Assert.That(CombatCalculator.ApplyArmourValue(100f,1000f,.08f,.10f),Is.EqualTo(52f).Within(.001f));
        var source=new GameObject("source",typeof(StatsComponent));
        var target=new GameObject("target",typeof(StatsComponent));
        var bleed=ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            source.GetComponent<StatsComponent>().SetBaseStat(StatTypes.PhysPenetration,10f);
            var defense=target.GetComponent<StatsComponent>();
            defense.SetBaseStat(StatTypes.FlatArmour,1000f);
            defense.SetBaseStat(StatTypes.PhysicalDamageReduction,.08f);
            bleed.ConfigureRuntime("Bleed",StatusEffects.StatusType.DamageOverTime,
                StatusEffects.AilmentKind.Bleed,ElementMask.Phys,.1f,5,1,
                StatusEffects.StackPolicy.StackIndependently,2);
            Assert.That(CombatCalculator.CalculateAilmentTickDamage(100f,bleed,
                source.GetComponent<StatsComponent>(),defense),Is.EqualTo(52f).Within(.001f));
        }
        finally { Object.DestroyImmediate(source);Object.DestroyImmediate(target);Object.DestroyImmediate(bleed); }
    }

    [TestCase(.02f, 2f, 98f)]
    [TestCase(.1f, 10f, 90f)]
    public void FractionStoredPhysicalReductionMatchesDisplayedPercentAndCombat(float roll, float display, float remainingHit)
    {
        var target=new GameObject("target",typeof(StatsComponent));
        try
        {
            var stats=target.GetComponent<StatsComponent>();
            stats.SetBaseStat(StatTypes.PhysicalDamageReduction,roll);
            Assert.That(stats.GetStat(StatTypes.PhysicalDamageReduction),Is.EqualTo(roll).Within(.00001f));
            Assert.That(StatDisplayFormatting.FormatValue(stats,StatTypes.PhysicalDamageReduction),Is.EqualTo($"{display:0.##}%"));
            Assert.That(CombatCalculator.ApplyArmourValue(100f,0f,stats.GetStat(StatTypes.PhysicalDamageReduction),0f),Is.EqualTo(remainingHit).Within(.001f));
        }
        finally { Object.DestroyImmediate(target); }
    }

    [Test]
    public void AllElementalResistanceAndMaximumExcludeVoid()
    {
        var target=new GameObject("target",typeof(StatsComponent));
        var source=new GameObject("source",typeof(StatsComponent));
        try
        {
            var stats=target.GetComponent<StatsComponent>();
            stats.SetBaseStat(StatTypes.AllRes,20f);
            stats.SetBaseStat(StatTypes.MaxAllRes,10f);
            Assert.That(CombatCalculator.GetMaximumResistance(Element.Fire,stats),Is.EqualTo(.85f).Within(.001f));
            Assert.That(CombatCalculator.GetMaximumResistance(Element.Void,stats),Is.EqualTo(.75f).Within(.001f));
            var fire=new DamageContext(1);fire.AddDamage(Element.Fire,100);
            var vd=new DamageContext(1);vd.AddDamage(Element.Void,100);
            Assert.That(CombatCalculator.CalculateFinalDamage(fire,source.GetComponent<StatsComponent>(),stats),Is.EqualTo(80f).Within(.001f));
            Assert.That(CombatCalculator.CalculateFinalDamage(vd,source.GetComponent<StatsComponent>(),stats),Is.EqualTo(100f).Within(.001f));
        }
        finally { Object.DestroyImmediate(target);Object.DestroyImmediate(source); }
    }
}
