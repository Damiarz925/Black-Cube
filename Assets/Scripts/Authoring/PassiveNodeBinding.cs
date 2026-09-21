using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class PassiveNodeBinding : MonoBehaviour
{
    [SerializeField] string stableUiId = string.Empty;
    [SerializeField] string logicalSlotId = string.Empty;
    [SerializeField] Button button;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text magnitude;
    [SerializeField] TMP_Text authoringLabel;
    public string StableUiId => stableUiId ?? string.Empty;
    public string LogicalSlotId => logicalSlotId ?? string.Empty;
    public Button Button => button;
    public Image Icon => icon;
    public TMP_Text Magnitude => magnitude;
    public void Configure(string uiId, string slotId, Button owner, Image nodeIcon, TMP_Text valueLabel, TMP_Text debugLabel) { stableUiId = uiId; logicalSlotId = slotId; button = owner; icon = nodeIcon; magnitude = valueLabel; authoringLabel = debugLabel; }
    public void ApplyAuthoringPreview(PassiveAuthoredNode data, PassiveNodeIconLibrarySO library, bool showLabel)
    {
        if (data == null) return;
        if (icon != null && library != null) icon.sprite = library.Resolve(data);
        if (magnitude != null) magnitude.text = data.Effects.Count > 0 ? data.Effects[0].Value.ToString("+0.##;-0.##;0") : string.Empty;
        if (authoringLabel != null) { authoringLabel.gameObject.SetActive(showLabel); authoringLabel.text = ShortLabel(data.LogicalSlotId); }
    }
    static string ShortLabel(string id) { if (string.IsNullOrEmpty(id)) return "UNBOUND"; string[] p = id.Split('.'); return p.Length >= 4 ? string.Join("-", p, Math.Max(0, p.Length - 3), Math.Min(3, p.Length)).ToUpperInvariant() : id.ToUpperInvariant(); }
}
