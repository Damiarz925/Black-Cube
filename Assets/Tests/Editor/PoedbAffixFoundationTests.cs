using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PoedbAffixFoundationTests
{
    const string DatabasePath="Assets/Prefabs/Scriptable Objects/ModDatabase.asset";

    [Test] public void ReusableValidatorChecksCatalogAndReachableFilterCategories()
    {
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
        var errors=new List<string>();
        ItemizationValidator.ValidateCatalog(database,errors);
        Assert.That(errors,Is.Empty,string.Join("\n",errors));
        Assert.That(System.Array.Exists(InventoryModFilter.SimpleCategories,
            entry=>entry.Category is ModFilterCategory.Evasion or ModFilterCategory.Block
                or ModFilterCategory.Accuracy or ModFilterCategory.Cooldown),Is.False);
    }

    [Test] public void ReusableValidatorRejectsIllegalTierSideCapAndUnhandledPair()
    {
        var database=AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
        var errors=new List<string>();
        var bad=new List<RolledMod>{new(StatTypes.Life,1,99999f),
            new(StatTypes.Life,1,99999f),new(StatTypes.FlatArmour,1,5f)};
        ItemizationValidator.ValidateRolledMods(LootManager.GearType.BodyArmours,
            LootManager.GearRarity.Magic,1,bad,database,errors);
        Assert.That(errors,Has.Some.Contains("duplicate family"));
        Assert.That(errors,Has.Some.Contains("illegal at ilvl"));
        Assert.That(errors,Has.Some.Contains("capacity violated"));
        errors.Clear();
        var invalidPair=new AffixTier{tierIndex=1,minItemLevel=1,weight=1,
            minValue=1,maxValue=2,pairedDamage=true,minHighValue=3,maxHighValue=4};
        ItemizationValidator.ValidateTiers(StatTypes.FlatPhys,LootManager.GearType.Weapons,
            new[]{invalidPair},errors);
        Assert.That(errors,Is.Empty);
        invalidPair.maxHighValue=0f;
        ItemizationValidator.ValidateTiers(StatTypes.FlatPhys,LootManager.GearType.Weapons,
            new[]{invalidPair},errors);
        Assert.That(errors,Has.Some.Contains("paired high-roll"));
    }

    [Test] public void DirectFamiliesUseVariableSlotRelativeCountsAndStrongestT1()
    {
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.Life,LootManager.GearType.BodyArmours,out var body),Is.True);
        Assert.That(body.Count,Is.EqualTo(13));
        Assert.That(body[0].tierIndex,Is.EqualTo(13));
        Assert.That(body[12].tierIndex,Is.EqualTo(1));
        Assert.That(body[12].minValue,Is.EqualTo(175));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.Life,LootManager.GearType.Rings,out var ring),Is.True);
        Assert.That(ring.Count,Is.EqualTo(8));
        Assert.That(ring[7].tierIndex,Is.EqualTo(1));
        Assert.That(ring[7].maxValue,Is.EqualTo(114));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.Mana,LootManager.GearType.Belts,out var beltMana),Is.True);
        Assert.That(beltMana.Count,Is.EqualTo(11));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.Mana,LootManager.GearType.Rings,out var ringMana),Is.True);
        Assert.That(ringMana.Count,Is.EqualTo(13));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.FireRes,LootManager.GearType.Weapons,out var forbidden),Is.True);
        Assert.That(forbidden,Is.Empty);
    }

    [Test] public void ColdAndLightningLocalPairsMatchVerifiedOrdinaryRows()
    {
        PoedbAffixCatalog.TryGet(StatTypes.FlatCold,LootManager.GearType.Weapons,out var cold);
        AffixTier twelve=cold.Find(t=>t.minItemLevel==12);
        Assert.That((twelve.minValue,twelve.maxValue,twelve.minHighValue,twelve.maxHighValue),
            Is.EqualTo((12f,17f,26f,30f)));
        PoedbAffixCatalog.TryGet(StatTypes.FlatLight,LootManager.GearType.Weapons,out var lightning);
        Assert.That(lightning.Count,Is.EqualTo(10));
        AffixTier three=lightning.Find(t=>t.minItemLevel==3);
        AffixTier fortyTwo=lightning.Find(t=>t.minItemLevel==42);
        Assert.That((three.minValue,three.minHighValue,three.maxHighValue),Is.EqualTo((1f,5f,6f)));
        Assert.That((fortyTwo.minValue,fortyTwo.maxValue,fortyTwo.minHighValue,fortyTwo.maxHighValue),
            Is.EqualTo((5f,8f,112f,131f)));
        Assert.That(lightning.FindAll(t=>t.minItemLevel<=90).Count,Is.EqualTo(10),
            "Higher item level keeps all eligible lower tiers.");
    }

    [Test] public void OrdinaryCriticalRowsAreWeaponLocalOrAmuletGlobal()
    {
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.CritChance,LootManager.GearType.Weapons,
            out var weaponChance),Is.True);
        Assert.That(weaponChance.Count,Is.EqualTo(6));
        Assert.That((weaponChance[0].minItemLevel,weaponChance[0].minValue,
            weaponChance[5].minItemLevel,weaponChance[5].maxValue),
            Is.EqualTo((1,10f,73,38f)));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.CritChance,LootManager.GearType.Amulets,
            out var amuletChance),Is.True);
        Assert.That(amuletChance[5].minItemLevel,Is.EqualTo(72));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.CritMult,LootManager.GearType.Weapons,
            out var weaponMultiplier),Is.True);
        Assert.That((weaponMultiplier[0].minValue,weaponMultiplier[5].minItemLevel),
            Is.EqualTo((10f,73)));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.CritMult,LootManager.GearType.Amulets,
            out var amuletMultiplier),Is.True);
        Assert.That((amuletMultiplier[0].minValue,amuletMultiplier[5].minItemLevel),
            Is.EqualTo((8f,74)));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.CritMult,LootManager.GearType.Gloves,
            out _),Is.False,"Glove historical family has no ordinary non-influenced direct mapping.");
        Assert.That(AffixPolicy.Side(StatTypes.CritChance),Is.EqualTo(AffixSide.Suffix));
        Assert.That(AffixPolicy.Side(StatTypes.CritMult),Is.EqualTo(AffixSide.Suffix));
    }

    [Test] public void OrdinaryFlatLifeRegenerationUsesSlotRelativeBodyAndHelmetRows()
    {
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.LifeRegeneration,
            LootManager.GearType.Helmets,out var helmet),Is.True);
        Assert.That(helmet.Count,Is.EqualTo(9));
        Assert.That((helmet[0].minItemLevel,helmet[0].minValue,
            helmet[8].minItemLevel,helmet[8].maxValue),Is.EqualTo((1,1f,78,128f)));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.LifeRegeneration,
            LootManager.GearType.BodyArmours,out var body),Is.True);
        Assert.That(body.Count,Is.EqualTo(11));
        Assert.That((body[10].minItemLevel,body[10].minValue,body[10].maxValue),
            Is.EqualTo((86,152.1f,176f)));
        Assert.That(AffixPolicy.Side(StatTypes.LifeRegeneration),Is.EqualTo(AffixSide.Suffix));
        Assert.That(PoedbAffixCatalog.TryGet(StatTypes.ManaRegeneration,
            LootManager.GearType.BodyArmours,out _),Is.False,
            "Flat Black-Cube Mana/sec must not import PoE's increased-rate percent family.");
    }

    [Test] public void PrefixSuffixCapacityCountsLockedOriginalOnItsRealSide()
    {
        var prefix=new RolledMod(StatTypes.Life,1,100f,true);
        var suffix=new RolledMod(StatTypes.FireRes,1,40f);
        Assert.That(AffixPolicy.Side(prefix.statType),Is.EqualTo(AffixSide.Prefix));
        Assert.That(AffixPolicy.Side(suffix.statType),Is.EqualTo(AffixSide.Suffix));
        Assert.That(AffixPolicy.CanAdd(new List<RolledMod>{prefix},LootManager.GearRarity.Magic,AffixSide.Prefix),Is.False);
        Assert.That(AffixPolicy.CanAdd(new List<RolledMod>{prefix},LootManager.GearRarity.Magic,AffixSide.Suffix),Is.True);
        Assert.That(AffixPolicy.CanAdd(new List<RolledMod>{prefix,suffix},LootManager.GearRarity.Magic,AffixSide.Suffix),Is.False);
        var legendary=new List<RolledMod>{prefix,new(StatTypes.Mana,1,50f),
            new(StatTypes.ArmourPercent,1,80f),suffix,new(StatTypes.ColdRes,1,40f)};
        Assert.That(AffixPolicy.CanAdd(legendary,LootManager.GearRarity.Legendary,AffixSide.Prefix),Is.False);
        Assert.That(AffixPolicy.CanAdd(legendary,LootManager.GearRarity.Legendary,AffixSide.Suffix),Is.True);
        legendary.Add(new RolledMod(StatTypes.LightRes,1,40f));
        Assert.That(AffixPolicy.CanAdd(legendary,LootManager.GearRarity.Legendary,AffixSide.Suffix),Is.False);
        Assert.That(AffixPolicy.CanAdd(new List<RolledMod>{prefix,suffix},LootManager.GearRarity.Magic,
            AffixSide.Suffix,suffix),Is.True,"A reroll excludes only the replaced unlocked modifier.");
    }

    [Test] public void ItemTooltipShowsExactPairedTierRangeSideAndLocalLockFlags()
    {
        var go=new GameObject("fire tier tooltip");
        try
        {
            var weapon=go.AddComponent<Gear>();
            weapon.Initialize(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,26,Element.Fire);
            weapon.BaseDamage=weapon.BaseDamageMin=weapon.BaseDamageMax=80f;
            weapon.ApplyMods(new List<RolledMod>{new(StatTypes.FlatFire,7,21f,38f,true)});
            string text=ItemTooltipFormatter.DescribeGear(weapon);
            Assert.That(text,Does.Contain("PREFIXES"));
            Assert.That(text,Does.Contain("Adds 21–38 Fire Damage"));
            Assert.That(text,Does.Contain("T7 (min 17–24, max 35–41)"));
            Assert.That(text,Does.Contain("LOCAL, LOCKED ORIGINAL"));
            Assert.That(text.IndexOf("PREFIXES"),Is.LessThan(text.IndexOf("SUFFIXES")));
        }
        finally{Object.DestroyImmediate(go);}
    }

    [Test] public void LootRollerGeneratesOnlyLegalSideDistributions()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
        Assert.That(db,Is.Not.Null);
        var go=new GameObject("affix policy roller");
        Random.State old=Random.state;
        try
        {
            var roller=go.AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(db);
            foreach(var rarity in new[]{LootManager.GearRarity.Normal,LootManager.GearRarity.Magic,
                LootManager.GearRarity.Rare,LootManager.GearRarity.Legendary})
            {
                for(int seed=0;seed<24;seed++)
                {
                    Random.InitState(67000+seed+(int)rarity*100);
                    int total=rarity==LootManager.GearRarity.Normal?1:rarity==LootManager.GearRarity.Magic?2:
                        rarity==LootManager.GearRarity.Rare?4:6;
                    var mods=roller.RollModsForItem(LootManager.GearType.Rings,rarity,90,total,Element.Phys);
                    int prefix=0,suffix=0;
                    foreach(var mod in mods)if(AffixPolicy.Side(mod.statType)==AffixSide.Prefix)prefix++;else suffix++;
                    Assert.That(mods.Count,Is.LessThanOrEqualTo(AffixPolicy.MaximumTotal(rarity)));
                    Assert.That(prefix,Is.LessThanOrEqualTo(AffixPolicy.MaximumOnSide(rarity)));
                    Assert.That(suffix,Is.LessThanOrEqualTo(AffixPolicy.MaximumOnSide(rarity)));
                    Assert.That(new HashSet<StatTypes>(mods.ConvertAll(x=>x.statType)).Count,Is.EqualTo(mods.Count));
                }
            }
        }
        finally {Random.state=old;Object.DestroyImmediate(go);}
    }
}
