using System.Linq;
using BlackCube.BalanceWorkbench;
using BlackCube.CombatSimulation;
using NUnit.Framework;

public sealed class SubclassSurveyPreflightTests
{
    [Test] public void ProjectileSearchAndCombatUseProductionFloorForFractionalAdditionalAmounts()
    {
        var build=new PlayerBuildSnapshot{weaponTypeId=WeaponTypeIds.Bow};
        build.analysisDeltas.Add(new AnalysisStatDelta{stat=StatTypes.ProjectileAmount,amount=14.56f});
        Assert.That(PlayerBuildEvaluator.Evaluate(build).projectileCount,Is.EqualTo(15));
        var snapshot=CombatLabAdapters.PlayerSnapshot(build);
        Assert.That(snapshot.projectileCount,Is.EqualTo(15));
        Assert.That(snapshot.skills.Single(x=>x.effect==WeaponSkillEffect.DoubleProjectiles).projectiles,Is.EqualTo(30));
    }

    [TestCase(WeaponTypeIds.Sword)]
    [TestCase(WeaponTypeIds.TwoHandedAxe)]
    [TestCase(WeaponTypeIds.Bow)]
    [TestCase(WeaponTypeIds.Staff)]
    [TestCase(WeaponTypeIds.Sceptre)]
    [TestCase(WeaponTypeIds.Dagger)]
    public void ProductionAdapterLoadsBothWeaponSkillsDespiteLegacyCatalog(string weapon)
    {
        var snapshot=CombatLabAdapters.PlayerSnapshot(new PlayerBuildSnapshot
        {playerLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=weapon});
        Assert.That(snapshot.skills.Count,Is.EqualTo(2));
        Assert.That(snapshot.skills.All(x=>!string.IsNullOrEmpty(x.id)),Is.True);
        // No gear is needed to verify catalog loading; supply an attack-capable
        // combat fixture rather than the unarmed evaluator's zero attack speed.
        snapshot.attackSpeed=1;snapshot.maximumMana=1000;snapshot.maximumLife=10000;
        var config=Config(20);config.actionPolicy=PlayerActionPolicy.Skill2Priority;
        var result=HeadlessCombatSimulator.Run(snapshot,Enemy(),config);
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),result.error);
        Assert.That(result.skills.Count,Is.GreaterThan(0),"Production skill policy silently became basic-only.");
        Assert.That(result.manaSpent,Is.GreaterThan(0));
    }

    static CombatantSnapshot Player(string weapon=WeaponTypeIds.Bow) => new()
    {
        id="player",name="Player",player=true,weaponTypeId=weapon,
        maximumLife=10000,maximumMana=1000,attackSpeed=1,
        basicDamage=new CombatDamageSnapshot{physical=20},
        projectileCount=3,projectileTravelTime=.01f
    };

    static CombatantSnapshot Enemy() => new()
    {
        id="enemy",name="Enemy",maximumLife=100000,attackSpeed=.01f,
        basicDamage=new CombatDamageSnapshot{physical=1}
    };

    static CombatSimulationConfig Config(float duration=1.1f) => new()
    {
        seed=17001,maximumDuration=duration,maximumEvents=10000,
        actionPolicy=PlayerActionPolicy.SkillsWhenAvailable,retainTrace=false
    };

    [Test] public void DoubleVolleyUsesAlreadyExpandedFinalProjectileCountOnce()
    {
        var player=Player();
        player.attackSpeed=.5f;
        player.skills.Add(new CombatSkillSnapshot
        {
            id="double-volley",name="Double Volley",projectile=true,
            projectiles=6,effect=WeaponSkillEffect.DoubleProjectiles,
            castMode=PlayerSkillCastMode.QueuedAttackReplacement
        });
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(2.8f));
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),result.error);
        Assert.That(result.projectilesLaunched,Is.EqualTo(6));
        Assert.That(result.projectilesImpacted,Is.EqualTo(6));
    }

    [Test] public void ProjectileRangerCollapsesOnlyInFocusedMode()
    {
        var volley=Player();volley.subclassId=SubclassIds.RangerProjectile;
        var focused=Player();focused.subclassId=SubclassIds.RangerProjectile;
        focused.projectileMode=SubclassProjectileMode.Focused;
        var a=HeadlessCombatSimulator.Run(volley,Enemy(),Config());
        var b=HeadlessCombatSimulator.Run(focused,Enemy(),Config());
        Assert.That(a.projectilesLaunched,Is.EqualTo(3));
        Assert.That(b.projectilesLaunched,Is.EqualTo(1));
    }

    [Test] public void RapidFlurryHitCountUsesBonusAttackSpeedNotFinalAttacksPerSecond()
    {
        var player=Player(WeaponTypeIds.Sword);player.attackSpeed=2;player.attackSpeedBonus=0;
        player.skills.Add(new CombatSkillSnapshot
        {
            id="flurry",name="Rapid Flurry",effect=WeaponSkillEffect.RapidFlurry,
            castMode=PlayerSkillCastMode.QueuedAttackReplacement
        });
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(.51f));
        Assert.That(result.damage.Single(x=>x.id=="Player/Rapid Flurry").total,Is.EqualTo(60).Within(.01));
    }

    [Test] public void FireEruptionCannotRecursivelyTriggerItself()
    {
        var player=Player(WeaponTypeIds.TwoHandedAxe);player.subclassId=SubclassIds.BarbarianFire;
        player.attackSpeed=6;
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(8));
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),result.error);
        Assert.That(result.damage.Any(x=>x.id=="Player/Eruption"),Is.True);
        Assert.That(result.eruptionTriggers,Is.GreaterThan(0));
        Assert.That(result.eventCount,Is.LessThan(200));
    }

    [Test] public void ShockBarrageUsesOneToSixHitsAtTwentyPercentSteps()
    {
        Assert.That(CombatDeterministicRules.ShockBarrageHits(0),Is.EqualTo(1));
        for(int i=1;i<=5;i++)
            Assert.That(CombatDeterministicRules.ShockBarrageHits(i*.2f+.0001f),Is.EqualTo(i+1));
        Assert.That(CombatDeterministicRules.ShockBarrageHits(2),Is.EqualTo(11));
    }

    [Test] public void VenomShotHasVirtualPoisonButNoDirectHit()
    {
        var player=Player();player.subclassId=SubclassIds.RangerPoison;
        player.projectileCount=1;player.poisonMagnitude=80;
        player.skills.Add(new CombatSkillSnapshot
        {
            id="venom",name="Venom Shot",castMode=PlayerSkillCastMode.QueuedAttackReplacement,
            projectile=true,projectiles=1,effect=WeaponSkillEffect.VirtualPoison,
            specializedAilment=StatusEffects.AilmentKind.Poison,
            guaranteedAilmentApplications=1,ailmentBasisMultiplier=3
        });
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(5));
        Assert.That(result.damage.Single(x=>x.id=="Player/Venom Shot").total,Is.EqualTo(0));
        Assert.That(result.ailments.Single(x=>x.id=="Poison").damage,Is.GreaterThan(0));
    }

    [Test] public void DarkPriestConvertsNonRegenerationHealingToVoidDamage()
    {
        var player=Player(WeaponTypeIds.Sceptre);player.subclassId=SubclassIds.PriestDark;
        player.lifeOnHit=25;
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(2.1f));
        Assert.That(result.damage.Single(x=>x.id=="Player/Healing Converted to Void").total,Is.GreaterThan(0));
        Assert.That(result.healing.Any(x=>x.id=="Life on Hit"),Is.False);
    }

    [Test] public void AilmentAssassinCriticalPoisonIsRolledOncePerApplication()
    {
        var player=Player(WeaponTypeIds.Dagger);player.subclassId=SubclassIds.ThiefAilmentCrit;
        player.basicDamage.voidDamage=10;player.poisonChance=1;player.poisonMagnitude=100;
        player.critChance=1;player.critMultiplier=2;
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(5));
        var poison=result.ailments.Single(x=>x.id=="Poison");
        Assert.That(poison.criticalApplications,Is.EqualTo(poison.applications));
        Assert.That(poison.damage,Is.GreaterThan(0));
    }

    [Test] public void AssassinTakesOpeningActionBeforeEnemyGauge()
    {
        var player=Player(WeaponTypeIds.Dagger);player.subclassId=SubclassIds.ThiefAssassin;
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(.1f));
        Assert.That(result.playerAttacks,Is.EqualTo(1));
        Assert.That(result.enemyAttacks,Is.EqualTo(0));
        Assert.That(result.openingDamage,Is.GreaterThan(0));
    }

    [Test] public void PoisonDurationAndSpeedAffectCombatLabTicks()
    {
        CombatantSnapshot Poisoner(int extra)
        {
            var p=Player(WeaponTypeIds.Dagger);p.basicDamage.voidDamage=10;
            p.poisonChance=1;p.poisonMagnitude=40;p.poisonExtraTicks=extra;p.poisonSpeed=.25f;
            return p;
        }
        var config=Config(18);config.retainTrace=true;
        var baseRun=HeadlessCombatSimulator.Run(Poisoner(0),Enemy(),config);
        var extended=HeadlessCombatSimulator.Run(Poisoner(2),Enemy(),config);
        Assert.That(extended.ailments.Single(x=>x.id=="Poison").damage,
            Is.GreaterThan(baseRun.ailments.Single(x=>x.id=="Poison").damage));
        Assert.That(baseRun.trace.First(x=>x.type==CombatEventType.AilmentTick&&x.action=="Poison tick").time,
            Is.EqualTo(2.6f).Within(.02f));
    }

    [Test] public void BleedWarriorRuptureConsumesPendingBleedDamage()
    {
        var player=Player(WeaponTypeIds.Sword);player.subclassId=SubclassIds.WarriorBleed;
        player.bleedChance=2;player.bleedMagnitude=50;player.attackSpeed=5;
        var result=HeadlessCombatSimulator.Run(player,Enemy(),Config(12));
        Assert.That(result.ruptureTriggers,Is.GreaterThan(0));
        Assert.That(result.damage.Single(x=>x.id=="Player/Rupture").total,Is.GreaterThan(0));
    }

    [Test] public void TimeZeroAssassinWinUsesOneActionWindowForMeasuredDps()
    {
        var player=Player(WeaponTypeIds.Dagger);player.attackSpeed=2;
        Assert.That(CombatLabAdapters.MeasuredDpsDuration(new CombatSimulationResult{duration=0},player),
            Is.EqualTo(.5).Within(.0001));
        Assert.That(CombatLabAdapters.MeasuredDpsDuration(new CombatSimulationResult{duration=3},player),
            Is.EqualTo(3));
    }
}
