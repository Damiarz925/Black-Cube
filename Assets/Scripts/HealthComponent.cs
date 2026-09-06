using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float maxLife = 100f;
    public float CurrentLife { get; private set; }

    [Header("Flags")]
    [SerializeField] private bool isEnemy;
    [SerializeField] private bool isBoss;

    private bool isDead = false;
    private StatsComponent playerStats;
    public float MaxLife => playerStats != null ? Mathf.Max(0f, playerStats.GetStat(StatTypes.Life)) : maxLife;

    private void Awake()
    {
        // Only players with PlayerStatSetup use derived Life. Enemy prefab health is unchanged.
        if (!isEnemy && GetComponent<PlayerStatSetup>() != null)
            playerStats = GetComponent<StatsComponent>();
        CurrentLife = MaxLife;
        if (playerStats != null) Debug.Log($"Player health initialized: {CurrentLife}/{MaxLife}", this);
    }

    private void OnEnable()
    {
        if (playerStats != null) playerStats.StatsChanged += SyncMaximum;
        SyncMaximum();
    }
    private void OnDisable() { if (playerStats != null) playerStats.StatsChanged -= SyncMaximum; }
    private void SyncMaximum()
    {
        CurrentLife = Mathf.Min(CurrentLife, MaxLife);
        if (CurrentLife <= 0f && !isDead) Die();
    }

    public void LoseLife(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        CurrentLife -= amount;

        if (CurrentLife <= 0f)
        {
            CurrentLife = 0f;
            Die();
        }
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
        if (CurrentLife <= 0f) Die();
        Debug.Log($"HealthComponent: ReviveToFullLife called for {name}. CurrentLife={CurrentLife}", this);
    }
}
