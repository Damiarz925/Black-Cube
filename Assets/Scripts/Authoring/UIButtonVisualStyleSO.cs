using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/UI/Button Visual Style", fileName = "SO_ButtonVisualStyle")]
public sealed class UIButtonVisualStyleSO : ScriptableObject
{
    public UIButtonStateVisual normal = new(), hovered = new(), pressed = new(), disabled = new(), selected = new();
    public UIButtonStateVisual For(UIButtonVisualState state) => state switch { UIButtonVisualState.Hovered => hovered, UIButtonVisualState.Pressed => pressed, UIButtonVisualState.Disabled => disabled, UIButtonVisualState.Selected => selected, _ => normal };
}
