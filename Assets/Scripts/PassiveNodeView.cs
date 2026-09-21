using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PassiveNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IPointerClickHandler
{
    SkillTreeUI owner;
    int nodeId;
    public void Initialize(SkillTreeUI tree, int id) { owner = tree; nodeId = id; }
    public void OnPointerEnter(PointerEventData eventData) => owner?.SetHovered(nodeId, true);
    public void OnPointerExit(PointerEventData eventData) => owner?.SetHovered(nodeId, false);
    public void OnSelect(BaseEventData eventData) => owner?.ShowDetails(nodeId);
    public void OnPointerClick(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Right) owner?.Refund(nodeId); }
}
