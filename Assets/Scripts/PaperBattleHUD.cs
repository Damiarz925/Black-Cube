// Developer map: Reads run/actor state, coordinates player and enemy inspection, and owns explicit pause/play controls. Runtime controls mirror the builder-authored HUD.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Scene-local presentation; reads the existing gameplay state.</summary>
public class PaperBattleHUD : MonoBehaviour
{
    public TMP_Text runText;
    public TMP_Text playerText;
    public TMP_Text enemyText;
    public HealthComponent player;
    public CombatHealthBar playerHealthBar;
    public CombatHealthBar enemyHealthBar;
    public GameObject inventoryPanel;
    public GameObject statsPanel;
    private EnemyInspectionPanelUI enemyInspection;
    private Button pauseButton;
    private Button playButton;
    private TMP_Text pauseLabel;
    private TMP_Text playLabel;
    private bool explicitlyPaused;
    private static readonly Color ControlInk = new Color(.12f,.125f,.14f);
    private static readonly Color SignalRed = new Color(.78f,.12f,.16f);
    private static readonly Color LiveGreen = new Color(.18f,.48f,.3f);

    public bool IsEnemyInspectionOpen => enemyInspection != null && enemyInspection.IsOpen;
    public bool IsExplicitlyPaused => explicitlyPaused;

    private void Awake()
    {
        if(GetComponent<StatusHUD>()==null)gameObject.AddComponent<StatusHUD>();
        if(GetComponent<SkillTreeUI>()==null)gameObject.AddComponent<SkillTreeUI>();
        enemyInspection = GetComponent<EnemyInspectionPanelUI>();
        if (enemyInspection == null) enemyInspection = gameObject.AddComponent<EnemyInspectionPanelUI>();
        enemyInspection.Initialize(this);
        ConfigureMainControls();
        ConfigureHealthBars();
        var corruptionTheme = GetComponent<CorruptionUITheme>();
        if (corruptionTheme == null) corruptionTheme = gameObject.AddComponent<CorruptionUITheme>();
        corruptionTheme.Initialize(this, enemyInspection);
    }
    public void ToggleInventory()
    {
        if (player != null && player.CurrentLife <= 0) return;
        GetComponent<SkillTreeUI>()?.Close();
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
    public void ToggleStats()
    {
        if (player != null && player.CurrentLife <= 0) return;
        GetComponent<SkillTreeUI>()?.Close();
        bool open = !statsPanel.activeSelf;
        enemyInspection?.Close();
        statsPanel.SetActive(open);
    }
    public void ToggleEnemyInspection() => enemyInspection?.Toggle();
    public void CloseEnemyInspection() => enemyInspection?.Close();
    public void PauseGameplay()
    {
        if (player != null && player.CurrentLife <= 0f) return;
        explicitlyPaused = true;
        Time.timeScale = 0f;
        RefreshPlaybackControls();
    }
    public void PlayGameplay()
    {
        if (player != null && player.CurrentLife <= 0f) return;
        explicitlyPaused = false;
        Time.timeScale = 1f;
        RefreshPlaybackControls();
    }
    private void Update()
    {
        if (explicitlyPaused && Time.timeScale != 0f)
            Time.timeScale = 0f;

        if (player != null && player.CurrentLife <= 0)
        {
            inventoryPanel.SetActive(false); statsPanel.SetActive(false);
            enemyInspection?.Close();
            GetComponent<SkillTreeUI>()?.Close();
        }
        RefreshPlaybackControls();
        var zone = FindFirstObjectByType<ZoneManager>();
        var run = GameManager.Instance;
        if (runText != null)
        {
            string location = zone != null ? zone.LocationLabel.ToUpperInvariant() : "FOREST 1";
            string progress = run == null ? "" : "  /  STAGE " + run.EncounterStage + "/" + (run.NormalKillsRequired + 1) + (run.BossActive ? "  /  BOSS" : "");
            runText.text = "BLACK CUBE  /  " + location + progress;
        }
        if (playerText != null && player != null) playerText.text = "WANDERER   " + Mathf.CeilToInt(player.CurrentLife) + " / " + Mathf.CeilToInt(player.MaxLife) + " HP";
        if (playerHealthBar != null)
            playerHealthBar.SetHealth(player != null ? player.CurrentLife : 0f, player != null ? player.MaxLife : 0f);
        var enemy = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        if (enemyText != null && enemy != null)
        {
            var hp = enemy.GetComponent<HealthComponent>();
            var actor = enemy.GetComponent<PaperSpriteActor>();
            string enemyName = actor != null ? actor.DisplayName : "Ghoul";
            enemyText.text = (hp.IsBoss ? "BOSS  /  " : "") + enemy.CurrentRarity.ToString().ToUpperInvariant() + " " + enemyName.ToUpperInvariant() + "   " + Mathf.CeilToInt(hp.CurrentLife) + " HP";
            if (enemyHealthBar != null) enemyHealthBar.SetHealth(hp.CurrentLife, hp.MaxLife);
        }
        else
        {
            if (enemyText != null) enemyText.text = string.Empty;
            if (enemyHealthBar != null) enemyHealthBar.SetHealth(0f, 1f);
        }
    }

    private void ConfigureMainControls()
    {
        PlaceControl(runText != null ? runText.transform : null, .0605f, .49f, .45f, .91f);
        FitLabel(runText, 12f, 22f);
        FitLabel(playerText, 9f, 15f);
        FitLabel(enemyText, 9f, 15f);
        PlaceControl(transform.Find("INVENTORY"), .76f, .25f, .835f, .76f);
        PlaceControl(transform.Find("STATS"), .84f, .25f, .905f, .76f);
        CreateControl("ENEMY", .68f, .25f, .755f, .76f, ToggleEnemyInspection, out _);
        pauseButton = CreateControl("PAUSE", .915f, .53f, .985f, .82f, PauseGameplay, out pauseLabel);
        playButton = CreateControl("PLAY", .915f, .16f, .985f, .45f, PlayGameplay, out playLabel);
        RefreshPlaybackControls();
    }

    private void ConfigureHealthBars()
    {
        playerHealthBar = EnsureHealthBar(playerHealthBar, "Wanderer Health", .184f, .326f, .339f, .398f);
        enemyHealthBar = EnsureHealthBar(enemyHealthBar, "Enemy Health", .446f, .326f, .601f, .398f);
    }

    private CombatHealthBar EnsureHealthBar(CombatHealthBar bar, string objectName,
        float x, float y, float maxX, float maxY)
    {
        bool needsDefaultLayout = false;
        if (bar == null)
        {
            Transform existing = transform.Find(objectName);
            if (existing != null) bar = existing.GetComponent<CombatHealthBar>();
        }
        if (bar == null)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CombatHealthBar));
            go.transform.SetParent(transform, false);
            bar = go.GetComponent<CombatHealthBar>();
            needsDefaultLayout = true;
        }
        if (needsDefaultLayout) PlaceControl(bar.transform, x, y, maxX, maxY);
        bar.color = new Color(1f, .35f, .035f, 1f);
        bar.raycastTarget = false;
        bar.transform.SetAsFirstSibling();
        return bar;
    }

    private static void FitLabel(TMP_Text label, float minSize, float maxSize)
    {
        if (label == null) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = minSize;
        label.fontSizeMax = maxSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private Button CreateControl(string label, float x, float y, float maxX, float maxY,
        UnityAction action, out TMP_Text text)
    {
        Transform existing = transform.Find(label);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        if (existing == null) go.transform.SetParent(transform, false);
        PlaceControl(go.transform, x, y, maxX, maxY);

        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.color = ControlInk;
        Button button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        text = go.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            text = textObject.GetComponent<TextMeshProUGUI>();
        }
        text.text = label;
        text.fontSize = 11f;
        text.color = new Color(.91f,.92f,.93f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }

    private static void PlaceControl(Transform target, float x, float y, float maxX, float maxY)
    {
        if (target == null) return;
        RectTransform rect = target as RectTransform;
        if (rect == null) return;
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void RefreshPlaybackControls()
    {
        bool alive = player == null || player.CurrentLife > 0f;
        if (pauseButton != null)
        {
            pauseButton.interactable = alive && !explicitlyPaused;
            var skin = pauseButton.GetComponent<CorruptionUIButtonSkin>();
            if (skin != null) skin.SetSelected(explicitlyPaused);
            else pauseButton.GetComponent<Image>().color = explicitlyPaused ? SignalRed : ControlInk;
        }
        if (playButton != null)
        {
            playButton.interactable = alive && explicitlyPaused;
            var skin = playButton.GetComponent<CorruptionUIButtonSkin>();
            if (skin != null) skin.SetSelected(false);
            else playButton.GetComponent<Image>().color = explicitlyPaused ? LiveGreen : ControlInk;
        }
        if (pauseLabel != null) pauseLabel.text = explicitlyPaused ? "PAUSED" : "PAUSE";
        if (playLabel != null) playLabel.text = explicitlyPaused ? "PLAY" : "LIVE";
    }

    private void OnDestroy()
    {
        if (explicitlyPaused)
            Time.timeScale = 1f;
    }
}
