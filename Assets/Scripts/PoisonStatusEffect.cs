// Developer map: Poison asset defaults and validation. Eligible source damage is selected centrally in AilmentCalculator; stack instances belong to the target.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

[CreateAssetMenu(fileName = "PoisonStatus", menuName = "Status Effects/Poison")]
public class PoisonStatusEffect : StatusEffects
{
    [Header("Poison Settings")]
    [SerializeField] private float poisonScalar = 0.10f;
    [SerializeField] private int poisonMaxStacks = 0; // 0 is unlimited; instances never share a timer.

    private void OnValidate()
    {
        ailmentKind = AilmentKind.Poison;
        elements = ElementMask.Phys | ElementMask.Fire | ElementMask.Cold | ElementMask.Light | ElementMask.Void;
        statusType = StatusType.DamageOverTime;
        stackPolicy = StackPolicy.StackIndependently;
        effectMagnitude = poisonScalar;
        tickDuration = 4; // four ticks over eight global combat turns
        baseTurnInterval = 2;
        maxStacks = poisonMaxStacks;
    }
}
