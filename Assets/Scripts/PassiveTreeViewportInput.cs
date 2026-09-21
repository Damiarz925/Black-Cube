using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PassiveTreeViewportInput : MonoBehaviour, IScrollHandler
{
    SkillTreeUI owner;
    public void Initialize(SkillTreeUI tree) => owner = tree;
    public void OnScroll(PointerEventData eventData) => owner?.AdjustZoom(eventData);
}
