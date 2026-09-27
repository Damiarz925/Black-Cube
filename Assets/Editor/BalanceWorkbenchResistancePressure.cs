using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class ResistanceTierReference
    {
        public StatTypes stat;
        public LootManager.GearType slot;
        public int tier,itemLevel;
        public float midpoint;
    }

    [Serializable] public sealed class ResistanceProgressionPoint
    {
        public int itemLevel;
        public float fire,cold,lightning,voidResistance;
    }

    [Serializable] public sealed class ResistancePressureResult
    {
        public int allT1AvailableAt,referenceInvestmentAvailableAt;
        public bool reachesTarget;
        public float target=.75f;
        public List<ResistanceTierReference> t1Options=new(),referenceInvestment=new();
        public List<ResistanceProgressionPoint> earlierTiers=new();
        public string assumptions,warning;
    }

    // Read-only investment-pressure analysis. It never reserves affix slots in
    // the actual player optimizer and never invents lower-level targets.
    public static class ResistancePressureAnalyzer
    {
        public static void RunReport()
        {
            try
            {
                using var session=new WorkbenchSession();
                var result=Analyze(session.Roller.Database);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/RealPlayerResistancePressure.json",
                    JsonUtility.ToJson(result,true));
                EditorApplication.Exit(0);
            }
            catch(Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }
        static readonly StatTypes[] Families=
        {StatTypes.FireRes,StatTypes.ColdRes,StatTypes.LightRes,StatTypes.VoidRes,StatTypes.AllRes};
        static readonly StatTypes[] Required=
        {StatTypes.FireRes,StatTypes.ColdRes,StatTypes.LightRes,StatTypes.VoidRes};

        public static ResistancePressureResult Analyze(ModDatabase database,float target=.75f)
        {
            if(database==null)throw new ArgumentNullException(nameof(database));
            var result=new ResistancePressureResult{target=target,
                assumptions="Rare-quality reference: at most two Suffixes per item; one copy of an affix family per item. T1 and earlier values are tier-range midpoints from production affix data. All Elemental Resistance covers Fire/Cold/Lightning, not Void. This is investment pressure, not gear-slot reservation."};
            var pools=GearStatLists.BuildDefaultStatPools();
            foreach(var pair in pools)
            foreach(var stat in Families)
            {
                if(!pair.Value.Contains(stat)||AffixPolicy.Side(stat)!=AffixSide.Suffix)continue;
                var definition=database.GetDefinition(stat);
                var tier=ModManager.ApplicableTiers(definition,pair.Key)
                    .FirstOrDefault(x=>x.tierIndex==1);
                if(tier==null)continue;
                result.t1Options.Add(new ResistanceTierReference{stat=stat,slot=pair.Key,
                    tier=1,itemLevel=tier.minItemLevel,
                    midpoint=(tier.minValue+tier.maxValue)*.005f});
            }
            foreach(var stat in Required)
                if(!result.t1Options.Any(x=>x.stat==stat))
                {
                    result.warning="Missing legal T1 option for "+stat;
                    return result;
                }
            result.allT1AvailableAt=Required.Max(stat=>result.t1Options
                .Where(x=>x.stat==stat).Min(x=>x.itemLevel));
            // A small legal greedy cover measures reference affix pressure. The
            // actual character search still evaluates random complete gearsets.
            var totals=new float[4];
            var capacity=new Dictionary<LootManager.GearType,int>();
            var used=new HashSet<string>(StringComparer.Ordinal);
            for(int step=0;step<24;step++)
            {
                ResistanceTierReference best=null;
                float bestGain=0f;
                foreach(var candidate in result.t1Options)
                {
                    if(capacity.GetValueOrDefault(candidate.slot)>=AffixPolicy.MaximumOnSide(LootManager.GearRarity.Rare)
                        ||used.Contains(candidate.slot+"/"+candidate.stat))continue;
                    float gain=Gain(candidate.stat,candidate.midpoint,totals,target);
                    if(gain>bestGain){bestGain=gain;best=candidate;}
                }
                if(best==null||bestGain<=0f)break;
                result.referenceInvestment.Add(best);
                capacity[best.slot]=capacity.GetValueOrDefault(best.slot)+1;
                used.Add(best.slot+"/"+best.stat);
                Apply(best.stat,best.midpoint,totals);
                if(totals.All(x=>x>=target-.00001f))break;
            }
            result.reachesTarget=totals.All(x=>x>=target-.00001f);
            result.referenceInvestmentAvailableAt=result.referenceInvestment.Count==0?0:
                result.referenceInvestment.Max(x=>x.itemLevel);
            if(!result.reachesTarget)
                result.warning="Legal rare-quality T1 midpoint affixes alone did not cover all four 75% targets in this greedy reference. Passives, other item quality, or a different legal combination may alter the result.";
            for(int level=1;level<=100;level++)
            {
                if(level!=1&&level!=100&&level%10!=0&&level!=result.allT1AvailableAt&&
                    level!=result.referenceInvestmentAvailableAt)continue;
                var atLevel=new float[4];
                foreach(var investment in result.referenceInvestment)
                {
                    var definition=database.GetDefinition(investment.stat);
                    var available=ModManager.ApplicableTiers(definition,investment.slot)
                        .Where(x=>x.minItemLevel<=level)
                        .OrderBy(x=>x.tierIndex).FirstOrDefault();
                    if(available!=null)Apply(investment.stat,
                        (available.minValue+available.maxValue)*.005f,atLevel);
                }
                result.earlierTiers.Add(new ResistanceProgressionPoint{itemLevel=level,
                    fire=Math.Min(target,atLevel[0]),cold=Math.Min(target,atLevel[1]),
                    lightning=Math.Min(target,atLevel[2]),voidResistance=Math.Min(target,atLevel[3])});
            }
            return result;
        }

        static float Gain(StatTypes stat,float value,float[] current,float target)
        {
            float gain=0f;
            for(int i=0;i<4;i++)if(Affects(stat,i))
                gain+=Math.Max(0f,Math.Min(value,target-current[i]));
            return gain;
        }
        static void Apply(StatTypes stat,float value,float[] current)
        {for(int i=0;i<4;i++)if(Affects(stat,i))current[i]+=value;}
        static bool Affects(StatTypes stat,int index)=>
            (stat==StatTypes.AllRes&&index<3)||(index switch
            {0=>stat==StatTypes.FireRes,1=>stat==StatTypes.ColdRes,
                2=>stat==StatTypes.LightRes,_=>stat==StatTypes.VoidRes});
    }
}
