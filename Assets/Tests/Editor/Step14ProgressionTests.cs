using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public sealed class Step14ProgressionTests
{
    readonly List<GameObject> cleanup=new();
    UnityEngine.Random.State randomState;

    [SetUp] public void SetUp(){randomState=UnityEngine.Random.state;}
    [TearDown] public void TearDown(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)UnityEngine.Object.DestroyImmediate(cleanup[i]);cleanup.Clear();UnityEngine.Random.state=randomState;}

    [TestCase(1,1)] [TestCase(59,1)] [TestCase(60,1)] [TestCase(61,1)]
    [TestCase(210,50)] [TestCase(360,100)] [TestCase(999,100)]
    public void RelicLevelUsesTheLockedZoneFormula(int zone,int expected)
        =>Assert.That(RelicInventory.LevelForZone(zone),Is.EqualTo(expected));

    [Test] public void RelicRollsOnlyUseLevelEligibleTiersAndT1IsStrongest()
    {
        foreach(var definition in RelicModifierDefinitions.All)
        {
            for(int i=1;i<definition.Tiers.Count;i++)
                Assert.That(definition.Tiers[i].Maximum,Is.GreaterThanOrEqualTo(definition.Tiers[i-1].Maximum),definition.Id.ToString());
            foreach(int level in new[]{1,20,40,60,80,100})for(int sample=0;sample<25;sample++)
            {
                var rolled=RelicRolls.Roll(level,false);
                var tier=RelicModifierDefinitions.Get(rolled.type).Tier(rolled.tierIndex);
                Assert.That(tier,Is.Not.Null);Assert.That(tier.MinimumRelicLevel,Is.LessThanOrEqualTo(level));
            }
        }
    }

    [Test] public void CentralTierPolicyImprovesMeanQualityAcrossSmallDeterministicSamples()
    {
        var all=new[]{Tier(5,1,50),Tier(4,20,35),Tier(3,40,20),Tier(2,60,10),Tier(1,75,5)};
        var random=new System.Random(14014);double previous=-1;
        foreach(int level in new[]{1,25,50,75,100})
        {
            var eligible=all.Where(t=>t.minItemLevel<=level).ToList();double sum=0;
            for(int i=0;i<400;i++)sum+=6-AffixTierWeightPolicy.Choose(eligible,t=>t.tierIndex,t=>t.weight,level,(float)random.NextDouble()).tierIndex;
            double mean=sum/400d;Assert.That(mean,Is.GreaterThan(previous),$"ilvl {level} mean quality");previous=mean;
        }
    }

    [Test] public void RelicCardLeftClickEquipsUnequipsAndDoesNotReplaceFullSlots()
    {
        var inventory=Track(new GameObject("relic inventory")).AddComponent<RelicInventory>();SetRelicInstance(inventory);
        var relics=new List<RelicData>();for(int i=0;i<5;i++)relics.Add(inventory.BeginNewCycle(60));
        var card=Track(new GameObject("relic card",typeof(RectTransform),typeof(RelicSlotUI))).GetComponent<RelicSlotUI>();card.Initialize(relics[0]);
        var eventSystem=Track(new GameObject("events",typeof(EventSystem))).GetComponent<EventSystem>();var click=new PointerEventData(eventSystem){button=PointerEventData.InputButton.Left};
        card.OnPointerClick(click);Assert.That(inventory.Active(0),Is.SameAs(relics[0]));
        card.OnPointerClick(click);Assert.That(inventory.Active(0),Is.Null);
        for(int i=0;i<4;i++)Assert.That(inventory.Equip(relics[i],i),Is.True);
        card.Initialize(relics[4]);LogAssert.Expect(LogType.Warning,"Relic slots full");card.OnPointerClick(click);
        Assert.That(card.LastFeedback,Is.EqualTo("Relic slots full"));Assert.That(inventory.Active(0),Is.SameAs(relics[0]));
    }

    [Test] public void StarterElementUsesStrongestTierThenActiveSlotOrderAndBonusesStackAsDocumented()
    {
        var inventory=Track(new GameObject("relic inventory")).AddComponent<RelicInventory>();SetRelicInstance(inventory);
        var fire=Relic("fire",RelicModifierType.StarterWeaponFire,1,5);
        var cold=Relic("cold",RelicModifierType.StarterWeaponCold,1,5);
        fire.modifiers.Add(new RelicModifier(RelicModifierType.StarterWeaponBaseDamage,10,false,5));
        cold.modifiers.Add(new RelicModifier(RelicModifierType.StarterWeaponItemLevel,20,false,3));
        cold.modifiers.Add(new RelicModifier(RelicModifierType.StarterWeaponLegendaryChance,30,false,1));
        inventory.Restore(new List<RelicData>{fire,cold},1,new[]{0,1,-1,-1});
        Assert.That(inventory.StarterElement,Is.EqualTo(Element.Fire));
        Assert.That(inventory.StarterBaseDamagePercent,Is.EqualTo(10));Assert.That(inventory.StarterItemLevelBonus,Is.EqualTo(20));Assert.That(inventory.StarterLegendaryChance,Is.EqualTo(30));
        cold.modifiers[0].tierIndex=4;Assert.That(inventory.StarterElement,Is.EqualTo(Element.Cold));
    }

    [Test] public void GuaranteedLegendaryStarterUsesLegalProductionAffixesOnceAndRoundTripsGeneratedState()
    {
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");Assert.That(database,Is.Not.Null);
        var roller=Track(new GameObject("roller")).AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(database);
        typeof(ModManager).GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new object[]{roller});
        var inventory=Track(new GameObject("relic inventory")).AddComponent<RelicInventory>();SetRelicInstance(inventory);
        var legendary=Relic("legendary",RelicModifierType.StarterWeaponLegendaryChance,100,1);inventory.Restore(new List<RelicData>{legendary},1,new[]{0,-1,-1,-1});
        GamePersistence.BeginFreshRunIdentity();var controller=Track(new GameObject("player")).AddComponent<PlayerController>();
        var starter=controller.EnsureStarterWeapon();Assert.That(starter.ItemRarity,Is.EqualTo(LootManager.GearRarity.Legendary));Assert.That(controller.EnsureStarterWeapon(),Is.SameAs(starter));
        var nonbase=starter.rolledMods.Where(m=>!Gear.IsWeaponBaseStat(m.statType)).ToArray();Assert.That(nonbase.Count(m=>m.lockedOriginal),Is.EqualTo(1));Assert.That(nonbase.Count(m=>!m.lockedOriginal),Is.EqualTo(6));
        Assert.That(nonbase.Count(m=>!m.lockedOriginal&&AffixPolicy.Side(m.statType)==AffixSide.Prefix),Is.EqualTo(3));Assert.That(nonbase.Count(m=>!m.lockedOriginal&&AffixPolicy.Side(m.statType)==AffixSide.Suffix),Is.EqualTo(3));
        var errors=new List<string>();ItemizationValidator.ValidateRolledMods(starter.ItemType,starter.ItemRarity,starter.ItemLevel,starter.rolledMods,database,errors,true);Assert.That(errors,Is.Empty,string.Join("\n",errors));
        var snapshot=GearSnapshotData.Capture(starter);var restored=JsonUtility.FromJson<GearSnapshotData>(JsonUtility.ToJson(snapshot));Assert.That(restored.rarity,Is.EqualTo(LootManager.GearRarity.Legendary));Assert.That(restored.mods.Count,Is.EqualTo(starter.rolledMods.Count));
    }

    [Test] public void ConfirmedZoneSixtyRebirthCreatesOneRelicAndProvisionsOneStarterBeforeCombat()
    {
        SetStatic(typeof(GameManager),"Instance",null);SetStatic(typeof(RelicInventory),"Instance",null);SetStatic(typeof(RebirthManager),"Instance",null);SetStatic(typeof(CurrencyInventory),"Instance",null);SetStatic(typeof(ModManager),"Instance",null);
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");var roller=Track(new GameObject("roller")).AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(database);SetStatic(typeof(ModManager),"Instance",roller);
        var currency=Track(new GameObject("currency")).AddComponent<CurrencyInventory>();SetStatic(typeof(CurrencyInventory),"Instance",currency);
        var manager=Track(new GameObject("manager")).AddComponent<GameManager>();SetStatic(typeof(GameManager),"Instance",manager);var relics=manager.GetComponent<RelicInventory>()??manager.gameObject.AddComponent<RelicInventory>();SetRelicInstance(relics);var rebirth=manager.GetComponent<RebirthManager>()??manager.gameObject.AddComponent<RebirthManager>();SetStatic(typeof(RebirthManager),"Instance",rebirth);
        typeof(GameManager).GetField("currentZoneLevel",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,60);
        var player=Track(new GameObject("player")).AddComponent<PlayerController>();Assert.That(rebirth.RequestRebirth(),Is.True);LogAssert.Expect(LogType.Error,"GameManager: Cannot start zone because ZoneManager is missing.");Assert.That(rebirth.ConfirmRebirth(),Is.True);
        Assert.That(relics.Relics.Count,Is.EqualTo(1));Assert.That(relics.Relics[0].relicLevel,Is.EqualTo(1));Assert.That(player.EquippedWeapon,Is.Not.Null);Assert.That(player.EnsureStarterWeapon(),Is.SameAs(player.EquippedWeapon));
    }

    static RelicData Relic(string id,RelicModifierType type,float value,int tier)=>new(){id=id,cycle=1,relicLevel=100,modifiers=new List<RelicModifier>{new(type,value,false,tier)}};
    static AffixTier Tier(int index,int level,int weight)=>new(){tierIndex=index,minItemLevel=level,minValue=1,maxValue=2,weight=weight};
    static void SetRelicInstance(RelicInventory value)=>typeof(RelicInventory).GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new object[]{value});
    static void SetStatic(Type type,string property,object value)=>type.GetProperty(property,BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
    GameObject Track(GameObject value){cleanup.Add(value);return value;}
}
