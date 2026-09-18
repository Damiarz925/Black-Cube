using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class Step12_5GTests
{
    const string DatabasePath="Assets/Prefabs/Scriptable Objects/ModDatabase.asset";
    readonly List<GameObject> created=new();
    UnityEngine.Random.State randomState;
    float timeScale;
    ModManager roller;
    ModDatabase database;

    [SetUp] public void SetUp()
    {
        randomState=UnityEngine.Random.state;
        timeScale=Time.timeScale;
        database=AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
        Assert.That(database,Is.Not.Null);
        var host=new GameObject("Step 12.5G isolated roller");created.Add(host);
        roller=host.AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(database);
    }
    [TearDown] public void TearDown()
    {
        for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)UnityEngine.Object.DestroyImmediate(created[i]);
        created.Clear();UnityEngine.Random.state=randomState;Time.timeScale=timeScale;
    }

    [Test] public void NaturalEquipmentHasOneImplicitAndExactBalancedExplicitCount()
    {
        foreach(LootManager.GearType slot in Enum.GetValues(typeof(LootManager.GearType)))
            foreach(LootManager.GearRarity rarity in Enum.GetValues(typeof(LootManager.GearRarity)))
                foreach(int level in new[]{1,25,50,75,100})
                {
                    var element=slot==LootManager.GearType.Weapons?Element.Fire:Element.Phys;
                    var mods=Construct(slot,rarity,level,element);
                    var actual=mods.Where(m=>!Gear.IsWeaponBaseStat(m.statType)).ToArray();
                    Assert.That(actual.Count(m=>m.lockedOriginal),Is.EqualTo(1),$"{slot} {rarity} ilvl {level}");
                    var explicitMods=actual.Where(m=>!m.lockedOriginal).ToArray();
                    int sideCap=AffixPolicy.MaximumOnSide(rarity);
                    Assert.That(explicitMods.Length,Is.EqualTo(sideCap*2));
                    Assert.That(explicitMods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Prefix),Is.EqualTo(sideCap));
                    Assert.That(explicitMods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Suffix),Is.EqualTo(sideCap));
                    var errors=new List<string>();
                    ItemizationValidator.ValidateRolledMods(slot,rarity,level,mods,database,errors,requireImplicit:true);
                    Assert.That(errors,Is.Empty,string.Join("\n",errors));
                }
    }

    [Test] public void AuthoredStarterWeaponHasAValidPermanentImplicitWithoutChangingItsBaseRange()
    {
        var host=new GameObject("Isolated starter controller");created.Add(host);
        var controller=host.AddComponent<PlayerController>();
        var starter=controller.CreateStarterWeaponForIsolatedBaseline(roller);
        Assert.That(starter.BaseDamageMin,Is.EqualTo(18f));
        Assert.That(starter.BaseDamageMax,Is.EqualTo(27f));
        Assert.That(starter.BaseAttackSpeed,Is.EqualTo(.45f));
        Assert.That(starter.BaseCritChance,Is.EqualTo(.05f));
        Assert.That(starter.ImplicitMod,Is.Not.Null);
        Assert.That(starter.CraftingModCount,Is.Zero);
        var errors=new List<string>();
        ItemizationValidator.ValidateRolledMods(starter.ItemType,starter.ItemRarity,
            starter.ItemLevel,starter.rolledMods,database,errors,requireImplicit:true);
        Assert.That(errors,Is.Empty,string.Join("\n",errors));
    }

    [Test] public void EnemyIntrinsicModifierCountsRemainAtTheirPrePassProfile()
    {
        Assert.That(Gear.RollEnemyModNumber(LootManager.GearRarity.Normal),Is.EqualTo(1));
        Assert.That(Gear.RollEnemyModNumber(LootManager.GearRarity.Magic),Is.EqualTo(2));
        for(int i=0;i<100;i++)
        {
            Assert.That(Gear.RollEnemyModNumber(LootManager.GearRarity.Rare),Is.InRange(3,4));
            Assert.That(Gear.RollEnemyModNumber(LootManager.GearRarity.Legendary),Is.InRange(5,6));
        }
        foreach(LootManager.GearRarity rarity in Enum.GetValues(typeof(LootManager.GearRarity)))
        {
            int count=Gear.RollEnemyModNumber(rarity);
            var mods=roller.RollModsForItem(LootManager.GearType.Rings,rarity,100,count,
                Element.Phys,forEnemy:true);
            Assert.That(mods.Count,Is.LessThanOrEqualTo(AffixPolicy.EnemyMaximumTotal(rarity)));
            Assert.That(mods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Prefix),
                Is.LessThanOrEqualTo(AffixPolicy.EnemyMaximumOnSide(rarity)));
            Assert.That(mods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Suffix),
                Is.LessThanOrEqualTo(AffixPolicy.EnemyMaximumOnSide(rarity)));
        }
    }

    [Test] public void PermanentImplicitCanOriginateFromEitherPrefixOrSuffixFamilies()
    {
        UnityEngine.Random.InitState(125501);
        int prefixes=0,suffixes=0;
        for(int i=0;i<250;i++)
        {
            var mods=Construct(LootManager.GearType.Rings,LootManager.GearRarity.Normal,100,Element.Phys);
            var implicitMod=mods.Single(m=>m.lockedOriginal);
            if(AffixPolicy.Side(implicitMod.statType)==AffixSide.Prefix)prefixes++;
            else suffixes++;
        }
        Assert.That(prefixes,Is.GreaterThan(0));
        Assert.That(suffixes,Is.GreaterThan(0));
    }

    [Test] public void ImplicitFamilyCanRepeatOneExplicitButExplicitDuplicatesAreRejected()
    {
        var definition=database.GetDefinition(StatTypes.FireRes);
        var tier=ModManager.ApplicableTiers(definition,LootManager.GearType.Rings)
            .First(t=>t.minItemLevel<=100);
        float value=(tier.minValue+tier.maxValue)*.5f;
        var mods=new List<RolledMod>{new(StatTypes.FireRes,tier.tierIndex,value,true),
            new(StatTypes.FireRes,tier.tierIndex,value,false)};
        var errors=new List<string>();
        ItemizationValidator.ValidateRolledMods(LootManager.GearType.Rings,
            LootManager.GearRarity.Magic,100,mods,database,errors,requireImplicit:true);
        Assert.That(errors,Is.Empty,string.Join("\n",errors));
        mods.Add(new RolledMod(StatTypes.FireRes,tier.tierIndex,value,false));
        errors.Clear();
        ItemizationValidator.ValidateRolledMods(LootManager.GearType.Rings,
            LootManager.GearRarity.Rare,100,mods,database,errors,requireImplicit:true);
        Assert.That(errors,Has.Some.Contains("duplicate explicit family"));
    }

    [Test] public void CraftingChangesOnlyExplicitsAndLeavesUnderFilledItemsUnderFilled()
    {
        var gear=NewGear(LootManager.GearRarity.Normal);
        gear.RestoreCraftingState(LootManager.GearRarity.Legendary,14,14); // Legacy operation-coverage fixture; Step14_5FoundationTests owns natural budgets.
        gear.ApplyMods(Construct(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement));
        var implicitMod=gear.ImplicitMod;
        Assert.That(gear.CraftingModCount,Is.Zero);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.NormalToMagic,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(2));
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
        AssertSides(gear,1);
        var before=Explicits(gear);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RerollMagic,gear,roller),Is.True);
        var after=Explicits(gear);
        Assert.That(before.Count(m=>after.Contains(m)),Is.EqualTo(1));
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
        AssertSides(gear,1);
        before=Explicits(gear);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.MagicToRare,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(4));
        Assert.That(before.All(m=>gear.rolledMods.Contains(m)),Is.True);
        AssertSides(gear,2);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.AddRareModifier,gear,roller),Is.False);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(3));
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RerollRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(3));
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.AddRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(4));
        gear.RestoreCraftingState(LootManager.GearRarity.Legendary,14,14);
        for(int i=0;i<4;i++)Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.Zero);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,roller),Is.False);
        Assert.That(gear.ItemRarity,Is.EqualTo(LootManager.GearRarity.Rare));
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
    }

    [Test] public void LegendaryCraftingUsesSixExplicitCapacityAndNeverTargetsImplicit()
    {
        var gear=NewGear(LootManager.GearRarity.Legendary);
        gear.ApplyMods(Construct(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement));
        var implicitMod=gear.ImplicitMod;
        AssertSides(gear,3);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.AddRareModifier,gear,roller),Is.False);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RerollRareModifier,gear,roller),Is.True);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(5));
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.AddRareModifier,gear,roller),Is.True);
        AssertSides(gear,3);
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
    }

    [Test] public void UnderFilledMagicToRareAddsExactlyTwoAndPreservesItsRemainingExplicit()
    {
        var gear=NewGear(LootManager.GearRarity.Normal);
        gear.ApplyMods(Construct(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement));
        var implicitMod=gear.ImplicitMod;
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.NormalToMagic,gear,roller),Is.True);
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.RemoveRareModifier,gear,roller),Is.True);
        Assert.That(gear.CraftingModCount,Is.EqualTo(1));
        var remaining=Explicits(gear).Single();
        Assert.That(EquipmentCrafting.TryApply(CraftingCurrencyType.MagicToRare,gear,roller),Is.True);
        Assert.That(gear.ItemRarity,Is.EqualTo(LootManager.GearRarity.Rare));
        Assert.That(gear.CraftingModCount,Is.EqualTo(3));
        Assert.That(gear.rolledMods.Contains(remaining),Is.True);
        Assert.That(gear.ImplicitMod,Is.SameAs(implicitMod));
        Assert.That(Explicits(gear).Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Prefix),
            Is.LessThanOrEqualTo(2));
        Assert.That(Explicits(gear).Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Suffix),
            Is.LessThanOrEqualTo(2));
    }

    [Test] public void CodexFamiliesAndTiersAreExactlyTheAuthoritativeSlotDefinitions()
    {
        foreach(LootManager.GearType slot in Enum.GetValues(typeof(LootManager.GearType)))
        {
            var all=CodexModCatalog.Families(database,slot,CodexSideFilter.All);
            var expected=GearStatLists.GetCanonicalStatPoolForType(slot).Distinct()
                .Where(stat=>!Gear.IsWeaponBaseStat(stat))
                .Where(stat=>database.GetDefinition(stat)!=null)
                .Where(stat=>ModManager.ApplicableTiers(database.GetDefinition(stat),slot).Count>0)
                .ToArray();
            Assert.That(all.Select(f=>f.Stat),Is.EquivalentTo(expected),slot.ToString());
            foreach(var family in all)
            {
                var underlying=ModManager.ApplicableTiers(family.Definition,slot);
                Assert.That(family.Tiers.Select(t => (t.tierIndex,t.minValue,t.maxValue,
                        t.minItemLevel,t.weight,t.pairedDamage,t.minHighValue,t.maxHighValue)),
                    Is.EquivalentTo(underlying.Select(t => (t.tierIndex,t.minValue,t.maxValue,
                        t.minItemLevel,t.weight,t.pairedDamage,t.minHighValue,t.maxHighValue))));
                Assert.That(family.Tiers.First().tierIndex,Is.EqualTo(1));
            }
            Assert.That(CodexModCatalog.Families(database,slot,CodexSideFilter.Prefixes)
                .All(f=>f.Definition.side==AffixSide.Prefix),Is.True);
            Assert.That(CodexModCatalog.Families(database,slot,CodexSideFilter.Suffixes)
                .All(f=>f.Definition.side==AffixSide.Suffix),Is.True);
            Assert.That(CodexModCatalog.Format(database,slot,CodexSideFilter.All),
                Does.Contain("permanent Implicit"));
        }
    }

    [Test] public void CodexUiRendersActiveDatabaseAndScrollableSideFilteredTierRows()
    {
        var canvasHost=new GameObject("Codex reference canvas",typeof(RectTransform),typeof(Canvas));
        created.Add(canvasHost);
        ((RectTransform)canvasHost.transform).sizeDelta=new Vector2(1600,900);
        var codex=canvasHost.AddComponent<CodexModListUI>();
        codex.Initialize(canvasHost.transform,()=>{});
        codex.OpenCodex();codex.OpenModList();
        codex.SelectType(LootManager.GearType.BodyArmours);
        codex.SelectFilter(CodexSideFilter.Suffixes);
        Assert.That(codex.Scroll,Is.Not.Null);
        Assert.That(codex.Scroll.vertical,Is.True);
        Assert.That(codex.VisibleList,Does.Contain("permanent Implicit"));
        Assert.That(codex.VisibleList,Does.Contain("T1"));
        Assert.That(codex.VisibleList,Does.Contain("ilvl"));
        Assert.That(codex.SelectedType,Is.EqualTo(LootManager.GearType.BodyArmours));
        Assert.That(codex.SelectedFilter,Is.EqualTo(CodexSideFilter.Suffixes));
        codex.ReturnToCodex();
        Assert.That(codex.IsCodexPageOpen,Is.True);
    }

    [Test] public void CanonicalAncientIconsMapToSuppliedSpritesWithoutGeneratedCurrencyGlyphs()
    {
        var mapping=new Dictionary<CraftingCurrencyType,string>
        {
            [CraftingCurrencyType.AncientNormalToMagic]="AncientWhiteToBlue",
            [CraftingCurrencyType.AncientMagicToRare]="AncientBlueToYellow",
            [CraftingCurrencyType.AncientRareToLegendary]="AncientYellowReroll",
            [CraftingCurrencyType.AncientReroll]="AncientBlueReroll",
            [CraftingCurrencyType.AncientAddModifier]="AncientAddYellow",
            [CraftingCurrencyType.AncientRemoveModifier]="AncientRemove"
        };
        foreach(var pair in mapping)
        {
            string path="Assets/Resources/UI/Currency/"+pair.Value+".png";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite,Is.Not.Null,path);
            Assert.That(InventoryArtCatalog.ResourcePath(pair.Key),Is.EqualTo("UI/Currency/"+pair.Value));
            Assert.That(InventoryArtCatalog.Currency(pair.Key),Is.EqualTo(sprite));
            Assert.That(sprite.name,Does.Not.StartWith("currency/"));
        }
    }

    [Test] public void ArmedAncientCursorUsesTheSameCanonicalArtAsInventory()
    {
        var canvasHost=new GameObject("Ancient cursor canvas",typeof(RectTransform),typeof(Canvas));
        created.Add(canvasHost);
        var currencyHost=new GameObject("Ancient currency stack",typeof(CurrencyInventory));
        created.Add(currencyHost);
        var currency=currencyHost.GetComponent<CurrencyInventory>();
        typeof(CurrencyInventory).GetProperty("Instance",BindingFlags.Public|BindingFlags.Static)
            .GetSetMethod(true).Invoke(null,new object[]{currency});
        var cursor=CraftingCurrencyCursorUI.Ensure(canvasHost.GetComponent<Canvas>());
        foreach(CraftingCurrencyType type in new[]{CraftingCurrencyType.AncientNormalToMagic,
            CraftingCurrencyType.AncientMagicToRare,CraftingCurrencyType.AncientRareToLegendary,
            CraftingCurrencyType.AncientReroll,CraftingCurrencyType.AncientAddModifier,
            CraftingCurrencyType.AncientRemoveModifier})
        {
            currency.Add(type);
            Assert.That(currency.Arm(type),Is.True);
            Assert.That(cursor.Icon.sprite,Is.SameAs(InventoryArtCatalog.Currency(type)));
            Assert.That(cursor.Icon.sprite,Is.Not.Null);
            currency.CancelArmed();
        }
    }

    [Test] public void FullAddAndEmptyRemoveFailWithoutConsumingCurrency()
    {
        var gear=NewGear(LootManager.GearRarity.Rare);
        gear.ApplyMods(Construct(gear.ItemType,gear.ItemRarity,gear.ItemLevel,gear.BaseElement));
        var host=new GameObject("Failure currency stack",typeof(CurrencyInventory));created.Add(host);
        var currency=host.GetComponent<CurrencyInventory>();
        typeof(CurrencyInventory).GetProperty("Instance",BindingFlags.Public|BindingFlags.Static)
            .GetSetMethod(true).Invoke(null,new object[]{currency});
        currency.Add(CraftingCurrencyType.AddRareModifier);
        Assert.That(currency.Arm(CraftingCurrencyType.AddRareModifier),Is.True);
        Assert.That(currency.TryApplyArmedToGear(gear),Is.False);
        Assert.That(currency.Count(CraftingCurrencyType.AddRareModifier),Is.EqualTo(1));
        gear.rolledMods.RemoveAll(m=>m!=null&&!m.lockedOriginal&&!Gear.IsWeaponBaseStat(m.statType));
        gear.RebuildMods();
        currency.Add(CraftingCurrencyType.RemoveRareModifier);
        Assert.That(currency.Arm(CraftingCurrencyType.RemoveRareModifier),Is.True);
        Assert.That(currency.TryApplyArmedToGear(gear),Is.False);
        Assert.That(currency.Count(CraftingCurrencyType.RemoveRareModifier),Is.EqualTo(1));
    }

    [Test] public void EquipmentTooltipShowsCompactImplicitThenPrefixesThenSuffixesWithRealPadlock()
    {
        var gear=NewGear(LootManager.GearRarity.Magic);
        var mods=new List<RolledMod>
        {
            LegalMod(StatTypes.FireRes,gear.ItemType,true),
            LegalMod(StatTypes.Life,gear.ItemType,false),
            LegalMod(StatTypes.Strength,gear.ItemType,false)
        };
        gear.ApplyMods(mods);
        var canvasHost=new GameObject("Tooltip reference canvas",typeof(RectTransform),typeof(Canvas));
        created.Add(canvasHost);
        ((RectTransform)canvasHost.transform).sizeDelta=new Vector2(1600,900);
        var anchor=new GameObject("Tooltip anchor",typeof(RectTransform));
        anchor.transform.SetParent(canvasHost.transform,false);
        var tooltip=ItemTooltipUI.Create(canvasHost.transform);
        tooltip.Show(gear,(RectTransform)anchor.transform,true,requirePlayerEquipment:false);
        string text=tooltip.BodyText;
        Assert.That(text.IndexOf("IMPLICIT",StringComparison.Ordinal),Is.LessThan(text.IndexOf(ItemTooltipFormatter.Divider,StringComparison.Ordinal)));
        Assert.That(text.IndexOf(ItemTooltipFormatter.Divider,StringComparison.Ordinal),Is.LessThan(text.IndexOf("PREFIXES",StringComparison.Ordinal)));
        Assert.That(text.IndexOf("PREFIXES",StringComparison.Ordinal),Is.LessThan(text.IndexOf("SUFFIXES",StringComparison.Ordinal)));
        Assert.That(text,Does.Contain("T"));
        Assert.That(text,Does.Contain("–"));
        Assert.That(text,Does.Not.Contain("LOCKED"));
        Assert.That(text,Does.Not.Contain("UNLOCKED"));
        Assert.That(tooltip.ImplicitLockIcon.gameObject.activeSelf,Is.True);
        Assert.That(tooltip.ImplicitLockIcon.sprite,Is.Not.Null);
        var body=(TMP_Text)typeof(ItemTooltipUI).GetField("body",BindingFlags.Instance|BindingFlags.NonPublic)
            .GetValue(tooltip);
        Assert.That(body.fontSize,Is.EqualTo(12));
        string lifeLabel=StatDisplayFormatting.ToFriendlyName(StatTypes.Life)+":";
        string lifeLine=text.Split('\n').First(line=>line.IndexOf(lifeLabel,StringComparison.Ordinal)>=0);
        Assert.That(body.GetPreferredValues(lifeLine).x,Is.LessThan(416f));
    }

    [Test] public void FiftyThousandLevelAwareRarityRollsReachConstructibleLegendaries()
    {
        var host=new GameObject("Seeded rarity audit");created.Add(host);
        var loot=host.AddComponent<LootManager>();
        var filterHost=new GameObject("Seeded filter probe",typeof(Inventory));created.Add(filterHost);
        var inventory=filterHost.GetComponent<Inventory>();
        foreach(string field in new[]{"filterLevelEnabled","filterRarityEnabled","filterModMismatchEnabled"})
            typeof(Inventory).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(inventory,false);
        var gearHost=new GameObject("Legendary pickup filter probe",typeof(Gear));created.Add(gearHost);
        var filterProbe=gearHost.GetComponent<Gear>();
        const int rollsPerLevel=10000;
        var report=new System.Text.StringBuilder();
        report.AppendLine("Step 12.5G seeded Legendary constructibility audit using current level-aware Step 13 player-drop rates.");
        foreach(int level in new[]{1,25,50,75,100})
        {
            UnityEngine.Random.InitState(125000+level);
            int legendary=0,constructed=0,failures=0,legalityFailures=0,filterRejections=0;
            for(int i=0;i<rollsPerLevel;i++)
            {
                if(loot.RollItemRarity(level)!=LootManager.GearRarity.Legendary)continue;
                legendary++;
                var slot=loot.RollItemType();
                var element=slot==LootManager.GearType.Weapons?Element.Fire:Element.Phys;
                List<RolledMod> mods=null;
                for(int attempt=0;attempt<64&&mods==null;attempt++)
                    mods=roller.RollEquipmentModsForItem(slot,LootManager.GearRarity.Legendary,level,element);
                if(mods==null){failures++;continue;}
                constructed++;
                filterProbe.Initialize(slot,LootManager.GearRarity.Legendary,level,element);
                if(inventory.MatchesFilter(filterProbe))filterRejections++;
                var errors=new List<string>();
                ItemizationValidator.ValidateRolledMods(slot,LootManager.GearRarity.Legendary,
                    level,mods,database,errors,requireImplicit:true);
                if(errors.Count>0)legalityFailures++;
            }
            report.AppendLine($"ilvl {level}: rolls {rollsPerLevel}, Legendary {legendary} "
                +$"({legendary*100f/rollsPerLevel:0.###}%), constructed {constructed}, "
                +$"failures {failures}, legality failures {legalityFailures}, "
                +$"filter rejections {filterRejections} with filters disabled "
                +"(configured pickup filters may auto-dismantle).");
            Assert.That(legendary,Is.GreaterThan(0),$"ilvl {level}");
            Assert.That(failures,Is.Zero,$"ilvl {level}");
            Assert.That(legalityFailures,Is.Zero,$"ilvl {level}");
            Assert.That(filterRejections,Is.Zero,$"ilvl {level}");
        }
        string output=Path.GetFullPath("Logs/Step12_5G-legendary-drop.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output,report.ToString());
    }

    [Test] public void Step13PlayerRarityBandsAreSmoothAndRemainSeparateFromEnemyRarity()
    {
        var host=new GameObject("Level-aware player rarity");created.Add(host);
        var loot=host.AddComponent<LootManager>();
        var expected=new Dictionary<int,Vector4>
        {
            {1,new Vector4(60,31,8,1)},
            {20,new Vector4(42,39,17,2)},
            {50,new Vector4(28,41,28,3)},
            {75,new Vector4(19,35,42,4)},
            {100,new Vector4(19,35,42,4)}
        };
        foreach(var pair in expected)
        {
            Vector4 rates=LootManager.RarityRatesForLevel(pair.Key);
            Assert.That(rates,Is.EqualTo(pair.Value));
            Assert.That(rates.x+rates.y+rates.z+rates.w,Is.EqualTo(100f).Within(.001f));
            UnityEngine.Random.InitState(130000+pair.Key);
            int[] counts=new int[4];
            for(int i=0;i<50000;i++)counts[(int)loot.RollItemRarity(pair.Key)]++;
            float[] targets={rates.x,rates.y,rates.z,rates.w};
            for(int rarity=0;rarity<4;rarity++)
                Assert.That(counts[rarity]*100f/50000f,
                    Is.EqualTo(targets[rarity]).Within(.65f),$"level {pair.Key} rarity {rarity}");
        }
        foreach(int level in new[]{2,10,19,21,35,49,51,65,74})
        {
            Vector4 rates=LootManager.RarityRatesForLevel(level);
            Assert.That(rates.x+rates.y+rates.z+rates.w,Is.EqualTo(100f).Within(.001f));
            Assert.That((rates-LootManager.RarityRatesForLevel(level-1)).magnitude,Is.LessThan(2f));
        }
    }

    List<RolledMod> Construct(LootManager.GearType slot,LootManager.GearRarity rarity,
        int level,Element element)
    {
        List<RolledMod> mods=null;
        for(int attempt=0;attempt<64&&mods==null;attempt++)
            mods=roller.RollEquipmentModsForItem(slot,rarity,level,element);
        Assert.That(mods,Is.Not.Null,$"Cannot construct {rarity} {slot} at ilvl {level}");
        return mods;
    }
    Gear NewGear(LootManager.GearRarity rarity)
    {
        var go=new GameObject("Step 12.5G gear");created.Add(go);
        var gear=go.AddComponent<Gear>();
        gear.Initialize(LootManager.GearType.BodyArmours,rarity,100,Element.Phys);
        return gear;
    }
    RolledMod LegalMod(StatTypes stat,LootManager.GearType slot,bool implicitMod)
    {
        var def=database.GetDefinition(stat);
        var tier=ModManager.ApplicableTiers(def,slot).First(t=>t.minItemLevel<=100);
        return new RolledMod(stat,tier.tierIndex,(tier.minValue+tier.maxValue)*.5f,implicitMod);
    }
    static List<RolledMod> Explicits(Gear gear)=>gear.rolledMods
        .Where(m=>m!=null&&!m.lockedOriginal&&!Gear.IsWeaponBaseStat(m.statType)).ToList();
    static void AssertSides(Gear gear,int expected)
    {
        var mods=Explicits(gear);
        Assert.That(mods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Prefix),Is.EqualTo(expected));
        Assert.That(mods.Count(m=>AffixPolicy.Side(m.statType)==AffixSide.Suffix),Is.EqualTo(expected));
    }
}
