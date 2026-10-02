using System.Linq;
using NUnit.Framework;
using UnityEditor;
using BlackCube.CombatSimulation;

public sealed class BarbarianRangerReworkTests
{
    static PassiveClassBranchSO Branch(string name)=>AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(
        $"Assets/GameData/PassiveTree/Branches/Class/SO_{name}_Branch.asset");
    static void AssertStat(PassiveAuthoredNode node,StatTypes stat,float amount)
    {
        Assert.That(node.Effects.Any(x=>x.Stat==stat&&System.Math.Abs(x.Value-amount)<.0001f),Is.True,node.StableId);
    }

    [Test] public void BarbarianHasUniformGenericChoicesAndAlternatingSubclassChoices()
    {
        var branch=Branch("Barbarian");Assert.NotNull(branch);Assert.That(branch.Tiers.Count,Is.EqualTo(10));
        foreach(var tier in branch.Tiers)
        {
            AssertStat(tier.Spine,StatTypes.Strength,10);
            AssertStat(tier.Left.A,StatTypes.LifeRegeneration,1.5f);
            AssertStat(tier.Left.B,StatTypes.DamageReductionPerRage,.02f);
            AssertStat(tier.Left.C,StatTypes.RageDecayReduction,5);
            AssertStat(tier.Right.A,StatTypes.PhysDmg,30);AssertStat(tier.Right.A,StatTypes.AttackSpeed,-15);
            AssertStat(tier.Right.B,StatTypes.RageGeneration,15);
            AssertStat(tier.Right.C,StatTypes.MaximumRage,5);
            if(tier.Tier%2==1)
            {
                AssertStat(tier.Left.SubclassA,StatTypes.TitanFortification,4);
                AssertStat(tier.Right.SubclassA,StatTypes.TitanRevengeBonus,5);
                AssertStat(tier.Left.SubclassB,StatTypes.DamageTakenAsFire,8);
                AssertStat(tier.Right.SubclassB,StatTypes.EruptionCoefficient,5);
            }
            else
            {
                AssertStat(tier.Left.SubclassA,StatTypes.TitanRageRegeneration,.06f);
                AssertStat(tier.Right.SubclassA,StatTypes.TitanFullLifeMore,7.5f);
                AssertStat(tier.Left.SubclassB,StatTypes.FireLifeLeech,2);
                AssertStat(tier.Right.SubclassB,StatTypes.PhysicalToFireConversion,12);
                AssertStat(tier.Right.SubclassB,StatTypes.FireDmg,30);
            }
        }
        Assert.That(branch.AllAuthoredNodes().Select(x=>x.StableId).Distinct().Count(),Is.EqualTo(branch.AllAuthoredNodes().Count()));
    }

    [Test] public void RangerHasUniformGenericChoicesAndAlternatingSubclassChoices()
    {
        var branch=Branch("Ranger");Assert.NotNull(branch);Assert.That(branch.Tiers.Count,Is.EqualTo(10));
        foreach(var tier in branch.Tiers)
        {
            AssertStat(tier.Spine,StatTypes.Dexterity,10);
            AssertStat(tier.Left.A,StatTypes.DodgeChance,3);
            AssertStat(tier.Left.B,StatTypes.LifeOnKill,50);
            AssertStat(tier.Left.C,StatTypes.ReducedShockEffect,4);AssertStat(tier.Left.C,StatTypes.ReducedChillEffect,4);
            AssertStat(tier.Right.A,StatTypes.ProjectilePrecisionChance,8);
            AssertStat(tier.Right.B,StatTypes.ProjectileSpeed,15);
            AssertStat(tier.Right.C,StatTypes.PoisonChance,30);
            if(tier.Tier%2==1)
            {
                AssertStat(tier.Left.SubclassA,StatTypes.PoisonLifeLeech,2);
                AssertStat(tier.Right.SubclassA,StatTypes.PoisonSpeed,10);
                AssertStat(tier.Left.SubclassB,StatTypes.ProjectileGuard,.1f);
                AssertStat(tier.Right.SubclassB,StatTypes.PrecisionMore,5);
            }
            else
            {
                AssertStat(tier.Left.SubclassA,StatTypes.ToxicSuppression,.5f);
                AssertStat(tier.Right.SubclassA,StatTypes.PoisonDuration,10);
                AssertStat(tier.Left.SubclassB,StatTypes.DodgeChance,2);
                AssertStat(tier.Left.SubclassB,StatTypes.DodgeLifeRecovery,2);
                AssertStat(tier.Right.SubclassB,StatTypes.ProjectileAmount,.4f);
            }
        }
        Assert.That(branch.AllAuthoredNodes().Select(x=>x.StableId).Distinct().Count(),Is.EqualTo(branch.AllAuthoredNodes().Count()));
    }

    [Test] public void RageUsesContinuousScaledDecayAndDynamicMaximum()
    {
        Assert.That(WeaponMechanicProfile.DecayRate(50,0,false),Is.EqualTo(5).Within(.0001));
        Assert.That(WeaponMechanicProfile.DecayRate(100,0,false),Is.EqualTo(10).Within(.0001));
        Assert.That(WeaponMechanicProfile.DecayRate(150,0,false),Is.EqualTo(15).Within(.0001));
        Assert.That(WeaponMechanicProfile.DecayRate(100,.5f,false),Is.EqualTo(5).Within(.0001));
        Assert.That(WeaponMechanicProfile.AdvanceRage(100,0,0,1,0,false,150),Is.LessThan(100));
        Assert.That(WeaponMechanicProfile.AdvanceRage(149,100,1,1,0,false,150),Is.EqualTo(150));
        Assert.That(WeaponMechanicProfile.AdvanceRage(100,6,10,10,0,false,150),Is.LessThan(100));
        Assert.That(WeaponMechanicProfile.AdvanceRage(100,9.6f,10,10,.2f,false,150),Is.GreaterThan(100));
    }

    [Test] public void FractionalProjectilesAndDodgeCapAreCanonical()
    {
        int extra=Enumerable.Range(0,1000).Count(i=>RangerSubclassRules.ProjectileCount(.4f,0,(i+.5f)/1000f)==2);
        Assert.That(extra,Is.EqualTo(400));
        Assert.That(RangerSubclassRules.ProjectileCount(2.4f,0,.39f),Is.EqualTo(4));
        Assert.That(RangerSubclassRules.ProjectileCount(2.4f,0,.41f),Is.EqualTo(3));
        Assert.That(RangerSubclassRules.DodgeChance(.9f),Is.EqualTo(RangerSubclassRules.DodgeCap));
    }

    [Test] public void TitanAndRangerDefensiveScalesMatchChosenNodeTotals()
    {
        float fortification=.04f*5;
        for(int prior=0;prior<=5;prior++)
            Assert.That(BarbarianRangerCombatRules.TitanFortification(prior,fortification),
                Is.EqualTo(System.Math.Max(.2f,1-prior*.2f)).Within(.0001));
        Assert.That(BarbarianRangerCombatRules.RageReductionMultiplier(100,.0002f*10),Is.EqualTo(.8f).Within(.0001));
        Assert.That(BarbarianRangerCombatRules.RageReductionMultiplier(150,.0002f*10),Is.EqualTo(.7f).Within(.0001));
        Assert.That(BarbarianRangerCombatRules.ToxicSuppression(100,.005f*5),Is.EqualTo(.75f).Within(.0001));
        Assert.That(BarbarianRangerCombatRules.ProjectileGuard(40,.001f*5),Is.EqualTo(.8f).Within(.0001));
        Assert.That(BarbarianRangerCombatRules.ProjectileSpeedMore(1),Is.EqualTo(1.5f).Within(.0001));
    }

    [Test] public void VolleyKeepsOneProjectileCountAndMigratesLegacyDefinition()
    {
        var skill=PlayerSkillDefinition.UpgradeProjectileDefinitions(PlayerSkillDefinition.CreateProductionDefaults())
            .Single(x=>x.id==PlayerSkillId.BowDoubleVolley);
        Assert.That(skill.displayName,Is.EqualTo("Volley"));
        Assert.That(skill.description,Does.Contain("guaranteed Critical and Precision"));
        Assert.That(skill.projectile,Is.True);
        Assert.That(skill.baseHitCount,Is.EqualTo(1));
    }

    [Test] public void ReworkedSubclassChoicesGrantNoAutomaticNumericPackage()
    {
        foreach(string subclass in new[]{SubclassIds.BarbarianBigHit,SubclassIds.BarbarianFire,SubclassIds.RangerPoison,SubclassIds.RangerProjectile})
        {
            int automatic=0;SubclassStatPackage.Apply(subclass,(_,__)=>automatic++);
            Assert.That(automatic,Is.Zero,subclass);
        }
    }

    [Test] public void VolleyGuaranteesBothHitQualitiesAndRewardsNaturalSuccessesSeparately()
    {
        CombatantSnapshot Player(float crit,float precision)
        {
            var player=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Bow,
                maximumLife=1000,maximumMana=100,attackSpeed=1,critChance=crit,critMultiplier=1.5f,
                precisionChance=precision,precisionMultiplier=1.5f,projectileCount=1,
                projectileTravelTime=.01f,basicDamage=new CombatDamageSnapshot{physical=20}};
            player.skills.Add(new CombatSkillSnapshot{id="volley",name="Volley",effect=WeaponSkillEffect.DoubleProjectiles,
                castMode=PlayerSkillCastMode.QueuedAttackReplacement,projectile=true,supportsPrecision=true,projectiles=1});
            return player;
        }
        var enemy=new CombatantSnapshot{id="enemy",maximumLife=100000,attackSpeed=.01f,
            basicDamage=new CombatDamageSnapshot{physical=1}};
        var config=new CombatSimulationConfig{seed=1729,maximumDuration=1.1f,retainTrace=false};
        var forced=HeadlessCombatSimulator.Run(Player(0,0),enemy,config);
        var natural=HeadlessCombatSimulator.Run(Player(1,1),enemy,config);
        Assert.That(forced.damage.Single(x=>x.id=="Player/Volley").total,Is.EqualTo(45).Within(.01));
        Assert.That(natural.damage.Single(x=>x.id=="Player/Volley").total,Is.EqualTo(45*1.21).Within(.01));
        Assert.That(forced.projectilesLaunched,Is.EqualTo(1));
        Assert.That(natural.projectilesLaunched,Is.EqualTo(1));
    }
}
