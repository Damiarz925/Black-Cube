using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BlackCube.CombatSimulation;

public sealed class PlaytestFollowUpTests
{
    readonly List<GameObject> objects=new();
    Gear Item(LootManager.GearType slot=LootManager.GearType.Helmets,LootManager.GearRarity rarity=LootManager.GearRarity.Rare,Element element=Element.Phys,string weapon=null)
    {
        var go=new GameObject("Focused item");go.SetActive(false);objects.Add(go);var item=go.AddComponent<Gear>();item.Initialize(slot,rarity,100,element,weapon);return item;
    }
    [TearDown] public void Cleanup(){foreach(var go in objects)if(go!=null)UnityEngine.Object.DestroyImmediate(go);objects.Clear();}
    [Test] public void TierAndImplicitRequirementsDistinguishExplicitFromImplicit()
    {
        var item=Item();item.ApplyMods(new(){new(StatTypes.Life,1,10,true),new(StatTypes.FireRes,3,40)});
        var f=new ItemTypeLootFilter{enabled=true,requirements=new(){new(){stat=StatTypes.FireRes,minimumTier=3}}};
        Assert.That(f.Matches(item),Is.True);f.requirements[0].minimumTier=2;Assert.That(f.Matches(item),Is.False);
        f.requirements[0].requireImplicit=true;f.requirements[0].minimumImplicitTier=3;Assert.That(f.Matches(item),Is.False);
        item.ApplyMods(new(){new(StatTypes.FireRes,2,40,true)});Assert.That(f.Matches(item),Is.True);
    }
    [Test] public void AnyAndOverfullAllRespectCurrentLegalCapacity()
    {
        var item=Item();item.ApplyMods(new(){new(StatTypes.Life,1,10,true),new(StatTypes.FireRes,1,40),new(StatTypes.ColdRes,1,40),new(StatTypes.LightRes,1,40),new(StatTypes.VoidRes,1,40)});
        var f=new ItemTypeLootFilter{enabled=true,all=false,minimumMatches=2};
        foreach(var stat in new[]{StatTypes.FireRes,StatTypes.ColdRes,StatTypes.Strength})f.requirements.Add(new(){stat=stat});
        Assert.That(f.Matches(item),Is.True);f.minimumMatches=3;Assert.That(f.Matches(item),Is.False);
        f.all=true;f.requirements.Add(new(){stat=StatTypes.LightRes});f.requirements.Add(new(){stat=StatTypes.VoidRes});
        Assert.That(f.Matches(item),Is.True);item.rolledMods.RemoveAt(4);Assert.That(f.Matches(item),Is.False,"partial Rare must not masquerade as a full desired affix set");
    }
    [Test] public void RarityWinsAndWeaponSelectionUsesVoidRatherThanLegacyPoisonBit()
    {
        var f=new AdvancedLootFilter{enabled=true};var item=Item(LootManager.GearType.Weapons,element:Element.Void,weapon:WeaponTypeIds.Dagger);
        Assert.That(f.Keeps(item),Is.True);f.keptRarities&=~(1<<(int)LootManager.GearRarity.Rare);Assert.That(f.Keeps(item),Is.False);
        item.RestoreLock(true);Assert.That(f.Keeps(item),Is.True);item.RestoreLock(false);f.keptRarities=15;
        f.keptElements&=~(1<<(int)Element.Void);Assert.That(f.Keeps(item),Is.False);f.keptElements=47;f.keptWeapons&=~(1<<5);Assert.That(f.Keeps(item),Is.False);
    }
    [Test] public void ItemLockSurvivesJsonSaveRoundtripAndBlocksDestructiveCrafting()
    {
        var item=Item();item.ApplyMods(new(){new(StatTypes.FireRes,1,40,true),new(StatTypes.Life,1,10)});item.RestoreLock(true);
        var snapshot=JsonUtility.FromJson<GearSnapshotData>(JsonUtility.ToJson(GearSnapshotData.Capture(item)));var loaded=snapshot.Create();objects.Add(loaded.gameObject);
        Assert.That(loaded.IsLocked,Is.True);Assert.That(EquipmentCrafting.CanApply(CraftingCurrencyType.RemoveRareModifier,loaded),Is.False);
        var go=new GameObject("Locked inventory");go.SetActive(false);objects.Add(go);var inventory=go.AddComponent<Inventory>();inventory.Add(loaded);inventory.FilterRarityEnabled=true;inventory.FilterRarity=LootManager.GearRarity.Legendary;
        Assert.That(inventory.CanDismantle(loaded),Is.False);Assert.That(inventory.MatchesFilter(loaded),Is.False);
    }
    [Test] public void LocalArmourIsAppliedOnceAndGlobalArmourRemainsSeparate()
    {
        var item=Item();item.ApplyMods(new(){new(StatTypes.FlatArmour,1,100,true),new(StatTypes.ArmourPercent,1,50)});
        Assert.That(item.FinalItemArmour,Is.EqualTo((2475+100)*1.5f));
        Assert.That(item.globalRolledMods.Count(m=>m.statType==StatTypes.ArmourPercent),Is.Zero);
        Assert.That(item.globalRolledMods.Single(m=>m.statType==StatTypes.FlatArmour).value,Is.EqualTo(item.FinalItemArmour));
        item.RebuildMods();Assert.That(item.globalRolledMods.Single(m=>m.statType==StatTypes.FlatArmour).value,Is.EqualTo(item.FinalItemArmour));
        Assert.That(ItemArmourProfile.Base(LootManager.GearType.Rings,100),Is.Zero);
    }
    [Test] public void PerfectArmourWithDecentGlobalInvestmentHitsRaw95AndActual90()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");float total=0,pdr=0;
        foreach(var slot in new[]{LootManager.GearType.BodyArmours,LootManager.GearType.Helmets,LootManager.GearType.Gloves,LootManager.GearType.Boots})
        {
            var item=Item(slot);var flat=ModManager.ApplicableTiers(db.GetDefinition(StatTypes.FlatArmour),slot).First(t=>t.tierIndex==1);
            var percent=ModManager.ApplicableTiers(db.GetDefinition(StatTypes.ArmourPercent),slot).First(t=>t.tierIndex==1);
            item.ApplyMods(new(){new(StatTypes.FlatArmour,1,flat.maxValue,true),new(StatTypes.ArmourPercent,1,percent.maxValue)});total+=item.FinalItemArmour;
            if(slot is LootManager.GearType.BodyArmours or LootManager.GearType.Helmets)pdr+=db.GetDefinition(StatTypes.PhysicalDamageReduction).tiers.First(t=>t.tierIndex==1).maxValue;
        }
        float raw=ItemArmourProfile.RawReduction(total*2,ItemArmourProfile.ReferenceHit,pdr);
        Debug.Log($"ARMOUR REFERENCE: gear={total}; global=100%; final={total*2}; explicit={pdr}; raw={raw:P4}");
        Assert.That(raw,Is.EqualTo(.95f).Within(.002f));
        Assert.That(1-CombatCalculator.ApplyArmourValue(ItemArmourProfile.ReferenceHit,total*2,pdr,0)/ItemArmourProfile.ReferenceHit,Is.EqualTo(.9f).Within(.0001f));
    }
    [Test] public void BloodEngineConverts30PercentRegenInto15PercentDamageRecovery()
    {
        var go=new GameObject("Blood engine");go.SetActive(false);objects.Add(go);var stats=go.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.LifeRegeneration,30);
        var keys=go.AddComponent<PassiveKeystoneState>();keys.ApplyAllocatedNodes(new[]{PassiveTreeDefinition.Nodes.First(n=>n.Keystone==PassiveKeystone.BarbarianRecovery)});
        Assert.That(keys.LifeRegenerationMultiplier,Is.Zero);Assert.That(keys.DamageRecoveryFraction,Is.EqualTo(.15f).Within(.0001f));
    }
    [Test] public void FlurryHasCostAndRepeatedStrikesConsolidateRatherThanRepeatBaseEvent()
    {
        var skill=PlayerSkillDefinition.CreateProductionDefaults().First(s=>s.id==PlayerSkillId.SwordRapidFlurry);
        Assert.That(skill.manaCost,Is.EqualTo(25));Assert.That(WeaponMechanicProfile.RapidFlurryHits(0),Is.EqualTo(3));
        Assert.That(ClassKeystoneMechanics.ConsolidatedMultiplier(2),Is.EqualTo(3.1f).Within(.0001f));
    }
    [Test] public void EveryWeaponImplicitUsesElementAndExclusiveWeaponLegality()
    {
        var go=new GameObject("Isolated rolling");go.SetActive(false);objects.Add(go);var roller=go.AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset"));
        int count=0;foreach(var weapon in WeaponTypeCatalog.All)foreach(var element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void})for(int seed=0;seed<3;seed++)
        {
            var mods=roller.RollEquipmentModsForItem(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,100,element,weapon.Id,new SeededSimulationRandomSource(seed+9100));
            var implicitMod=mods.First(m=>!Gear.IsWeaponBaseStat(m.statType));
            Assert.That(ModManager.IsWeaponAffixEligible(implicitMod.statType,element),Is.True);Assert.That(WeaponExclusiveAffixRules.Allows(implicitMod.statType,weapon.Id),Is.True);count++;
        }
        Debug.Log($"IMPLICIT AUDIT: {count} weapon/element seeded cases; no illegal combinations.");
    }
    [Test] public void SmallProgressionLootSampleAndSustainDiagnostics()
    {
        var random=new SeededSimulationRandomSource(29186);
        var host=new GameObject("Small loot sample");host.SetActive(false);objects.Add(host);var loot=host.AddComponent<LootManager>();
        int direct=0,rare=0,legendary=0,gear=0;float expected=0;
        foreach(int level in new[]{10,25,50,75,100})for(int n=0;n<50;n++)
        {
            var context=new DropRateContext(level,(EnemyAI.EnemyRarity)(n%3),false,1);
            expected+=LootDropBalanceProfileSO.Current.EvaluateCurrency(CraftingCurrencyType.MagicToRare,context).finalBudget;
            var reward=EnemyLootProfile.RollConfigured(context,random);
            direct+=reward.currencyStacks.Where(s=>s.currency==CraftingCurrencyType.MagicToRare).Sum(s=>s.amount);
            for(int i=0;i<reward.gearCount;i++)
            {
                gear++;var rarity=loot.RollItemRarity(level,random);
                if(rarity==LootManager.GearRarity.Rare)rare++;
                if(rarity==LootManager.GearRarity.Legendary)legendary++;
            }
        }
        Debug.Log($"CURRENCY SAMPLE: 250 reward rolls, 50 each L10/25/50/75/100, neutral power, Normal/Magic/Rare rotation; expected direct={expected:0.##}, observed direct={direct}, gear={gear}, Rare={rare}, Legendary={legendary}; if rejected, Rare fragments={rare}, Legendary fragments={legendary*2}; manual and auto each yield one/Rare. No fights or rate changes.");
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        float gearRegen=0;
        foreach(var slot in new[]{LootManager.GearType.BodyArmours,LootManager.GearType.Helmets,LootManager.GearType.Belts})
        {
            var tiers=ModManager.ApplicableTiers(db.GetDefinition(StatTypes.LifeRegeneration),slot);
            float max=tiers.Where(t=>t.tierIndex==1).Max(t=>t.maxValue);gearRegen+=max;
            Debug.Log($"REGEN GEAR: {slot} T1 maximum={max} percentage points");
        }
        float Best(PassiveChoiceSideData side,StatTypes stat)=>side.GenericNodes.Max(node=>node.Effects.Where(e=>e.Kind==PassiveEffectKind.Stat&&e.Stat==stat).Sum(e=>e.Value));
        var axe=AssetDatabase.LoadAssetAtPath<PassiveWeaponBranchSO>("Assets/GameData/PassiveTree/Branches/Weapon/SO_TwoHandedAxe_Branch.asset");
        var barb=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Barbarian);
        float weaponRegen=axe.Tiers.Sum(t=>Best(t.Left,StatTypes.LifeRegeneration)+Best(t.Right,StatTypes.LifeRegeneration));
        float classRegen=barb.Tiers.Sum(t=>Best(t.Left,StatTypes.LifeRegeneration)+Best(t.Right,StatTypes.LifeRegeneration));
        Debug.Log($"REGEN HEAVY ceiling: gear={gearRegen} + Barbarian={classRegen} + Axe={weaponRegen} = {gearRegen+classRegen+weaponRegen}% Max Life/sec before recovery/aura modifiers; Blood Engine half={(gearRegen+classRegen+weaponRegen)/2}% actual damage recovery.");
        var warrior=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Warrior);
        float global=warrior.Tiers.Sum(t=>Best(t.Left,StatTypes.ArmourPercent)+Best(t.Right,StatTypes.ArmourPercent));
        Debug.Log($"WARRIOR EXTREME: native Armour side choices={global}% global; uncapped Armour stays available for Armour Strike.");
        Assert.That(direct,Is.GreaterThan(0));Assert.That(gearRegen+weaponRegen+classRegen,Is.GreaterThan(15));
    }
    [Test] public void NeutralEstimateUsesCanonicalHitAndCopiesActiveModifiersWithoutMutatingActor()
    {
        var host=new GameObject("Estimate actor");host.SetActive(false);objects.Add(host);var stats=host.AddComponent<StatsComponent>();
        PlayerStatSetup.ApplyBaseline(stats);stats.SetBaseStat(StatTypes.UnarmedDamage,100);
        stats.SetBaseStat(StatTypes.CritChance,20);stats.SetBaseStat(StatTypes.AttackSpeed,50);
        var player=host.AddComponent<PlayerController>();player.ConfigureItemPreview(1,null);
        float expected=player.BuildNonCriticalAttackContext().Hits.Sum(h=>h.Amount)*(1+player.GetFinalCritChance()*.5f)*player.GetFinalAttackSpeed();
        Assert.That(CharacterDamageEstimate.Calculate(player,false),Is.EqualTo(expected).Within(.01f));
        var source=new object();stats.AddModifier(new StatModifier(StatTypes.GenericDmg,StatOp.Flat,50,source));
        var candidate=Item();float before=CharacterDamageEstimate.Calculate(player);
        Assert.That(CharacterDamageEstimate.Replacement(player,candidate),Is.EqualTo(before).Within(.01f));
        Assert.That(CharacterDamageEstimate.Calculate(player),Is.EqualTo(before));
    }
    [Test] public void ConsolidatedFlurryConvertsTwoExtraStrikes()
    {
        var player=new CombatantSnapshot{player=true,id="player",name="Player",weaponTypeId=WeaponTypeIds.Sword,
            maximumLife=1000,maximumMana=100,attackSpeed=2,lifeOnHit=10,basicDamage=new(){physical=20},
            classKeystones=new(){PassiveKeystone.WarriorConsolidation}};
        player.skills.Add(new(){id="flurry",name="Rapid Flurry",effect=WeaponSkillEffect.RapidFlurry,manaCost=25,castMode=PlayerSkillCastMode.QueuedAttackReplacement});
        var enemy=new CombatantSnapshot{id="enemy",name="Enemy",maximumLife=10000,attackSpeed=.01f,basicDamage=new(){physical=1}};
        var result=HeadlessCombatSimulator.Run(player,enemy,new(){maximumDuration=.51f,startingPlayerLife=500});
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError),result.error);
        Assert.That(result.damage.Single(x=>x.id=="Player/Rapid Flurry").total,Is.EqualTo(62).Within(.01));
        Assert.That(result.manaSpent,Is.EqualTo(25));
    }
    [Test] public void ProductionUiBindingsAndInventoryOverflowAreAuthored()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        var status=prefab.GetComponentInChildren<StatusHUD>(true);
        Assert.That(new SerializedObject(status).FindProperty("authoredView").objectReferenceValue,Is.Not.Null);
        var options=prefab.GetComponentInChildren<PauseMenuView>(true);
        Assert.That(options.hudOptionsButton,Is.Not.Null);Assert.That(options.gameMenuButton,Is.Not.Null);
        var inventory=prefab.GetComponentInChildren<InventoryView>(true);
        var grid=inventory.itemGridRoot.GetComponent<GridLayoutGroup>();var scroll=grid.GetComponentInParent<ScrollRect>(true);
        Assert.That(scroll.content,Is.EqualTo(inventory.itemGridRoot));Assert.That(scroll.viewport.GetComponent<RectMask2D>(),Is.Not.Null);
        Assert.That(grid.cellSize.x,Is.EqualTo(45));Assert.That(inventory.authoredRoot.sizeDelta.x,Is.EqualTo(560).Within(.01));
        float fullInventoryHeight=Mathf.Ceil(100f/grid.constraintCount)*(grid.cellSize.y+grid.spacing.y);
        Assert.That(fullInventoryHeight,Is.GreaterThan(300));
        Assert.That(prefab.GetComponentsInChildren<WeaponSkillTooltip>(true).Length,Is.EqualTo(2));
    }
    [TestCase(PlayerClassIds.Warrior)] [TestCase(PlayerClassIds.Mage)] [TestCase(PlayerClassIds.Barbarian)]
    [TestCase(PlayerClassIds.Ranger)] [TestCase(PlayerClassIds.Thief)] [TestCase(PlayerClassIds.Priest)]
    public void AllSixFreshClassBaselinesHaveZeroInnateRegen(string classId)
    {
        var host=new GameObject("Fresh "+classId);host.SetActive(false);objects.Add(host);
        host.AddComponent<PlayerIdentityState>().BeginNewGame(classId);
        var stats=host.AddComponent<StatsComponent>();PlayerStatSetup.ApplyBaseline(stats);
        Assert.That(stats.GetStat(StatTypes.LifeRegeneration),Is.Zero);Assert.That(stats.GetStat(StatTypes.ManaRegeneration),Is.Zero);
    }
}
