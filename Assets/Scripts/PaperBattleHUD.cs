// Developer map: Reads run, health and current-enemy state for the paper HUD and toggles inventory/stats panels. Opening those panels is separate from the skill-tree combat pause.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;

/// <summary>Scene-local presentation; reads the existing gameplay state.</summary>
public class PaperBattleHUD : MonoBehaviour
{
    public TMP_Text runText;
    public TMP_Text playerText;
    public TMP_Text enemyText;
    public HealthComponent player;
    public GameObject inventoryPanel;
    public GameObject statsPanel;
    private void Awake()
    {
        if(GetComponent<StatusHUD>()==null)gameObject.AddComponent<StatusHUD>();
        if(GetComponent<SkillTreeUI>()==null)gameObject.AddComponent<SkillTreeUI>();
        if (enemyText != null) enemyText.rectTransform.anchorMax = new Vector2(.61f, enemyText.rectTransform.anchorMax.y);
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
        statsPanel.SetActive(!statsPanel.activeSelf);
    }
    private void Update()
    {
        if (player != null && player.CurrentLife <= 0)
        {
            inventoryPanel.SetActive(false); statsPanel.SetActive(false);
            GetComponent<SkillTreeUI>()?.Close();
        }
        var zone = FindFirstObjectByType<ZoneManager>();
        var run = GameManager.Instance;
        if (runText != null)
        {
            string location = zone != null ? zone.LocationLabel.ToUpperInvariant() : "FOREST 1";
            string progress = run == null ? "" : "  /  STAGE " + run.EncounterStage + "/" + (run.NormalKillsRequired + 1) + (run.BossActive ? "  /  BOSS" : "");
            runText.text = "BLACK CUBE  /  " + location + progress;
        }
        if (playerText != null && player != null) playerText.text = "WANDERER   " + Mathf.CeilToInt(player.CurrentLife) + " / " + Mathf.CeilToInt(player.MaxLife) + " HP";
        var enemy = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        if (enemyText != null && enemy != null)
        {
            var hp = enemy.GetComponent<HealthComponent>();
            var actor = enemy.GetComponent<PaperSpriteActor>();
            string enemyName = actor != null ? actor.DisplayName : "Ghoul";
            enemyText.text = (hp.IsBoss ? "BOSS  /  " : "") + enemy.CurrentRarity.ToString().ToUpperInvariant() + " " + enemyName.ToUpperInvariant() + "   " + Mathf.CeilToInt(hp.CurrentLife) + " HP";
        }
    }
}
