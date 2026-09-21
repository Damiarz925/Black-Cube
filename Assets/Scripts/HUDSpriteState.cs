using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HUDSpriteState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] Button button;
    [SerializeField] Image image;
    [SerializeField] Sprite normal, hover, pressed;
    bool pointerOver, pointerDown, focused, persistentActive;
    public bool PersistentActive => persistentActive;
    public int VisualStateIndex => button != null && !button.IsInteractable() ? -1 : persistentActive ? 2 : pointerDown ? 2 : pointerOver || focused ? 1 : 0;
    public void Initialize(Button owner, TopHUDButtonKind kind)
    {
        Bind(owner);
        if (normal == null) Debug.LogWarning($"HUD button {kind} has no editor-authored visual state set. Assign it through SO_UIVisualLibrary.", this);
    }
    public void Configure(Button owner, Sprite normalSprite, Sprite hoverSprite, Sprite pressedSprite) { normal = normalSprite; hover = hoverSprite; pressed = pressedSprite; Bind(owner); }
    public void Bind(Button owner) { button = owner; image = owner != null ? owner.targetGraphic as Image : null; if (image == null && owner != null) image = owner.GetComponent<Image>(); if (owner != null) { owner.transition = Selectable.Transition.None; owner.targetGraphic = image; } if (image != null) { image.preserveAspect = false; image.raycastTarget = true; } Refresh(); }
    public void SetPersistentActive(bool value) { if (persistentActive == value) return; persistentActive = value; Refresh(); }
    public void Refresh() { if (image == null) return; int state = VisualStateIndex; image.sprite = state == 2 ? pressed : state == 1 ? hover : normal; image.color = state < 0 ? new Color(.42f, .42f, .42f, 1f) : Color.white; image.enabled = image.sprite != null; }
    public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { pointerOver = false; pointerDown = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pointerDown = true; Refresh(); }
    public void OnPointerUp(PointerEventData eventData) { pointerDown = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { focused = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { focused = false; pointerDown = false; Refresh(); }
    void OnDisable() { pointerOver = pointerDown = focused = false; Refresh(); }
}
