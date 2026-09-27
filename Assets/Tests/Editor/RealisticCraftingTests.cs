using System;
using System.Linq;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class RealisticCraftingTests
{
    static RealisticInventoryResult Inventory()
    {
        var inventory=new RealisticInventoryResult();
        using var session=new WorkbenchSession();
        var rng=new SeededSimulationRandomSource(290);
        foreach(LootManager.GearType slot in Enum.GetValues(typeof(LootManager.GearType)))
        {
            var item=session.ReusableItem;
            item.Initialize(slot,LootManager.GearRarity.Normal,10,Element.Phys,
                slot==LootManager.GearType.Weapons?WeaponTypeIds.Sword:null);
            var mods=session.Roller.RollEquipmentModsForItem(slot,
                LootManager.GearRarity.Normal,10,Element.Phys,item.WeaponTypeId,rng);
            item.ApplyMods(mods);
            if(slot==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(item);
            inventory.items.Add(GearSnapshot.Capture(item));
        }
        inventory.observedDrops=inventory.generatedItems=inventory.items.Count;
        return inventory;
    }

    static RealisticCraftingResult Run(RealisticInventoryResult inventory)=>
        RealisticCraftingOptimizer.Run(new PlayerBuildSnapshot
        {playerLevel=10,combatLevel=10,weaponTypeId=WeaponTypeIds.Sword},
            inventory,new OptimizationObjective(),new OptimizationConstraints(),
            RealisticCraftingSearch.Fast,4,821);

    [Test]
    public void NoCurrency_LeavesEveryOriginalItemAvailable()
    {
        var ground=Inventory();
        var result=Run(ground);
        Assert.That(result.actions,Is.Empty);
        Assert.That(result.inventory.items.Count,Is.EqualTo(ground.items.Count));
        Assert.That(result.inventory.items.Select(x=>x.Description),
            Is.EqualTo(ground.items.Select(x=>x.Description)));
    }

    [Test]
    public void SharedCurrency_CannotBeSpentTwiceAndReplayIsDeterministic()
    {
        var ground=Inventory();
        ground.currencies.Add(new ProgressionCurrencyCount
            {currency=CraftingCurrencyType.NormalToMagic,count=1});
        var first=Run(ground);
        var second=Run(ground);
        Assert.That(first.actions.Count,Is.EqualTo(1),first.stoppingReason);
        Assert.That(first.spent.Sum(x=>x.count),Is.EqualTo(first.actions.Count));
        Assert.That(first.remaining.Single(x=>x.currency==CraftingCurrencyType.NormalToMagic).count,
            Is.EqualTo(1-first.actions.Count));
        Assert.That(first.inventory.items.Count,Is.EqualTo(ground.items.Count));
        Assert.That(first.inventory.items.Select(x=>x.Description),
            Is.EqualTo(second.inventory.items.Select(x=>x.Description)));
        Assert.That(first.actions.Select(x=>x.inventoryIndex),
            Is.EqualTo(second.actions.Select(x=>x.inventoryIndex)));
    }
}
