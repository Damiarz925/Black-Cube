using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class Step16ClassWeaponFoundationTests
{
    [SetUp] public void SetUp(){GameLaunchSelection.Clear();WeaponSkillBindings.ClearDeveloperFixtures();SubclassCatalog.ClearDeveloperFixtures();GamePersistence.ResetStaticStateForTests();}
    [TearDown] public void TearDown(){GameLaunchSelection.Clear();WeaponSkillBindings.ClearDeveloperFixtures();SubclassCatalog.ClearDeveloperFixtures();GamePersistence.ResetStaticStateForTests();}

    [Test] public void SixStableClassesMapToApprovedSignatureWeaponsWithoutStatPayloads()
    {
        Assert.That(PlayerClassCatalog.All.Count,Is.EqualTo(6));
        var expected=new[]{
            (PlayerClassIds.Warrior,WeaponTypeIds.Sword),(PlayerClassIds.Mage,WeaponTypeIds.Staff),
            (PlayerClassIds.Ranger,WeaponTypeIds.Bow),(PlayerClassIds.Barbarian,WeaponTypeIds.TwoHandedAxe),
            (PlayerClassIds.Priest,WeaponTypeIds.Sceptre),(PlayerClassIds.Thief,WeaponTypeIds.Dagger)};
        foreach(var pair in expected){Assert.That(PlayerClassCatalog.TryGet(pair.Item1,out var value),Is.True);Assert.That(value.SignatureWeaponTypeId,Is.EqualTo(pair.Item2));Assert.That(value.SubclassSlotIds,Has.Length.EqualTo(2));}
    }

    [Test] public void SixWeaponProfilesHaveRequiredMeleeOrderingAndRangedMetadata()
    {
        Assert.That(WeaponTypeCatalog.All.Count,Is.EqualTo(6));var dagger=WeaponTypeCatalog.Get(WeaponTypeIds.Dagger);var sword=WeaponTypeCatalog.Get(WeaponTypeIds.Sword);var axe=WeaponTypeCatalog.Get(WeaponTypeIds.TwoHandedAxe);
        Assert.That(dagger.AttacksPerSecond,Is.GreaterThan(sword.AttacksPerSecond));Assert.That(sword.AttacksPerSecond,Is.GreaterThan(axe.AttacksPerSecond));
        Assert.That(axe.BaseDamageMax,Is.GreaterThan(sword.BaseDamageMax));Assert.That(sword.BaseDamageMax,Is.GreaterThan(dagger.BaseDamageMax));
        Assert.That(WeaponTypeCatalog.Get(WeaponTypeIds.Bow).IsRanged,Is.True);Assert.That(WeaponTypeCatalog.Get(WeaponTypeIds.Bow).ProjectileMetadataHook,Is.Not.Empty);
    }

    [Test] public void NewGameSelectionAndSubclassValidationUseStableParentIdentity()
    {
        Assert.That(GameLaunchSelection.SelectNewGameClass(PlayerClassIds.Mage),Is.True);Assert.That(GameLaunchSelection.ConsumeOrDefault(),Is.EqualTo(PlayerClassIds.Mage));
        var go=new GameObject("identity");try{var state=go.AddComponent<PlayerIdentityState>();Assert.That(state.BeginNewGame(PlayerClassIds.Thief),Is.True);Assert.That(state.BaseClassId,Is.EqualTo(PlayerClassIds.Thief));
            var wrong=new SubclassDefinition("test.subclass.wrong",PlayerClassIds.Warrior,"Wrong","test.section");var right=new SubclassDefinition("test.subclass.right",PlayerClassIds.Thief,"Right","test.section");SubclassCatalog.RegisterDeveloperFixture(wrong);SubclassCatalog.RegisterDeveloperFixture(right);
            Assert.That(state.SelectSubclass(right.Id),Is.False);Assert.That(state.CompleteMilestone(PlayerIdentityState.StoryCompletionMilestoneId),Is.True);Assert.That(state.SelectSubclass(wrong.Id),Is.False);Assert.That(state.SelectSubclass(right.Id),Is.True);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void SchemaSevenMigrationAddsOnlyStableClassAndSwordWeaponIdentity()
    {
        string directory=Path.Combine(Path.GetTempPath(),"BlackCube-Step16-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);GamePersistence.SaveDirectoryOverride=directory;
        try
        {
            var envelope=new SaveEnvelope{schemaVersion=7,runId="legacy",runSeed=7,savedAtUtc="2026-01-01T00:00:00Z",payload=new GameStatePayload{combatLevel=1,playerLevel=1,encounterStartLife=100,encounterStartMana=100}};
            for(int i=0;i<RelicInventory.ActiveSlotCount;i++)envelope.payload.activeRelicIds.Add(string.Empty);
            envelope.payload.gearItems.Add(new GearSnapshotData{id="weapon",type=LootManager.GearType.Weapons,rarity=LootManager.GearRarity.Normal,originRarity=LootManager.GearRarity.Normal,currentCraftingPotential=6,maximumCraftingPotential=6,itemLevel=1,element=Element.Phys,baseDamage=22,baseDamageMin=18,baseDamageMax=27,baseAttackSpeed=.45f,baseCritChance=.05f,legacyAffixRules=true,mods=new(){new RolledMod(StatTypes.GenericDmg,5,7,true)}});envelope.payload.inventoryGearIds.Add("weapon");
            File.WriteAllText(GamePersistence.PrimaryPath,JsonUtility.ToJson(envelope));Assert.That(GamePersistence.TryReadFile(GamePersistence.PrimaryPath,out var migrated,out var error),Is.True,error);
            Assert.That(migrated.schemaVersion,Is.EqualTo(GamePersistence.SchemaVersion));Assert.That(migrated.payload.availablePassivePoints,Is.EqualTo(1));Assert.That(migrated.payload.baseClassId,Is.EqualTo(PlayerClassIds.Warrior));Assert.That(migrated.payload.gearItems[0].weaponTypeId,Is.EqualTo(WeaponTypeIds.Sword));Assert.That(migrated.payload.gearItems[0].baseDamageMin,Is.EqualTo(18));
        }
        finally{GamePersistence.SaveDirectoryOverride=null;if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }

    [Test] public void WeaponSkillFixtureProvidesTwoSlotsAndWeaponSwapClearsQueue()
    {
        WeaponSkillBindings.RegisterDeveloperFixture(WeaponTypeIds.Sword,PlayerSkillId.HeavyStrike,PlayerSkillId.IceStrike);WeaponSkillBindings.RegisterDeveloperFixture(WeaponTypeIds.Dagger,PlayerSkillId.Shiv,PlayerSkillId.Envenom);
        var go=new GameObject("player");var swordGo=new GameObject("sword");var daggerGo=new GameObject("dagger");
        try
        {
            go.AddComponent<StatsComponent>();go.AddComponent<ManaComponent>();var player=go.AddComponent<PlayerController>();var skills=go.AddComponent<PlayerSkillController>();typeof(PlayerSkillController).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(skills,null);
            var sword=swordGo.AddComponent<Gear>();sword.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Sword);player.EquipWeapon(sword);Assert.That(skills.WeaponSkills.Count,Is.EqualTo(2));
            typeof(PlayerSkillController).GetField("<QueuedSkill>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(skills,skills.WeaponSkills[0]);
            var dagger=daggerGo.AddComponent<Gear>();dagger.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Dagger);player.EquipWeapon(dagger);
            Assert.That(skills.HasQueuedSkill,Is.False);Assert.That(skills.WeaponSkills.Select(x=>x.id),Is.EqualTo(new[]{PlayerSkillId.Shiv,PlayerSkillId.Envenom}));
        }
        finally{UnityEngine.Object.DestroyImmediate(daggerGo);UnityEngine.Object.DestroyImmediate(swordGo);UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void AffixesCanOptionallyRestrictStableWeaponTypesAndPassiveMetadataIsTopologyAgnostic()
    {
        var affix=new AffixDefinitions{allowedWeaponTypeIds=new[]{WeaponTypeIds.Bow},tiers=new(){new AffixTier{minItemLevel=1}}};Assert.That(affix.AllowsWeaponType(WeaponTypeIds.Bow),Is.True);Assert.That(affix.AllowsWeaponType(WeaponTypeIds.Sword),Is.False);
        var metadata=new PassiveExtensionMetadata{StableSectionId="test.section",ClassStartIds=new[]{"passive-start.ranger"},Affinities=new[]{PassiveAffinity.Projectile},RequiredSubclassId="test.subclass",SpecializationGroupId="test.group",MutuallyExclusive=true,IsTravelNode=true};
        Assert.That(metadata.Affinities,Does.Contain(PassiveAffinity.Projectile));Assert.That(metadata.IsTravelNode,Is.True);Assert.That(PassiveTreeDefinition.Edges.Count,Is.GreaterThan(PassiveTreeDefinition.NodeCount));
    }
}
