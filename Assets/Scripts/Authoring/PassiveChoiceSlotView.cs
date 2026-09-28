using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PassiveChoiceSlotView : MonoBehaviour
{
    public string choiceGroupId;
    public Button button;
    public Image icon;
    public TMP_Text label;
    public Sprite emptyIcon;
    public EmptyPassiveSlotGraphic emptyGlow;
}
