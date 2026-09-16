using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class CraftingAndRebirthTests
{
    readonly List<GameObject> cleanup=new();
    [TearDown]public void TearDown(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)Object.DestroyImmediate(cleanup[i]);cleanup.Clear();PlayerPrefs.DeleteKey(GamePersistence.SaveKey);}

    [Test]
    public void EquipmentCrafting_FullOrdinaryPathPreservesImplicitAndCapsRareAtFourExplicits()
    {
        ModManager manager=CreateModManager();
        Gear gear=CreateGear(LootManager.GearRarity.Normal);
        gear.ApplyMods(manager.RollEquipmentModsForItem(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement));
        Assert.That(gear.CraftingModCount,Is.EqualTo(0));
        RolledMod locked=gear.rolledMods.Single(m=>!Gear.IsWeaponBaseStat(m.statType));
        Assert.That(locked.lockedOriginal,Is.True);

        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.NormalToMagic,gear,manager),Is.True);
        Assert.That(gear.ItemRarity,Is.EqualTo(LootManager.GearRarity.Magic));Assert.That(gear.CraftingModCount,Is.EqualTo(2));AssertLocked(gear,locked);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RerollMagic,gear,manager),Is.True);AssertLocked(gear,locked);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.MagicToRare,gear,manager),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(4));AssertLocked(gear,locked);
        Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.AddRareModifier,gear),Is.False);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RerollRareModifier,gear,manager),Is.True);AssertLocked(gear,locked);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,manager),Is.True);Assert.That(gear.CraftingModCount,Is.EqualTo(3));AssertLocked(gear,locked);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.AddRareModifier,gear,manager),Is.True);Assert.That(gear.CraftingModCount,Is.EqualTo(4));AssertLocked(gear,locked);
    }

    [Test]
    public void CurrencyInventory_InvalidTargetDoesNotConsumeAndAncientCannotTargetEquipment()
    {
        CreateModManager();var host=Track(new GameObject("currency"));var currency=host.AddComponent<CurrencyInventory>();
        Gear gear=CreateGear(LootManager.GearRarity.Normal);gear.ApplyMods(new List<RolledMod>{new(StatTypes.Life,1,10,true)});
        currency.Add(CraftingCurrencyType.MagicToRare);currency.Arm(CraftingCurrencyType.MagicToRare);
        Assert.That(currency.TryApplyArmedToGear(gear),Is.False);Assert.That(currency.Count(CraftingCurrencyType.MagicToRare),Is.EqualTo(1));Assert.That(currency.ArmedCurrency,Is.Null);
        currency.Add(CraftingCurrencyType.AncientAddModifier);currency.Arm(CraftingCurrencyType.AncientAddModifier);
        Assert.That(currency.TryApplyArmedToGear(gear),Is.False);Assert.That(currency.Count(CraftingCurrencyType.AncientAddModifier),Is.EqualTo(1));Assert.That(currency.ArmedCurrency,Is.Null);
    }

    [Test]
    public void SaveDtosRoundTripRarityLockedModsCurrenciesAndRelics()
    {
        Gear gear=CreateGear(LootManager.GearRarity.Rare);gear.ApplyMods(new List<RolledMod>{new(StatTypes.Life,2,17,true),new(StatTypes.FireRes,3,22,false)});gear.SetRarity(LootManager.GearRarity.Rare);
        var data=new GameSaveData();data.inventory.Add(GearSaveData.Capture(gear));data.currencies.Add(new CurrencyStackData(CraftingCurrencyType.AddRareModifier,4));
        data.relicCycle=3;data.activeRelicIndices=new[]{0,-1};data.relics.Add(new RelicData{id="saved",cycle=3,rarity=LootManager.GearRarity.Magic,craftableThisCycle=true,modifiers=new List<RelicModifier>{new(RelicModifierType.MoreDamage,8,true),new(RelicModifierType.IncreasedExperience,6,false)}});
        GameSaveData restored=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
        Assert.That(restored.inventory[0].rarity,Is.EqualTo(LootManager.GearRarity.Rare));Assert.That(restored.inventory[0].mods[0].lockedOriginal,Is.True);Assert.That(restored.currencies[0].amount,Is.EqualTo(4));
        Assert.That(restored.relicCycle,Is.EqualTo(3));Assert.That(restored.activeRelicIndices,Is.EqualTo(new[]{0,-1}));Assert.That(restored.relics[0].modifiers[0].lockedOriginal,Is.True);
    }

    [Test]
    public void AncientCrafting_ReachesOneTwoThreeFourFiveSixAndNeverMutatesLockedOrOldRelic()
    {
        var host=Track(new GameObject("relics"));var inventory=host.AddComponent<RelicInventory>();RelicData relic=inventory.BeginNewCycle();RelicModifier locked=relic.modifiers[0];
        Assert.That(relic.ModifierCount,Is.EqualTo(1));
        ApplyAncient(CraftingCurrencyType.AncientNormalToMagic,relic,inventory,2,locked);
        ApplyAncient(CraftingCurrencyType.AncientMagicToRare,relic,inventory,3,locked);
        ApplyAncient(CraftingCurrencyType.AncientAddModifier,relic,inventory,4,locked);
        ApplyAncient(CraftingCurrencyType.AncientRareToLegendary,relic,inventory,5,locked);
        ApplyAncient(CraftingCurrencyType.AncientAddModifier,relic,inventory,6,locked);
        ApplyAncient(CraftingCurrencyType.AncientReroll,relic,inventory,6,locked);
        ApplyAncient(CraftingCurrencyType.AncientRemoveModifier,relic,inventory,5,locked);
        Assert.That(AncientRelicCrafting.CanApply(CraftingCurrencyType.AncientRemoveModifier,relic,inventory),Is.False);
        RelicData next=inventory.BeginNewCycle();string oldSnapshot=JsonUtility.ToJson(relic);
        Assert.That(inventory.IsCurrentCraftable(relic),Is.False);Assert.That(AncientRelicCrafting.TryApply(CraftingCurrencyType.AncientReroll,relic,inventory),Is.False);Assert.That(JsonUtility.ToJson(relic),Is.EqualTo(oldSnapshot));Assert.That(next.cycle,Is.EqualTo(2));
    }

    [Test]
    public void RelicBonusesUseOnlyTheTwoEquippedSlots()
    {
        var host=Track(new GameObject("relic effects"));var inventory=host.AddComponent<RelicInventory>();
        var damage=new RelicData{id="damage",modifiers=new List<RelicModifier>{new(RelicModifierType.MoreDamage,10,true)}};
        var speed=new RelicData{id="speed",modifiers=new List<RelicModifier>{new(RelicModifierType.MoreAttackSpeed,5,true)}};
        var xp=new RelicData{id="xp",modifiers=new List<RelicModifier>{new(RelicModifierType.IncreasedExperience,99,true)}};
        inventory.Restore(new List<RelicData>{damage,speed,xp},1,new[]{0,1});
        Assert.That(inventory.DamageMultiplier,Is.EqualTo(1.1f).Within(.0001f));Assert.That(inventory.AttackSpeedMultiplier,Is.EqualTo(1.05f).Within(.0001f));Assert.That(inventory.ExperienceMultiplier,Is.EqualTo(1f));
        inventory.Equip(xp,1);Assert.That(inventory.AttackSpeedMultiplier,Is.EqualTo(1f));Assert.That(inventory.ExperienceMultiplier,Is.EqualTo(1.99f).Within(.0001f));
        inventory.Unequip(0);Assert.That(inventory.DamageMultiplier,Is.EqualTo(1f));
    }

    [Test]
    public void AncientCurrencyRolloverClearsPriorCycleAndGrantsFreshSet()
    {
        var host=Track(new GameObject("currencies"));var inventory=host.AddComponent<CurrencyInventory>();
        foreach(CraftingCurrencyType type in System.Enum.GetValues(typeof(CraftingCurrencyType)))if(CurrencyInventory.IsAncient(type))inventory.Add(type,4);
        inventory.Add(CraftingCurrencyType.NormalToMagic,3);inventory.ClearAncient();
        foreach(CraftingCurrencyType type in System.Enum.GetValues(typeof(CraftingCurrencyType)))if(CurrencyInventory.IsAncient(type))inventory.Add(type);
        Assert.That(inventory.Count(CraftingCurrencyType.NormalToMagic),Is.EqualTo(3));
        foreach(CraftingCurrencyType type in System.Enum.GetValues(typeof(CraftingCurrencyType)))if(CurrencyInventory.IsAncient(type))Assert.That(inventory.Count(type),Is.EqualTo(1),type.ToString());
    }

    [Test]
    public void RepeatedConfirmedRebirthLocksOldRelicAndReplacesAncientStacks()
    {
        var inventoryHost=Track(new GameObject("inventory"));inventoryHost.AddComponent<Inventory>();var currency=inventoryHost.AddComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),currency);
        SetInstance(typeof(GameManager),null);SetInstance(typeof(RelicInventory),null);SetInstance(typeof(RebirthManager),null);
        var managerHost=Track(new GameObject("combat progression"));var manager=managerHost.AddComponent<GameManager>();var relics=managerHost.GetComponent<RelicInventory>()??managerHost.AddComponent<RelicInventory>();var rebirth=managerHost.GetComponent<RebirthManager>()??managerHost.AddComponent<RebirthManager>();SetInstance(typeof(GameManager),manager);SetInstance(typeof(RelicInventory),relics);SetInstance(typeof(RebirthManager),rebirth);
        SetZone(manager,60);Assert.That(rebirth.RequestRebirth(),Is.True);LogAssert.Expect(LogType.Error,"GameManager: Cannot start zone because ZoneManager is missing.");Assert.That(rebirth.ConfirmRebirth(),Is.True);RelicData first=relics.Relics[0];
        CurrencyInventory.Instance.Add(CraftingCurrencyType.AncientReroll,7);
        SetZone(manager,60);Assert.That(rebirth.RequestRebirth(),Is.True);LogAssert.Expect(LogType.Error,"GameManager: Cannot start zone because ZoneManager is missing.");Assert.That(rebirth.ConfirmRebirth(),Is.True);
        Assert.That(relics.Relics.Count,Is.EqualTo(2));Assert.That(first.craftableThisCycle,Is.False);Assert.That(relics.IsCurrentCraftable(relics.Relics[1]),Is.True);
        foreach(CraftingCurrencyType type in System.Enum.GetValues(typeof(CraftingCurrencyType)))if(CurrencyInventory.IsAncient(type))Assert.That(CurrencyInventory.Instance.Count(type),Is.EqualTo(1),type.ToString());
    }

    static void ApplyAncient(CraftingCurrencyType type,RelicData relic,RelicInventory inventory,int count,RelicModifier locked)
    {Assert.That(AncientRelicCrafting.TryApply(type,relic,inventory),Is.True,type.ToString());Assert.That(relic.ModifierCount,Is.EqualTo(count));Assert.That(relic.modifiers.Contains(locked),Is.True);Assert.That(locked.lockedOriginal,Is.True);}
    static void AssertLocked(Gear gear,RolledMod locked){Assert.That(gear.rolledMods.Contains(locked),Is.True);Assert.That(locked.lockedOriginal,Is.True);}
    Gear CreateGear(LootManager.GearRarity rarity){var go=Track(new GameObject("gear"));var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,rarity,80,Element.Phys);return gear;}
    ModManager CreateModManager()
    {
        var lists=Track(new GameObject("stat lists")).AddComponent<GearStatLists>();SetInstance(typeof(GearStatLists),lists);typeof(GearStatLists).GetField("statPools",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(lists,GearStatLists.BuildDefaultStatPools());var go=Track(new GameObject("mods"));var manager=go.AddComponent<ModManager>();SetInstance(typeof(ModManager),manager);
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");Assert.That(database,Is.Not.Null);database.Initialize();
        typeof(ModManager).GetField("modDatabase",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,database);return manager;
    }
    static void SetInstance(System.Type type,object value)=>type.GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
    static void SetLevel(PlayerProgression progression,int value)=>typeof(PlayerProgression).GetField("level",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progression,value);
    static void SetZone(GameManager manager,int value)=>typeof(GameManager).GetField("currentZoneLevel",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,value);
    GameObject Track(GameObject value){cleanup.Add(value);return value;}
}
