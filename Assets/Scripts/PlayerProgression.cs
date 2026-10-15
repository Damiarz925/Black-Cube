// Developer map: Session XP, level cap and six skill ranks on GameManager. Skills replace modifiers by this component as source; death restart keeps XP while StartNewRun resets it.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
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
    [SerializeField] int[] ranks = new int[6];

    const double ReqAtLevel10Xp = 300d;
    const double ReqAtLevel16Xp = 619d;
    const int ReqAnchorLevel10 = 10;
    const int ReqAnchorLevel16 = 16;
    const int TopTransitionLevel = 99;

    StatsComponent boundStats;
    public event Action Changed;
    public int Level => level;
    public int AvailablePoints => availablePoints;
    public double Experience => experience;
    public bool AtCap => level >= Mathf.Max(1, maxLevel);
    public double RequiredXp => RequirementAt(level);
    public double RequirementAt(int atLevel) => atLevel < maxLevel ? Math.Max(1d, Math.Round(ReqAtLevel10Xp * Math.Pow(GetRequirementGrowth(), atLevel - ReqAnchorLevel10))) : 0d;
    public double EnemyReward(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => RequirementAt(combatLevel) / KillsToLevelUp(combatLevel) * EnemyXpMultiplier(rarity, wasBoss);

    public static readonly string[] NodeNames = { "Vitality", "Force", "Tempo", "Endurance", "Might", "Momentum" };
    public static readonly string[] NodeBonuses = { "+100 maximum HP", "+15% increased damage", "+5% attack speed", "+150 maximum HP", "+20% increased damage", "+8% attack speed" };
    public int Rank(int node) => node >= 0 && node < ranks.Length ? ranks[node] : 0;
    public bool CanSpend(int node) => node >= 0 && node < 6 && availablePoints > 0 && Rank(node) < 5 && (node < 3 || Rank(node - 3) > 0);
    public bool TrySpend(int node)
    {
        if (!CanSpend(node)) return false;
        ranks[node]++; availablePoints--;
        BindStats(); ApplySkills(); Changed?.Invoke();
        return true;
    }
    public void AwardEnemy(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => AddExperience(EnemyReward(combatLevel, rarity, wasBoss));
    public void AddExperience(double amount)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0 || AtCap) return;
        experience += amount;
        var player = FindFirstObjectByType<PlayerController>();
        var health = player != null ? player.GetComponent<HealthComponent>() : null;
        while (!AtCap && experience >= RequiredXp)
        {
            experience -= RequiredXp;
            level++; availablePoints++;
            // Healing cannot resurrect an actor whose death menu has already opened.
            if (health != null) health.RestoreFullLife();
            Debug.Log($"Player level {level}: +1 skill point, full HP.");
        }
        if (AtCap) experience = 0;
        Changed?.Invoke();
    }
    public void ResetProgression()
    {
        level = 1; experience = 0; availablePoints = 0; ranks = new int[6];
        BindStats(); ApplySkills(); Changed?.Invoke();
    }
    void Update() { BindStats(); }
    void BindStats()
    {
        if (boundStats != null) return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        boundStats = player.GetComponent<StatsComponent>();
        ApplySkills();
    }
    void ApplySkills()
    {
        if (boundStats == null) return;
        boundStats.BeginUpdate();
        try
        {
            boundStats.RemoveModifiersFromSource(this);
            boundStats.AddModifier(new StatModifier(StatTypes.Life, StatOp.Flat, Rank(0)*100 + Rank(3)*150, this));
            boundStats.AddModifier(new StatModifier(StatTypes.GenericDmg, StatOp.Flat, Rank(1)*15 + Rank(4)*20, this));
            boundStats.AddModifier(new StatModifier(StatTypes.AttackSpeed, StatOp.Flat, Rank(2)*5 + Rank(5)*8, this));
        }
        finally { boundStats.EndUpdate(); }
    }
    void OnDestroy() { if (boundStats != null) boundStats.RemoveModifiersFromSource(this); }

    private double GetRequirementGrowth()
    {
        return Math.Pow(ReqAtLevel16Xp / ReqAtLevel10Xp, 1d / (ReqAnchorLevel16 - ReqAnchorLevel10));
    }

    private double KillsToLevelUp(int atLevel)
    {
        if (atLevel <= 1) return 1d;
        if (atLevel >= TopTransitionLevel) return Math.Max(1d, maxEnemiesFrom99To100);

        double min = 1d;
        double max = Math.Max(1d, maxEnemiesFrom99To100);
        double progress = (atLevel - 1d) / (TopTransitionLevel - 1d);
        return Math.Round(Math.Pow(max / min, progress));
    }

    private double EnemyXpMultiplier(EnemyAI.EnemyRarity rarity, bool wasBoss)
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
