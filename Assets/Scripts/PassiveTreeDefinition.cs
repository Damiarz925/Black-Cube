// Canonical data-only layout and undirected adjacency graph for the radial passive tree.
// Existing node ids 0-229 remain stable; statless outer travel-ring ids are 230-269.
using System;
using System.Collections.Generic;

public enum PassiveBranch
{
    Defense, Life, Mana, Magic, Lightning, Fire, Poison, Projectile, Physical, Cold,
    IncreasedProjectileAmount, AttackSpeed, BleedChance, PoisonChance, ChillChance,
    IgniteChance, ShockChance, ChanceToHitTwice, LifeRegeneration, ManaRegeneration, EmptyTravel
}

public enum PassiveNodeSize { Small, Medium, Large }

public enum PassiveKeystone
{
    None, IronBastion, LivingFortress, ManaShield, ArcaneOverload, LivingCurrent,
    InfernalConversion, VenomousTransmutation, BallisticBarrage, BruteForce, AbsoluteZero,
    OpenWounds, Wildfire, DeepFreeze, Overcharged, ToxicSaturation, UndyingFlesh,
    EndlessCurrent, Frenzy, BulletHell, EchoingStrikes
}

public readonly struct PassiveNodeDefinition
{
    public readonly int Id;
    public readonly PassiveBranch Branch;
    public readonly int Position;
    public readonly PassiveNodeSize Size;
    public readonly float Magnitude;
    public readonly int PrerequisiteId;
    public readonly PassiveKeystone Keystone;

    public PassiveNodeDefinition(int id, PassiveBranch branch, int position,
        PassiveNodeSize size, float magnitude, int prerequisiteId, PassiveKeystone keystone = PassiveKeystone.None)
    {
        Id = id; Branch = branch; Position = position; Size = size;
        Magnitude = magnitude; PrerequisiteId = prerequisiteId;
        Keystone = keystone;
    }
}

public readonly struct PassiveTreeEdge
{
    // A == -1 represents the always-allocated player root.
    public readonly int A;
    public readonly int B;
    public PassiveTreeEdge(int a, int b) { A = a; B = b; }
}

public static class PassiveTreeDefinition
{
    public const int OriginalBranchCount = 10;
    public const int BridgeBranchCount = 10;
    public const int BranchCount = 21;
    public const int OriginalNodesPerBranch = 12;
    public const int NodesPerBranch = OriginalNodesPerBranch;
    public const int BridgeNodesPerBranch = 11;
    public const int RingNodesPerGap = 4;
    public const int RingNodeCount = OriginalBranchCount * RingNodesPerGap;
    public const int OriginalNodeCount = OriginalBranchCount * OriginalNodesPerBranch;
    public const int ExistingNodeCount = OriginalNodeCount + BridgeBranchCount * BridgeNodesPerBranch;
    public const int InnerKeystoneNodeCount = OriginalBranchCount;
    public const int OuterKeystoneNodeCount = BridgeBranchCount;
    public const int KeystoneNodeCount = InnerKeystoneNodeCount + OuterKeystoneNodeCount;
    public const int KeystoneStartId = ExistingNodeCount + RingNodeCount;
    public const int NodeCount = KeystoneStartId + KeystoneNodeCount;
    public const float BranchAngleDegrees = 36f;
    public const int PointCost = 1;

    static readonly PassiveNodeDefinition[] nodes = BuildNodes();
    static readonly PassiveTreeEdge[] edges = BuildEdges();
    static readonly int[][] adjacency = BuildAdjacency();
    public static IReadOnlyList<PassiveNodeDefinition> Nodes => nodes;
    public static IReadOnlyList<PassiveTreeEdge> Edges => edges;

    public static PassiveNodeDefinition Node(int id)
    {
        if (id < 0 || id >= nodes.Length) throw new ArgumentOutOfRangeException(nameof(id));
        return nodes[id];
    }

    public static bool IsOriginalBranch(PassiveBranch branch) => (int)branch < OriginalBranchCount;
    public static bool IsBridgeBranch(PassiveBranch branch) =>
        (int)branch >= OriginalBranchCount && (int)branch < OriginalBranchCount + BridgeBranchCount;
    public static bool IsRingBranch(PassiveBranch branch) => branch == PassiveBranch.EmptyTravel;
    public static int NodesInBranch(PassiveBranch branch) => IsOriginalBranch(branch) ? OriginalNodesPerBranch
        : IsBridgeBranch(branch) ? BridgeNodesPerBranch : IsRingBranch(branch) ? RingNodeCount
        : throw new ArgumentOutOfRangeException(nameof(branch));

    // Original spoke ids intentionally retain their historic slots despite the new visual order.
    static int StableSpokeSlot(PassiveBranch branch) => branch switch
    {
        PassiveBranch.Poison => 0, PassiveBranch.Life => 1, PassiveBranch.Defense => 2,
        PassiveBranch.Mana => 3, PassiveBranch.Magic => 4, PassiveBranch.Projectile => 5,
        PassiveBranch.Cold => 6, PassiveBranch.Fire => 7, PassiveBranch.Lightning => 8,
        PassiveBranch.Physical => 9,
        _ => throw new ArgumentException($"{branch} is not an original branch.", nameof(branch))
    };

    public static int NodeId(PassiveBranch branch, int position)
    {
        int count = NodesInBranch(branch);
        if (position < 0 || position >= count) throw new ArgumentOutOfRangeException(nameof(position));
        if (IsOriginalBranch(branch)) return StableSpokeSlot(branch) * OriginalNodesPerBranch + position;
        if (IsBridgeBranch(branch))
            return OriginalNodeCount + ((int)branch - OriginalBranchCount) * BridgeNodesPerBranch + position;
        return ExistingNodeCount + position;
    }

    public static int TerminalNodeId(PassiveBranch branch) => NodeId(branch, NodesInBranch(branch) - 1);
    public static int KeystoneNodeId(PassiveBranch branch) => KeystoneStartId + (int)branch;
    public static int OuterKeystoneNodeId(PassiveBranch branch)
    {
        for (int i = 0; i < OuterKeystoneNodeCount; i++)
            if (OuterKeystoneBranch(i) == branch) return KeystoneStartId + InnerKeystoneNodeCount + i;
        throw new ArgumentException($"{branch} has no outer keystone.", nameof(branch));
    }
    public static bool IsKeystone(int id) => id >= KeystoneStartId && id < NodeCount;
    public static bool IsRootConnected(int id) => id >= 0 && id < OriginalNodeCount && Node(id).Position == 0;
    public static IReadOnlyList<int> AdjacentNodeIds(int id) => adjacency[id];

    public static PassiveKeystone KeystoneFor(PassiveBranch branch) => branch switch
    {
        PassiveBranch.Defense => PassiveKeystone.IronBastion,
        PassiveBranch.Life => PassiveKeystone.LivingFortress,
        PassiveBranch.Mana => PassiveKeystone.ManaShield,
        PassiveBranch.Magic => PassiveKeystone.ArcaneOverload,
        PassiveBranch.Lightning => PassiveKeystone.LivingCurrent,
        PassiveBranch.Fire => PassiveKeystone.InfernalConversion,
        PassiveBranch.Poison => PassiveKeystone.VenomousTransmutation,
        PassiveBranch.Projectile => PassiveKeystone.BallisticBarrage,
        PassiveBranch.Physical => PassiveKeystone.BruteForce,
        PassiveBranch.Cold => PassiveKeystone.AbsoluteZero,
        _ => PassiveKeystone.None
    };

    public static PassiveBranch OuterKeystoneBranch(int index) => index switch
    {
        0 => PassiveBranch.BleedChance, 1 => PassiveBranch.IgniteChance,
        2 => PassiveBranch.ChillChance, 3 => PassiveBranch.ShockChance,
        4 => PassiveBranch.PoisonChance, 5 => PassiveBranch.LifeRegeneration,
        6 => PassiveBranch.ManaRegeneration, 7 => PassiveBranch.AttackSpeed,
        8 => PassiveBranch.IncreasedProjectileAmount, 9 => PassiveBranch.ChanceToHitTwice,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public static PassiveKeystone OuterKeystoneFor(PassiveBranch branch) => branch switch
    {
        PassiveBranch.BleedChance => PassiveKeystone.OpenWounds,
        PassiveBranch.IgniteChance => PassiveKeystone.Wildfire,
        PassiveBranch.ChillChance => PassiveKeystone.DeepFreeze,
        PassiveBranch.ShockChance => PassiveKeystone.Overcharged,
        PassiveBranch.PoisonChance => PassiveKeystone.ToxicSaturation,
        PassiveBranch.LifeRegeneration => PassiveKeystone.UndyingFlesh,
        PassiveBranch.ManaRegeneration => PassiveKeystone.EndlessCurrent,
        PassiveBranch.AttackSpeed => PassiveKeystone.Frenzy,
        PassiveBranch.IncreasedProjectileAmount => PassiveKeystone.BulletHell,
        PassiveBranch.ChanceToHitTwice => PassiveKeystone.EchoingStrikes,
        _ => PassiveKeystone.None
    };

    public static string KeystoneName(PassiveKeystone keystone) => keystone switch
    {
        PassiveKeystone.IronBastion => "Iron Bastion", PassiveKeystone.LivingFortress => "Living Fortress",
        PassiveKeystone.ManaShield => "Mana Shield", PassiveKeystone.ArcaneOverload => "Arcane Overload",
        PassiveKeystone.LivingCurrent => "Living Current", PassiveKeystone.InfernalConversion => "Infernal Conversion",
        PassiveKeystone.VenomousTransmutation => "Venomous Transmutation",
        PassiveKeystone.BallisticBarrage => "Ballistic Barrage", PassiveKeystone.BruteForce => "Brute Force",
        PassiveKeystone.AbsoluteZero => "Absolute Zero",
        PassiveKeystone.OpenWounds => "Open Wounds", PassiveKeystone.Wildfire => "Wildfire",
        PassiveKeystone.DeepFreeze => "Deep Freeze", PassiveKeystone.Overcharged => "Overcharged",
        PassiveKeystone.ToxicSaturation => "Toxic Saturation", PassiveKeystone.UndyingFlesh => "Undying Flesh",
        PassiveKeystone.EndlessCurrent => "Endless Current", PassiveKeystone.Frenzy => "Frenzy",
        PassiveKeystone.BulletHell => "Bullet Hell", PassiveKeystone.EchoingStrikes => "Echoing Strikes",
        _ => string.Empty
    };

    public static string KeystoneEffect(PassiveKeystone keystone) => keystone switch
    {
        PassiveKeystone.BruteForce => "Deal Double Damage\n25% LESS Attack Speed\nCan only deal Physical Damage",
        PassiveKeystone.AbsoluteZero => "50% of non-Cold Damage converted to Cold\n25% MORE Damage\nCan deal no non-Cold Damage",
        PassiveKeystone.InfernalConversion => "50% of non-Fire Damage converted to Fire\n25% MORE Damage\nCan deal no non-Fire Damage",
        PassiveKeystone.LivingCurrent => "50% of non-Lightning Damage converted to Lightning\n25% MORE Damage\nCan deal no non-Lightning Damage",
        PassiveKeystone.LivingFortress => "50% MORE Maximum Life\n50% LESS Defense",
        PassiveKeystone.ManaShield => "50% of Damage Taken is taken from Mana before Life\n25% LESS Maximum Life",
        PassiveKeystone.BallisticBarrage => "50% MORE Projectile Damage\n50% LESS Non-Projectile Damage\nProjectile behavior is a placeholder",
        PassiveKeystone.ArcaneOverload => "40% MORE Magic Damage\nMana Costs are doubled",
        PassiveKeystone.IronBastion => "75% MORE Defense\n30% LESS Maximum Life",
        PassiveKeystone.VenomousTransmutation => "Hits deal no direct damage\n100% of Hit Damage is instead added to Poison Damage",
        PassiveKeystone.OpenWounds => "Bleed Chance is doubled\nBleeds deal 35% LESS Damage\nBleeds can stack twice as many times",
        PassiveKeystone.Wildfire => "Ignite Chance is doubled\nIgnite Damage is 30% LESS\nIgnites can stack one additional time",
        PassiveKeystone.DeepFreeze => "Chill Chance is doubled\nChill Effectiveness is 25% LESS\nMaximum Chill Effect is increased (amount TBD)",
        PassiveKeystone.Overcharged => "Shock Chance is doubled\nShock requires 50% fewer stacks\nShock-triggered hits deal 35% LESS Damage",
        PassiveKeystone.ToxicSaturation => "Poison Chance is doubled\nPoison Damage is 40% LESS",
        PassiveKeystone.UndyingFlesh => "Life Regeneration is doubled\n25% LESS Maximum Life",
        PassiveKeystone.EndlessCurrent => "Mana Regeneration is doubled\n25% LESS Maximum Mana",
        PassiveKeystone.Frenzy => "50% MORE Attack Speed\n30% LESS Hit Damage",
        PassiveKeystone.BulletHell => "+2 Projectiles\nProjectiles deal 35% LESS Damage\nProjectile spawning is a placeholder",
        PassiveKeystone.EchoingStrikes => "Chance to Hit Twice is doubled\nHits deal 25% LESS Damage",
        _ => string.Empty
    };

    public static PassiveBranch BridgeAtClockwiseGap(int gap) => gap switch
    {
        0 => PassiveBranch.LifeRegeneration, 1 => PassiveBranch.ChanceToHitTwice,
        2 => PassiveBranch.ManaRegeneration, 3 => PassiveBranch.ShockChance,
        4 => PassiveBranch.IgniteChance, 5 => PassiveBranch.PoisonChance,
        6 => PassiveBranch.IncreasedProjectileAmount, 7 => PassiveBranch.AttackSpeed,
        8 => PassiveBranch.BleedChance, 9 => PassiveBranch.ChillChance,
        _ => throw new ArgumentOutOfRangeException(nameof(gap))
    };

    public static void BridgeEndpoints(PassiveBranch branch, out PassiveBranch left, out PassiveBranch right)
    {
        switch (branch)
        {
            case PassiveBranch.LifeRegeneration: left = PassiveBranch.Defense; right = PassiveBranch.Life; return;
            case PassiveBranch.ChanceToHitTwice: left = PassiveBranch.Life; right = PassiveBranch.Mana; return;
            case PassiveBranch.ManaRegeneration: left = PassiveBranch.Mana; right = PassiveBranch.Magic; return;
            case PassiveBranch.ShockChance: left = PassiveBranch.Magic; right = PassiveBranch.Lightning; return;
            case PassiveBranch.IgniteChance: left = PassiveBranch.Lightning; right = PassiveBranch.Fire; return;
            case PassiveBranch.PoisonChance: left = PassiveBranch.Fire; right = PassiveBranch.Poison; return;
            case PassiveBranch.IncreasedProjectileAmount: left = PassiveBranch.Poison; right = PassiveBranch.Projectile; return;
            case PassiveBranch.AttackSpeed: left = PassiveBranch.Projectile; right = PassiveBranch.Physical; return;
            case PassiveBranch.BleedChance: left = PassiveBranch.Physical; right = PassiveBranch.Cold; return;
            case PassiveBranch.ChillChance: left = PassiveBranch.Cold; right = PassiveBranch.Defense; return;
            default: throw new ArgumentException($"{branch} is not a bridge branch.", nameof(branch));
        }
    }

    public static string DisplayName(PassiveBranch branch) => branch switch
    {
        PassiveBranch.IncreasedProjectileAmount => "Increased Projectile Amount",
        PassiveBranch.AttackSpeed => "Attack Speed", PassiveBranch.BleedChance => "Bleed Chance",
        PassiveBranch.Poison => "Void Damage",
        PassiveBranch.PoisonChance => "Poison Chance", PassiveBranch.ChillChance => "Chill Chance",
        PassiveBranch.IgniteChance => "Ignite Chance", PassiveBranch.ShockChance => "Shock Chance",
        PassiveBranch.ChanceToHitTwice => "Chance to Hit Twice",
        PassiveBranch.LifeRegeneration => "Life Regeneration",
        PassiveBranch.ManaRegeneration => "Mana Regeneration",
        PassiveBranch.EmptyTravel => "Outer Ring Travel", _ => branch.ToString()
    };

    public static string GameplayMeaning(PassiveBranch branch) => branch switch
    {
        PassiveBranch.Defense => "armour", PassiveBranch.Life => "maximum HP",
        PassiveBranch.Mana => "maximum mana", PassiveBranch.Magic => "Magic-tagged damage",
        PassiveBranch.Lightning => "Lightning damage", PassiveBranch.Fire => "Fire damage",
        PassiveBranch.Poison => "Void damage", PassiveBranch.Projectile => "projectile skill damage",
        PassiveBranch.Physical => "Physical damage", PassiveBranch.Cold => "Cold damage",
        PassiveBranch.IncreasedProjectileAmount => "additional projectile amount",
        PassiveBranch.AttackSpeed => "attack speed", PassiveBranch.BleedChance => "Bleed chance",
        PassiveBranch.PoisonChance => "Poison chance", PassiveBranch.ChillChance => "Chill chance",
        PassiveBranch.IgniteChance => "Ignite chance", PassiveBranch.ShockChance => "Shock chance",
        PassiveBranch.ChanceToHitTwice => "chance to hit twice",
        PassiveBranch.LifeRegeneration => "life regenerated per second",
        PassiveBranch.ManaRegeneration => "mana regenerated per second",
        PassiveBranch.EmptyTravel => "no stat bonus", _ => "bonus"
    };

    public static bool UsesPercentDisplay(PassiveBranch branch) =>
        branch != PassiveBranch.IncreasedProjectileAmount && branch != PassiveBranch.LifeRegeneration
        && branch != PassiveBranch.ManaRegeneration && branch != PassiveBranch.EmptyTravel;

    static PassiveNodeSize NodeSize(int position, int firstLarge, int terminal) =>
        position < firstLarge ? PassiveNodeSize.Small : position == firstLarge || position == terminal
            ? PassiveNodeSize.Large : PassiveNodeSize.Medium;

    static PassiveNodeDefinition[] BuildNodes()
    {
        var result = new PassiveNodeDefinition[NodeCount];
        for (int i = 0; i < OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            for (int position = 0; position < OriginalNodesPerBranch; position++)
            {
                var size = NodeSize(position, 5, OriginalNodesPerBranch - 1);
                float magnitude = size == PassiveNodeSize.Small ? 5f : size == PassiveNodeSize.Medium ? 10f : 25f;
                int id = NodeId(branch, position);
                result[id] = new PassiveNodeDefinition(id, branch, position, size, magnitude,
                    position == 0 ? -1 : NodeId(branch, position - 1));
            }
        }

        for (int i = OriginalBranchCount; i < OriginalBranchCount + BridgeBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            int first = NodeId(branch, 0);
            BridgeEndpoints(branch, out var left, out var right);
            for (int position = 0; position < BridgeNodesPerBranch; position++)
            {
                var size = NodeSize(position, 4, BridgeNodesPerBranch - 1);
                float magnitude = branch switch
                {
                    PassiveBranch.IncreasedProjectileAmount => size == PassiveNodeSize.Small ? .25f : size == PassiveNodeSize.Medium ? .5f : 1f,
                    PassiveBranch.ChanceToHitTwice => size == PassiveNodeSize.Small ? 2f : size == PassiveNodeSize.Medium ? 5f : 10f,
                    PassiveBranch.LifeRegeneration or PassiveBranch.ManaRegeneration => size == PassiveNodeSize.Small ? 1f : size == PassiveNodeSize.Medium ? 3f : 10f,
                    _ => size == PassiveNodeSize.Small ? 5f : size == PassiveNodeSize.Medium ? 10f : 25f
                };
                int id = first + position;
                int prerequisite = position switch
                {
                    0 => TerminalNodeId(left), 1 => id - 1, 2 => TerminalNodeId(right),
                    3 => id - 1, 4 => first + 1, _ => id - 1
                };
                result[id] = new PassiveNodeDefinition(id, branch, position, size, magnitude, prerequisite);
            }
        }

        for (int position = 0; position < RingNodeCount; position++)
        {
            int id = NodeId(PassiveBranch.EmptyTravel, position);
            int prerequisite = position % RingNodesPerGap == 0
                ? TerminalNodeId(BridgeAtClockwiseGap(position / RingNodesPerGap)) : id - 1;
            result[id] = new PassiveNodeDefinition(id, PassiveBranch.EmptyTravel, position,
                PassiveNodeSize.Small, 0f, prerequisite);
        }
        for (int i = 0; i < OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            int id = KeystoneNodeId(branch);
            result[id] = new PassiveNodeDefinition(id, branch, OriginalNodesPerBranch,
                PassiveNodeSize.Large, 0f, TerminalNodeId(branch), KeystoneFor(branch));
        }
        for (int i = 0; i < OuterKeystoneNodeCount; i++)
        {
            var branch = OuterKeystoneBranch(i);
            int id = KeystoneStartId + InnerKeystoneNodeCount + i;
            result[id] = new PassiveNodeDefinition(id, branch, BridgeNodesPerBranch,
                PassiveNodeSize.Large, 0f, TerminalNodeId(branch), OuterKeystoneFor(branch));
        }
        return result;
    }

    static PassiveTreeEdge[] BuildEdges()
    {
        var result = new List<PassiveTreeEdge>(310);
        for (int i = 0; i < OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            result.Add(new PassiveTreeEdge(-1, NodeId(branch, 0)));
            for (int p = 1; p < OriginalNodesPerBranch; p++)
                result.Add(new PassiveTreeEdge(NodeId(branch, p - 1), NodeId(branch, p)));
        }
        for (int i = OriginalBranchCount; i < OriginalBranchCount + BridgeBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            int first = NodeId(branch, 0);
            BridgeEndpoints(branch, out var left, out var right);
            result.Add(new PassiveTreeEdge(TerminalNodeId(left), first));
            result.Add(new PassiveTreeEdge(first, first + 1));
            result.Add(new PassiveTreeEdge(TerminalNodeId(right), first + 2));
            result.Add(new PassiveTreeEdge(first + 2, first + 3));
            result.Add(new PassiveTreeEdge(first + 1, first + 4));
            result.Add(new PassiveTreeEdge(first + 3, first + 4));
            for (int p = 5; p < BridgeNodesPerBranch; p++)
                result.Add(new PassiveTreeEdge(first + p - 1, first + p));
        }
        for (int gap = 0; gap < OriginalBranchCount; gap++)
        {
            int first = NodeId(PassiveBranch.EmptyTravel, gap * RingNodesPerGap);
            result.Add(new PassiveTreeEdge(TerminalNodeId(BridgeAtClockwiseGap(gap)), first));
            for (int p = 1; p < RingNodesPerGap; p++)
                result.Add(new PassiveTreeEdge(first + p - 1, first + p));
            result.Add(new PassiveTreeEdge(first + RingNodesPerGap - 1,
                TerminalNodeId(BridgeAtClockwiseGap((gap + 1) % OriginalBranchCount))));
        }
        for (int i = 0; i < OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            result.Add(new PassiveTreeEdge(TerminalNodeId(branch), KeystoneNodeId(branch)));
        }
        for (int i = 0; i < OuterKeystoneNodeCount; i++)
        {
            var branch = OuterKeystoneBranch(i);
            result.Add(new PassiveTreeEdge(TerminalNodeId(branch), OuterKeystoneNodeId(branch)));
        }
        return result.ToArray();
    }

    static int[][] BuildAdjacency()
    {
        var lists = new List<int>[NodeCount];
        for (int i = 0; i < lists.Length; i++) lists[i] = new List<int>(3);
        foreach (var edge in edges)
        {
            if (edge.A < 0) continue;
            lists[edge.A].Add(edge.B);
            lists[edge.B].Add(edge.A);
        }
        var result = new int[NodeCount][];
        for (int i = 0; i < result.Length; i++) result[i] = lists[i].ToArray();
        return result;
    }
}
