using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChallengeView : MonoBehaviour
{
    public GameObject panel;
    public Button openButton, closeButton;
    public TMP_Text body;
    public List<Button> entries=new();
}
