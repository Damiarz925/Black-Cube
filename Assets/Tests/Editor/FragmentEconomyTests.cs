using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class FragmentEconomyTests
{
    readonly List<GameObject> created = new();
    const string FilterKey = "BlackCube.InventoryFilters.V1";
    bool hadFilters;
    string oldFilters;

    [SetUp] public void SetUp()
    {
        hadFilters = PlayerPrefs.HasKey(FilterKey);
        oldFilters = hadFilters ? PlayerPrefs.GetString(FilterKey) : null;
        PlayerPrefs.DeleteKey(FilterKey);
        SetInstance(typeof(Inventory), null);
        SetInstance(typeof(CurrencyInventory), null);
        GamePersistence.ResetStaticStateForTests();
    }

    [TearDown] public void TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();
        SetInstance(typeof(Inventory), null);
        SetInstance(typeof(CurrencyInventory), null);
        if (hadFilters) PlayerPrefs.SetString(FilterKey, oldFilters); else PlayerPrefs.DeleteKey(FilterKey);
        GamePersistence.ResetStaticStateForTests();
    }

    [Test] public void FragmentsConvertEveryTenAndPreserveRemainder()
    {
        var currency = CreateInventory().GetComponent<CurrencyInventory>();
        Assert.That(currency.AddFragments(CraftingCurrencyType.NormalToMagic, 9), Is.True);
        Assert.That(currency.AddFragments(CraftingCurrencyType.NormalToMagic, 3), Is.True);
        Assert.That(currency.NormalToMagicFragments, Is.EqualTo(2));
        Assert.That(currency.Count(CraftingCurrencyType.NormalToMagic), Is.EqualTo(1));
        Assert.That(currency.AddFragments(CraftingCurrencyType.MagicToRare, 22), Is.True);
        Assert.That(currency.MagicToRareFragments, Is.EqualTo(2));
        Assert.That(currency.Count(CraftingCurrencyType.MagicToRare), Is.EqualTo(2));
        Assert.That(currency.AddFragments(CraftingCurrencyType.RerollMagic, 1), Is.False);
        Assert.That(currency.RestoreFragments(9, 8), Is.True);
        Assert.That(currency.RestoreFragments(10, 0), Is.False);
        Assert.That(currency.NormalToMagicFragments, Is.EqualTo(9));
        Assert.That(currency.MagicToRareFragments, Is.EqualTo(8));
    }

    [Test] public void ManualAndPickupFilterDismantleShareFragmentRuleWithoutDuplicateRewards()
    {
        var inventory = CreateInventory().GetComponent<Inventory>();
        var currency = inventory.GetComponent<CurrencyInventory>();
        Gear normal = Gear(LootManager.GearRarity.Normal);
        inventory.Add(normal);
        Assert.That(inventory.CanDismantle(normal), Is.True);
        ExpectEditModeDestroy();
        Assert.That(inventory.TryDismantle(normal), Is.True);
        Assert.That(currency.NormalToMagicFragments, Is.Zero);
        Assert.That(currency.MagicToRareFragments, Is.Zero);
        Assert.That(inventory.TryDismantle(normal), Is.False);

        Gear magic = Gear(LootManager.GearRarity.Magic);
        inventory.Add(magic);
        ExpectEditModeDestroy();
        Assert.That(inventory.TryDismantle(magic), Is.True);
        Assert.That(currency.NormalToMagicFragments, Is.EqualTo(1));
        Assert.That(inventory.TryDismantle(magic), Is.False);

        inventory.FilterRarity = LootManager.GearRarity.Legendary;
        inventory.FilterRarityEnabled = true;
        Gear rare = Gear(LootManager.GearRarity.Rare);
        Gear legendary = Gear(LootManager.GearRarity.Legendary);
        ExpectEditModeDestroy();
        Assert.That(inventory.Pickup(rare), Is.True);
        ExpectEditModeDestroy();
        Assert.That(inventory.Pickup(legendary), Is.True);
        Assert.That(inventory.Pickup(legendary), Is.False);
        Assert.That(inventory.Items, Is.Empty);
        Assert.That(currency.MagicToRareFragments, Is.EqualTo(3));
        Assert.That(currency.Count(CraftingCurrencyType.RerollMagic), Is.Zero);
    }

    [Test] public void NewGameAndRebirthClearFragmentsWhileOrdinaryRestoreRehydratesThem()
    {
        var currency = CreateInventory().GetComponent<CurrencyInventory>();
        currency.AddFragments(CraftingCurrencyType.NormalToMagic, 7);
        currency.AddFragments(CraftingCurrencyType.MagicToRare, 6);
        var stacks = new List<CurrencyStackData>(currency.Stacks);
        currency.ResetForNewRun();
        Assert.That(currency.NormalToMagicFragments, Is.Zero);
        Assert.That(currency.MagicToRareFragments, Is.Zero);
        currency.Restore(stacks);
        Assert.That(currency.RestoreFragments(7, 6), Is.True);
        Assert.That(currency.NormalToMagicFragments, Is.EqualTo(7));
        Assert.That(currency.MagicToRareFragments, Is.EqualTo(6));
        currency.ResetForNewGame();
        Assert.That(currency.NormalToMagicFragments, Is.Zero);
        Assert.That(currency.MagicToRareFragments, Is.Zero);
    }

    GameObject CreateInventory()
    {
        var host = new GameObject("fragment inventory", typeof(Inventory), typeof(CurrencyInventory));
        created.Add(host);
        SetInstance(typeof(Inventory), host.GetComponent<Inventory>());
        SetInstance(typeof(CurrencyInventory), host.GetComponent<CurrencyInventory>());
        return host;
    }

    Gear Gear(LootManager.GearRarity rarity)
    {
        var host = new GameObject("fragment gear", typeof(Gear));
        created.Add(host);
        var gear = host.GetComponent<Gear>();
        gear.Initialize(LootManager.GearType.Helmets, rarity, 25, Element.Phys);
        return gear;
    }

    static void SetInstance(Type type, object value) => type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
        .GetSetMethod(true).Invoke(null, new[] { value });
    static void ExpectEditModeDestroy() => LogAssert.Expect(LogType.Error,
        new Regex("Destroy may not be called from edit mode"));
}
