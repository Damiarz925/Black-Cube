// Developer map: Session XP and the 366-node six-sector Passive Tree V2 on GameManager.
// Allocations replace stat modifiers by this component as source; encounter restart retains them.
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Session progression lives with GameManager; encounter restart never resets it.</summary>
public sealed class PlayerProgression : MonoBehaviour
{
    [Header("First-run combat XP pacing")]
    [SerializeField, Min(1)] int maxLevel = 100;
    [SerializeField, Min(1)] float maxEnemiesFrom99To100 = 45;
    [SerializeField, Min(1)] float bossXpMultiplier = 5f;
    [SerializeField] int level = 1;
    [SerializeField] double experience;
    [SerializeField] int availablePoints = 1;
    [SerializeField] int[] ranks = new int[PassiveTreeDefinition.NodeCount];

    const double ReqAtLevel10Xp = 300d;
    const double ReqAtLevel16Xp = 619d;
    const int ReqAnchorLevel10 = 10;
    const int ReqAnchorLevel16 = 16;
    const int TopTransitionLevel = 99;

    StatsComponent boundStats;
    PassiveKeystoneState boundKeystones;
    PlayerController boundPlayer;
    PlayerIdentityState identity;
    public event Action Changed;
    public int Level => level;
    public int AvailablePoints => availablePoints;
    public double Experience => experience;
    public bool AtCap => level >= Mathf.Max(1, maxLevel);
    public double RequiredXp => RequirementAt(level);
    public double RequirementAt(int atLevel) => atLevel < maxLevel ? Math.Max(1d, Math.Round(ReqAtLevel10Xp * Math.Pow(GetRequirementGrowth(), atLevel - ReqAnchorLevel10))) : 0d;
    public double EnemyReward(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => RequirementAt(combatLevel) / KillsToLevelUp(combatLevel) * EnemyXpMultiplier(rarity, wasBoss);
    // Level-owned Life grows before equipment/passive percentage multipliers.
    // The gentler early rate keeps first-run midgame TTD credible; the late
    // rate supports the enemy's authored pre-100 growth. Level 1 remains 1000.
    public static float LevelLifeBonus(int atLevel)
    {
        int level=Mathf.Clamp(atLevel,1,100);
        int early=Mathf.Min(level-1,49);
        int late=Mathf.Max(0,level-50);
        return 1000f*((float)(Math.Pow(1.012d,early)*Math.Pow(1.025d,late))-1f);
    }

    private void Awake() => NormalizeAllocations();
    private void OnEnable() => NormalizeAllocations();

    public int Rank(int node) => ranks != null && node >= 0 && node < ranks.Length && ranks[node] != 0 ? 1 : 0;
    public int[] CopyRanks() => ranks != null ? (int[])ranks.Clone() : new int[PassiveTreeDefinition.NodeCount];
    public bool IsAllocated(int node) => Rank(node) > 0;
    public bool HasKeystone(PassiveKeystone keystone)
    {
        if (keystone == PassiveKeystone.None) return false;
        foreach(var node in PassiveTreeDefinition.Nodes)
            if(node.Keystone==keystone&&IsAllocated(node.Id))return true;
        return false;
    }
    public string ActiveClassId=>(identity??=GetComponent<PlayerIdentityState>())?.BaseClassId??PlayerClassIds.Warrior;
    public int ActiveStartNodeId=>PassiveTreeDefinition.StartNodeId(ActiveClassId);
    public bool CanSpend(int node)
    {
        if (node < 0 || node >= PassiveTreeDefinition.NodeCount || PassiveTreeDefinition.IsClassStart(node)
            || availablePoints < PassiveTreeDefinition.PointCost || IsAllocated(node))
            return false;
        if (PassiveTreeDefinition.IsRootConnected(node,ActiveClassId)) return true;
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
        GamePersistence.MarkDirty();
        return true;
    }

    public bool CanRefund(int node)
    {
        if (!IsAllocated(node)) return false;
        var reachable = new bool[PassiveTreeDefinition.NodeCount];
        var queue = new Queue<int>();
        for (int id = 0; id < PassiveTreeDefinition.NodeCount; id++)
        {
            if (id != node && IsAllocated(id) && PassiveTreeDefinition.IsRootConnected(id,ActiveClassId))
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
        GamePersistence.MarkDirty();
        return true;
    }

    public void RefundAll()
    {
        int refunded=0;for(int i=0;i<ranks.Length;i++)if(ranks[i]!=0){ranks[i]=0;refunded++;}
        availablePoints+=refunded;BindStats();ApplySkills();Changed?.Invoke();GamePersistence.MarkDirty();
    }

    public int AllocatedCount(PassiveBranch branch)
    {
        int count = 0;
        foreach(var node in PassiveTreeDefinition.Nodes)if(node.Branch==branch)count+=Rank(node.Id);
        return count;
    }

    /// <summary>Returns raw percentage points contributed by allocated nodes.</summary>
    public float GetBonus(PassiveBranch branch)
    {
        float total = 0f;
        foreach(var node in PassiveTreeDefinition.Nodes)if(node.Branch==branch&&IsAllocated(node.Id))total+=node.Magnitude;
        return total;
    }

    public void AwardEnemy(int combatLevel, EnemyAI.EnemyRarity rarity, bool wasBoss) => AddExperience(EnemyReward(combatLevel, rarity, wasBoss));
    public void AddExperience(double amount)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0 || AtCap) return;
        experience += amount * (RelicInventory.Instance != null ? RelicInventory.Instance.ExperienceMultiplier : 1f);
        var player = FindFirstObjectByType<PlayerController>();
        var health = player != null ? player.GetComponent<HealthComponent>() : null;
        bool leveled=false;
        while (!AtCap && experience >= RequiredXp)
        {
            experience -= RequiredXp;
            level++;
            availablePoints++;
            leveled=true;
            Debug.Log($"Player level {level}: +1 passive point, full HP.");
        }
        if(leveled)
        {
            BindStats();ApplySkills();
            if(health!=null)health.RestoreFullLife();
        }
        if (AtCap) experience = 0;
        Changed?.Invoke();
        GamePersistence.MarkDirty();
    }

    public void ResetProgression()
    {
        level = 1;
        experience = 0;
        availablePoints = 1;
        ranks = new int[PassiveTreeDefinition.NodeCount];
        BindStats();
        ApplySkills();
        Changed?.Invoke();
    }

    public bool RestoreProgression(int restoredLevel, double restoredExperience, int restoredAvailablePoints, int[] restoredRanks)
    {
        if (restoredLevel < 1 || restoredLevel > Mathf.Max(1, maxLevel) || double.IsNaN(restoredExperience)
            || double.IsInfinity(restoredExperience) || restoredExperience < 0d || restoredAvailablePoints < 0
            || restoredRanks == null || restoredRanks.Length != PassiveTreeDefinition.NodeCount) return false;
        if (restoredLevel < maxLevel && restoredExperience >= RequirementAt(restoredLevel)) return false;
        if (restoredLevel >= maxLevel && restoredExperience != 0d) return false;
        int allocated = 0;
        for (int i = 0; i < restoredRanks.Length; i++)
        {
            if (restoredRanks[i] is not (0 or 1)) return false;
            allocated += restoredRanks[i];
        }
        if (allocated + restoredAvailablePoints != restoredLevel) return false;
        level = restoredLevel;
        experience = restoredExperience;
        availablePoints = restoredAvailablePoints;
        ranks = (int[])restoredRanks.Clone();
        BindStats();
        ApplySkills();
        Changed?.Invoke();
        return true;
    }

    void Update() => BindStats();

    void BindStats()
    {
        if (boundStats != null) return;
        boundPlayer = FindFirstObjectByType<PlayerController>();
        if (boundPlayer == null) return;
        boundStats = boundPlayer.GetComponent<StatsComponent>();
        boundKeystones = boundPlayer.GetComponent<PassiveKeystoneState>();
        if (boundKeystones == null) boundKeystones = boundPlayer.gameObject.AddComponent<PassiveKeystoneState>();
        boundPlayer.AttackChanged-=OnWeaponChanged;boundPlayer.AttackChanged+=OnWeaponChanged;
        ApplySkills();
    }

    void ApplySkills()
    {
        if (boundStats == null) return;
        boundStats.BeginUpdate();
        try
        {
            boundStats.RemoveModifiersFromSource(this);
            float levelLife=LevelLifeBonus(level);
            if(levelLife>0f)boundStats.AddModifier(new StatModifier(StatTypes.Life,StatOp.Flat,levelLife,this));
            string equipped=boundPlayer?.EquippedWeapon?.WeaponTypeId;
            foreach(var node in PassiveTreeDefinition.Nodes)
            {
                if(!IsAllocated(node.Id))continue;
                if(!string.IsNullOrEmpty(node.WeaponTypeRestriction)&&node.WeaponTypeRestriction!=equipped)continue;
                foreach(var effect in node.Effects)boundStats.AddModifier(new StatModifier(effect.Stat,StatOp.Flat,effect.Amount,this));
            }
            boundKeystones?.Apply(this);
        }
        finally { boundStats.EndUpdate(); }
    }

    public void ReleaseSceneReferences()
    {
        if(boundPlayer!=null)boundPlayer.AttackChanged-=OnWeaponChanged;
        boundKeystones?.Apply(null);
        if (boundStats != null) boundStats.RemoveModifiersFromSource(this);
        boundStats = null;
        boundKeystones = null;
        boundPlayer = null;
    }

    void OnWeaponChanged(){if(boundStats==null)return;ApplySkills();Changed?.Invoke();}

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
        // V1 node identities are intentionally not mapped to V2. Persistence
        // refunds them from player level during schema migration.
    }

    void OnDestroy()
    {
        ReleaseSceneReferences();
    }

    double GetRequirementGrowth() => Math.Pow(ReqAtLevel16Xp / ReqAtLevel10Xp, 1d / (ReqAnchorLevel16 - ReqAnchorLevel10));

    double KillsToLevelUp(int atLevel)
    {
        // Normal-enemy XP equivalents, interpolated against the combat-time
        // checkpoints instead of an exponential late-game kill wall.
        int[] levels = { 1, 10, 25, 50, 75, TopTransitionLevel };
        double[] kills = { 8d, 10d, 16d, 22d, 32d,
            Math.Max(1d, maxEnemiesFrom99To100) };
        if (atLevel <= levels[0]) return kills[0];
        for (int i = 1; i < levels.Length; i++)
            if (atLevel <= levels[i])
                return Math.Max(1d, Math.Round(kills[i - 1] +
                    (kills[i] - kills[i - 1]) * (atLevel - levels[i - 1]) /
                    (levels[i] - levels[i - 1])));
        return kills[kills.Length - 1];
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
