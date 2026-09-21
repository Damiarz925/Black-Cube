using System;
using UnityEngine;

public sealed class PlayerDisplayNameProvider : MonoBehaviour
{
    const string PreferenceKey = "BlackCube.PlayerDisplayName";
    [SerializeField] string displayName = "Wanderer";
    public string DisplayName => Normalize(displayName);
    public event Action Changed;
    void Awake() { displayName = Normalize(PlayerPrefs.GetString(PreferenceKey, DisplayName)); }
    public static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? "Wanderer" : value.Trim();
    public void SetDisplayName(string value) { string next = Normalize(value); if (next == DisplayName) return; displayName = next; PlayerPrefs.SetString(PreferenceKey, displayName); PlayerPrefs.Save(); Changed?.Invoke(); }
}
