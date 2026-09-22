using UnityEngine;

[System.Serializable]
public struct EnemyScalingValues
{
    public int curveBreakLevel;
    public float lifeGrowth,lateLifeGrowth,damageGrowth,lateDamageGrowth,armourPerLevel,resistancePointsPerLevel,resistancePointsCap;
}

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
    public EnemyScalingValues Capture()=>new(){curveBreakLevel=curveBreakLevel,lifeGrowth=lifeGrowth,lateLifeGrowth=lateLifeGrowth,damageGrowth=damageGrowth,lateDamageGrowth=lateDamageGrowth,armourPerLevel=armourPerLevel,resistancePointsPerLevel=resistancePointsPerLevel,resistancePointsCap=resistancePointsCap};
    public void Apply(EnemyScalingValues values){curveBreakLevel=Mathf.Max(2,values.curveBreakLevel);lifeGrowth=Mathf.Max(.0001f,values.lifeGrowth);lateLifeGrowth=Mathf.Max(.0001f,values.lateLifeGrowth);damageGrowth=Mathf.Max(.0001f,values.damageGrowth);lateDamageGrowth=Mathf.Max(.0001f,values.lateDamageGrowth);armourPerLevel=Mathf.Max(0,values.armourPerLevel);resistancePointsPerLevel=Mathf.Max(0,values.resistancePointsPerLevel);resistancePointsCap=Mathf.Max(0,values.resistancePointsCap);}

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
