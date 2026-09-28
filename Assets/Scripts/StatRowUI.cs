// Developer map: Binds a stat name/value pair to serialized TMP labels. Formatting and which rows exist are decided by PlayerStatsPanelUI.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    public void Set(string name, string value)
    {
        if (nameText != null && nameText.text != name) nameText.text = name;
        if (valueText != null && valueText.text != value) valueText.text = value;
        var layout=GetComponent<LayoutElement>();
        if(layout!=null&&valueText!=null)
        {
            float width=valueText.rectTransform.rect.width;
            float height=valueText.GetPreferredValues(value,Mathf.Max(180,width),0).y+4;
            float preferred=Mathf.Max(23,height);
            if(!Mathf.Approximately(layout.preferredHeight,preferred))layout.preferredHeight=preferred;
        }
    }
}
