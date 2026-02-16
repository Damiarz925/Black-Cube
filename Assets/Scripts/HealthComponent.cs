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
            GameManager.Instance.OnEnemyKilled(this, isBoss);
        }
        else
        {
            GameManager.Instance.OnPlayerKilled(this);
        }

        // TODO: play death anim / vfx
        Destroy(gameObject, 0.5f);
    }
}
