using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pure loadout calculations used by isolated Workbench builds (no live singleton reads).
public static class RelicLoadoutRules
{
    public static float Sum(IEnumerable<RelicData> relics,RelicModifierType type)=>(relics??System.Array.Empty<RelicData>()).SelectMany(x=>x.modifiers).Where(x=>x.type==type).Sum(x=>x.value);
    public static float Product(IEnumerable<RelicData> relics,RelicModifierType type)=>(relics??System.Array.Empty<RelicData>()).SelectMany(x=>x.modifiers).Where(x=>x.type==type).Aggregate(1f,(value,x)=>value*(1+x.value/100));
    public static void ApplyStats(IEnumerable<RelicData> relics,StatsComponent stats,object source)
    {
        if(relics==null)return;
        void Add(StatTypes type,float value){if(value!=0)stats.AddModifier(new StatModifier(type,StatOp.Flat,value,source));}
        Add(StatTypes.AllRes,Sum(relics,RelicModifierType.AllResistances));Add(StatTypes.ChanceToHitTwice,Sum(relics,RelicModifierType.ChanceToHitTwice));Add(StatTypes.ProjectileAmount,Sum(relics,RelicModifierType.ProjectileAmount));
        foreach(var relic in relics)if(relic.uniqueRelic)foreach(var mod in relic.forgedStats)
        {
            if(mod.statType==StatTypes.AxePhysicalRage){Add(StatTypes.PhysMult,mod.value);Add(StatTypes.RageGeneration,mod.HighValue);}
            else Add(mod.statType,mod.hasSecondaryValue?(mod.value+mod.HighValue)*.5f:mod.value);
        }
    }
}
