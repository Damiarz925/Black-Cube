using UnityEngine;
using UnityEngine.UI;

public sealed class HUDPortraitMaskGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh) { vh.Clear(); Rect r = GetPixelAdjustedRect(); float cut = Mathf.Min(r.width * .18f, r.height * .14f); Vector2[] edge = { new(r.xMin + cut, r.yMin), new(r.xMax - cut, r.yMin), new(r.xMax, r.yMin + cut), new(r.xMax, r.yMax - cut), new(r.xMax - cut, r.yMax), new(r.xMin + cut, r.yMax), new(r.xMin, r.yMax - cut), new(r.xMin, r.yMin + cut) }; UIVertex v = UIVertex.simpleVert; v.color = Color.white; v.position = r.center; vh.AddVert(v); foreach (Vector2 point in edge) { v.position = point; vh.AddVert(v); } for (int i = 0; i < 8; i++) vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1); }
}
