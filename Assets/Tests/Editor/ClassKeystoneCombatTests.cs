using System.Linq;
using NUnit.Framework;
using UnityEngine;
using BlackCube.CombatSimulation;

public sealed class ClassKeystoneCombatTests
{
    GameObject root;
    PassiveKeystoneState Keys(PassiveKeystone key)
    {root=new GameObject("Keystone combat fixture");root.AddComponent<StatsComponent>();var state=root.AddComponent<PassiveKeystoneState>();state.ApplyAllocatedNodes(PassiveTreeDefinition.Nodes.Where(n=>n.Keystone==key));return state;}
    [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
    [Test] public void TempoIsMoreAndRecoverySuppressesRegeneration()
    {var k=Keys(PassiveKeystone.WarriorTempo);Assert.That(k.AttackSpeedMultiplier,Is.EqualTo(1.15f));k.ApplyAllocatedNodes(PassiveTreeDefinition.Nodes.Where(n=>n.Keystone==PassiveKeystone.BarbarianRecovery));root.GetComponent<StatsComponent>().SetBaseStat(StatTypes.LifeRegeneration,30);Assert.That(k.LifeRegenerationMultiplier,Is.Zero);Assert.That(k.DamageRecoveryFraction,Is.EqualTo(.15f).Within(.0001));}
    [Test] public void FullRagePayoffUsesOrdinaryCapAndContinuousDecayMath()
    {Assert.That(ClassKeystoneMechanics.FullRageMultiplier(99,0,true),Is.EqualTo(1));Assert.That(ClassKeystoneMechanics.FullRageMultiplier(100,0,true),Is.EqualTo(ClassKeystoneMechanics.FullRageMultiplier(100,0,false)*1.4f).Within(.0001));Assert.That(ClassKeystoneMechanics.FullRageMultiplier(100,1,true),Is.EqualTo(ClassKeystoneMechanics.FullRageMultiplier(100,1,false)*1.4f).Within(.0001));}
    [Test] public void EndlessPoisonUsesOnlyAdditionalDurationTicks()
    {Assert.That(ClassKeystoneMechanics.EndlessPoisonMultiplier(2),Is.EqualTo(.66f).Within(.0001));Assert.That(ClassKeystoneMechanics.EndlessPoisonMultiplier(-3),Is.EqualTo(.6f));}
    [Test] public void IgniteAndBleedCapsDoNotMultiplyApplications()
    {var effect=ScriptableObject.CreateInstance<StatusEffects>();try{effect.ConfigureRuntime("Ignite",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Ignite,ElementMask.Fire,1,2,1,StatusEffects.StackPolicy.StackIndependently);var k=Keys(PassiveKeystone.BarbarianFire);Assert.That(k.EffectiveAilmentStackCap(effect),Is.EqualTo(2));k.ApplyAllocatedNodes(System.Array.Empty<PassiveNodeDefinition>());Assert.That(k.EffectiveAilmentStackCap(effect),Is.EqualTo(1));}finally{Object.DestroyImmediate(effect);}}
    [Test] public void FractureSkipsOneAttackAndCannotBeShatteredOrRefrozen()
    {root=new GameObject("Fracture fixture");var status=root.AddComponent<StatusController>();Assert.That(status.ApplyKeystoneFreeze(1.99f,PassiveKeystone.PriestFracture),Is.False);Assert.That(status.ApplyKeystoneFreeze(2,PassiveKeystone.PriestFracture),Is.True);Assert.That(status.TryConsumeFreeze(out _),Is.False);Assert.That(status.ConsumeFrozenAttackSkip(),Is.True);Assert.That(status.IsFractured,Is.True);Assert.That(status.ConsumeFrozenAttackSkip(),Is.False);Assert.That(status.ApplyFreeze(3),Is.False);status.ClearStatuses();Assert.That(status.IsFractured,Is.False);}
    [Test] public void RegenerationUsesPercentagePointsWhilePhysicalReductionKeepsFraction()
    {Assert.That(StatsComponent.ToGameplayValue(StatTypes.LifeRegeneration,3),Is.EqualTo(.03f));Assert.That(StatsComponent.ToGameplayValue(StatTypes.PhysicalDamageReduction,.02f),Is.EqualTo(.02f));}
    [TestCase(StatTypes.ChanceToHitTwice,WeaponTypeIds.Sword)]
    [TestCase(StatTypes.ProjectileAmount,WeaponTypeIds.Bow)]
    [TestCase(StatTypes.CooldownReduction,WeaponTypeIds.Staff)]
    [TestCase(StatTypes.AuraEffect,WeaponTypeIds.Sceptre)]
    [TestCase(StatTypes.AxePhysicalRage,WeaponTypeIds.TwoHandedAxe)]
    [TestCase(StatTypes.CullingStrike,WeaponTypeIds.Dagger)]
    public void ExclusiveWeaponLaddersAreGatedAndNeverLeak(StatTypes stat,string weapon)
    {foreach(string id in PassiveTreeDefinition.WeaponIds){Assert.That(WeaponExclusiveAffixRules.TryGet(stat,LootManager.GearType.Weapons,id,out var tiers),Is.True);Assert.That(tiers.Count,Is.EqualTo(id==weapon?5:0));if(tiers.Count>0){Assert.That(tiers.Min(t=>t.minItemLevel),Is.EqualTo(50));Assert.That(tiers.Last().tierIndex,Is.EqualTo(1));}}Assert.That(AffixPolicy.Side(stat),Is.EqualTo(AffixSide.Suffix));}
    [TestCase(Element.Phys,115f)] [TestCase(Element.Fire,100f)]
    public void AxeLocalMoreAppliesOnlyToLocalPhysicalAndDoesNotAccumulate(Element element,float expected)
    {root=new GameObject("Axe fixture");var gear=root.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Rare,100,element,WeaponTypeIds.TwoHandedAxe);var mods=new System.Collections.Generic.List<RolledMod>{new(StatTypes.WeaponBaseDmg,1,100),new(StatTypes.AxePhysicalRage,1,15,30,false)};gear.ApplyMods(mods);Assert.That(gear.GetEffectiveBaseDamage(),Is.EqualTo(expected).Within(.001));Assert.That(gear.globalRolledMods.Single(m=>m.statType==StatTypes.RageGeneration).value,Is.EqualTo(30));gear.ApplyMods(mods);Assert.That(gear.GetEffectiveBaseDamage(),Is.EqualTo(expected).Within(.001));}
    static CombatSimulationResult Fight(PassiveKeystone key,string weapon=WeaponTypeIds.Sword)
    {var p=new CombatantSnapshot{id="player",player=true,weaponTypeId=weapon,maximumLife=1000,maximumMana=1000,attackSpeed=2,hitTwiceChance=1,precisionMultiplier=1.5f,basicDamage=new CombatDamageSnapshot{physical=10}};p.classKeystones.Add(key);return HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=100000,attackSpeed=.1f},new CombatSimulationConfig{maximumDuration=5,actionPolicy=PlayerActionPolicy.BasicOnly});}
    [Test] public void ConsolidationIsOneHitInsteadOfRepeatTriggers()
    {var result=Fight(PassiveKeystone.WarriorConsolidation);Assert.That(result.error,Is.Null.Or.Empty);Assert.That(result.hitTwiceCount,Is.Zero);Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Multistrikes converted").total,Is.GreaterThan(0));}
    [Test] public void SplitReplacesOriginalWithThreeChildren()
    {var ordinary=Fight(PassiveKeystone.None,WeaponTypeIds.Bow);var split=Fight(PassiveKeystone.RangerSplit,WeaponTypeIds.Bow);Assert.That(split.error,Is.Null.Or.Empty);Assert.That(split.projectilesLaunched,Is.EqualTo(ordinary.projectilesLaunched*3));Assert.That(split.hitTwiceCount,Is.Zero);}
    [Test] public void CullingIncludesBossesAndUsesMaximumLife()
    {Assert.That(ClassKeystoneMechanics.Cull(150,1000,.15f),Is.True);Assert.That(ClassKeystoneMechanics.Cull(151,1000,.15f),Is.False);Assert.That(ClassKeystoneMechanics.Cull(0,1000,.15f),Is.False);}
    [Test] public void LabDotUsesActualHitAndConsolidationDoesNotAddApplicationTriggers()
    {
        CombatSimulationResult Run(PassiveKeystone key)
        {
            var p=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Sword,maximumLife=1000,attackSpeed=1,hitTwiceChance=1,bleedChance=1,bleedHitFactors=new[]{1f,0,0,0,0},basicDamage=new CombatDamageSnapshot{physical=10}};
            p.classKeystones.Add(key);
            return HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=100000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=4,actionPolicy=PlayerActionPolicy.BasicOnly});
        }
        var ordinary=Run(PassiveKeystone.None);var consolidated=Run(PassiveKeystone.WarriorConsolidation);
        var a=ordinary.ailments.Single(x=>x.id=="Bleed");var b=consolidated.ailments.Single(x=>x.id=="Bleed");
        Assert.That(b.applications*2,Is.EqualTo(a.applications));
        Assert.That(b.magnitudeTotal/b.applications,Is.EqualTo(a.magnitudeTotal/a.applications*2.05f).Within(.001));
    }
    [Test] public void SelfBoltIsSpellTaggedAndIncomingBoltDoesNotGrantOffensiveRecovery()
    {
        var p=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Sword,maximumLife=1000,maximumMana=1000,attackSpeed=.01f,lightningResistance=.75f,lifeOnHit=10,shockChance=1,basicDamage=new CombatDamageSnapshot{physical=100}};
        p.classKeystones.Add(PassiveKeystone.MageSelfBolt);
        p.skills.Add(new CombatSkillSnapshot{id="test.spell",name="Test spell",magic=true,castMode=PlayerSkillCastMode.AutoCooldown,cooldown=1,manaCost=0,hitMultiplier=1});
        var result=HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=100000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=2.1f});
        Assert.That(result.error,Is.Null.Or.Empty);
        Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Incoming self-hit damage").total,Is.GreaterThan(0));
        Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Enemy bolt damage").total,Is.GreaterThan(0));
    }
    static CombatantSnapshot Actor(PassiveKeystone key,CombatDamageSnapshot hit)
    {var p=new CombatantSnapshot{id="player",player=true,weaponTypeId=WeaponTypeIds.Sword,maximumLife=1000,maximumMana=1000,attackSpeed=1,basicDamage=hit};p.classKeystones.Add(key);return p;}
    static CombatSimulationResult Run(CombatantSnapshot p,float duration=1.01f,float enemySpeed=.01f)
    =>HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=10000,attackSpeed=enemySpeed},new CombatSimulationConfig{maximumDuration=duration,actionPolicy=PlayerActionPolicy.BasicOnly});
    [Test] public void FireCommitmentDiscardsNonFireButMoltenEdgePreservesPhysicalBasis()
    {
        var fire=Run(Actor(PassiveKeystone.MageFire,new CombatDamageSnapshot{physical=100,fire=20,cold=40}));
        Assert.That(fire.damageByType.Single(x=>x.id=="Player/Fire").total,Is.EqualTo(30).Within(.001));
        Assert.That(fire.damageByType.Any(x=>x.id=="Player/Physical"||x.id=="Player/Cold"),Is.False);
        var p=Actor(PassiveKeystone.BarbarianFire,new CombatDamageSnapshot{physical=100});p.bleedChance=p.igniteChance=1;p.bleedHitFactors=p.igniteHitFactors=new[]{1f,0,0,0,0};
        var molten=Run(p);Assert.That(molten.damageByType.Single(x=>x.id=="Player/Fire").total,Is.EqualTo(100));Assert.That(molten.ailments.Single(x=>x.id=="Bleed").applications,Is.EqualTo(1));Assert.That(molten.ailments.Single(x=>x.id=="Ignite").applications,Is.EqualTo(1));
    }
    [Test] public void SacrificeConvertsEachDistinctAilmentOnceWithoutCreatingDots()
    {
        var p=Actor(PassiveKeystone.PriestSacrifice,new CombatDamageSnapshot{physical=10,fire=10,voidDamage=10});p.poisonChance=p.bleedChance=p.igniteChance=3;
        p.poisonHitFactors=new[]{0f,0,0,0,1};p.bleedHitFactors=new[]{1f,0,0,0,0};p.igniteHitFactors=new[]{0f,1,0,0,0};
        var result=Run(p);Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Distinct ailment conversions").total,Is.EqualTo(3));Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Sacrifice Max-Life damage").total,Is.EqualTo(1500));Assert.That(result.ailments,Is.Empty);
    }
    [Test] public void FractureIsPermanentAfterExactlyOneFrozenAttackOpportunity()
    {
        var p=Actor(PassiveKeystone.PriestFracture,new CombatDamageSnapshot{cold=100});p.chillChance=1;p.chillEffect=40;
        var result=Run(p,4.1f,.75f);Assert.That(result.enemyAttacksSkipped,Is.EqualTo(1));Assert.That(result.ailments.Single(x=>x.id=="Freeze").applications,Is.EqualTo(1));Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Fracture uptime seconds").total,Is.GreaterThan(0));
    }
    [Test] public void StormglassDetonatesExistingFreezeAndEmitsSeparateMaximumLifeColdBurst()
    {
        var p=Actor(PassiveKeystone.MageShatter,new CombatDamageSnapshot{cold=100,lightning=100});p.chillChance=1;p.chillEffect=20;
        var result=Run(p,2.01f);Assert.That(result.shatters,Is.EqualTo(1));Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Cold Max-Life Shatter damage").total,Is.EqualTo(1000).Within(.001));
    }
    [Test] public void OpenerAppliesDifferentMultipliersToFullAndInjuredTarget()
    {
        var result=Run(Actor(PassiveKeystone.ThiefOpener,new CombatDamageSnapshot{physical=100}),2.01f);
        Assert.That(result.damage.Single(x=>x.id=="Player/Basic Attack").total,Is.EqualTo(210).Within(.001));Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Full-health hits").total,Is.EqualTo(1));Assert.That(result.keystoneTelemetry.Single(x=>x.id=="Injured-target hits").total,Is.EqualTo(1));
    }
    [Test] public void BloodEngineRecoversFromSeparateShatterAsWellAsPrimaryHits()
    {
        var p=Actor(PassiveKeystone.MageShatter,new CombatDamageSnapshot{cold=100,lightning=100});
        p.classKeystones.Add(PassiveKeystone.BarbarianRecovery);p.wouldBeLifeRegenerationFraction=.1f;p.chillChance=1;p.chillEffect=20;
        var result=Run(p,2.01f);Assert.That(result.shatters,Is.EqualTo(1));
        float damage=(float)result.damage.Where(x=>x.id.StartsWith("Player/")).Sum(x=>x.total);
        Assert.That(result.healing.Single(x=>x.id=="Damage-based Recovery").total,Is.EqualTo(damage*.05f).Within(.001));
        Assert.That(result.damage.Single(x=>x.id=="Player/Keystone Shatter").total,Is.EqualTo(1200).Within(.001));
    }
    [Test] public void EmpoweredBleedUsesNormalBasisRatherThanReducedDirectHit()
    {
        var p=Actor(PassiveKeystone.None,new CombatDamageSnapshot{physical=100});p.bleedHitFactors=new[]{.1f,0,0,0,0};
        p.skills.Add(new CombatSkillSnapshot{id="test.hemorrhage",name="Hemorrhage",effect=WeaponSkillEffect.EmpoweredBleed,castMode=PlayerSkillCastMode.QueuedAttackReplacement,hitMultiplier=.6f,ailmentBasisMultiplier=3/.6f,specializedAilment=StatusEffects.AilmentKind.Bleed,guaranteedAilmentApplications=1});
        var result=HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=10000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=1.01f});
        Assert.That(result.ailments.Single(x=>x.id=="Bleed").magnitudeTotal,Is.EqualTo(30).Within(.001));
        Assert.That(result.damage.Single(x=>x.id=="Player/Hemorrhage").total,Is.EqualTo(60).Within(.001));
    }
    [Test] public void AutomaticCooldownSurveyPolicyCannotDisableStaffSkills()
    {
        var actor=new CombatantSnapshot();actor.skills.Add(new CombatSkillSnapshot{castMode=PlayerSkillCastMode.AutoCooldown});actor.skills.Add(new CombatSkillSnapshot{castMode=PlayerSkillCastMode.AutoCooldown});
        var request=new BlackCube.BalanceWorkbench.CombatLabRequest();request.config.actionPolicy=PlayerActionPolicy.BasicOnly;request.config.enableSkill1=request.config.enableSkill2=false;
        BlackCube.BalanceWorkbench.ClassKeystoneSurveyRunner.EnforceAutomaticCooldownPolicy(request,actor);
        Assert.That(request.config.actionPolicy,Is.EqualTo(PlayerActionPolicy.SkillsWhenAvailable));Assert.That(request.config.enableSkill1&&request.config.enableSkill2,Is.True);
    }
    [Test] public void VirtualPoisonUsesWouldBeFullHitDespiteZeroDirectDamageMultiplier()
    {
        var p=Actor(PassiveKeystone.None,new CombatDamageSnapshot{physical=100});p.weaponTypeId=WeaponTypeIds.Bow;
        p.poisonFullHitFactors=new[]{.1f,.1f,.1f,.1f,.1f};p.poisonHitFactors=new[]{0f,0,0,0,.1f};
        p.skills.Add(new CombatSkillSnapshot{id="test.venom",name="Venom Shot",effect=WeaponSkillEffect.VirtualPoison,castMode=PlayerSkillCastMode.QueuedAttackReplacement,hitMultiplier=0,ailmentBasisMultiplier=3,specializedAilment=StatusEffects.AilmentKind.Poison,guaranteedAilmentApplications=1,projectile=true,projectiles=1,baseProjectileSpeed=10});
        var result=HeadlessCombatSimulator.Run(p,new CombatantSnapshot{id="enemy",maximumLife=10000,attackSpeed=.01f},new CombatSimulationConfig{maximumDuration=1.2f});
        Assert.That(result.ailments.Single(x=>x.id=="Poison").applications,Is.EqualTo(1));Assert.That(result.ailments.Single(x=>x.id=="Poison").magnitudeTotal,Is.EqualTo(30).Within(.001));Assert.That(result.damageByType.Where(x=>x.id.StartsWith("Player/")).Sum(x=>x.total),Is.Zero);
    }
}
