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
    private void Awake() { if(GetComponent<StatusHUD>()==null)gameObject.AddComponent<StatusHUD>(); }
    public void ToggleInventory() { inventoryPanel.SetActive(!inventoryPanel.activeSelf); }
    public void ToggleStats()
    {
        statsPanel.SetActive(!statsPanel.activeSelf);
    }
    private void Update()
    {
        var zone = FindFirstObjectByType<ZoneManager>();
        if (runText != null) runText.text = "BLACK CUBE  /  FOREST " + (zone != null ? zone.zoneLevel : 1);
        if (playerText != null && player != null) playerText.text = "WANDERER   " + Mathf.CeilToInt(player.CurrentLife) + " / " + Mathf.CeilToInt(player.MaxLife) + " HP";
        var enemy = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        if (enemyText != null && enemy != null)
        {
            var hp = enemy.GetComponent<HealthComponent>();
            enemyText.text = enemy.CurrentRarity.ToString().ToUpperInvariant() + " GHOUL   " + Mathf.CeilToInt(hp.CurrentLife) + " HP";
        }
    }
}
