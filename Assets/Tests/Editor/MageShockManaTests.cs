using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BlackCube.CombatSimulation;

public sealed class MageShockManaTests
{
    readonly List<GameObject> objects=new();
    GameObject Actor(){var go=new GameObject("Mage rules test");go.SetActive(false);objects.Add(go);return go;}
    [TearDown] public void Cleanup(){foreach(var go in objects)Object.DestroyImmediate(go);objects.Clear();}
    [TestCase(0,.2f)] [TestCase(1,.4f)] [TestCase(2,.6f)] [TestCase(3,.8f)] [TestCase(4,1)]
    public void ShockEffectLadder(float increased,float expected)=>Assert.That(ShockRules.Effect(increased),Is.EqualTo(expected).Within(.00001));
    [Test] public void CapPrecedesPerStackReductionAndCanBeRaised()
    {Assert.That(ShockRules.Effect(9,1,.5f),Is.EqualTo(.5f));Assert.That(ShockRules.Effect(9,1.4f,.5f),Is.EqualTo(.7f));}
    [TestCase(.2f,2)] [TestCase(.4f,3)] [TestCase(.6f,4)] [TestCase(1,6)] [TestCase(1.5f,8)] [TestCase(3,16)]
    public void BarrageUsesSummedStrengthWithoutAnArbitraryCap(float sum,int hits)=>Assert.That(ShockRules.BarrageHits(sum),Is.EqualTo(hits));
    [TestCase(.2f,1.728f)] [TestCase(1,8)]
    public void ProductionShockMultipliesAndExpiresInSeconds(float effect,float multiplier)
    {
        var go=Actor();var stats=go.AddComponent<StatsComponent>();var status=go.AddComponent<StatusController>();
        for(int i=0;i<3;i++)status.AddShockInstance(effect,3,3);
        var hit=new DamageContext(1);hit.AddDamage(Element.Light,100);
        Assert.That(CombatCalculator.CalculateFinalDamage(hit,null,stats),Is.EqualTo(100*multiplier).Within(.001));
        status.TickStatuses();Assert.That(status.ShockCount,Is.EqualTo(3));
        status.TickRealtime(2.5f);Assert.That(status.ShockCount,Is.EqualTo(3));
        Assert.That(status.GetStatusSummaries().Count(s=>s.DisplayName=="Shock"),Is.EqualTo(3));
        status.TickRealtime(.5f);Assert.That(status.ShockCount,Is.Zero);
        Assert.That(ShockRules.Duration(.5f),Is.EqualTo(4.5f));
    }
    [Test] public void ConsumptionClearsMultiplierAndReapplicationCannotChangeSnapshot()
    {
        var go=Actor();var status=go.AddComponent<StatusController>();
        for(int i=0;i<3;i++)status.AddShockInstance(1,3,3);
        int hits=ShockRules.BarrageHits(status.ConsumeShocks());Assert.That(hits,Is.EqualTo(16));Assert.That(status.CombinedShockEffect,Is.Zero);
        status.AddShockInstance(.2f,3,3);Assert.That(hits,Is.EqualTo(16));
        int echoHits=ShockRules.BarrageHits(status.ConsumeShocks());Assert.That(echoHits,Is.EqualTo(2));
    }
    [TestCase(.35f,100,35,65)] [TestCase(.5f,20,20,80)] [TestCase(1,200,100,0)] [TestCase(1,30,30,70)] [TestCase(2,200,100,0)]
    public void ManaSplitConservesDamage(float fraction,float mana,float expectedMana,float expectedLife)
    {Assert.That(ManaBeforeLifeRules.LifeDamage(100,fraction,mana,out float absorbed),Is.EqualTo(expectedLife));Assert.That(absorbed,Is.EqualTo(expectedMana));}
    [Test] public void ReceiverSplitsAfterResistanceAndAilmentMitigation()
    {
        var go=Actor();var stats=go.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,200);stats.SetBaseStat(StatTypes.Mana,200);
        stats.SetBaseStat(StatTypes.FireRes,75);stats.SetBaseStat(StatTypes.DamageTakenFromManaBeforeLife,35);
        var hp=go.AddComponent<HealthComponent>();hp.ConfigureIsolatedStats(stats);var mana=go.AddComponent<ManaComponent>();mana.ConfigureIsolatedStats(stats);
        var receiver=go.AddComponent<DamageReceiver>();typeof(DamageReceiver).GetMethod("Awake",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(receiver,null);
        var hit=new DamageContext(1);hit.AddDamage(Element.Fire,400);
        receiver.TakeDamage(CombatCalculator.CalculateFinalDamage(hit,null,stats),hit);
        Assert.That(mana.CurrentMana,Is.EqualTo(165));Assert.That(hp.CurrentLife,Is.EqualTo(135));
        var effect=ScriptableObject.CreateInstance<StatusEffects>();
        try{effect.ConfigureRuntime("Poison",StatusEffects.StatusType.DamageOverTime,StatusEffects.AilmentKind.Poison,ElementMask.Void,.1f,2,20,StatusEffects.StackPolicy.StackIndependently);
            receiver.TakeDamage(CombatCalculator.CalculateAilmentTickDamage(50,effect,null,stats),Element.Void,effect);
            Assert.That(mana.CurrentMana,Is.EqualTo(147.5f));Assert.That(hp.CurrentLife,Is.EqualTo(102.5f));}
        finally{Object.DestroyImmediate(effect);}
    }
    [Test] public void TreeTotalsAndShockChoicesRemainIntentional()
    {
        var mage=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Mage).Tiers.SelectMany(t=>t.Left.GenericNodes.Concat(t.Right.GenericNodes)).ToArray();
        float Total(StatTypes stat)=>mage.SelectMany(n=>n.Effects).Where(e=>e.Stat==stat).Sum(e=>e.Value);
        Assert.That(Total(StatTypes.DamageTakenFromManaBeforeLife),Is.EqualTo(35));Assert.That(Total(StatTypes.LifeRegeneration),Is.EqualTo(3.75f));
        Assert.That(Total(StatTypes.ShockChance),Is.EqualTo(105));Assert.That(Total(StatTypes.LifeOnKill),Is.Zero);Assert.That(Total(StatTypes.LifeOnHit),Is.Zero);
        var staff=AssetDatabase.LoadAssetAtPath<PassiveWeaponBranchSO>("Assets/GameData/PassiveTree/Branches/Weapon/SO_Staff_Branch.asset");
        var effects=staff.Tiers.SelectMany(t=>t.Left.GenericNodes.Concat(t.Right.GenericNodes)).SelectMany(n=>n.Effects).ToArray();
        Assert.That(effects.Where(e=>e.Stat==StatTypes.DamageTakenFromManaBeforeLife).Sum(e=>e.Value),Is.EqualTo(65));
        Assert.That(effects.Where(e=>e.Stat==StatTypes.SpellEchoChance).Sum(e=>e.Value),Is.EqualTo(20));
        Assert.That(ProvisionalWeaponTreeContent.StaffManaDefense.Sum(),Is.EqualTo(65));Assert.That(ProvisionalWeaponTreeContent.StaffEcho.Sum(),Is.EqualTo(20));
    }
    [Test] public void StaffBarrageStillUsesAffordableCooldownAttackReplacement()
    {
        var skill=PlayerSkillDefinition.CreateProductionDefaults().Single(s=>s.id==PlayerSkillId.StaffShockBarrage);
        Assert.That(skill.castMode,Is.EqualTo(PlayerSkillCastMode.AutoQueuedReplacement));Assert.That(skill.baseCooldown,Is.EqualTo(5));
        Assert.That(skill.manaCost,Is.EqualTo(50));Assert.That(skill.maximumCount,Is.Zero);
    }
    [Test] public void SharedPopupFanSeparatesSixteenHits()
    {Assert.That(Enumerable.Range(0,16).Select(i=>DamagePopup.FanOffset(i)).Distinct().Count(),Is.EqualTo(16));}
    [Test] public void LaboratoryManaAbsorptionMatchesLifeAndTypedDamageTelemetry()
    {
        var player=new CombatantSnapshot{id="p",player=true,weaponTypeId=WeaponTypeIds.Staff,maximumLife=1000,maximumMana=1000,
            manaBeforeLife=1,attackSpeed=1,basicDamage=new CombatDamageSnapshot()};
        var enemy=new CombatantSnapshot{id="e",maximumLife=1000,attackSpeed=1,basicDamage=new CombatDamageSnapshot{fire=100}};
        var result=HeadlessCombatSimulator.Run(player,enemy,new CombatSimulationConfig{maximumDuration=2,actionPolicy=PlayerActionPolicy.BasicOnly});
        Assert.That(result.error,Is.Null.Or.Empty);Assert.That(result.manaAbsorbed,Is.GreaterThan(0));
        Assert.That(result.playerLife,Is.EqualTo(1000));Assert.That(result.playerMana,Is.EqualTo(1000-result.manaAbsorbed));
        Assert.That(result.damageByType.Where(x=>x.id.StartsWith("Enemy/")).Sum(x=>x.total),Is.Zero);
    }
    [TestCase(false,1600,0)] [TestCase(true,1900,1)]
    public void LiveBarrageConsumesBeforeDamageAndSnapshotsOnce(bool reapply,float expected,int rebuilt)
    {
        var player=Actor();var stats=player.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,1000);
        stats.SetBaseStat(StatTypes.UnarmedDamage,100);stats.SetBaseStat(StatTypes.ShockChance,reapply?100:0);
        var playerHp=player.AddComponent<HealthComponent>();playerHp.ConfigureIsolatedStats(stats);
        var controller=player.AddComponent<PlayerController>();controller.ConfigureItemPreview(1,null);
        var enemy=Actor();var defender=enemy.AddComponent<StatsComponent>();defender.SetBaseStat(StatTypes.Life,10000);
        var hp=enemy.AddComponent<HealthComponent>();hp.ConfigureIsolatedStats(defender);
        var status=enemy.AddComponent<StatusController>();var receiver=enemy.AddComponent<DamageReceiver>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(DamageReceiver).GetMethod("Awake",flags).Invoke(receiver,null);
        for(int i=0;i<3;i++)status.AddShockInstance(1,3,3);
        var battle=Actor().AddComponent<BattleManager>();
        void Bind(string name,object value)=>typeof(BattleManager).GetField(name,flags).SetValue(battle,value);
        Bind("player",player);Bind("playerStats",stats);Bind("playerHealth",playerHp);Bind("playerController",controller);
        Bind("currentEnemy",enemy);Bind("enemyStats",defender);Bind("enemyHealth",hp);Bind("enemyDamageReceiver",receiver);
        var effect=ScriptableObject.CreateInstance<StatusEffects>();
        try
        {
            effect.ConfigureRuntime("Shock",StatusEffects.StatusType.Shock,StatusEffects.AilmentKind.None,ElementMask.Light,.2f,3,1,StatusEffects.StackPolicy.ReplaceAlways);
            Bind("shockEffect",effect);
            var skill=new PlayerSkillDefinition{id=PlayerSkillId.StaffShockBarrage,effect=WeaponSkillEffect.ShockBarrage,
                hitDamageMultiplier=1,conversionElement=Element.Light,nonMatchingConversion=1,displayName="Test Barrage"};
            var resolve=typeof(BattleManager).GetMethod("ResolvePlayerSkill",flags);
            resolve.Invoke(battle,new object[]{skill,hp,status});
            Assert.That(10000-hp.CurrentLife,Is.EqualTo(expected).Within(.01));
            Assert.That(status.ShockCount,Is.EqualTo(rebuilt));
            float before=hp.CurrentLife;resolve.Invoke(battle,new object[]{skill,hp,status});
            // A new (e.g. echoed) cast sees only rebuilt Shocks: 1 hit or 2 hits, not the old 16.
            Assert.That(before-hp.CurrentLife,Is.EqualTo(reapply?220:100).Within(.01));
        }
        finally{Object.DestroyImmediate(effect);}
    }
}
