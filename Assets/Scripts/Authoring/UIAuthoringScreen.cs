using UnityEngine;

public sealed class UIAuthoringScreen : MonoBehaviour
{
    [SerializeField] UIAuthoringScreenKind screen;
    [SerializeField] string stableUiId = string.Empty;
    [SerializeField] UIVisualLibrarySO visualLibrary;
    public UIAuthoringScreenKind Screen => screen;
    public string StableUiId => stableUiId ?? string.Empty;
    public UIVisualLibrarySO VisualLibrary => visualLibrary;
    public void Configure(UIAuthoringScreenKind kind, string id, UIVisualLibrarySO library) { screen = kind; stableUiId = id ?? string.Empty; visualLibrary = library; }
}
