using UnityEngine;

[ExecuteAlways]
public sealed class PassiveConnectionBinding : MonoBehaviour
{
    [SerializeField] RectTransform from, to, line;
    [SerializeField] string fromSlotId = string.Empty, toSlotId = string.Empty, visibilitySlotId = string.Empty;
    [SerializeField] bool junctionStem;
    [SerializeField, Min(.5f)] float thickness = 4f;
    public RectTransform From => from; public RectTransform To => to; public RectTransform Line => line;
    public string FromSlotId => fromSlotId ?? string.Empty; public string ToSlotId => toSlotId ?? string.Empty; public string VisibilitySlotId => visibilitySlotId ?? string.Empty; public bool IsJunctionStem => junctionStem;
    public void Configure(RectTransform first, RectTransform second, RectTransform authoredLine, string firstSlot = null, string secondSlot = null, string visibleSlot = null, bool isStem = false) { from = first; to = second; line = authoredLine; fromSlotId = firstSlot ?? string.Empty; toSlotId = secondSlot ?? string.Empty; visibilitySlotId = visibleSlot ?? secondSlot ?? string.Empty; junctionStem = isStem; UpdateGeometry(); }
    void OnEnable() => UpdateGeometry();
    void OnValidate() => UpdateGeometry();
    void LateUpdate() { if (!Application.isPlaying && (from == null || to == null || line == null)) return; UpdateGeometry(); }
    public void UpdateGeometry()
    {
        if (from == null || to == null || line == null || line.parent == null) return;
        RectTransform parent = line.parent as RectTransform; if (parent == null) return;
        Vector2 a = parent.InverseTransformPoint(from.TransformPoint(from.rect.center)); Vector2 b = parent.InverseTransformPoint(to.TransformPoint(to.rect.center));
        line.anchorMin = line.anchorMax = Vector2.one * .5f; line.anchoredPosition = (a + b) * .5f; line.sizeDelta = new Vector2(Vector2.Distance(a, b), thickness);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
    }
}
