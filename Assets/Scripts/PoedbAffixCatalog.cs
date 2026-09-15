// Ordinary PoE1 analogues captured 2026-09-15; Cold low-row hybrid is a
// documented Black-Cube deviation in Docs/POEDB_AFFIX_BASELINE.md.
// No Essence/influenced/crafted tiers are included. Empty list means disallowed slot.
using System.Collections.Generic;

public static class PoedbAffixCatalog
{
    static readonly LootManager.GearType W=LootManager.GearType.Weapons;
    static readonly LootManager.GearType H=LootManager.GearType.Helmets;
    static readonly LootManager.GearType B=LootManager.GearType.BodyArmours;
    static readonly LootManager.GearType G=LootManager.GearType.Gloves;
    static readonly LootManager.GearType T=LootManager.GearType.Boots;
    static readonly LootManager.GearType R=LootManager.GearType.Rings;
    static readonly LootManager.GearType A=LootManager.GearType.Amulets;
    static readonly LootManager.GearType L=LootManager.GearType.Belts;

    static List<AffixTier> Scalar(params int[] rows)
    {
        var tiers=new List<AffixTier>();
        for(int i=0;i<rows.Length;i+=3)
            tiers.Add(new AffixTier{minItemLevel=rows[i],minValue=rows[i+1],maxValue=rows[i+2],weight=1});
        for(int i=0;i<tiers.Count;i++)tiers[i].tierIndex=tiers.Count-i;
        return tiers;
    }
    static List<AffixTier> Pair(params int[] rows)
    {
        var tiers=new List<AffixTier>();
        for(int i=0;i<rows.Length;i+=5)
            tiers.Add(new AffixTier{minItemLevel=rows[i],minValue=rows[i+1],maxValue=rows[i+2],
                pairedDamage=true,minHighValue=rows[i+3],maxHighValue=rows[i+4],weight=1});
        for(int i=0;i<tiers.Count;i++)tiers[i].tierIndex=tiers.Count-i;
        return tiers;
    }
    static List<AffixTier> Through(List<AffixTier> source,int maximumLevel)
    {
        var result=source.FindAll(x=>x.minItemLevel<=maximumLevel);
        for(int i=0;i<result.Count;i++)result[i]=new AffixTier{minItemLevel=result[i].minItemLevel,
            minValue=result[i].minValue,maxValue=result[i].maxValue,weight=result[i].weight,
            pairedDamage=result[i].pairedDamage,minHighValue=result[i].minHighValue,
            maxHighValue=result[i].maxHighValue,tierIndex=result.Count-i};
        return result;
    }
    static bool Slot(LootManager.GearType slot,params LootManager.GearType[] allowed)
    {
        foreach(var x in allowed)if(slot==x)return true;
        return false;
    }

    public static bool TryGet(StatTypes stat,LootManager.GearType slot,out List<AffixTier> tiers)
    {
        tiers=null;
        switch(stat)
        {
            case StatTypes.FireRes:
                tiers=Slot(slot,H,B,G,T,R,A,L)?Scalar(1,6,11,12,12,17,24,18,23,36,24,29,
                    48,30,35,60,36,41,72,42,45,84,46,48):new();return true;
            case StatTypes.LightRes:
                tiers=Slot(slot,H,B,G,T,R,A,L)?Scalar(1,6,11,13,12,17,25,18,23,37,24,29,
                    49,30,35,60,36,41,72,42,45,84,46,48):new();return true;
            case StatTypes.ColdRes:
            case StatTypes.VoidRes:
                tiers=Slot(slot,H,B,G,T,R,A,L)?Scalar(1,6,11,14,12,17,26,18,23,38,24,29,
                    50,30,35,60,36,41,72,42,45,84,46,48):new();return true;
            case StatTypes.AllRes:
                tiers=Slot(slot,A,R,L)?Scalar(12,3,5,24,6,8,36,9,11,48,12,14,
                    60,15,16,85,17,18):new();return true;
            case StatTypes.Life:
                if(!Slot(slot,H,B,G,T,A,R,L)){tiers=new();return true;}
                var life=Scalar(1,3,9,5,10,24,11,25,39,18,40,54,24,55,69,
                    30,70,84,36,85,99,44,100,114,54,115,129,64,130,144,
                    73,145,159,81,160,174,86,175,189);
                tiers=slot==B?life:slot==H||slot==L?Through(life,64):slot==R?Through(life,44):Through(life,54);
                return true;
            case StatTypes.Mana:
                if(!Slot(slot,A,R,L)){tiers=new();return true;}
                var mana=Scalar(1,15,19,11,20,24,17,25,29,23,30,34,29,35,39,
                    35,40,44,42,45,49,51,50,54,60,55,59,69,60,64,
                    75,65,68,81,69,73,85,74,78);
                tiers=slot==L?Through(mana,75):mana;return true;
            case StatTypes.Strength:
            case StatTypes.Dexterity:
            case StatTypes.Intelligence:
                bool allowed=stat==StatTypes.Strength?Slot(slot,W,A,R,L)
                    :stat==StatTypes.Dexterity?Slot(slot,W,A,R,G):Slot(slot,W,A,R,H);
                if(!allowed){tiers=new();return true;}
                var attributes=Scalar(1,8,12,11,13,17,22,18,22,33,23,27,
                    44,28,32,55,33,37,66,38,42,74,43,50,82,51,55);
                bool special=stat==StatTypes.Strength&&slot==L||stat==StatTypes.Dexterity&&slot==G
                    ||stat==StatTypes.Intelligence&&slot==H;
                if(special)attributes.Add(new AffixTier{minItemLevel=85,minValue=56,maxValue=60,weight=1});
                for(int i=0;i<attributes.Count;i++)attributes[i].tierIndex=attributes.Count-i;
                tiers=attributes;return true;
            case StatTypes.ArmourPercent:
                if(!Slot(slot,H,B,G,T)){tiers=new();return true;}
                var armour=Scalar(3,15,26,17,27,42,29,43,55,42,56,67,
                    60,68,79,72,80,91,84,92,100);
                if(slot==B)armour.Add(new AffixTier{minItemLevel=86,minValue=101,maxValue=110,weight=1});
                for(int i=0;i<armour.Count;i++)armour[i].tierIndex=armour.Count-i;
                tiers=armour;return true;
            case StatTypes.LifeOnKill:
                tiers=Scalar(1,7,10,23,12,18,40,24,32,52,35,44,66,56,72,81,84,110);return true;
            case StatTypes.ManaOnKill:
                tiers=Scalar(24,7,10,40,11,15,52,16,25,66,26,37,81,38,50);return true;
            case StatTypes.PhysDmg:
                if(slot!=W)return false; // existing nonweapon identity keeps its authored values
                tiers=Scalar(1,40,49,11,50,64,23,65,84,35,85,109,
                    46,110,134,60,135,154,73,155,169,83,170,179);return true;
            case StatTypes.AttackSpeed:
                if(slot!=W)return false;
                tiers=Scalar(1,5,7,11,8,10,22,11,13,30,14,16,
                    37,17,19,45,20,22,60,23,25,77,26,27);return true;
            case StatTypes.FlatPhys:
                if(slot!=W)return false;
                tiers=Pair(2,1,2,2,3,13,4,5,8,9,21,6,9,13,15,
                    29,8,12,17,20,36,11,14,21,25,46,13,18,27,31,
                    54,16,21,32,38,65,19,25,39,45,77,22,29,45,52);return true;
            case StatTypes.FlatFire:
                if(slot!=W)return false;
                tiers=Pair(1,1,2,3,4,11,8,10,15,18,18,12,17,25,29,
                    26,17,24,35,41,33,24,33,49,57,42,34,46,68,80,
                    51,46,62,93,107,62,59,81,120,140,74,74,101,150,175,
                    82,89,121,180,210);return true;
            case StatTypes.FlatCold:
            case StatTypes.FlatVoid:
                if(slot!=W)return false;
                tiers=Pair(2,2,3,6,7,12,12,17,26,30,19,11,15,23,26,
                    27,16,21,31,37,34,22,30,44,51,43,31,42,62,71,
                    52,41,57,83,97,63,54,74,108,126,75,68,92,136,157,
                    82,81,111,163,189);return true;
            case StatTypes.FlatLight:
                if(slot!=W)return false;
                tiers=Pair(3,1,1,5,6,13,2,2,25,29,19,2,2,41,48,
                    31,3,3,57,67,34,4,5,80,94,42,5,8,112,131,
                    51,8,10,152,176,63,10,14,197,229,74,13,17,247,286,
                    82,15,21,296,344);return true;
            default:return false;
        }
    }
}
