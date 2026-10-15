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
            string location = zone != null ? zone.ZoneName.ToUpperInvariant() + " " + zone.StageNumber : "FOREST 1";
            string progress = run == null ? "" : run.BossActive ? "  /  BOSS" : "  /  " + run.NormalKills + "/" + run.NormalKillsRequired;
            runText.text = "BLACK CUBE  /  " + location + progress;
        }
        if (playerText != null && player != null) playerText.text = "WANDERER   " + Mathf.CeilToInt(player.CurrentLife) + " / " + Mathf.CeilToInt(player.MaxLife) + " HP";
        var enemy = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        if (enemyText != null && enemy != null)
        {
            var hp = enemy.GetComponent<HealthComponent>();
            enemyText.text = (hp.IsBoss ? "BOSS  /  " : "") + enemy.CurrentRarity.ToString().ToUpperInvariant() + " GHOUL   " + Mathf.CeilToInt(hp.CurrentLife) + " HP";
        }
    }
}
