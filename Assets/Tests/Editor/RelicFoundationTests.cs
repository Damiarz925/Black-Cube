using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RelicFoundationTests
{
    readonly List<GameObject> created=new();
    RelicInventory previousRelics;
    CurrencyInventory previousCurrencies;

    [SetUp]
    public void SetUp()
    {
        previousRelics=RelicInventory.Instance;previousCurrencies=CurrencyInventory.Instance;
        SetInstance(typeof(RelicInventory),null);SetInstance(typeof(CurrencyInventory),null);
    }
    [TearDown]
    public void TearDown()
    {
        for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();SetInstance(typeof(RelicInventory),previousRelics);SetInstance(typeof(CurrencyInventory),previousCurrencies);
    }

    [Test]
    public void StableRelicPoolPreservesLegacyIdsAndWeightsBuildChangingFamiliesLess()
    {
        Assert.That((int)RelicModifierType.MoreDamage,Is.Zero);
        Assert.That((int)RelicModifierType.IncreasedExperience,Is.EqualTo(1));
        Assert.That((int)RelicModifierType.MoreAttackSpeed,Is.EqualTo(2));
        foreach(RelicModifierType id in Enum.GetValues(typeof(RelicModifierType)))Assert.That(RelicModifierDefinitions.Get(id),Is.Not.Null,id.ToString());
        Assert.That(RelicModifierDefinitions.Get(RelicModifierType.ProjectileAmount).Weight,
            Is.LessThan(RelicModifierDefinitions.Get(RelicModifierType.MoreDamage).Weight));
    }

    [Test]
    public void SelectedSkillLevelRelicAffectsOnlyEquippedActiveSkill()
    {
        var relics=Track(new GameObject("relics",typeof(RelicInventory))).GetComponent<RelicInventory>();
        SetInstance(typeof(RelicInventory),relics);
        var relic=new RelicData{id="skill-relic",cycle=1,rarity=LootManager.GearRarity.Normal,
            craftableThisCycle=true,modifiers=new List<RelicModifier>{
                new(RelicModifierType.EquippedSkillLevel,1f,true)}};
        relics.Restore(new List<RelicData>{relic},1,new[]{0,-1,-1,-1});
        var actor=Track(new GameObject("skill actor",typeof(StatsComponent),typeof(ManaComponent),
            typeof(PlayerSkillController))).GetComponent<PlayerSkillController>();
        typeof(PlayerSkillController).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(actor,null);
        Assert.That(actor.RestoreSelection(true,PlayerSkillId.HeavyStrike),Is.True);
        Assert.That(actor.EffectiveSkillLevel(actor.SelectedSkill),Is.EqualTo(2));
        PlayerSkillDefinition other=null;
        foreach(var skill in actor.Skills)if(skill.id==PlayerSkillId.Fireball){other=skill;break;}
        Assert.That(other,Is.Not.Null);
        Assert.That(actor.EffectiveSkillLevel(other),Is.EqualTo(1));
    }

    [Test]
    public void OwnedRelicsHaveVisibleGlyphClickableTooltipAndCurrentCycleAncientTarget()
    {
        var relics=Track(new GameObject("relics",typeof(RelicInventory))).GetComponent<RelicInventory>();SetInstance(typeof(RelicInventory),relics);
        var older=relics.BeginNewCycle();var current=relics.BeginNewCycle();
        var currencies=Track(new GameObject("currencies",typeof(CurrencyInventory))).GetComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),currencies);
        currencies.Add(CraftingCurrencyType.AncientNormalToMagic,2);
        var canvas=Track(new GameObject("canvas",typeof(RectTransform),typeof(Canvas))).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var panel=Track(new GameObject("inventory panel",typeof(RectTransform),typeof(CurrencyInventoryPanel))).GetComponent<CurrencyInventoryPanel>();
        panel.transform.SetParent(canvas.transform,false);panel.Initialize(null,null);panel.ShowRelics();
        var slots=panel.GetComponentsInChildren<RelicSlotUI>(true);
        Assert.That(slots.Length,Is.EqualTo(2));
        foreach(var slot in slots)
        {
            var glyph=slot.transform.Find("Relic glyph")?.GetComponent<Image>();
            Assert.That(glyph,Is.Not.Null);Assert.That(glyph.sprite,Is.Not.Null);
            Assert.That(glyph.raycastTarget,Is.False);Assert.That(slot.GetComponent<Button>(),Is.Not.Null);
            Assert.That(slot.GetComponentInChildren<TMP_Text>().text,Does.Contain("RELIC"));
        }
        var oldSlot=Array.Find(slots,x=>ReferenceEquals(x.Item,older));
        var currentSlot=Array.Find(slots,x=>ReferenceEquals(x.Item,current));
        oldSlot.OnPointerEnter(null);Assert.That(UnityEngine.Object.FindAnyObjectByType<RelicTooltipUI>(),Is.Not.Null);
        currencies.Arm(CraftingCurrencyType.AncientNormalToMagic);
        oldSlot.Activate();Assert.That(older.rarity,Is.EqualTo(LootManager.GearRarity.Normal));Assert.That(currencies.Count(CraftingCurrencyType.AncientNormalToMagic),Is.EqualTo(2));
        currentSlot.Activate();Assert.That(current.rarity,Is.EqualTo(LootManager.GearRarity.Magic));Assert.That(currencies.Count(CraftingCurrencyType.AncientNormalToMagic),Is.EqualTo(1));
    }

    [Test]
    public void SixAncientCursorIconsMatchTheirEntriesAndManualTabsCancelOnlyManualArming()
    {
        var currencies=Track(new GameObject("currencies",typeof(CurrencyInventory))).GetComponent<CurrencyInventory>();SetInstance(typeof(CurrencyInventory),currencies);
        var canvas=Track(new GameObject("canvas",typeof(RectTransform),typeof(Canvas))).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var panel=Track(new GameObject("inventory panel",typeof(RectTransform),typeof(CurrencyInventoryPanel))).GetComponent<CurrencyInventoryPanel>();
        panel.transform.SetParent(canvas.transform,false);panel.Initialize(null,null);
        Assert.That(panel.GearTab,Is.Not.Null);Assert.That(panel.RelicTab,Is.Not.Null);
        panel.ShowRelics();Assert.That(panel.IsShowingRelics,Is.True);
        panel.ShowEquipment();Assert.That(panel.IsShowingRelics,Is.False);
        var cursor=CraftingCurrencyCursorUI.Ensure(canvas.rootCanvas);
        Assert.That(panel.GetComponentsInChildren<CurrencySlotUI>(true).Length,Is.EqualTo(13),"All source entries, including the endgame catalyst, exist before cursor checks.");
        foreach(CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType)))
        {
            if(!CurrencyInventory.IsAncient(type))continue;
            currencies.Add(type);Assert.That(currencies.Arm(type),Is.True);panel.ShowRelicsForCurrency();cursor.RefreshPresentation();
            var slot=Array.Find(panel.GetComponentsInChildren<CurrencySlotUI>(true),x=>x.Type==type);
            Assert.That(slot,Is.Not.Null,"Missing source entry for "+type);
            Assert.That(slot.GetComponent<Image>().sprite,Is.Not.Null,"Missing visible source sprite for "+type);
            Assert.That(cursor.Icon.sprite,Is.SameAs(slot.GetComponent<Image>().sprite));
            Assert.That(currencies.ArmedCurrency,Is.EqualTo(type),"Currency-triggered switch preserves the newly armed currency.");
            panel.ShowEquipment();Assert.That(currencies.ArmedCurrency,Is.Null,"A manual tab switch cancels armed currency.");
        }
    }
    GameObject Track(GameObject gameObject){created.Add(gameObject);return gameObject;}
    static void SetInstance(Type type,object value)=>type.GetProperty("Instance",BindingFlags.Static|BindingFlags.Public).GetSetMethod(true).Invoke(null,new[]{value});
}
