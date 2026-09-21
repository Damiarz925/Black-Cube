using System;
using UnityEngine;

public enum PlayerGearSelectionStrategy { RandomLegal, PercentileTarget, BestPerSlot, FullGearsetBeamSearch }

[CreateAssetMenu(menuName="Black Cube/Balance/Player Gear Profile",fileName="SO_PlayerGearProfile")]
public sealed class PlayerGearProfileSO:ScriptableObject
{
    [TextArea] public string simulationNotice="SIMULATION ASSUMPTIONS — NOT GAME BALANCE";
    [Min(1)] public int version=1;
    [Min(1)] public int candidatesPerSlot=12;
    public PlayerGearSelectionStrategy selectionStrategy=PlayerGearSelectionStrategy.BestPerSlot;
    [Range(0,1)] public float targetObjectivePercentile=.5f;
    [Min(1)] public int candidateRetention=6;
    [Min(1)] public int gearsetBeamWidth=50;
    public LootManager.GearRarity minimumRarity=LootManager.GearRarity.Magic;
    public LootManager.GearRarity maximumRarity=LootManager.GearRarity.Rare;
    public bool useNaturalRarity=true;
    public bool allowNaturalLegendaries;
    public bool requireProductionLegalAffixes=true;
    public bool allowManualSlotLocks=true;
    [Range(0,1)] public float selectionImperfection;
    public bool allowLegalCrafting;
    public bool allowEmpoweredModifiers;
    public bool allowBossSpecialModifiers;
    public bool allowDeepEndgameImplicitRepair;

    public int ResolveItemLevel(int playerLevel,int combatLevel)=>Mathf.Clamp(Mathf.Max(playerLevel,combatLevel),1,100);
}
