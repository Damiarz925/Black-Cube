using System;
using BlackCube.BalanceWorkbench;
using NUnit.Framework;

public sealed class RealisticGearsetTests
{
    [Test]
    public void WholeInventorySearch_UsesHistoricalCandidatesWithoutGeneratingNewItems()
    {
        var inventory=new RealisticInventoryResult();
        using(var session=new WorkbenchSession())
        {
            var rng=new SeededSimulationRandomSource(190);
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
        }
        var result=RealisticGearsetOptimizer.Optimize(new PlayerBuildSnapshot
        {playerLevel=10,combatLevel=10,weaponTypeId=WeaponTypeIds.Sword},inventory,
            new OptimizationObjective(),new OptimizationConstraints());
        Assert.That(result.build,Is.Not.Null);
        Assert.That(result.build.equipment.Count,Is.EqualTo(8));
        Assert.That(result.inventorySize,Is.EqualTo(8));
        Assert.That(result.metrics.totalSustainableDps,Is.GreaterThan(0));
    }
}
