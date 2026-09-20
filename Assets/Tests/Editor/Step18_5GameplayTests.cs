using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class Step18_5GameplayTests
{
    readonly List<Object> cleanup=new();
    [TearDown] public void TearDown(){foreach(var value in cleanup.Where(x=>x!=null).Reverse())Object.DestroyImmediate(value);cleanup.Clear();}

    [TestCase(StatusEffects.AilmentKind.Bleed,StatusEffects.StatusType.DamageOverTime,Element.Phys,100)]
    [TestCase(StatusEffects.AilmentKind.Bleed,StatusEffects.StatusType.DamageOverTime,Element.Fire,0)]
    [TestCase(StatusEffects.AilmentKind.Ignite,StatusEffects.StatusType.DamageOverTime,Element.Fire,100)]
    [TestCase(StatusEffects.AilmentKind.Ignite,StatusEffects.StatusType.DamageOverTime,Element.Light,0)]
    [TestCase(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime,Element.Phys,100)]
    [TestCase(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime,Element.Void,100)]
    [TestCase(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime,Element.Light,0)]
    [TestCase(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Shock,Element.Light,100)]
    [TestCase(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Shock,Element.Fire,0)]
    [TestCase(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Chill,Element.Cold,100)]
    [TestCase(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Chill,Element.Void,0)]
    public void DefaultEligibilityMatrix(StatusEffects.AilmentKind kind,StatusEffects.StatusType type,Element element,float expected)
    {var effect=Effect(kind,type);var hit=new DamageContext(1);hit.AddDamage(element,100);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(effect,hit,null),Is.EqualTo(expected));}

    [Test] public void MixedHitsUseOnlyEligiblePortionAndLightningCannotPoisonFromChanceAlone()
    {var poison=Effect(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime);var hit=new DamageContext(3);hit.AddDamage(Element.Phys,20);hit.AddDamage(Element.Void,30);hit.AddDamage(Element.Light,100);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(poison,hit,null),Is.EqualTo(50));var lightning=new DamageContext(1);lightning.AddDamage(Element.Light,100);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(poison,lightning,Stats()),Is.Zero);}

    [Test] public void ExplicitOverridesAreEffectScopedAndRevocable()
    {
        var poison=Effect(StatusEffects.AilmentKind.Poison,StatusEffects.StatusType.DamageOverTime);var shock=Effect(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Shock);var chill=Effect(StatusEffects.AilmentKind.None,StatusEffects.StatusType.Chill);var hit=new DamageContext(2);hit.AddDamage(Element.Fire,40);hit.AddDamage(Element.Void,60);var stats=Stats();var state=stats.GetComponent<SubclassCombatState>();
        Assert.That(state.GrantExternalEffect(SubclassEffectIds.RangerAllDamagePoison),Is.True);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(poison,hit,stats),Is.EqualTo(100));Assert.That(AilmentEligibilityResolver.EligibleRawDamage(shock,hit,stats),Is.Zero);Assert.That(state.RevokeExternalEffect(SubclassEffectIds.RangerAllDamagePoison),Is.True);
        Assert.That(state.GrantExternalEffect(SubclassEffectIds.DarkPriestVoidAilments),Is.True);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(shock,hit,stats),Is.EqualTo(60));Assert.That(AilmentEligibilityResolver.EligibleRawDamage(chill,hit,stats),Is.EqualTo(60));
        state.RevokeExternalEffect(SubclassEffectIds.DarkPriestVoidAilments);Assert.That(AilmentEligibilityResolver.EligibleRawDamage(chill,hit,stats),Is.Zero);
        hit.EventTags|=CombatEventTags.FullAilmentBasis;Assert.That(AilmentEligibilityResolver.EligibleRawDamage(poison,hit,stats),Is.EqualTo(100),"Venom Shot's virtual context is an explicit full-basis exception.");
    }

    [Test] public void LootFactorsBudgetsCapsAndCurrencyTableAreCentralized()
    {
        Assert.That(EnemyLootProfile.LevelFactor(1),Is.EqualTo(.75f));Assert.That(EnemyLootProfile.LevelFactor(360),Is.EqualTo(2f));
        Assert.That(EnemyLootProfile.RarityMultiplier(EnemyAI.EnemyRarity.Normal,false),Is.EqualTo(1));Assert.That(EnemyLootProfile.RarityMultiplier(EnemyAI.EnemyRarity.Rare,false),Is.EqualTo(2.6f));Assert.That(EnemyLootProfile.RarityMultiplier(EnemyAI.EnemyRarity.Legendary,false),Is.EqualTo(4));Assert.That(EnemyLootProfile.RarityMultiplier(EnemyAI.EnemyRarity.Normal,true),Is.EqualTo(5));
        Assert.That(EnemyLootProfile.StochasticRound(99,EnemyLootProfile.MaximumExtraGear,()=>0),Is.EqualTo(12));Assert.That(EnemyLootProfile.StochasticRound(99,EnemyLootProfile.MaximumCurrencyRolls,()=>0),Is.EqualTo(8));
        Assert.That(CurrencyLootTable.Entries.Any(x=>x.Currency==CraftingCurrencyType.EmpowermentCatalyst),Is.False);Assert.That(CurrencyLootTable.Entries.Count(x=>CurrencyInventory.IsAncient(x.Currency)),Is.EqualTo(6));
        Assert.That(CurrencyLootTable.Choose(1,EnemyAI.EnemyRarity.Legendary,false,false,()=>.999f).Currency,Is.Not.EqualTo(CraftingCurrencyType.AncientRemoveModifier));
    }

    [Test] public void NaturalWeaponsAreUniformAndLevelOneProfilesMatchContract()
    {
        Random.InitState(185);var counts=WeaponTypeCatalog.All.ToDictionary(x=>x.Id,_=>0);for(int i=0;i<600;i++)counts[LootManager.RollWeaponTypeId()]++;foreach(var count in counts.Values)Assert.That(count,Is.InRange(70,130));
        var expected=new Dictionary<string,(float,float,float,float)>{{WeaponTypeIds.Sword,(18,27,.45f,.05f)},{WeaponTypeIds.TwoHandedAxe,(26,38,.30f,.04f)},{WeaponTypeIds.Bow,(17,25,.50f,.05f)},{WeaponTypeIds.Staff,(18,28,.40f,.06f)},{WeaponTypeIds.Dagger,(14,20,.60f,.08f)},{WeaponTypeIds.Sceptre,(19,28,.42f,.05f)}};
        foreach(var pair in expected){var profile=WeaponTypeCatalog.Get(pair.Key);Assert.That((profile.BaseDamageMin,profile.BaseDamageMax,profile.AttacksPerSecond,profile.BaseCritChance),Is.EqualTo(pair.Value));}
    }

    [TestCase(1280,720)][TestCase(1600,900)][TestCase(1920,1080)][TestCase(2560,1440)][TestCase(3440,1440)]
    public void BottomActionRowHasNoRectOverlapAtSupportedResolutions(int width,int height)
    {
        var canvasObject=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));cleanup.Add(canvasObject);var root=(RectTransform)canvasObject.transform;root.sizeDelta=new Vector2(width,height);var buttons=new List<RectTransform>();
        for(int i=0;i<5;i++){var go=new GameObject("Control "+i,typeof(RectTransform),typeof(Image),typeof(Button));cleanup.Add(go);go.transform.SetParent(root,false);BottomActionBarLayout.Attach(go.GetComponent<Button>(),10+i*10,i is 2 or 3?255:190);buttons.Add((RectTransform)go.transform);}Canvas.ForceUpdateCanvases();Assert.That(HUDRectOverlapValidator.FindOverlaps(buttons),Is.Empty);
    }

    StatsComponent Stats(){var go=new GameObject("attacker",typeof(StatsComponent),typeof(PlayerController),typeof(SubclassCombatState));cleanup.Add(go);return go.GetComponent<StatsComponent>();}
    StatusEffects Effect(StatusEffects.AilmentKind kind,StatusEffects.StatusType type){var effect=ScriptableObject.CreateInstance<StatusEffects>();cleanup.Add(effect);effect.ConfigureRuntime(kind.ToString(),type,kind,ElementMask.All,1,2,1,StatusEffects.StackPolicy.StackIndependently,1);return effect;}
}
