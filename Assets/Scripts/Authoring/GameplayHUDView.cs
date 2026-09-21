using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayHUDView : MonoBehaviour
{
    [Header("Roots")] public RectTransform authoredRoot, artworkRoot, bottomActionBar;
    [Header("Identity")] public Image playerPortrait, enemyPortrait; public TMP_Text playerName, enemyName, runSummary;
    [Header("Resources")] public HUDResourceBar playerLifeFill, playerManaFill, enemyLifeFill, enemyManaFill; public TMP_Text playerLifeText, playerManaText, enemyLifeText, enemyManaText;
    [Header("Controls")] public Button skillsButton, passivesButton, enemyButton, inventoryButton, statsButton, pauseButton, playButton;
    [Header("Panels")] public GameObject inventoryPanel, statsPanel, passiveTreePanel, skillPanel, subclassPanel, pausePanel, challengePanel, craftingPanel, enemyInspectionPanel, rebirthPanel;
}
