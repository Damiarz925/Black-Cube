using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class Step17FoundationTests
{
    [Test] public void NewStableStatIdsAreAppendedAndCooldownRecoveryIsUntouched()
    {Assert.That((int)StatTypes.CooldownRecovery,Is.EqualTo(84));Assert.That((int)StatTypes.CastSpeed,Is.EqualTo(120));Assert.That((int)StatTypes.RageDecayReduction,Is.EqualTo(125));}

    [Test] public void ProjectileTravelFormulaAndPrecisionDefaultsAreCentralized()
    {Assert.That(WeaponMechanicProfile.ProjectileTravelTime(0),Is.EqualTo(1).Within(.001));Assert.That(WeaponMechanicProfile.ProjectileTravelTime(1),Is.EqualTo(.5).Within(.001));Assert.That(WeaponMechanicProfile.PrecisionChance(.2f),Is.EqualTo(.3f).Within(.001));Assert.That(WeaponMechanicProfile.PrecisionMultiplier(.5f),Is.EqualTo(2.25f).Within(.001));Assert.That(WeaponMechanicProfile.ProjectileBarrageSpacing,Is.EqualTo(.1f));}

    [Test] public void StaffCooldownUsesCastSpeedButNeverDropsBelowClamp()
    {
        var go=new GameObject("staff cooldown");try{var stats=go.AddComponent<StatsComponent>();go.AddComponent<ManaComponent>();var controller=go.AddComponent<PlayerSkillController>();var skill=new PlayerSkillDefinition{baseCooldown=4,scalesWithCastSpeed=true,castMode=PlayerSkillCastMode.AutoCooldown};Assert.That(controller.EffectiveCooldown(skill),Is.EqualTo(4).Within(.001));stats.AddModifier(new StatModifier(StatTypes.CastSpeed,StatOp.Flat,100,this));Assert.That(controller.EffectiveCooldown(skill),Is.EqualTo(2).Within(.001));skill.baseCooldown=.01f;Assert.That(controller.EffectiveCooldown(skill),Is.EqualTo(PlayerSkillController.MinimumAutoCooldown));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void WeaponDpsUsesOnlyLocalFinalDamageAndAps()
    {
        var go=new GameObject("weapon");try{var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Sword);gear.BaseDamageMin=10;gear.BaseDamageMax=20;gear.BaseAttackSpeed=2;Assert.That(gear.GetAverageWeaponDps(),Is.EqualTo(30).Within(.001));gear.ApplyMods(new List<RolledMod>{new(StatTypes.PhysDmg,1,100,true),new(StatTypes.AttackSpeed,1,100)});Assert.That(gear.GetAverageWeaponDps(),Is.EqualTo(120).Within(.001));}finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void StaffAutoCooldownsAreIndependentAndWaitReadyForMana()
    {
        var go=new GameObject("staff auto cooldowns");try
        {
            var stats=go.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Mana,100);var mana=go.AddComponent<ManaComponent>();InvokeAwake(mana);var controller=go.AddComponent<PlayerSkillController>();InvokeAwake(controller);
            var first=new PlayerSkillDefinition{id=PlayerSkillId.Fireball,displayName="Fixture One",castMode=PlayerSkillCastMode.AutoCooldown,baseCooldown=1,manaCost=10,scalesWithCastSpeed=true};
            var second=new PlayerSkillDefinition{id=PlayerSkillId.IceStrike,displayName="Fixture Two",castMode=PlayerSkillCastMode.AutoCooldown,baseCooldown=2,manaCost=10,scalesWithCastSpeed=true};
            controller.ConfigureDeveloperAutoSkills(first,second);int firstCasts=0,secondCasts=0;
            controller.TickAutoCooldowns(1,skill=>{if(skill==first)firstCasts++;if(skill==second)secondCasts++;return true;});
            Assert.That(firstCasts,Is.EqualTo(1));Assert.That(secondCasts,Is.Zero);Assert.That(controller.CooldownRemaining(1),Is.EqualTo(1).Within(.001));
            controller.TickAutoCooldowns(1,skill=>{if(skill==first)firstCasts++;if(skill==second)secondCasts++;return true;});
            Assert.That(firstCasts,Is.EqualTo(2));Assert.That(secondCasts,Is.EqualTo(1));
            controller.Mana.SpendUpTo(controller.Mana.CurrentMana);controller.TickAutoCooldowns(2,_=>{Assert.Fail("A Mana-starved ready skill cast.");return true;});
            Assert.That(controller.AutoSkillReady(0),Is.True);Assert.That(controller.AutoSkillReady(1),Is.True);
            controller.Mana.RestoreFull();int resumed=0;controller.TickAutoCooldowns(.01f,_=>{resumed++;return true;});Assert.That(resumed,Is.EqualTo(2));
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }

    [Test] public void AxeRageGenerationDelayDecayBenefitAndWeaponSwapAreCoherent()
    {
        var actor=new GameObject("rage actor");var axeObject=new GameObject("axe");var swordObject=new GameObject("sword");try
        {
            actor.AddComponent<StatsComponent>();var player=actor.AddComponent<PlayerController>();var rage=actor.AddComponent<RageState>();InvokeAwake(rage);
            var axe=axeObject.AddComponent<Gear>();axe.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.TwoHandedAxe);
            var sword=swordObject.AddComponent<Gear>();sword.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Sword);
            player.EquipWeapon(axe);rage.GainFromDamageDealt(20,100);Assert.That(rage.Rage,Is.EqualTo(15));Assert.That(rage.SustainedDamageMultiplier,Is.GreaterThan(1));Assert.That(rage.IncomingDamageMultiplier,Is.LessThan(1));
            rage.Tick(WeaponMechanicProfile.RageDecayDelay);Assert.That(rage.Rage,Is.EqualTo(15));rage.Tick(.5f);Assert.That(rage.Rage,Is.EqualTo(10).Within(.001));
            player.EquipWeapon(sword);Assert.That(rage.Rage,Is.Zero);Assert.That(rage.FinisherArmed,Is.False);
        }finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(axeObject);UnityEngine.Object.DestroyImmediate(swordObject);}
    }

    [Test] public void RageFinisherIsManualEmpowersOneEventAndConsumesOnlyAfterResolution()
    {
        var manager=new GameObject("progression");var actor=new GameObject("rage finisher actor");var axeObject=new GameObject("finisher axe");try
        {
            var identity=manager.AddComponent<PlayerIdentityState>();identity.BeginNewGame(PlayerClassIds.Barbarian);var progression=manager.AddComponent<PlayerProgression>();
            actor.AddComponent<StatsComponent>();var player=actor.AddComponent<PlayerController>();var rage=actor.AddComponent<RageState>();InvokeAwake(rage);var axe=axeObject.AddComponent<Gear>();axe.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.TwoHandedAxe);player.EquipWeapon(axe);
            int target=PassiveTreeDefinition.FindIndexForTest(PassiveKeystone.RageFinisher);var path=PathFromStart(PlayerClassIds.Barbarian,target);var ranks=new int[PassiveTreeDefinition.NodeCount];foreach(int id in path)ranks[id]=1;Assert.That(progression.RestoreProgression(100,0,100-path.Count,ranks),Is.True);
            Assert.That(rage.FinisherAvailable,Is.False);for(int i=0;i<7;i++)rage.GainFromDamageDealt(20,100);Assert.That(rage.Rage,Is.EqualTo(100));Assert.That(rage.FinisherArmed,Is.False);Assert.That(rage.FinisherAvailable,Is.True);Assert.That(rage.TryArmFinisher(),Is.True);Assert.That(rage.BeginAttackEventMultiplier(),Is.EqualTo(WeaponMechanicProfile.RageFinisherMoreMultiplier));rage.CompleteAttackEvent(false);Assert.That(rage.Rage,Is.EqualTo(100));Assert.That(rage.FinisherArmed,Is.True);rage.CompleteAttackEvent(true);Assert.That(rage.Rage,Is.Zero);Assert.That(rage.FinisherArmed,Is.False);
        }finally{UnityEngine.Object.DestroyImmediate(manager);UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(axeObject);}
    }

    [Test] public void SixSlotPathsAreIndependentAndExpandableThroughOneAuthority()
    {
        string old=GamePersistence.SaveDirectoryOverride;string path=Path.Combine(Path.GetTempPath(),"BlackCubeStep17-"+Guid.NewGuid().ToString("N"));try{GamePersistence.SaveDirectoryOverride=path;for(int i=1;i<=GamePersistence.CharacterSlotCount;i++){Assert.That(GamePersistence.SlotPath(i),Does.EndWith($"slot-{i:00}.json"));Assert.That(GamePersistence.HasSlot(i),Is.False);}Assert.Throws<ArgumentOutOfRangeException>(()=>GamePersistence.SlotPath(7));}finally{GamePersistence.SaveDirectoryOverride=old;GamePersistence.ResetStaticStateForTests();if(Directory.Exists(path))Directory.Delete(path,true);}
    }

    [Test] public void LegacyTreeMigrationReachesSchemaTenAndRefundsLevelTotal()
    {
        string path=Path.Combine(Path.GetTempPath(),"BlackCubeStep17Migration-"+Guid.NewGuid().ToString("N")+".json");try{var e=new SaveEnvelope{schemaVersion=8,runId="run",runSeed=2,savedAtUtc=DateTime.UtcNow.ToString("O"),payload=new GameStatePayload{playerLevel=72,availablePassivePoints=0,encounterStartLife=100,encounterStartMana=100}};e.payload.passiveRanks.Add(new PassiveRankData(0,1));for(int i=0;i<RelicInventory.ActiveSlotCount;i++)e.payload.activeRelicIds.Add(string.Empty);File.WriteAllText(path,JsonUtility.ToJson(e));Assert.That(GamePersistence.TryReadFile(path,out var migrated,out var error),Is.True,error);Assert.That(migrated.schemaVersion,Is.EqualTo(10));Assert.That(migrated.payload.passiveRanks,Is.Empty);Assert.That(migrated.payload.availablePassivePoints,Is.EqualTo(72));}finally{if(File.Exists(path))File.Delete(path);}
    }

    [Test] public void LegacySingleFileMigratesOnceIntoSlotOneWithoutDeletingSource()
    {
        string old=GamePersistence.SaveDirectoryOverride;string path=Path.Combine(Path.GetTempPath(),"BlackCubeStep17Legacy-"+Guid.NewGuid().ToString("N"));try
        {
            Directory.CreateDirectory(path);GamePersistence.SaveDirectoryOverride=path;GamePersistence.ResetStaticStateForTests();var envelope=ValidEnvelope(PlayerClassIds.Warrior,1);File.WriteAllText(GamePersistence.LegacyPrimaryPath,JsonUtility.ToJson(envelope));
            Assert.That(GamePersistence.HasSave,Is.True);Assert.That(File.Exists(GamePersistence.SlotPath(1)),Is.True);Assert.That(File.Exists(GamePersistence.LegacyPrimaryPath),Is.True);
            var summary=GamePersistence.GetSlotSummary(1);Assert.That(summary.occupied,Is.True);Assert.That(summary.baseClassId,Is.EqualTo(PlayerClassIds.Warrior));Assert.That(GamePersistence.GetSlotSummary(2).occupied,Is.False);
        }finally{GamePersistence.SaveDirectoryOverride=old;GamePersistence.ResetStaticStateForTests();if(Directory.Exists(path))Directory.Delete(path,true);}
    }

    static SaveEnvelope ValidEnvelope(string classId,int level)
    {
        var payload=new GameStatePayload{baseClassId=classId,playerLevel=level,availablePassivePoints=level,combatLevel=1,encounterStartLife=100,encounterStartMana=100};
        for(int i=0;i<RelicInventory.ActiveSlotCount;i++)payload.activeRelicIds.Add(string.Empty);
        return new SaveEnvelope{schemaVersion=GamePersistence.SchemaVersion,runId=Guid.NewGuid().ToString("N"),runSeed=17,savedAtUtc=DateTime.UtcNow.ToString("O"),payload=payload};
    }
    static List<int> PathFromStart(string classId,int target)
    {
        int start=PassiveTreeDefinition.StartNodeId(classId);var parent=new int[PassiveTreeDefinition.NodeCount];for(int i=0;i<parent.Length;i++)parent[i]=-2;parent[start]=-1;var queue=new Queue<int>();queue.Enqueue(start);
        while(queue.Count>0&&parent[target]==-2){int current=queue.Dequeue();foreach(int next in PassiveTreeDefinition.AdjacentNodeIds(current)){if(parent[next]!=-2||PassiveTreeDefinition.IsClassStart(next)&&next!=start)continue;parent[next]=current;queue.Enqueue(next);}}
        var path=new List<int>();for(int id=target;id!=start;id=parent[id])path.Add(id);path.Reverse();return path;
    }
    static void InvokeAwake(object target)=>target.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(target,null);
}
