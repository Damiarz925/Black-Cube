using UnityEngine;

//Automatically adds StatsComponent to the object if it doesn't exist on it already
[RequireComponent(typeof(StatsComponent))]
public class EnemyStatSetup : MonoBehaviour
{
    private StatsComponent stats; //Private statscomponent field stats

    private void Awake()
    {
        stats = GetComponent<StatsComponent>(); //Get the stats component and assign it to the stats field

        //Later the enemy will have some base stats based on the ilvl of the enemy, and those base stats will be modified by randomly created gear items
        stats.SetBaseStat(StatTypes.Life, 80f); //Currently manually sets the base stats of the enemy
        stats.SetBaseStat(StatTypes.AttackSpeed, 0f);   //Currently set to not attack for testing

        stats.SetBaseStat(StatTypes.FireRes, 0f);   //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.ColdRes, 0f);   //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.LightRes, 0f);  //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.AllRes, 0f);    //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.FlatArmour, 0f);    //Currently set to 0 for testing
    }

    public void SetupForZone(int zoneLevel, bool isBoss)
    {
        // Still open for future scaling via ZoneManager multipliers.
    }
}
