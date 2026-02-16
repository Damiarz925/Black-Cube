#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ModDatabase", menuName = "Scriptable Objects/ModDatabase")]
public class ModDatabase : ScriptableObject
{
    [SerializeField] private List<AffixDefinitions> allAffixes = new(); //New list of affix definitions

    private Dictionary<StatTypes, AffixDefinitions> lookupByEnum;       //Dictionary where key is stat types, and value is affix definitions
    private AffixDefinitions[] lookupById;  //array of affix definitions used to lookup by id

    public void Initialize()
    {
        if (lookupByEnum != null)   //if lookupbyenum is not null return
            return;

        if (allAffixes == null)     //if allaffixes is null, create new list
            allAffixes = new List<AffixDefinitions>();

        lookupByEnum = new Dictionary<StatTypes, AffixDefinitions>();   //create new dictionary for lookupbyenums

        int maxId = -1; //int maxid defined as -1

        foreach (var def in allAffixes) //loop through each definition in allaffixes
        {
            if (def == null) continue;  //if def is null, skip it
            lookupByEnum[def.statType] = def;   //set lookupbyenum at that stat type to def

            int id = (int)def.statType; //id is the affix definition's stat type casted to int
            if (id > maxId) maxId = id; //set maxID to ID if ID is greater than maxID
        }

        lookupById = new AffixDefinitions[maxId + 1];   //lookupbyID is a new affixdefinitions array of size max ID + 1

        foreach (var kvp in lookupByEnum)   //for each key value pair in lookup by enum, set lookupbyID at the key's ID to the value from lookupbyenum
            lookupById[(int)kvp.Key] = kvp.Value;
    }

    public AffixDefinitions GetDefinition(StatTypes stat)
    {
        Initialize();   //Call initialize
        return lookupByEnum.TryGetValue(stat, out var def) ? def : null;    //use lookupbyenum to try to get the value for the passed in stat, if it isn't found, return null
    }

    public bool TryGetDefinition(StatTypes stat, out AffixDefinitions def)
    {
        Initialize();   //Call initialize
        return lookupByEnum.TryGetValue(stat, out def); //use lookupbyenum to try to get the definition, and store it back in def
    }

    public AffixDefinitions GetDefinition(int statId)
    {
        Initialize();   //Call initialize
        if (statId < 0 || statId >= lookupById.Length) return null; //If statID is less than 0, or statID is greater than or equal to lookupbyID's length, return null, otherwise return lookupbyID at the passed in int (statID)
        return lookupById[statId];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (allAffixes == null) //if allaffixes is null, create a new list of affixdefinitions
            allAffixes = new List<AffixDefinitions>();

        // Ensure an AffixDefinitions exists for every StatType
        var map = allAffixes.Where(a => a != null)  //creates a dictionary where each affix is stored under its stattype
                            .ToDictionary(a => a.statType, a => a);

        allAffixes = Enum.GetValues(typeof(StatTypes))  //loops through entire stat types enum to generate def for each and add it to list
            .Cast<StatTypes>()
            .Select(stat =>
            {
                if (!map.TryGetValue(stat, out var def) || def == null) //If there is no def value or def is null
                    def = new AffixDefinitions { statType = stat }; //create a new affixdefinitions setting stattype equal to stat

                def.EnsureTiersGenerated(); //ensure that tiers were generated
                return def; //return the definition
            }).ToList();

        AutoPopulateAllowedSlots();

        lookupByEnum = null;    //set lookupbyenum and lookupbyid to null
        lookupById = null;

        EditorUtility.SetDirty(this);   //mark this object as dirty
    }

    private void AutoPopulateAllowedSlots()
    {
        var gearPools = GearStatLists.BuildDefaultStatPools();  //build default stat pools
        var statToSlots = new Dictionary<StatTypes, List<LootManager.GearType>>();      //create dictionary where key is stat type and value is a list of gear types

        // Reverse mapping: stat → all gear types that allow it
        foreach (var kv in gearPools)   //for each key value in gear pools
        {
            var gearType = kv.Key;  //gear type is the key 
            foreach (var stat in kv.Value)  //for each stat in  kv.value
            {
                if (!statToSlots.TryGetValue(stat, out var list))   //if we can't get the value of the current stat in statToSlots, create a new list tied to this key
                    statToSlots[stat] = list = new List<LootManager.GearType>();

                if (!list.Contains(gearType))   //if list doesn't contain the current gear type, add it
                    list.Add(gearType);
            }
        }

        foreach (var def in allAffixes) //for each definition in allaffixes
        {
            if (statToSlots.TryGetValue(def.statType, out var slots))   //if we find the stat type in stattoslots, out slots and set def.allowslots to slots.toarray
                def.allowedSlots = slots.ToArray();
            else
                def.allowedSlots = Array.Empty<LootManager.GearType>(); //otherwise, def.allowedslots is an empty array of gear types
        }
    }
#endif
}
