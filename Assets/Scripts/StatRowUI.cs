// Developer map: Binds a stat name/value pair to serialized TMP labels. Formatting and which rows exist are decided by PlayerStatsPanelUI.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StatRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    string label, amount, tooltip;
    bool hovered;
    public void Set(string name, string value, string explanation = null)
    {
        label=name; amount=value; tooltip=explanation;
        if(valueText!=null)valueText.raycastTarget=!string.IsNullOrEmpty(explanation);
        Render();
    }
    public void OnPointerEnter(PointerEventData eventData){hovered=true;Render();}
    public void OnPointerExit(PointerEventData eventData){hovered=false;Render();}
    void OnDisable(){hovered=false;}
    void Render()
    {
        string name=label, value=hovered&&!string.IsNullOrEmpty(tooltip)?amount+"\n"+tooltip:amount;
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
