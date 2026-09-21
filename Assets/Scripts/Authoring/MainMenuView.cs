using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuView : MonoBehaviour
{
    public GameObject authoredRoot, optionsPanel, overwriteConfirmation, classSelectionPanel, slotSelectionPanel;
    public Button achievementsButton, newGameButton, loadGameButton, optionsButton, pausePassiveTreeButton, optionsBackButton;
    public Button confirmOverwriteButton, cancelOverwriteButton, beginSelectedClassButton, cancelClassButton, cancelSlotsButton;
    public TMP_Text pausePassiveTreeLabel, classSelectionLabel;
    public List<Button> classButtons=new(), slotButtons=new();
}
