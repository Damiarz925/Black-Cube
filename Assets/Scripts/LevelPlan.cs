using System.Collections.Generic;
using UnityEngine;

public class LevelPlan
{
    public int LevelIndex;
    public int Seed;
    public ThemeDefinition Theme;

    public LevelBounds Bounds;

    public List<Placement> TreePlacements;
    public List<Placement> RockPlacements;
    public List<Placement> ForegroundPlacements;
    public List<Placement> TerrainPlacements;
}

[System.Serializable]
public struct LevelBounds
{
    public float MinZ;
    public float MaxZ;
    public float GroundY;
}

[System.Serializable]
public struct Placement
{
    public GameObject Prefab;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public int SortingOrder;
    public string PoolKey;
}
