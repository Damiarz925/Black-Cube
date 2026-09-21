using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/Passive Tree/Weapon Branch", fileName = "SO_Weapon_Branch")]
public sealed class PassiveWeaponBranchSO : PassiveBranchDataSO
{
    [SerializeField] string weaponId = string.Empty;
    [SerializeField] string owningClassId = string.Empty;
    [SerializeField] List<PassiveWeaponTierData> tiers = new();
    public string WeaponId => weaponId ?? string.Empty;
    public string OwningClassId => owningClassId ?? string.Empty;
    public IReadOnlyList<PassiveWeaponTierData> Tiers => tiers;
    public override string RouteId => WeaponId;
    public override int TierCount => tiers.Count;
    public override IEnumerable<PassiveAuthoredNode> AllAuthoredNodes()
    { foreach (var tier in tiers) { yield return tier.Spine; foreach (var node in tier.Left.GenericNodes) yield return node; foreach (var node in tier.Right.GenericNodes) yield return node; } }
    public void Configure(string id, string classId, List<PassiveWeaponTierData> authoredTiers)
    { weaponId = id ?? string.Empty; owningClassId = classId ?? string.Empty; tiers = authoredTiers ?? new(); }
}
