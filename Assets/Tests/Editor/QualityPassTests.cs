using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class QualityPassTests
{
    [Test]
    public void DestroyedAilmentSourceDoesNotReenterUniqueRuntime()
    {
        var source=new GameObject("Temporary enemy source");
        var stats=source.AddComponent<StatsComponent>();
        Object.DestroyImmediate(source);
        Assert.DoesNotThrow(()=>UniqueCombatRuntime.For(stats));
        Assert.That(UniqueCombatRuntime.For(stats),Is.Null);
    }

    [Test]
    public void TrueDamageBypassesDefensiveStats()
    {
        var target=new GameObject("True damage defender");
        try
        {
            var stats=target.AddComponent<StatsComponent>();
            stats.AddModifier(new StatModifier(StatTypes.FlatArmour,StatOp.Flat,100000f,this));
            stats.AddModifier(new StatModifier(StatTypes.PhysicalDamageReduction,StatOp.Flat,.9f,this));
            stats.AddModifier(new StatModifier(StatTypes.AllRes,StatOp.Flat,.75f,this));
            var hit=new DamageContext(1);hit.AddDamage(Element.True,42f);
            Assert.That(CombatCalculator.CalculateFinalDamage(hit,null,stats),Is.EqualTo(42f).Within(.001f));
        }
        finally{Object.DestroyImmediate(target);}
    }

    [Test]
    public void OnlyBowAndStaffAreRanged()
    {
        var ranged=WeaponTypeCatalog.All.Where(w=>w.IsRanged).Select(w=>w.Id).ToArray();
        Assert.That(ranged,Is.EquivalentTo(new[]{WeaponTypeIds.Bow,WeaponTypeIds.Staff}));
    }

    [Test]
    public void ClassSpecificRecoveryMeetsOrdinaryReferencePremium()
    {
        var priest=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Priest);
        var light=priest.AllAuthoredNodes().Where(n=>n.StableId.Contains("priest-light"))
            .SelectMany(n=>n.Effects).Where(e=>e.Stat==StatTypes.LifeOnHit).ToArray();
        Assert.That(light.Length,Is.GreaterThan(0));
        Assert.That(light.All(e=>e.Value>=1.2f),Is.True);
    }

    [Test]
    public void AuthoredGameplayPrefabContainsNewOptionsAndDismantleSafety()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaperBattle/PaperBattle.prefab");
        Assert.That(prefab,Is.Not.Null);
        var pause=prefab.GetComponentInChildren<PauseMenuView>(true);
        Assert.That(pause?.autoRestartButton,Is.Not.Null);
        Assert.That(pause?.upgradeDiagnosticsButton,Is.Not.Null);
        Assert.That(prefab.GetComponentInChildren<DismantleConfirmationUI>(true),Is.Not.Null);
    }

    [Test]
    public void MainMenuAuthoringDoesNotDuplicateSelectionPanels()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainMenu.prefab");
        Assert.That(prefab,Is.Not.Null);
        foreach(var name in new[]{"Options Panel","Options Button","New Game Class Selection","Character Slot Selection","New Game Overwrite Confirmation"})
            Assert.That(prefab.GetComponentsInChildren<Transform>(true).Count(t=>t.name==name),Is.EqualTo(1),name);
    }

    [Test]
    public void ClassRowLayoutHasRoomForBranchWidth()
    {
        var layout=AssetDatabase.LoadAssetAtPath<PassiveTreeLayoutSO>("Assets/Resources/GameData/PassiveTree/SO_PassiveTreeLayout.asset");
        Assert.That(layout,Is.Not.Null);
        Assert.That(layout.classRowSpacing,Is.GreaterThanOrEqualTo(900f));
        Assert.That(layout.classRowHeight,Is.GreaterThan(0));
    }

    [Test]
    public void WeaponAffixForUnarmedLowestAttributeHasDedicatedTiers()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        var def=db.GetDefinition(StatTypes.DmgPerLowestStat);
        Assert.That(def.displayName,Does.Contain("Unarmed"));
        Assert.That(def.tiers[0].minValue,Is.EqualTo(.1f).Within(.001f));
        Assert.That(def.tiers[4].maxValue,Is.EqualTo(1.2f).Within(.001f));
    }
}
