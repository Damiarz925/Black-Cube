// Developer map: Binds a stat name/value pair to serialized TMP labels. Formatting and which rows exist are decided by PlayerStatsPanelUI.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
