using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PassiveTreeView : MonoBehaviour
{
    public GameObject panel;
    public ScrollRect scroll;
    public RectTransform content;
    public TMP_Text points, progression, details;
    public Button closeButton, refundAllButton;
    public TMP_Text refundAllLabel;
    public List<PassiveBranchBinding> branches = new();
    public List<PassiveConnectionBinding> connections = new();
    public IEnumerable<PassiveNodeBinding> AllNodes { get { foreach (var branch in branches) if (branch != null) foreach (var node in branch.AllNodes()) if (node != null) yield return node; } }
    public void Configure(GameObject panelRoot, ScrollRect scrollView, RectTransform contentRoot, TMP_Text pointLabel, TMP_Text progressionLabel, TMP_Text detailLabel, Button close, Button refund, TMP_Text refundLabel, List<PassiveBranchBinding> branchViews, List<PassiveConnectionBinding> lineViews)
    { panel = panelRoot; scroll = scrollView; content = contentRoot; points = pointLabel; progression = progressionLabel; details = detailLabel; closeButton = close; refundAllButton = refund; refundAllLabel = refundLabel; branches = branchViews ?? new(); connections = lineViews ?? new(); }
}
