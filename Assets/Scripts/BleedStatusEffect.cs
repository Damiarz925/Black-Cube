// Developer map: Bleed asset defaults and editor validation for physical-hit damage over time. Active stacks and ticking belong to StatusController, not this shared asset.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

[CreateAssetMenu(fileName = "BleedStatus", menuName = "Status Effects/Bleed")]
public class BleedStatusEffect : StatusEffects
{
    [Header("Bleed Settings")]
    [SerializeField] private float bleedScalar = 0.50f; //Set the bleed scalar to do half of the hit damage per tick (modifiable in inspector)
    [SerializeField] private int bleedMaxStacks = 5;

    private void OnValidate()
    {
        ailmentKind = AilmentKind.Bleed;
        statusType = StatusType.DamageOverTime;
        stackPolicy = StackPolicy.StackIndependently;
        elements = ElementMask.Phys;
        effectMagnitude = bleedScalar;
        tickDuration = 5; // five ticks over ten afflicted-actor turns
        baseTurnInterval = 2;
        maxStacks = bleedMaxStacks;
    }
}
