using UnityEngine;

[CreateAssetMenu(fileName = "BleedStatus", menuName = "Status Effects/Bleed")]
public class BleedStatusEffect : StatusEffects
{
    [Header("Bleed Settings")]
    [SerializeField] private float bleedScalar = 0.50f; //Set the bleed scalar to do half of the hit damage per tick (modifiable in inspector)
    [SerializeField] private int bleedMaxStacks = 4;    //Set the bleed status effect to have 4 max stacks (modifiable in inspector)

    private void OnValidate()
    {
        ailmentKind = AilmentKind.Bleed;
        statusType = StatusType.DamageOverTime;
        stackPolicy = StackPolicy.StackAndRefresh;
        elements = ElementMask.Phys;
        effectMagnitude = bleedScalar;
        tickDuration = 2;
        baseTurnInterval = 1;
        maxStacks = bleedMaxStacks;
    }
}
