// Developer map: Early player baseline setup before health Awake reads Life. Values entering StatsComponent use raw units: Life is HP and percentage buckets use percentage points.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

[RequireComponent(typeof(StatsComponent))]
[DefaultExecutionOrder(-100)]
public class PlayerStatSetup : MonoBehaviour
{
    private StatsComponent stats;

    private void Awake()
    {
        stats = GetComponent<StatsComponent>();

        stats.SetBaseStat(StatTypes.Life, 1000f);
        stats.SetBaseStat(StatTypes.Mana, 100f);
        stats.SetBaseStat(StatTypes.UnarmedDamage, 0f);
        stats.SetBaseStat(StatTypes.AttackSpeed, 0f);

        stats.SetBaseStat(StatTypes.FireRes, 0f);
        stats.SetBaseStat(StatTypes.ColdRes, 0f);
        stats.SetBaseStat(StatTypes.LightRes, 0f);
        stats.SetBaseStat(StatTypes.AllRes, 0f);
    }
}
