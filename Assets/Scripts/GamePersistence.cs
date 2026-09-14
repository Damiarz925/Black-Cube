// Developer map: One-version JSON snapshot for crafted equipment, currencies, permanent relics and active relic slots.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GearSaveData
{
    public LootManager.GearType type;
    public LootManager.GearRarity rarity;
    public int itemLevel;
    public Element element;
    public float baseDamage,baseAttackSpeed,baseCritChance;
    public List<RolledMod> mods=new();
    public static GearSaveData Capture(Gear gear)
    {
        var data=new GearSaveData{type=gear.ItemType,rarity=gear.ItemRarity,itemLevel=gear.ItemLevel,element=gear.BaseElement,
            baseDamage=gear.BaseDamage,baseAttackSpeed=gear.BaseAttackSpeed,baseCritChance=gear.BaseCritChance};
        foreach(var mod in gear.rolledMods)if(mod!=null)data.mods.Add(new RolledMod(mod.statType,mod.tierIndex,mod.value,mod.lockedOriginal));
        return data;
    }
    public Gear Create(string objectName="Loaded Gear")
    {
        var go=new GameObject(objectName);var gear=go.AddComponent<Gear>();gear.Initialize(type,rarity,Mathf.Max(1,itemLevel),element);
        var copies=new List<RolledMod>();if(mods!=null)foreach(var mod in mods)if(mod!=null)copies.Add(new RolledMod(mod.statType,mod.tierIndex,mod.value,mod.lockedOriginal));
        gear.ApplyMods(copies);gear.SetRarity(rarity);
        if(!copies.Exists(m=>m!=null&&m.statType==StatTypes.WeaponBaseDmg))gear.BaseDamage=baseDamage;
        if(!copies.Exists(m=>m!=null&&m.statType==StatTypes.WeaponBaseAttackSpeed))gear.BaseAttackSpeed=baseAttackSpeed;
        if(!copies.Exists(m=>m!=null&&m.statType==StatTypes.WeaponBaseCrit))gear.BaseCritChance=baseCritChance;
        return gear;
    }
}

[Serializable]
public sealed class EquippedGearSaveData{public LootManager.GearType slot;public GearSaveData gear;}

[Serializable]
public sealed class GameSaveData
{
    public int version=1;
    public List<GearSaveData> inventory=new();
    public List<EquippedGearSaveData> equipped=new();
    public List<CurrencyStackData> currencies=new();
    public List<RelicData> relics=new();
    public int relicCycle;
    public int[] activeRelicIndices;
}

public static class GamePersistence
{
    public const string SaveKey="BlackCube.Save.V1";
    static bool loadRequested;
    public static bool LoadRequested=>loadRequested;
    public static bool RequestLoad()
    {
        if(!PlayerPrefs.HasKey(SaveKey))return false;
        loadRequested=true;
        return true;
    }
    public static void RequestNewGame()=>loadRequested=false;
    public static bool ConsumeLoadRequest(){bool value=loadRequested;loadRequested=false;return value;}
    public static bool RestoreRequestedGame(out bool restored)
    {
        if(!ConsumeLoadRequest()){restored=false;return false;}
        restored=Load();
        return true;
    }
    public static void Save()
    {
        var data=new GameSaveData();
        if(Inventory.Instance!=null)foreach(var gear in Inventory.Instance.Items)if(gear!=null&&!gear.IsScrap)data.inventory.Add(GearSaveData.Capture(gear));
        if(EquipmentManager.Instance!=null)foreach(var pair in EquipmentManager.Instance.EquippedItems)data.equipped.Add(new EquippedGearSaveData{slot=pair.Key,gear=GearSaveData.Capture(pair.Value)});
        if(CurrencyInventory.Instance!=null)foreach(var stack in CurrencyInventory.Instance.Stacks)data.currencies.Add(stack);
        if(RelicInventory.Instance!=null)
        {
            data.relicCycle=RelicInventory.Instance.CurrentCycle;data.activeRelicIndices=RelicInventory.Instance.CopyActiveIndices();
            foreach(var relic in RelicInventory.Instance.Relics)data.relics.Add(CloneRelic(relic));
        }
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();
    }
    public static bool Load()
    {
        if(!PlayerPrefs.HasKey(SaveKey))return false;
        var data=JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey));if(data==null||data.version!=1)return false;
        EquipmentManager.Instance?.ResetForRebirth();Inventory.Instance?.ResetForRebirth();
        CurrencyInventory.Instance?.Restore(data.currencies);
        if(Inventory.Instance!=null&&data.inventory!=null)foreach(var item in data.inventory)if(item!=null)Inventory.Instance.Add(item.Create());
        if(EquipmentManager.Instance!=null&&data.equipped!=null)foreach(var entry in data.equipped)if(entry?.gear!=null)EquipmentManager.Instance.Equip(entry.gear.Create("Loaded Equipped Gear"));
        RelicInventory.Instance?.Restore(data.relics,data.relicCycle,data.activeRelicIndices);
        return true;
    }
    static RelicData CloneRelic(RelicData source)
    {
        var copy=new RelicData{id=source.id,cycle=source.cycle,rarity=source.rarity,craftableThisCycle=source.craftableThisCycle};
        if(source.modifiers!=null)foreach(var mod in source.modifiers)if(mod!=null)copy.modifiers.Add(new RelicModifier(mod.type,mod.value,mod.lockedOriginal));return copy;
    }
}

public sealed class GamePersistenceHost:MonoBehaviour
{
    void OnApplicationPause(bool paused){if(paused)GamePersistence.Save();}
    void OnApplicationQuit()=>GamePersistence.Save();
}
