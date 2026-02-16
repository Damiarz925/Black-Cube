using UnityEngine;

//Serializable data container RolledMod, used to store stat types, tier index, and value for the rolled mod.
[System.Serializable]
public class RolledMod
{
    public StatTypes statType; //The type of stat that it is (from the StatTypes enum)
    public int tierIndex;   //The index of the tier (generally T4 being the highest, T1 being the lowest)
    public float value;     //The value of the modifier.

    public RolledMod(StatTypes statType, int tierIndex, float value) //Constructor used to initialize a rolled modifier with its stat type, tier, and final value
    {
        this.statType = statType;   
        this.tierIndex = tierIndex;
        this.value = value;
    }
}
