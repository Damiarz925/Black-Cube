using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StatusBadge : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public StatusHUD Owner;
    public TMP_Text Label;
    public StatusGlyph Glyph;
    public StatusController.Summary Summary;
    public void OnPointerEnter(PointerEventData e){Owner.ShowTooltip(this);}
    public void OnPointerExit(PointerEventData e){Owner.HideTooltip();}
}
