using UnityEngine;
using UnityEngine.UI;

public sealed class HUDResourceBar : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] float fillAmount;
    public float FillAmount { get => fillAmount; set { value = Mathf.Clamp01(value); if (Mathf.Approximately(fillAmount, value)) return; fillAmount = value; SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper vh) { vh.Clear(); Rect r = GetPixelAdjustedRect(); r.width *= fillAmount; if (r.width <= 0f || r.height <= 0f) return; UIVertex v = UIVertex.simpleVert; v.color = color; v.position = new Vector2(r.xMin, r.yMin); vh.AddVert(v); v.position = new Vector2(r.xMin, r.yMax); vh.AddVert(v); v.position = new Vector2(r.xMax, r.yMax); vh.AddVert(v); v.position = new Vector2(r.xMax, r.yMin); vh.AddVert(v); vh.AddTriangle(0, 1, 2); vh.AddTriangle(2, 3, 0); }
}
