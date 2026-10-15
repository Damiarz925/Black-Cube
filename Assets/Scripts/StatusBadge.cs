// Developer map: Pointer target for one status summary in StatusHUD. Hover delegates to the HUD tooltip rather than changing the status model.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
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
