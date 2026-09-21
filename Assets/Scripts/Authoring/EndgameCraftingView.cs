using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndgameCraftingView : MonoBehaviour
{
    public GameObject panel;
    public Button openButton, applyButton, nextItemButton, nextModButton, nextPoolButton, closeButton;
    public List<Button> modeButtons=new();
    public TMP_Text body;
}
