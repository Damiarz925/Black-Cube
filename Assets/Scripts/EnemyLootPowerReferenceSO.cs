using System;
using System.Collections.Generic;
using BlackCube.CombatSimulation;
using UnityEngine;

[Serializable]
public sealed class EnemyLootPowerReferenceRow
{
    public int combatLevel;
    public EnemyAI.EnemyRarity rarity;
    public CombatantSnapshot referencePlayer;
    public CombatantSnapshot averageEnemy;
    public int sampleCount;
    public string sourceBuildFingerprint;
}

[CreateAssetMenu(menuName="Black Cube/Balance/Enemy Loot Power Reference")]
public sealed class EnemyLootPowerReferenceSO:ScriptableObject
{
    [Range(0f,1f)] public float offenseWeight=.5f;
    public string productionFingerprint;
    public List<EnemyLootPowerReferenceRow> rows=new();
    static EnemyLootPowerReferenceSO current;
    public static EnemyLootPowerReferenceSO Current
    {
        get
        {
            if(current==null)current=Resources.Load<EnemyLootPowerReferenceSO>("EnemyLootPowerReference");
            return current;
        }
    }
    public bool TryGet(int level,EnemyAI.EnemyRarity rarity,out EnemyLootPowerReferenceRow row)
    {
        row=rows?.Find(x=>x!=null&&x.combatLevel==level&&x.rarity==rarity);
        return row?.referencePlayer!=null&&row.averageEnemy!=null;
    }
    public bool IsStale(string fingerprint)=>string.IsNullOrEmpty(productionFingerprint)
        || !string.Equals(productionFingerprint,fingerprint,StringComparison.Ordinal);
}

public readonly struct EnemyPowerResult
{
    public readonly bool available;
    public readonly float offensePressure,defensePressure,power,actualIncomingDps,
        referenceIncomingDps,actualPlayerDps,referencePlayerDps;
    public EnemyPowerResult(bool available,float offense,float defense,float power,
        float actualIncoming,float referenceIncoming,float actualPlayer,float referencePlayer)
    {this.available=available;offensePressure=offense;defensePressure=defense;this.power=power;
        actualIncomingDps=actualIncoming;referenceIncomingDps=referenceIncoming;
        actualPlayerDps=actualPlayer;referencePlayerDps=referencePlayer;}
    public static EnemyPowerResult Unavailable=>new(false,1f,1f,1f,0f,0f,0f,0f);
}

// Cheap shared power estimate. The reference player and average enemy are explicit,
// editor-authored production snapshots; no combat simulation runs on a kill.
public static class EnemyLootPowerScorer
{
    public static EnemyPowerResult Evaluate(CombatantSnapshot actual,EnemyLootPowerReferenceRow row,float offenseWeight=.5f)
    {
        if(actual==null||row?.referencePlayer==null||row.averageEnemy==null)return EnemyPowerResult.Unavailable;
        var player=row.referencePlayer;var baseline=row.averageEnemy;
        float actualIncoming=ExpectedDps(actual,player),referenceIncoming=ExpectedDps(baseline,player);
        float actualPlayer=ExpectedDps(player,actual),referencePlayer=ExpectedDps(player,baseline);
        if(referenceIncoming<=0f||actualPlayer<=0f||referencePlayer<=0f||baseline.maximumLife<=0f)
            return EnemyPowerResult.Unavailable;
        float offense=Mathf.Max(.001f,actualIncoming/referenceIncoming);
        float defense=Mathf.Max(.001f,(actual.maximumLife/actualPlayer)/(baseline.maximumLife/referencePlayer));
        float w=Mathf.Clamp01(offenseWeight);
        return new EnemyPowerResult(true,offense,defense,Mathf.Pow(offense,w)*Mathf.Pow(defense,1f-w),
            actualIncoming,referenceIncoming,actualPlayer,referencePlayer);
    }
    public static float ExpectedDps(CombatantSnapshot attacker,CombatantSnapshot defender)
    {
        if(attacker?.basicDamage==null||defender==null)return 0f;
        var hit=attacker.basicDamage;
        float damage=CombatCalculator.ApplyArmourValue(hit.physical,defender.armour,
            defender.physicalDamageReduction,attacker.physicalPenetration);
        damage+=CombatCalculator.ApplyResistanceValue(hit.fire,defender.fireResistance,
            attacker.firePenetration,defender.maximumResistance);
        damage+=CombatCalculator.ApplyResistanceValue(hit.cold,defender.coldResistance,
            attacker.coldPenetration,defender.maximumResistance);
        damage+=CombatCalculator.ApplyResistanceValue(hit.lightning,defender.lightningResistance,
            attacker.lightningPenetration,defender.maximumResistance);
        damage+=CombatCalculator.ApplyResistanceValue(hit.voidDamage,defender.voidResistance,
            attacker.voidPenetration,defender.maximumResistance);
        float crit=1f+Mathf.Clamp01(attacker.critChance)*Mathf.Max(0f,attacker.critMultiplier-1f);
        return Mathf.Max(0f,damage)*Mathf.Max(0f,attacker.attackSpeed)*crit*
            (1f+Mathf.Clamp01(attacker.hitTwiceChance));
    }
    public static EnemyPowerResult Evaluate(EnemyAI enemy,EnemyLootPowerReferenceSO profile=null)
    {
        profile??=EnemyLootPowerReferenceSO.Current;
        if(enemy==null||profile==null||!profile.TryGet(enemy.EnemyLevel,enemy.CurrentRarity,out var row))
            return EnemyPowerResult.Unavailable;
        var stats=enemy.GetComponent<StatsComponent>();var health=enemy.GetComponent<HealthComponent>();
        if(stats==null||health==null)return EnemyPowerResult.Unavailable;
        var actual=new CombatantSnapshot
        {
            maximumLife=health.MaxLife,attackSpeed=enemy.GetFinalAttackSpeed(),
            critChance=enemy.GetFinalCritChance(),critMultiplier=CombatCalculator.BaseCriticalMultiplier+stats.GetStat(StatTypes.CritMult),
            hitTwiceChance=stats.GetStat(StatTypes.ChanceToHitTwice),
            armour=stats.GetStat(StatTypes.FlatArmour)*(1f+stats.GetStat(StatTypes.ArmourPercent)),
            physicalDamageReduction=stats.GetStat(StatTypes.PhysicalDamageReduction),
            fireResistance=stats.GetStat(StatTypes.FireRes)+stats.GetStat(StatTypes.AllRes),
            coldResistance=stats.GetStat(StatTypes.ColdRes)+stats.GetStat(StatTypes.AllRes),
            lightningResistance=stats.GetStat(StatTypes.LightRes)+stats.GetStat(StatTypes.AllRes),
            voidResistance=stats.GetStat(StatTypes.VoidRes)
        };
        var context=enemy.BuildNonCriticalAttackContext();
        if(context.Hits!=null)foreach(var hit in context.Hits)switch(hit.Element)
        {
            case Element.Phys:actual.basicDamage.physical+=hit.Amount;break;
            case Element.Fire:actual.basicDamage.fire+=hit.Amount;break;
            case Element.Cold:actual.basicDamage.cold+=hit.Amount;break;
            case Element.Light:actual.basicDamage.lightning+=hit.Amount;break;
            default:actual.basicDamage.voidDamage+=hit.Amount;break;
        }
        return Evaluate(actual,row,profile.offenseWeight);
    }
}
