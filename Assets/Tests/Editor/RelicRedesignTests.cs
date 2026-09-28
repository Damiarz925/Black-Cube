using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class RelicRedesignTests
{
    [Test] public void DepthRewardsAndLevelsMatchAllMilestones()
    {for(int i=0;i<8;i++){Assert.That(RelicProgressionRules.RewardCount(60+40*i),Is.EqualTo(i+1));}Assert.That(RelicInventory.LevelForZone(60),Is.EqualTo(1));Assert.That(RelicInventory.LevelForZone(340),Is.EqualTo(100));Assert.That(RelicProgressionRules.RollRarity(60,.999f),Is.EqualTo(LootManager.GearRarity.Normal));Assert.That(RelicInventory.LevelForZone(100),Is.LessThan(20));}
    [Test] public void GeneratedRelicsHaveOneImplicitDistinctModsAndNoRetiredFamilies()
    {foreach(var rarity in new[]{LootManager.GearRarity.Normal,LootManager.GearRarity.Magic,LootManager.GearRarity.Rare,LootManager.GearRarity.Legendary}){var relic=RelicProgressionRules.Generate(rarity,100,1);Assert.That(relic.Pristine);Assert.That(relic.modifiers.Count,Is.InRange(AncientRelicCrafting.Minimum(rarity),AncientRelicCrafting.Maximum(rarity)));Assert.That(relic.modifiers.Count(m=>m.lockedOriginal),Is.EqualTo(1));Assert.That(relic.modifiers.Select(m=>m.type).Distinct().Count(),Is.EqualTo(relic.ModifierCount));Assert.That(relic.modifiers.Any(m=>RelicModifierDefinitions.IsRetired(m.type)),Is.False);}}
    [Test] public void MigrationPreservesSlotsAndRelicIdentityWithoutInventingPristineHistory()
    {var relic=new RelicData{id="legacy",cycle=1,modifiers=new(){new(RelicModifierType.ShockThresholdReduction,1,true,5)}};var save=new SaveEnvelope{schemaVersion=13};save.payload.activeRelicIds=new(){"legacy","","",""};save.payload.relics.Add(relic);GamePersistence.MigrateSchema13(save);Assert.That(save.schemaVersion,Is.EqualTo(GamePersistence.SchemaVersion));Assert.That(save.payload.activeRelicIds.Count,Is.EqualTo(8));Assert.That(save.payload.activeRelicIds[0],Is.EqualTo("legacy"));Assert.That(relic.crafted);Assert.That(relic.modifiers[0].type,Is.EqualTo(RelicModifierType.MaximumShockEffect));}
    [Test] public void FusionIsAtomicAndCraftingPermanentlyDisqualifiesInputs()
    {
        var go=new GameObject("Relic test");go.SetActive(false);var inv=go.AddComponent<RelicInventory>();
        try
        {
            var items=new List<RelicData>();for(int i=0;i<5;i++)items.Add(RelicProgressionRules.Generate(LootManager.GearRarity.Normal,1+i*5,1));inv.Restore(items,1,new[]{0,-1,-1,-1});
            Assert.That(inv.CopyActiveIndices().Length,Is.EqualTo(8));
            items[0].crafted=true;Assert.That(inv.TryFuse(items.ToArray(),out _),Is.False);Assert.That(inv.Relics.Count,Is.EqualTo(5));items[0].crafted=false;
            Assert.That(inv.TryFuse(items.ToArray(),out var output),Is.True);Assert.That(inv.Relics.Count,Is.EqualTo(1));Assert.That(output.relicLevel,Is.EqualTo(11));Assert.That(output.rarity,Is.EqualTo(LootManager.GearRarity.Magic));Assert.That(output.Pristine);Assert.That(inv.Active(0),Is.Null);
            Assert.That(AncientRelicCrafting.TryApply(CraftingCurrencyType.AncientMagicToRare,output,inv),Is.True);Assert.That(output.crafted);Assert.That(output.modifiers[0].lockedOriginal);
        }
        finally{Object.DestroyImmediate(go);GamePersistence.ResetStaticStateForTests();}
    }
}
