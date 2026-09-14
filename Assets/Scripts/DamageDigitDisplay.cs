// Developer map: Composes arbitrary damage values from a ten-cell, transparent 0-9 texture strip.
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds a compact, resolution-independent UI number from authored digit artwork.</summary>
public sealed class DamageDigitDisplay : MonoBehaviour
{
    public void SetValue(Texture2D atlas, string value, float digitHeight, float spacing, float maxWidth = 180f)
    {
        if (atlas == null || string.IsNullOrEmpty(value))
            return;

        var root = transform as RectTransform;
        if (root == null)
            return;

        const float verticalUvStart = 0.25f;
        const float verticalUvSpan = 0.5f;
        float cellAspect = atlas.width / (10f * atlas.height * verticalUvSpan);
        float digitWidth = digitHeight * cellAspect;
        float totalWidth = value.Length * digitWidth + Mathf.Max(0, value.Length - 1) * spacing;
        if (totalWidth > maxWidth)
        {
            float scale = maxWidth / totalWidth;
            digitHeight *= scale;
            digitWidth *= scale;
            spacing *= scale;
            totalWidth = maxWidth;
        }
        root.sizeDelta = new Vector2(totalWidth, digitHeight);

        for (int i = 0; i < value.Length; i++)
        {
            int digit = value[i] - '0';
            if (digit < 0 || digit > 9)
                continue;

            var child = new GameObject($"Digit {value[i]}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            child.transform.SetParent(transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(digitWidth, digitHeight);
            rect.anchoredPosition = new Vector2(-totalWidth * 0.5f + digitWidth * 0.5f + i * (digitWidth + spacing), 0f);

            var image = child.GetComponent<RawImage>();
            image.texture = atlas;
            image.uvRect = new Rect(digit * 0.1f, verticalUvStart, 0.1f, verticalUvSpan);
            image.raycastTarget = false;
        }
    }
}
