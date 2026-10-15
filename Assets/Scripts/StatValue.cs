using UnityEngine;
using System.Collections.Generic;

public class StatValue
{
    private float baseValue;
    public float BaseValue { get => baseValue; set { baseValue = value; _dirty = true; } }
    private readonly bool additiveMultiplicativePercent;
    private readonly List<StatModifier> _modifiers = new(); //Readonly list of stat modifiers
    private bool _dirty = true; //Indicates whether the stat has been modified
    private float _cachedValue; //Stores the cached value of the stat

    //Function sets the base value field based on the passed in base value, if nothing is passed in, it is set as 0.
    public StatValue(float baseValue = 0f, bool additiveMultiplicativePercent = false)
    {
        this.additiveMultiplicativePercent = additiveMultiplicativePercent;
        BaseValue = baseValue;
    }

    //Adds the passed in StatModifier object to the _modifiers list, and sets dirty to true
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _dirty = true;
    }

    //Removes all modifiers originating from the given source object
    public void RemoveModifiersFromSource(object source)
    {
        if (source == null) return; //Checks whether the passed in source is null, if so, return
        _modifiers.RemoveAll(m => m.Source == source);  //If the modifier's source is the same as the source, call's RemoveAll on the _modifiers list (source is generally gear items, removes mods given to object from source)
        _dirty = true;  //Sets dirty to true
    }

    public float GetValue()
    {
        if (!_dirty) return _cachedValue;   //If dirty is false, return the cached value (the value has not been modified)

        //Defines variables to be used in operations based on the statOp
        float flat = 0f;
        float additive = 0f;
        float multiplicative = 1f;
        float? overrideValue = null; //Create nullable float using float? and set it to null

        //Loop through all of _modifiers, for each modifier
        foreach (var m in _modifiers)
        {
            switch (m.Operation)
            {
                case StatOp.Flat:   //If the operation is flat, flat += the value
                    flat += m.Value;
                    break;
                case StatOp.Additive:   //If the operation is additive, additive += the value
                    additive += m.Value;
                    break;
                case StatOp.Multiplicative:
                    // Damage "more" rolls are stored as percent points. Their raw/UI value
                    // remains the sum; combat preserves each roll through GetPercentMultiplier.
                    if (additiveMultiplicativePercent) additive += m.Value;
                    else multiplicative *= (1f + m.Value);
                    break;
                case StatOp.Override:   //If the operation is override, override is overriden as the value directly
                    overrideValue = m.Value;
                    break;
            }
        }

        //result is the base value plus the flat, multiplied by 1 + additive, and then multiplied by multiplicative.
        float result = BaseValue + flat + additive;
        result *= multiplicative;

        //If overrideValue has a value (Has value is part of Nullable<T>. If it has a value, it is not null), result becomes that value directly, negating all previous math.
        if (overrideValue.HasValue)
            result = overrideValue.Value;

        //Cache the result
        _cachedValue = result;
        //Set Dirty equal to false
        _dirty = false;
        //Return the result
        return result;
    }

    /// <summary>
    /// Converts every contributor to a percent-point stat into its own multiplier.
    /// For example, separate 20 and 10 rolls return 1.20 * 1.10, while GetValue
    /// continues to return 30 for raw/UI display.
    /// </summary>
    public float GetPercentMultiplier()
    {
        float? overrideValue = null;
        foreach (var modifier in _modifiers)
            if (modifier.Operation == StatOp.Override)
                overrideValue = modifier.Value;

        if (overrideValue.HasValue)
            return 1f + overrideValue.Value / 100f;

        float multiplier = BaseValue == 0f ? 1f : 1f + BaseValue / 100f;
        foreach (var modifier in _modifiers)
        {
            if (modifier.Operation != StatOp.Override)
                multiplier *= 1f + modifier.Value / 100f;
        }

        return multiplier;
    }
}
