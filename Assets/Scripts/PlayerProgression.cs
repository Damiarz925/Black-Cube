// Developer map: Session XP and the 290-node radial/bridge/ring/keystone passive tree on GameManager.
// Allocations replace stat modifiers by this component as source; encounter restart retains them.
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Session progression lives with GameManager; encounter restart never resets it.</summary>
public sealed class PlayerProgression : MonoBehaviour
{
    [Header("Forgiving prototype XP (rebalance here)")]
    [SerializeField, Min(1)] int maxLevel = 100;
    [SerializeField, Min(1)] float maxEnemiesFrom99To100 = 1000;
    [SerializeField, Min(1)] float bossXpMultiplier = 8f;
    [SerializeField] int level = 1;
    [SerializeField] double experience;
    [SerializeField] int availablePoints;
    [SerializeField] int[] ranks = new int[PassiveTreeDefinition.NodeCount];

    const double ReqAtLevel10Xp = 300d;
    const double ReqAtLevel16Xp = 619d;
    const int ReqAnchorLevel10 = 10;
    const int ReqAnchorLevel16 = 16;
    const int TopTransitionLevel = 99;

    StatsComponent boundStats;
    PassiveKeystoneState boundKeystones;
    public event Action Changed;
    public int Level => level;
    public int AvailablePoints => availablePoints;
    public double Experience => experience;
    public bool AtCap => level >= Mathf.Max(1, maxLevel);
    public double RequiredXp => RequirementAt(level);
    public double RequirementAt(int atLevel) => atLevel < maxLevel ? Math.Max(1d, Math.Round(ReqAtLevel10Xp * Math.Pow(GetRequirementGrowth(), atLevel - ReqAnchorLevel10))) : 0d;
    public double EnemyReward(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => RequirementAt(combatLevel) / KillsToLevelUp(combatLevel) * EnemyXpMultiplier(rarity, wasBoss);

    private void Awake() => NormalizeAllocations();
    private void OnEnable() => NormalizeAllocations();

    public int Rank(int node) => ranks != null && node >= 0 && node < ranks.Length && ranks[node] != 0 ? 1 : 0;
    public bool IsAllocated(int node) => Rank(node) > 0;
    public bool HasKeystone(PassiveKeystone keystone)
    {
        if (keystone == PassiveKeystone.None) return false;
        for (int i = 0; i < PassiveTreeDefinition.OriginalBranchCount; i++)
        {
            var branch = (PassiveBranch)i;
            if (PassiveTreeDefinition.KeystoneFor(branch) == keystone)
                return IsAllocated(PassiveTreeDefinition.KeystoneNodeId(branch));
        }
        for (int i = 0; i < PassiveTreeDefinition.OuterKeystoneNodeCount; i++)
        {
            var branch = PassiveTreeDefinition.OuterKeystoneBranch(i);
            if (PassiveTreeDefinition.OuterKeystoneFor(branch) == keystone)
                return IsAllocated(PassiveTreeDefinition.OuterKeystoneNodeId(branch));
        }
        return false;
    }
    public bool CanSpend(int node)
    {
        if (node < 0 || node >= PassiveTreeDefinition.NodeCount || availablePoints < PassiveTreeDefinition.PointCost || IsAllocated(node))
            return false;
        if (PassiveTreeDefinition.IsRootConnected(node)) return true;
        foreach (int adjacent in PassiveTreeDefinition.AdjacentNodeIds(node))
            if (IsAllocated(adjacent)) return true;
        return false;
    }

    public bool TrySpend(int node)
    {
        if (!CanSpend(node)) return false;
        ranks[node] = 1;
        availablePoints -= PassiveTreeDefinition.PointCost;
        BindStats();
        ApplySkills();
        Changed?.Invoke();
        return true;
    }

    public bool CanRefund(int node)
    {
        if (!IsAllocated(node)) return false;
        var reachable = new bool[PassiveTreeDefinition.NodeCount];
        var queue = new Queue<int>();
        for (int id = 0; id < PassiveTreeDefinition.NodeCount; id++)
        {
            if (id != node && IsAllocated(id) && PassiveTreeDefinition.IsRootConnected(id))
            {
                reachable[id] = true;
                queue.Enqueue(id);
            }
        }
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int adjacent in PassiveTreeDefinition.AdjacentNodeIds(current))
            {
                if (adjacent == node || reachable[adjacent] || !IsAllocated(adjacent)) continue;
                reachable[adjacent] = true;
                queue.Enqueue(adjacent);
            }
        }
        for (int id = 0; id < PassiveTreeDefinition.NodeCount; id++)
            if (id != node && IsAllocated(id) && !reachable[id]) return false;
        return true;
    }

    public bool TryRefund(int node)
    {
        if (!CanRefund(node)) return false;
        ranks[node] = 0;
        availablePoints += PassiveTreeDefinition.PointCost;
        BindStats();
        ApplySkills();
        Changed?.Invoke();
        return true;
    }

    public int AllocatedCount(PassiveBranch branch)
    {
        int count = 0;
        int first = PassiveTreeDefinition.NodeId(branch, 0);
        for (int position = 0; position < PassiveTreeDefinition.NodesInBranch(branch); position++) count += Rank(first + position);
        return count;
    }

    /// <summary>Returns raw percentage points contributed by allocated nodes.</summary>
    public float GetBonus(PassiveBranch branch)
    {
        float total = 0f;
        int first = PassiveTreeDefinition.NodeId(branch, 0);
        for (int position = 0; position < PassiveTreeDefinition.NodesInBranch(branch); position++)
            if (IsAllocated(first + position)) total += PassiveTreeDefinition.Node(first + position).Magnitude;
        return total;
    }

    public void AwardEnemy(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => AddExperience(EnemyReward(combatLevel, rarity, wasBoss));
    public void AddExperience(double amount)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0 || AtCap) return;
        experience += amount * (RelicInventory.Instance != null ? RelicInventory.Instance.ExperienceMultiplier : 1f);
        var player = FindFirstObjectByType<PlayerController>();
        var health = player != null ? player.GetComponent<HealthComponent>() : null;
        while (!AtCap && experience >= RequiredXp)
        {
            experience -= RequiredXp;
            level++;
            availablePoints++;
            if (health != null) health.RestoreFullLife();
            Debug.Log($"Player level {level}: +1 passive point, full HP.");
        }
        if (AtCap) experience = 0;
        Changed?.Invoke();
    }

    public void ResetProgression()
    {
        level = 1;
        experience = 0;
        availablePoints = 0;
        ranks = new int[PassiveTreeDefinition.NodeCount];
        BindStats();
        ApplySkills();
        Changed?.Invoke();
    }

    void Update() => BindStats();

    void BindStats()
    {
        if (boundStats != null) return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        boundStats = player.GetComponent<StatsComponent>();
        boundKeystones = player.GetComponent<PassiveKeystoneState>();
        if (boundKeystones == null) boundKeystones = player.gameObject.AddComponent<PassiveKeystoneState>();
        ApplySkills();
    }

    void ApplySkills()
    {
        if (boundStats == null) return;
        boundStats.BeginUpdate();
        try
        {
            boundStats.RemoveModifiersFromSource(this);
            AddPercent(StatTypes.ArmourPercent, PassiveBranch.Defense);
            AddPercent(StatTypes.LifePercent, PassiveBranch.Life);
            AddPercent(StatTypes.ManaPercent, PassiveBranch.Mana);
            AddPercent(StatTypes.MagicDmg, PassiveBranch.Magic);
            AddPercent(StatTypes.LightDmg, PassiveBranch.Lightning);
            AddPercent(StatTypes.FireDmg, PassiveBranch.Fire);
            AddPercent(StatTypes.PoisonDmg, PassiveBranch.Poison);
            AddPercent(StatTypes.ProjectileDmg, PassiveBranch.Projectile);
            AddPercent(StatTypes.PhysDmg, PassiveBranch.Physical);
            AddPercent(StatTypes.ColdDmg, PassiveBranch.Cold);
            AddFlat(StatTypes.ProjectileAmount, PassiveBranch.IncreasedProjectileAmount);
            AddPercent(StatTypes.AttackSpeed, PassiveBranch.AttackSpeed);
            AddPercent(StatTypes.BleedChance, PassiveBranch.BleedChance);
            AddPercent(StatTypes.PoisonChance, PassiveBranch.PoisonChance);
            AddPercent(StatTypes.ChillChance, PassiveBranch.ChillChance);
            AddPercent(StatTypes.IgniteChance, PassiveBranch.IgniteChance);
            AddPercent(StatTypes.ShockChance, PassiveBranch.ShockChance);
            AddPercent(StatTypes.ChanceToHitTwice, PassiveBranch.ChanceToHitTwice);
            AddFlat(StatTypes.LifeRegeneration, PassiveBranch.LifeRegeneration);
            AddFlat(StatTypes.ManaRegeneration, PassiveBranch.ManaRegeneration);
            boundKeystones?.Apply(this);
        }
        finally { boundStats.EndUpdate(); }
    }

    void AddPercent(StatTypes stat, PassiveBranch branch)
    {
        AddFlat(stat, branch);
    }

    void AddFlat(StatTypes stat, PassiveBranch branch)
    {
        float amount = GetBonus(branch);
        if (amount > 0f) boundStats.AddModifier(new StatModifier(stat, StatOp.Flat, amount, this));
    }

    // Existing prefabs serialize the retired six rank counters. Preserve the number
    // of points invested in their Life/Damage/Speed themes, then use only new modifiers.
    void NormalizeAllocations()
    {
        if (ranks != null && ranks.Length == PassiveTreeDefinition.NodeCount)
        {
            for (int i = 0; i < ranks.Length; i++) ranks[i] = ranks[i] != 0 ? 1 : 0;
            return;
        }

        int[] legacy = ranks;
        ranks = new int[PassiveTreeDefinition.NodeCount];
        if (legacy == null) return;
        if (legacy.Length != 6)
        {
            int preserved = Mathf.Min(legacy.Length, PassiveTreeDefinition.NodeCount);
            for (int i = 0; i < preserved; i++) ranks[i] = legacy[i] != 0 ? 1 : 0;
            return;
        }
        MigrateLegacyBranch(PassiveBranch.Life, legacy[0] + legacy[3]);
        // The retired broad-Damage branch was explicitly replaced by Poison.
        MigrateLegacyBranch(PassiveBranch.Poison, legacy[1] + legacy[4]);
        MigrateLegacyBranch(PassiveBranch.Mana, legacy[2] + legacy[5]);
    }

    void MigrateLegacyBranch(PassiveBranch branch, int spentPoints)
    {
        int count = Mathf.Clamp(spentPoints, 0, PassiveTreeDefinition.NodesInBranch(branch));
        int first = PassiveTreeDefinition.NodeId(branch, 0);
        for (int i = 0; i < count; i++) ranks[first + i] = 1;
    }

    void OnDestroy()
    {
        boundKeystones?.Apply(null);
        if (boundStats != null) boundStats.RemoveModifiersFromSource(this);
    }

    double GetRequirementGrowth() => Math.Pow(ReqAtLevel16Xp / ReqAtLevel10Xp, 1d / (ReqAnchorLevel16 - ReqAnchorLevel10));

    double KillsToLevelUp(int atLevel)
    {
        if (atLevel <= 1) return 1d;
        if (atLevel >= TopTransitionLevel) return Math.Max(1d, maxEnemiesFrom99To100);
        double max = Math.Max(1d, maxEnemiesFrom99To100);
        double progress = (atLevel - 1d) / (TopTransitionLevel - 1d);
        return Math.Round(Math.Pow(max, progress));
    }

    double EnemyXpMultiplier(EnemyAI.EnemyRarity rarity, bool wasBoss)
    {
        if (wasBoss) return Mathf.Max(1f, bossXpMultiplier);
        return rarity switch
        {
            EnemyAI.EnemyRarity.Magic => 2d,
            EnemyAI.EnemyRarity.Rare => 3d,
            EnemyAI.EnemyRarity.Legendary => 8d,
            _ => 1d,
        };
    }
}
