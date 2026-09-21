using TMPro;
using UnityEngine;

public sealed class StatusHUDView : MonoBehaviour
{
    public RectTransform authoredRoot;
    public RectTransform playerStrip;
    public RectTransform enemyStrip;
    public RectTransform tooltip;
    public TMP_Text tooltipText;
    public StatusBadge badgePrefab;
}
