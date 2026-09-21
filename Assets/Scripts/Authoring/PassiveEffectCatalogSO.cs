using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/Passive Tree/Effect Catalog", fileName = "SO_PassiveEffectCatalog")]
public sealed class PassiveEffectCatalogSO : ScriptableObject
{
    [SerializeField] List<PassiveMechanicDefinition> mechanics = new();
    public IReadOnlyList<PassiveMechanicDefinition> Mechanics => mechanics;
    public bool Contains(string id) { foreach (var mechanic in mechanics) if (mechanic != null && mechanic.StableId == id) return true; return false; }
    public void Configure(List<PassiveMechanicDefinition> authoredMechanics) => mechanics = authoredMechanics ?? new();
}
