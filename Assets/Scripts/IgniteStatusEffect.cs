using UnityEngine;

[CreateAssetMenu(fileName = "IgniteStatus", menuName = "Status Effects/Ignite")]
public class IgniteStatusEffect : StatusEffects
{
    [Header("Ignite Settings")]
    [SerializeField] private float igniteScalar = 0.80f;    //Ignite scalar, set by default to have ignites deal 80% of the fire damage that the hit that applied it dealt

    private void OnValidate()
    {
        ailmentKind = AilmentKind.Ignite;
        statusType = StatusType.DamageOverTime;
        stackPolicy = StackPolicy.ReplaceIfStronger;
        elements = ElementMask.Fire;
        effectMagnitude = igniteScalar;
        tickDuration = 2;
        baseTurnInterval = 2;
        maxStacks = 1;
    }
}
