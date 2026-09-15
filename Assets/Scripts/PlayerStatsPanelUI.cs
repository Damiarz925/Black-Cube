// Developer map: Rebuilds categorized stat rows on StatsChanged/AttackChanged while retaining collapsed sections. Attack previews use the noncritical context so inspecting stats does not consume random rolls.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private StatsComponent subscribedStats;
    private PlayerController subscribedPlayer;
    private readonly HashSet<string> collapsed = new();
    private readonly Dictionary<string, Button> sectionButtons = new();
    public bool IsSectionExpanded(string section) => !collapsed.Contains(section);
    public Button SectionButton(string section) => sectionButtons.TryGetValue(section, out var button) ? button : null;

    private void OnEnable()
    {
        Bind();
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
        Unbind();
        playerStats = target;
        if (isActiveAndEnabled) Bind();
        Refresh();
    }

    private void Bind()
    {
        Unbind();
        subscribedStats = playerStats;
        subscribedPlayer = playerStats != null ? playerStats.GetComponent<PlayerController>() : null;
        if (subscribedStats != null) subscribedStats.StatsChanged += Refresh;
        if (subscribedPlayer != null) subscribedPlayer.AttackChanged += Refresh;
    }
    private void Unbind()
    {
        if (subscribedStats != null) subscribedStats.StatsChanged -= Refresh;
        if (subscribedPlayer != null) subscribedPlayer.AttackChanged -= Refresh;
        subscribedStats = null; subscribedPlayer = null;
    }
    private void OnDisable() { Unbind(); }

    public void Refresh()
    {
        if (contentRoot == null || headerPrefab == null || rowPrefab == null)
            return;

        Clear();
        if (playerStats == null)
        {
            AddHeader("Stats");
            var empty = Instantiate(rowPrefab, contentRoot);
            empty.Set("No current target", "");
            _spawned.Add(empty.gameObject);
            return;
        }

        var player = playerStats.GetComponent<PlayerController>();
        if (player != null)
        {
            var header = Instantiate(headerPrefab, contentRoot);
            header.SetText("Basic Attack / Before Defenses"); _spawned.Add(header.gameObject);
            var lowContext = player.BuildNonCriticalAttackContextAtRangeEnd(false);
            var highContext = player.BuildNonCriticalAttackContextAtRangeEnd(true);
            for (int i = 0; i < lowContext.Hits.Count; i++)
            {
                var hit = lowContext.Hits[i];
                if (hit.Amount <= 0f) continue;
                var damage = Instantiate(rowPrefab, contentRoot);
                float high = i < highContext.Hits.Count ? highContext.Hits[i].Amount : hit.Amount;
                damage.Set($"{ItemTooltipUI.ElementName(hit.Element)} / Hit (Noncritical)",
                    $"{hit.Amount:0.##}-{high:0.##} (Average {(hit.Amount+high)*.5f:0.##})");
                _spawned.Add(damage.gameObject);
            }
            var crit = Instantiate(rowPrefab, contentRoot);
            crit.Set("Critical Chance (Final)", (player.GetFinalCritChance() * 100f).ToString("0.##") + "%");
            _spawned.Add(crit.gameObject);
            var speed = Instantiate(rowPrefab, contentRoot);
            speed.Set("Attacks / Second (Final)", player.GetFinalAttackSpeed().ToString("0.##"));
            _spawned.Add(speed.gameObject);
            if (player.EquippedWeaponBaseDamage != 0)
            {
                var weapon = Instantiate(rowPrefab, contentRoot);
                player.EquippedWeapon.GetEffectiveBaseDamageRange(out float low, out float high);
                weapon.Set($"Weapon Base {ItemTooltipUI.ElementName(player.EquippedWeaponElement)}",
                    $"{low:0.##}-{high:0.##} (Average {(low+high)*.5f:0.##})");
                _spawned.Add(weapon.gameObject);
            }
            if (StatDisplayFormatting.ShouldDisplay(playerStats, StatTypes.UnarmedDamage))
            {
                var unarmed = Instantiate(rowPrefab, contentRoot);
                unarmed.Set("Unarmed Damage", playerStats.GetStat(StatTypes.UnarmedDamage).ToString("0.##"));
                _spawned.Add(unarmed.gameObject);
            }
        }
        else
        {
            var enemy = playerStats.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                AddHeader("Basic Attack / Before Defenses");
                var lowContext = enemy.BuildNonCriticalAttackContextAtRangeEnd(false);
                var highContext = enemy.BuildNonCriticalAttackContextAtRangeEnd(true);
                for (int i = 0; i < lowContext.Hits.Count; i++)
                {
                    var hit = lowContext.Hits[i];
                    if (hit.Amount <= 0f) continue;
                    var damage = Instantiate(rowPrefab, contentRoot);
                    float high = i < highContext.Hits.Count ? highContext.Hits[i].Amount : hit.Amount;
                    damage.Set($"{ItemTooltipUI.ElementName(hit.Element)} / Hit (Noncritical)",
                        $"{hit.Amount:0.##}-{high:0.##} (Average {(hit.Amount+high)*.5f:0.##})");
                    _spawned.Add(damage.gameObject);
                }
                var crit = Instantiate(rowPrefab, contentRoot);
                crit.Set("Critical Chance (Final)", (enemy.GetFinalCritChance() * 100f).ToString("0.##") + "%");
                _spawned.Add(crit.gameObject);
                var speed = Instantiate(rowPrefab, contentRoot);
                speed.Set("Attacks / Second (Final)", enemy.GetFinalAttackSpeed().ToString("0.##"));
                _spawned.Add(speed.gameObject);
                if (enemy.EquippedWeaponBaseDamage != 0f)
                {
                    var weapon = Instantiate(rowPrefab, contentRoot);
                    enemy.EquippedWeapon.GetEffectiveBaseDamageRange(out float low, out float high);
                    weapon.Set($"Weapon Base {ItemTooltipUI.ElementName(enemy.WeaponMainElement)}",
                        $"{low:0.##}-{high:0.##} (Average {(low+high)*.5f:0.##})");
                    _spawned.Add(weapon.gameObject);
                }
            }
        }

        // Only display stats that the player currently has => tracked keys
        var tracked = playerStats.GetTrackedStats().OrderBy(t => (int)t).ToList();

        // Only display stats with non-zero values
        tracked = tracked.Where(t => StatDisplayFormatting.ShouldDisplay(playerStats, t)).ToList();
        if (player != null) tracked.RemoveAll(t => t == StatTypes.UnarmedDamage || t == StatTypes.WeaponBaseDmg);

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

        foreach (string section in new[] { "Damage", "Critical & Speed", "Status Effects", "Defenses", "Resources", "Attributes", "Other" })
        {
            var values = tracked.Where(t => Section(t) == section).ToList();
            if (values.Count == 0) continue;
            var h = AddHeader((IsSectionExpanded(section) ? "[-] " : "[+] ") + section);
            var background = h.GetComponent<Image>();
            if (background == null) background = h.gameObject.AddComponent<Image>();
            background.color = new Color(.10f,.12f,.15f,1); background.raycastTarget = true;
            foreach(var text in h.GetComponentsInChildren<TMP_Text>()) text.raycastTarget = false;
            var button = h.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            CorruptionUIButtonSkin.Ensure(button)?.SetSelected(IsSectionExpanded(section));
            sectionButtons[section] = button;
            button.onClick.AddListener(() =>
            {
                if (!collapsed.Add(section)) collapsed.Remove(section);
                Refresh();
            });
            if (!IsSectionExpanded(section)) continue;
            if (section == "Status Effects")
            {
                foreach (string sub in new[] { "Application Chances", "Damage / Increases & Multipliers", "Effect Strength", "Duration & Tick Rate", "Penetration", "Resistances", "Other Status Modifiers" })
                {
                    var subset = values.Where(t => StatusSubsection(t) == sub).ToList();
                    if (subset.Count == 0) continue;
                    AddHeader(sub);
                    foreach (var type in subset) AddStat(type);
                }
            }
            else foreach (var type in values) AddStat(type);
        }
    }

    private StatHeaderUI AddHeader(string title)
    { var h = Instantiate(headerPrefab, contentRoot); h.SetText(title); _spawned.Add(h.gameObject); return h; }
    private void AddStat(StatTypes type)
    {
        var row = Instantiate(rowPrefab, contentRoot);
        row.Set(StatDisplayFormatting.ToFriendlyName(type), StatDisplayFormatting.FormatValue(playerStats,type));
        _spawned.Add(row.gameObject);
    }
    private static bool IsStatusStat(StatTypes type) =>
        ((int)type >= (int)StatTypes.PoisonDmg && (int)type <= (int)StatTypes.ChillDuration)
        || ((int)type >= (int)StatTypes.PoisonRes && (int)type <= (int)StatTypes.AllAilmentRes)
        || type == StatTypes.GenericDotMult || type == StatTypes.DoTMultPerIntelligence
        || type == StatTypes.Plus1Poison || type == StatTypes.Plus1Bleed || type == StatTypes.Plus1Ignite;
    private static string Section(StatTypes type)
    {
        if (IsStatusStat(type)) return "Status Effects";
        if (type == StatTypes.ChanceToBlock) return "Defenses";
        return StatCategoryMapping.GetCategory(type) switch
        {
            StatCategory.WeaponBase or StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage or StatCategory.Penetration => "Damage",
            StatCategory.Utility => "Critical & Speed",
            StatCategory.Defenses => "Defenses",
            StatCategory.Resources => "Resources",
            StatCategory.Attributes => "Attributes",
            _ => "Other"
        };
    }
    private static string StatusSubsection(StatTypes type) => type switch
    {
        StatTypes.PoisonChance or StatTypes.BleedChance or StatTypes.IgniteChance or StatTypes.ChillChance or StatTypes.ShockChance => "Application Chances",
        StatTypes.PoisonDmg or StatTypes.IgniteDmg or StatTypes.BleedDmg or StatTypes.PoisonMult or StatTypes.IgniteMult or StatTypes.BleedMult or StatTypes.GenericDotMult or StatTypes.DoTMultPerIntelligence => "Damage / Increases & Multipliers",
        StatTypes.ShockEffect or StatTypes.ChillEffect => "Effect Strength",
        StatTypes.PoisonDuration or StatTypes.BleedDuration or StatTypes.IgniteDuration or StatTypes.ChillDuration or StatTypes.ShockDuration or StatTypes.PoisonTickRate or StatTypes.BleedTickRate or StatTypes.IgniteTickRate => "Duration & Tick Rate",
        StatTypes.PoisonPenetration or StatTypes.IgnitePenetration or StatTypes.BleedPenetration => "Penetration",
        StatTypes.PoisonRes or StatTypes.BleedRes or StatTypes.IgniteRes or StatTypes.ChillRes or StatTypes.ShockRes or StatTypes.AllAilmentRes => "Resistances",
        _ => "Other Status Modifiers"
    };

    private void Clear()
    {
        sectionButtons.Clear();
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                _spawned[i].SetActive(false);
                Destroy(_spawned[i]);
            }
        }
        _spawned.Clear();
    }
}
