// Developer map: Prefab Life seeds intrinsic scaling. Source-owned defense bonuses remain distinct from gear.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

//Automatically adds StatsComponent to the object if it doesn't exist on it already
[RequireComponent(typeof(StatsComponent))]
[RequireComponent(typeof(HealthComponent))]
public class EnemyStatSetup : MonoBehaviour
{
    private StatsComponent stats; //Private statscomponent field stats
    private HealthComponent health;
    public EnemyScalingMath.Intrinsic Intrinsic { get; private set; }
    public float AuthoredLevelOneLife => health != null ? health.PrefabMaxLife : GetComponent<HealthComponent>().PrefabMaxLife;

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
        => SetupForZone(zoneLevel, isBoss, null);

    public void SetupForZone(int zoneLevel, bool isBoss, EnemyScalingValues? previewOverride)
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

        Intrinsic = previewOverride.HasValue
            ? EnemyScalingMath.Calculate(zoneLevel, previewOverride.Value)
            : EnemyScalingMath.Calculate(zoneLevel);
        stats.BeginUpdate();
        try
        {
            stats.RemoveModifiersFromSource(this);
            stats.SetBaseStat(StatTypes.Life, Intrinsic.ScaledLife(health.PrefabMaxLife));
            stats.AddModifier(new StatModifier(StatTypes.FlatArmour, StatOp.Additive, Intrinsic.Armour, this));
            stats.AddModifier(new StatModifier(StatTypes.FireRes, StatOp.Additive, Intrinsic.ResistancePoints, this));
            stats.AddModifier(new StatModifier(StatTypes.ColdRes, StatOp.Additive, Intrinsic.ResistancePoints, this));
            stats.AddModifier(new StatModifier(StatTypes.LightRes, StatOp.Additive, Intrinsic.ResistancePoints, this));
            stats.AddModifier(new StatModifier(StatTypes.VoidRes, StatOp.Additive, Intrinsic.ResistancePoints, this));
        }
        finally { stats.EndUpdate(); }
        health.UseStatsForMaximumLife(stats);
    }
}
