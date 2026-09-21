using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillSelectionView : MonoBehaviour
{
    public GameObject informationPanel;
    public List<Button> skillButtons=new();
    public List<TMP_Text> skillLabels=new();
    public Button rageFinisherButton, closeButton;
    public TMP_Text rageFinisherLabel, title, body;
}
