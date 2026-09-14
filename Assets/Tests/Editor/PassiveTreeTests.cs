using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PassiveTreeTests
{
    [Test]
    public void CorruptionTheme_DoesNotReplacePassiveNodeArtwork()
    {
        var node = new GameObject("passive-art-regression", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(PassiveNodeView));
        try
        {
            var method = typeof(CorruptionUITheme).GetMethod("ShouldSkin", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { node.GetComponent<UnityEngine.UI.Button>() }), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(node); }
    }

    [Test]
    public void Definition_PreservesTenSpokesAndPopulatesAllTenBridgeGaps()
    {
        string[] expected = { "Defense", "Life", "Mana", "Magic", "Lightning", "Fire", "Poison", "Projectile", "Physical", "Cold",
            "IncreasedProjectileAmount", "AttackSpeed", "BleedChance", "PoisonChance", "ChillChance", "IgniteChance", "ShockChance",
            "ChanceToHitTwice", "LifeRegeneration", "ManaRegeneration", "EmptyTravel" };
        Assert.That(Enum.GetNames(typeof(PassiveBranch)), Is.EqualTo(expected));
        Assert.That(PassiveTreeDefinition.OriginalBranchCount, Is.EqualTo(10));
        Assert.That(PassiveTreeDefinition.BranchCount, Is.EqualTo(21));
        Assert.That(PassiveTreeDefinition.BranchAngleDegrees, Is.EqualTo(36f));
        Assert.That(PassiveTreeDefinition.Nodes.Count, Is.EqualTo(290));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.Poison, 0), Is.EqualTo(0));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.Mana, 0), Is.EqualTo(36));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.Mana, 11), Is.EqualTo(47));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.Physical, 11), Is.EqualTo(119));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.IncreasedProjectileAmount, 0), Is.EqualTo(120));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.ShockChance, 10), Is.EqualTo(196));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.ChanceToHitTwice, 0), Is.EqualTo(197));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.ChanceToHitTwice, 10), Is.EqualTo(207));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.LifeRegeneration, 0), Is.EqualTo(208));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.LifeRegeneration, 10), Is.EqualTo(218));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.ManaRegeneration, 0), Is.EqualTo(219));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.ManaRegeneration, 10), Is.EqualTo(229));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel, 0), Is.EqualTo(230));
        Assert.That(PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel, 39), Is.EqualTo(269));

        foreach (PassiveBranch branch in Enum.GetValues(typeof(PassiveBranch)).Cast<PassiveBranch>().Take(10))
        {
            PassiveNodeDefinition[] nodes = PassiveTreeDefinition.Nodes.Where(n => n.Branch == branch && !PassiveTreeDefinition.IsKeystone(n.Id)).ToArray();
            Assert.That(nodes.Length, Is.EqualTo(12), branch.ToString());
            Assert.That(nodes.Select(n => n.Size), Is.EqualTo(new[]
            {
                PassiveNodeSize.Small, PassiveNodeSize.Small, PassiveNodeSize.Small, PassiveNodeSize.Small, PassiveNodeSize.Small,
                PassiveNodeSize.Large,
                PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium,
                PassiveNodeSize.Large
            }), branch.ToString());
            Assert.That(nodes.Select(n => n.Magnitude), Is.EqualTo(new[] { 5f, 5f, 5f, 5f, 5f, 25f, 10f, 10f, 10f, 10f, 10f, 25f }));
            Assert.That(nodes[0].PrerequisiteId, Is.EqualTo(-1));
            for (int i = 1; i < nodes.Length; i++) Assert.That(nodes[i].PrerequisiteId, Is.EqualTo(nodes[i - 1].Id));
        }

        foreach (PassiveBranch branch in Enum.GetValues(typeof(PassiveBranch)).Cast<PassiveBranch>().Skip(10).Take(10))
        {
            PassiveNodeDefinition[] nodes = PassiveTreeDefinition.Nodes.Where(n => n.Branch == branch).ToArray();
            Assert.That(nodes.Length, Is.EqualTo(11), branch.ToString());
            Assert.That(nodes.Select(n => n.Size), Is.EqualTo(new[]
            {
                PassiveNodeSize.Small, PassiveNodeSize.Small, PassiveNodeSize.Small, PassiveNodeSize.Small,
                PassiveNodeSize.Large,
                PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium, PassiveNodeSize.Medium,
                PassiveNodeSize.Large
            }), branch.ToString());
            float[] expectedMagnitudes = branch switch
            {
                PassiveBranch.IncreasedProjectileAmount => new[] { .25f, .25f, .25f, .25f, 1f, .5f, .5f, .5f, .5f, .5f, 1f },
                PassiveBranch.ChanceToHitTwice => new[] { 2f, 2f, 2f, 2f, 10f, 5f, 5f, 5f, 5f, 5f, 10f },
                PassiveBranch.LifeRegeneration or PassiveBranch.ManaRegeneration => new[] { 1f, 1f, 1f, 1f, 10f, 3f, 3f, 3f, 3f, 3f, 10f },
                _ => new[] { 5f, 5f, 5f, 5f, 25f, 10f, 10f, 10f, 10f, 10f, 25f }
            };
            Assert.That(nodes.Select(n => n.Magnitude), Is.EqualTo(expectedMagnitudes), branch.ToString());
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(nodes[4].Id).Count, Is.EqualTo(3), branch + " merge adjacency");
        }

        AssertBridge(PassiveBranch.LifeRegeneration, PassiveBranch.Defense, PassiveBranch.Life);
        AssertBridge(PassiveBranch.ChanceToHitTwice, PassiveBranch.Life, PassiveBranch.Mana);
        AssertBridge(PassiveBranch.ManaRegeneration, PassiveBranch.Mana, PassiveBranch.Magic);
        AssertBridge(PassiveBranch.ShockChance, PassiveBranch.Magic, PassiveBranch.Lightning);
        AssertBridge(PassiveBranch.IgniteChance, PassiveBranch.Lightning, PassiveBranch.Fire);
        AssertBridge(PassiveBranch.PoisonChance, PassiveBranch.Fire, PassiveBranch.Poison);
        AssertBridge(PassiveBranch.IncreasedProjectileAmount, PassiveBranch.Poison, PassiveBranch.Projectile);
        AssertBridge(PassiveBranch.AttackSpeed, PassiveBranch.Projectile, PassiveBranch.Physical);
        AssertBridge(PassiveBranch.BleedChance, PassiveBranch.Physical, PassiveBranch.Cold);
        AssertBridge(PassiveBranch.ChillChance, PassiveBranch.Cold, PassiveBranch.Defense);
        for (int spoke = 0; spoke < PassiveTreeDefinition.OriginalBranchCount; spoke++)
        {
            PassiveBranch a = (PassiveBranch)spoke;
            PassiveBranch b = (PassiveBranch)((spoke + 1) % PassiveTreeDefinition.OriginalBranchCount);
            int matchingBridges = Enum.GetValues(typeof(PassiveBranch)).Cast<PassiveBranch>()
                .Where(PassiveTreeDefinition.IsBridgeBranch).Count(branch => BridgeConnects(branch, a, b));
            Assert.That(matchingBridges, Is.EqualTo(1), $"{a}-{b} outer gap");
        }
        PassiveNodeDefinition[] ring = PassiveTreeDefinition.Nodes.Where(n => n.Branch == PassiveBranch.EmptyTravel).ToArray();
        Assert.That(ring.Length, Is.EqualTo(40));
        Assert.That(ring.All(n => n.Size == PassiveNodeSize.Small && n.Magnitude == 0f), Is.True);
        Assert.That(ring.All(n => PassiveTreeDefinition.AdjacentNodeIds(n.Id).Count == 2), Is.True);
        Assert.That(PassiveTreeDefinition.Edges.Count, Is.EqualTo(310));
        string[] names = { "Iron Bastion", "Living Fortress", "Mana Shield", "Arcane Overload", "Living Current",
            "Infernal Conversion", "Venomous Transmutation", "Ballistic Barrage", "Brute Force", "Absolute Zero" };
        for (int i = 0; i < PassiveTreeDefinition.OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            int id = PassiveTreeDefinition.KeystoneNodeId(branch);
            Assert.That(id, Is.EqualTo(270 + i));
            Assert.That(PassiveTreeDefinition.Node(id).PrerequisiteId, Is.EqualTo(PassiveTreeDefinition.TerminalNodeId(branch)));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(id), Is.EqualTo(new[] { PassiveTreeDefinition.TerminalNodeId(branch) }));
            Assert.That(PassiveTreeDefinition.KeystoneName(PassiveTreeDefinition.Node(id).Keystone), Is.EqualTo(names[i]));
            Assert.That(PassiveTreeDefinition.KeystoneEffect(PassiveTreeDefinition.Node(id).Keystone), Is.Not.Empty);
        }
        string[] outerNames = { "Open Wounds", "Wildfire", "Deep Freeze", "Overcharged", "Toxic Saturation",
            "Undying Flesh", "Endless Current", "Frenzy", "Bullet Hell", "Echoing Strikes" };
        for (int i = 0; i < PassiveTreeDefinition.OuterKeystoneNodeCount; i++)
        {
            var branch = PassiveTreeDefinition.OuterKeystoneBranch(i);
            int id = PassiveTreeDefinition.OuterKeystoneNodeId(branch);
            Assert.That(id, Is.EqualTo(280 + i));
            Assert.That(PassiveTreeDefinition.Node(id).PrerequisiteId, Is.EqualTo(PassiveTreeDefinition.TerminalNodeId(branch)));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(id), Is.EqualTo(new[] { PassiveTreeDefinition.TerminalNodeId(branch) }));
            Assert.That(PassiveTreeDefinition.KeystoneName(PassiveTreeDefinition.Node(id).Keystone), Is.EqualTo(outerNames[i]));
        }
    }

    [Test]
    public void Refund_OnlyAllowsNodesWhoseRemovalKeepsEveryAllocationRootConnected()
    {
        var game = new GameObject("passive-refund-test");
        try
        {
            var progression = game.AddComponent<PlayerProgression>();
            SetPoints(progression, 3);
            int first = PassiveTreeDefinition.NodeId(PassiveBranch.Defense, 0);
            Assert.That(progression.TrySpend(first), Is.True);
            Assert.That(progression.TrySpend(first + 1), Is.True);
            Assert.That(progression.CanRefund(first), Is.False);
            Assert.That(progression.TryRefund(first), Is.False);
            Assert.That(progression.CanRefund(first + 1), Is.True);
            Assert.That(progression.TryRefund(first + 1), Is.True);
            Assert.That(progression.AvailablePoints, Is.EqualTo(2));
            Assert.That(progression.TryRefund(first), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(game); }
    }

    [Test]
    public void InnerKeystone_RequiresSpokeTerminalAndRefundPreservesConnectivity()
    {
        var game = new GameObject("inner-keystone-allocation-test");
        try
        {
            var progression = game.AddComponent<PlayerProgression>();
            SetPoints(progression, 13);
            int keystone = PassiveTreeDefinition.KeystoneNodeId(PassiveBranch.Defense);
            Assert.That(progression.TrySpend(keystone), Is.False);
            AllocateSpoke(progression, PassiveBranch.Defense);
            Assert.That(progression.TrySpend(keystone), Is.True);
            Assert.That(progression.CanRefund(PassiveTreeDefinition.TerminalNodeId(PassiveBranch.Defense)), Is.False);
            Assert.That(progression.TryRefund(keystone), Is.True);
            Assert.That(progression.AvailablePoints, Is.EqualTo(1));
        }
        finally { UnityEngine.Object.DestroyImmediate(game); }
    }

    [Test]
    public void OuterRing_EverySegmentIsBidirectionalAndTouchesBothNeighboringTerminals()
    {
        for (int gap = 0; gap < PassiveTreeDefinition.OriginalBranchCount; gap++)
        {
            int first = PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel, gap * PassiveTreeDefinition.RingNodesPerGap);
            int last = first + PassiveTreeDefinition.RingNodesPerGap - 1;
            int fromTerminal = PassiveTreeDefinition.TerminalNodeId(PassiveTreeDefinition.BridgeAtClockwiseGap(gap));
            int toTerminal = PassiveTreeDefinition.TerminalNodeId(PassiveTreeDefinition.BridgeAtClockwiseGap((gap + 1) % 10));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(fromTerminal), Does.Contain(first));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(first), Does.Contain(fromTerminal));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(toTerminal), Does.Contain(last));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(last), Does.Contain(toTerminal));
            for (int p = 1; p < PassiveTreeDefinition.RingNodesPerGap; p++)
            {
                Assert.That(PassiveTreeDefinition.AdjacentNodeIds(first + p - 1), Does.Contain(first + p));
                Assert.That(PassiveTreeDefinition.AdjacentNodeIds(first + p), Does.Contain(first + p - 1));
            }
        }
    }

    [Test]
    public void OuterJunctions_KeystoneIsRadialLeafAndRingLeavesBothSidesWithoutKeystones()
    {
        var keystoneIds = new HashSet<int>();
        for (int i = 0; i < PassiveTreeDefinition.OuterKeystoneNodeCount; i++)
            keystoneIds.Add(PassiveTreeDefinition.OuterKeystoneNodeId(PassiveTreeDefinition.OuterKeystoneBranch(i)));

        for (int gap = 0; gap < PassiveTreeDefinition.OriginalBranchCount; gap++)
        {
            PassiveBranch branch = PassiveTreeDefinition.BridgeAtClockwiseGap(gap);
            int terminal = PassiveTreeDefinition.TerminalNodeId(branch);
            int previousTerminal = PassiveTreeDefinition.TerminalNodeId(
                PassiveTreeDefinition.BridgeAtClockwiseGap((gap + 9) % 10));
            int nextTerminal = PassiveTreeDefinition.TerminalNodeId(
                PassiveTreeDefinition.BridgeAtClockwiseGap((gap + 1) % 10));
            int clockwiseRing = PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel,
                gap * PassiveTreeDefinition.RingNodesPerGap);
            int counterClockwiseRing = PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel,
                ((gap + 9) % 10) * PassiveTreeDefinition.RingNodesPerGap + PassiveTreeDefinition.RingNodesPerGap - 1);
            int keystone = PassiveTreeDefinition.OuterKeystoneNodeId(branch);

            CollectionAssert.AreEquivalent(new[] { terminal }, PassiveTreeDefinition.AdjacentNodeIds(keystone));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(terminal), Does.Contain(keystone));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(terminal), Does.Contain(clockwiseRing));
            Assert.That(PassiveTreeDefinition.AdjacentNodeIds(terminal), Does.Contain(counterClockwiseRing));
            Assert.That(ReachableAlongRingGap(terminal, nextTerminal, gap, keystoneIds), Is.True, branch + " clockwise");
            Assert.That(ReachableAlongRingGap(terminal, previousTerminal, (gap + 9) % 10, keystoneIds), Is.True, branch + " counter-clockwise");

            Vector2 terminalPosition = SkillTreeUI.CalculateNodePosition(PassiveTreeDefinition.Node(terminal));
            Vector2 keystonePosition = SkillTreeUI.CalculateNodePosition(PassiveTreeDefinition.Node(keystone));
            Vector2 clockwisePosition = SkillTreeUI.CalculateNodePosition(PassiveTreeDefinition.Node(clockwiseRing));
            Vector2 counterPosition = SkillTreeUI.CalculateNodePosition(PassiveTreeDefinition.Node(counterClockwiseRing));
            Assert.That(keystonePosition.magnitude, Is.GreaterThan(terminalPosition.magnitude));
            Assert.That(Mathf.Abs(Vector2.SignedAngle(terminalPosition, keystonePosition)), Is.LessThan(.01f));
            Assert.That(Mathf.Abs(Vector2.SignedAngle(terminalPosition, clockwisePosition)), Is.GreaterThan(.01f));
            Assert.That(Mathf.Abs(Vector2.SignedAngle(terminalPosition, counterPosition)), Is.GreaterThan(.01f));
        }
    }

    static bool ReachableAlongRingGap(int start, int target, int gap, HashSet<int> excluded)
    {
        var allowed = new HashSet<int> { start, target };
        int first = PassiveTreeDefinition.NodeId(PassiveBranch.EmptyTravel, gap * PassiveTreeDefinition.RingNodesPerGap);
        for (int i = 0; i < PassiveTreeDefinition.RingNodesPerGap; i++) allowed.Add(first + i);
        var visited = new HashSet<int> { start };
        var queue = new Queue<int>(); queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == target) return true;
            foreach (int adjacent in PassiveTreeDefinition.AdjacentNodeIds(current))
                if (allowed.Contains(adjacent) && !excluded.Contains(adjacent) && visited.Add(adjacent)) queue.Enqueue(adjacent);
        }
        return false;
    }

    [Test]
    public void Zoom_AddsExactlyTwoStepsBelowFormerMinimum()
    {
        float lowTwo = SkillTreeUI.CalculateNextZoom(.19f, -1f);
        float lowOne = SkillTreeUI.CalculateNextZoom(lowTwo, -1f);
        Assert.That(lowTwo, Is.EqualTo(.16333333f).Within(.0001f));
        Assert.That(lowOne, Is.EqualTo(.13666667f).Within(.0001f));
        Assert.That(SkillTreeUI.CalculateNextZoom(lowOne, -1f), Is.EqualTo(.11f).Within(.0001f));
        Assert.That(SkillTreeUI.CalculateNextZoom(.11f, 1f), Is.EqualTo(lowOne).Within(.0001f));
    }

    [TestCase(PassiveKeystone.AbsoluteZero, Element.Cold)]
    [TestCase(PassiveKeystone.InfernalConversion, Element.Fire)]
    [TestCase(PassiveKeystone.LivingCurrent, Element.Light)]
    public void ElementKeystones_ConvertHalfSuppressRemainderAndApplyMore(PassiveKeystone keystone, Element target)
    {
        var stateObject = new GameObject("keystone-state");
        var progressionObject = new GameObject("keystone-progression");
        try
        {
            stateObject.AddComponent<StatsComponent>();
            var state = stateObject.AddComponent<PassiveKeystoneState>();
            ActivateKeystone(progressionObject.AddComponent<PlayerProgression>(), state, keystone);
            var context = new DamageContext(2);
            context.AddDamage(target, 100f);
            context.AddDamage(target == Element.Phys ? Element.Fire : Element.Phys, 100f);
            DamageContext result = state.TransformOutgoing(context);
            Assert.That(result.Hits.Count, Is.EqualTo(1));
            Assert.That(result.Hits[0].Element, Is.EqualTo(target));
            Assert.That(result.Hits[0].Amount, Is.EqualTo(187.5f).Within(.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            UnityEngine.Object.DestroyImmediate(stateObject);
        }
    }

    [Test]
    public void KeystoneMultipliers_ManaShieldAndPoisonTransmutationUseRuntimeSemantics()
    {
        var player = new GameObject("keystone-player");
        var progressionObject = new GameObject("keystone-progression");
        try
        {
            var stats = player.AddComponent<StatsComponent>();
            stats.SetBaseStat(StatTypes.Mana, 100f);
            var mana = player.AddComponent<ManaComponent>();
            var state = player.AddComponent<PassiveKeystoneState>();
            var progression = progressionObject.AddComponent<PlayerProgression>();

            ActivateKeystone(progression, state, PassiveKeystone.ManaShield);
            mana.RestoreFull();
            Assert.That(state.MaximumLifeMultiplier, Is.EqualTo(.75f));
            Assert.That(state.RedirectDamageToMana(150f), Is.EqualTo(75f));
            Assert.That(mana.CurrentMana, Is.EqualTo(25f));
            Assert.That(state.RedirectDamageToMana(50f), Is.EqualTo(25f));
            Assert.That(mana.CurrentMana, Is.Zero);

            ActivateKeystone(progression, state, PassiveKeystone.VenomousTransmutation);
            var hit = new DamageContext(2);
            hit.AddDamage(Element.Phys, 40f);
            hit.AddDamage(Element.Fire, 60f);
            DamageContext poison = PassiveKeystoneState.AsPoisonBasis(hit);
            Assert.That(state.TransmutesHitsToPoison, Is.True);
            Assert.That(poison.Hits.Count, Is.EqualTo(1));
            Assert.That(poison.Hits[0].Element, Is.EqualTo(Element.Poison));
            Assert.That(poison.Hits[0].Amount, Is.EqualTo(100f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void BruteForceAndArcaneOverload_ApplyMultiplicativeDamageSpeedAndManaCost()
    {
        var player = new GameObject("keystone-player");
        var progressionObject = new GameObject("keystone-progression");
        try
        {
            player.AddComponent<StatsComponent>();
            var state = player.AddComponent<PassiveKeystoneState>();
            var progression = progressionObject.AddComponent<PlayerProgression>();
            ActivateKeystone(progression, state, PassiveKeystone.BruteForce);
            var hit = new DamageContext(2);
            hit.AddDamage(Element.Phys, 30f);
            hit.AddDamage(Element.Fire, 70f);
            DamageContext result = state.TransformOutgoing(hit);
            Assert.That(result.Hits.Count, Is.EqualTo(1));
            Assert.That(result.Hits[0].Amount, Is.EqualTo(60f));
            Assert.That(state.AttackSpeedMultiplier, Is.EqualTo(.75f));

            ActivateKeystone(progression, state, PassiveKeystone.ArcaneOverload);
            var magic = new DamageContext(1) { Scopes = DamageScope.Magic };
            magic.AddDamage(Element.Fire, 100f);
            Assert.That(state.TransformOutgoing(magic).Hits[0].Amount, Is.EqualTo(140f).Within(.001f));
            Assert.That(state.ManaCostMultiplier, Is.EqualTo(2f));
            var skills = player.AddComponent<PlayerSkillController>();
            Assert.That(skills.ManaCost(new PlayerSkillDefinition { manaCost = 20f }), Is.EqualTo(40f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void LifeDefenseAndProjectileKeystones_UseIndependentMoreLessFactors()
    {
        var player = new GameObject("keystone-player");
        var progressionObject = new GameObject("keystone-progression");
        try
        {
            player.AddComponent<StatsComponent>();
            var state = player.AddComponent<PassiveKeystoneState>();
            var progression = progressionObject.AddComponent<PlayerProgression>();
            ActivateKeystone(progression, state, PassiveKeystone.LivingFortress);
            Assert.That(state.MaximumLifeMultiplier, Is.EqualTo(1.5f));
            Assert.That(state.DefenseMultiplier, Is.EqualTo(.5f));
            ActivateKeystone(progression, state, PassiveKeystone.IronBastion);
            Assert.That(state.MaximumLifeMultiplier, Is.EqualTo(.7f));
            Assert.That(state.DefenseMultiplier, Is.EqualTo(1.75f));
            ActivateKeystone(progression, state, PassiveKeystone.BallisticBarrage);
            var projectile = new DamageContext(1) { Scopes = DamageScope.Projectile };
            projectile.AddDamage(Element.Phys, 100f);
            var ordinary = new DamageContext(1);
            ordinary.AddDamage(Element.Phys, 100f);
            Assert.That(state.TransformOutgoing(projectile).Hits[0].Amount, Is.EqualTo(150f));
            Assert.That(state.TransformOutgoing(ordinary).Hits[0].Amount, Is.EqualTo(50f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void OuterKeystones_ExposeExactChanceDamageResourceAndCapFactors()
    {
        var player = new GameObject("outer-keystone-player");
        var progressionObject = new GameObject("outer-keystone-progression");
        try
        {
            player.AddComponent<StatsComponent>();
            var state = player.AddComponent<PassiveKeystoneState>();
            var progression = progressionObject.AddComponent<PlayerProgression>();
            var cases = new[]
            {
                (PassiveKeystone.OpenWounds, StatTypes.BleedChance),
                (PassiveKeystone.Wildfire, StatTypes.IgniteChance),
                (PassiveKeystone.DeepFreeze, StatTypes.ChillChance),
                (PassiveKeystone.Overcharged, StatTypes.ShockChance),
                (PassiveKeystone.ToxicSaturation, StatTypes.PoisonChance),
                (PassiveKeystone.EchoingStrikes, StatTypes.ChanceToHitTwice)
            };
            foreach (var pair in cases)
            {
                ActivateKeystone(progression, state, pair.Item1);
                Assert.That(state.ChanceMultiplier(pair.Item2), Is.EqualTo(2f), pair.Item1.ToString());
            }
            ActivateKeystone(progression, state, PassiveKeystone.OpenWounds);
            Assert.That(state.AilmentDamageMultiplier(StatusEffects.AilmentKind.Bleed), Is.EqualTo(.65f));
            ActivateKeystone(progression, state, PassiveKeystone.Wildfire);
            Assert.That(state.AilmentDamageMultiplier(StatusEffects.AilmentKind.Ignite), Is.EqualTo(.7f));
            ActivateKeystone(progression, state, PassiveKeystone.ToxicSaturation);
            Assert.That(state.AilmentDamageMultiplier(StatusEffects.AilmentKind.Poison), Is.EqualTo(.6f));
            ActivateKeystone(progression, state, PassiveKeystone.UndyingFlesh);
            Assert.That(state.LifeRegenerationMultiplier, Is.EqualTo(2f));
            Assert.That(state.MaximumLifeMultiplier, Is.EqualTo(.75f));
            ActivateKeystone(progression, state, PassiveKeystone.EndlessCurrent);
            Assert.That(state.ManaRegenerationMultiplier, Is.EqualTo(2f));
            Assert.That(state.MaximumManaMultiplier, Is.EqualTo(.75f));
            ActivateKeystone(progression, state, PassiveKeystone.Frenzy);
            Assert.That(state.AttackSpeedMultiplier, Is.EqualTo(1.5f));
            var hit = new DamageContext(1); hit.AddDamage(Element.Phys, 100f);
            Assert.That(state.TransformOutgoing(hit).Hits[0].Amount, Is.EqualTo(70f));
            ActivateKeystone(progression, state, PassiveKeystone.BulletHell);
            Assert.That(state.ProjectileAmountBonus, Is.EqualTo(2));
            var projectile = new DamageContext(1) { Scopes = DamageScope.Projectile }; projectile.AddDamage(Element.Phys, 100f);
            Assert.That(state.TransformOutgoing(projectile).Hits[0].Amount, Is.EqualTo(65f));
            ActivateKeystone(progression, state, PassiveKeystone.EchoingStrikes);
            Assert.That(state.TransformOutgoing(hit).Hits[0].Amount, Is.EqualTo(75f));
            ActivateKeystone(progression, state, PassiveKeystone.DeepFreeze);
            Assert.That(state.ChillEffectMultiplier, Is.EqualTo(.75f));
            Assert.That(state.DeepFreezeMaximumEffectIncrease, Is.Zero);
            ActivateKeystone(progression, state, PassiveKeystone.Overcharged);
            Assert.That(state.ShockStackRequirementMultiplier, Is.EqualTo(.5f));
            Assert.That(state.ShockTriggeredHitMultiplier, Is.EqualTo(.65f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(progressionObject);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void BridgeAllocation_AcceptsEitherApproachAndAccumulatesExactStats()
    {
        var player = new GameObject("bridge-passive-test-player");
        var game = new GameObject("bridge-passive-test-progression");
        try
        {
            var stats = player.AddComponent<StatsComponent>();
            player.AddComponent<PlayerController>();
            var progression = game.AddComponent<PlayerProgression>();
            SetPoints(progression, 150);

            AllocateSpoke(progression, PassiveBranch.Poison);
            int amountFirst = PassiveTreeDefinition.NodeId(PassiveBranch.IncreasedProjectileAmount, 0);
            Assert.That(progression.TrySpend(amountFirst), Is.True);
            Assert.That(progression.GetBonus(PassiveBranch.IncreasedProjectileAmount), Is.EqualTo(.25f));
            Assert.That(stats.GetRawStat(StatTypes.ProjectileAmount), Is.EqualTo(.25f));
            Assert.That(progression.TrySpend(amountFirst + 1), Is.True);
            int amountMerge = PassiveTreeDefinition.NodeId(PassiveBranch.IncreasedProjectileAmount, 4);
            Assert.That(progression.TrySpend(amountMerge), Is.True, "Left approach alone must unlock the merge.");
            Assert.That(stats.GetRawStat(StatTypes.ProjectileAmount), Is.EqualTo(1.5f));

            AllocateSpoke(progression, PassiveBranch.Physical);
            int speedRight = PassiveTreeDefinition.NodeId(PassiveBranch.AttackSpeed, 2);
            Assert.That(progression.TrySpend(speedRight), Is.True);
            Assert.That(progression.TrySpend(speedRight + 1), Is.True);
            int speedMerge = PassiveTreeDefinition.NodeId(PassiveBranch.AttackSpeed, 4);
            Assert.That(progression.TrySpend(speedMerge), Is.True, "Right approach alone must unlock the merge.");
            Assert.That(stats.GetRawStat(StatTypes.AttackSpeed), Is.EqualTo(35f));

            AllocateSpoke(progression, PassiveBranch.Life);
            AllocateSpoke(progression, PassiveBranch.Mana);
            AllocateSpoke(progression, PassiveBranch.Magic);
            AllocateSpoke(progression, PassiveBranch.Cold);
            AllocateSpoke(progression, PassiveBranch.Fire);
            AllocateSpoke(progression, PassiveBranch.Lightning);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.BleedChance, 0)), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.PoisonChance, 0)), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.ChillChance, 0)), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.IgniteChance, 0)), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.ShockChance, 0)), Is.True);
            Assert.That(stats.GetRawStat(StatTypes.BleedChance), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.PoisonChance), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ChillChance), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.IgniteChance), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ShockChance), Is.EqualTo(5f));

            int hitTwiceRight = PassiveTreeDefinition.NodeId(PassiveBranch.ChanceToHitTwice, 2);
            Assert.That(progression.TrySpend(hitTwiceRight), Is.True);
            Assert.That(progression.TrySpend(hitTwiceRight + 1), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.ChanceToHitTwice, 4)), Is.True,
                "Mana-side approach alone must unlock the hit-twice merge.");
            Assert.That(stats.GetRawStat(StatTypes.ChanceToHitTwice), Is.EqualTo(14f));

            int lifeRegenRight = PassiveTreeDefinition.NodeId(PassiveBranch.LifeRegeneration, 2);
            Assert.That(progression.TrySpend(lifeRegenRight), Is.True);
            Assert.That(progression.TrySpend(lifeRegenRight + 1), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.LifeRegeneration, 4)), Is.True);
            Assert.That(stats.GetRawStat(StatTypes.LifeRegeneration), Is.EqualTo(12f));

            int manaRegenLeft = PassiveTreeDefinition.NodeId(PassiveBranch.ManaRegeneration, 0);
            Assert.That(progression.TrySpend(manaRegenLeft), Is.True);
            Assert.That(progression.TrySpend(manaRegenLeft + 1), Is.True);
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.ManaRegeneration, 4)), Is.True);
            Assert.That(stats.GetRawStat(StatTypes.ManaRegeneration), Is.EqualTo(12f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(game);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void Migration_PreservesAllOriginalIdsAndLeavesBridgeIdsEmpty()
    {
        var game = new GameObject("passive-migration-test");
        try
        {
            var progression = game.AddComponent<PlayerProgression>();
            var ranksField = typeof(PlayerProgression).GetField("ranks", BindingFlags.Instance | BindingFlags.NonPublic);
            var legacy = new int[PassiveTreeDefinition.OriginalNodeCount];
            legacy[0] = 1;
            legacy[57] = 3;
            legacy[119] = -1;
            ranksField.SetValue(progression, legacy);
            typeof(PlayerProgression).GetMethod("NormalizeAllocations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progression, null);
            var migrated = (int[])ranksField.GetValue(progression);
            Assert.That(migrated.Length, Is.EqualTo(290));
            Assert.That(migrated[0], Is.EqualTo(1));
            Assert.That(migrated[57], Is.EqualTo(1));
            Assert.That(migrated[119], Is.EqualTo(1));
            Assert.That(migrated.Skip(120), Is.All.Zero);
        }
        finally { UnityEngine.Object.DestroyImmediate(game); }
    }

    [Test]
    public void Migration_PreservesEveryPrevious197NodeIdAndLeavesNewBranchEmpty()
    {
        var game = new GameObject("passive-197-migration-test");
        try
        {
            var progression = game.AddComponent<PlayerProgression>();
            var ranksField = typeof(PlayerProgression).GetField("ranks", BindingFlags.Instance | BindingFlags.NonPublic);
            var legacy = new int[197];
            legacy[0] = 1;
            legacy[120] = 1;
            legacy[196] = 1;
            ranksField.SetValue(progression, legacy);
            typeof(PlayerProgression).GetMethod("NormalizeAllocations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progression, null);
            var migrated = (int[])ranksField.GetValue(progression);
            Assert.That(migrated.Length, Is.EqualTo(290));
            Assert.That(migrated[0], Is.EqualTo(1));
            Assert.That(migrated[120], Is.EqualTo(1));
            Assert.That(migrated[196], Is.EqualTo(1));
            Assert.That(migrated.Skip(197), Is.All.Zero);
        }
        finally { UnityEngine.Object.DestroyImmediate(game); }
    }

    [Test]
    public void Migration_PreservesEveryPrevious208NodeIdAndLeavesRegenerationBranchesEmpty()
    {
        var game = new GameObject("passive-208-migration-test");
        try
        {
            var progression = game.AddComponent<PlayerProgression>();
            var ranksField = typeof(PlayerProgression).GetField("ranks", BindingFlags.Instance | BindingFlags.NonPublic);
            var legacy = new int[208];
            legacy[0] = legacy[196] = legacy[197] = legacy[207] = 1;
            ranksField.SetValue(progression, legacy);
            typeof(PlayerProgression).GetMethod("NormalizeAllocations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(progression, null);
            var migrated = (int[])ranksField.GetValue(progression);
            Assert.That(migrated.Length, Is.EqualTo(290));
            Assert.That(migrated[0], Is.EqualTo(1));
            Assert.That(migrated[196], Is.EqualTo(1));
            Assert.That(migrated[197], Is.EqualTo(1));
            Assert.That(migrated[207], Is.EqualTo(1));
            Assert.That(migrated.Skip(208), Is.All.Zero);
        }
        finally { UnityEngine.Object.DestroyImmediate(game); }
    }

    [Test]
    public void PassiveTreePausePreference_DefaultsOffAndPersistsBothValues()
    {
        bool hadPreference = PlayerPrefs.HasKey(GameplayOptions.PausePassiveTreeKey);
        int previous = PlayerPrefs.GetInt(GameplayOptions.PausePassiveTreeKey, 0);
        try
        {
            PlayerPrefs.DeleteKey(GameplayOptions.PausePassiveTreeKey);
            Assert.That(GameplayOptions.PausePassiveTree, Is.False);
            GameplayOptions.PausePassiveTree = true;
            Assert.That(GameplayOptions.PauseWhenOpen(GameplayWindow.PassiveTree), Is.True);
            GameplayOptions.PausePassiveTree = false;
            Assert.That(GameplayOptions.PauseWhenOpen(GameplayWindow.PassiveTree), Is.False);
        }
        finally
        {
            if (hadPreference) PlayerPrefs.SetInt(GameplayOptions.PausePassiveTreeKey, previous);
            else PlayerPrefs.DeleteKey(GameplayOptions.PausePassiveTreeKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void Allocation_RequiresPreviousNodeAndAppliesCorrectValues()
    {
        var player = new GameObject("passive-test-player");
        var game = new GameObject("passive-test-progression");
        try
        {
            var stats = player.AddComponent<StatsComponent>();
            player.AddComponent<PlayerController>();
            var progression = game.AddComponent<PlayerProgression>();
            SetPoints(progression, 20);

            int poison0 = PassiveTreeDefinition.NodeId(PassiveBranch.Poison, 0);
            Assert.That(progression.TrySpend(poison0 + 1), Is.False);
            Assert.That(progression.TrySpend(poison0), Is.True);
            Assert.That(progression.TrySpend(poison0), Is.False);
            Assert.That(progression.GetBonus(PassiveBranch.Poison), Is.EqualTo(5));
            Assert.That(stats.GetRawStat(StatTypes.PoisonDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ColdDmg), Is.EqualTo(0f), "Poison must not grant Cold damage");
            Assert.That(stats.GetRawStat(StatTypes.GenericDmg), Is.EqualTo(0f));

            AllocateFirst(progression, PassiveBranch.Life);
            AllocateFirst(progression, PassiveBranch.Defense);
            AllocateFirst(progression, PassiveBranch.Mana);
            AllocateFirst(progression, PassiveBranch.Magic);
            AllocateFirst(progression, PassiveBranch.Projectile);
            AllocateFirst(progression, PassiveBranch.Cold);
            Assert.That(stats.GetRawStat(StatTypes.ColdDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.PoisonDmg), Is.EqualTo(5f), "Cold must not alter Poison damage");
            AllocateFirst(progression, PassiveBranch.Fire);
            AllocateFirst(progression, PassiveBranch.Lightning);
            AllocateFirst(progression, PassiveBranch.Physical);

            Assert.That(stats.GetRawStat(StatTypes.LifePercent), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ArmourPercent), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ManaPercent), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.MagicDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ProjectileDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.ColdDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.PoisonDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.FireDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.LightDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.PhysDmg), Is.EqualTo(5f));
            Assert.That(stats.GetRawStat(StatTypes.MinionDmg), Is.EqualTo(0f), "Physical spoke must not grant Minion damage");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(game);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void ExplicitDamageScopes_StackOnlyWhenTagged()
    {
        var attackerObject = new GameObject("scope-attacker");
        var defenderObject = new GameObject("scope-defender");
        try
        {
            var attacker = attackerObject.AddComponent<StatsComponent>();
            var defender = defenderObject.AddComponent<StatsComponent>();
            attacker.SetBaseStat(StatTypes.MagicDmg, 5f);
            attacker.SetBaseStat(StatTypes.ProjectileDmg, 10f);
            attacker.SetBaseStat(StatTypes.MinionDmg, 25f);

            Assert.That(Damage(attacker, defender, DamageScope.None), Is.EqualTo(100f).Within(.001f));
            Assert.That(Damage(attacker, defender, DamageScope.Magic), Is.EqualTo(105f).Within(.001f));
            Assert.That(Damage(attacker, defender, DamageScope.Projectile), Is.EqualTo(110f).Within(.001f));
            Assert.That(Damage(attacker, defender, DamageScope.Magic | DamageScope.Projectile), Is.EqualTo(115f).Within(.001f));
            Assert.That(Damage(attacker, defender, DamageScope.Minion), Is.EqualTo(125f).Within(.001f));

            PlayerSkillDefinition fireball = PlayerSkillDefinition.CreateDefaults().Single(s => s.id == PlayerSkillId.Fireball);
            Assert.That(fireball.DamageScopes, Is.EqualTo(DamageScope.Projectile));
            Assert.That(fireball.magic, Is.False, "Elemental/projectile content must not be implicitly Magic-tagged");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(attackerObject);
            UnityEngine.Object.DestroyImmediate(defenderObject);
        }
    }

    static void AllocateFirst(PlayerProgression progression, PassiveBranch branch) =>
        Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(branch, 0)), Is.True, branch.ToString());

    static void AllocateSpoke(PlayerProgression progression, PassiveBranch branch)
    {
        for (int position = 0; position < PassiveTreeDefinition.OriginalNodesPerBranch; position++)
            Assert.That(progression.TrySpend(PassiveTreeDefinition.NodeId(branch, position)), Is.True, $"{branch} {position}");
    }

    static void AssertBridge(PassiveBranch bridge, PassiveBranch left, PassiveBranch right)
    {
        PassiveTreeDefinition.BridgeEndpoints(bridge, out PassiveBranch actualLeft, out PassiveBranch actualRight);
        Assert.That(actualLeft, Is.EqualTo(left));
        Assert.That(actualRight, Is.EqualTo(right));
        int first = PassiveTreeDefinition.NodeId(bridge, 0);
        Assert.That(PassiveTreeDefinition.AdjacentNodeIds(first), Does.Contain(PassiveTreeDefinition.TerminalNodeId(left)));
        Assert.That(PassiveTreeDefinition.AdjacentNodeIds(first + 2), Does.Contain(PassiveTreeDefinition.TerminalNodeId(right)));
    }

    static bool BridgeConnects(PassiveBranch bridge, PassiveBranch a, PassiveBranch b)
    {
        PassiveTreeDefinition.BridgeEndpoints(bridge, out PassiveBranch left, out PassiveBranch right);
        return left == a && right == b || left == b && right == a;
    }

    static void SetPoints(PlayerProgression progression, int points) =>
        typeof(PlayerProgression).GetField("availablePoints", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(progression, points);

    static void ActivateKeystone(PlayerProgression progression, PassiveKeystoneState state, PassiveKeystone keystone)
    {
        var ranksField = typeof(PlayerProgression).GetField("ranks", BindingFlags.Instance | BindingFlags.NonPublic);
        var ranks = new int[PassiveTreeDefinition.NodeCount];
        for (int i = 0; i < PassiveTreeDefinition.OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            if (PassiveTreeDefinition.KeystoneFor(branch) == keystone)
                ranks[PassiveTreeDefinition.KeystoneNodeId(branch)] = 1;
        }
        for (int i = 0; i < PassiveTreeDefinition.OuterKeystoneNodeCount; i++)
        {
            var branch = PassiveTreeDefinition.OuterKeystoneBranch(i);
            if (PassiveTreeDefinition.OuterKeystoneFor(branch) == keystone)
                ranks[PassiveTreeDefinition.OuterKeystoneNodeId(branch)] = 1;
        }
        ranksField.SetValue(progression, ranks);
        state.Apply(progression);
    }

    static float Damage(StatsComponent attacker, StatsComponent defender, DamageScope scopes)
    {
        var context = new DamageContext(1) { Scopes = scopes };
        context.AddDamage(Element.Phys, 100f);
        return CombatCalculator.CalculateFinalDamage(context, attacker, defender);
    }
}
