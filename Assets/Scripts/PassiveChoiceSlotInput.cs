using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PassiveChoiceSlotInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler
{
    public SkillTreeUI owner;
    public PassiveChoiceSlotView slot;
    public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)owner?.RefundSlot(slot);}
    public void OnPointerEnter(PointerEventData e)=>owner?.DescribeSlot(slot);
    public void OnSelect(BaseEventData e)=>owner?.DescribeSlot(slot);
}
