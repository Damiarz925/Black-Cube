using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/Passive Tree/Database", fileName = "SO_PassiveTreeDatabase")]
public sealed class PassiveTreeDatabaseSO : ScriptableObject
{
    [SerializeField] List<PassiveClassBranchSO> classBranches = new();
    [SerializeField] List<PassiveWeaponBranchSO> weaponBranches = new();
    [SerializeField] PassiveNodeIconLibrarySO iconLibrary;
    [SerializeField] PassiveEffectCatalogSO effectCatalog;
    public IReadOnlyList<PassiveClassBranchSO> ClassBranches => classBranches;
    public IReadOnlyList<PassiveWeaponBranchSO> WeaponBranches => weaponBranches;
    public PassiveNodeIconLibrarySO IconLibrary => iconLibrary;
    public PassiveEffectCatalogSO EffectCatalog => effectCatalog;
    public void Configure(List<PassiveClassBranchSO> classes, List<PassiveWeaponBranchSO> weapons, PassiveNodeIconLibrarySO icons, PassiveEffectCatalogSO effects)
    { classBranches = classes ?? new(); weaponBranches = weapons ?? new(); iconLibrary = icons; effectCatalog = effects; }
}
