// Developer map: Player mana resource backed by Mana/ManaPercent/ManaRegeneration stats.
using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
public sealed class ManaComponent : MonoBehaviour
{
    private StatsComponent stats;
    public float CurrentMana { get; private set; }
    public float MaxMana
    {
        get
        {
            // Editor-created actors may query the resource before Awake has run;
            // the attached stat store remains the sole resource authority.
            if (stats == null) stats = GetComponent<StatsComponent>();
            if (stats == null) return 0f;
            float value = Mathf.Max(0f, (stats.GetStat(StatTypes.Mana) + DerivedStatCalculator.AddedMana(stats))
                * (1f + stats.GetStat(StatTypes.ManaPercent) + DerivedStatCalculator.IntelligenceIncreased(stats)));
            var keystones = GetComponent<PassiveKeystoneState>();
            return value * (keystones != null ? keystones.MaximumManaMultiplier : 1f);
        }
    }
    public event System.Action ManaChanged;

    private void Awake()
    {
        stats = GetComponent<StatsComponent>();
        CurrentMana = MaxMana;
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<StatsComponent>();
        stats.StatsChanged += SyncMaximum;
        SyncMaximum();
    }

    private void OnDisable()
    {
        if (stats != null) stats.StatsChanged -= SyncMaximum;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || stats == null || SkillTreeUI.PausesGameplay) return;
        var keystones = GetComponent<PassiveKeystoneState>();
        float multiplier = keystones != null ? keystones.ManaRegenerationMultiplier : 1f;
        Restore(stats.GetStat(StatTypes.ManaRegeneration) * multiplier * Time.deltaTime);
    }

    public bool CanSpend(float amount) => amount >= 0f && CurrentMana + .0001f >= amount;

    public bool TrySpend(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (!CanSpend(amount)) return false;
        CurrentMana = Mathf.Max(0f, CurrentMana - amount);
        ManaChanged?.Invoke();
        return true;
    }

    public float SpendUpTo(float amount)
    {
        float spent = Mathf.Min(CurrentMana, Mathf.Max(0f, amount));
        if (spent <= 0f) return 0f;
        CurrentMana -= spent;
        ManaChanged?.Invoke();
        return spent;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount)) return;
        float next = Mathf.Min(MaxMana, CurrentMana + amount);
        if (Mathf.Approximately(next, CurrentMana)) return;
        CurrentMana = next;
        ManaChanged?.Invoke();
    }

    public void RestoreFull()
    {
        CurrentMana = MaxMana;
        ManaChanged?.Invoke();
    }

    public bool RestoreCheckpointMana(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > MaxMana + .001f) return false;
        CurrentMana = Mathf.Clamp(value, 0f, MaxMana);
        ManaChanged?.Invoke();
        return true;
    }

    private void SyncMaximum()
    {
        float next = Mathf.Min(CurrentMana, MaxMana);
        if (Mathf.Approximately(next, CurrentMana)) return;
        CurrentMana = next;
        ManaChanged?.Invoke();
    }
}
