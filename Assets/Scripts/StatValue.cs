// Developer map: Caches one raw stat bucket and invalidates on base/modifier changes. Result is (base + flat + additive) times multiplicative; more-percent buckets sum their modifier points instead.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;

public class StatValue
{
    private float baseValue;
    public float BaseValue { get => baseValue; set { baseValue = value; _dirty = true; } }
    private readonly List<StatModifier> _modifiers = new(); //Readonly list of stat modifiers
    private bool _dirty = true; //Indicates whether the stat has been modified
    private float _cachedValue; //Stores the cached value of the stat
    private readonly bool additiveMorePercent;

    //Function sets the base value field based on the passed in base value, if nothing is passed in, it is set as 0.
    public StatValue(float baseValue = 0f, bool additiveMorePercent = false)
    {
        this.additiveMorePercent = additiveMorePercent;
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
                case StatOp.Multiplicative: //If the operation is multiplicative, multiplicative *= 1 + the value
                    // Damage "more" is a summed percentage bucket, even for legacy Mult operations.
                    if (additiveMorePercent) additive += m.Value;
                    else multiplicative *= (1f + m.Value);
                    break;
                case StatOp.Override:   //If the operation is override, override is overriden as the value directly
                    overrideValue = m.Value;
                    break;
            }
        }

        // Flat and additive values are raw points in this bucket, not a second
        // percentage multiplier. Gameplay formulas convert/apply percent buckets.
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
}
