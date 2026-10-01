using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BlackCube.CombatSimulation;

public sealed class SystemsRedesignRegressionTests
{
    readonly List<GameObject> objects=new();
    [Test] public void RarityRetentionNeverHighlightsWithoutSelectedModMatches()
    {
        var go=new GameObject("Highlight test");go.SetActive(false);objects.Add(go);
        var gear=go.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Rare,100,Element.Phys);
        var filter=new AdvancedLootFilter{enabled=true};
        foreach(LootManager.GearRarity rarity in Enum.GetValues(typeof(LootManager.GearRarity)))
        {
            gear.Initialize(LootManager.GearType.Helmets,rarity,100,Element.Phys);
            Assert.That(filter.Keeps(gear),Is.True);
            Assert.That(filter.Highlights(gear),Is.False);
        }
        gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Rare,100,Element.Phys);
        var policy=filter.For(gear.ItemType);policy.enabled=true;
        Assert.That(filter.Highlights(gear),Is.False);
        policy.requirements.Add(new LootModRequirement{stat=StatTypes.Life,minimumTier=2});
        gear.rolledMods.Add(new RolledMod(StatTypes.Life,3,10));
        Assert.That(filter.Highlights(gear),Is.False);
        gear.rolledMods.Last().tierIndex=2;
        Assert.That(filter.Highlights(gear),Is.True);
        filter.keptRarities=0;
        Assert.That(filter.Keeps(gear),Is.False);
        Assert.That(filter.Highlights(gear),Is.True,"Mod matching remains independent of rarity retention.");
        policy.enabled=false;Assert.That(filter.Highlights(gear),Is.False);
        policy.enabled=true;filter.enabled=false;Assert.That(filter.Highlights(gear),Is.False);
    }
    [Test] public void RelicActionsAreInsideRelicViewAndOutsideListLayout()
    {
        foreach(string path in new[]{PersistentUIAuthoringInstaller.GameplayPrefabPath,InventoryAuthoringBuilder.PrefabPath})
        {
            var inventory=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<InventoryUI>(true);
            var root=inventory.GetComponent<InventoryView>().relicRoot;
            var toolbar=root.Find("Relic actions");Assert.That(toolbar,Is.Not.Null);
            Assert.That(toolbar.GetComponent<LayoutElement>().ignoreLayout,Is.True);
            Assert.That(root.GetComponent<VerticalLayoutGroup>().padding.top,Is.GreaterThanOrEqualTo(52));
            foreach(Component component in new Component[]{inventory.GetComponent<RelicFusionUI>(),inventory.GetComponent<UniqueRelicForgeUI>()})
                Assert.That(((Button)new SerializedObject(component).FindProperty("open").objectReferenceValue).transform.parent,Is.EqualTo(toolbar));
        }
    }
    [TearDown] public void Cleanup(){foreach(var obj in objects)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);objects.Clear();}
    [Test] public void ProductionStaffUsesAutomaticAttackReplacementNotDirectCooldownCasts()
    {
        var skills=PlayerSkillDefinition.CreateProductionDefaults().Where(s=>s.weaponTypeId==WeaponTypeIds.Staff).ToArray();
        Assert.That(skills.Length,Is.EqualTo(2));Assert.That(skills.All(s=>s.castMode==PlayerSkillCastMode.AutoQueuedReplacement),Is.True);
        var actor=new GameObject("Staff queue test");actor.SetActive(false);objects.Add(actor);
        var stats=actor.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Mana,200);
        var controller=actor.AddComponent<PlayerSkillController>();
        typeof(PlayerSkillController).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,null);
        controller.Mana.ConfigureIsolatedStats(stats);controller.ConfigureDeveloperAutoSkills(skills[0],skills[1]);
        int directCasts=0;controller.TickAutoCooldowns(10,_=>{directCasts++;return true;});
        Assert.That(directCasts,Is.Zero);controller.PrepareAutomaticReplacement();Assert.That(controller.QueuedSkill,Is.SameAs(skills[0]));
        Assert.That(controller.TryConsumeQueuedForAttack(out var resolved,out float spent),Is.True);Assert.That(spent,Is.EqualTo(60));controller.NotifyQueuedSkillResolved(resolved);
        controller.PrepareAutomaticReplacement();Assert.That(controller.QueuedSkill,Is.SameAs(skills[1]));
        controller.ToggleAutocast(1);Assert.That(controller.HasQueuedSkill,Is.False);
        controller.TickAutoCooldowns(5,_=>{directCasts++;return true;});controller.ToggleAutocast(0);controller.PrepareAutomaticReplacement();Assert.That(controller.HasQueuedSkill,Is.False);
        controller.RestoreAutocast(true,false);controller.Mana.SpendUpTo(1000);controller.PrepareAutomaticReplacement();Assert.That(controller.HasQueuedSkill,Is.False);
        controller.Mana.Restore(60);controller.PrepareAutomaticReplacement();Assert.That(controller.QueuedSkill,Is.SameAs(skills[0]));
    }
    [Test] public void AutocastSaveFieldsRoundtripAndLegacyDefaultsAreEnabled()
    {
        var payload=JsonUtility.FromJson<GameStatePayload>("{}");Assert.That(payload.staffAutocastFirst&&payload.staffAutocastSecond,Is.True);
        payload.staffAutocastFirst=false;payload.staffAutocastSecond=true;
        var roundtrip=JsonUtility.FromJson<GameStatePayload>(JsonUtility.ToJson(payload));Assert.That(roundtrip.staffAutocastFirst,Is.False);Assert.That(roundtrip.staffAutocastSecond,Is.True);
    }
    [Test] public void EchoUsesFractionalChanceAndEscalatingMana()
    {
        Assert.That(SpellEchoRules.EffectiveChance(.1f),Is.EqualTo(.1f));Assert.That(SpellEchoRules.EffectiveChance(2),Is.EqualTo(.6f));
        Assert.That(SpellEchoRules.ManaCost(60,1),Is.EqualTo(75));Assert.That(SpellEchoRules.ManaCost(60,2),Is.EqualTo(90));Assert.That(SpellEchoRules.ManaCost(60,3),Is.EqualTo(105));
        float mana=190;var costs=new List<float>();
        int echoes=SpellEchoRules.CastEchoes(.6f,60,()=>0,cost=>{if(mana<cost)return false;mana-=cost;costs.Add(cost);return true;});
        Assert.That(echoes,Is.EqualTo(2));Assert.That(costs,Is.EqualTo(new[]{75f,90f}));Assert.That(mana,Is.EqualTo(25));
        Assert.That(SpellEchoRules.CastEchoes(2,60,()=>.7f,_=>true),Is.Zero);
    }
    [Test] public void FilteredPickupSurvivesWhenAutomaticDismantlingIsOff()
    {
        GamePersistence.ResetStaticStateForTests();
        var go=new GameObject("Non-destructive filter");go.SetActive(false);objects.Add(go);var inv=go.AddComponent<Inventory>();
        inv.AdvancedFilter.enabled=true;inv.AdvancedFilter.keptRarities=0;inv.AdvancedFilter.autoDismantleFilteredItems=false;
        var itemObject=new GameObject("Rejected pickup");itemObject.SetActive(false);objects.Add(itemObject);var gear=itemObject.AddComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Rare,100,Element.Phys);
        Assert.That(inv.MatchesFilter(gear),Is.True);Assert.That(inv.Pickup(gear),Is.True);Assert.That(inv.Items.Contains(gear),Is.True);Assert.That(gear,Is.Not.Null);
        var restored=JsonUtility.FromJson<AdvancedLootFilter>(JsonUtility.ToJson(inv.AdvancedFilter));Assert.That(restored.autoDismantleFilteredItems,Is.False);
        GamePersistence.ResetStaticStateForTests();
    }
    [Test] public void MageFamiliesHaveRealValuesAndStableNodeIds()
    {
        var branch=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Mage);var nodes=branch.Tiers.SelectMany(t=>t.Left.GenericNodes.Concat(t.Right.GenericNodes)).ToArray();
        var effects=nodes.SelectMany(n=>n.Effects).ToArray();Assert.That(nodes.Length,Is.EqualTo(60));
        foreach(var stat in new[]{StatTypes.ReducedShockEffect,StatTypes.ReducedChillEffect})Assert.That(effects.Where(e=>e.Stat==stat).Sum(e=>e.Value),Is.EqualTo(40).Within(.001));
        Assert.That(effects.Where(e=>e.Stat==StatTypes.SpellEchoChance).Sum(e=>e.Value),Is.EqualTo(10).Within(.001));Assert.That(effects.Any(e=>e.Stat==StatTypes.ColdDmg),Is.False);
        Assert.That(nodes.Select(n=>n.StableId).Distinct().Count(),Is.EqualTo(nodes.Length));
    }
    [Test] public void HeadlessStaffNeverFiresBetweenAttackGaugeEvents()
    {
        var p=new CombatantSnapshot{player=true,id="player",name="Player",weaponTypeId=WeaponTypeIds.Staff,maximumLife=1000,maximumMana=1000,attackSpeed=1,basicDamage=new(){physical=10}};
        p.skills.Add(new(){id="first",name="First",castMode=PlayerSkillCastMode.AutoQueuedReplacement,cooldown=5,manaCost=60});
        p.skills.Add(new(){id="second",name="Second",castMode=PlayerSkillCastMode.AutoQueuedReplacement,cooldown=5,manaCost=50});
        var e=new CombatantSnapshot{id="enemy",name="Enemy",maximumLife=1000000,attackSpeed=.1f};
        var result=HeadlessCombatSimulator.Run(p,e,new(){maximumDuration=4.5f,retainTrace=true});
        Assert.That(result.outcome,Is.Not.EqualTo(CombatOutcome.SimulationError));Assert.That(result.playerAttacks,Is.EqualTo(4));
        Assert.That(result.skills.Sum(s=>s.activations),Is.EqualTo(2));Assert.That(result.manaSpent,Is.EqualTo(110));
    }
    [Test] public void RecapRetainsBothTagsAndKillingEventLast()
    {
        var go=new GameObject("History");objects.Add(go);var history=go.AddComponent<IncomingDamageHistory>();
        for(int i=1;i<=6;i++){var context=new DamageContext(1){IsCrit=i==6,EventTags=CombatEventTags.Ailment};context.AddDamage(Element.Fire,i);history.Record(i,context,null,null);}
        var lines=history.Describe().Split('\n');Assert.That(lines.Length,Is.EqualTo(5));StringAssert.Contains("6",lines.Last());StringAssert.Contains("CRIT",lines.Last());StringAssert.Contains("AILMENT",lines.Last());
    }
    [Test] public void InventoryIconsNeverCreateDpsLabels()
    {
        var go=new GameObject("Slot",typeof(RectTransform));objects.Add(go);var slot=go.AddComponent<ItemSlotUI>();slot.SetDpsUpgrade(20);
        Assert.That(go.transform.Find("Estimated DPS Upgrade"),Is.Null);
    }
    [Test] public void StatsRefreshReusesRowsAndKeepsHeaderCallbacksSingular()
    {
        var go=new GameObject("Stats test",typeof(RectTransform));go.SetActive(false);objects.Add(go);
        var actor=new GameObject("Stats actor");actor.SetActive(false);objects.Add(actor);
        var stats=actor.AddComponent<StatsComponent>();PlayerStatSetup.ApplyBaseline(stats);actor.AddComponent<PlayerController>().ConfigureIsolatedBuildLevel(1);
        var root=new GameObject("Rows",typeof(RectTransform));root.transform.SetParent(go.transform,false);
        var panel=go.AddComponent<PlayerStatsPanelUI>();var data=new SerializedObject(panel);
        data.FindProperty("contentRoot").objectReferenceValue=root.transform;
        data.FindProperty("rowPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<StatRowUI>("Assets/Prefabs/PaperBattle/StatRow.prefab");
        data.FindProperty("headerPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<StatHeaderUI>("Assets/Prefabs/PaperBattle/StatHeader.prefab");data.ApplyModifiedPropertiesWithoutUndo();
        panel.SetTarget(stats);var before=root.GetComponentsInChildren<StatRowUI>(true).ToArray();Assert.That(before.Length,Is.GreaterThan(0));
        for(int i=0;i<20;i++)panel.Refresh();
        Assert.That(root.GetComponentsInChildren<StatRowUI>(true),Is.EqualTo(before));
        var button=panel.SectionButton("Advanced Sources");button.onClick.Invoke();Assert.That(panel.IsSectionExpanded("Advanced Sources"),Is.True);
        for(int i=0;i<10;i++)panel.Refresh();panel.SectionButton("Advanced Sources").onClick.Invoke();Assert.That(panel.IsSectionExpanded("Advanced Sources"),Is.False);
    }
    [Test] public void AdvancedLootHasAuthoredDropdownSliderHelpAndNoLegacyMenus()
    {
        foreach(string path in new[]{PersistentUIAuthoringInstaller.GameplayPrefabPath,InventoryAuthoringBuilder.PrefabPath})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var inventory=root.GetComponentInChildren<InventoryUI>(true);var ui=inventory.GetComponent<AdvancedLootFilterUI>();var data=new SerializedObject(ui);
            Assert.That(data.FindProperty("itemTypeDropdown").objectReferenceValue,Is.Not.Null);Assert.That(data.FindProperty("helpPanel").objectReferenceValue,Is.Not.Null);
            Assert.That(inventory.GetComponent<InventoryModHighlightUI>(),Is.Null);Assert.That(inventory.GetComponent<InventoryFilterUI>(),Is.Null);
            Assert.That(inventory.GetComponentInChildren<Slider>(true),Is.Not.Null);
        }
    }
}
