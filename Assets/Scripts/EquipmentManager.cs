using UnityEngine;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; } //Public getter/private setter defining the instance (part of creating a singleton)

    [SerializeField] private StatsComponent playerStats;      
    [SerializeField] private PlayerController playerController;

    private readonly Dictionary<LootManager.GearType, Gear> equipped = new(); //Dictionary. Key is GearType enum, Value is Gear object
    public event System.Action EquipmentChanged;
    public Gear GetEquipped(LootManager.GearType type) => equipped.TryGetValue(type, out var gear) ? gear : null;

    private void Awake() //Safely declare singleton on awake and find the statscomponent and playercontroller
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsurePlayerReferences();
    }

    //This function is used whenever the player wants to equip a new piece of equipment.
    public void Equip(Gear gear)
    {
        //If the passed gear item is null, exit the function
        if (gear == null) return;          
        if (GetEquipped(gear.ItemType) == gear) return;

        EnsurePlayerReferences();
        
        //Define variables for the item type of the gear item, and the inventory instance
        var slot = gear.ItemType;
        var inventory = Inventory.Instance;
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

    //This grabs the correct operation for the stat
    private StatOp GetOperationForStat(StatTypes stat)
    {
        string name = stat.ToString(); //Changes the stat's name to a string
        if (name.StartsWith("Flat")) return StatOp.Flat; //Checks if it starts with flat to determine if it needs to be added as a flat value
        if (name.EndsWith("Mult")) return StatOp.Multiplicative; //Checks if it ends with mult to determine if it's a "More" modifier
        return StatOp.Additive; //Otherwise, the value is additive "increased" modifier
    }

    private void EnsurePlayerReferences()
    {
        if (playerStats == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerStats = playerObject.GetComponentInChildren<StatsComponent>();
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }
}
