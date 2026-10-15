// Developer map: Draws the matching slanted player/enemy HUD health tracks and their normalized live fill.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatHealthBar : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float normalizedValue = 1f;
    [SerializeField] private Color trackColor = new Color(.025f, .028f, .034f, 1f);
    [SerializeField] private float slant = 3f;

    public float NormalizedValue => normalizedValue;

    public void SetHealth(float currentLife, float maxLife)
    {
        float next = maxLife > 0f && !float.IsNaN(currentLife) && !float.IsNaN(maxLife)
            ? Mathf.Clamp01(currentLife / maxLife)
            : 0f;
        if (Mathf.Approximately(normalizedValue, next)) return;
        normalizedValue = next;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        AddQuad(vh,
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMin, rect.yMax),
            new Vector2(rect.xMax, rect.yMax),
            new Vector2(rect.xMax, rect.yMin),
            trackColor);

        float width = rect.width * normalizedValue;
        if (width <= 0.01f) return;

        // The authored Wanderer accent is a trapezoid whose top edge is inset.
        // Both actors deliberately use this same orientation instead of mirroring it.
        float inset = Mathf.Min(slant, width * .5f);
        float right = rect.xMin + width;
        AddQuad(vh,
            new Vector2(rect.xMin, rect.yMin),
            new Vector2(rect.xMin + inset, rect.yMax),
            new Vector2(right - inset, rect.yMax),
            new Vector2(right, rect.yMin),
            color);
    }

    private static void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topLeft,
        Vector2 topRight, Vector2 bottomRight, Color tint)
    {
        int first = vh.currentVertCount;
        vh.AddVert(bottomLeft, tint, Vector2.zero);
        vh.AddVert(topLeft, tint, Vector2.zero);
        vh.AddVert(topRight, tint, Vector2.zero);
        vh.AddVert(bottomRight, tint, Vector2.zero);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first, first + 2, first + 3);
    }
}
