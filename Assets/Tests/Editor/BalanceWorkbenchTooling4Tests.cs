using System.Linq;
using BlackCube.BalanceWorkbench;
using BlackCube.CombatSimulation;
using NUnit.Framework;
using UnityEngine;

public sealed class BalanceWorkbenchTooling4Tests
{
    static CombatantSnapshot Player(string weapon=WeaponTypeIds.Sword)=>new()
    {
        id="player",name="Player",player=true,weaponTypeId=weapon,maximumLife=1000,maximumMana=100,
        attackSpeed=1,critMultiplier=2,basicDamage=new CombatDamageSnapshot{physical=20},projectileTravelTime=.5f,
        precisionMultiplier=1.5f,maximumBleedStacks=5,maximumIgniteStacks=1,maximumShockInstances=1
    };

    static CombatantSnapshot Enemy()=>new()
    {
        id="enemy",name="Enemy",maximumLife=1000,attackSpeed=1,critMultiplier=2,
        basicDamage=new CombatDamageSnapshot{physical=10},maximumResistance=.75f
    };

    static CombatSimulationConfig Config(long seed=44001,float duration=8)=>new()
    {seed=seed,maximumDuration=duration,maximumEvents=10000,maximumSameTimestampEvents=128,retainTrace=true,traceLimit=10000};

    [Test] public void SameSeed_ReproducesCompleteLogicalTrace()
    {
        var a=HeadlessCombatSimulator.Run(Player(),Enemy(),Config());
        var b=HeadlessCombatSimulator.Run(Player(),Enemy(),Config());
        Assert.That(JsonUtility.ToJson(b),Is.EqualTo(JsonUtility.ToJson(a)));
        Assert.That(a.trace,Is.Not.Empty);
    }

    [Test] public void Scheduler_OrdersAttackGaugesAndDerivesUniqueFightSeeds()
    {
        var fast=Player();fast.attackSpeed=2;var result=HeadlessCombatSimulator.Run(fast,Enemy(),Config(duration:2.1f));
        Assert.That(result.playerAttacks,Is.EqualTo(4));
        Assert.That(result.enemyAttacks,Is.EqualTo(2));
        Assert.That(CombatDeterministicRules.DeriveSeed(7,0),Is.Not.EqualTo(CombatDeterministicRules.DeriveSeed(7,1)));
    }

    [Test] public void QueuedSkill_ConsumesManaAndReplacesBasicAttack()
    {
        var p=Player();p.skills.Add(new CombatSkillSnapshot{id="queued",name="Queued",castMode=PlayerSkillCastMode.QueuedAttackReplacement,manaCost=25,hitMultiplier=2});
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:2.1f));
        Assert.That(result.skills.Single().resolves,Is.EqualTo(2));
        Assert.That(result.manaSpent,Is.EqualTo(50).Within(.001));
        Assert.That(result.damage.Any(x=>x.id=="Player/Queued"),Is.True);
    }

    [Test] public void StaffAutoCooldown_RemainsReadyAndRetriesWhileManaStarved()
    {
        var p=Player(WeaponTypeIds.Staff);p.maximumMana=10;p.manaRegeneration=5;p.skills.Add(new CombatSkillSnapshot{id="staff",name="Staff Auto",castMode=PlayerSkillCastMode.AutoCooldown,manaCost=20,cooldown=.25f,hitMultiplier=2});
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:3));var metric=result.skills.Single();
        Assert.That(metric.manaFailures,Is.GreaterThan(0));
        Assert.That(metric.readyStarvedTime,Is.GreaterThan(0));
        Assert.That(metric.resolves,Is.LessThan(metric.activations));
    }

    [Test] public void DaggerImmediate_ObeysSimulationPolicy()
    {
        CombatantSnapshot Create(){var p=Player(WeaponTypeIds.Dagger);p.skills.Add(new CombatSkillSnapshot{id="quick",name="Quick",castMode=PlayerSkillCastMode.ImmediateCooldown,cooldown=.5f,manaCost=0});return p;}
        var basic=Config(duration:1.6f);basic.actionPolicy=PlayerActionPolicy.BasicOnly;var skills=Config(duration:1.6f);skills.actionPolicy=PlayerActionPolicy.SkillsWhenAvailable;
        Assert.That(HeadlessCombatSimulator.Run(Create(),Enemy(),basic).skills,Is.Empty);
        Assert.That(HeadlessCombatSimulator.Run(Create(),Enemy(),skills).skills.Single().resolves,Is.EqualTo(3));
    }

    [Test] public void BowProjectiles_StaggerTravelImpactPrecisionAndFizzle()
    {
        var p=Player(WeaponTypeIds.Bow);p.projectileCount=3;p.precisionChance=1;p.basicDamage.physical=1000;
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:3));
        Assert.That(result.projectilesLaunched,Is.EqualTo(3));
        Assert.That(result.projectilesImpacted,Is.EqualTo(1));
        Assert.That(result.projectilesFizzled,Is.EqualTo(2));
        Assert.That(result.precisionCount,Is.EqualTo(1));
        Assert.That(result.trace.Where(x=>x.type==CombatEventType.ProjectileLaunch).Select(x=>x.time),Is.Ordered);
    }

    [Test] public void DamagingAilments_UseScheduledTicksCapsAndMetrics()
    {
        var p=Player();p.basicDamage.fire=10;p.poisonChance=1;p.bleedChance=1;p.igniteChance=1;p.poisonMagnitude=40;p.bleedMagnitude=50;p.igniteMagnitude=20;
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:6.1f));
        Assert.That(result.ailments.Single(x=>x.id=="Poison").damage,Is.GreaterThan(0));
        Assert.That(result.ailments.Single(x=>x.id=="Bleed").maxStacks,Is.LessThanOrEqualTo(p.maximumBleedStacks));
        Assert.That(result.ailments.Single(x=>x.id=="Ignite").maxStacks,Is.LessThanOrEqualTo(1));
        Assert.That(result.trace.Any(x=>x.type==CombatEventType.AilmentTick),Is.True);
    }

    [Test] public void MultiShock_UsesMultiplicativeCombinedEffectAndExpires()
    {
        var p=Player();p.basicDamage.lightning=20;p.shockChance=2;p.maximumShockInstances=3;
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:7));var shock=result.ailments.Single(x=>x.id=="Shock");
        Assert.That(shock.applications,Is.GreaterThanOrEqualTo(2));
        Assert.That(shock.maximumEffect,Is.GreaterThan(1));
        Assert.That(shock.uptime,Is.GreaterThan(0));
    }

    [Test] public void FreezeSkipsEnemyAttackWithoutResettingBeforeSuccessfulAttack()
    {
        var p=Player();p.basicDamage.physical=0;p.basicDamage.cold=1;p.chillChance=1;p.chillEffect=10;p.skills.Add(new CombatSkillSnapshot{id="frost",name="Frost",castMode=PlayerSkillCastMode.QueuedAttackReplacement,effect=WeaponSkillEffect.FrostJudgment,hitMultiplier=1});
        var result=HeadlessCombatSimulator.Run(p,Enemy(),Config(duration:20.1f));
        Assert.That(result.enemyAttacksSkipped,Is.GreaterThan(0));
        Assert.That(result.ailments.Single(x=>x.id=="Freeze").applications,Is.GreaterThan(0));
    }

    [Test] public void RagePolicies_ProduceDifferentFinisherUseAndResetRage()
    {
        CombatantSnapshot Axe(){var p=Player(WeaponTypeIds.TwoHandedAxe);p.hasRageFinisher=true;p.basicDamage.physical=100;return p;}CombatantSnapshot DurableEnemy(){var e=Enemy();e.maximumLife=10000;e.attackSpeed=.1f;return e;}
        var never=Config(duration:30);never.ragePolicy=RageFinisherPolicy.Never;var immediate=Config(duration:30);immediate.ragePolicy=RageFinisherPolicy.Immediately;
        var a=HeadlessCombatSimulator.Run(Axe(),DurableEnemy(),never);var b=HeadlessCombatSimulator.Run(Axe(),DurableEnemy(),immediate);
        Assert.That(a.rageGenerated,Is.GreaterThan(0));
        Assert.That(a.rageFinisherUses,Is.EqualTo(0));
        Assert.That(b.rageFinisherUses,Is.GreaterThan(0));
        Assert.That(b.rageSpent,Is.GreaterThan(0));
    }

    [Test] public void EnemyBehaviorAndBossPhasesUseProductionResolvers()
    {
        var e=Enemy();e.enemySkills.Add(new CombatEnemySkillSnapshot{id="slam",name="Slam",damageMultiplier=2});e.behavior=new EnemyBehaviorProfileDefinition{stableId="test",preserveLegacyRotatingCadence=false,rules=new(){new EnemyActionRule{stableId="always",skillId="slam",condition=EnemyBehaviorCondition.Always,priority=1}}};e.boss=true;e.maximumLife=100;e.phases=new BossPhaseProfile{stableId="phases",phases=new(){new BossPhaseDefinition{beginsAtLifeFraction=1,mechanicId="opening"},new BossPhaseDefinition{beginsAtLifeFraction=.5f,mechanicId="desperation"}}};var p=Player();p.basicDamage.physical=60;
        var result=HeadlessCombatSimulator.Run(p,e,Config(duration:4));
        Assert.That(result.trace.Any(x=>x.type==CombatEventType.BehaviorDecision&&x.ruleId=="always"),Is.True);
        Assert.That(result.trace.Any(x=>x.type==CombatEventType.BossPhase&&x.phase=="desperation"),Is.True);
        Assert.That(result.phaseTime.Sum(x=>x.total),Is.EqualTo(result.duration).Within(.001));
    }

    [Test] public void TimeoutAndSafetyCapAreClassifiedSeparately()
    {
        var timeout=HeadlessCombatSimulator.Run(Player(),Enemy(),Config(duration:.2f));Assert.That(timeout.outcome,Is.EqualTo(CombatOutcome.Timeout));
        var cap=Config(duration:100);cap.maximumEvents=1;var error=HeadlessCombatSimulator.Run(Player(),Enemy(),cap);Assert.That(error.outcome,Is.EqualTo(CombatOutcome.SimulationError));Assert.That(error.error,Is.Not.Empty);
    }

    [Test] public void Tooling2PlayerSnapshotMatchesEvaluatorInitialStats()
    {
        var build=new PlayerBuildSnapshot{playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,weaponTypeId=WeaponTypeIds.Sword,equipment=new(){new GearSnapshot{slot=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Normal,itemLevel=50,element=Element.Phys,weaponTypeId=WeaponTypeIds.Sword,baseMin=20,baseMax=30,baseSpeed=1.2f,baseCrit=.05f}}};
        var expected=PlayerBuildEvaluator.Evaluate(build);var actual=CombatLabAdapters.PlayerSnapshot(build);
        Assert.That(actual.maximumLife,Is.EqualTo(expected.life).Within(.001));Assert.That(actual.maximumMana,Is.EqualTo(expected.mana).Within(.001));Assert.That(actual.attackSpeed,Is.EqualTo(expected.attacksPerSecond).Within(.001));Assert.That(actual.basicDamage.Total,Is.EqualTo(expected.averageHit).Within(.001));
    }

    [Test] public void Tooling3EnemySnapshotMatchesExactPreviewInitialStats()
    {
        WorldContentCatalog.Reload();var id=WorldContentCatalog.Reference.enemyArchetypes[0].stableId;var preview=EnemyAuthoringAdapters.PreviewEnemy(id,40,EnemyAI.EnemyRarity.Rare,20,44101);var actual=CombatLabAdapters.EnemySnapshot(preview,null);
        Assert.That(actual.maximumLife,Is.EqualTo(preview.sample.life).Within(.001));Assert.That(actual.attackSpeed,Is.EqualTo(preview.sample.attackSpeed).Within(.001));Assert.That(actual.basicDamage.Total,Is.EqualTo(preview.sample.damagePerHit).Within(.001));Assert.That(actual.behaviorProfileId,Is.EqualTo(preview.behaviorProfileId));
    }

    [Test] public void DistributionsExposeFullRequestedPercentiles()
    {
        var d=CombatDistribution.From(Enumerable.Range(1,100).Select(x=>(double)x));Assert.That(d.p01,Is.LessThan(d.p05));Assert.That(d.p10,Is.LessThan(d.p25));Assert.That(d.p50,Is.LessThan(d.p75));Assert.That(d.p90,Is.LessThan(d.p95));Assert.That(d.p99,Is.GreaterThan(d.p95));Assert.That(d.stdDev,Is.GreaterThan(0));
    }
}
