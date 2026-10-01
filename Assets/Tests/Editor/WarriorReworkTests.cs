using System.Linq;
using BlackCube.CombatSimulation;
using NUnit.Framework;
using UnityEditor;

public sealed class WarriorReworkTests
{
    [Test] public void WarriorTreeHasFixedGenericChoicesAndAlternatingSubclassChoices()
    {
        var branch=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>("Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset");
        Assert.That(branch,Is.Not.Null);
        Assert.That(branch.Tiers.Count,Is.EqualTo(10));
        for(int index=0;index<10;index++)
        {
            var tier=branch.Tiers[index];
            Assert.That(tier.Spine.Effects.Count,Is.EqualTo(2));
            Check(tier.Spine.Effects[0],StatTypes.Strength,5);
            Check(tier.Spine.Effects[1],StatTypes.Dexterity,5);
            Check(tier.Left.A,StatTypes.ArmourPercent,30);
            Check(tier.Left.B,StatTypes.LifeOnHit,10);
            Check(tier.Left.C,StatTypes.AllRes,4);
            Check(tier.Right.A,StatTypes.BleedChance,15);
            Check(tier.Right.B,StatTypes.ChanceToHitTwice,5);
            Check(tier.Right.C,StatTypes.AttackSpeed,8);
            bool odd=(index&1)==0;
            Check(tier.Left.SubclassA,odd?StatTypes.LifeOnHitVsBleeding:StatTypes.DeferredWounds,odd?25:8);
            Check(tier.Right.SubclassA,odd?StatTypes.WarriorDotMultiplier:StatTypes.RuptureDamage,5);
            Check(tier.Left.SubclassB,odd?StatTypes.ElementalPlating:StatTypes.ChanceToBlock,odd?5:8);
            Check(tier.Right.SubclassB,odd?StatTypes.EscalatingMultistrike:StatTypes.UnbrokenAssault,odd?5:10);
        }
    }

    static void Check(PassiveAuthoredNode node,StatTypes stat,float value)
    {
        Assert.That(node.Effects.Count,Is.EqualTo(1),node.StableId);
        Check(node.Effects[0],stat,value);
    }
    static void Check(PassiveAuthoredEffect effect,StatTypes stat,float value)
    {
        Assert.That(effect.Kind,Is.EqualTo(PassiveEffectKind.Stat));
        Assert.That(effect.Stat,Is.EqualTo(stat));
        Assert.That(effect.Value,Is.EqualTo(value));
    }

    [Test] public void BleedOverflowAndRuptureLockoutAreDeterministic()
    {
        Assert.That(WarriorSubclassRules.AcceptedBleedApplications(0,5,5),Is.EqualTo(5));
        Assert.That(WarriorSubclassRules.AcceptedBleedApplications(0,10,5),Is.EqualTo(5));
        Assert.That(WarriorSubclassRules.AcceptedBleedApplications(3,5,5),Is.EqualTo(2));
        Assert.That(WarriorSubclassRules.CanRupture(5,5,false),Is.True);
        Assert.That(WarriorSubclassRules.CanRupture(5,5,true),Is.False);
        Assert.That(WarriorSubclassRules.AcceptedBleedApplications(3,3,5),Is.EqualTo(2));
        Assert.That(WarriorSubclassRules.RuptureTotal(100, .25f),Is.EqualTo(125));
    }

    [Test] public void MomentumConversionAndMoreMultipliersDoNotCompound()
    {
        WarriorSubclassRules.MomentumConversion(60,20,out float speed,out float multistrike);
        Assert.That(speed,Is.EqualTo(100));
        Assert.That(multistrike,Is.EqualTo(50));
        Assert.That(WarriorSubclassRules.EscalatingMultiplier(0,.25f),Is.EqualTo(1));
        Assert.That(WarriorSubclassRules.EscalatingMultiplier(1,.25f),Is.EqualTo(1.25f));
        Assert.That(WarriorSubclassRules.EscalatingMultiplier(2,.25f),Is.EqualTo(1.5f));
        Assert.That(WarriorSubclassRules.EscalatingMultiplier(3,.25f),Is.EqualTo(1.75f));
        Assert.That(WarriorSubclassRules.AssaultMultiplier(4,.5f),Is.EqualTo(3));
    }

    [Test] public void SwordProductionSkillsAndLegacyMigration()
    {
        var defaults=PlayerSkillDefinition.CreateProductionDefaults();
        Assert.That(defaults.Any(x=>x.id==PlayerSkillId.SwordRapidFlurry),Is.False);
        var rending=defaults.Single(x=>x.id==PlayerSkillId.SwordRendingStrike);
        var armour=defaults.Single(x=>x.id==PlayerSkillId.SwordArmourStrike);
        Assert.That(rending.effect,Is.EqualTo(WeaponSkillEffect.RendingStrike));
        Assert.That(rending.hitDamageMultiplier,Is.EqualTo(1.2f));
        Assert.That(rending.baseCooldown,Is.EqualTo(4));
        Assert.That(armour.baseCooldown,Is.EqualTo(4));
        Assert.That(rending.queuedCooldownEnabled&&armour.queuedCooldownEnabled,Is.True);
        var legacy=new PlayerSkillDefinition{id=PlayerSkillId.SwordRapidFlurry};
        var upgraded=PlayerSkillDefinition.UpgradeProjectileDefinitions(new[]{legacy}).Single();
        Assert.That(upgraded.id,Is.EqualTo(PlayerSkillId.SwordRendingStrike));
    }

    [Test] public void BlockAndPlatingResolveBeforeElementalResistance()
    {
        float raw=1000,armour=5000,resistance=.75f,plating=.25f;
        float blocked=raw*WarriorSubclassRules.BlockedHitMultiplier;
        float plated=CombatCalculator.ApplyArmourValue(blocked,armour*plating,0,0);
        float final=CombatCalculator.ApplyResistanceValue(plated,resistance,0,CombatCalculator.BaseMaximumResistance);
        Assert.That(final,Is.LessThan(raw*.5f*.25f));
        Assert.That(CombatCalculator.ApplyResistanceValue(blocked,resistance,0,CombatCalculator.BaseMaximumResistance),Is.EqualTo(125).Within(.0001f));
    }

    [Test] public void CombatLabBlocksBeforeFirePlatingAndNeverPlatesVoid()
    {
        var player=new CombatantSnapshot{id="warrior",name="Momentum Warrior",player=true,subclassId=SubclassIds.WarriorMultihit,maximumLife=10000,maximumMana=0,attackSpeed=.0001f,armour=5000,elementalPlating=.25f,blockChance=1,fireResistance=.75f,voidResistance=.75f};
        var enemy=new CombatantSnapshot{id="caster",name="Caster",maximumLife=10000,attackSpeed=1,basicDamage=new CombatDamageSnapshot{fire=1000}};
        var config=new CombatSimulationConfig{maximumDuration=1.1f,seed=9,retainTrace=false};
        var fire=HeadlessCombatSimulator.Run(player,enemy,config);
        Assert.That(fire.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),fire.error);
        float expectedFire=CombatCalculator.ApplyResistanceValue(CombatCalculator.ApplyArmourValue(500,1250,0,0),.75f,0,CombatCalculator.BaseMaximumResistance);
        Assert.That(10000-fire.playerLife,Is.EqualTo(expectedFire).Within(.01f));
        enemy.basicDamage=new CombatDamageSnapshot{voidDamage=1000};
        var voidHit=HeadlessCombatSimulator.Run(player,enemy,config);
        Assert.That(voidHit.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),voidHit.error);
        Assert.That(10000-voidHit.playerLife,Is.EqualTo(125).Within(.01f));
    }

    [Test] public void RendingStrikeCanFillAndRuptureOnce()
    {
        var player=new CombatantSnapshot{id="warrior",name="Bleed Warrior",player=true,subclassId=SubclassIds.WarriorBleed,weaponTypeId=WeaponTypeIds.Sword,maximumLife=10000,maximumMana=1000,attackSpeed=1,bleedChance=3,bleedMagnitude=20,basicDamage=new CombatDamageSnapshot{physical=100}};
        player.skills.Add(new CombatSkillSnapshot{id="skill.sword.rending_strike",name="Rending Strike",castMode=PlayerSkillCastMode.QueuedAttackReplacement,effect=WeaponSkillEffect.RendingStrike,hitMultiplier=1.2f,manaCost=0,cooldown=4,queuedCooldownEnabled=true});
        var enemy=new CombatantSnapshot{id="dummy",name="Dummy",maximumLife=10000,attackSpeed=.0001f};
        var result=HeadlessCombatSimulator.Run(player,enemy,new CombatSimulationConfig{maximumDuration=1.1f,seed=11,retainTrace=false});
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),result.error);
        Assert.That(result.ruptureTriggers,Is.EqualTo(1));
        Assert.That(result.ailments.Single(x=>x.id=="Bleed").applications,Is.EqualTo(5));
    }
}
