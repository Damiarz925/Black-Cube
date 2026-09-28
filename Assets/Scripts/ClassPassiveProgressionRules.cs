using System;
using System.Collections.Generic;
using System.Linq;

// Shared authority for live allocations, save validation and laboratory legality.
public static class ClassPassiveProgressionRules
{
    public static IEnumerable<int> Keystones(string route)=>PassiveTreeDefinition.RouteNodes(route).Where(id=>PassiveTreeDefinition.Node(id).Kind==PassiveNodeKind.Keystone);
    public static bool SidesComplete(string route,Func<int,bool> allocated)
    {
        for(int tier=1;tier<=10;tier++)foreach(string side in new[]{"left","right"})
            if(!PassiveTreeDefinition.ChoiceNodes($"tree.v3.{route}.t{tier:00}.{side}").Any(allocated))return false;
        return true;
    }
    public static bool KeystoneEligible(string route,Func<int,bool> allocated)=>PassiveTreeDefinition.IsClassSpineComplete(route,allocated)&&SidesComplete(route,allocated);
    public static bool ClassComplete(string route,Func<int,bool> allocated)=>KeystoneEligible(route,allocated)&&Keystones(route).Count(allocated)==1;
    public static bool CanSelectRoute(string home,IReadOnlyList<string> selected,Func<int,bool> allocated)=>ClassComplete(home,allocated)&&selected.Count<5&&(selected.Count==0||PassiveTreeDefinition.IsClassSpineComplete(selected[selected.Count-1],allocated));
    public static bool ValidSelections(string home,IReadOnlyList<string> routes,string weapon)
    {if(routes==null||routes.Count>5||routes.Distinct().Count()!=routes.Count||routes.Any(x=>x==home||!PlayerClassCatalog.IsValid(x)))return false;return string.IsNullOrEmpty(weapon)||WeaponTypeCatalog.IsValid(weapon);}
    public static bool Validate(int[] values,string home,string subclass,IReadOnlyList<string> routes,string weapon)
    {
        if(values==null||values.Length!=PassiveTreeDefinition.NodeCount||!PlayerClassCatalog.IsValid(home)||!ValidSelections(home,routes,weapon))return false;
        bool A(int id)=>id>=0&&id<values.Length&&values[id]==1;
        bool complete=ClassComplete(home,A);
        if((routes.Count>0||!string.IsNullOrEmpty(weapon))&&!complete)return false;
        for(int i=0;i<routes.Count-1;i++)if(!PassiveTreeDefinition.IsClassSpineComplete(routes[i],A))return false;
        foreach(var n in PassiveTreeDefinition.Nodes)
        {
            if(values[n.Id] is not (0 or 1))return false;if(!A(n.Id))continue;
            if(n.IsClassRoute&&n.RouteClassId!=home&&!routes.Contains(n.RouteClassId))return false;
            if(n.IsWeaponRoute&&n.RouteWeaponId!=weapon)return false;
            if(n.IsChoice)
            {
                if(n.Kind==PassiveNodeKind.Keystone){if(!KeystoneEligible(n.RouteClassId,A))return false;}
                else if(!A(n.PrerequisiteId))return false;
                if(n.IsSubclassChoice&&(n.RouteClassId!=home||string.IsNullOrEmpty(subclass)))return false;
                if(PassiveTreeDefinition.ChoiceNodes(n.ChoiceGroupId).Count(A)!=1)return false;
            }
            else if(n.Kind==PassiveNodeKind.Spine){if(n.Tier>1&&!A(PassiveTreeDefinition.ClassSpineNode(n.RouteClassId,n.Tier-1)))return false;}
            else if(n.Kind==PassiveNodeKind.WeaponSpine){if(!complete||n.Tier>1&&!A(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,n.Tier-1)))return false;}
            else return false;
        }
        return true;
    }
}
