using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class Step20EndgameItemizationTests
{
    readonly List<GameObject> created=new();
    EndgameResourceLedger ledger;
    [SetUp]public void SetUp(){var go=New("Endgame ledger");ledger=go.AddComponent<EndgameResourceLedger>();}
    [TearDown]public void TearDown(){for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)UnityEngine.Object.DestroyImmediate(created[i]);created.Clear();}
    GameObject New(string name){var go=new GameObject(name);created.Add(go);return go;}

    [Test]public void SchemaElevenAndProductionCatalogAreValid()
    {Assert.That(GamePersistence.SchemaVersion,Is.EqualTo(11));Assert.That(EndgameItemizationValidation.Validate(WorldContentCatalog.Reference),Is.Empty);Assert.That(WorldContentCatalog.Reference.challengeSpecialAffixPools,Has.Count.EqualTo(6));Assert.That(WorldContentCatalog.Reference.challengeSpecialAffixPools.ConvertAll(x=>x.modifiers.Count),Is.All.EqualTo(6));}

    [Test]public void CharacterLedgerCapturesRestoresAndRejectsUnknownIds()
    {
        string key=EndgameResourceIds.ChallengeKey(ProductionWorldContent.BiomeIds[0]),essence=EndgameResourceIds.ChallengeEssence(ProductionWorldContent.BiomeIds[5]),clear=EndgameResourceIds.ChallengeFirstClear(ProductionWorldContent.BiomeIds[5]);
        Assert.That(ledger.Add(key,2),Is.True);Assert.That(ledger.Add(essence,3),Is.True);Assert.That(ledger.Add(EndgameResourceIds.ImplicitReforger),Is.True);Assert.That(ledger.MarkFirstClear(clear),Is.True);
        var resources=ledger.CaptureResources();var flags=ledger.CaptureFirstClears();ledger.ResetForNewGame();Assert.That(ledger.Restore(resources,flags),Is.True);Assert.That(ledger.Count(key),Is.EqualTo(2));Assert.That(ledger.Count(essence),Is.EqualTo(3));Assert.That(ledger.HasFirstClear(clear),Is.True);Assert.That(ledger.Add("unknown.resource"),Is.False);
    }

    [Test]public void SchemaTenMigratesWithEmptyEndgameOwnership()
    {
        string path=Path.Combine(Path.GetTempPath(),"BlackCube-Step20-"+Guid.NewGuid().ToString("N")+".json");try
        {var payload=new GameStatePayload{playerLevel=1,availablePassivePoints=1,encounterStartLife=100,encounterStartMana=100};payload.endgameResources=null;payload.challengeFirstClears=null;for(int i=0;i<RelicInventory.ActiveSlotCount;i++)payload.activeRelicIds.Add(string.Empty);var envelope=new SaveEnvelope{schemaVersion=10,runId="step20",runSeed=20,savedAtUtc=DateTime.UtcNow.ToString("O"),payload=payload};File.WriteAllText(path,JsonUtility.ToJson(envelope));Assert.That(GamePersistence.TryReadFile(path,out var migrated,out var error),Is.True,error);Assert.That(migrated.schemaVersion,Is.EqualTo(11));Assert.That(migrated.payload.endgameResources,Is.Empty);Assert.That(migrated.payload.challengeFirstClears,Is.Empty);}
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [TestCase(119,0)][TestCase(120,1)][TestCase(159,1)][TestCase(160,2)][TestCase(209,2)][TestCase(210,3)][TestCase(259,3)][TestCase(260,4)][TestCase(309,4)][TestCase(310,5)][TestCase(359,5)][TestCase(360,6)]
    public void EmpowermentThresholdsAreExact(int level,int expected)=>Assert.That(EmpowermentProgressionProfile.MaximumEmpoweredModifiers(level),Is.EqualTo(expected));

    [Test]public void CatalystCurveAndChallengeKeyRollUseInjectedProductionStream()
    {
        Assert.That(EndgameDropProfile.CatalystChance(119),Is.Zero);Assert.That(EndgameDropProfile.CatalystChance(120),Is.EqualTo(.05f).Within(.0001));Assert.That(EndgameDropProfile.CatalystChance(360),Is.EqualTo(.30f).Within(.0001));
        Assert.That(ReferenceEquals(EndgameResourceLedger.Instance,ledger),Is.True);string awarded=EndgameDropProfile.TryAwardChallengeKey(360,EnemyAI.EnemyRarity.Legendary,true,1,WorldContentCatalog.Reference,new SequenceLootRandomSource(20,0,0));Assert.That(awarded,Is.Not.Null);Assert.That(ledger.Count(awarded),Is.EqualTo(1));
    }

    [Test]public void EveryBiomePoolCanReplaceOneSameSideOrdinaryExplicit()
    {
        var db=WorldContentCatalog.Reference;foreach(var challenge in db.challengeEncounters)
        {
            Gear gear=MakeLegendary();RolledMod target=gear.rolledMods.Find(x=>x!=null&&!x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType));int before=gear.CraftingModCount;
            Assert.That(ledger.Add(challenge.rewardResourceId),Is.True);Assert.That(EndgameCraftingService.TryBossInfuse(gear,target,challenge,360,new SequenceLootRandomSource(20,0,0,0)),Is.True,challenge.stableContentId);
            Assert.That(gear.CraftingModCount,Is.EqualTo(before));Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(11));Assert.That(ledger.Count(challenge.rewardResourceId),Is.Zero);Assert.That(gear.rolledMods.Exists(x=>x.isBossSpecial&&x.specialPoolId==challenge.specialAffixPoolId),Is.True);
        }
    }

    [Test]public void InvalidInfusionConsumesNothingAndSpecialCannotBeOrdinarilyCrafted()
    {
        var challenge=WorldContentCatalog.Reference.challengeEncounters[0];Gear gear=MakeLegendary();RolledMod target=gear.rolledMods.Find(x=>!x.lockedOriginal&&!Gear.IsWeaponBaseStat(x.statType));ledger.Add(challenge.rewardResourceId);
        Assert.That(EndgameCraftingService.TryBossInfuse(gear,target,challenge,360,new SequenceLootRandomSource(20,0,0)),Is.True);RolledMod special=gear.rolledMods.Find(x=>x.isBossSpecial);int essence=ledger.Count(challenge.rewardResourceId),potential=gear.CurrentCraftingPotential;
        Assert.That(BossSpecialCrafting.TryReplace(gear,special,WorldContentCatalog.Reference.ChallengeSpecialPool(challenge.specialAffixPoolId),360,new SequenceLootRandomSource(20,0)),Is.False);Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RemoveRareModifier,gear),Is.False);Assert.That(EquipmentCrafting.RemovableForTests(gear).Contains(special),Is.False);Assert.That(EmpowermentCrafting.IsEligible(gear,special,360),Is.False);Assert.That(ledger.Count(challenge.rewardResourceId),Is.EqualTo(essence));Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(potential));
    }

    [Test]public void ReforgerEligibilityRejectsLowLevelAndMagicWithoutSpending()
    {
        ledger.Add(EndgameResourceIds.ImplicitReforger,2);Gear low=MakeLegendary(99);Assert.That(EndgameCraftingService.TryReforgeImplicit(low,new SequenceLootRandomSource(20,0)),Is.False);Gear magic=MakeLegendary();magic.SetRarity(LootManager.GearRarity.Magic);Assert.That(EndgameCraftingService.TryReforgeImplicit(magic,new SequenceLootRandomSource(20,0)),Is.False);Assert.That(ledger.Count(EndgameResourceIds.ImplicitReforger),Is.EqualTo(2));
    }

    Gear MakeLegendary(int itemLevel=100)
    {
        var go=New("Legendary fixture");var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Legendary,itemLevel,Element.Phys,WeaponTypeIds.Sword);
        gear.ApplyMods(new List<RolledMod>{new(StatTypes.Life,1,10,true),new(StatTypes.GenericDmg,1,.1f,false)});gear.RestoreCraftingState(LootManager.GearRarity.Legendary,14,14);return gear;
    }
}
