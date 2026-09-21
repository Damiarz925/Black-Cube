using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SubclassView : MonoBehaviour
{
    public GameObject panel;
    public Button openButton, projectileModeButton;
    public List<Button> choiceButtons=new();
    public TMP_Text openLabel, title, description, projectileModeLabel, auraLabel, frozenLabel;
    public List<TMP_Text> choiceLabels=new();
}
