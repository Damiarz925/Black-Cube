using UnityEngine;

[CreateAssetMenu(fileName = "PoisonStatus", menuName = "Status Effects/Poison")]
public class PoisonStatusEffect : StatusEffects
{
    [Header("Poison Settings")]
    [SerializeField] private float poisonScalar = 0.10f;
    [SerializeField] private int poisonMaxStacks = 100;

    private void OnValidate()
    {
        ailmentKind = AilmentKind.Poison;
        statusType = StatusType.DamageOverTime;
        stackPolicy = StackPolicy.StackIndependently;
        effectMagnitude = poisonScalar;
        tickDuration = 2;
        baseTurnInterval = 4;
        maxStacks = poisonMaxStacks;
    }
}
