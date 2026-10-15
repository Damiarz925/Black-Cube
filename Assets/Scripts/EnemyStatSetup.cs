// Developer map: Initial enemy stat buckets; prefab HealthComponent maxLife seeds Life. SetupForZone remains the extension point for future level scaling.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

//Automatically adds StatsComponent to the object if it doesn't exist on it already
[RequireComponent(typeof(StatsComponent))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyStatSetup : MonoBehaviour
{
    private StatsComponent stats; //Private statscomponent field stats
    private HealthComponent health;

    private void Awake()
    {
        stats = GetComponent<StatsComponent>(); //Get the stats component and assign it to the stats field
        health = GetComponent<HealthComponent>();

        //Later the enemy will have some base stats based on the ilvl of the enemy, and those base stats will be modified by randomly created gear items
        stats.SetBaseStat(StatTypes.AttackSpeed, 0f);   // Zero increased speed; weapon/base speed still permits attacks.

        stats.SetBaseStat(StatTypes.FireRes, 0f);   //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.ColdRes, 0f);   //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.LightRes, 0f);  //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.AllRes, 0f);    //Currently set to 0 for testing
        stats.SetBaseStat(StatTypes.FlatArmour, 0f);    //Currently set to 0 for testing
    }

    public void SetupForZone(int zoneLevel, bool isBoss)
    {
        if (stats == null)
            stats = GetComponent<StatsComponent>();
        if (health == null)
            health = GetComponent<HealthComponent>();
        if (stats == null || health == null)
        {
            Debug.LogWarning("EnemyStatSetup requires StatsComponent and HealthComponent to seed enemy life.", this);
            return;
        }

        // No level multiplier yet: each archetype's serialized maxLife is its base Life.
        stats.SetBaseStat(StatTypes.Life, health.PrefabMaxLife);
        health.UseStatsForMaximumLife(stats);
    }
}
