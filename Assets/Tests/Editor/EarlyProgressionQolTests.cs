using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class EarlyProgressionQolTests
{
    [Test] public void XpCurveTargetsEarlyLevelsAndPreservesLevelTenOnward()
    {
        for(int level=1;level<=5;level++)Assert.That(EarlyProgressionRules.Requirement(level),Is.LessThan(EarlyProgressionRules.LegacyRequirement(level)*.55));
        for(int level=1;level<99;level++)Assert.That(EarlyProgressionRules.Requirement(level+1),Is.GreaterThan(EarlyProgressionRules.Requirement(level)));
        for(int level=10;level<100;level++)Assert.That(EarlyProgressionRules.Requirement(level),Is.EqualTo(EarlyProgressionRules.LegacyRequirement(level)));
    }
    [Test] public void CombatLevelXpRewardKeepsTheExistingCapBoundary()
    {
        var go=new GameObject("xp reward",typeof(PlayerProgression));
        try{var p=go.GetComponent<PlayerProgression>();Assert.That(p.EnemyReward(1,EnemyAI.EnemyRarity.Normal,false),Is.GreaterThan(0));Assert.That(p.EnemyReward(100,EnemyAI.EnemyRarity.Normal,false),Is.Zero);Assert.That(p.EnemyReward(350,EnemyAI.EnemyRarity.Legendary,true),Is.Zero);}
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void OldXpAndReductionRollsMigrateOnce()
    {
        var e=new SaveEnvelope{schemaVersion=15,payload=new GameStatePayload{playerLevel=1,availablePassivePoints=1,experience=20}};
        var gear=new GearSnapshotData();gear.mods.Add(new RolledMod(StatTypes.ReducedShockEffect,1,.25f));gear.mods.Add(new RolledMod(StatTypes.ReducedChillEffect,1,.3125f){isEmpowered=true});e.payload.gearItems.Add(gear);
        GamePersistence.MigrateSchema15(e);Assert.That(e.schemaVersion,Is.EqualTo(16));Assert.That(e.payload.playerLevel,Is.GreaterThan(1));Assert.That(e.payload.availablePassivePoints,Is.EqualTo(e.payload.playerLevel));
        Assert.That(gear.mods[0].value,Is.EqualTo(25));Assert.That(gear.mods[1].value,Is.EqualTo(31.25));GamePersistence.MigrateSchema15(e);Assert.That(gear.mods[0].value,Is.EqualTo(25));
    }
    [Test] public void EveryUniqueIsEligibleAtLevelOneAndEveryBandRollValidates()
    {
        Assert.That(UniqueCatalog.All.Length,Is.EqualTo(13));Assert.That(UniqueTierProfileSO.Current.database,Is.Not.Null);
        foreach(var d in UniqueCatalog.All)foreach(int level in new[]{1,19,20,39,40,59,60,79,80,100})foreach(float roll in new[]{0f,.5f,1f})
        {
            var gear=UniqueCatalog.Create(d.id,level,fixedRoll:roll);
            try{Assert.That(gear,Is.Not.Null,d.id);Assert.That(UniqueCatalog.Validate(gear.UniqueData,gear.ItemType,gear.WeaponTypeId,gear.BaseElement,level,gear.rolledMods),Is.True,d.id+" / "+level);Assert.That(gear.UniqueData.rollVersion,Is.EqualTo(1));if(d.slot==LootManager.GearType.Weapons){Assert.That(gear.BaseDamageMin,Is.GreaterThan(0));Assert.That(gear.BaseAttackSpeed,Is.GreaterThan(0));}}
            finally{if(gear!=null)UnityEngine.Object.DestroyImmediate(gear.gameObject);}
        }
    }
    [Test] public void UniqueProtectionWinsEveryAdvancedDiscardRule()
    {
        var p=new AdvancedLootFilter{enabled=true,keptRarities=0,keptWeapons=0,keptElements=0};
        var g=UniqueCatalog.Create(UniqueCatalog.All[0].id,1);
        try{p.For(g.ItemType).enabled=true;p.For(g.ItemType).requirements.Add(new LootModRequirement{stat=StatTypes.ColdDmg});Assert.That(p.Keeps(g),Is.True);}
        finally{UnityEngine.Object.DestroyImmediate(g.gameObject);}
    }
    [Test] public void UniquePickupSurvivesCombinedAdvancedAndLegacyDiscardRules()
    {
        var host=new GameObject("pickup test");host.SetActive(false);var inventory=host.AddComponent<Inventory>();var gear=UniqueCatalog.Create(UniqueCatalog.All[0].id,1);
        try
        {
            inventory.AdvancedFilter.enabled=true;inventory.AdvancedFilter.keptRarities=0;inventory.AdvancedFilter.keptElements=0;inventory.AdvancedFilter.keptWeapons=0;
            foreach(string field in new[]{"filterLevelEnabled","filterRarityEnabled","filterModMismatchEnabled"})typeof(Inventory).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,true);
            typeof(Inventory).GetField("filterRarity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,LootManager.GearRarity.Unique);
            Assert.That(inventory.MatchesFilter(gear),Is.False);Assert.That(inventory.Pickup(gear),Is.True);Assert.That(inventory.Items.Contains(gear),Is.True);Assert.That(gear,Is.Not.Null);Assert.That(inventory.Pickup(gear),Is.False);
        }
        finally{UnityEngine.Object.DestroyImmediate(host);if(gear!=null)UnityEngine.Object.DestroyImmediate(gear.gameObject);}
    }
    [Test] public void ClearingModsPreservesRarityElementWeaponAndOtherSlots()
    {
        var p=new AdvancedLootFilter{enabled=true,keptRarities=4,keptWeapons=2,keptElements=8,autoDismantleFilteredItems=false};
        p.For(LootManager.GearType.Boots).requirements.Add(new LootModRequirement{stat=StatTypes.LightDmg});p.For(LootManager.GearType.Weapons,WeaponTypeIds.Bow).requirements.Add(new LootModRequirement{stat=StatTypes.LightDmg});p.For(LootManager.GearType.Weapons,WeaponTypeIds.Staff).requirements.Add(new LootModRequirement{stat=StatTypes.LightDmg});
        p.ClearSlotModifiers(LootManager.GearType.Weapons,WeaponTypeIds.Bow);Assert.That(p.For(LootManager.GearType.Weapons,WeaponTypeIds.Staff).requirements.Count,Is.EqualTo(1));Assert.That(p.For(LootManager.GearType.Boots).requirements.Count,Is.EqualTo(1));
        p.ClearAllModifiers();Assert.That(p.itemTypes.All(f=>f.requirements.Count==0),Is.True);Assert.That(p.keptRarities,Is.EqualTo(4));Assert.That(p.keptWeapons,Is.EqualTo(2));Assert.That(p.keptElements,Is.EqualTo(8));Assert.That(p.enabled,Is.True);Assert.That(p.autoDismantleFilteredItems,Is.False);
    }
    [Test] public void ReductionLaddersUsePercentagePointsNotTinyFractions()
    {
        var db=UniqueTierProfileSO.Current.database;
        foreach(var stat in new[]{StatTypes.ReducedShockEffect,StatTypes.ReducedChillEffect})
        {var tiers=ModManager.ApplicableTiers(db.GetDefinition(stat),LootManager.GearType.Helmets,null);Assert.That(tiers.Single(t=>t.tierIndex==1).maxValue,Is.EqualTo(25));Assert.That(tiers.Single(t=>t.tierIndex==5).minValue,Is.EqualTo(4));}
    }
    [Test] public void MageAndPriestSustainMovedWithoutLosingChoices()
    {
        var mage=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Mage);Assert.That(mage.Tiers[0].Left.B.Effects.Any(e=>e.Stat==StatTypes.LifeRegeneration),Is.True);Assert.That(mage.Tiers[1].Right.C.Effects.Any(e=>e.Stat==StatTypes.ManaRegeneration),Is.True);
        var priest=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Priest);Assert.That(priest.Tiers[0].Left.B.Effects.Any(e=>e.Stat==StatTypes.LifeRecoveryEffect),Is.True);Assert.That(priest.Tiers[1].Left.A.Effects.Any(e=>e.Stat==StatTypes.VoidRes),Is.True);
    }
    [Test] public void TriggerSwitchesPersistIndependentlyFromStaffAutocast()
    {
        var go=new GameObject("switch test",typeof(StatsComponent),typeof(ManaComponent),typeof(PlayerSkillController));var c=go.GetComponent<PlayerSkillController>();
        try{c.ToggleRelicTrigger(PlayerSkillId.StaffShockBarrage);Assert.That(c.RelicTriggerEnabled(PlayerSkillId.StaffShockBarrage),Is.False);Assert.That(c.RelicTriggerEnabled(PlayerSkillId.StaffFireball),Is.True);Assert.That(c.AutocastEnabled(0),Is.True);var p=JsonUtility.FromJson<GameStatePayload>(JsonUtility.ToJson(new GameStatePayload{disabledRelicTriggers=c.CaptureDisabledRelicTriggers()}));c.RestoreDisabledRelicTriggers(null);c.RestoreDisabledRelicTriggers(p.disabledRelicTriggers);Assert.That(c.RelicTriggerEnabled(PlayerSkillId.StaffShockBarrage),Is.False);}
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test] public void SetupCaptureRestoreCannotCancelOrRepeatGrant()
    {
        var go=new GameObject("setup test",typeof(RebirthManager));var manager=go.GetComponent<RebirthManager>();
        try{var p=new GameStatePayload{rebirthPhase=RebirthPhase.Review,rebirthReachedLevel=100,rebirthWeapon=WeaponTypeIds.Staff};manager.RestoreSetup(p,new Dictionary<string,Gear>());manager.Cancel();Assert.That(manager.Phase,Is.EqualTo(RebirthPhase.Review));Assert.That(manager.ConfirmRebirth(),Is.False);var result=new GameStatePayload();manager.CaptureSetup(result);Assert.That(result.rebirthWeapon,Is.EqualTo(WeaponTypeIds.Staff));Assert.That(result.rebirthReachedLevel,Is.EqualTo(100));manager.ResetForNewGame();Assert.That(manager.IsSetup,Is.False);}
        finally{UnityEngine.Object.DestroyImmediate(go);Time.timeScale=1;}
    }
    [Test] public void LiveCrossWeaponRelicTriggerSpendsNormalManaOnceAndHonoursOffAndInsufficientMana()
    {
        var objects=new List<GameObject>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var previous=RelicInventory.Instance;float oldScale=Time.timeScale;
        GameObject Actor(){var go=new GameObject("trigger fixture");go.SetActive(false);objects.Add(go);return go;}
        void Relics(RelicInventory value)=>typeof(RelicInventory).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{value});
        try
        {
            Time.timeScale=1;var inventory=Actor().AddComponent<RelicInventory>();Relics(inventory);
            var a=new RelicData{id="a",cycle=1,modifiers=new(){new(RelicModifierType.TriggerFrostJudgment,1,false)}};var b=new RelicData{id="b",cycle=1,modifiers=new(){new(RelicModifierType.TriggerFrostJudgment,1,false)}};inventory.Restore(new(){a,b},1,new[]{0,1,-1,-1,-1,-1,-1,-1});
            var player=Actor();var stats=player.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,1000);stats.SetBaseStat(StatTypes.Mana,100);
            var hp=player.AddComponent<HealthComponent>();hp.ConfigureIsolatedStats(stats);var pc=player.AddComponent<PlayerController>();
            var weapon=Actor().AddComponent<Gear>();weapon.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1,Element.Phys,WeaponTypeIds.Sword);weapon.BaseDamage=100;pc.ConfigureItemPreview(1,weapon);
            var controller=player.AddComponent<PlayerSkillController>();typeof(PlayerSkillController).GetMethod("Awake",flags).Invoke(controller,null);controller.Mana.ConfigureIsolatedStats(stats);controller.RestoreAutocast(false,false);
            var enemy=Actor();var es=enemy.AddComponent<StatsComponent>();es.SetBaseStat(StatTypes.Life,10000);var eh=enemy.AddComponent<HealthComponent>();eh.ConfigureIsolatedStats(es);var status=enemy.AddComponent<StatusController>();var receiver=enemy.AddComponent<DamageReceiver>();typeof(DamageReceiver).GetMethod("Awake",flags).Invoke(receiver,null);
            var battle=Actor().AddComponent<BattleManager>();void Bind(string name,object value)=>typeof(BattleManager).GetField(name,flags).SetValue(battle,value);
            Bind("player",player);Bind("playerStats",stats);Bind("playerHealth",hp);Bind("playerController",pc);Bind("currentEnemy",enemy);Bind("enemyStats",es);Bind("enemyHealth",eh);Bind("enemyDamageReceiver",receiver);Bind("enemyStatusCont",status);
            var dispatch=typeof(BattleManager).GetMethod("ResolveRelicSkillTriggers",flags);void Cast()=>dispatch.Invoke(battle,new object[]{controller});
            controller.ToggleRelicTrigger(PlayerSkillId.SceptreFrostJudgment);Cast();Assert.That(controller.Mana.CurrentMana,Is.EqualTo(100));Assert.That(eh.CurrentLife,Is.EqualTo(10000));
            controller.ToggleRelicTrigger(PlayerSkillId.SceptreFrostJudgment);Cast();Assert.That(controller.Mana.CurrentMana,Is.EqualTo(60));Assert.That(eh.CurrentLife,Is.LessThan(10000));
            controller.Mana.SpendUpTo(60);float life=eh.CurrentLife;Cast();Assert.That(controller.Mana.CurrentMana,Is.Zero);Assert.That(eh.CurrentLife,Is.EqualTo(life));
            controller.Mana.Restore(40);Cast();Assert.That(controller.Mana.CurrentMana,Is.Zero);Assert.That(eh.CurrentLife,Is.LessThan(life));Assert.That(controller.AutocastEnabled(0),Is.False);
        }
        finally{foreach(var go in objects.AsEnumerable().Reverse())if(go!=null)UnityEngine.Object.DestroyImmediate(go);Relics(previous==null?null:previous);Time.timeScale=oldScale;}
    }
}
