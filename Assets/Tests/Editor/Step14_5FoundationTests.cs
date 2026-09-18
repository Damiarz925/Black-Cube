using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class Step14_5FoundationTests
{
    readonly List<GameObject> cleanup=new();
    UnityEngine.Random.State randomState;

    [SetUp] public void SetUp(){randomState=UnityEngine.Random.state;Time.timeScale=1f;SetStatic(typeof(SkillTreeUI),"IsOpen",false);SetStatic(typeof(PlayerSkillMenuUI),"IsOpen",false);}
    [TearDown] public void TearDown(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)UnityEngine.Object.DestroyImmediate(cleanup[i]);cleanup.Clear();UnityEngine.Random.state=randomState;Time.timeScale=1f;}

    [TestCase(LootManager.GearRarity.Normal,6)]
    [TestCase(LootManager.GearRarity.Magic,8)]
    [TestCase(LootManager.GearRarity.Rare,10)]
    [TestCase(LootManager.GearRarity.Legendary,14)]
    public void NaturalRarityOwnsCentralizedPotential(LootManager.GearRarity rarity,int expected)
    {
        var gear=Gear(rarity);Assert.That(gear.OriginRarity,Is.EqualTo(rarity));
        Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(expected));Assert.That(gear.MaximumCraftingPotential,Is.EqualTo(expected));
    }

    [Test] public void RarityUpgradesSpendPotentialWithoutChangingNormalOriginMaximum()
    {
        var mods=CreateModManager();var gear=Gear(LootManager.GearRarity.Normal);
        var natural=mods.RollEquipmentModsForItem(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement);gear.ApplyMods(natural);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.NormalToMagic,gear,mods),Is.True);
        Assert.That(gear.ItemRarity,Is.EqualTo(LootManager.GearRarity.Magic));Assert.That(gear.OriginRarity,Is.EqualTo(LootManager.GearRarity.Normal));
        Assert.That(gear.MaximumCraftingPotential,Is.EqualTo(6));Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(5));
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.MagicToRare,gear,mods),Is.True);
        Assert.That(gear.ItemRarity,Is.EqualTo(LootManager.GearRarity.Rare));Assert.That(gear.MaximumCraftingPotential,Is.EqualTo(6));Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(4));
        Gear naturalLegendary=Gear(LootManager.GearRarity.Legendary);Assert.That(naturalLegendary.MaximumCraftingPotential,Is.EqualTo(14));
    }

    [Test] public void FailedOrZeroPotentialCraftPreservesPotentialCurrencyAndImplicit()
    {
        CreateModManager();var inventory=Track(new GameObject("currency",typeof(CurrencyInventory))).GetComponent<CurrencyInventory>();
        var gear=Gear(LootManager.GearRarity.Normal);var implicitMod=new RolledMod(StatTypes.GenericDmg,1,5,true);gear.ApplyMods(new List<RolledMod>{implicitMod});gear.RestoreCraftingState(LootManager.GearRarity.Normal,0,6);
        inventory.Add(CraftingCurrencyType.NormalToMagic);inventory.Arm(CraftingCurrencyType.NormalToMagic);
        Assert.That(inventory.TryApplyArmedToGear(gear),Is.False);Assert.That(inventory.Count(CraftingCurrencyType.NormalToMagic),Is.EqualTo(1));
        Assert.That(gear.CurrentCraftingPotential,Is.Zero);Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
    }

    [TestCase(119,0)] [TestCase(120,1)] [TestCase(160,2)] [TestCase(210,3)]
    [TestCase(260,4)] [TestCase(310,5)] [TestCase(360,6)] [TestCase(999,6)]
    public void EmpowermentThresholdsAreCentralized(int level,int expected)
        =>Assert.That(EmpowermentProgressionProfile.MaximumEmpoweredModifiers(level),Is.EqualTo(expected));

    [Test] public void EmpowermentRequiresOrdinaryNumericT1AndLocksTheExactPersistedResult()
    {
        var manager=CreateModManager();var gear=Gear(LootManager.GearRarity.Legendary);
        var definition=manager.Database.GetDefinition(StatTypes.Life);definition.empowerable=true;
        var tier=ModManager.ApplicableTiers(definition,gear.ItemType).Find(x=>x.tierIndex==1);Assert.That(tier,Is.Not.Null);
        var implicitMod=new RolledMod(StatTypes.GenericDmg,1,5,true);
        var t1=new RolledMod(StatTypes.Life,1,tier.minValue,false);gear.ApplyMods(new List<RolledMod>{implicitMod,t1});
        Assert.That(EmpowermentCrafting.TryApply(gear,119,0,0,0),Is.False);
        Assert.That(EmpowermentCrafting.TryApply(gear,120,0,0,0),Is.True);Assert.That(t1.isEmpowered,Is.True);Assert.That(t1.value,Is.EqualTo(Mathf.Round(tier.minValue*125f)/100f).Within(.001f));
        Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RerollRareModifier,gear),Is.False);
        Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RemoveRareModifier,gear),Is.False);
        var snapshot=JsonUtility.FromJson<GearSnapshotData>(JsonUtility.ToJson(GearSnapshotData.Capture(gear)));
        Assert.That(snapshot.mods.Single(x=>x.isEmpowered).value,Is.EqualTo(t1.value));Assert.That(snapshot.currentCraftingPotential,Is.EqualTo(14));
    }

    [Test] public void T2ImplicitDiscreteAndBossSpecialModifiersCannotEmpower()
    {
        var manager=CreateModManager();var gear=Gear(LootManager.GearRarity.Legendary);manager.Database.GetDefinition(StatTypes.Life).empowerable=true;
        gear.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,5,true),new(StatTypes.Life,2,10),new(StatTypes.Plus1Phys,1,1),new(StatTypes.FireRes,1,10){isBossSpecial=true,specialPoolId="fixture",specialModifierId="fixture-mod",specialAffixSide=AffixSide.Suffix}});
        Assert.That(EmpowermentCrafting.TryApply(gear,360,0,0,0),Is.False);
    }

    [Test] public void CatalystConsumesOnlyAfterSuccessfulEmpowerment()
    {
        var manager=CreateModManager();manager.Database.GetDefinition(StatTypes.Life).empowerable=true;
        var gm=Track(new GameObject("game",typeof(GameManager))).GetComponent<GameManager>();SetStatic(typeof(GameManager),"Instance",gm);SetField(gm,"currentZoneLevel",120);
        var inventory=Track(new GameObject("currency",typeof(CurrencyInventory))).GetComponent<CurrencyInventory>();inventory.Add(CraftingCurrencyType.EmpowermentCatalyst,2);
        var invalid=Gear(LootManager.GearRarity.Legendary);invalid.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,5,true)});inventory.Arm(CraftingCurrencyType.EmpowermentCatalyst);
        Assert.That(inventory.TryApplyArmedToGear(invalid),Is.False);Assert.That(inventory.Count(CraftingCurrencyType.EmpowermentCatalyst),Is.EqualTo(2));
        var valid=Gear(LootManager.GearRarity.Legendary);var tier=ModManager.ApplicableTiers(manager.Database.GetDefinition(StatTypes.Life),valid.ItemType).Find(x=>x.tierIndex==1);valid.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,5,true),new(StatTypes.Life,1,tier.minValue)});inventory.Arm(CraftingCurrencyType.EmpowermentCatalyst);
        Assert.That(inventory.TryApplyArmedToGear(valid),Is.True);Assert.That(inventory.Count(CraftingCurrencyType.EmpowermentCatalyst),Is.EqualTo(1));
    }

    [Test] public void EmpowermentUsesDeterministicCandidateSelectionAndNeverEntersNormalGeneration()
    {
        var manager=CreateModManager();manager.Database.GetDefinition(StatTypes.Life).empowerable=true;manager.Database.GetDefinition(StatTypes.FireRes).empowerable=true;
        var gear=Gear(LootManager.GearRarity.Legendary);gear.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,5,true),new(StatTypes.Life,1,10),new(StatTypes.FireRes,1,10)});
        Assert.That(EmpowermentCrafting.TryApply(gear,120,.99f,0,0),Is.True);Assert.That(gear.rolledMods.Single(x=>x.statType==StatTypes.FireRes).isEmpowered,Is.True);Assert.That(gear.rolledMods.Single(x=>x.statType==StatTypes.Life).isEmpowered,Is.False);
        var generated=manager.RollEquipmentModsForItem(gear.ItemType,LootManager.GearRarity.Legendary,100,Element.Phys);
        Assert.That(generated.Any(x=>x.isEmpowered||x.isBossSpecial||!string.IsNullOrEmpty(x.specialPoolId)),Is.False);
    }

    [Test] public void SpecialPoolReplacementPreservesCountImplicitAndEmpoweredCommitment()
    {
        var gear=Gear(LootManager.GearRarity.Legendary);var implicitMod=new RolledMod(StatTypes.GenericDmg,1,5,true);var replaceable=new RolledMod(StatTypes.Life,1,10);var committed=new RolledMod(StatTypes.FireDmg,1,10){isEmpowered=true};gear.ApplyMods(new List<RolledMod>{implicitMod,replaceable,committed});
        var pool=new SpecialAffixPoolDefinition{stableId="developer-fixture",poolName="Developer Fixture",associatedContentId="future-boss"};
        pool.modifiers.Add(new SpecialAffixDefinition{stableId="developer-life",side=AffixSide.Prefix,allowedItemTypes=new[]{LootManager.GearType.Helmets},statType=StatTypes.ColdDmg,minimum=20,maximum=20,minimumItemLevel=100,minimumCombatLevel=120,weight=1,description="Developer-only architecture fixture."});
        int before=gear.CraftingModCount;Assert.That(BossSpecialCrafting.TryReplace(gear,pool,120,0,0,0,0),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(before));Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));Assert.That(gear.rolledMods,Does.Contain(committed));
        var special=gear.rolledMods.Single(x=>x.isBossSpecial);Assert.That(special.specialPoolId,Is.EqualTo(pool.stableId));Assert.That(special.specialModifierId,Is.EqualTo("developer-life"));Assert.That(special.specialAffixSide,Is.EqualTo(AffixSide.Prefix));Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(11));
        gear.RestoreCraftingState(gear.OriginRarity,2,gear.MaximumCraftingPotential);Assert.That(BossSpecialCrafting.TryReplace(gear,pool,120),Is.False);Assert.That(gear.CurrentCraftingPotential,Is.EqualTo(2));
        var illegal=Gear(LootManager.GearRarity.Legendary,LootManager.GearType.Boots);illegal.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,5,true),new(StatTypes.Life,1,10)});Assert.That(BossSpecialCrafting.TryReplace(illegal,pool,120),Is.False);
    }

    [Test] public void SkillQueueDoesNotMoveGaugeAndSpendsManaOnlyOnScheduledResolution()
    {
        BuildCombatFixture(out var battle,out var skills,out var enemy);
        var selected=skills.Skills.First(x=>x.id==PlayerSkillId.HeavyStrike);Assert.That(skills.TrySelect(selected),Is.True);
        SetField(battle,"playerGauge",42f);float beforeMana=skills.Mana.CurrentMana;
        Assert.That(skills.TryQueueSelected(),Is.True);Assert.That((float)GetField(battle,"playerGauge"),Is.EqualTo(42f));Assert.That(skills.Mana.CurrentMana,Is.EqualTo(beforeMana));
        float life=enemy.CurrentLife;Invoke(battle,"ResolvePlayerTurn");Assert.That(skills.HasQueuedSkill,Is.False);Assert.That(skills.Mana.CurrentMana,Is.EqualTo(beforeMana-skills.ManaCost(selected)));Assert.That(enemy.CurrentLife,Is.LessThan(life));
        float afterSkill=skills.Mana.CurrentMana;Invoke(battle,"ResolvePlayerTurn");Assert.That(skills.Mana.CurrentMana,Is.EqualTo(afterSkill));
    }

    [Test] public void QueueRejectsLowManaAndResolutionFallbackStillUsesBasicAttack()
    {
        BuildCombatFixture(out var battle,out var skills,out var enemy);var selected=skills.Skills.First(x=>x.id==PlayerSkillId.HeavyStrike);skills.TrySelect(selected);
        skills.Mana.SpendUpTo(skills.Mana.CurrentMana);Assert.That(skills.TryQueueSelected(),Is.False);
        skills.Mana.RestoreFull();Assert.That(skills.TryQueueSelected(),Is.True);skills.Mana.SpendUpTo(skills.Mana.CurrentMana);float life=enemy.CurrentLife;
        Invoke(battle,"ResolvePlayerTurn");Assert.That(skills.HasQueuedSkill,Is.False);Assert.That(enemy.CurrentLife,Is.LessThan(life));Assert.That(skills.Mana.CurrentMana,Is.Zero);
    }

    [Test] public void QueueTargetsReplacementEnemyAndCleanRestoreClearsTransientQueue()
    {
        BuildCombatFixture(out var battle,out var skills,out var original);var selected=skills.Skills.First(x=>x.id==PlayerSkillId.HeavyStrike);skills.TrySelect(selected);Assert.That(skills.TryQueueSelected(),Is.True);
        var replacement=Track(new GameObject("replacement enemy"));var replacementStats=replacement.AddComponent<StatsComponent>();replacementStats.SetBaseStat(StatTypes.Life,10000);var replacementHealth=replacement.AddComponent<HealthComponent>();replacement.AddComponent<StatusController>();var replacementReceiver=replacement.AddComponent<DamageReceiver>();Invoke(replacementReceiver,"Awake");replacementHealth.ReviveToFullLife();
        SetField(battle,"currentEnemy",replacement);SetField(battle,"enemyHealth",replacementHealth);SetField(battle,"enemyStatusCont",replacement.GetComponent<StatusController>());SetField(battle,"enemyStats",replacementStats);SetField(battle,"enemyDamageReceiver",replacement.GetComponent<DamageReceiver>());
        float originalLife=original.CurrentLife,replacementLife=replacementHealth.CurrentLife;Invoke(battle,"ResolvePlayerTurn");Assert.That(original.CurrentLife,Is.EqualTo(originalLife));Assert.That(replacementHealth.CurrentLife,Is.LessThan(replacementLife));
        Assert.That(skills.TryQueueSelected(),Is.True);Assert.That(skills.RestoreSelection(true,selected.id),Is.True);Assert.That(skills.HasQueuedSkill,Is.False,"Encounter restore must never persist the queued combat transient.");
    }

    void BuildCombatFixture(out BattleManager battle,out PlayerSkillController skills,out HealthComponent enemyHealth)
    {
        var player=Track(new GameObject("player"));var stats=player.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,1000);stats.SetBaseStat(StatTypes.Mana,200);var playerHealth=player.AddComponent<HealthComponent>();playerHealth.ReviveToFullLife();var mana=player.AddComponent<ManaComponent>();mana.RestoreFull();var controller=player.AddComponent<PlayerController>();Invoke(controller,"Awake");skills=player.AddComponent<PlayerSkillController>();Invoke(skills,"Awake");SetField(skills,"skills",PlayerSkillDefinition.CreateDefaults());
        var weapon=Gear(LootManager.GearRarity.Normal,LootManager.GearType.Weapons);weapon.BaseDamage=weapon.BaseDamageMin=weapon.BaseDamageMax=10;weapon.BaseAttackSpeed=1;weapon.BaseCritChance=0;weapon.ApplyMods(new List<RolledMod>{new(StatTypes.GenericDmg,1,1,true)});controller.EquipWeapon(weapon);
        var enemy=Track(new GameObject("enemy"));var enemyStats=enemy.AddComponent<StatsComponent>();enemyStats.SetBaseStat(StatTypes.Life,10000);enemyHealth=enemy.AddComponent<HealthComponent>();enemy.AddComponent<StatusController>();var receiver=enemy.AddComponent<DamageReceiver>();Invoke(receiver,"Awake");enemyHealth.ReviveToFullLife();
        battle=Track(new GameObject("battle",typeof(BattleManager))).GetComponent<BattleManager>();SetStatic(typeof(BattleManager),"Instance",battle);SetField(battle,"player",player);SetField(battle,"playerController",controller);SetField(battle,"playerStats",stats);SetField(battle,"playerHealth",player.GetComponent<HealthComponent>());SetField(battle,"currentEnemy",enemy);SetField(battle,"enemyHealth",enemyHealth);SetField(battle,"enemyStatusCont",enemy.GetComponent<StatusController>());SetField(battle,"enemyStats",enemyStats);SetField(battle,"enemyDamageReceiver",enemy.GetComponent<DamageReceiver>());
        Assert.That(BattleManager.Instance,Is.SameAs(battle));Assert.That(skills.Mana,Is.SameAs(mana));Assert.That(mana.CurrentMana,Is.GreaterThan(0));Assert.That(battle.CanCastPlayerSkill,Is.True);
    }

    ModManager CreateModManager(){var database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");database.Initialize();var manager=Track(new GameObject("mods",typeof(ModManager))).GetComponent<ModManager>();manager.ConfigureForIsolatedRolling(database);SetStatic(typeof(ModManager),"Instance",manager);return manager;}
    Gear Gear(LootManager.GearRarity rarity,LootManager.GearType type=LootManager.GearType.Helmets){var gear=Track(new GameObject("gear",typeof(Gear))).GetComponent<Gear>();gear.Initialize(type,rarity,100,Element.Phys);return gear;}
    GameObject Track(GameObject value){cleanup.Add(value);return value;}
    static void SetField(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static object GetField(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Invoke(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static void SetStatic(Type type,string name,object value)=>type.GetProperty(name,BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
}
