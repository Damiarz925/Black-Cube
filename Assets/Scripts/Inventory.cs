using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; } //Define instance and public getter/private setter for inventory

    private readonly List<Gear> items = new();  //List of Gear objects called items (Readonly in C# does not prevent list from being modified. It prevents the field from pointing to a different list)
    public IReadOnlyList<Gear> Items => items;  //Exposes a public readonly reference to the private list items, so that other classes can access it

    public event Action OnInventoryChanged; //Declares an action for other classes to subscribe to so that we can invoke when adding and removing items

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
        items.Add(item);    //Add the passed in item to the list
        OnInventoryChanged?.Invoke();   //Call all of the functions subscribed to this event
    }

    public void Remove(Gear item)
    {
        if (items.Remove(item)) //Remove the passed in item from the list
            OnInventoryChanged?.Invoke();   //Call all of the functions subscribed to this event
    }
}
