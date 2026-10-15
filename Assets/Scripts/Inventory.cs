// Developer map: Session item ownership, pickup-only auto-scrap and one accumulated Scrap stack. Pickup applies filters once; Add returns equipped items without applying pickup filters.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; } //Define instance and public getter/private setter for inventory

    private readonly List<Gear> items = new();  //List of Gear objects called items (Readonly in C# does not prevent list from being modified. It prevents the field from pointing to a different list)
    public IReadOnlyList<Gear> Items => items;  //Exposes a public readonly reference to the private list items, so that other classes can access it

    public event Action OnInventoryChanged; //Declares an action for other classes to subscribe to so that we can invoke when adding and removing items
    public bool FilterLevelEnabled { get; set; }
    public bool FilterRarityEnabled { get; set; }
    public int FilterLevel { get; set; } = 10;
    public LootManager.GearRarity FilterRarity { get; set; } = LootManager.GearRarity.Magic;
    public bool MatchesFilter(Gear item) => item != null && !item.IsScrap &&
        ((FilterLevelEnabled && item.ItemLevel <= Mathf.Max(1, FilterLevel)) ||
         (FilterRarityEnabled && item.ItemRarity <= FilterRarity));

    // Only world reward pickups use this entry point. Equipment returns use Add.
    public bool Pickup(Gear item)
    {
        if (item == null || item.PickupClaimed || item.Dismantled || items.Contains(item) ||
            (EquipmentManager.Instance != null && EquipmentManager.Instance.GetEquipped(item.ItemType) == item)) return false;
        item.PickupClaimed = true;
        items.Add(item);
        if (MatchesFilter(item) && TryDismantle(item)) return true;
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void Awake()    //Logic for singleton is currently in awake, as well as declaring object as Don't Destroy On Load
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //Function used to add gear items to the items list (the list is essentially the items shown in the inventory)
    public void Add(Gear item)
    {
        if (item == null || item.Dismantled || items.Contains(item)) return;
        items.Add(item);    //Add the passed in item to the list
        OnInventoryChanged?.Invoke();   //Call all of the functions subscribed to this event
    }

    public void Remove(Gear item)
    {
        if (items.Remove(item)) //Remove the passed in item from the list
            OnInventoryChanged?.Invoke();   //Call all of the functions subscribed to this event
    }

    public static int ScrapYield(Gear item) => item == null || item.IsScrap ? 0 : item.ItemRarity switch
    {
        LootManager.GearRarity.Normal => 1,
        LootManager.GearRarity.Magic => 2,
        LootManager.GearRarity.Rare => 3,
        LootManager.GearRarity.Legendary => 5,
        _ => 0
    };

    public bool CanDismantle(Gear item) => ScrapYield(item) > 0 && !item.Dismantled && items.Contains(item)
        && (EquipmentManager.Instance == null || EquipmentManager.Instance.GetEquipped(item.ItemType) != item);

    public bool TryDismantle(Gear item)
    {
        if (!CanDismantle(item)) return false;
        int amount = ScrapYield(item);
        Gear scrap = items.Find(g => g != null && g.IsScrap);
        if (scrap != null && scrap.StackCount > int.MaxValue - amount) return false;
        // Claim before notifications or delayed destruction, so repeat callbacks cannot pay twice.
        int index = items.IndexOf(item);
        item.Dismantled = true;
        items.RemoveAt(index);
        if (scrap == null)
        {
            var go = new GameObject("Scrap");
            go.transform.SetParent(transform, false);
            scrap = go.AddComponent<Gear>();
            scrap.InitializeScrap(amount);
            items.Insert(index, scrap);
        }
        else scrap.AddScrap(amount);
        Destroy(item.gameObject);
        OnInventoryChanged?.Invoke();
        return true;
    }
}
