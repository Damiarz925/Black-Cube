using UnityEngine;

public enum StatTypes
{
    //Damage Mods
    WeaponBaseDmg = 0,
    WeaponBaseAttackSpeed = 1,
    WeaponBaseCrit = 2,
    ChanceToBlock = 3,

    FlatPhys = 4,
    FlatCold = 5,
    FlatLight = 6,
    FlatFire = 7,

    GenericDmg = 8,
    GenericMult = 9,
    GenericDotMult = 10,

    CritChance = 11,
    CritMult = 12,
    BaseCritChance = 13,

    PhysDmg = 14,
    ColdDmg = 15,
    LightDmg = 16,
    FireDmg = 17,

    PhysMult = 18,
    ColdMult = 19,
    LightMult = 20,
    FireMult = 21,

    PhysPenetration = 22,
    ColdPenetration = 23,
    LightPenetration = 24,
    FirePenetration = 25,

    //DoT Mods
    PoisonDmg = 26,
    IgniteDmg = 27,
    BleedDmg = 28,

    PoisonMult = 29,
    IgniteMult = 30,
    BleedMult = 31,

    PoisonChance = 32,
    IgniteChance = 33,
    BleedChance = 34,

    PoisonTickRate = 35,
    IgniteTickRate = 36,
    BleedTickRate = 37,

    PoisonDuration = 38,
    IgniteDuration = 39,
    BleedDuration = 40,

    PoisonPenetration = 41,
    IgnitePenetration = 42,
    BleedPenetration = 43,

    //Non-Damaging Ailment Mods
    ShockChance = 44,
    ChillChance = 45,

    ShockEffect = 46,
    ChillEffect = 47,

    ShockDuration = 48,
    ChillDuration = 49,

    //Defense Mods
    FlatArmour = 50,
    FlatEvasion = 51,

    ArmourPercent = 52,
    EvasionPercent = 53,

    ColdRes = 54,
    LightRes = 55,
    FireRes = 56,
    AllRes = 57,

    MaxColdRes = 58,
    MaxLightRes = 59,
    MaxFireRes = 60,
    MaxAllRes = 61,

    PoisonRes = 62,
    IgniteRes = 63,
    BleedRes = 64,
    ShockRes = 65,
    ChillRes = 66,
    AllAilmentRes = 67,

    //Resource Mods
    Life = 68,
    Mana = 69,

    LifePercent = 70,
    ManaPercent = 71,

    LifeRegeneration = 72,
    ManaRegeneration = 73,

    LifeOnHit = 74,
    ManaOnHit = 75,
    LifeOnKill = 76,
    ManaOnKill = 77,

    DmgPerMaxMana = 78,
    DmgPerCurrentMana = 79,

    ManaCost = 80,

    //Utility & Speed Mods
    AttackSpeed = 81,
    Accuracy = 82,
    ChanceToHitTwice = 83,
    CooldownRecovery = 84,

    Plus1Phys = 85,
    Plus1Fire = 86,
    Plus1Cold = 87,
    Plus1Light = 88,
    Plus1Poison = 89,
    Plus1Bleed = 90,
    Plus1Ignite = 91,

    //Attribute Scaling Mods
    Strength = 92,
    Intelligence = 93,
    Dexterity = 94,

    StrengthPercent = 95,
    IntelligencePercent = 96,
    DexterityPercent = 97,

    LifePerStrength = 98,
    DamagePerStrength = 99,
    ManaPerIntelligence = 100,
    DoTMultPerIntelligence = 101,
    AttackSpeedPerDexterity = 102,
    AccuracyPerDexterity = 103,

    FlatFirePerStrength = 104,
    FlatLightPerIntelligence = 105,
    FlatColdPerDexterity = 106,

    DmgPerLowestStat = 107,
    UnarmedDamage = 108
}
