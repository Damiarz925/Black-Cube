// Developer map: One resolved affix: StatTypes key, one-based tier and raw numeric roll. Gear routes this value into local weapon fields or global modifiers.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

//Serializable data container RolledMod, used to store stat types, tier index, and value for the rolled mod.
[System.Serializable]
public class RolledMod
{
    public StatTypes statType; //The type of stat that it is (from the StatTypes enum)
    public int tierIndex;   //The index of the tier (generally T4 being the highest, T1 being the lowest)
    public float value;     //The value of the modifier.
    // Paired local flat weapon damage retains both independently rolled ends.
    // Legacy scalar rolls have hasSecondaryValue=false and therefore mean X-X.
    public float secondaryValue;
    public bool hasSecondaryValue;
    public float HighValue => hasSecondaryValue ? secondaryValue : value;
    [Tooltip("The first random modifier on an item/relic is permanent and cannot be rerolled or removed.")]
    public bool lockedOriginal;

    public RolledMod(StatTypes statType, int tierIndex, float value, bool lockedOriginal = false) //Constructor used to initialize a rolled modifier with its stat type, tier, and final value
    {
        this.statType = statType;   
        this.tierIndex = tierIndex;
        this.value = value;
        this.lockedOriginal = lockedOriginal;
    }

    public RolledMod(StatTypes statType, int tierIndex, float minimum, float maximum, bool lockedOriginal)
        : this(statType, tierIndex, minimum, lockedOriginal)
    {
        secondaryValue = maximum;
        hasSecondaryValue = true;
    }
}
