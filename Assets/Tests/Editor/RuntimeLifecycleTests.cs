using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RuntimeLifecycleTests
{
    GameObject inventoryHost;

    [TearDown]
    public void TearDown()
    {
        if (inventoryHost != null) UnityEngine.Object.DestroyImmediate(inventoryHost);
    }

    [Test]
    public void InventoryOwnsGearAcrossSceneLifetime()
    {
        inventoryHost = new GameObject("Inventory lifecycle fixture");
        Inventory inventory = inventoryHost.AddComponent<Inventory>();
        var gearObject = new GameObject("Run gear");
        Gear gear = gearObject.AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.Helmets, LootManager.GearRarity.Normal, 1, Element.Phys);
        inventory.Add(gear);
        Assert.That(gear.transform.parent, Is.EqualTo(inventory.transform));
        inventory.Remove(gear);
        UnityEngine.Object.DestroyImmediate(gearObject);
    }

    [Test]
    public void NewGameRequestAlwaysClearsStaleLoadIntent()
    {
        bool hadSave = PlayerPrefs.HasKey(GamePersistence.SaveKey);
        string value = hadSave ? PlayerPrefs.GetString(GamePersistence.SaveKey) : null;
        try
        {
            PlayerPrefs.SetString(GamePersistence.SaveKey, JsonUtility.ToJson(new GameSaveData()));
            Assert.That(GamePersistence.RequestLoad(), Is.True);
            GamePersistence.RequestNewGame();
            Assert.That(GamePersistence.LoadRequested, Is.False);
            Assert.That(GamePersistence.ConsumeLoadRequest(), Is.False);
        }
        finally
        {
            GamePersistence.RequestNewGame();
            if (hadSave) PlayerPrefs.SetString(GamePersistence.SaveKey, value); else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        }
    }

    [Test]
    public void FreshRunClearsOrdinaryButDefersAncientCurrencyPolicy()
    {
        var host = new GameObject("Currency lifecycle fixture");
        try
        {
            CurrencyInventory currency = host.AddComponent<CurrencyInventory>();
            currency.Add(CraftingCurrencyType.MagicToRare, 3);
            currency.Add(CraftingCurrencyType.AncientMagicToRare, 2);
            currency.Arm(CraftingCurrencyType.MagicToRare);
            currency.ResetForNewRun();
            Assert.That(currency.Count(CraftingCurrencyType.MagicToRare), Is.Zero);
            Assert.That(currency.Count(CraftingCurrencyType.AncientMagicToRare), Is.EqualTo(2));
            Assert.That(currency.ArmedCurrency, Is.Null);
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }

    [Test]
    public void SceneReferenceReleaseClearsPersistentBindings()
    {
        var host = new GameObject("Equipment lifecycle fixture");
        try
        {
            EquipmentManager manager = host.AddComponent<EquipmentManager>();
            SetField(manager, "playerStats", host.AddComponent<StatsComponent>());
            SetField(manager, "playerController", host.AddComponent<PlayerController>());
            manager.ReleaseSceneReferences();
            Assert.That(GetField(manager, "playerStats"), Is.Null);
            Assert.That(GetField(manager, "playerController"), Is.Null);
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }

    static object GetField(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    static void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
