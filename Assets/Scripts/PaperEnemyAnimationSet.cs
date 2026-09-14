// Developer map: Enemy display name, rest/attack/hit sprites and popup anchor shared by prefab and builder. Eight attack poses use index3 impact; hit reactions are visual-only.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

/// <summary>Enemy presentation only; boss role, health and combat cadence remain on gameplay components.</summary>
[CreateAssetMenu(fileName = "EnemyAnimation", menuName = "Black Cube/Paper Enemy Animation")]
public class PaperEnemyAnimationSet : ScriptableObject
{
    public string displayName;
    public Sprite[] idleFrames;
    [Min(0.1f)] public float idleFramesPerSecond = 2f;
    // Exactly eight drawings in cycle order. Index 3 is the damage impact.
    public Sprite[] attackFrames;
    // Optional 4-6 frame visual flinch. DamageReceiver starts it after life is
    // deducted; PaperSpriteActor never changes combat timing or damage.
    public Sprite[] hitFrames;
    [Range(0.18f, 0.24f)] public float hitReactionDuration = 0.21f;
    public Vector3 popupOffset = new Vector3(0f, 3.5f, 0f);
}
