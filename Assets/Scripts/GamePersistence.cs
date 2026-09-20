// Developer map: Versioned file checkpoints, legacy V1 migration, validation, atomic writes and canonical autosave.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Historical PlayerPrefs V1 DTOs remain readable only for one-way migration.
[Serializable] public sealed class GearSaveData
{
    public LootManager.GearType type; public LootManager.GearRarity rarity; public int itemLevel; public Element element;
    public float baseDamage,baseAttackSpeed,baseCritChance; public List<RolledMod> mods=new();
    public static GearSaveData Capture(Gear gear){var d=new GearSaveData{type=gear.ItemType,rarity=gear.ItemRarity,itemLevel=gear.ItemLevel,element=gear.BaseElement,baseDamage=gear.BaseDamage,baseAttackSpeed=gear.BaseAttackSpeed,baseCritChance=gear.BaseCritChance};foreach(var m in gear.rolledMods)if(m!=null)d.mods.Add(Clone(m));return d;}
    public Gear Create(string name="Loaded Gear")=>GearSnapshotData.FromLegacy(this,Guid.NewGuid().ToString("N")).Create(name);
    static RolledMod Clone(RolledMod m)=>new(m.statType,m.tierIndex,m.value,m.HighValue,m.lockedOriginal)
        {hasSecondaryValue=m.hasSecondaryValue,isEmpowered=m.isEmpowered,isBossSpecial=m.isBossSpecial,specialPoolId=m.specialPoolId,specialModifierId=m.specialModifierId,specialAffixSide=m.specialAffixSide};
}
[Serializable] public sealed class EquippedGearSaveData{public LootManager.GearType slot;public GearSaveData gear;}
[Serializable] public sealed class GameSaveData
{
    public int version=1; public List<GearSaveData> inventory=new(); public List<EquippedGearSaveData> equipped=new();
    public List<CurrencyStackData> currencies=new(); public List<RelicData> relics=new(); public int relicCycle; public int[] activeRelicIndices;
}

[Serializable] public sealed class SaveEnvelope
{
    public int schemaVersion=GamePersistence.SchemaVersion; public string runId; public int runSeed; public string savedAtUtc; public GameStatePayload payload=new();
}
[Serializable] public sealed class GameStatePayload
{
    public int combatLevel=1,completedNormalEncounters,playerLevel=1,availablePassivePoints,relicCycle; public bool bossEncounter,hasSelectedSkill;
    public string baseClassId=PlayerClassIds.Warrior,selectedSubclassId; public bool subclassChoiceUnlocked; public SubclassProjectileMode subclassProjectileMode;
    public double experience; public PlayerSkillId selectedSkill; public float encounterStartLife,encounterStartMana;
    public List<PassiveRankData> passiveRanks=new(); public List<string> transformedPassiveNodeIds=new(); public List<GearSnapshotData> gearItems=new(); public List<string> inventoryGearIds=new();
    public List<EquippedGearReference> equippedGear=new(); public List<CurrencyStackData> currencies=new(); public int normalToMagicFragments,magicToRareFragments; public List<RelicData> relics=new(); public List<string> activeRelicIds=new();
}
[Serializable] public sealed class PassiveRankData{public int nodeId,rank;public string stableNodeId;public PassiveRankData(int id,int value){nodeId=id;rank=value;stableNodeId=id>=0&&id<PassiveTreeDefinition.NodeCount?PassiveTreeDefinition.Node(id).StableId:string.Empty;}}
[Serializable] public sealed class CharacterSlotSummary
{
    public int slot;public bool occupied;public string baseClassId,selectedSubclassId,lastPlayedUtc;public int playerLevel,combatLevel;
}
[Serializable] public sealed class EquippedGearReference{public LootManager.GearType slot;public string gearId;}
[Serializable] public sealed class GearSnapshotData
{
    public string id,weaponTypeId; public LootManager.GearType type; public LootManager.GearRarity rarity; public int itemLevel; public Element element;
    public float baseDamage,baseDamageMin,baseDamageMax,baseAttackSpeed,baseCritChance; public bool legacyAffixRules;
    public LootManager.GearRarity originRarity; public int currentCraftingPotential,maximumCraftingPotential; public List<RolledMod> mods=new();
    public static GearSnapshotData Capture(Gear gear){var d=new GearSnapshotData{id=gear.PersistentId,weaponTypeId=gear.WeaponTypeId,type=gear.ItemType,rarity=gear.ItemRarity,originRarity=gear.OriginRarity,currentCraftingPotential=gear.CurrentCraftingPotential,maximumCraftingPotential=gear.MaximumCraftingPotential,itemLevel=gear.ItemLevel,element=gear.BaseElement,baseDamage=gear.BaseDamage,baseDamageMin=gear.BaseDamageMin>0||gear.BaseDamageMax>0?gear.BaseDamageMin:gear.BaseDamage,baseDamageMax=gear.BaseDamageMin>0||gear.BaseDamageMax>0?gear.BaseDamageMax:gear.BaseDamage,baseAttackSpeed=gear.BaseAttackSpeed,baseCritChance=gear.BaseCritChance,legacyAffixRules=gear.LegacyAffixRules};foreach(var m in gear.rolledMods)if(m!=null)d.mods.Add(Clone(m));return d;}
    public static GearSnapshotData FromLegacy(GearSaveData old,string stableId){int potential=CraftingPotentialProfile.Maximum(old.rarity);var d=new GearSnapshotData{id=stableId,weaponTypeId=old.type==LootManager.GearType.Weapons?WeaponTypeCatalog.HistoricalDefaultId:string.Empty,type=old.type,rarity=old.rarity,originRarity=old.rarity,currentCraftingPotential=potential,maximumCraftingPotential=potential,itemLevel=old.itemLevel,element=old.element,baseDamage=old.baseDamage,baseDamageMin=old.baseDamage,baseDamageMax=old.baseDamage,baseAttackSpeed=old.baseAttackSpeed,baseCritChance=old.baseCritChance,legacyAffixRules=true};if(old.mods!=null)foreach(var m in old.mods)if(m!=null)d.mods.Add(Clone(m));return d;}
    public Gear Create(string name="Loaded Gear")
    {
        var go=new GameObject(name);var gear=go.AddComponent<Gear>();gear.Initialize(type,rarity,Mathf.Max(1,itemLevel),element,weaponTypeId);gear.RestorePersistentId(id);gear.RestoreLegacyAffixRules(legacyAffixRules);gear.RestoreCraftingState(originRarity,currentCraftingPotential,maximumCraftingPotential);
        var copies=new List<RolledMod>();if(mods!=null)foreach(var m in mods)if(m!=null)copies.Add(Clone(m));gear.ApplyMods(copies);gear.SetRarity(rarity);
        if(type==LootManager.GearType.Weapons){gear.BaseDamage=baseDamage;gear.BaseDamageMin=baseDamageMin;gear.BaseDamageMax=baseDamageMax;gear.BaseAttackSpeed=baseAttackSpeed;gear.BaseCritChance=baseCritChance;}return gear;
    }
    static RolledMod Clone(RolledMod m)=>new(m.statType,m.tierIndex,m.value,m.HighValue,m.lockedOriginal)
        {hasSecondaryValue=m.hasSecondaryValue,isEmpowered=m.isEmpowered,isBossSpecial=m.isBossSpecial,specialPoolId=m.specialPoolId,specialModifierId=m.specialModifierId,specialAffixSide=m.specialAffixSide};
}
public enum SaveLoadSource{None,Primary,Backup,LegacyV1}

public static class GamePersistence
{
    public const string SaveKey="BlackCube.Save.V1"; public const int SchemaVersion=10;public const int CharacterSlotCount=6;
    public const string PrimaryFileName="current-save.json",BackupFileName="current-save.json.bak",TemporaryFileName="current-save.json.tmp";
    public const float AutosaveDebounceSeconds=2f;
    static bool loadRequested,confirmedNewGameRequested,restoring,dirty,hasEncounterCheckpoint,pendingSchemaMigration; static float dirtySince,encounterStartLife,encounterStartMana;
    static string runId,saveDirectoryOverride; static int runSeed,activeSlot=1;
    public static string SaveDirectoryOverride{get=>saveDirectoryOverride;set=>saveDirectoryOverride=value;}
    public static string SaveDirectory=>string.IsNullOrWhiteSpace(saveDirectoryOverride)?Application.persistentDataPath:saveDirectoryOverride;
    public static int ActiveSlot=>activeSlot;
    public static string SlotPath(int slot)=>Path.Combine(SaveDirectory,$"slot-{ValidateSlot(slot):00}.json");
    public static string SlotBackupPath(int slot)=>SlotPath(slot)+".bak";
    public static string SlotTemporaryPath(int slot)=>SlotPath(slot)+".tmp";
    public static string PrimaryPath=>SlotPath(activeSlot); public static string BackupPath=>SlotBackupPath(activeSlot); public static string TemporaryPath=>SlotTemporaryPath(activeSlot);
    public static string LegacyPrimaryPath=>Path.Combine(SaveDirectory,PrimaryFileName);public static string LegacyBackupPath=>Path.Combine(SaveDirectory,BackupFileName);
    public static bool HasSave{get{EnsureLegacySlotMigration();for(int i=1;i<=CharacterSlotCount;i++)if(HasSlot(i))return true;return PlayerPrefs.HasKey(SaveKey);}}
    public static bool HasSlot(int slot){ValidateSlot(slot);return File.Exists(SlotPath(slot))||File.Exists(SlotBackupPath(slot));}
    public static bool LoadRequested=>loadRequested; public static bool ConfirmedNewGameRequested=>confirmedNewGameRequested;
    public static bool IsRestoring=>restoring; public static bool HasPendingAutosave=>dirty; public static string CurrentRunId=>runId; public static int CurrentRunSeed=>runSeed;
    public static SaveLoadSource LastLoadSource{get;private set;} public static string LastError{get;private set;}
    public static bool RequestLoad()=>RequestLoad(activeSlot);
    public static bool RequestLoad(int slot){ValidateSlot(slot);EnsureLegacySlotMigration();if(!HasSlot(slot)&&(slot!=1||!PlayerPrefs.HasKey(SaveKey)))return false;activeSlot=slot;loadRequested=true;confirmedNewGameRequested=false;return true;}
    public static void RequestNewGame(){loadRequested=confirmedNewGameRequested=dirty=false;}
    public static void RequestNewGame(int slot){ValidateSlot(slot);activeSlot=slot;RequestNewGame();}
    public static void RequestConfirmedNewGame(){loadRequested=false;confirmedNewGameRequested=true;dirty=false;}
    public static void RequestConfirmedNewGame(int slot){ValidateSlot(slot);activeSlot=slot;RequestConfirmedNewGame();}
    public static bool ConsumeLoadRequest(){bool v=loadRequested;loadRequested=false;return v;} public static bool ConsumeConfirmedNewGameRequest(){bool v=confirmedNewGameRequested;confirmedNewGameRequested=false;return v;}
    public static CharacterSlotSummary GetSlotSummary(int slot)
    {
        ValidateSlot(slot);EnsureLegacySlotMigration();var summary=new CharacterSlotSummary{slot=slot,occupied=false};
        int previous=activeSlot;activeSlot=slot;try{if(!TryReadBestEnvelope(out var e,out _,out _))return summary;summary.occupied=true;summary.baseClassId=e.payload.baseClassId;summary.selectedSubclassId=e.payload.selectedSubclassId;summary.playerLevel=e.payload.playerLevel;summary.combatLevel=e.payload.combatLevel;summary.lastPlayedUtc=e.savedAtUtc;return summary;}finally{activeSlot=previous;}
    }
    public static void BeginFreshRunIdentity(){runId=Guid.NewGuid().ToString("N");runSeed=Seed(runId);hasEncounterCheckpoint=false;encounterStartLife=encounterStartMana=0f;}
    public static void RecordEncounterStart(HealthComponent health,ManaComponent mana){if(restoring||health==null||mana==null)return;encounterStartLife=health.CurrentLife;encounterStartMana=mana.CurrentMana;hasEncounterCheckpoint=true;MarkDirty();}
    public static int EncounterSeed(int level,int completed,bool boss){unchecked{int h=runSeed;h=h*397^level;h=h*397^completed;return h*397^(boss?1:0);}}
    public static void GenerateDeterministicEncounter(int level,int completed,bool boss,Action action){if(action==null)return;var old=UnityEngine.Random.state;UnityEngine.Random.InitState(EncounterSeed(level,completed,boss));try{action();}finally{UnityEngine.Random.state=old;}}
    public static void MarkDirty(){if(restoring)return;if(!dirty)dirtySince=Time.realtimeSinceStartup;dirty=true;}
    public static bool FlushPendingAutosave(bool force=false){if(restoring||!dirty)return true;if(!force&&Time.realtimeSinceStartup-dirtySince<AutosaveDebounceSeconds)return false;return TrySave();}
    public static void Save()=>TrySave(); public static bool TrySave()=>TrySaveInternal(false); public static bool CommitConfirmedNewGame()=>TrySaveInternal(true);
    static bool TrySaveInternal(bool freshBackup)
    {
        if(restoring){LastError="A save cannot be captured during restoration.";return false;}
        try{if(!TryCapture(out var e,out var error)){LastError=error;return false;}if(!ValidateEnvelope(e,out error)||!WriteEnvelope(e,freshBackup,out error)){LastError=error;Debug.LogError("GamePersistence: "+error);return false;}dirty=false;LastError=null;return true;}
        catch(Exception ex){LastError=ex.Message;Debug.LogError($"GamePersistence: save failed. {ex}");return false;}
    }
    public static bool RestoreRequestedGame(out bool restored){if(!ConsumeLoadRequest()){restored=false;return false;}restored=Load();return true;}
    public static bool Load()
    {
        if(!TryReadBestEnvelope(out var e,out var source,out var error)){LastError=error;LastLoadSource=SaveLoadSource.None;Debug.LogError("GamePersistence: "+error);return false;}
        SaveEnvelope rollback=null;TryCapture(out rollback,out _);restoring=true;
        try
        {
            if(!ApplyEnvelope(e,out error))throw new InvalidOperationException(error);runId=e.runId;runSeed=e.runSeed;hasEncounterCheckpoint=true;encounterStartLife=e.payload.encounterStartLife;encounterStartMana=e.payload.encounterStartMana;
            if((source==SaveLoadSource.LegacyV1||pendingSchemaMigration)&&!WriteEnvelope(e,false,out error))throw new IOException("Restore succeeded but migration commit failed: "+error);
            pendingSchemaMigration=false;
            restoring=false;
            LastLoadSource=source;LastError=null;dirty=false;if(source==SaveLoadSource.Backup)Debug.LogWarning("GamePersistence: primary invalid; restored backup.");if(source==SaveLoadSource.LegacyV1)Debug.Log("GamePersistence: migrated PlayerPrefs V1.");return true;
        }
        catch(Exception ex){if(restoring&&rollback!=null)ApplyEnvelope(rollback,out _);restoring=false;LastError=ex.Message;LastLoadSource=SaveLoadSource.None;Debug.LogError($"GamePersistence: transactional load failed. {ex}");return false;}
    }
    public static bool TryCapture(out SaveEnvelope envelope,out string error)
    {
        envelope=null;error=null;var gm=GameManager.Instance;var inv=Inventory.Instance;var eq=EquipmentManager.Instance;var cur=CurrencyInventory.Instance;var rel=RelicInventory.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var prog=gm!=null?gm.GetComponent<PlayerProgression>():null;var identity=gm!=null?gm.GetComponent<PlayerIdentityState>():null;var skill=player!=null?player.GetComponent<PlayerSkillController>():null;var hp=player!=null?player.GetComponent<HealthComponent>():null;var mana=player!=null?player.GetComponent<ManaComponent>():null;
        if(gm==null||inv==null||eq==null||cur==null||rel==null||prog==null||identity==null||player==null||skill==null||hp==null||mana==null)return Fail("Cannot capture a complete checkpoint because gameplay authorities are unavailable.",out error);
        if(string.IsNullOrWhiteSpace(runId))BeginFreshRunIdentity();if(!hasEncounterCheckpoint){encounterStartLife=hp.CurrentLife;encounterStartMana=mana.CurrentMana;hasEncounterCheckpoint=true;}
        var p=new GameStatePayload{baseClassId=identity.BaseClassId,subclassChoiceUnlocked=identity.SubclassChoiceUnlocked,selectedSubclassId=identity.SelectedSubclassId,subclassProjectileMode=identity.ProjectileMode,combatLevel=gm.CurrentCombatLevel,completedNormalEncounters=gm.NormalKills,bossEncounter=gm.BossActive,playerLevel=prog.Level,experience=prog.Experience,availablePassivePoints=prog.AvailablePoints,hasSelectedSkill=skill.SelectedSkill!=null,selectedSkill=skill.SelectedSkill!=null?skill.SelectedSkill.id:default,encounterStartLife=encounterStartLife,encounterStartMana=encounterStartMana,relicCycle=rel.CurrentCycle};
        p.transformedPassiveNodeIds.AddRange(prog.CopyTransformedStableIds());
        int[] ranks=prog.CopyRanks();for(int i=0;i<ranks.Length;i++)if(ranks[i]>0)p.passiveRanks.Add(new PassiveRankData(i,ranks[i]));var owned=new HashSet<string>();
        foreach(var gear in inv.Items)if(gear!=null&&!gear.IsScrap&&!gear.Dismantled){var d=GearSnapshotData.Capture(gear);if(!owned.Add(d.id))return Fail("Duplicate gear ID during capture.",out error);p.gearItems.Add(d);p.inventoryGearIds.Add(d.id);}
        foreach(var pair in eq.EquippedItems)if(pair.Value!=null){var d=GearSnapshotData.Capture(pair.Value);if(!owned.Add(d.id))return Fail("Gear has duplicate ownership during capture.",out error);p.gearItems.Add(d);p.equippedGear.Add(new EquippedGearReference{slot=pair.Key,gearId=d.id});}
        foreach(var stack in cur.Stacks)p.currencies.Add(stack);p.normalToMagicFragments=cur.NormalToMagicFragments;p.magicToRareFragments=cur.MagicToRareFragments;foreach(var relic in rel.Relics)p.relics.Add(CloneRelic(relic));for(int i=0;i<RelicInventory.ActiveSlotCount;i++)p.activeRelicIds.Add(rel.Active(i)?.id??string.Empty);
        envelope=new SaveEnvelope{schemaVersion=SchemaVersion,runId=runId,runSeed=runSeed,savedAtUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture),payload=p};return true;
    }
    public static bool ValidateEnvelope(SaveEnvelope e,out string error)
    {
        error=null;if(e==null)return Fail("Save envelope is missing.",out error);if(e.schemaVersion!=SchemaVersion)return Fail(e.schemaVersion>SchemaVersion?$"Schema {e.schemaVersion} is newer than supported schema {SchemaVersion}.":$"Schema {e.schemaVersion} requires migration.",out error);
        if(string.IsNullOrWhiteSpace(e.runId)||e.runId.Length>128||e.runSeed==0)return Fail("Run identity is invalid.",out error);if(string.IsNullOrWhiteSpace(e.savedAtUtc)||!DateTime.TryParse(e.savedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out _))return Fail("Save timestamp is invalid.",out error);if(e.payload==null)return Fail("Save payload is missing.",out error);var p=e.payload;
        if(p.combatLevel<1||p.combatLevel>1000000||p.completedNormalEncounters<0||p.completedNormalEncounters>9||(p.bossEncounter&&p.completedNormalEncounters!=9))return Fail("Combat position is invalid.",out error);
        if(p.playerLevel<1||p.playerLevel>100||!Finite(p.experience)||p.experience<0||p.availablePassivePoints<0)return Fail("Player progression is invalid.",out error);
        if(!PlayerClassCatalog.IsValid(p.baseClassId))return Fail("Base class ID is unknown.",out error);if(!string.IsNullOrEmpty(p.selectedSubclassId)&&(!p.subclassChoiceUnlocked||!SubclassCatalog.TryGet(p.selectedSubclassId,out var subclass)||subclass.ParentClassId!=p.baseClassId))return Fail("Subclass selection is invalid for this class.",out error);
        int allocated=0;var seenRanks=new HashSet<int>();if(p.passiveRanks==null)return Fail("Passive allocations are missing.",out error);foreach(var r in p.passiveRanks){int id=!string.IsNullOrEmpty(r?.stableNodeId)?PassiveTreeDefinition.NodeId(r.stableNodeId):r?.nodeId??-1;if(r==null||id<0||id>=PassiveTreeDefinition.NodeCount||PassiveTreeDefinition.IsClassStart(id)||r.rank!=1||!seenRanks.Add(id))return Fail("Passive allocation is invalid or duplicated.",out error);allocated++;}
        if(!ValidateTransformations(p.transformedPassiveNodeIds,seenRanks,p.selectedSubclassId))return Fail("Subclass transformations are invalid.",out error);
        if(allocated+p.availablePassivePoints!=p.playerLevel||!ValidPassiveTopology(seenRanks,p.baseClassId))return Fail("Passive point accounting or topology is invalid.",out error);if(p.playerLevel<100&&p.experience>=RequirementAt(p.playerLevel)||p.playerLevel==100&&p.experience!=0)return Fail("XP does not match player level.",out error);
        if(p.hasSelectedSkill&&!Enum.IsDefined(typeof(PlayerSkillId),p.selectedSkill))return Fail("Selected skill ID is unknown.",out error);if(!Finite(p.encounterStartLife)||!Finite(p.encounterStartMana)||p.encounterStartLife<=0||p.encounterStartLife>1000000000f||p.encounterStartMana<0||p.encounterStartMana>1000000000f)return Fail("Encounter checkpoint resources are invalid.",out error);
        if(p.normalToMagicFragments<0||p.normalToMagicFragments>=10||p.magicToRareFragments<0||p.magicToRareFragments>=10)return Fail("Fragment remainder is invalid.",out error);
        return ValidateGear(p,out error)&&ValidateCurrencies(p.currencies,out error)&&ValidateRelics(p,out error);
    }
    static bool ValidateGear(GameStatePayload p,out string error)
    {
        error=null;if(p.gearItems==null||p.inventoryGearIds==null||p.equippedGear==null)return Fail("Gear collections are missing.",out error);var ids=new HashSet<string>();var owned=new HashSet<string>();var slots=new HashSet<LootManager.GearType>();
        foreach(var g in p.gearItems){if(g==null||string.IsNullOrWhiteSpace(g.id)||!ids.Add(g.id))return Fail("Gear IDs must be unique.",out error);if(g.type==LootManager.GearType.Weapons?!WeaponTypeCatalog.IsValid(g.weaponTypeId):!string.IsNullOrEmpty(g.weaponTypeId))return Fail("Gear weapon type identity is invalid.",out error);if(!Enum.IsDefined(typeof(LootManager.GearType),g.type)||!Enum.IsDefined(typeof(LootManager.GearRarity),g.rarity)||!Enum.IsDefined(typeof(LootManager.GearRarity),g.originRarity)||g.maximumCraftingPotential!=CraftingPotentialProfile.Maximum(g.originRarity)||g.currentCraftingPotential<0||g.currentCraftingPotential>g.maximumCraftingPotential||!Enum.IsDefined(typeof(Element),g.element)||g.element==Element.Count||g.itemLevel<1||g.itemLevel>100||!Finite(g.baseDamage)||!Finite(g.baseDamageMin)||!Finite(g.baseDamageMax)||!Finite(g.baseAttackSpeed)||!Finite(g.baseCritChance)||g.baseDamage<0||g.baseDamageMin<0||g.baseDamageMax<g.baseDamageMin||g.baseAttackSpeed<0||g.baseCritChance<0)return Fail("Gear base data or Crafting Potential is invalid.",out error);if(g.mods==null)return Fail("Gear modifiers are missing.",out error);foreach(var m in g.mods)if(m==null||!Enum.IsDefined(typeof(StatTypes),m.statType)||m.tierIndex<1||m.tierIndex>20||!Finite(m.value)||!Finite(m.secondaryValue)||m.hasSecondaryValue&&m.secondaryValue<m.value||m.isEmpowered&&m.isBossSpecial||m.lockedOriginal&&(m.isEmpowered||m.isBossSpecial)||m.isEmpowered&&m.tierIndex!=1||m.isBossSpecial&&(string.IsNullOrWhiteSpace(m.specialPoolId)||string.IsNullOrWhiteSpace(m.specialModifierId)||!Enum.IsDefined(typeof(AffixSide),m.specialAffixSide))||!m.isBossSpecial&&(!string.IsNullOrEmpty(m.specialPoolId)||!string.IsNullOrEmpty(m.specialModifierId)||!ValidGearModifier(g.type,m.statType)))return Fail("Gear modifier is invalid.",out error);if(!g.legacyAffixRules&&!ValidateCurrentAffixes(g))return Fail("Gear affix tier, slot, family or side capacity is invalid.",out error);}
        foreach(string id in p.inventoryGearIds)if(string.IsNullOrWhiteSpace(id)||!ids.Contains(id)||!owned.Add(id))return Fail("Inventory ownership is invalid.",out error);foreach(var x in p.equippedGear)if(x==null||!Enum.IsDefined(typeof(LootManager.GearType),x.slot)||string.IsNullOrWhiteSpace(x.gearId)||!ids.Contains(x.gearId)||!owned.Add(x.gearId)||!slots.Add(x.slot))return Fail("Equipped ownership is invalid.",out error);return owned.Count==ids.Count||Fail("Every gear item must have one owner.",out error);
    }
    static bool ValidateCurrentAffixes(GearSnapshotData gear)
    {
        int total=0,prefix=0,suffix=0,implicits=0;var families=new HashSet<StatTypes>();
        foreach(var mod in gear.mods)
        {
            if(Gear.IsWeaponBaseStat(mod.statType))continue;
            if(mod.lockedOriginal)implicits++;
            else
            {
                total++;
                if(!families.Add(mod.statType))return false;
                if(AffixPolicy.Side(mod)==AffixSide.Prefix)prefix++;else suffix++;
            }
            if(mod.isBossSpecial)continue;
            AffixDefinitions definition=ModManager.Instance!=null?ModManager.Instance.Database?.GetDefinition(mod.statType):null;
            if(gear.type==LootManager.GearType.Weapons&&definition!=null&&!definition.AllowsWeaponType(gear.weaponTypeId))return false;
            if(PoedbAffixCatalog.TryGet(mod.statType,gear.type,out var tiers))
            {
                AffixTier tier=tiers.Find(t=>t.tierIndex==mod.tierIndex&&t.minItemLevel<=gear.itemLevel);
                if(tier==null||tier.pairedDamage!=mod.hasSecondaryValue)return false;
                float minimum=tier.minValue,maximum=tier.maxValue,minimumHigh=tier.minHighValue,maximumHigh=tier.maxHighValue;
                if(mod.isEmpowered)EmpowermentCrafting.EmpoweredRange(ModManager.Instance?.Database?.GetDefinition(mod.statType),tier,out minimum,out maximum,out minimumHigh,out maximumHigh);
                if(mod.value<minimum-.011f||mod.value>maximum+.011f)return false;
                if(tier.pairedDamage&&(mod.HighValue<minimumHigh-.011f||mod.HighValue>maximumHigh+.011f))return false;
            }
        }
        return implicits==1 && total<=AffixPolicy.MaximumTotal(gear.rarity)
            && prefix<=AffixPolicy.MaximumOnSide(gear.rarity)
            && suffix<=AffixPolicy.MaximumOnSide(gear.rarity);
    }
    static bool ValidateCurrencies(List<CurrencyStackData> list,out string error){error=null;if(list==null)return Fail("Currency collection is missing.",out error);var seen=new HashSet<CraftingCurrencyType>();foreach(var x in list)if(!Enum.IsDefined(typeof(CraftingCurrencyType),x.type)||x.amount<=0||!seen.Add(x.type))return Fail("Currency stack is invalid.",out error);return true;}
    static bool ValidateRelics(GameStatePayload p,out string error)
    {
        error=null;if(p.relics==null||p.activeRelicIds==null||p.activeRelicIds.Count!=RelicInventory.ActiveSlotCount||p.relicCycle<0)return Fail("Relic collection or cycle is invalid.",out error);var ids=new HashSet<string>();int craftable=0;
        foreach(var r in p.relics){if(r==null||string.IsNullOrWhiteSpace(r.id)||!ids.Add(r.id)||r.cycle<1||r.cycle>p.relicCycle||r.relicLevel<1||r.relicLevel>100||!Enum.IsDefined(typeof(LootManager.GearRarity),r.rarity)||r.modifiers==null)return Fail("Relic identity, level or cycle is invalid.",out error);if(r.craftableThisCycle){craftable++;if(r.cycle!=p.relicCycle)return Fail("Craftable relic cycle is invalid.",out error);}foreach(var m in r.modifiers)if(m==null||!Enum.IsDefined(typeof(RelicModifierType),m.type)||!Finite(m.value)||m.value<0||m.tierIndex<0||m.tierIndex>5)return Fail("Relic modifier is invalid.",out error);}
        if(craftable>1||p.relicCycle==0&&p.relics.Count>0)return Fail("Relic current-cycle relationship is invalid.",out error);var active=new HashSet<string>();foreach(string id in p.activeRelicIds)if(!string.IsNullOrEmpty(id)&&(!ids.Contains(id)||!active.Add(id)))return Fail("Active relic slot is invalid.",out error);return true;
    }
    public static bool TryReadBestEnvelope(out SaveEnvelope e,out SaveLoadSource source,out string error)
    {
        e=null;source=SaveLoadSource.None;pendingSchemaMigration=false;var failures=new List<string>();if(File.Exists(PrimaryPath)){if(TryReadFile(PrimaryPath,out e,out var x)){source=SaveLoadSource.Primary;error=null;return true;}failures.Add("primary: "+x);}if(File.Exists(BackupPath)){if(TryReadFile(BackupPath,out e,out var x)){source=SaveLoadSource.Backup;error=null;return true;}failures.Add("backup: "+x);}
        if(activeSlot==1&&!File.Exists(PrimaryPath)&&!File.Exists(BackupPath)&&PlayerPrefs.HasKey(SaveKey)){if(TryMigrateLegacy(PlayerPrefs.GetString(SaveKey),out e,out var x)){source=SaveLoadSource.LegacyV1;error=null;return true;}failures.Add("legacy: "+x);}error=failures.Count==0?"No gameplay save exists.":"No valid save: "+string.Join("; ",failures);return false;
    }
    public static bool TryReadFile(string path,out SaveEnvelope e,out string error){e=null;error=null;try{string json=File.ReadAllText(path,Encoding.UTF8);if(string.IsNullOrWhiteSpace(json))return Fail("File is empty.",out error);e=JsonUtility.FromJson<SaveEnvelope>(json);bool migrated=e?.schemaVersion is 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9;if(e?.schemaVersion==2)MigrateSchema2(e);if(e?.schemaVersion==3)MigrateEquipmentImplicits(e);if(e?.schemaVersion==4)MigrateSchema4(e);if(e?.schemaVersion==5)MigrateSchema5(e);if(e?.schemaVersion==6)MigrateSchema6(e);if(e?.schemaVersion==7)MigrateSchema7(e);if(e?.schemaVersion==8)MigrateSchema8(e);if(e?.schemaVersion==9)MigrateSchema9(e);bool normalized=NormalizeFragmentPayload(e?.payload);bool valid=ValidateEnvelope(e,out error);pendingSchemaMigration=valid&&(migrated||normalized);return valid;}catch(Exception ex){pendingSchemaMigration=false;return Fail(ex.Message,out error);}}
    static bool NormalizeFragmentPayload(GameStatePayload payload)
    {
        if(payload==null||payload.currencies==null)return false;
        bool changed=false;
        changed|=NormalizeOne(CraftingCurrencyType.NormalToMagic,ref payload.normalToMagicFragments,payload.currencies);
        changed|=NormalizeOne(CraftingCurrencyType.MagicToRare,ref payload.magicToRareFragments,payload.currencies);
        return changed;
    }
    static bool NormalizeOne(CraftingCurrencyType type,ref int fragments,List<CurrencyStackData> currencies)
    {
        if(fragments<10)return false;
        int full=fragments/10;
        for(int i=0;i<currencies.Count;i++)if(currencies[i].type==type)
        {
            if(currencies[i].amount>int.MaxValue-full)return false;
            currencies[i]=new CurrencyStackData(type,currencies[i].amount+full);
            fragments%=10;return true;
        }
        currencies.Add(new CurrencyStackData(type,full));fragments%=10;return true;
    }
    // Schema 3 already serialized the locked-original flag and exact rolls;
    // PlayerPrefs V1 gear may need its first surviving roll marked. Reuse that
    // field as the implicit. Schema 4 distinguishes strict new caps from
    // historical 3-side gear that must remain legacy-compatible.
    static bool MigrateEquipmentImplicits(SaveEnvelope e)
    {
        bool changed=false;
        if(e.payload?.gearItems==null){e.schemaVersion=4;return false;}
        foreach(var gear in e.payload.gearItems)
        {
            if(gear?.mods==null)continue;
            RolledMod first=null,implicitMod=null;
            foreach(var mod in gear.mods)
            {
                if(mod==null||Gear.IsWeaponBaseStat(mod.statType))continue;
                first??=mod;
                if(mod.lockedOriginal&&implicitMod==null)implicitMod=mod;
            }
            implicitMod??=first;
            foreach(var mod in gear.mods)
            {
                if(mod==null||Gear.IsWeaponBaseStat(mod.statType))continue;
                bool shouldLock=ReferenceEquals(mod,implicitMod);
                if(mod.lockedOriginal!=shouldLock){mod.lockedOriginal=shouldLock;changed=true;}
            }
            // Old 3-side Rare or underfilled historical rolls may exceed the
            // new 2/2 side split. Preserve their entire item as legacy-compatible.
            if(!gear.legacyAffixRules&&!ValidateCurrentAffixes(gear))
            {gear.legacyAffixRules=true;changed=true;}
        }
        e.schemaVersion=4;
        return changed;
    }
    static void MigrateSchema4(SaveEnvelope e)
    {
        if(e.payload!=null){e.payload.normalToMagicFragments=0;e.payload.magicToRareFragments=0;}
        e.schemaVersion=5;
    }
    static void MigrateSchema5(SaveEnvelope e)
    {
        if(e.payload?.relics!=null)foreach(var relic in e.payload.relics)if(relic!=null)
        {relic.relicLevel=1;if(relic.modifiers!=null)foreach(var modifier in relic.modifiers)if(modifier!=null)modifier.tierIndex=0;}
        e.schemaVersion=6;
    }
    static void MigrateSchema6(SaveEnvelope e)
    {
        if(e.payload?.gearItems!=null)foreach(var gear in e.payload.gearItems)if(gear!=null)
        {
            gear.originRarity=gear.rarity;
            gear.maximumCraftingPotential=CraftingPotentialProfile.Maximum(gear.rarity);
            gear.currentCraftingPotential=gear.maximumCraftingPotential;
            if(gear.mods!=null)foreach(var mod in gear.mods)if(mod!=null)
            {mod.isEmpowered=false;mod.isBossSpecial=false;mod.specialPoolId=null;mod.specialModifierId=null;mod.specialAffixSide=default;}
        }
        e.schemaVersion=7;
    }
    static void MigrateSchema7(SaveEnvelope e)
    {
        if(e.payload!=null)
        {
            e.payload.baseClassId=PlayerClassIds.Warrior;e.payload.subclassChoiceUnlocked=false;e.payload.selectedSubclassId=string.Empty;
            if(e.payload.gearItems!=null)foreach(var gear in e.payload.gearItems)if(gear!=null)
                gear.weaponTypeId=gear.type==LootManager.GearType.Weapons?WeaponTypeCatalog.HistoricalDefaultId:string.Empty;
        }
        e.schemaVersion=8;
    }
    static void MigrateSchema8(SaveEnvelope e)
    {
        if(e.payload!=null){e.payload.passiveRanks=new List<PassiveRankData>();e.payload.availablePassivePoints=Mathf.Clamp(e.payload.playerLevel,1,100);}
        e.schemaVersion=9;
    }
    static void MigrateSchema9(SaveEnvelope e)
    {if(e.payload!=null){e.payload.transformedPassiveNodeIds=new List<string>();e.payload.subclassProjectileMode=SubclassProjectileMode.Volley;}e.schemaVersion=10;}
    static void MigrateSchema2(SaveEnvelope e)
    {
        if(e.payload?.gearItems!=null)foreach(var gear in e.payload.gearItems)
        {
            if(gear==null)continue;
            gear.baseDamageMin=gear.baseDamageMax=gear.baseDamage;
            gear.legacyAffixRules=true;
            if(gear.mods!=null)foreach(var mod in gear.mods)if(mod!=null)
            {
                mod.hasSecondaryValue=false;
                mod.secondaryValue=mod.value;
            }
        }
        e.schemaVersion=3;
    }
    public static bool TryMigrateLegacy(string json,out SaveEnvelope e,out string error)
    {
        e=null;error=null;try{if(string.IsNullOrWhiteSpace(json))return Fail("Legacy JSON is empty.",out error);var old=JsonUtility.FromJson<GameSaveData>(json);if(old==null||old.version!=1)return Fail("Legacy version is not V1.",out error);old.inventory??=new();old.equipped??=new();old.currencies??=new();old.relics??=new();string id=Guid.NewGuid().ToString("N");var p=new GameStatePayload{encounterStartLife=100,encounterStartMana=100,relicCycle=Mathf.Max(0,old.relicCycle)};
            foreach(var x in old.inventory){if(x==null)return Fail("Legacy inventory is malformed.",out error);var g=GearSnapshotData.FromLegacy(x,Guid.NewGuid().ToString("N"));p.gearItems.Add(g);p.inventoryGearIds.Add(g.id);}foreach(var x in old.equipped){if(x?.gear==null)return Fail("Legacy equipment is malformed.",out error);var g=GearSnapshotData.FromLegacy(x.gear,Guid.NewGuid().ToString("N"));p.gearItems.Add(g);p.equippedGear.Add(new EquippedGearReference{slot=x.slot,gearId=g.id});}
            foreach(var x in old.currencies)p.currencies.Add(x);foreach(var x in old.relics)if(x!=null)p.relics.Add(CloneRelic(x));else return Fail("Legacy relic is malformed.",out error);for(int i=0;i<RelicInventory.ActiveSlotCount;i++){int index=old.activeRelicIndices!=null&&i<old.activeRelicIndices.Length?old.activeRelicIndices[i]:-1;if(index < -1 || index >= p.relics.Count)return Fail("Legacy active relic slot is invalid.",out error);p.activeRelicIds.Add(index>=0?p.relics[index].id:string.Empty);}e=new SaveEnvelope{runId=id,runSeed=Seed(id),savedAtUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture),payload=p};e.schemaVersion=3;MigrateEquipmentImplicits(e);MigrateSchema4(e);MigrateSchema5(e);MigrateSchema6(e);MigrateSchema7(e);MigrateSchema8(e);MigrateSchema9(e);return ValidateEnvelope(e,out error);
        }catch(Exception ex){return Fail(ex.Message,out error);}
    }
    static bool ApplyEnvelope(SaveEnvelope e,out string error)
    {
        error=null;if(!ValidateEnvelope(e,out error))return false;var gm=GameManager.Instance;var inv=Inventory.Instance;var eq=EquipmentManager.Instance;var cur=CurrencyInventory.Instance;var rel=RelicInventory.Instance;var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var prog=gm!=null?gm.GetComponent<PlayerProgression>():null;var identity=gm!=null?gm.GetComponent<PlayerIdentityState>():null;var skill=player!=null?player.GetComponent<PlayerSkillController>():null;var hp=player!=null?player.GetComponent<HealthComponent>():null;var mana=player!=null?player.GetComponent<ManaComponent>():null;
        if(gm==null||inv==null||eq==null||cur==null||rel==null||prog==null||identity==null||player==null||skill==null||hp==null||mana==null)return Fail("Gameplay authorities are unavailable for restore.",out error);var p=e.payload;if(!identity.Restore(p.baseClassId,p.subclassChoiceUnlocked,p.selectedSubclassId,p.subclassProjectileMode))return Fail("Identity rejected restore.",out error);eq.ResetForNewRun();inv.ResetForNewRun();cur.ResetForNewGame();rel.ResetForNewGame();var relics=new List<RelicData>();foreach(var x in p.relics)relics.Add(CloneRelic(x));var ri=new Dictionary<string,int>();for(int i=0;i<relics.Count;i++)ri[relics[i].id]=i;var active=new int[RelicInventory.ActiveSlotCount];for(int i=0;i<active.Length;i++)active[i]=string.IsNullOrEmpty(p.activeRelicIds[i])?-1:ri[p.activeRelicIds[i]];rel.Restore(relics,p.relicCycle,active);
        var gear=new Dictionary<string,Gear>();foreach(var x in p.gearItems)gear.Add(x.id,x.Create());foreach(string id in p.inventoryGearIds)inv.Add(gear[id]);foreach(var x in p.equippedGear)eq.Equip(gear[x.gearId]);var ranks=new int[PassiveTreeDefinition.NodeCount];foreach(var x in p.passiveRanks){int id=!string.IsNullOrEmpty(x.stableNodeId)?PassiveTreeDefinition.NodeId(x.stableNodeId):x.nodeId;if(id>=0)ranks[id]=x.rank;}if(!prog.RestoreProgression(p.playerLevel,p.experience,p.availablePassivePoints,ranks))return Fail("Progression rejected restore.",out error);if(!prog.RestoreTransformations(p.transformedPassiveNodeIds))return Fail("Transformations rejected restore.",out error);if(!skill.RestoreSelection(p.hasSelectedSkill,p.selectedSkill))return Fail("Skill rejected restore.",out error);cur.Restore(p.currencies);if(!cur.RestoreFragments(p.normalToMagicFragments,p.magicToRareFragments))return Fail("Fragment restore failed.",out error);cur.CancelArmed();runId=e.runId;runSeed=e.runSeed;encounterStartLife=p.encounterStartLife;encounterStartMana=p.encounterStartMana;hasEncounterCheckpoint=true;if(!gm.RestoreRunState(p.combatLevel,p.completedNormalEncounters,p.bossEncounter))return Fail("Combat restore failed.",out error);player.GetComponent<StatusController>()?.ClearStatuses();if(!hp.RestoreCheckpointLife(Mathf.Min(p.encounterStartLife,hp.MaxLife))||!mana.RestoreCheckpointMana(Mathf.Min(p.encounterStartMana,mana.MaxMana)))return Fail("Resource checkpoint restore failed.",out error);Time.timeScale=1;return true;
    }
    static bool WriteEnvelope(SaveEnvelope e,bool freshBackup,out string error)
    {
        error=null;try{Directory.CreateDirectory(SaveDirectory);WriteDurable(TemporaryPath,JsonUtility.ToJson(e,true));if(!TryReadFile(TemporaryPath,out var check,out var x)||check.runId!=e.runId)return Fail("Temporary verification failed: "+x,out error);if(File.Exists(PrimaryPath))File.Replace(TemporaryPath,PrimaryPath,BackupPath,true);else File.Move(TemporaryPath,PrimaryPath);if(freshBackup||!File.Exists(BackupPath))CopyPrimaryToBackup();return true;}catch(Exception ex){return Fail(ex.Message,out error);}
    }
    static void CopyPrimaryToBackup(){File.Copy(PrimaryPath,TemporaryPath,true);using(var stream=new FileStream(TemporaryPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None))stream.Flush(true);if(!TryReadFile(TemporaryPath,out _,out var error))throw new IOException(error);if(File.Exists(BackupPath))File.Replace(TemporaryPath,BackupPath,null,true);else File.Move(TemporaryPath,BackupPath);}
    static void WriteDurable(string path,string value){using var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None);using var writer=new StreamWriter(stream,new UTF8Encoding(false));writer.Write(value);writer.Flush();stream.Flush(true);}
    static RelicData CloneRelic(RelicData x){var c=new RelicData{id=x.id,cycle=x.cycle,rarity=x.rarity,craftableThisCycle=x.craftableThisCycle,relicLevel=x.relicLevel};if(x.modifiers!=null)foreach(var m in x.modifiers)if(m!=null)c.modifiers.Add(new RelicModifier(m.type,m.value,m.lockedOriginal,m.tierIndex));return c;}
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);static bool Fail(string value,out string error){error=value;return false;}static int Seed(string value){unchecked{int h=17;foreach(char c in value)h=h*31+c;return h==0?1:h;}}
    static bool ValidGearModifier(LootManager.GearType type,StatTypes stat)
    {
        if(Gear.IsWeaponBaseStat(stat))return type==LootManager.GearType.Weapons;
        var pools=GearStatLists.BuildDefaultStatPools();return pools.TryGetValue(type,out var values)&&values.Contains(stat);
    }
    static bool ValidateTransformations(List<string> values,HashSet<int> allocated,string subclassId)
    {
        if(values==null)return false;if(values.Count==0)return true;if(string.IsNullOrEmpty(subclassId)||values.Count>SubclassTransformationProfile.MaximumTransformedNodes)return false;
        var ids=new HashSet<int>();foreach(string stable in values){int id=PassiveTreeDefinition.NodeId(stable);if(id<0||!allocated.Contains(id)||PassiveTreeDefinition.IsClassStart(id)||PassiveTreeDefinition.IsKeystone(id)||!ids.Add(id))return false;}
        var reached=new HashSet<int>();var queue=new Queue<int>();foreach(int id in ids){queue.Enqueue(id);reached.Add(id);break;}while(queue.Count>0){int current=queue.Dequeue();foreach(int adjacent in PassiveTreeDefinition.AdjacentNodeIds(current))if(ids.Contains(adjacent)&&reached.Add(adjacent))queue.Enqueue(adjacent);}return reached.Count==ids.Count;
    }
    static bool ValidPassiveTopology(HashSet<int> allocated,string classId)
    {
        if(allocated.Count==0)return true;var reached=new HashSet<int>();var queue=new Queue<int>();foreach(int id in allocated)if(PassiveTreeDefinition.IsRootConnected(id,classId)){reached.Add(id);queue.Enqueue(id);}
        while(queue.Count>0){int current=queue.Dequeue();foreach(int adjacent in PassiveTreeDefinition.AdjacentNodeIds(current))if(allocated.Contains(adjacent)&&reached.Add(adjacent))queue.Enqueue(adjacent);}return reached.Count==allocated.Count;
    }
    static double RequirementAt(int level){const double x=300d,y=619d;return level<100?Math.Max(1d,Math.Round(x*Math.Pow(y/x,(level-10)/6d))):0d;}
    static int ValidateSlot(int slot){if(slot<1||slot>CharacterSlotCount)throw new ArgumentOutOfRangeException(nameof(slot));return slot;}
    static void EnsureLegacySlotMigration()
    {
        if(File.Exists(SlotPath(1))||File.Exists(SlotBackupPath(1)))return;string source=File.Exists(LegacyPrimaryPath)?LegacyPrimaryPath:File.Exists(LegacyBackupPath)?LegacyBackupPath:null;if(source==null)return;
        if(!TryReadFile(source,out var envelope,out _))return;int previous=activeSlot;try{activeSlot=1;if(WriteEnvelope(envelope,true,out _))Debug.Log("GamePersistence: migrated legacy single save to character Slot 1.");}catch(Exception ex){Debug.LogWarning("GamePersistence: legacy slot migration deferred: "+ex.Message);}finally{activeSlot=previous;}
    }
    public static void ResetStaticStateForTests(){loadRequested=confirmedNewGameRequested=restoring=dirty=hasEncounterCheckpoint=pendingSchemaMigration=false;runId=null;runSeed=0;activeSlot=1;encounterStartLife=encounterStartMana=dirtySince=0;LastError=null;LastLoadSource=SaveLoadSource.None;}
}
public sealed class GamePersistenceHost:MonoBehaviour
{
    void Update()=>GamePersistence.FlushPendingAutosave(); void OnApplicationPause(bool paused){if(paused)GamePersistence.FlushPendingAutosave(true);} void OnApplicationQuit()=>GamePersistence.FlushPendingAutosave(true);
}
