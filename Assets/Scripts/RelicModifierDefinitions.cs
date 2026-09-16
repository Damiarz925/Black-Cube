// Persistence-stable Relic modifier pool with variable-length level-gated tiers.
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RelicModifierTier
{
    public int TierIndex { get; }
    public int MinimumRelicLevel { get; }
    public float Minimum { get; }
    public float Maximum { get; }
    public int Weight { get; }
    public bool FixedValue => Minimum == Maximum;
    public RelicModifierTier(int tierIndex,int minimumLevel,float minimum,float maximum,int weight)
    {TierIndex=tierIndex;MinimumRelicLevel=minimumLevel;Minimum=minimum;Maximum=maximum;Weight=weight;}
}

public sealed class RelicModifierDefinition
{
    public RelicModifierType Id { get; }
    public string Label { get; }
    public int Weight { get; }
    public bool Percent { get; }
    public IReadOnlyList<RelicModifierTier> Tiers { get; }
    public RelicModifierDefinition(RelicModifierType id,string label,int weight,bool percent,params RelicModifierTier[] tiers)
    {if(weight<=0||tiers==null||tiers.Length==0)throw new ArgumentOutOfRangeException(nameof(weight));Id=id;Label=label;Weight=weight;Percent=percent;Tiers=tiers;}
    public RelicModifierTier Tier(int tierIndex)=>Tiers.FirstOrDefault(t=>t.TierIndex==tierIndex);
}

public static class RelicModifierDefinitions
{
    static RelicModifierTier T(int index,float min,float max)=>new(index,index switch{5=>1,4=>20,3=>40,2=>60,_=>80},min,max,index switch{5=>50,4=>35,3=>20,2=>10,_=>5});
    static RelicModifierTier One(float value)=>new(5,1,value,value,50);
    static readonly RelicModifierDefinition[] Definitions=
    {
        new(RelicModifierType.MoreDamage,"More Damage",12,true,T(5,3,5),T(4,4,7),T(3,5,10),T(2,8,13),T(1,11,16)),
        new(RelicModifierType.IncreasedExperience,"Increased Experience Gained",12,true,T(5,6,10),T(4,8,14),T(3,10,20),T(2,16,25),T(1,22,30)),
        new(RelicModifierType.MoreAttackSpeed,"More Attack Speed",12,true,T(5,2,4),T(4,3,6),T(3,4,8),T(2,6,10),T(1,8,12)),
        new(RelicModifierType.IncreasedMaximumLife,"Increased Maximum Life",12,true,T(5,5,8),T(4,7,11),T(3,8,15),T(2,12,18),T(1,16,22)),
        new(RelicModifierType.IncreasedMaximumMana,"Increased Maximum Mana",12,true,T(5,5,8),T(4,7,11),T(3,8,15),T(2,12,18),T(1,16,22)),
        new(RelicModifierType.AllResistances,"All Resistances",12,true,T(5,3,5),T(4,4,7),T(3,5,10),T(2,8,13),T(1,11,16)),
        new(RelicModifierType.IncreasedVoidDamage,"Increased Void Damage",12,true,T(5,9,15),T(4,12,21),T(3,15,30),T(2,24,38),T(1,32,46)),
        new(RelicModifierType.IncreasedAilmentDamage,"Increased Ailment Damage",12,true,T(5,9,15),T(4,12,21),T(3,15,30),T(2,24,38),T(1,32,46)),
        new(RelicModifierType.ChanceToHitTwice,"Chance to Hit Twice",3,true,T(5,3,5),T(4,4,7),T(3,5,10),T(2,8,13),T(1,11,16)),
        new(RelicModifierType.ProjectileAmount,"Projectile Amount",2,false,One(1)),
        new(RelicModifierType.MaximumBleedStacks,"Maximum Bleed Stacks",2,false,One(1)),
        new(RelicModifierType.MaximumIgniteStacks,"Maximum Ignite Stacks",2,false,One(1)),
        new(RelicModifierType.EquippedSkillLevel,"Equipped Active Skill Level",2,false,One(1)),
        new(RelicModifierType.ShockThresholdReduction,"Fewer Shock Stacks for Trigger",2,false,One(1)),
        new(RelicModifierType.MaximumChillSlow,"Maximum Chill Slow",2,true,One(5)),
        new(RelicModifierType.StarterWeaponFire,"Starting Weapon Becomes Fire",4,false,One(1)),
        new(RelicModifierType.StarterWeaponCold,"Starting Weapon Becomes Cold",4,false,One(1)),
        new(RelicModifierType.StarterWeaponLightning,"Starting Weapon Becomes Lightning",4,false,One(1)),
        new(RelicModifierType.StarterWeaponVoid,"Starting Weapon Becomes Void",4,false,One(1)),
        new(RelicModifierType.StarterWeaponBaseDamage,"Starting Weapon Base Damage",8,true,T(5,5,10),T(4,10,18),T(3,18,28),T(2,28,40),T(1,40,55)),
        new(RelicModifierType.StarterWeaponItemLevel,"Starting Weapon Item Level",6,false,T(5,5,5),T(4,10,10),T(3,20,20),T(2,35,35),T(1,50,50)),
        new(RelicModifierType.StarterWeaponLegendaryChance,"Starting Weapon Legendary Chance",5,true,T(5,5,5),T(4,10,10),T(3,15,15),T(2,20,20),T(1,30,30))
    };
    static readonly Dictionary<RelicModifierType,RelicModifierDefinition> ById=BuildLookup();
    public static IReadOnlyList<RelicModifierDefinition> All=>Definitions;
    public static RelicModifierDefinition Get(RelicModifierType id)=>ById.TryGetValue(id,out var definition)?definition:null;
    static Dictionary<RelicModifierType,RelicModifierDefinition> BuildLookup(){var lookup=new Dictionary<RelicModifierType,RelicModifierDefinition>();foreach(var definition in Definitions)if(!lookup.TryAdd(definition.Id,definition))throw new InvalidOperationException("Duplicate relic modifier ID: "+definition.Id);foreach(RelicModifierType id in Enum.GetValues(typeof(RelicModifierType)))if(!lookup.ContainsKey(id))throw new InvalidOperationException("Missing relic modifier ID: "+id);return lookup;}
}
