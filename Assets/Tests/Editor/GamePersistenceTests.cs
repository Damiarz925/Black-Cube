using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class GamePersistenceTests
{
    string directory; bool hadLegacy; string legacy;
    [SetUp] public void SetUp()
    {
        directory=Path.Combine(Path.GetTempPath(),"BlackCube-SaveTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        GamePersistence.SaveDirectoryOverride=directory;GamePersistence.ResetStaticStateForTests();
        hadLegacy=PlayerPrefs.HasKey(GamePersistence.SaveKey);legacy=hadLegacy?PlayerPrefs.GetString(GamePersistence.SaveKey):null;PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
    }
    [TearDown] public void TearDown()
    {
        GamePersistence.ResetStaticStateForTests();GamePersistence.SaveDirectoryOverride=null;
        if(hadLegacy)PlayerPrefs.SetString(GamePersistence.SaveKey,legacy);else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }

    [Test] public void NoSaveReturnsClearFailure(){Assert.That(GamePersistence.TryReadBestEnvelope(out _,out var source,out var error),Is.False);Assert.That(source,Is.EqualTo(SaveLoadSource.None));Assert.That(error,Does.Contain("No gameplay save"));}
    [Test] public void ValidEnvelopePasses(){Assert.That(GamePersistence.ValidateEnvelope(Valid(),out var error),Is.True,error);}
    [Test] public void FutureSchemaIsRejected(){var e=Valid();e.schemaVersion=99;AssertInvalid(e,"newer");}
    [Test] public void Schema2ScalarGearMigratesToExactConstantDamageRange()
    {
        var old=Valid();old.schemaVersion=2;
        var item=Gear("old-weapon");item.type=LootManager.GearType.Weapons;
        item.baseDamage=87f;item.mods[0]=new RolledMod(StatTypes.FlatPhys,2,17f,true);
        old.payload.gearItems.Add(item);old.payload.inventoryGearIds.Add(item.id);
        File.WriteAllText(GamePersistence.PrimaryPath,JsonUtility.ToJson(old));
        Assert.That(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var migrated,out var error),Is.True,error);
        Assert.That(migrated.schemaVersion,Is.EqualTo(4));
        Assert.That(migrated.payload.gearItems[0].baseDamageMin,Is.EqualTo(87f));
        Assert.That(migrated.payload.gearItems[0].baseDamageMax,Is.EqualTo(87f));
        Assert.That(migrated.payload.gearItems[0].legacyAffixRules,Is.True);
        Assert.That(migrated.payload.gearItems[0].mods[0].HighValue,Is.EqualTo(17f));
    }
    [Test] public void Schema3LockedOriginalMigratesToImplicitWithoutDeletingHistoricalSideExcess()
    {
        var old=Valid();old.schemaVersion=3;
        var ring=Gear("old-rare");ring.rarity=LootManager.GearRarity.Rare;
        ring.itemLevel=100;ring.mods[0]=new RolledMod(StatTypes.GenericDmg,1,31f,true);
        ring.mods.Add(new RolledMod(StatTypes.FireRes,8,8f));
        ring.mods.Add(new RolledMod(StatTypes.ColdRes,8,9f));
        ring.mods.Add(new RolledMod(StatTypes.LightRes,8,10f));
        old.payload.gearItems.Add(ring);old.payload.inventoryGearIds.Add(ring.id);
        File.WriteAllText(GamePersistence.PrimaryPath,JsonUtility.ToJson(old));
        Assert.That(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var migrated,out var error),Is.True,error);
        var item=migrated.payload.gearItems[0];
        Assert.That(migrated.schemaVersion,Is.EqualTo(4));
        Assert.That(item.mods.Count,Is.EqualTo(4));
        Assert.That(item.mods[0].lockedOriginal,Is.True);
        Assert.That(item.mods[0].value,Is.EqualTo(31f));
        Assert.That(item.mods[1].value,Is.EqualTo(8f));
        Assert.That(item.mods[2].value,Is.EqualTo(9f));
        Assert.That(item.mods[3].value,Is.EqualTo(10f));
        Assert.That(item.legacyAffixRules,Is.True);
    }
    [Test] public void Schema3PairedLockedWeaponRollAndBaseRangeRemainExact()
    {
        var old=Valid();old.schemaVersion=3;
        var weapon=Gear("old-paired");weapon.type=LootManager.GearType.Weapons;
        weapon.itemLevel=100;weapon.baseDamage=80f;weapon.baseDamageMin=64f;
        weapon.baseDamageMax=96f;
        weapon.mods[0]=new RolledMod(StatTypes.FlatFire,1,21f,38f,true);
        old.payload.gearItems.Add(weapon);old.payload.inventoryGearIds.Add(weapon.id);
        File.WriteAllText(GamePersistence.PrimaryPath,JsonUtility.ToJson(old));
        Assert.That(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var migrated,out var error),Is.True,error);
        var item=migrated.payload.gearItems[0];
        Assert.That(item.baseDamageMin,Is.EqualTo(64f));
        Assert.That(item.baseDamageMax,Is.EqualTo(96f));
        Assert.That(item.mods[0].lockedOriginal,Is.True);
        Assert.That(item.mods[0].value,Is.EqualTo(21f));
        Assert.That(item.mods[0].HighValue,Is.EqualTo(38f));
        Assert.That(item.mods[0].hasSecondaryValue,Is.True);
    }
    [Test] public void UnknownEnumIsRejected(){var e=Valid();e.payload.currencies.Add(new CurrencyStackData((CraftingCurrencyType)999,1));AssertInvalid(e,"Currency");}
    [Test] public void InvalidModifierIsRejected(){var e=Valid();e.payload.gearItems.Add(Gear("a",(StatTypes)999));e.payload.inventoryGearIds.Add("a");AssertInvalid(e,"modifier");}
    [Test] public void CurrentDirectAffixTierAndSideCapacityAreValidated()
    {
        var e=Valid();var ring=Gear("direct-ring",StatTypes.FireRes);
        ring.mods[0]=new RolledMod(StatTypes.FireRes,8,8f,true);
        e.payload.gearItems.Add(ring);e.payload.inventoryGearIds.Add(ring.id);
        Assert.That(GamePersistence.ValidateEnvelope(e,out var error),Is.True,error);
        ring.mods[0].hasSecondaryValue=true;ring.mods[0].secondaryValue=8f;
        AssertInvalid(e,"affix");
        ring.mods[0].hasSecondaryValue=false;
        ring.mods[0].value=50f;AssertInvalid(e,"affix");
        ring.mods[0].value=8f;ring.rarity=LootManager.GearRarity.Magic;
        ring.mods.Add(new RolledMod(StatTypes.ColdRes,8,8f));
        Assert.That(GamePersistence.ValidateEnvelope(e,out error),Is.True,error);
        ring.mods.Add(new RolledMod(StatTypes.LightRes,8,8f));
        AssertInvalid(e,"side capacity");
    }
    [Test] public void CurrentPairedWeaponAffixNeedsBothLegalRolls()
    {
        var e=Valid();var weapon=Gear("paired-weapon",StatTypes.FlatPhys);
        weapon.type=LootManager.GearType.Weapons;weapon.mods[0]=new RolledMod(StatTypes.FlatPhys,9,1f,2f,true);
        e.payload.gearItems.Add(weapon);e.payload.inventoryGearIds.Add(weapon.id);
        Assert.That(GamePersistence.ValidateEnvelope(e,out var error),Is.True,error);
        weapon.mods[0].hasSecondaryValue=false;AssertInvalid(e,"affix");
    }
    [Test] public void DuplicateGearIdIsRejected(){var e=Valid();e.payload.gearItems.Add(Gear("a"));e.payload.gearItems.Add(Gear("a"));e.payload.inventoryGearIds.Add("a");AssertInvalid(e,"unique");}
    [Test] public void DuplicateGearOwnershipIsRejected(){var e=Valid();e.payload.gearItems.Add(Gear("a"));e.payload.inventoryGearIds.Add("a");e.payload.equippedGear.Add(new EquippedGearReference{slot=LootManager.GearType.Weapons,gearId="a"});AssertInvalid(e,"ownership");}
    [Test] public void InvalidPassiveAllocationIsRejected(){var e=Valid();e.payload.passiveRanks.Add(new PassiveRankData(PassiveTreeDefinition.NodeCount,1));AssertInvalid(e,"Passive");}
    [Test] public void InvalidRelicSlotIsRejected(){var e=Valid();e.payload.activeRelicIds[0]="missing";AssertInvalid(e,"slot");}
    [Test] public void IllegalCombatPositionIsRejected(){var e=Valid();e.payload.bossEncounter=true;e.payload.completedNormalEncounters=3;AssertInvalid(e,"Combat");}
    [Test] public void NonFiniteCheckpointIsRejected(){var e=Valid();e.payload.encounterStartLife=float.NaN;AssertInvalid(e,"checkpoint");}
    [Test] public void MalformedJsonIsRejected(){File.WriteAllText(GamePersistence.PrimaryPath,"{not-json");Assert.That(GamePersistence.TryReadBestEnvelope(out _,out _,out var error),Is.False);Assert.That(error,Does.Contain("primary"));}
    [Test] public void InvalidPrimaryFallsBackToValidBackup(){File.WriteAllText(GamePersistence.PrimaryPath,"bad");File.WriteAllText(GamePersistence.BackupPath,JsonUtility.ToJson(Valid()));Assert.That(GamePersistence.TryReadBestEnvelope(out _,out var source,out var error),Is.True,error);Assert.That(source,Is.EqualTo(SaveLoadSource.Backup));}
    [Test] public void InvalidPrimaryAndBackupFailWithoutDeletion(){File.WriteAllText(GamePersistence.PrimaryPath,"bad");File.WriteAllText(GamePersistence.BackupPath,"also bad");Assert.That(GamePersistence.TryReadBestEnvelope(out _,out _,out _),Is.False);Assert.That(File.Exists(GamePersistence.PrimaryPath)&&File.Exists(GamePersistence.BackupPath),Is.True);}
    [Test] public void LegacyV1MigratesWithoutDeletingHistoricalKey()
    {
        var old=new GameSaveData();old.currencies.Add(new CurrencyStackData(CraftingCurrencyType.MagicToRare,7));PlayerPrefs.SetString(GamePersistence.SaveKey,JsonUtility.ToJson(old));
        Assert.That(GamePersistence.TryReadBestEnvelope(out var e,out var source,out var error),Is.True,error);Assert.That(source,Is.EqualTo(SaveLoadSource.LegacyV1));Assert.That(e.payload.currencies[0].amount,Is.EqualTo(7));Assert.That(PlayerPrefs.HasKey(GamePersistence.SaveKey),Is.True);
    }
    [Test] public void LegacyV1GearCommitsItsFirstHistoricalRollAsImplicit()
    {
        var old=new GameSaveData();
        old.inventory.Add(new GearSaveData{type=LootManager.GearType.Rings,
            rarity=LootManager.GearRarity.Magic,itemLevel=25,element=Element.Phys,
            mods=new System.Collections.Generic.List<RolledMod>
            {new(StatTypes.FireRes,3,21f,false),new(StatTypes.Life,2,35f,false)}});
        Assert.That(GamePersistence.TryMigrateLegacy(JsonUtility.ToJson(old),out var migrated,out var error),Is.True,error);
        Assert.That(migrated.schemaVersion,Is.EqualTo(4));
        var mods=migrated.payload.gearItems[0].mods;
        Assert.That(mods[0].lockedOriginal,Is.True);
        Assert.That(mods[0].value,Is.EqualTo(21f));
        Assert.That(mods[1].lockedOriginal,Is.False);
        Assert.That(mods[1].value,Is.EqualTo(35f));
    }
    [Test] public void TransactionalWriterCreatesPrimaryAndBackupForSameRun()
    {
        var method=typeof(GamePersistence).GetMethod("WriteEnvelope",BindingFlags.Static|BindingFlags.NonPublic);object[] args={Valid(),true,null};
        Assert.That((bool)method.Invoke(null,args),Is.True,args[2] as string);Assert.That(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var primary,out _),Is.True);Assert.That(GamePersistence.TryReadFile(GamePersistence.BackupPath,out var backup,out _),Is.True);Assert.That(backup.runId,Is.EqualTo(primary.runId));
    }
    [Test] public void InterruptedTemporaryFileDoesNotHideValidPrimary()
    {
        File.WriteAllText(GamePersistence.PrimaryPath,JsonUtility.ToJson(Valid()));File.WriteAllText(GamePersistence.TemporaryPath,"{interrupted");
        Assert.That(GamePersistence.TryReadBestEnvelope(out var restored,out var source,out var error),Is.True,error);Assert.That(source,Is.EqualTo(SaveLoadSource.Primary));Assert.That(restored.runId,Is.EqualTo("run-1"));Assert.That(File.Exists(GamePersistence.TemporaryPath),Is.True);
    }
    [Test] public void RestorationSuppressesDirtyAutosaveSignals()
    {
        typeof(GamePersistence).GetField("restoring",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);GamePersistence.MarkDirty();
        Assert.That(GamePersistence.IsRestoring,Is.True);Assert.That(GamePersistence.HasPendingAutosave,Is.False);Assert.That(GamePersistence.FlushPendingAutosave(true),Is.True);
    }
    [Test] public void FailedAutosavePreservesPreviousPrimary()
    {
        var method=typeof(GamePersistence).GetMethod("WriteEnvelope",BindingFlags.Static|BindingFlags.NonPublic);object[] args={Valid(),true,null};Assert.That((bool)method.Invoke(null,args),Is.True,args[2] as string);string before=File.ReadAllText(GamePersistence.PrimaryPath);
        GamePersistence.MarkDirty();Assert.That(GamePersistence.FlushPendingAutosave(true),Is.False);Assert.That(File.ReadAllText(GamePersistence.PrimaryPath),Is.EqualTo(before));Assert.That(GamePersistence.HasPendingAutosave,Is.True);
    }
    [Test] public void RichSnapshotJsonRoundTripPreservesAuthoritativeFields()
    {
        var e=Valid();e.payload.passiveRanks.Add(new PassiveRankData(0,1));e.payload.availablePassivePoints=1;e.payload.hasSelectedSkill=true;e.payload.selectedSkill=PlayerSkillId.Fireball;
        var gear=Gear("gear-rich",StatTypes.FireDmg);gear.rarity=LootManager.GearRarity.Rare;gear.itemLevel=37;gear.baseDamage=42.5f;gear.baseDamageMin=35f;gear.baseDamageMax=50f;gear.baseAttackSpeed=1.35f;gear.baseCritChance=.07f;e.payload.gearItems.Add(gear);e.payload.equippedGear.Add(new EquippedGearReference{slot=LootManager.GearType.Rings,gearId=gear.id});
        e.payload.currencies.Add(new CurrencyStackData(CraftingCurrencyType.AddRareModifier,8));e.payload.currencies.Add(new CurrencyStackData(CraftingCurrencyType.AncientReroll,3));
        var relic=new RelicData{id="relic-3",cycle=3,rarity=LootManager.GearRarity.Magic,craftableThisCycle=true};relic.modifiers.Add(new RelicModifier(RelicModifierType.MoreDamage,7.25f,true));e.payload.relicCycle=3;e.payload.relics.Add(relic);e.payload.activeRelicIds[2]=relic.id;
        string json=JsonUtility.ToJson(e);var restored=JsonUtility.FromJson<SaveEnvelope>(json);Assert.That(GamePersistence.ValidateEnvelope(restored,out var error),Is.True,error);
        Assert.That(restored.payload.combatLevel,Is.EqualTo(8));Assert.That(restored.payload.completedNormalEncounters,Is.EqualTo(4));Assert.That(restored.payload.selectedSkill,Is.EqualTo(PlayerSkillId.Fireball));Assert.That(restored.payload.gearItems[0].id,Is.EqualTo("gear-rich"));Assert.That(restored.payload.currencies[1].amount,Is.EqualTo(3));Assert.That(restored.payload.relics[0].modifiers[0].value,Is.EqualTo(7.25f));Assert.That(restored.payload.activeRelicIds[2],Is.EqualTo("relic-3"));
    }
    [Test] public void Schema4ImplicitAndExplicitSameFamilyRoundTripWithoutReroll()
    {
        var sourceObject=new GameObject("implicit roundtrip source");
        Gear created=null;
        try
        {
            var source=sourceObject.AddComponent<Gear>();
            source.Initialize(LootManager.GearType.Rings,LootManager.GearRarity.Magic,100,Element.Phys);
            source.ApplyMods(new System.Collections.Generic.List<RolledMod>
            {
                new(StatTypes.FireRes,8,8f,true),new(StatTypes.FireRes,8,9f,false)
            });
            var snapshot=GearSnapshotData.Capture(source);
            snapshot=JsonUtility.FromJson<GearSnapshotData>(JsonUtility.ToJson(snapshot));
            var envelope=Valid();envelope.payload.gearItems.Add(snapshot);
            envelope.payload.inventoryGearIds.Add(snapshot.id);
            Assert.That(GamePersistence.ValidateEnvelope(envelope,out var error),Is.True,error);
            created=snapshot.Create();
            Assert.That(created.ImplicitMod,Is.Not.Null);
            Assert.That(created.ImplicitMod.value,Is.EqualTo(8f));
            Assert.That(created.CraftingModCount,Is.EqualTo(1));
            Assert.That(created.rolledMods[1].value,Is.EqualTo(9f));
            Assert.That(created.PersistentId,Is.EqualTo(source.PersistentId));
        }
        finally
        {
            if(created!=null)UnityEngine.Object.DestroyImmediate(created.gameObject);
            UnityEngine.Object.DestroyImmediate(sourceObject);
        }
    }
    [Test] public void RapidDirtySignalsCoalesceBehindOneDebounceWindow()
    {
        GamePersistence.MarkDirty();GamePersistence.MarkDirty();GamePersistence.MarkDirty();Assert.That(GamePersistence.HasPendingAutosave,Is.True);Assert.That(GamePersistence.FlushPendingAutosave(),Is.False);Assert.That(GamePersistence.HasPendingAutosave,Is.True);Assert.That(GamePersistence.AutosaveDebounceSeconds,Is.InRange(1f,3f));
    }
    [Test] public void EncounterSeedIsStableForIdentityAndPosition()
    {
        GamePersistence.BeginFreshRunIdentity();int a=GamePersistence.EncounterSeed(12,4,false);int b=GamePersistence.EncounterSeed(12,4,false);int c=GamePersistence.EncounterSeed(12,5,false);Assert.That(b,Is.EqualTo(a));Assert.That(c,Is.Not.EqualTo(a));
    }

    void AssertInvalid(SaveEnvelope e,string contains){Assert.That(GamePersistence.ValidateEnvelope(e,out var error),Is.False);Assert.That(error,Does.Contain(contains).IgnoreCase);}
    static SaveEnvelope Valid(){var e=new SaveEnvelope{runId="run-1",runSeed=123,savedAtUtc="2026-01-01T00:00:00Z",payload=new GameStatePayload{combatLevel=8,completedNormalEncounters=4,playerLevel=3,experience=1,availablePassivePoints=2,encounterStartLife=62,encounterStartMana=17}};for(int i=0;i<RelicInventory.ActiveSlotCount;i++)e.payload.activeRelicIds.Add(string.Empty);return e;}
    static GearSnapshotData Gear(string id,StatTypes stat=StatTypes.GenericDmg)=>new(){id=id,type=LootManager.GearType.Rings,rarity=LootManager.GearRarity.Normal,itemLevel=2,element=Element.Phys,mods=new System.Collections.Generic.List<RolledMod>{new(stat,1,5,true)}};
}
