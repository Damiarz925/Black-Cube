using System;
using UnityEngine;

/// <summary>Session progression lives with GameManager; encounter restart never resets it.</summary>
public sealed class PlayerProgression : MonoBehaviour
{
    [Header("Forgiving prototype XP (rebalance here)")]
    [SerializeField, Min(1)] int maxLevel = 100;
    [SerializeField, Min(1)] float enemyXpPerCombatLevel = 10;
    [SerializeField, Min(1)] float bossXpMultiplier = 2;
    [SerializeField, Min(1)] float firstLevelXp = 30;
    [SerializeField, Min(0)] float additionalXpPerLevel = 2;
    [SerializeField] int level = 1;
    [SerializeField] double experience;
    [SerializeField] int availablePoints;
    [SerializeField] int[] ranks = new int[6];
    StatsComponent boundStats;
    public event Action Changed;
    public int Level => level;
    public int AvailablePoints => availablePoints;
    public double Experience => experience;
    public bool AtCap => level >= Mathf.Max(1, maxLevel);
    public double RequiredXp => RequirementAt(level);
    public double RequirementAt(int atLevel) => Math.Max(1, firstLevelXp + Math.Max(0, atLevel - 1) * (double)Mathf.Max(0, additionalXpPerLevel));
    public double EnemyReward(int combatLevel, bool boss) => Mathf.Max(1, enemyXpPerCombatLevel) * (double)Mathf.Max(1, combatLevel) * (boss ? Mathf.Max(1, bossXpMultiplier) : 1);
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
    public void AwardEnemy(int combatLevel, bool boss) => AddExperience(EnemyReward(combatLevel, boss));
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
}
