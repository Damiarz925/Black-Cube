// First-party, persistence-stable Relic modifier pool; values are provisional until Step 13.
using System;
using System.Collections.Generic;

public sealed class RelicModifierDefinition
{
    public RelicModifierType Id { get; }
    public string Label { get; }
    public float Minimum { get; }
    public float Maximum { get; }
    public int Weight { get; }
    public bool FixedValue => Minimum==Maximum;
    public bool Percent { get; }
    public RelicModifierDefinition(RelicModifierType id,string label,float minimum,float maximum,int weight,bool percent=true)
    {
        if(weight<=0||minimum>maximum)throw new ArgumentOutOfRangeException(nameof(weight));
        Id=id;Label=label;Minimum=minimum;Maximum=maximum;Weight=weight;Percent=percent;
    }
}

public static class RelicModifierDefinitions
{
    static readonly RelicModifierDefinition[] Definitions=
    {
        new(RelicModifierType.MoreDamage,"More Damage",5,10,12),
        new(RelicModifierType.IncreasedExperience,"Increased Experience Gained",10,20,12),
        new(RelicModifierType.MoreAttackSpeed,"More Attack Speed",4,8,12),
        new(RelicModifierType.IncreasedMaximumLife,"Increased Maximum Life",8,15,12),
        new(RelicModifierType.IncreasedMaximumMana,"Increased Maximum Mana",8,15,12),
        new(RelicModifierType.AllResistances,"All Resistances",5,10,12),
        new(RelicModifierType.IncreasedVoidDamage,"Increased Void Damage",15,30,12),
        new(RelicModifierType.IncreasedAilmentDamage,"Increased Ailment Damage",15,30,12),
        new(RelicModifierType.ChanceToHitTwice,"Chance to Hit Twice",5,10,3),
        new(RelicModifierType.ProjectileAmount,"Projectile Amount",1,1,2,false),
        new(RelicModifierType.MaximumBleedStacks,"Maximum Bleed Stacks",1,1,2,false),
        new(RelicModifierType.MaximumIgniteStacks,"Maximum Ignite Stacks",1,1,2,false),
        new(RelicModifierType.EquippedSkillLevel,"Equipped Active Skill Level",1,1,2,false),
        new(RelicModifierType.ShockThresholdReduction,"Fewer Shock Stacks for Trigger",1,1,2,false),
        new(RelicModifierType.MaximumChillSlow,"Maximum Chill Slow",5,5,2)
    };
    static readonly Dictionary<RelicModifierType,RelicModifierDefinition> ById=BuildLookup();
    public static IReadOnlyList<RelicModifierDefinition> All=>Definitions;
    public static RelicModifierDefinition Get(RelicModifierType id)=>ById.TryGetValue(id,out var definition)?definition:null;
    static Dictionary<RelicModifierType,RelicModifierDefinition> BuildLookup()
    {
        var lookup=new Dictionary<RelicModifierType,RelicModifierDefinition>();
        foreach(var definition in Definitions)if(!lookup.TryAdd(definition.Id,definition))throw new InvalidOperationException("Duplicate relic modifier ID: "+definition.Id);
        foreach(RelicModifierType id in Enum.GetValues(typeof(RelicModifierType)))if(!lookup.ContainsKey(id))throw new InvalidOperationException("Missing relic modifier ID: "+id);
        return lookup;
    }
}
