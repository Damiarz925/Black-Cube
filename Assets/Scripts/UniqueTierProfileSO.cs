using UnityEngine;

[CreateAssetMenu(menuName="Black-Cube/Balance/Unique Tier Profile")]
public sealed class UniqueTierProfileSO:ScriptableObject
{
    public ModDatabase database;
    public int[] minimumLevels={1,20,40,60,80};
    public float[] magnitude={.22f,.38f,.58f,.78f,1};
    public float signatureMinimumPremium=1.1f,signatureMaximumPremium=2.2f;
    public static UniqueTierProfileSO Current=>Resources.Load<UniqueTierProfileSO>("UniqueTierProfile");
}
