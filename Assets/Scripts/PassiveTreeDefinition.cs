// Passive Tree V3 runtime topology. Node content is authoritative in Branch ScriptableObjects.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PassiveBranch { Defense, Life, Mana, Magic, Lightning, Fire, Poison, Projectile, Physical, Cold, IncreasedProjectileAmount, AttackSpeed, BleedChance, PoisonChance, ChillChance, IgniteChance, ShockChance, ChanceToHitTwice, LifeRegeneration, ManaRegeneration, CriticalChance, CriticalMultiplier, LifeOnHit, ManaOnHit, LifeOnKill, ManaOnKill, Strength, Dexterity, Intelligence, CooldownReduction, ProjectileSpeed, PrecisionChance, PrecisionDamage, RageGeneration, RageEffect, RageRetention, BleedDamage, IgniteDamage, PoisonDamage, ShockEffect, ChillEffect, PoisonSpeed, Resistances, PhysicalPenetration, LargeHitPower, EmptyTravel }
public enum PassiveNodeSize { Small, Medium, Large }
public enum PassiveNodeKind { ClassStart, Travel, Small, Notable, Keystone, Spine, Choice, SubclassChoice, WeaponSpine }
public enum PassiveRegion { Warrior, Ranger, Thief, Mage, Priest, Barbarian, WarriorRanger, RangerThief, ThiefMage, MagePriest, PriestBarbarian, BarbarianWarrior, Center }
public enum PassiveKeystone { None, BruteForce, InfernalConversion, VenomousTransmutation, ManaShield, LivingCurrent, RageFinisher, IronBastion, LivingFortress, ArcaneOverload, AbsoluteZero, BallisticBarrage, OpenWounds, Wildfire, DeepFreeze, Overcharged, ToxicSaturation, UndyingFlesh, EndlessCurrent, Frenzy, BulletHell, EchoingStrikes }

[Serializable]
public readonly struct PassiveEffect
{
    public readonly StatTypes Stat;
    public readonly float Amount;
    public PassiveEffect(StatTypes stat, float amount) { Stat = stat; Amount = amount; }
}

public readonly struct PassiveNodeDefinition
{
    public readonly int Id, Position, PrerequisiteId, Tier;
    public readonly string StableId, DisplayName, Description, WeaponTypeRestriction, RouteClassId, RouteWeaponId, ChoiceGroupId;
    public readonly PassiveBranch Branch;
    public readonly PassiveNodeSize Size;
    public readonly PassiveNodeKind Kind;
    public readonly PassiveRegion Region;
    public readonly Vector2 LayoutPosition;
    public readonly float Magnitude;
    public readonly PassiveKeystone Keystone;
    public readonly PassiveEffect[] Effects;
    public readonly string[] MechanicIds;
    public readonly float[] MechanicValues;
    public readonly PassiveExtensionMetadata ExtensionMetadata;
    public bool IsClassRoute => !string.IsNullOrEmpty(RouteClassId);
    public bool IsWeaponRoute => !string.IsNullOrEmpty(RouteWeaponId);
    public bool IsChoice => Kind is PassiveNodeKind.Choice or PassiveNodeKind.SubclassChoice;
    public bool IsSubclassChoice => Kind == PassiveNodeKind.SubclassChoice;

    public PassiveNodeDefinition(int id, string stable, string name, PassiveBranch branch, int position, PassiveNodeSize size, PassiveNodeKind kind, PassiveRegion region, Vector2 layout, int prerequisite, PassiveEffect[] effects, string[] mechanicIds = null, float[] mechanicValues = null, string weapon = null, PassiveKeystone keystone = PassiveKeystone.None, PassiveExtensionMetadata metadata = null, string description = null, string routeClass = null, string routeWeapon = null, int tier = 0, string group = null)
    {
        Id = id; StableId = stable; DisplayName = name; Branch = branch; Position = position; Size = size; Kind = kind; Region = region; LayoutPosition = layout; PrerequisiteId = prerequisite;
        Effects = effects ?? Array.Empty<PassiveEffect>(); MechanicIds = mechanicIds ?? Array.Empty<string>(); MechanicValues = mechanicValues ?? Array.Empty<float>(); WeaponTypeRestriction = weapon ?? string.Empty; Keystone = keystone;
        ExtensionMetadata = metadata ?? new PassiveExtensionMetadata(); Description = description ?? string.Empty; Magnitude = Effects.Length > 0 ? Effects[0].Amount : MechanicValues.Length > 0 ? MechanicValues[0] : 0;
        RouteClassId = routeClass ?? string.Empty; RouteWeaponId = routeWeapon ?? string.Empty; Tier = tier; ChoiceGroupId = group ?? string.Empty;
    }
}

public readonly struct PassiveTreeEdge { public readonly int A, B; public PassiveTreeEdge(int a, int b) { A = a; B = b; } }

public static class PassiveTreeDefinition
{
    public const int ClassCount = 6, ClassTierCount = 10, WeaponCount = 6, WeaponTierCount = 5, GenericChoicesPerGroup = 3, ChoiceGroupsPerTier = 2, NodesPerClass = 90, NodesPerWeapon = 35, NodeCount = 750, PointCost = 1;
    public const float WeaponSpecificEfficiencyMultiplier = 1.60f, BranchAngleDegrees = 60f;
    public const int ClassSectorCount = 6, NodesPerSector = 90, BridgeRegionCount = 0, BridgeNodesPerRegion = 0, CenterNodeCount = 0, OriginalBranchCount = 10, BridgeBranchCount = 10, BranchCount = 37, OriginalNodesPerBranch = 90, NodesPerBranch = 90, RingNodesPerGap = 0, RingNodeCount = 0, OriginalNodeCount = 540, ExistingNodeCount = 750, InnerKeystoneNodeCount = 0, OuterKeystoneNodeCount = 0, KeystoneNodeCount = 0, KeystoneStartId = 0;

    static readonly List<string> classes = new() { PlayerClassIds.Warrior, PlayerClassIds.Ranger, PlayerClassIds.Thief, PlayerClassIds.Mage, PlayerClassIds.Priest, PlayerClassIds.Barbarian };
    static readonly float[] angles = { 90, 30, -30, -90, -150, 150 };
    static readonly List<string> weapons = new() { WeaponTypeIds.Sword, WeaponTypeIds.Bow, WeaponTypeIds.Dagger, WeaponTypeIds.Staff, WeaponTypeIds.Sceptre, WeaponTypeIds.TwoHandedAxe };
    static readonly List<PassiveNodeDefinition> building = new(NodeCount);
    static readonly List<PassiveTreeEdge> edgeBuild = new(NodeCount);
    static readonly Dictionary<string, int> stableIds = new(StringComparer.Ordinal);
    static readonly int[,] classSpines = new int[ClassCount, ClassTierCount], weaponSpines = new int[WeaponCount, WeaponTierCount];
    static readonly PassiveTreeDatabaseSO database;
    static readonly PassiveNodeDefinition[] nodes;
    static readonly PassiveTreeEdge[] edges;
    static readonly int[][] adjacency;

    static PassiveTreeDefinition()
    {
        database = Resources.Load<PassiveTreeDatabaseSO>("GameData/PassiveTree/SO_PassiveTreeDatabase");
        if (database == null) throw new InvalidOperationException("Authoritative Passive Tree database is missing from Resources/GameData/PassiveTree/SO_PassiveTreeDatabase.");
        for (int i = 0; i < ClassCount; i++) BuildClass(i, RequiredClassBranch(classes[i]));
        for (int i = 0; i < WeaponCount; i++) BuildWeapon(i, RequiredWeaponBranch(weapons[i]));
        nodes = building.ToArray(); edges = edgeBuild.ToArray(); adjacency = BuildAdjacency();
        if (nodes.Length != NodeCount) throw new InvalidOperationException($"Passive Tree V3 expected {NodeCount} nodes, got {nodes.Length} from authoring assets.");
    }

    public static PassiveTreeDatabaseSO Database => database;
    public static IReadOnlyList<PassiveNodeDefinition> Nodes => nodes;
    public static IReadOnlyList<PassiveTreeEdge> Edges => edges;
    public static IReadOnlyList<string> ClassIds => classes;
    public static IReadOnlyList<string> WeaponIds => weapons;
    public static PassiveNodeDefinition Node(int id) => id >= 0 && id < NodeCount ? nodes[id] : throw new ArgumentOutOfRangeException(nameof(id));
    public static bool TryNode(string stable, out PassiveNodeDefinition node) { if (stable != null && stableIds.TryGetValue(stable, out int id)) { node = nodes[id]; return true; } node = default; return false; }
    public static int NodeId(string stable) => stable != null && stableIds.TryGetValue(stable, out int id) ? id : -1;
    public static int ClassIndex(string id) { for (int i = 0; i < classes.Count; i++) if (classes[i] == id) return i; return -1; }
    public static int WeaponIndex(string id) { for (int i = 0; i < weapons.Count; i++) if (weapons[i] == id) return i; return -1; }
    public static int ClassSpineNode(string id, int tier) { int i = ClassIndex(id); return i >= 0 && tier is >= 1 and <= ClassTierCount ? classSpines[i, tier - 1] : -1; }
    public static int WeaponSpineNode(string id, int tier) { int i = WeaponIndex(id); return i >= 0 && tier is >= 1 and <= WeaponTierCount ? weaponSpines[i, tier - 1] : -1; }
    public static int StartNodeId(string id) => ClassSpineNode(id, 1);
    public static bool IsClassStart(int id) => id >= 0 && id < NodeCount && nodes[id].Kind == PassiveNodeKind.Spine && nodes[id].Tier == 1;
    public static int RageFinisherNodeId => NodeId("tree.v3.weapon.two_handed_axe.t05.right.c");
    public static bool IsKeystone(int id) => id >= 0 && id < NodeCount && nodes[id].Keystone != PassiveKeystone.None;
    public static bool IsRootConnected(int id, string classId) => id == StartNodeId(classId);
    public static bool IsRootConnected(int id) => IsRootConnected(id, PlayerClassIds.Warrior);
    public static IReadOnlyList<int> AdjacentNodeIds(int id) => id >= 0 && id < NodeCount ? adjacency[id] : Array.Empty<int>();
    public static bool IsClassSpineComplete(string id, Func<int, bool> allocated) { for (int t = 1; t <= ClassTierCount; t++) if (!allocated(ClassSpineNode(id, t))) return false; return true; }
    public static string SignatureWeapon(string classId) => RequiredClassBranch(classId).SignatureWeaponId;
    public static string WeaponClass(string weaponId) => RequiredWeaponBranch(weaponId).OwningClassId;
    public static IEnumerable<int> ChoiceNodes(string group) { foreach (var node in nodes) if (node.ChoiceGroupId == group) yield return node.Id; }
    public static IEnumerable<int> RouteNodes(string classId, string weaponId = null) { foreach (var node in nodes) if ((weaponId == null && node.RouteClassId == classId) || (weaponId != null && node.RouteWeaponId == weaponId)) yield return node.Id; }
    public static PassiveKeystone KeystoneAt(int id) => Node(id).Keystone;
    public static bool IsOriginalBranch(PassiveBranch branch) => false;
    public static bool IsBridgeBranch(PassiveBranch branch) => false;
    public static bool IsRingBranch(PassiveBranch branch) => false;
    public static int NodesInBranch(PassiveBranch branch) { int count = 0; foreach (var node in nodes) if (node.Branch == branch) count++; return count; }
    public static int NodeId(PassiveBranch branch, int position) { int count = 0; foreach (var node in nodes) if (node.Branch == branch && count++ == position) return node.Id; throw new ArgumentOutOfRangeException(nameof(position)); }
    public static int TerminalNodeId(PassiveBranch branch) => NodeId(branch, NodesInBranch(branch) - 1);
    public static PassiveKeystone KeystoneFor(PassiveBranch branch) => PassiveKeystone.None;
    public static int KeystoneNodeId(PassiveBranch branch) { foreach (var node in nodes) if (node.Branch == branch && node.Keystone != PassiveKeystone.None) return node.Id; return -1; }
    public static PassiveBranch OuterKeystoneBranch(int index) => throw new ArgumentOutOfRangeException(nameof(index));
    public static PassiveKeystone OuterKeystoneFor(PassiveBranch branch) => PassiveKeystone.None;
    public static int OuterKeystoneNodeId(PassiveBranch branch) => -1;

#if UNITY_EDITOR
    public static int FindIndexForTest(PassiveKeystone keystone) { foreach (var node in nodes) if (node.Keystone == keystone) return node.Id; return -1; }
#endif

    public static string KeystoneName(PassiveKeystone keystone) => keystone == PassiveKeystone.RageFinisher ? "Rage Finisher" : string.Empty;
    public static string KeystoneEffect(PassiveKeystone keystone) => keystone == PassiveKeystone.RageFinisher ? "At maximum Rage, arm one empowered Axe attack." : string.Empty;
    public static string DisplayName(PassiveBranch branch) => branch switch { PassiveBranch.Poison => "Void Damage", PassiveBranch.IncreasedProjectileAmount => "Additional Projectiles", PassiveBranch.ChanceToHitTwice => "Hit Twice", PassiveBranch.CriticalChance => "Critical Chance", PassiveBranch.CriticalMultiplier => "Critical Multiplier", PassiveBranch.PrecisionChance => "Projectile Precision", PassiveBranch.PrecisionDamage => "Precision Damage", _ => Split(branch.ToString()) };
    public static string GameplayMeaning(PassiveBranch branch) => DisplayName(branch);
    public static bool UsesPercentDisplay(PassiveBranch branch) => branch is not (PassiveBranch.LifeRegeneration or PassiveBranch.ManaRegeneration or PassiveBranch.LifeOnHit or PassiveBranch.ManaOnHit or PassiveBranch.LifeOnKill or PassiveBranch.ManaOnKill or PassiveBranch.IncreasedProjectileAmount or PassiveBranch.EmptyTravel);

    public static PassiveEffect[] SubclassEffects(string subclassId, PassiveNodeDefinition node)
    {
        if (!node.IsSubclassChoice || string.IsNullOrEmpty(subclassId)) return Array.Empty<PassiveEffect>();
        PassiveClassBranchSO branch = RequiredClassBranch(node.RouteClassId);
        PassiveClassTierData tier = branch.Tiers[node.Tier - 1];
        PassiveChoiceSideData side = node.ChoiceGroupId.EndsWith("right", StringComparison.Ordinal) ? tier.Right : tier.Left;
        PassiveAuthoredNode authored = subclassId == branch.SubclassAId ? side.SubclassA : subclassId == branch.SubclassBId ? side.SubclassB : null;
        return authored == null ? Array.Empty<PassiveEffect>() : StatEffects(authored);
    }

    public static PassiveAuthoredNode AuthoredNode(PassiveNodeDefinition node, string subclassId = null)
    {
        if (node.IsSubclassChoice && !string.IsNullOrEmpty(subclassId))
        {
            PassiveClassBranchSO branch = RequiredClassBranch(node.RouteClassId); PassiveClassTierData tier = branch.Tiers[node.Tier - 1];
            PassiveChoiceSideData side = node.ChoiceGroupId.EndsWith("right", StringComparison.Ordinal) ? tier.Right : tier.Left;
            return subclassId == branch.SubclassAId ? side.SubclassA : subclassId == branch.SubclassBId ? side.SubclassB : null;
        }
        PassiveBranchDataSO route = node.IsClassRoute ? RequiredClassBranch(node.RouteClassId) : RequiredWeaponBranch(node.RouteWeaponId);
        return route.AllAuthoredNodes().FirstOrDefault(candidate => candidate != null && candidate.LogicalSlotId == node.StableId);
    }

    static void BuildClass(int classIndex, PassiveClassBranchSO branch)
    {
        if (branch.Tiers.Count != ClassTierCount) throw new InvalidOperationException(branch.name + " must contain ten tiers.");
        Vector2 direction = Direction(angles[classIndex]), tangent = new(-direction.y, direction.x); int previous = -1;
        for (int tierNumber = 1; tierNumber <= ClassTierCount; tierNumber++)
        {
            PassiveClassTierData tier = branch.Tiers[tierNumber - 1]; Vector2 position = direction * (430 + (tierNumber - 1) * 255);
            int spine = Add(tier.Spine, tier.Spine.LogicalSlotId, PassiveNodeKind.Spine, (PassiveRegion)classIndex, position, previous, routeClass: branch.ClassId, tier: tierNumber);
            classSpines[classIndex, tierNumber - 1] = spine; if (previous >= 0) Edge(previous, spine); previous = spine;
            BuildClassGroup(classIndex, tierNumber, false, spine, position, direction, tangent, tier.Left, branch.ClassId);
            BuildClassGroup(classIndex, tierNumber, true, spine, position, direction, tangent, tier.Right, branch.ClassId);
        }
    }

    static void BuildClassGroup(int classIndex, int tier, bool right, int spine, Vector2 position, Vector2 direction, Vector2 tangent, PassiveChoiceSideData sideData, string classId)
    {
        int sign = right ? 1 : -1; string side = right ? "right" : "left"; string group = $"tree.v3.class.{ClassSlug(classId)}.t{tier:00}.{side}";
        Vector2[] positions = { position + tangent * (sign * 260) + direction * 80, position + tangent * (sign * 350) + direction * 40, position + tangent * (sign * 350) - direction * 40, position + tangent * (sign * 260) - direction * 80 };
        PassiveAuthoredNode[] choices = { sideData.A, sideData.B, sideData.C };
        for (int i = 0; i < choices.Length; i++) { int id = Add(choices[i], choices[i].LogicalSlotId, PassiveNodeKind.Choice, (PassiveRegion)classIndex, positions[i], spine, routeClass: classId, tier: tier, group: group); Edge(spine, id); }
        PassiveAuthoredNode subclass = sideData.SubclassA;
        int subclassId = Add(subclass, subclass.LogicalSlotId, PassiveNodeKind.SubclassChoice, (PassiveRegion)classIndex, positions[3], spine, routeClass: classId, tier: tier, group: group, suppressEffects: true); Edge(spine, subclassId);
    }

    static void BuildWeapon(int classIndex, PassiveWeaponBranchSO branch)
    {
        if (branch.Tiers.Count != WeaponTierCount) throw new InvalidOperationException(branch.name + " must contain five tiers.");
        Vector2 direction = Direction(angles[classIndex]), tangent = new(-direction.y, direction.x); int previous = -1;
        for (int tierNumber = 1; tierNumber <= WeaponTierCount; tierNumber++)
        {
            PassiveWeaponTierData tier = branch.Tiers[tierNumber - 1]; Vector2 position = direction * (2980 + (tierNumber - 1) * 255);
            int spine = Add(tier.Spine, tier.Spine.LogicalSlotId, PassiveNodeKind.WeaponSpine, (PassiveRegion)classIndex, position, previous, weapon: branch.WeaponId, routeWeapon: branch.WeaponId, tier: tierNumber);
            weaponSpines[classIndex, tierNumber - 1] = spine; if (previous >= 0) Edge(previous, spine); else Edge(classSpines[classIndex, ClassTierCount - 1], spine); previous = spine;
            BuildWeaponGroup(classIndex, tierNumber, false, spine, position, direction, tangent, tier.Left, branch.WeaponId);
            BuildWeaponGroup(classIndex, tierNumber, true, spine, position, direction, tangent, tier.Right, branch.WeaponId);
        }
    }

    static void BuildWeaponGroup(int classIndex, int tier, bool right, int spine, Vector2 position, Vector2 direction, Vector2 tangent, PassiveChoiceSideData sideData, string weaponId)
    {
        int sign = right ? 1 : -1; string side = right ? "right" : "left"; string group = $"tree.v3.weapon.{weaponId.Replace("weapon.", string.Empty)}.t{tier:00}.{side}";
        Vector2[] positions = { position + tangent * (sign * 260) + direction * 80, position + tangent * (sign * 330), position + tangent * (sign * 260) - direction * 80 };
        PassiveAuthoredNode[] choices = { sideData.A, sideData.B, sideData.C };
        for (int i = 0; i < choices.Length; i++) { int id = Add(choices[i], choices[i].LogicalSlotId, PassiveNodeKind.Choice, (PassiveRegion)classIndex, positions[i], spine, weapon: weaponId, routeWeapon: weaponId, tier: tier, group: group); Edge(spine, id); }
    }

    static int Add(PassiveAuthoredNode authored, string stableId, PassiveNodeKind runtimeKind, PassiveRegion region, Vector2 layout, int prerequisite, string weapon = null, string routeClass = null, string routeWeapon = null, int tier = 0, string group = null, bool suppressEffects = false)
    {
        if (authored == null) throw new InvalidOperationException("Passive authoring node is missing for " + stableId);
        int id = building.Count; if (string.IsNullOrWhiteSpace(stableId) || stableIds.ContainsKey(stableId)) throw new InvalidOperationException("Missing or duplicate V3 passive ID: " + stableId); stableIds.Add(stableId, id);
        PassiveEffect[] statEffects = suppressEffects ? Array.Empty<PassiveEffect>() : StatEffects(authored);
        string[] mechanicIds = suppressEffects ? Array.Empty<string>() : authored.Effects.Where(x => x.Kind == PassiveEffectKind.Mechanic).Select(x => x.MechanicId).ToArray();
        float[] mechanicValues = suppressEffects ? Array.Empty<float>() : authored.Effects.Where(x => x.Kind == PassiveEffectKind.Mechanic).Select(x => x.Value).ToArray();
        var metadata = new PassiveExtensionMetadata { StableSectionId = group ?? stableId, RequiredSubclassId = runtimeKind == PassiveNodeKind.SubclassChoice ? "selected" : string.Empty, ClassStartIds = string.IsNullOrEmpty(routeClass) ? Array.Empty<string>() : new[] { routeClass }, SpecializationGroupId = group ?? string.Empty, MutuallyExclusive = !string.IsNullOrEmpty(group), IsTravelNode = runtimeKind is PassiveNodeKind.Spine or PassiveNodeKind.WeaponSpine };
        building.Add(new PassiveNodeDefinition(id, stableId, authored.DisplayName, authored.Branch, id, authored.Size, runtimeKind, region, layout, prerequisite, statEffects, mechanicIds, mechanicValues, weapon, authored.Keystone, metadata, authored.Description, routeClass, routeWeapon, tier, group));
        return id;
    }

    static PassiveEffect[] StatEffects(PassiveAuthoredNode authored) => authored.Effects.Where(x => x.Kind == PassiveEffectKind.Stat).Select(x => new PassiveEffect(x.Stat, x.Value)).ToArray();
    static void Edge(int a, int b) => edgeBuild.Add(new PassiveTreeEdge(a, b));
    static int[][] BuildAdjacency() { var lists = new List<int>[NodeCount]; for (int i = 0; i < lists.Length; i++) lists[i] = new(); foreach (var edge in edgeBuild) { lists[edge.A].Add(edge.B); lists[edge.B].Add(edge.A); } var result = new int[lists.Length][]; for (int i = 0; i < lists.Length; i++) result[i] = lists[i].ToArray(); return result; }
    static PassiveClassBranchSO RequiredClassBranch(string id) => database.ClassBranches.FirstOrDefault(x => x != null && x.ClassId == id) ?? throw new InvalidOperationException("Missing class branch authoring asset: " + id);
    static PassiveWeaponBranchSO RequiredWeaponBranch(string id) => database.WeaponBranches.FirstOrDefault(x => x != null && x.WeaponId == id) ?? throw new InvalidOperationException("Missing weapon branch authoring asset: " + id);
    static Vector2 Direction(float angle) { float radians = angle * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)); }
    static string ClassSlug(string classId) => classId.Replace("class.", string.Empty);
    static string Split(string value) { for (int i = 1; i < value.Length; i++) if (char.IsUpper(value[i])) value = value.Insert(i++, " "); return value; }
}
