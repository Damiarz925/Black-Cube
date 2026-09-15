using UnityEngine;

// The single tunable source for intrinsic enemy progression. Gear never owns these values.
[CreateAssetMenu(menuName = "Black Cube/Enemy Scaling Profile")]
public sealed class EnemyScalingProfile : ScriptableObject
{
    [SerializeField] private int curveBreakLevel = 100;
    [SerializeField] private float lifeGrowth = 1.04f;
    [SerializeField] private float lateLifeGrowth = 1.02f;
    [SerializeField] private float damageGrowth = 1.03f;
    [SerializeField] private float lateDamageGrowth = 1.015f;
    [SerializeField] private float armourPerLevel = 5f;
    [SerializeField] private float resistancePointsPerLevel = .15f;
    [SerializeField] private float resistancePointsCap = 20f;

    public int CurveBreakLevel => curveBreakLevel;
    public float LifeGrowth => lifeGrowth;
    public float LateLifeGrowth => lateLifeGrowth;
    public float DamageGrowth => damageGrowth;
    public float LateDamageGrowth => lateDamageGrowth;
    public float ArmourPerLevel => armourPerLevel;
    public float ResistancePointsPerLevel => resistancePointsPerLevel;
    public float ResistancePointsCap => resistancePointsCap;

    private static EnemyScalingProfile defaultProfile;
    public static EnemyScalingProfile Default
    {
        get
        {
            if (defaultProfile == null)
                defaultProfile = Resources.Load<EnemyScalingProfile>("EnemyScalingProfile");
            if (defaultProfile == null)
                throw new System.InvalidOperationException("Missing Assets/Resources/EnemyScalingProfile.asset");
            return defaultProfile;
        }
    }
}
