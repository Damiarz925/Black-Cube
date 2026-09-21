using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/Passive Tree/Class Branch", fileName = "SO_Class_Branch")]
public sealed class PassiveClassBranchSO : PassiveBranchDataSO
{
    [SerializeField] string classId = string.Empty;
    [SerializeField] string signatureWeaponId = string.Empty;
    [SerializeField] string subclassAId = string.Empty;
    [SerializeField] string subclassBId = string.Empty;
    [SerializeField] List<PassiveClassTierData> tiers = new();
    public string ClassId => classId ?? string.Empty;
    public string SignatureWeaponId => signatureWeaponId ?? string.Empty;
    public string SubclassAId => subclassAId ?? string.Empty;
    public string SubclassBId => subclassBId ?? string.Empty;
    public IReadOnlyList<PassiveClassTierData> Tiers => tiers;
    public override string RouteId => ClassId;
    public override int TierCount => tiers.Count;
    public override IEnumerable<PassiveAuthoredNode> AllAuthoredNodes()
    {
        foreach (var tier in tiers) { yield return tier.Spine; foreach (var node in tier.Left.GenericNodes) yield return node; yield return tier.Left.SubclassA; yield return tier.Left.SubclassB; foreach (var node in tier.Right.GenericNodes) yield return node; yield return tier.Right.SubclassA; yield return tier.Right.SubclassB; }
    }
    public void Configure(string id, string weaponId, string firstSubclassId, string secondSubclassId, List<PassiveClassTierData> authoredTiers)
    { classId = id ?? string.Empty; signatureWeaponId = weaponId ?? string.Empty; subclassAId = firstSubclassId ?? string.Empty; subclassBId = secondSubclassId ?? string.Empty; tiers = authoredTiers ?? new(); }
}
