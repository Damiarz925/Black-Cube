using TMPro;
using UnityEngine;

public class StatHeaderUI : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;

    public void SetText(string s)
    {
        if (headerText != null)
            headerText.text = s;
    }
}
