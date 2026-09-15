using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PoedbAffixFoundationTests
{
    const string DatabasePath="Assets/Prefabs/Scriptable Objects/ModDatabase.asset";

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
