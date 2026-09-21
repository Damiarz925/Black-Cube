using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class PassiveBranchBinding : MonoBehaviour
{
    [SerializeField] string routeId = string.Empty;
    [SerializeField] PassiveBranchDataSO data;
    [SerializeField] List<PassiveTierViewBinding> tiers = new();
    [SerializeField] bool showAuthoringLabels;
    public string RouteId => routeId ?? string.Empty;
    public PassiveBranchDataSO Data => data;
    public IReadOnlyList<PassiveTierViewBinding> Tiers => tiers;
    public bool ShowAuthoringLabels { get => showAuthoringLabels; set { showAuthoringLabels = value; RefreshAuthoringPreview(); } }
    public IEnumerable<PassiveNodeBinding> AllNodes() { foreach (var tier in tiers) { if (tier?.spine != null) yield return tier.spine; if (tier?.left != null) foreach (var node in tier.left.Nodes) if (node != null) yield return node; if (tier?.right != null) foreach (var node in tier.right.Nodes) if (node != null) yield return node; } }
    public PassiveNodeBinding Find(string logicalSlotId) { foreach (var node in AllNodes()) if (node.LogicalSlotId == logicalSlotId) return node; return null; }
    public void Configure(string id, PassiveBranchDataSO branchData, List<PassiveTierViewBinding> authoredTiers) { routeId = id ?? string.Empty; data = branchData; tiers = authoredTiers ?? new(); RefreshAuthoringPreview(); }
    void OnEnable() { if (!Application.isPlaying) RefreshAuthoringPreview(); }
    void OnValidate() { if (!Application.isPlaying) RefreshAuthoringPreview(); }
    public void RefreshAuthoringPreview()
    {
        if (data == null) return;
        var lookup = new Dictionary<string, PassiveAuthoredNode>(StringComparer.Ordinal);
        foreach (var node in data.AllAuthoredNodes()) if (node != null && !string.IsNullOrEmpty(node.LogicalSlotId) && !lookup.ContainsKey(node.LogicalSlotId)) lookup.Add(node.LogicalSlotId, node);
        PassiveTreeDatabaseSO database = Resources.Load<PassiveTreeDatabaseSO>("GameData/PassiveTree/SO_PassiveTreeDatabase");
        foreach (var view in AllNodes()) if (lookup.TryGetValue(view.LogicalSlotId, out PassiveAuthoredNode node)) view.ApplyAuthoringPreview(node, database != null ? database.IconLibrary : null, showAuthoringLabels);
    }
}
