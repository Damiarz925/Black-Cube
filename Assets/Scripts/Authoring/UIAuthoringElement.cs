using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIAuthoringElement : MonoBehaviour
{
    [SerializeField] string stableUiId = string.Empty;
    [SerializeField] string friendlyName = string.Empty;
    [SerializeField] RectTransform authoredRect;
    [SerializeField] Image primaryImage;
    [SerializeField] TMP_Text primaryText;
    [SerializeField] Image optionalIcon;
    [SerializeField] UIButtonVisualStyleSO visualStyle;
    public string StableUiId => stableUiId ?? string.Empty;
    public string FriendlyName => friendlyName ?? string.Empty;
    public RectTransform AuthoredRect => authoredRect != null ? authoredRect : transform as RectTransform;
    public Image PrimaryImage => primaryImage;
    public TMP_Text PrimaryText => primaryText;
    public Image OptionalIcon => optionalIcon;
    public UIButtonVisualStyleSO VisualStyle => visualStyle;
    public void Configure(string id, string displayName, RectTransform rect, Image image, TMP_Text text, Image icon = null, UIButtonVisualStyleSO style = null)
    { stableUiId = id ?? string.Empty; friendlyName = displayName ?? string.Empty; authoredRect = rect; primaryImage = image; primaryText = text; optionalIcon = icon; visualStyle = style; }
}
