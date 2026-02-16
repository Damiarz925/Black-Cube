using System.Collections.Generic;


[System.Serializable]
public struct ElementalHit
{
    public Element Element; //The element of the elemental hit
    public float Amount;    //The Damage amount of the elemental hit

    public ElementalHit(Element element, float amount)  //Constructor for Elemental hit
    {
        Element = element;
        Amount = amount;
    }
}

//Damage Context is generated when a hit occurs. It holds most of the information necessary for calculating base hit damage (before any player or enemy modifiers)
public struct DamageContext
{
    public List<ElementalHit> Hits; //List of type ElementalHit that stores all of the hits in this instance of damage (Hit does cold & lightning dmg = 2 hits total. 1 context.)
    public bool IsCrit; //Bool for whether the hit is a critical strike
    public float CritMultiplier;    //Float for the crit multiplier of the hit

    public DamageContext(int initialCapacity = 4)   //Pass in the initial capacity of the hit list, or it defaults to 4. (DamageContext constructor)
    {
        Hits = new List<ElementalHit>(initialCapacity); //Create the new list with the set capacity
        IsCrit = false; //IsCrit is false by default
        CritMultiplier = 1f;    //Crit multi is 1 by default
    }

    //AddDamage function is used to actually add the damage amount for each hit to the hit list. (Called for each element type, pass in element and damage amount)
    public void AddDamage(Element element, float amount)
    {
        if (amount <= 0f) return;   //Return if hit has no damage

        for (int i = 0; i < Hits.Count; i++)    //Loop through Hits list
        {
            if (Hits[i].Element == element) //If the element that was passed in matches the element of a hit in the list, that hit is updated with the new damage value
            {
                var h = Hits[i];
                h.Amount += amount;
                Hits[i] = h;
                return;
            }
        }

        Hits.Add(new ElementalHit(element, amount));    //If we didn't enter the if statement at all, add the new hit to the list.
    }
}
