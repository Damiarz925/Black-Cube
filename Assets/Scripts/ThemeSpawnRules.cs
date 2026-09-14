// Developer map: Legacy scenery placement controls in world units/degrees, with count, density and skip probability. PositionJitter x/y fields perturb world X/Z; vertical offset is separate.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

[System.Serializable]
public struct ThemeSpawnRules
{
    public bool Enabled;

    // Density & counts
    public float densityMultiplier; // float (1.0 = default, 0.5 = half as many)
    public Vector2Int CountRange;   // Vector2Int (min,max) - used when anchors are discreet (Example: (3,7) trees)

    // Random offset from anchor (prevents grid feel)
    public Vector2 PositionJitter;  // Vector2 fields x/y offset world X/Z respectively, in Unity units.

    // Rotation
    public Vector2 RotationRangeDeg;    // Vector2 (minDeg, maxDeg)

    // Scale
    public Vector2 ScaleRange;      // Vector2 (minScale, maxScale)

    // Clipping / spacing control
    public float MinSeparation;     // Float (world units) - distance between objects in this category

    // Vertical placement constraints (mainly for keeping foreground flat)
    public Vector2 YOffsetRange;    // Vector2 (minY, maxY) addet to anchor.y

    // Chance to skip an anchor (creates sparser patterns)
    [Range(0f, 1f)]
    public float SkipChance;
}
