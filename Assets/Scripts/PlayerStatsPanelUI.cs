using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerStatsPanelUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StatsComponent playerStats;

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private StatHeaderUI headerPrefab;
    [SerializeField] private StatRowUI rowPrefab;

    [Header("Behavior")]
    [Tooltip("If > 0, refreshes repeatedly. If 0, only refreshes on enable / manual calls.")]
    [SerializeField] private float autoRefreshInterval = 0f;

    private float _timer;

    private readonly List<GameObject> _spawned = new();

    private void OnEnable()
    {
        Refresh();
        _timer = 0f;
    }

    private void Update()
    {
        if (autoRefreshInterval <= 0f) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autoRefreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public void SetTarget(StatsComponent target)
    {
        playerStats = target;
        Refresh();
    }

    public void Refresh()
    {
        if (playerStats == null || contentRoot == null || headerPrefab == null || rowPrefab == null)
            return;

        Clear();

        // Only display stats that the player currently has => tracked keys
        var tracked = playerStats.GetTrackedStats().OrderBy(t => (int)t).ToList();

        // Only display stats with non-zero values
        tracked = tracked.Where(t => StatDisplayFormatting.ShouldDisplay(playerStats, t)).ToList();

        if (tracked.Count == 0)
        {
            // Show a single header that says "No Stats"
            var h = Instantiate(headerPrefab, contentRoot);
            h.SetText("Stats");
            _spawned.Add(h.gameObject);

            var r = Instantiate(rowPrefab, contentRoot);
            r.Set("No active stats", "");
            _spawned.Add(r.gameObject);
            return;
        }

        StatCategory? currentCat = null;

        foreach (var statType in tracked)
        {
            var cat = StatCategoryMapping.GetCategory(statType);

            if (currentCat == null || currentCat.Value != cat)
            {
                currentCat = cat;

                var header = Instantiate(headerPrefab, contentRoot);
                header.SetText(StatCategoryMapping.GetHeaderName(cat));
                _spawned.Add(header.gameObject);
            }

            var row = Instantiate(rowPrefab, contentRoot);
            string name = StatDisplayFormatting.ToFriendlyName(statType);
            string value = StatDisplayFormatting.FormatValue(playerStats, statType);
            row.Set(name, value);
            _spawned.Add(row.gameObject);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }
}
