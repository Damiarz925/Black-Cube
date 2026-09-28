using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stable class-keystone definitions. Parameters are editable in the tuning SO;
// the framework is production, while the initial balance numbers are provisional.
[Serializable] public sealed class ClassKeystoneDefinition
{
    public PassiveKeystone id;public string classId,name,description;public float primary,secondary,tertiary;
    public string StableId=>"tree.v3."+classId+".keystone."+id.ToString().ToLowerInvariant();
    public ClassKeystoneDefinition(PassiveKeystone key,string route,string label,string text,float a,float b=0,float c=0){id=key;classId=route;name=label;description=text;primary=a;secondary=b;tertiary=c;}
}
public static class ClassKeystoneCatalog
{
    static ClassKeystoneTuningSO tuning;
    static readonly List<ClassKeystoneDefinition> defaults=new()
    {
        new(PassiveKeystone.WarriorBleed,PlayerClassIds.Warrior,"Blood Reservoir","35% less Bleed Damage; double maximum Bleed stack cap, not applications.",.65f,2),
        new(PassiveKeystone.WarriorTempo,PlayerClassIds.Warrior,"Unbroken Tempo","15% MORE Attack Speed; multiplicative after increased Attack Speed.",1.15f),
        new(PassiveKeystone.WarriorConsolidation,PlayerClassIds.Warrior,"One Decisive Strike","Consolidate rolled melee Multistrikes into the original hit at 110% of each would-be extra strike. No repeated triggers.",1.10f),
        new(PassiveKeystone.BarbarianFullRage,PlayerClassIds.Barbarian,"Crown of Fury","Gain Rage with any weapon. Triple constant decay. At maximum Rage, gain 30% MORE Damage in addition to ordinary Rage bonuses.",1.30f),
        new(PassiveKeystone.BarbarianFire,PlayerClassIds.Barbarian,"Molten Edge","Resolve scaled Physical hit damage as Fire; +1 maximum Ignite stack. Eruption remains separate.",1),
        new(PassiveKeystone.BarbarianRecovery,PlayerClassIds.Barbarian,"Blood Engine","No Life Regeneration. Recover half the would-be max-Life/sec regeneration percentage from actual damage dealt; 20% MORE Damage.",.5f,1.2f),
        new(PassiveKeystone.RangerSplit,PlayerClassIds.Ranger,"Threefold Flight","Each projectile is replaced once by three independent projectiles dealing 33% each; 15% MORE Projectile Damage.",3,.33f,1.15f),
        new(PassiveKeystone.RangerEndlessPoison,PlayerClassIds.Ranger,"Endless Venom","40% less Poison Damage; Poison lasts until target death. Each duration-derived additional tick grants 5% MORE Poison Damage.",.6f,.05f),
        new(PassiveKeystone.RangerPrecision,PlayerClassIds.Ranger,"Risky Precision","Projectiles always Precision hit if they land; independent 15% miss chance; 15% MORE Precision Hit Damage.",.15f,1.15f),
        new(PassiveKeystone.MageFire,PlayerClassIds.Mage,"Fire Commitment","Discard non-Fire outgoing damage at resolution; 50% MORE Fire Damage.",1.5f),
        new(PassiveKeystone.MageSelfBolt,PlayerClassIds.Mage,"Storm's Price","Once per spell cast: enemy and incoming self Lightning bolts at 35% of the primary single-hit basis. No Crit or recursion; self-hit grants no offensive on-hit effects.",.35f),
        new(PassiveKeystone.MageShatter,PlayerClassIds.Mage,"Stormglass","Freeze at 75% Chill Effect. Lightning against Frozen targets deals 1.5x Lightning damage, consumes Freeze and Shatters for 10% target max Life as Cold.",.75f,1.5f,.10f),
        new(PassiveKeystone.PriestSacrifice,PlayerClassIds.Priest,"Ailment Sacrifice","Cannot apply Poison, Bleed or Ignite. Each distinct type a hit successfully would apply loses 5% target maximum Life; at most 15% per hit. Counts for damage-based healing.",.05f),
        new(PassiveKeystone.PriestAura,PlayerClassIds.Priest,"Prismatic Discipline","20% Aura Effect per active aura. Hits with more than two damage types randomly retain only two types, independently each hit.",.2f,2),
        new(PassiveKeystone.PriestFracture,PlayerClassIds.Priest,"Deep Fracture","Freeze only at 200% Chill Effect for one attack opportunity, then permanently Fracture: 25% more damage taken, no further Freeze or Shatter.",2,1.25f),
        new(PassiveKeystone.ThiefOpener,PlayerClassIds.Thief,"First Blood","40% MORE Damage against full-health enemies; 30% LESS against any injured enemy.",1.4f,.7f),
        new(PassiveKeystone.ThiefAilmentCrit,PlayerClassIds.Thief,"Marked Weakness","+5 percentage points base Crit per unique Poison, Bleed, Ignite, Shock or Chill on the target, before increased Crit scaling.",.05f),
        new(PassiveKeystone.ThiefStealth,PlayerClassIds.Thief,"Vanishing Blade","15% MORE generic damage. Kill grants non-stacking Stealth: next enemy's first attack has 30% miss chance, consumed whether it hits or misses.",1.15f,.3f)
    };
    public static IReadOnlyList<ClassKeystoneDefinition> All {get{if(tuning==null)tuning=Resources.Load<ClassKeystoneTuningSO>("GameData/PassiveTree/SO_ClassKeystoneTuning");return tuning!=null&&tuning.definitions.Count==18?tuning.definitions:defaults;}}
    public static IEnumerable<ClassKeystoneDefinition> ForClass(string id)=>All.Where(x=>x.classId==id);
    public static ClassKeystoneDefinition Get(PassiveKeystone id)=>All.FirstOrDefault(x=>x.id==id);
    public static List<ClassKeystoneDefinition> CreateDefaults()=>defaults.Select(x=>new ClassKeystoneDefinition(x.id,x.classId,x.name,x.description,x.primary,x.secondary,x.tertiary)).ToList();
}
