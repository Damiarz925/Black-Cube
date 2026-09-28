using System.Linq;
using NUnit.Framework;
using BlackCube.CombatSimulation;

public sealed class ClassKeystoneFoundationTests
{
    [Test] public void CatalogHasEighteenDistinctChoicesAndThreePerClass()
    {
        var definitions = ClassKeystoneCatalog.CreateDefaults();
        Assert.That(definitions.Count, Is.EqualTo(18));
        Assert.That(definitions.Select(x => x.id).Distinct().Count(), Is.EqualTo(18));
        Assert.That(definitions.Select(x => x.StableId).Distinct().Count(), Is.EqualTo(18));
        foreach (var group in definitions.GroupBy(x => x.classId)) Assert.That(group.Count(), Is.EqualTo(3), group.Key);
        Assert.That(definitions.All(x => !string.IsNullOrWhiteSpace(x.description)), Is.True);
    }
    [TestCase(PlayerSkillId.StaffFireball, 60, 5, 2.5f, PlayerSkillCastMode.AutoCooldown)]
    [TestCase(PlayerSkillId.DaggerQuickStrike, 25, 4, 1, PlayerSkillCastMode.ImmediateCooldown)]
    public void ProjectileSkillPreservesManaCooldownAndMode(PlayerSkillId id, float mana, float cooldown, float multiplier, PlayerSkillCastMode mode)
    {
        var skill = PlayerSkillDefinition.CreateProductionDefaults().Single(x => x.id == id);
        Assert.That(skill.projectile, Is.True); Assert.That(skill.supportsPrecision, Is.True);
        Assert.That(skill.baseProjectileSpeed, Is.GreaterThan(0)); Assert.That(skill.manaCost, Is.EqualTo(mana));
        Assert.That(skill.baseCooldown, Is.EqualTo(cooldown)); Assert.That(skill.hitDamageMultiplier, Is.EqualTo(multiplier));
        Assert.That(skill.castMode, Is.EqualTo(mode));
    }
    [Test] public void SerializedUpgradeDoesNotMutateSharedCatalog()
    {
        var original = new PlayerSkillDefinition { id = PlayerSkillId.DaggerQuickStrike, projectile = false, baseProjectileSpeed = 0, manaCost = 25 };
        var upgraded = PlayerSkillDefinition.UpgradeProjectileDefinitions(new[] { original }).Single();
        Assert.That(original.projectile, Is.False); Assert.That(upgraded, Is.Not.SameAs(original));
        Assert.That(upgraded.projectile, Is.True); Assert.That(upgraded.baseProjectileSpeed, Is.EqualTo(1));
        Assert.That(upgraded.manaCost, Is.EqualTo(25));
    }
    [Test] public void ProjectileSpeedUsesAuthoredAndIncreasedValuesWithFloor()
    {
        Assert.That(WeaponMechanicProfile.ProjectileTravelTime(0), Is.EqualTo(1));
        Assert.That(WeaponMechanicProfile.ProjectileTravelTime(1), Is.EqualTo(.5f));
        Assert.That(WeaponMechanicProfile.ProjectileTravelTime(0, 2), Is.EqualTo(.5f));
        Assert.That(WeaponMechanicProfile.ProjectileTravelTime(1, 2), Is.EqualTo(.25f));
        Assert.That(WeaponMechanicProfile.ProjectileTravelTime(100, 100), Is.EqualTo(.1f));
    }
    [Test] public void ThrownDaggerCannotMultistrikeInLaboratory()
    {
        var player = new CombatantSnapshot { id = "player", player = true, weaponTypeId = WeaponTypeIds.Dagger,
            maximumLife = 1000, maximumMana = 100, attackSpeed = .01f, hitTwiceChance = 1,
            basicDamage = new CombatDamageSnapshot { physical = 1 } };
        player.skills.Add(new CombatSkillSnapshot { id = "quick", name = "Quick Strike", castMode = PlayerSkillCastMode.ImmediateCooldown,
            projectile = true, manaCost = 25, cooldown = 4, baseProjectileSpeed = 2 });
        var enemy = new CombatantSnapshot { id = "enemy", maximumLife = 1000, attackSpeed = .01f };
        var result = HeadlessCombatSimulator.Run(player, enemy, new CombatSimulationConfig { maximumDuration = 5 });
        Assert.That(result.error, Is.Null.Or.Empty); Assert.That(result.projectilesLaunched, Is.GreaterThan(0));
        Assert.That(result.projectilesImpacted, Is.GreaterThan(0)); Assert.That(result.hitTwiceCount, Is.Zero);
        Assert.That(result.projectileTravelTotal / result.projectilesLaunched, Is.EqualTo(.5f).Within(.0001));
        Assert.That(result.manaSpent, Is.GreaterThanOrEqualTo(25));
    }
}
