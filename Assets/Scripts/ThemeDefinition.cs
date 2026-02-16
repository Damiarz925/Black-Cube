using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThemeDefinition", menuName = "Scriptable Objects/ThemeDefinition")]
public class ThemeDefinition : ScriptableObject
{
    public string ThemeId;
    public int Weight = 1;

    // Sky / background
    public Material SkyboxMaterial;
    public Sprite BackgroundSprite;
    public Color AmbientTint = Color.white;

    // Prefab groups by category
    public List<GameObject> ForegroundTreePrefabs;
    public List<GameObject> ForegroundTreePrefabsRare;
    public List<GameObject> MidgroundTreePrefabs;
    public List<GameObject> BackgroundTreePrefabs;
    public List<GameObject> CameraRockPrefabs;
    public List<GameObject> ForegroundRockPrefabs;
    public List<GameObject> MidgroundRockPrefabs;
    public List<GameObject> ForegroundDecorPrefabs;
    public List<GameObject> BackgroundTerrainPrefabs;
    public List<GameObject> ForegroundTerrainPrefabs;

    // Per-category placement rules
    public ThemeSpawnRules ForegroundTreeRules;
    public ThemeSpawnRules ForeGroundTreeRulesRare;
    public ThemeSpawnRules MidgroundTreeRules;
    public ThemeSpawnRules BackgroundTreeRules;
    public ThemeSpawnRules CameraRockRules;
    public ThemeSpawnRules ForegroundRockRules;
    public ThemeSpawnRules MidgroundRockRules;
    public ThemeSpawnRules ForegroundTerrainRules;
    public ThemeSpawnRules BackgroundTerrainRules;
}
