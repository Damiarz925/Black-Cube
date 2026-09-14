// Developer map: Small serialized TMP label binding for a stats section heading; PlayerStatsPanelUI creates and manages these views.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
