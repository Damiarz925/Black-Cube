using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ThemeSet", menuName = "Scriptable Objects/ThemeSet")]
public class ThemeSet : ScriptableObject
{
    public List<ThemeDefinition> Themes; //List<ThemeDefinition>

    // Optional forced theme mapping
    public List<ForcedThemeEntry> ForcedThemes;
}

[System.Serializable]
public struct ForcedThemeEntry
{
    public int LevelIndex;
    public ThemeDefinition Theme;
}
