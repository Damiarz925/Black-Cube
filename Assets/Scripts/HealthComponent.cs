// Developer map: Owns life, death and single-claim enemy rewards; death can synchronously cause a replacement encounter. Prefab maxLife seeds enemy Life, while final maximums come from actor stats.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float maxLife = 100f;
    public float CurrentLife { get; private set; }

    [Header("Flags")]
    [SerializeField] private bool isEnemy;
    [SerializeField] private bool isBoss;

    private bool isDead = false;
    public event System.Action Changed;
    private bool deathRewardClaimed;
    public bool IsBoss => isBoss;
    public void SetEnemyRole(bool boss) { isEnemy = true; isBoss = boss; }
    public bool TryClaimEnemyDeath()
    {
        if (!isEnemy || !isDead || deathRewardClaimed) return false;
        deathRewardClaimed = true;
        return true;
    }
    private StatsComponent maxLifeStats;
    private StatsComponent healingStats;
    internal float PrefabMaxLife => Mathf.Max(0f, maxLife);
    public float MaxLife
    {
        get
        {
            float value = maxLifeStats != null
                ? Mathf.Max(0f, maxLifeStats.GetStat(StatTypes.Life) * (1f + maxLifeStats.GetStat(StatTypes.LifePercent)))
                : PrefabMaxLife;
            var keystones = GetComponent<PassiveKeystoneState>();
            return value * (keystones != null ? keystones.MaximumLifeMultiplier : 1f);
        }
    }

    private void Awake()
    {
        healingStats = GetComponent<StatsComponent>();
        // EnemyStatSetup opts enemies in after seeding Life from the prefab baseline.
        if (!isEnemy && GetComponent<PlayerStatSetup>() != null)
            maxLifeStats = healingStats;
        CurrentLife = MaxLife;
        if (maxLifeStats != null) Debug.Log($"Player health initialized: {CurrentLife}/{MaxLife}", this);
    }

    private void OnEnable()
    {
        if (healingStats != null) healingStats.StatsChanged += SyncMaximum;
        SyncMaximum();
    }
    private void OnDisable() { if (healingStats != null) healingStats.StatsChanged -= SyncMaximum; }
    private void Update()
    {
        if (healingStats == null || isDead || SkillTreeUI.PausesGameplay)
            return;

        // Life regeneration is stored and consumed as flat life per second.
        float regenerationRate = healingStats.GetStat(StatTypes.LifeRegeneration);
        var keystones = GetComponent<PassiveKeystoneState>();
        if (keystones != null) regenerationRate *= keystones.LifeRegenerationMultiplier;
        if (regenerationRate > 0f)
            RestoreLife(regenerationRate * Time.deltaTime);
    }
    private void SyncMaximum()
    {
        CurrentLife = Mathf.Min(CurrentLife, MaxLife);
        Changed?.Invoke();
        if (CurrentLife <= 0f && !isDead) Die();
    }

    internal void UseStatsForMaximumLife(StatsComponent source)
    {
        if (source == null)
            return;

        maxLifeStats = source;
        SyncMaximum();
    }

    public void LoseLife(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        CurrentLife -= amount;
        Changed?.Invoke();

        if (CurrentLife <= 0f)
        {
            CurrentLife = 0f;
            Die();
        }
    }

    public void RestoreLife(float amount)
    {
        if (isDead || amount <= 0f || float.IsNaN(amount))
            return;

        CurrentLife = Mathf.Min(MaxLife, CurrentLife + amount);
        Changed?.Invoke();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        GetComponent<StatusController>()?.ClearStatuses();

        if (isEnemy)
        {
            Debug.Log($"HealthComponent: Enemy died. name={name}, isBoss={isBoss}", this);
            if (GameManager.Instance != null)
                GameManager.Instance.OnEnemyKilled(this, isBoss);
            else
                Debug.LogWarning("HealthComponent: GameManager.Instance is null; enemy death could not advance the run.", this);
        }
        else
        {
            Debug.Log($"HealthComponent: Player died. name={name}", this);
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerKilled(this);
            else
                Debug.LogWarning("HealthComponent: GameManager.Instance is null; player death could not show death flow.", this);
        }

        // TODO: play death anim / vfx
        if (isEnemy)
        {
            Destroy(gameObject, 0.5f);
        }
    }

    public void ReviveToFullLife()
    {
        isDead = false;
        CurrentLife = MaxLife;
        Changed?.Invoke();
        if (CurrentLife <= 0f) Die();
        Debug.Log($"HealthComponent: ReviveToFullLife called for {name}. CurrentLife={CurrentLife}", this);
    }
    public void RestoreFullLife()
    {
        if (!isDead) { CurrentLife = MaxLife; Changed?.Invoke(); }
    }
}
