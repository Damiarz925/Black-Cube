// Developer map: Session XP and Passive Tree V3 route allocation on GameManager.
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
        if(keystone==PassiveKeystone.RageFinisher)return IsAllocated(PassiveTreeDefinition.RageFinisherNodeId);
        foreach(var node in PassiveTreeDefinition.Nodes)
            if(node.Keystone==keystone&&IsAllocated(node.Id))return true;
        return false;
    }
    public string ActiveClassId=>(identity??=GetComponent<PlayerIdentityState>())?.BaseClassId??PlayerClassIds.Warrior;
    public int ActiveStartNodeId=>PassiveTreeDefinition.StartNodeId(ActiveClassId);
    public bool NativeSpineComplete=>PassiveTreeDefinition.IsClassSpineComplete(ActiveClassId,IsAllocated);
    public bool CanSpend(int node)
    {
        if(node<0||node>=PassiveTreeDefinition.NodeCount||availablePoints<1||IsAllocated(node))return false;
        var n=PassiveTreeDefinition.Node(node);
        if(n.IsSubclassChoice){var state=identity??=GetComponent<PlayerIdentityState>();if(n.RouteClassId!=ActiveClassId||state?.SubclassChoiceUnlocked!=true||string.IsNullOrEmpty(state.SelectedSubclassId))return false;}
        if(n.IsChoice){if(!IsAllocated(n.PrerequisiteId))return false;foreach(int sibling in PassiveTreeDefinition.ChoiceNodes(n.ChoiceGroupId))if(IsAllocated(sibling))return false;return true;}
        if(n.Kind==PassiveNodeKind.Spine){if(n.Tier==1)return n.RouteClassId==ActiveClassId||NativeSpineComplete;return IsAllocated(PassiveTreeDefinition.ClassSpineNode(n.RouteClassId,n.Tier-1));}
        if(n.Kind==PassiveNodeKind.WeaponSpine){if(n.Tier==1)return PassiveTreeDefinition.IsClassSpineComplete(PassiveTreeDefinition.WeaponClass(n.RouteWeaponId),IsAllocated);return IsAllocated(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,n.Tier-1));}
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
        var n=PassiveTreeDefinition.Node(node);if(n.IsChoice)return true;
        if(n.Kind==PassiveNodeKind.Spine)
        {
            for(int t=n.Tier+1;t<=10;t++)if(IsAllocated(PassiveTreeDefinition.ClassSpineNode(n.RouteClassId,t)))return false;
            foreach(int id in PassiveTreeDefinition.RouteNodes(n.RouteClassId))if(id!=node&&IsAllocated(id)&&PassiveTreeDefinition.Node(id).Tier>=n.Tier)return false;
            string weapon=PassiveTreeDefinition.SignatureWeapon(n.RouteClassId);foreach(int id in PassiveTreeDefinition.RouteNodes(null,weapon))if(IsAllocated(id))return false;
            if(n.RouteClassId==ActiveClassId)foreach(var other in PassiveTreeDefinition.ClassIds)if(other!=ActiveClassId)foreach(int id in PassiveTreeDefinition.RouteNodes(other))if(IsAllocated(id))return false;
            return true;
        }
        if(n.Kind==PassiveNodeKind.WeaponSpine){for(int t=n.Tier+1;t<=5;t++)if(IsAllocated(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,t)))return false;foreach(int id in PassiveTreeDefinition.RouteNodes(null,n.RouteWeaponId))if(id!=node&&IsAllocated(id)&&PassiveTreeDefinition.Node(id).Tier>=n.Tier)return false;return true;}
        return false;
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
        if(!ValidateAllocationState(restoredRanks,ActiveClassId,(identity??=GetComponent<PlayerIdentityState>())?.SelectedSubclassId))return false;
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
        identity??=GetComponent<PlayerIdentityState>();if(identity!=null){identity.Changed-=OnIdentityChanged;identity.Changed+=OnIdentityChanged;}
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
                var effects=node.IsSubclassChoice&&identity!=null?PassiveTreeDefinition.SubclassEffects(identity.SelectedSubclassId,node):node.Effects;
                foreach(var effect in effects)boundStats.AddModifier(new StatModifier(effect.Stat,StatOp.Flat,effect.Amount,this));
            }
            ApplySubclassCoreModifiers();
            boundKeystones?.Apply(this);
        }
        finally { boundStats.EndUpdate(); }
    }

    void ApplySubclassCoreModifiers()
    {
        string subclass=(identity??=GetComponent<PlayerIdentityState>())?.SelectedSubclassId;
        SubclassStatPackage.Apply(subclass,(stat,amount)=>boundStats.AddModifier(new StatModifier(stat,StatOp.Flat,amount,this)));
    }

    public void ReleaseSceneReferences()
    {
        if(boundPlayer!=null)boundPlayer.AttackChanged-=OnWeaponChanged;
        if(identity!=null)identity.Changed-=OnIdentityChanged;
        boundKeystones?.Apply(null);
        if (boundStats != null) boundStats.RemoveModifiersFromSource(this);
        boundStats = null;
        boundKeystones = null;
        boundPlayer = null;
    }

    void OnWeaponChanged(){if(boundStats==null)return;ApplySkills();Changed?.Invoke();}
    void OnIdentityChanged(){if(boundStats==null)return;ApplySkills();Changed?.Invoke();}

    public int RefundSubclassChoiceNodes()
    {int count=0;for(int i=0;i<ranks.Length;i++)if(ranks[i]!=0&&PassiveTreeDefinition.Node(i).IsSubclassChoice){ranks[i]=0;availablePoints++;count++;}if(count>0){ApplySkills();Changed?.Invoke();GamePersistence.MarkDirty();}return count;}
    public static bool ValidateAllocationState(int[] values,string homeClass,string selectedSubclass)
    {
        if(values==null||values.Length!=PassiveTreeDefinition.NodeCount||!PlayerClassCatalog.IsValid(homeClass))return false;
        bool A(int id)=>id>=0&&id<values.Length&&values[id]!=0;bool nativeComplete=PassiveTreeDefinition.IsClassSpineComplete(homeClass,A);
        foreach(var n in PassiveTreeDefinition.Nodes)if(A(n.Id))
        {
            if(values[n.Id]!=1)return false;
            if(n.IsChoice){if(!A(n.PrerequisiteId)||n.IsSubclassChoice&&(n.RouteClassId!=homeClass||string.IsNullOrEmpty(selectedSubclass)))return false;int chosen=0;foreach(int id in PassiveTreeDefinition.ChoiceNodes(n.ChoiceGroupId))if(A(id))chosen++;if(chosen>1)return false;}
            else if(n.Kind==PassiveNodeKind.Spine){if(n.Tier==1){if(n.RouteClassId!=homeClass&&!nativeComplete)return false;}else if(!A(PassiveTreeDefinition.ClassSpineNode(n.RouteClassId,n.Tier-1)))return false;}
            else if(n.Kind==PassiveNodeKind.WeaponSpine){if(n.Tier==1){if(!PassiveTreeDefinition.IsClassSpineComplete(PassiveTreeDefinition.WeaponClass(n.RouteWeaponId),A))return false;}else if(!A(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,n.Tier-1)))return false;}
            else return false;
        }
        return true;
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
        // Historical node identities are intentionally not mapped to V3. Persistence
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
