using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PassiveChoicePopupView : MonoBehaviour
{
    public TMP_Text title;
    public List<Button> options = new();
    public List<Image> icons = new();
    public Button close;
}
