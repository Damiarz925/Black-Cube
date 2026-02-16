using UnityEngine;

public enum StatOp  //Enum for stat operators, flat, additive, multiplicative, and a full override of the value
{
    Flat,
    Additive,
    Multiplicative,
    Override
}

public struct StatModifier      //struct for stat modifiers and its constructor. Contains stat, operation, value and the source of the modifier.
{
    public StatTypes Stat;
    public StatOp Operation;
    public float Value;
    public object Source;

    public StatModifier(StatTypes stat, StatOp operation, float value, object source = null)
    {
        Stat = stat;
        Operation = operation;
        Value = value;
        Source = source;
    }
}
