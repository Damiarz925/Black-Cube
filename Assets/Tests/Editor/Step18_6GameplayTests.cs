using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Step18_6GameplayTests
{
    readonly List<UnityEngine.Object> cleanup=new();
    [TearDown] public void TearDown(){LootRandomSourceFactory.ResetFactoryForTests();foreach(var value in cleanup.Where(x=>x!=null).Reverse())UnityEngine.Object.DestroyImmediate(value);cleanup.Clear();}

    [Test] public void ProductionLootEntropyDoesNotConsumeUnityWorldRandom()
    {
        UnityEngine.Random.InitState(18601);float expected=UnityEngine.Random.value;
        UnityEngine.Random.InitState(18601);var source=LootRandomSourceFactory.CreateProduction();
        for(int i=0;i<20;i++){source.Value();source.Range(0,10);source.Range(-2f,4f);}
        Assert.That(UnityEngine.Random.value,Is.EqualTo(expected));
        Assert.That(source.SourceName,Does.Contain("Cryptography"));
    }

    [Test] public void EquivalentKillEventsConsumeAdvancingInjectedStreamWithoutStageReset()
    {
        var source=Sequence(77);LootRandomSourceFactory.SetFactoryForTests(()=>source);
        var first=LootRandomSourceFactory.CreateProduction();float a=first.Value();int afterFirst=source.ValuesConsumed;
        GamePersistence.ResetStaticStateForTests();
        var second=LootRandomSourceFactory.CreateProduction();float b=second.Value();
        Assert.That(second,Is.SameAs(first));Assert.That(source.ValuesConsumed,Is.EqualTo(afterFirst+1));Assert.That(b,Is.Not.EqualTo(a));
    }

    [Test] public void MultipleNaturalItemsConsumeIndependentInjectedDraws()
    {
        CreateModManager();var loot=Track(new GameObject("loot",typeof(LootManager))).GetComponent<LootManager>();var source=Sequence(101);
        Gear first=loot.GenerateLoot(EnemyAI.EnemyRarity.Rare,source);int afterFirst=source.ValuesConsumed;
        Gear second=loot.GenerateLoot(EnemyAI.EnemyRarity.Rare,source);
        Assert.That(first,Is.Not.Null);Assert.That(second,Is.Not.Null);Assert.That(source.ValuesConsumed,Is.GreaterThan(afterFirst));
        Assert.That(Summary(second),Is.Not.EqualTo(Summary(first)),"The second item must continue the event stream rather than restart it.");
        cleanup.Add(first.gameObject);cleanup.Add(second.gameObject);
    }

    [Test] public void DistinctProductionDeathEventsRemainFreshAcrossPersistenceReset()
    {
        var first=new ProductionLootRandomSource();GamePersistence.ResetStaticStateForTests();var second=new ProductionLootRandomSource();
        Assert.That(second.EventId,Is.GreaterThan(first.EventId));
        Assert.That(typeof(GamePersistence).GetMethod("GenerateDeterministicLoot",BindingFlags.Public|BindingFlags.Static),Is.Null);
        Assert.That(typeof(GamePersistence).GetFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Any(x=>x.Name.Contains("lootSeed",StringComparison.OrdinalIgnoreCase)),Is.False);
    }

    [Test] public void EnemyDeathGuardClaimsOncePerInstanceButAllowsRetryInstance()
    {
        var first=Track(new GameObject("enemy A",typeof(HealthComponent))).GetComponent<HealthComponent>();
        var retry=Track(new GameObject("enemy B",typeof(HealthComponent))).GetComponent<HealthComponent>();
        first.SetEnemyRole(false);retry.SetEnemyRole(false);SetDead(first);SetDead(retry);
        Assert.That(first.TryClaimEnemyDeath(),Is.True);Assert.That(first.TryClaimEnemyDeath(),Is.False);
        Assert.That(retry.TryClaimEnemyDeath(),Is.True,"A replacement enemy instance must own a fresh reward claim.");
    }

    [TestCase(WeaponTypeIds.Sword,50f,50f,0f,.25f)]
    [TestCase(WeaponTypeIds.TwoHandedAxe,100f,0f,0f,.50f)]
    [TestCase(WeaponTypeIds.Bow,0f,100f,0f,.50f)]
    [TestCase(WeaponTypeIds.Staff,0f,0f,100f,.50f)]
    [TestCase(WeaponTypeIds.Dagger,0f,100f,100f,.50f)]
    [TestCase(WeaponTypeIds.Sceptre,100f,0f,100f,.50f)]
    public void WeaponAttributeProfilesUseOnlyThematicAttributes(string weapon,float strength,float dexterity,float intelligence,float expected)
    {
        StatsComponent stats=Stats(strength,dexterity,intelligence);
        Assert.That(WeaponAttributeScalingProfile.IncreasedDamage(weapon,stats),Is.EqualTo(expected).Within(.0001f));
        var definition=WeaponAttributeScalingProfile.Get(weapon);Assert.That(definition.PrimaryPerPoint+definition.SecondaryPerPoint,Is.EqualTo(.005f).Within(.0001f));
    }

    [TestCase(WeaponTypeIds.Sword,StatTypes.Intelligence)]
    [TestCase(WeaponTypeIds.TwoHandedAxe,StatTypes.Dexterity)]
    [TestCase(WeaponTypeIds.Bow,StatTypes.Strength)]
    [TestCase(WeaponTypeIds.Staff,StatTypes.Dexterity)]
    [TestCase(WeaponTypeIds.Dagger,StatTypes.Strength)]
    [TestCase(WeaponTypeIds.Sceptre,StatTypes.Dexterity)]
    public void IrrelevantAttributeAddsNoWeaponProfileDamage(string weapon,StatTypes irrelevant)
    {
        StatsComponent stats=Stats(0,0,0);stats.SetBaseStat(irrelevant,100);
        Assert.That(WeaponAttributeScalingProfile.IncreasedDamage(weapon,stats),Is.Zero);
    }

    [TestCase(1,0f)][TestCase(10,.0225f)][TestCase(20,.0475f)][TestCase(50,.1225f)][TestCase(100,.2475f)][TestCase(101,.2475f)]
    public void PlayerLevelDamageUsesCappedAdditiveFormula(int level,float expected)
        =>Assert.That(PlayerLevelDamageProfile.IncreasedDamage(level),Is.EqualTo(expected).Within(.0001f));

    [Test] public void RootHitReceivesLevelBonusOnceAndLocalWeaponDpsDoesNotChange()
    {
        var host=Track(new GameObject("player",typeof(StatsComponent),typeof(PlayerProgression),typeof(PlayerController)));
        var player=host.GetComponent<PlayerController>();var progression=host.GetComponent<PlayerProgression>();Gear weapon=Weapon(player,WeaponTypeIds.Sword,Element.Cold);
        float local=weapon.GetAverageWeaponDps();SetLevel(progression,10);float level10=Sum(player.BuildNonCriticalAttackContext());
        SetLevel(progression,20);float level20=Sum(player.BuildNonCriticalAttackContext());
        Assert.That(level20/level10,Is.EqualTo((1f+.0475f)/(1f+.0225f)).Within(.0001f));
        host.GetComponent<StatsComponent>().SetBaseStat(StatTypes.Strength,100);
        Assert.That(weapon.GetAverageWeaponDps(),Is.EqualTo(local).Within(.0001f));
        var defender=Track(new GameObject("defender",typeof(StatsComponent))).GetComponent<StatsComponent>();
        DamageContext snapshot=player.BuildNonCriticalAttackContext();
        Assert.That(CombatCalculator.CalculateFinalDamage(snapshot,host.GetComponent<StatsComponent>(),defender),Is.EqualTo(Sum(snapshot)).Within(.001f),"Downstream mitigation must not reapply root attribute/level scaling.");
    }

    [Test] public void ConvertedWeaponSkillRootUsesAttributeAndLevelScaling()
    {
        var host=Track(new GameObject("player",typeof(StatsComponent),typeof(PlayerProgression),typeof(PlayerController)));
        var player=host.GetComponent<PlayerController>();var stats=host.GetComponent<StatsComponent>();SetLevel(host.GetComponent<PlayerProgression>(),20);Weapon(player,WeaponTypeIds.Dagger,Element.Phys);
        float baseline=Sum(player.BuildNonCriticalConvertedAttackContext(Element.Void,1f));stats.SetBaseStat(StatTypes.Dexterity,100);stats.SetBaseStat(StatTypes.Intelligence,100);
        float scaled=Sum(player.BuildNonCriticalConvertedAttackContext(Element.Void,1f));
        Assert.That(player.WeaponAttributeDamageBonus,Is.EqualTo(.5f).Within(.0001f));Assert.That(scaled,Is.GreaterThan(baseline));
    }

    [TestCase(WeaponTypeIds.Sword,PlayerSkillId.SwordRapidFlurry)]
    [TestCase(WeaponTypeIds.TwoHandedAxe,PlayerSkillId.AxeHemorrhage)]
    [TestCase(WeaponTypeIds.Bow,PlayerSkillId.BowVenomShot)]
    [TestCase(WeaponTypeIds.Staff,PlayerSkillId.StaffFireball)]
    [TestCase(WeaponTypeIds.Sceptre,PlayerSkillId.SceptreRestorativeStrike)]
    [TestCase(WeaponTypeIds.Dagger,PlayerSkillId.DaggerBackstab)]
    public void ProductionSkillRootsUseTheirWeaponAttributeProfile(string weaponType,PlayerSkillId skillId)
    {
        Assert.That(WeaponSkillBindings.For(weaponType),Does.Contain(skillId));
        var host=Track(new GameObject("player",typeof(StatsComponent),typeof(PlayerProgression),typeof(PlayerController)));
        var player=host.GetComponent<PlayerController>();var stats=host.GetComponent<StatsComponent>();Weapon(player,weaponType,Element.Phys);
        float baseline=Sum(player.BuildNonCriticalAttackContext());var profile=WeaponAttributeScalingProfile.Get(weaponType);
        stats.SetBaseStat(profile.Primary,100);if(profile.IsDual)stats.SetBaseStat(profile.Secondary,100);
        Assert.That(Sum(player.BuildNonCriticalAttackContext()),Is.GreaterThan(baseline));
    }

    SequenceLootRandomSource Sequence(long id)=>new(id,Enumerable.Range(1,251).Select(i=>(i%97)/100f).ToArray());
    T Track<T>(T value) where T:UnityEngine.Object{cleanup.Add(value);return value;}
    StatsComponent Stats(float strength,float dexterity,float intelligence){var stats=Track(new GameObject("stats",typeof(StatsComponent))).GetComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Strength,strength);stats.SetBaseStat(StatTypes.Dexterity,dexterity);stats.SetBaseStat(StatTypes.Intelligence,intelligence);return stats;}
    void CreateModManager(){var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");Assert.That(database,Is.Not.Null);database.Initialize();var manager=Track(new GameObject("mods",typeof(ModManager))).GetComponent<ModManager>();manager.ConfigureForIsolatedRolling(database);typeof(ModManager).GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new object[]{manager});}
    Gear Weapon(PlayerController player,string type,Element element){var profile=WeaponTypeCatalog.Get(type);var gear=Track(new GameObject(type,typeof(Gear))).GetComponent<Gear>();gear.transform.SetParent(player.transform);gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,element,type);gear.BaseDamageMin=gear.BaseDamageMax=gear.BaseDamage=100;gear.BaseAttackSpeed=profile.AttacksPerSecond;gear.BaseCritChance=profile.BaseCritChance;player.EquipWeapon(gear);return gear;}
    static void SetLevel(PlayerProgression progression,int level)=>typeof(PlayerProgression).GetField("level",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progression,level);
    static void SetDead(HealthComponent health)=>typeof(HealthComponent).GetField("isDead",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(health,true);
    static float Sum(DamageContext context)=>context.Hits.Sum(x=>x.Amount);
    static string Summary(Gear gear)=>$"{gear.ItemType}|{gear.ItemRarity}|{gear.WeaponTypeId}|{gear.BaseElement}|"+string.Join(";",gear.rolledMods.Where(x=>x!=null).Select(x=>$"{x.statType}:{x.tierIndex}:{x.value:0.000}:{x.HighValue:0.000}"));
}
