using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
public class PlayerStatSetup : MonoBehaviour
{
    private StatsComponent stats;

    private void Awake()
    {
        stats = GetComponent<StatsComponent>();

        stats.SetBaseStat(StatTypes.Life, 100f);
        stats.SetBaseStat(StatTypes.WeaponBaseDmg, 20f);
        stats.SetBaseStat(StatTypes.AttackSpeed, 0f);

        stats.SetBaseStat(StatTypes.FireRes, 0f);
        stats.SetBaseStat(StatTypes.ColdRes, 0f);
        stats.SetBaseStat(StatTypes.LightRes, 0f);
        stats.SetBaseStat(StatTypes.AllRes, 0f);
    }
}
