using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIButtonVisualController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] Button button;
    [SerializeField] Image targetImage;
    [SerializeField] TMP_Text targetText;
    [SerializeField] Image childIcon;
    [SerializeField] UIButtonVisualStyleSO style;
    bool over, down, selected;
    Vector3 authoredScale;
    public UIButtonVisualState CurrentState { get; private set; }
    void Awake() { EnsureBindings(); authoredScale = transform.localScale; Refresh(); }
    public void SetStyle(UIButtonVisualStyleSO value) { style = value; Refresh(); }
    public void SetSelected(bool value) { selected = value; Refresh(); }
    public void Refresh()
    {
        EnsureBindings();
        CurrentState = button != null && !button.interactable ? UIButtonVisualState.Disabled : selected ? UIButtonVisualState.Selected : down ? UIButtonVisualState.Pressed : over ? UIButtonVisualState.Hovered : UIButtonVisualState.Normal;
        if (style == null) return;
        UIButtonStateVisual visual = style.For(CurrentState);
        if (targetImage != null) { targetImage.sprite = visual.sprite; targetImage.color = visual.color; }
        if (targetText != null) targetText.color = visual.textColor;
        if (childIcon != null) childIcon.color = visual.childIconColor;
        transform.localScale = Vector3.Scale(authoredScale, new Vector3(visual.scaleMultiplier.x, visual.scaleMultiplier.y, 1f));
    }
    void EnsureBindings()
    {
        if (button == null) button = GetComponent<Button>();
        if (targetImage == null && button != null) targetImage = button.targetGraphic as Image;
        if (authoredScale == Vector3.zero) authoredScale = transform.localScale;
    }
    public void OnPointerEnter(PointerEventData e) { over = true; Refresh(); }
    public void OnPointerExit(PointerEventData e) { over = down = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) down = true; Refresh(); }
    public void OnPointerUp(PointerEventData e) { down = false; Refresh(); }
    public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData e) { selected = down = false; Refresh(); }
}
