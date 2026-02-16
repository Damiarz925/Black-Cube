using TMPro;
using UnityEngine;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    public void Set(string name, string value)
    {
        if (nameText != null) nameText.text = name;
        if (valueText != null) valueText.text = value;
    }
}
