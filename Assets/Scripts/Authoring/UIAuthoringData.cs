// Shared editor-authored visual state and stable UI binding contracts.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UIButtonVisualState { Normal, Hovered, Pressed, Disabled, Selected }

[Serializable]
public sealed class UIButtonStateVisual
{
    public Sprite sprite;
    public Color color = Color.white;
    public Vector2 scaleMultiplier = Vector2.one;
    public Color textColor = Color.white;
    public Color childIconColor = Color.white;
}

[Serializable]
public sealed class HUDButtonVisualSet
{
    public TopHUDButtonKind kind;
    public Sprite normal, hover, pressed;
}
