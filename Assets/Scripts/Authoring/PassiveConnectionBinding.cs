using UnityEngine;

[ExecuteAlways]
public sealed class PassiveConnectionBinding : MonoBehaviour
{
    [SerializeField] RectTransform from, to, line;
    [SerializeField] string fromSlotId = string.Empty, toSlotId = string.Empty, visibilitySlotId = string.Empty;
    [SerializeField] bool junctionStem;
    [SerializeField, Min(.5f)] float thickness = 4f;
    [SerializeField] Vector2 fromAnchor=new(.5f,.5f),toAnchor=new(.5f,.5f);
    [SerializeField] Vector2 fromOffset,toOffset;
    public Vector2 FromAnchor {get=>fromAnchor;set{fromAnchor=value;UpdateGeometry();}}
    public Vector2 ToAnchor {get=>toAnchor;set{toAnchor=value;UpdateGeometry();}}
    public Vector2 FromOffset {get=>fromOffset;set{fromOffset=value;UpdateGeometry();}}
    public Vector2 ToOffset {get=>toOffset;set{toOffset=value;UpdateGeometry();}}
    public Vector3 FromWorldPoint=>Attachment(from,fromAnchor,fromOffset);
    public Vector3 ToWorldPoint=>Attachment(to,toAnchor,toOffset);
    static Vector3 Attachment(RectTransform endpoint,Vector2 anchor,Vector2 offset)=>endpoint!=null?endpoint.TransformPoint(endpoint.rect.min+Vector2.Scale(endpoint.rect.size,anchor)+offset):Vector3.zero;
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
        Vector2 a = parent.InverseTransformPoint(FromWorldPoint); Vector2 b = parent.InverseTransformPoint(ToWorldPoint);
        line.anchorMin = line.anchorMax = Vector2.one * .5f; line.anchoredPosition = (a + b) * .5f - parent.rect.center; line.sizeDelta = new Vector2(Vector2.Distance(a, b), thickness);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
    }
}
