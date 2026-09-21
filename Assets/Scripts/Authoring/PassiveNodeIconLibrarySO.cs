using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/UI/Passive Node Icon Library", fileName = "SO_PassiveNodeIconLibrary")]
public sealed class PassiveNodeIconLibrarySO : ScriptableObject
{
    [SerializeField] Sprite genericFallback;
    [SerializeField] List<PassiveIconMapping> mappings = new();
    public Sprite GenericFallback => genericFallback;
    public IReadOnlyList<PassiveIconMapping> Mappings => mappings;
    public Sprite Resolve(PassiveAuthoredNode node, PassiveNodeVisualState state = PassiveNodeVisualState.Inactive)
    {
        if (node == null) return genericFallback;
        if (node.IconMode == PassiveIconMode.Custom && node.IconOverride != null) return node.IconOverride;
        if (node.Effects.Count > 0 && node.Effects[0].Kind == PassiveEffectKind.Stat)
            foreach (var mapping in mappings) if (mapping != null && mapping.Effect == node.Effects[0].Stat && mapping.Normal != null) return mapping.Resolve(state);
        return genericFallback;
    }
    public void Configure(Sprite fallback, List<PassiveIconMapping> authoredMappings) { genericFallback = fallback; mappings = authoredMappings ?? new(); }
}
