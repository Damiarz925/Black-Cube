// Developer map: Clones the established player stats panel and rebinds it to the live generated enemy without exposing equipment mutation.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyInspectionPanelUI : MonoBehaviour
{
    private PaperBattleHUD hud;
    private GameObject panel;
    private PlayerStatsPanelUI statsView;
    private EquipmentStatsUI equipmentView;
    private TMP_Text identityText;
    private EnemyAI boundEnemy;

    public bool IsOpen => panel != null && panel.activeSelf;
    public GameObject Panel => panel;

    public void Initialize(PaperBattleHUD owner)
    {
        if (panel != null || owner == null || owner.statsPanel == null)
            return;

        hud = owner;
        panel = Instantiate(owner.statsPanel, owner.statsPanel.transform.parent);
        panel.name = "Enemy Inspection";
        panel.SetActive(false);

        statsView = panel.GetComponent<PlayerStatsPanelUI>();
        equipmentView = panel.GetComponent<EquipmentStatsUI>();
        identityText = panel.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text.gameObject.name == "WANDERER");
        if (identityText != null)
        {
            identityText.enableAutoSizing = true;
            identityText.fontSizeMin = 11f;
            identityText.fontSizeMax = 20f;
        }

        Button oldClose = panel.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.gameObject.name == "Close");
        if (oldClose != null)
        {
            CreateCloseButton(oldClose);
            oldClose.gameObject.SetActive(false);
        }

        BindCurrentEnemy(force: true);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        BindCurrentEnemy();
        RefreshIdentity();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        if (panel == null || hud == null || hud.player == null || hud.player.CurrentLife <= 0f)
            return;

        hud.statsPanel.SetActive(false);
        GetComponent<SkillTreeUI>()?.Close();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        BindCurrentEnemy(force: true);
        RefreshIdentity();
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void BindCurrentEnemy(bool force = false)
    {
        EnemyAI current = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        bool sameEnemy = current == boundEnemy;
        bool previousEnemyWasDestroyed = current == null && !ReferenceEquals(boundEnemy, null);
        if (!force && sameEnemy && !previousEnemyWasDestroyed)
            return;

        boundEnemy = current;
        statsView?.SetTarget(boundEnemy != null ? boundEnemy.GetComponent<StatsComponent>() : null);
        equipmentView?.SetEnemyTarget(boundEnemy);
        RefreshIdentity();
    }

    private void RefreshIdentity()
    {
        if (identityText == null)
            return;

        if (boundEnemy == null)
        {
            identityText.text = "NO CURRENT ENEMY";
            return;
        }

        HealthComponent health = boundEnemy.GetComponent<HealthComponent>();
        PaperSpriteActor actor = boundEnemy.GetComponent<PaperSpriteActor>();
        string enemyName = actor != null ? actor.DisplayName : boundEnemy.name;
        string boss = health != null && health.IsBoss ? "BOSS  /  " : "";
        string life = health != null
            ? $"  /  {Mathf.CeilToInt(health.CurrentLife)} / {Mathf.CeilToInt(health.MaxLife)} HP"
            : "";
        identityText.text = $"{boss}{boundEnemy.CurrentRarity.ToString().ToUpperInvariant()} {enemyName.ToUpperInvariant()}  /  LV {boundEnemy.EnemyLevel}{life}";
    }

    private void CreateCloseButton(Button template)
    {
        var closeObject = new GameObject("Enemy Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(template.transform.parent, false);
        RectTransform sourceRect = (RectTransform)template.transform;
        RectTransform closeRect = (RectTransform)closeObject.transform;
        closeRect.anchorMin = sourceRect.anchorMin;
        closeRect.anchorMax = sourceRect.anchorMax;
        closeRect.pivot = sourceRect.pivot;
        closeRect.offsetMin = sourceRect.offsetMin;
        closeRect.offsetMax = sourceRect.offsetMax;

        Image sourceImage = template.targetGraphic as Image;
        Image image = closeObject.GetComponent<Image>();
        image.color = sourceImage != null ? sourceImage.color : new Color(.12f,.125f,.14f);
        image.sprite = sourceImage != null ? sourceImage.sprite : null;
        image.type = sourceImage != null ? sourceImage.type : Image.Type.Simple;

        Button close = closeObject.GetComponent<Button>();
        close.targetGraphic = image;
        close.transition = template.transition;
        close.colors = template.colors;
        close.navigation = template.navigation;
        close.onClick.AddListener(Close);

        var labelObject = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(closeObject.transform, false);
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TMP_Text sourceLabel = template.GetComponentInChildren<TMP_Text>(true);
        TMP_Text label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "X";
        label.font = sourceLabel != null ? sourceLabel.font : null;
        label.fontSize = sourceLabel != null ? sourceLabel.fontSize : 14f;
        label.color = sourceLabel != null ? sourceLabel.color : Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (panel != null)
            Destroy(panel);
    }
}
