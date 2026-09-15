using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ObsoleteResetRemovalTests
{
    GameObject host;

    [TearDown]
    public void TearDown()
    {
        if (host != null) UnityEngine.Object.DestroyImmediate(host);
        GamePersistence.RequestNewGame();
    }

    [Test]
    public void RuntimeAssemblyHasNoPrestigeEntryPointOrState()
    {
        Type[] runtimeTypes = typeof(GameManager).Assembly.GetTypes();
        var occurrences = runtimeTypes.Select(type => type.FullName)
            .Concat(runtimeTypes.SelectMany(type => Members(type)
                .Select(member => type.FullName + "." + member.Name)))
            .Where(name => name.IndexOf("Prestige", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Assert.That(occurrences, Is.Empty,
            "The superseded reset concept must not remain callable or serializable in runtime code.");
    }

    [TestCase(1, 2)]
    [TestCase(9, 10)]
    [TestCase(10, 11)]
    [TestCase(100, 101)]
    public void BossClearAlwaysAdvancesToTheNextCombatLevel(int clearedLevel, int expected)
    {
        MethodInfo next = typeof(GameManager).GetMethod(
            "NextCombatLevelAfterBoss", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(next, Is.Not.Null);
        Assert.That(next.Invoke(null, new object[] { clearedLevel }), Is.EqualTo(expected));
    }

    [Test]
    public void RebirthRemainsTheLevelFiftyMetaReset()
    {
        host = new GameObject("Rebirth authority fixture");
        var progression = host.AddComponent<PlayerProgression>();
        host.AddComponent<RelicInventory>();
        var rebirth = host.AddComponent<RebirthManager>();
        FieldInfo level = typeof(PlayerProgression).GetField("level", BindingFlags.Instance | BindingFlags.NonPublic);

        level.SetValue(progression, RebirthManager.RequiredLevel - 1);
        Assert.That(rebirth.Eligible, Is.False);
        Assert.That(rebirth.RequestRebirth(), Is.False);

        level.SetValue(progression, RebirthManager.RequiredLevel);
        Assert.That(rebirth.Eligible, Is.True);
        Assert.That(rebirth.RequestRebirth(), Is.True);
        Assert.That(rebirth.ConfirmationPending, Is.True);
    }

    [Test]
    public void NewGameRestartLoadAndRebirthRemainSeparateEntryPoints()
    {
        AssertMethod(typeof(GamePersistence), "RequestNewGame", BindingFlags.Static | BindingFlags.Public);
        AssertMethod(typeof(GamePersistence), "RequestLoad", BindingFlags.Static | BindingFlags.Public);
        AssertMethod(typeof(GameManager), "RestartCurrentLevelAfterDeath", BindingFlags.Instance | BindingFlags.Public);
        AssertMethod(typeof(RebirthManager), "RequestRebirth", BindingFlags.Instance | BindingFlags.Public);
        AssertMethod(typeof(RebirthManager), "ConfirmRebirth", BindingFlags.Instance | BindingFlags.Public);
    }

    static MemberInfo[] Members(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        return type.GetMembers(flags);
    }

    static void AssertMethod(Type type, string name, BindingFlags flags)
    {
        Assert.That(type.GetMethod(name, flags), Is.Not.Null, type.Name + "." + name);
    }
}
