// Developer map: Shared serialized player idle/attack frames and matching hand anchors. Eight attack slots follow PaperSpriteActor impact conventions; idle speed is frames/second.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

/// <summary>Weapon-free body cels and matching attachment poses, shared by prefab and scene builder.</summary>
[CreateAssetMenu(fileName = "PlayerAnimation", menuName = "Black Cube/Paper Player Animation")]
public class PaperPlayerAnimationSet : ScriptableObject
{
    public Sprite[] idleFrames;
    [Min(0.1f)] public float idleFramesPerSecond = 2f;
    public Sprite[] attackFrames;
    public PaperWeaponGrip[] idleWeaponGrips;
    public PaperWeaponGrip[] attackWeaponGrips;
    public PaperWeaponVisual defaultWeaponVisual;
}
