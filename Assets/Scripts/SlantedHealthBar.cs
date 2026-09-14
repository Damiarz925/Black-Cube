using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws the HUD's orange, right-leaning health signal without requiring a sprite.</summary>
public sealed class SlantedHealthBar : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
    [SerializeField, Min(0f)] private float slantWidth = 5f;

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(fillAmount, clamped)) return;
            fillAmount = clamped;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (fillAmount <= 0f) return;

        Rect bounds = GetPixelAdjustedRect();
        float width = bounds.width * fillAmount;
        float slant = Mathf.Min(slantWidth, width * .5f);
        float left = bounds.xMin;
        float right = left + width;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = new Vector3(left + slant, bounds.yMax); vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(right, bounds.yMax); vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(right - slant, bounds.yMin); vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(left, bounds.yMin); vertexHelper.AddVert(vertex);
        vertexHelper.AddTriangle(0, 1, 2);
        vertexHelper.AddTriangle(2, 3, 0);
    }
}
