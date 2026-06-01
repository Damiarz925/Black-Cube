using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private float maxLife = 100f;
    public float CurrentLife { get; private set; }

    [Header("Flags")]
    [SerializeField] private bool isEnemy;
    [SerializeField] private bool isBoss;

    private bool isDead = false;

    private void Awake()
    {
        CurrentLife = maxLife;
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

        if (isEnemy)
        {
            Debug.Log($"HealthComponent: Enemy died. name={name}, isBoss={isBoss}", this);
            GameManager.Instance.OnEnemyKilled(this, isBoss);
        }
        else
        {
            Debug.Log($"HealthComponent: Player died. name={name}", this);
            GameManager.Instance.OnPlayerKilled(this);
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
        CurrentLife = maxLife;
        Debug.Log($"HealthComponent: ReviveToFullLife called for {name}. CurrentLife={CurrentLife}", this);
    }
}
