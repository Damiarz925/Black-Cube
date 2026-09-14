// Developer map: Whole weapon sprite profile, including handle, with pivot correction and scale. Sprite art points along +X; gripOffset is measured in sprite-local world units before scale/rotation.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

/// <summary>Complete equipment art, including handle; local +X points toward the tip.</summary>
[CreateAssetMenu(fileName = "PaperWeapon", menuName = "Black Cube/Paper Weapon Visual")]
public class PaperWeaponVisual : ScriptableObject
{
    public Sprite sprite;
    [Tooltip("Grip point relative to the sprite pivot, in sprite world units before scaling.")]
    public Vector2 gripOffset;
    public float rotationOffset;
    [Min(0.01f)] public float scale = 1f;
}

[System.Serializable]
public struct PaperWeaponGrip
{
    public Vector2 position;
    public float angle;
    public bool inFront;
}
