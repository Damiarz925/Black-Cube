using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Explicit isolated-build context prevents previews from reading the live player's gear or Relics.
public sealed class UniqueLoadoutState:MonoBehaviour
{
    public readonly List<UniqueRoll> powers=new();
    public float Power(UniquePower power)=>powers.Where(x=>x.power==power).Sum(x=>x.value);
    public int AuraMask=>powers.Where(x=>x.power==UniquePower.GrantedAuras).Aggregate(0,(mask,x)=>mask|x.auraMask);
}
