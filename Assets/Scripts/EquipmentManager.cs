// Developer map: Transfers Gear between inventory and one slot per GearType, replacing source-owned global modifiers. Batches StatsChanged, then emits EquipmentChanged; weapon swaps also notify PlayerController.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; } //Public getter/private setter defining the instance (part of creating a singleton)

    [SerializeField] private StatsComponent playerStats;
    [SerializeField] private PlayerController playerController;

    private readonly Dictionary<LootManager.GearType, Gear> equipped = new(); //Dictionary. Key is GearType enum, Value is Gear object
    public event System.Action EquipmentChanged;
    public IReadOnlyDictionary<LootManager.GearType, Gear> EquippedItems => equipped;
    public Gear GetEquipped(LootManager.GearType type) => equipped.TryGetValue(type, out var gear) ? gear : null;

    private void Awake() //Safely declare singleton on awake and find the statscomponent and playercontroller
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    //This function is used whenever the player wants to equip a new piece of equipment.
    public void Equip(Gear gear)
    {
        //If the passed gear item is null, exit the function
        if (gear == null || gear.IsScrap || gear.Dismantled) return;
        if (GetEquipped(gear.ItemType) == gear) return;

        EnsurePlayerReferences();

        //Define variables for the item type of the gear item, and the inventory instance
        var slot = gear.ItemType;
        var inventory = Inventory.Instance;
        inventory?.RetainForRun(gear);
        playerStats?.BeginUpdate();
        try
        {

        //If the inventory isn't null, remove the item from it
        if (inventory != null)
            inventory.Remove(gear);

        //We use trygetvalue to check for an item in that slot, if it exists, we remove the stats of the old item from the player and add the old item to the player's inventory.
        if (equipped.TryGetValue(slot, out var oldGear))
        {
            if (playerStats != null)
                playerStats.RemoveModifiersFromSource(oldGear);

            if (inventory != null)
                inventory.Add(oldGear);
        }

        //Sets the value at key [slot] to "gear" or creates if nonexistent
        equipped[slot] = gear;

        //Add the global modifiers as stat modifiers
        if (playerStats != null)
        {
            foreach (var mod in gear.globalRolledMods) //Loops through the mods in the item's global rolled mods (excludes local mods)
            {
                StatOp op = GetOperationForStat(mod.statType); //Finds the correct operation to apply (additive vs. multiplicative modifiers)
                var statMod = new StatModifier(mod.statType, op, mod.value, gear); //Creates a new statmodifier object defining the stat type, operation type, value, and gear piece
                playerStats.AddModifier(statMod); //Adds that newly created modifier to the player character
            }
        }

        // If this is a weapon, notify the PlayerController
        if (slot == LootManager.GearType.Weapons && playerController != null)
        {
            playerController.EquipWeapon(gear);
        }
        }
        finally { playerStats?.EndUpdate(); }
        EquipmentChanged?.Invoke();
    }

    public void Unequip(LootManager.GearType slot)
    {
        EnsurePlayerReferences();
        if (!equipped.TryGetValue(slot, out var gear)) return;
        playerStats?.BeginUpdate();
        try
        {
            equipped.Remove(slot);
            playerStats?.RemoveModifiersFromSource(gear);
            if (slot == LootManager.GearType.Weapons && playerController != null) playerController.EquipWeapon(null);
            if (Inventory.Instance != null) Inventory.Instance.Add(gear);
        }
        finally { playerStats?.EndUpdate(); }
        EquipmentChanged?.Invoke();
    }

    public void NotifyItemChanged(Gear gear)
    {
        if (gear == null || GetEquipped(gear.ItemType) != gear) return;
        EnsurePlayerReferences(); playerStats?.BeginUpdate();
        try
        {
            playerStats?.RemoveModifiersFromSource(gear);
            if (playerStats != null)
                foreach (var mod in gear.globalRolledMods)
                    playerStats.AddModifier(new StatModifier(mod.statType, GetOperationForStat(mod.statType), mod.value, gear));
            if (gear.ItemType == LootManager.GearType.Weapons && playerController != null) playerController.EquipWeapon(gear);
        }
        finally { playerStats?.EndUpdate(); }
        EquipmentChanged?.Invoke();
    }

    public void ResetForNewRun()
    {
        EnsurePlayerReferences(); playerStats?.BeginUpdate();
        try
        {
            foreach (var pair in equipped)
            {
                playerStats?.RemoveModifiersFromSource(pair.Value);
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            equipped.Clear();
            if (playerController != null) playerController.EquipWeapon(null);
        }
        finally { playerStats?.EndUpdate(); }
        EquipmentChanged?.Invoke();
    }

    public void ResetForRebirth() => ResetForNewRun();

    //This grabs the correct operation for the stat
    private StatOp GetOperationForStat(StatTypes stat)
    {
        return StatMappings.GetRolledModifierOperation(stat);
    }

    private void EnsurePlayerReferences()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == GameSceneNames.Gameplay &&
            (playerStats == null || playerStats.gameObject.scene != scene ||
             playerController == null || playerController.gameObject.scene != scene))
            BindSceneReferences(scene);
    }

    public void BindSceneReferences(Scene scene)
    {
        if (scene.name != GameSceneNames.Gameplay || !scene.isLoaded) { ReleaseSceneReferences(); return; }
        PlayerController nextController = FindInScene<PlayerController>(scene);
        StatsComponent nextStats = nextController != null ? nextController.GetComponentInChildren<StatsComponent>() : null;
        if (playerController == nextController && playerStats == nextStats) return;
        ReleaseSceneReferences();
        playerController = nextController;
        playerStats = nextStats;
        ReapplyEquipmentToScenePlayer();
    }

    public void ReleaseSceneReferences()
    {
        playerStats = null;
        playerController = null;
    }

    private void ReapplyEquipmentToScenePlayer()
    {
        if (playerStats == null) return;
        playerStats.BeginUpdate();
        try
        {
            foreach (Gear gear in equipped.Values)
            {
                if (gear == null) continue;
                foreach (RolledMod mod in gear.globalRolledMods)
                    playerStats.AddModifier(new StatModifier(mod.statType, GetOperationForStat(mod.statType), mod.value, gear));
            }
            if (playerController != null) playerController.EquipWeapon(GetEquipped(LootManager.GearType.Weapons));
        }
        finally { playerStats.EndUpdate(); }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindSceneReferences(scene);
    private void OnSceneUnloaded(Scene scene) { if (scene.name == GameSceneNames.Gameplay) ReleaseSceneReferences(); }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        ReleaseSceneReferences();
        Instance = null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
