// Developer map: Builds the complete data-driven radial passive menu over PaperBattleHUD.
// Dragging and wheel zoom navigate the deterministic V3 radial class/weapon routes.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTreeUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public static bool PausesGameplay => IsOpen && GameplayOptions.PauseWhenOpen(GameplayWindow.PassiveTree);

    static readonly Color ConnectionInactive = new Color(.32f, .29f, .25f, 1f);
    static readonly Color ConnectionAllocated = new Color(.12f, .48f, .9f, 1f);

    [SerializeField] PassiveTreeView authoredView;
    PaperBattleHUD hud;
    GameObject panel;
    TMP_Text points;
    TMP_Text xp;
    TMP_Text details;
    Button refundAll;
    TMP_Text refundAllLabel;
    bool refundAllConfirmation;
    ScrollRect scroll;
    readonly Button[] nodes = new Button[PassiveTreeDefinition.NodeCount];
    readonly PassiveNodeBinding[] nodeBindings = new PassiveNodeBinding[PassiveTreeDefinition.NodeCount];
    readonly List<PassiveConnectionView> connections = new();
    readonly Dictionary<string, PassiveJunctionView> junctions = new();
    readonly Dictionary<PassiveBranch, TMP_Text> branchLabels = new();
    PlayerProgression progression;
    int selectedNode = -1;
    float treeZoom = 1f;

    const float MinimumTreeZoom = .11f;
    const float MaximumTreeZoom = 1.35f;
    const float TreeZoomStep = .08f;

    public GameObject Panel => panel;
    public Button NodeButton(int index) => index >= 0 && index < nodes.Length ? nodes[index] : null;
    public int JunctionCount => junctions.Count;

    sealed class PassiveConnectionView
    {
        public int A;
        public int B;
        public int VisibilityNodeId;
        public bool IsJunctionStem;
        public Image Image;
        public Outline Outline;
    }

    sealed class PassiveJunctionView
    {
        public int SpineNodeId;
        public int VisibilityNodeId;
        public Image Image;
        public PassiveConnectionView Stem;
    }

    void Start()
    {
        hud = GetComponent<PaperBattleHUD>();
        if (authoredView == null) authoredView = GetComponentInChildren<PassiveTreeView>(true);
        if (authoredView == null) { Debug.LogError("SkillTreeUI requires an authored PassiveTreeView. Run Black-Cube/UI Authoring/Install Authored UI In Production Prefabs.", this); enabled = false; return; }
        panel = authoredView.panel != null ? authoredView.panel : authoredView.gameObject;
        points = authoredView.points; xp = authoredView.progression; details = authoredView.details; refundAll = authoredView.refundAllButton; refundAllLabel = authoredView.refundAllLabel; scroll = authoredView.scroll;
        authoredView.closeButton.onClick.RemoveListener(Close); authoredView.closeButton.onClick.AddListener(Close);
        refundAll.onClick.RemoveListener(ConfirmRefundAll); refundAll.onClick.AddListener(ConfirmRefundAll);
        if(GetComponent<SubclassMenuUI>()==null)Debug.LogError("SkillTreeUI is missing its authored SubclassMenuUI.",this);
        PassiveTreeViewportInput viewportInput = scroll != null ? scroll.GetComponent<PassiveTreeViewportInput>() : null; if (viewportInput != null) viewportInput.Initialize(this);
        BindAuthoredTree();
        panel.SetActive(false);
        IsOpen = false;
        Bind();
    }

    public void SetAuthoredView(PassiveTreeView view) => authoredView = view;

    void BindAuthoredTree()
    {
        connections.Clear(); junctions.Clear();
        string homeClass = GameManager.Instance?.GetComponent<PlayerIdentityState>()?.BaseClassId ?? PlayerClassIds.Warrior;
        foreach (PassiveBranchBinding branch in authoredView.branches)
        {
            bool native = branch.Data is not PassiveClassBranchSO classData || classData.ClassId == homeClass;
            foreach (PassiveTierViewBinding tier in branch.Tiers)
            {
                BindNode(tier.spine);
                BindGroup(tier.left, native, tier.spine);
                BindGroup(tier.right, native, tier.spine);
            }
        }
        foreach (PassiveConnectionBinding line in authoredView.connections)
        {
            int a = PassiveTreeDefinition.NodeId(line.FromSlotId), b = PassiveTreeDefinition.NodeId(line.ToSlotId), visible = PassiveTreeDefinition.NodeId(line.VisibilitySlotId);
            if (a < 0 || visible < 0 || line.Line == null) continue;
            connections.Add(new PassiveConnectionView { A = a, B = b, VisibilityNodeId = visible, IsJunctionStem = line.IsJunctionStem, Image = line.Line.GetComponent<Image>(), Outline = line.Line.GetComponent<Outline>() });
        }
    }

    void BindGroup(PassiveChoiceGroupBinding group, bool native, PassiveNodeBinding spine)
    {
        if (group == null) return; group.SetNativeLayout(native);
        PassiveNodeBinding[] active = new List<PassiveNodeBinding>(group.RuntimeNodes(native)).ToArray();
        foreach (PassiveNodeBinding node in active) BindNode(node);
        if (spine == null || group.junction == null || active.Length == 0) return;
        int spineId = PassiveTreeDefinition.NodeId(spine.LogicalSlotId), visibleId = PassiveTreeDefinition.NodeId(active[0].LogicalSlotId);
        if (spineId < 0 || visibleId < 0) return;
        Image image = group.junction.GetComponent<Image>(); string key = PassiveTreeDefinition.Node(visibleId).ChoiceGroupId;
        junctions[key] = new PassiveJunctionView { SpineNodeId = spineId, VisibilityNodeId = visibleId, Image = image };
    }

    void BindNode(PassiveNodeBinding binding)
    {
        if (binding == null) return; int id = PassiveTreeDefinition.NodeId(binding.LogicalSlotId); if (id < 0) return;
        Button button = binding.Button; nodes[id] = button; nodeBindings[id] = binding; button.onClick.RemoveAllListeners(); int captured = id; button.onClick.AddListener(() => SelectAndSpend(captured));
        PassiveNodeView nodeView = button.GetComponent<PassiveNodeView>(); if (nodeView == null) nodeView = button.gameObject.AddComponent<PassiveNodeView>(); nodeView.Initialize(this, id);
    }

    public static Vector2 CalculateNodePosition(PassiveNodeDefinition node)
    {
        return node.LayoutPosition;
    }

    public static Vector2 CalculateNodePosition(PassiveNodeDefinition node,string homeClass)
    {
        if(!node.IsClassRoute||!node.IsChoice||node.IsSubclassChoice||node.RouteClassId==homeClass)return node.LayoutPosition;
        int spineId=PassiveTreeDefinition.ClassSpineNode(node.RouteClassId,node.Tier);
        Vector2 spine=PassiveTreeDefinition.Node(spineId).LayoutPosition;
        Vector2 outward=spine.normalized;
        Vector2 tangent=new(-outward.y,outward.x);
        int sign=node.StableId.Contains(".right.")?1:-1;
        char slot=node.StableId[node.StableId.Length-1];
        return slot switch
        {
            'a'=>spine+tangent*(sign*260)+outward*80,
            'b'=>spine+tangent*(sign*330),
            _=>spine+tangent*(sign*260)-outward*80
        };
    }

    public static Vector2 CalculateJunctionPosition(PassiveNodeDefinition choice, Vector2 spinePosition)
    {
        Vector2 outward = spinePosition.normalized;
        Vector2 tangent = new(-outward.y, outward.x);
        int sign = choice.StableId.Contains(".right.") ? 1 : -1;
        return spinePosition + tangent * (sign * 145f);
    }

    void Update()
    {
        Bind();
        if (hud != null && hud.player != null && hud.player.CurrentLife <= 0f) Close();
    }

    void Bind()
    {
        if (progression != null || GameManager.Instance == null) return;
        progression = GameManager.Instance.GetComponent<PlayerProgression>();
        if (progression != null) progression.Changed += Refresh;
        Refresh();
    }

    void Refresh()
    {
        if (progression == null || points == null) return;
        string value = progression.AtCap ? "MAX LEVEL" : $"{progression.Experience:0} / {progression.RequiredXp:0} XP";
        xp.text = $"LEVEL {progression.Level}   /   {value}";
        points.text = $"{progression.AvailablePoints} POINTS AVAILABLE     /     LEVEL {progression.Level}     /     {value}";
        if (!IsOpen) return;

        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
        {
            bool visible=!node.IsSubclassChoice||node.RouteClassId==progression.ActiveClassId;nodes[node.Id].gameObject.SetActive(visible);if(!visible)continue;
            bool allocated = progression.IsAllocated(node.Id);
            bool canSpend = progression.CanSpend(node.Id);
            nodes[node.Id].interactable = !allocated && canSpend;
            ApplyNodeVisual(node.Id, false);
        }

        foreach (PassiveConnectionView connection in connections)
        {
            if(connection.Image==null||nodes[connection.A]==null||nodes[connection.VisibilityNodeId]==null)continue;bool visible=nodes[connection.A].gameObject.activeSelf&&nodes[connection.VisibilityNodeId].gameObject.activeSelf;connection.Image.gameObject.SetActive(visible);if(!visible)continue;
            bool allocated = progression.IsAllocated(connection.A)&&(connection.IsJunctionStem||progression.IsAllocated(connection.B));
            connection.Image.color = allocated ? ConnectionAllocated : ConnectionInactive;
            if(connection.Outline!=null)connection.Outline.enabled = allocated;
        }


        foreach (PassiveJunctionView junction in junctions.Values)
        {
            bool visible = nodes[junction.VisibilityNodeId].gameObject.activeSelf;
            junction.Image.gameObject.SetActive(visible);
            if (visible) junction.Image.color = progression.IsAllocated(junction.SpineNodeId) ? ConnectionAllocated : new Color(1f, .38f, .08f, 1f);
        }

        foreach (var pair in branchLabels)
            pair.Value.text = $"{PassiveTreeDefinition.DisplayName(pair.Key).ToUpperInvariant()}\n<size=12>{TotalText(pair.Key, progression.GetBonus(pair.Key))} TOTAL</size>";
        if (selectedNode >= 0) ShowDetails(selectedNode);
    }

    public void SetHovered(int nodeId, bool hovered)
    {
        if (hovered) ShowDetails(nodeId);
        ApplyNodeVisual(nodeId, hovered);
    }

    void ApplyNodeVisual(int nodeId, bool hovered)
    {
        if (progression == null || nodeId < 0 || nodeId >= nodes.Length || nodes[nodeId] == null) return;
        PassiveNodeDefinition node = PassiveTreeDefinition.Node(nodeId);
        PassiveNodeVisualState state = progression.IsAllocated(nodeId)
            ? PassiveNodeVisualState.Allocated
            : progression.CanSpend(nodeId)
                ? hovered ? PassiveNodeVisualState.Hover : PassiveNodeVisualState.Inactive
                : PassiveNodeVisualState.Unavailable;
        string subclassId=GameManager.Instance?.GetComponent<PlayerIdentityState>()?.SelectedSubclassId;
        PassiveAuthoredNode authored=PassiveTreeDefinition.AuthoredNode(node,subclassId);
        if(nodeBindings[nodeId]!=null)nodeBindings[nodeId].ApplyAuthoringPreview(authored,PassiveTreeDefinition.Database.IconLibrary,false);
        nodes[nodeId].image.sprite = authoredView != null && PassiveTreeDefinition.Database.IconLibrary != null ? PassiveTreeDefinition.Database.IconLibrary.Resolve(authored,state) : nodes[nodeId].image.sprite;
        nodes[nodeId].image.color = node.IsSubclassChoice?new Color(.78f,.42f,1f):Color.white;
        if(node.IsSubclassChoice&&nodes[nodeId].transform.Find("Magnitude")?.GetComponent<TMP_Text>() is TMP_Text label)
        {var identity=GameManager.Instance?.GetComponent<PlayerIdentityState>();label.text=identity?.SubclassChoiceUnlocked==true&&!string.IsNullOrEmpty(identity.SelectedSubclassId)?"SUB":"LOCKED";}
    }

    public void ShowDetails(int nodeId)
    {
        if (progression == null || details == null || nodeId < 0 || nodeId >= PassiveTreeDefinition.NodeCount) return;
        selectedNode = nodeId;
        PassiveNodeDefinition node = PassiveTreeDefinition.Node(nodeId);
        string state;
        if (progression.IsAllocated(nodeId)) state = progression.CanRefund(nodeId)
            ? "ALLOCATED — RIGHT-CLICK TO REFUND"
            : "ALLOCATED — REFUND WOULD INVALIDATE A DEPENDENT ROUTE";
        else if(node.IsSubclassChoice&&(node.RouteClassId!=progression.ActiveClassId||string.IsNullOrEmpty(GameManager.Instance?.GetComponent<PlayerIdentityState>()?.SelectedSubclassId)))state="LOCKED — SELECT A SUBCLASS";
        else if (!progression.CanSpend(nodeId)) state = "LOCKED — ROUTE PREREQUISITE OR CHOICE SIBLING";
        else if (progression.AvailablePoints < PassiveTreeDefinition.PointCost) state = "UNAVAILABLE — NEEDS 1 PASSIVE POINT";
        else state = "AVAILABLE — CLICK TO ALLOCATE (COST: 1 POINT)";
        if(nodeId==PassiveTreeDefinition.RageFinisherNodeId)
        {details.text=$"<b>RAGE FINISHER / AXE SPECIALIZATION</b>\n{PassiveTreeDefinition.KeystoneEffect(PassiveKeystone.RageFinisher)}\n<size=13>{state}</size>";return;}
        if (PassiveTreeDefinition.IsKeystone(nodeId))
        {
            var keystone=PassiveTreeDefinition.KeystoneAt(nodeId);details.text = $"<b>{PassiveTreeDefinition.KeystoneName(keystone).ToUpperInvariant()} / KEYSTONE</b>\n{PassiveTreeDefinition.KeystoneEffect(keystone)}\n<size=13>{state}</size>";
            return;
        }
        var effects=node.IsSubclassChoice?PassiveTreeDefinition.SubclassEffects(GameManager.Instance?.GetComponent<PlayerIdentityState>()?.SelectedSubclassId,node):node.Effects;
        string bonus = effects.Length==0
            ? "NO STAT BONUS"
            : $"+{effects[0].Amount:0.##} {effects[0].Stat}";
        string title=node.IsSubclassChoice?(GameManager.Instance?.GetComponent<PlayerIdentityState>()?.SelectedSubclassId??"SUBCLASS CHOICE"):node.DisplayName;
        details.text = $"<b>{title.ToUpperInvariant()} / TIER {node.Tier}</b>     {bonus}\n<size=13>{state}</size>";
    }

    bool HasAllocatedConnection(int nodeId)
    {
        if (PassiveTreeDefinition.IsRootConnected(nodeId,progression.ActiveClassId)) return true;
        foreach (int adjacent in PassiveTreeDefinition.AdjacentNodeIds(nodeId))
            if (progression.IsAllocated(adjacent)) return true;
        return false;
    }

    static string MagnitudeText(PassiveBranch branch, float value)
    {
        string number = value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        return PassiveTreeDefinition.UsesPercentDisplay(branch) ? $"+{number}%" : $"+{number}";
    }

    static string TotalText(PassiveBranch branch, float value)
    {
        string number = value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        return branch switch
        {
            PassiveBranch.IncreasedProjectileAmount => $"+{number} PROJECTILES",
            PassiveBranch.LifeRegeneration => $"+{number} LIFE/SECOND",
            PassiveBranch.ManaRegeneration => $"+{number} MANA/SECOND",
            PassiveBranch.EmptyTravel => "NO STAT BONUS",
            _ => $"+{number}%"
        };
    }

    void SelectAndSpend(int nodeId)
    {
        selectedNode = nodeId;
        progression?.TrySpend(nodeId);
        Refresh();
        ShowDetails(nodeId);
    }

    public void Refund(int nodeId)
    {
        selectedNode = nodeId;
        if (progression != null) progression.TryRefund(nodeId);
        Refresh();
        ShowDetails(nodeId);
    }

    void ConfirmRefundAll()
    {
        if(progression==null)return;
        if(!refundAllConfirmation){refundAllConfirmation=true;refundAllLabel.text="CONFIRM REFUND ALL";return;}
        progression.RefundAll();refundAllConfirmation=false;refundAllLabel.text="REFUND ALL";selectedNode=-1;Refresh();
    }

    public void Toggle()
    {
        if (panel == null || hud.player == null || hud.player.CurrentLife <= 0f) return;
        if (IsOpen) { Close(); return; }
        hud.inventoryPanel.SetActive(false);
        hud.statsPanel.SetActive(false);
        hud.CloseEnemyInspection();
        GetComponent<PlayerSkillMenuUI>()?.Close();
        IsOpen = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        scroll.horizontalNormalizedPosition = .5f;
        scroll.verticalNormalizedPosition = .5f;
        Refresh();
    }

    public void Close() { IsOpen = false;refundAllConfirmation=false;if(refundAllLabel!=null)refundAllLabel.text="REFUND ALL";if (panel != null) panel.SetActive(false); }

    public void AdjustZoom(PointerEventData eventData)
    {
        if (!IsOpen || scroll == null || scroll.content == null || Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;
        float nextZoom = CalculateNextZoom(treeZoom, eventData.scrollDelta.y);
        if (Mathf.Approximately(nextZoom, treeZoom)) return;

        RectTransform contentRect = scroll.content;
        Camera eventCamera = eventData.enterEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, eventData.position, eventCamera, out Vector2 contentPoint))
            contentPoint = Vector2.zero;

        float previousZoom = treeZoom;
        treeZoom = nextZoom;
        contentRect.localScale = Vector3.one * treeZoom;
        contentRect.anchoredPosition += contentPoint * (previousZoom - treeZoom);
        scroll.StopMovement();
        Canvas.ForceUpdateCanvases();
    }

    public static float CalculateNextZoom(float current, float direction)
    {
        const float legacyFloor = .19f;
        float lowOne = MinimumTreeZoom + (legacyFloor - MinimumTreeZoom) / 3f;
        float lowTwo = MinimumTreeZoom + (legacyFloor - MinimumTreeZoom) * 2f / 3f;
        if (direction < 0f)
        {
            if (current > legacyFloor + .0001f) return Mathf.Max(legacyFloor, current - TreeZoomStep);
            if (current > lowTwo + .0001f) return lowTwo;
            if (current > lowOne + .0001f) return lowOne;
            return MinimumTreeZoom;
        }
        if (current < lowOne - .0001f) return lowOne;
        if (current < lowTwo - .0001f) return lowTwo;
        if (current < legacyFloor - .0001f) return legacyFloor;
        return Mathf.Min(MaximumTreeZoom, current + TreeZoomStep);
    }

    void OnDestroy()
    {
        IsOpen = false;
        if (progression != null) progression.Changed -= Refresh;
    }
}

public enum PassiveNodeVisualState
{
    Inactive,
    Hover,
    Allocated,
    Unavailable
}

public static class PassiveTreeIconAtlas
{
    static readonly Sprite[,,] Icons = new Sprite[PassiveTreeDefinition.BranchCount, 3, 4];
    static readonly Texture2D[] BranchSheets = new Texture2D[PassiveTreeDefinition.BranchCount];
    static readonly Sprite[,] KeystoneIcons = new Sprite[PassiveTreeDefinition.KeystoneNodeCount, 4];
    // Source centers are measured from each sheet's connected alpha silhouette.
    // Cold retains its approved rectangles; other sheets use normalized padding.
    static readonly float[,] TierCenterX =
    {
        { 420.5f, 935.5f, 1675.5f },
        { 450.5f, 982.5f, 1647.5f },
        { 285f, 884.5f, 1686f },
        { 369f, 966f, 1698f },
        { 371.5f, 940f, 1689.5f },
        { 428.5f, 953f, 1645.5f },
        { 428f, 936.5f, 1644f },
        { 371f, 934f, 1678.5f },
        { 427f, 928.5f, 1640f },
        { 368f, 913f, 1654f },
        { 364.5f, 992.5f, 1742f },
        { 367.5f, 982.5f, 1717.5f },
        { 422f, 1012f, 1715.5f },
        { 324.5f, 973.5f, 1731f },
        { 301f, 949f, 1704f },
        { 335f, 931f, 1704f },
        { 346.5f, 964.5f, 1717f },
        { 331.5f, 921.5f, 1704f },
        { 378f, 928f, 1640.5f },
        { 394f, 965.5f, 1691.5f },
        { 626f, 626f, 626f }
    };
    static readonly float[,] TierCenterY =
    {
        { 368f, 364f, 362f },
        { 356f, 351.5f, 362f },
        { 397.5f, 379.5f, 373f },
        { 376.5f, 368f, 362f },
        { 359.5f, 356.5f, 362f },
        { 365f, 364.5f, 362f },
        { 353f, 352.5f, 362f },
        { 361f, 350.5f, 362f },
        { 356f, 353f, 357f },
        { 350f, 345f, 362f },
        { 359.5f, 351.5f, 351.5f },
        { 367.5f, 365f, 358.5f },
        { 382f, 370.5f, 344.5f },
        { 418.5f, 392.5f, 347.5f },
        { 395f, 373f, 357f },
        { 366.5f, 354.5f, 352f },
        { 354.5f, 342f, 348f },
        { 367f, 352f, 346.5f },
        { 369.5f, 356.5f, 347f },
        { 380.5f, 368.5f, 359f },
        { 617f, 617f, 617f }
    };
    static readonly int[,] TierCropSizes =
    {
        { 368, 574, 770 },
        { 394, 578, 734 },
        { 400, 590, 750 },
        { 410, 604, 768 },
        { 372, 576, 748 },
        { 382, 568, 746 },
        { 398, 582, 742 },
        { 348, 570, 736 },
        { 376, 570, 748 },
        { 360, 540, 720 },
        { 376, 540, 716 },
        { 400, 546, 748 },
        { 300, 496, 700 },
        { 360, 568, 712 },
        { 338, 550, 710 },
        { 374, 568, 754 },
        { 346, 534, 730 },
        { 342, 548, 708 },
        { 374, 554, 724 },
        { 388, 560, 728 },
        { 568, 568, 568 }
    };
    const int StartCropSize = 1240;
    static readonly Vector2 StartCenterTop = new Vector2(627f, 620f);
    static Texture2D playerSheet;
    static Sprite start;

    public static Sprite Get(PassiveBranch branch, PassiveNodeSize size, PassiveNodeVisualState state)
    {
        int branchIndex = (int)branch;
        int sizeIndex = (int)size;
        int stateIndex = (int)state;
        if (Icons[branchIndex, sizeIndex, stateIndex] != null) return Icons[branchIndex, sizeIndex, stateIndex];
        Texture2D branchSheet = LoadBranchSheet(branch);
        if (branchSheet == null) return null;
        Sprite result = BuildFromIndividualSheet(branch, branchSheet, sizeIndex, state);
        result.name = $"{branch} {size} {state} Passive Node";
        Icons[branchIndex, sizeIndex, stateIndex] = result;
        return result;
    }

    public static Sprite GetStart()
    {
        if (start != null) return start;
        if (playerSheet == null) playerSheet = Resources.Load<Texture2D>("UI/PassiveTree/PlayerPassiveIcon");
        if (playerSheet == null) return null;
        Color[] pixels = CropPadded(playerSheet, StartCenterTop.x, StartCenterTop.y, StartCropSize);
        start = Create(pixels, StartCropSize, "Passive Tree Start");
        return start;
    }

    public static Sprite GetKeystone(PassiveKeystone keystone, PassiveNodeVisualState state)
    {
        int index = (int)keystone - 1;
        int stateIndex = (int)state;
        if (index < 0 || index >= PassiveTreeDefinition.KeystoneNodeCount) return null;
        if (KeystoneIcons[index, stateIndex] != null) return KeystoneIcons[index, stateIndex];
        Texture2D sheet = Resources.Load<Texture2D>($"UI/PassiveTree/Keystones/{KeystoneFileName(keystone)}");
        if (sheet == null) return null;
        int size = Mathf.Max(sheet.width, sheet.height);
        Color[] pixels = CropPadded(sheet, sheet.width * .5f, sheet.height * .5f, size);
        TintState(pixels, state);
        var sprite = Create(pixels, size, PassiveTreeDefinition.KeystoneName(keystone));
        KeystoneIcons[index, stateIndex] = sprite;
        return sprite;
    }

    static string KeystoneFileName(PassiveKeystone keystone) => keystone switch
    {
        PassiveKeystone.IronBastion => "IronBastion",
        PassiveKeystone.LivingFortress => "LivingFortress",
        PassiveKeystone.ManaShield => "ManaShield",
        PassiveKeystone.ArcaneOverload => "ArcaneOverload",
        PassiveKeystone.LivingCurrent => "LivingCurrent",
        PassiveKeystone.InfernalConversion => "InfernalConversion",
        PassiveKeystone.VenomousTransmutation => "VenomousTransmutation",
        PassiveKeystone.BallisticBarrage => "BallisticBarrage",
        PassiveKeystone.BruteForce => "BruteForce",
        PassiveKeystone.AbsoluteZero => "AbsoluteZero",
        PassiveKeystone.OpenWounds => "OpenWounds",
        PassiveKeystone.Wildfire => "Wildfire",
        PassiveKeystone.DeepFreeze => "DeepFreeze",
        PassiveKeystone.Overcharged => "Overcharged",
        PassiveKeystone.ToxicSaturation => "ToxicSaturation",
        PassiveKeystone.UndyingFlesh => "UndyingFlesh",
        PassiveKeystone.EndlessCurrent => "EndlessCurrent",
        PassiveKeystone.Frenzy => "Frenzy",
        PassiveKeystone.BulletHell => "BulletHell",
        PassiveKeystone.EchoingStrikes => "EchoingStrikes",
        PassiveKeystone.RageFinisher => "BruteForce",
        _ => string.Empty
    };

    static Texture2D LoadBranchSheet(PassiveBranch branch)
    {
        int index = (int)branch;
        if (BranchSheets[index] != null) return BranchSheets[index];
        BranchSheets[index] = Resources.Load<Texture2D>($"UI/PassiveTree/{SheetName(branch)}");
        return BranchSheets[index];
    }

    static string SheetName(PassiveBranch branch) => branch switch
    {
        PassiveBranch.Defense => "DefensePassiveIcons",
        PassiveBranch.Life => "LifePassiveIcons",
        PassiveBranch.Mana => "ManaPassiveIcons",
        PassiveBranch.Magic => "MagicPassiveIcons",
        PassiveBranch.Lightning => "LightningPassiveIcons",
        PassiveBranch.Fire => "FirePassiveIcons",
        PassiveBranch.Poison => "PoisonPassiveIcons",
        PassiveBranch.Projectile => "ProjectilePassiveIcons",
        PassiveBranch.Physical => "PhysicalPassiveIcon",
        PassiveBranch.Cold => "ColdPassiveIcons",
        PassiveBranch.IncreasedProjectileAmount => "IncreasedProjectileAmountPassiveIcons",
        PassiveBranch.AttackSpeed => "AttackSpeedPassiveIcons",
        PassiveBranch.BleedChance => "BleedChancePassiveIcons",
        PassiveBranch.PoisonChance => "PoisonChancePassiveIcons",
        PassiveBranch.ChillChance => "ChillChancePassiveIcons",
        PassiveBranch.IgniteChance => "IgniteChancePassiveIcons",
        PassiveBranch.ShockChance => "ShockChancePassiveIcons",
        PassiveBranch.ChanceToHitTwice => "DoubleStrikePassiveIcon",
        PassiveBranch.LifeRegeneration => "LifeRegenPassiveIcon",
        PassiveBranch.ManaRegeneration => "ManaRegenPassiveIcon",
        PassiveBranch.EmptyTravel => "EmptyPassiveNode",
        _ => "ColdPassiveIcons"
    };

    static Sprite BuildFromIndividualSheet(PassiveBranch branch, Texture2D sheet, int sizeIndex, PassiveNodeVisualState state)
    {
        int branchIndex = AtlasIndex(branch);
        int cropSize = TierCropSizes[branchIndex, sizeIndex];
        Color[] pixels = CropPadded(sheet, TierCenterX[branchIndex, sizeIndex], TierCenterY[branchIndex, sizeIndex], cropSize);
        TintState(pixels, state);
        return Create(pixels, cropSize, null);
    }

    static int AtlasIndex(PassiveBranch branch)=>(int)branch<21?(int)branch:branch switch
    {
        PassiveBranch.CriticalChance or PassiveBranch.CriticalMultiplier=>7,
        PassiveBranch.LifeOnHit or PassiveBranch.LifeOnKill=>1,
        PassiveBranch.ManaOnHit or PassiveBranch.ManaOnKill=>2,
        PassiveBranch.Strength=>8,PassiveBranch.Dexterity=>11,PassiveBranch.Intelligence=>3,
        PassiveBranch.CooldownReduction=>3,PassiveBranch.ProjectileSpeed or PassiveBranch.PrecisionChance or PassiveBranch.PrecisionDamage=>7,
        PassiveBranch.RageGeneration or PassiveBranch.RageEffect or PassiveBranch.RageRetention=>8,_=>20
    };

    static Color[] CropPadded(Texture2D source, float centerX, float topCenterY, int size)
    {
        var destination = new Color[size * size];
        int sourceX = Mathf.RoundToInt(centerX - size * .5f);
        int sourceTop = Mathf.RoundToInt(topCenterY - size * .5f);
        int copyX = Mathf.Max(0, sourceX);
        int copyTop = Mathf.Max(0, sourceTop);
        int copyRight = Mathf.Min(source.width, sourceX + size);
        int copyBottom = Mathf.Min(source.height, sourceTop + size);
        int copyWidth = copyRight - copyX;
        int copyHeight = copyBottom - copyTop;
        if (copyWidth <= 0 || copyHeight <= 0) return destination;

        Color[] sourcePixels = source.GetPixels(copyX, source.height - copyBottom, copyWidth, copyHeight);
        int destinationX = copyX - sourceX;
        int destinationY = size - (copyBottom - sourceTop);
        for (int row = 0; row < copyHeight; row++)
            System.Array.Copy(sourcePixels, row * copyWidth, destination, (destinationY + row) * size + destinationX, copyWidth);
        return destination;
    }

    static void TintState(Color[] pixels, PassiveNodeVisualState state)
    {
        if (state == PassiveNodeVisualState.Inactive) return;
        Color tint = state switch
        {
            PassiveNodeVisualState.Hover => new Color(1f, .72f, .2f),
            PassiveNodeVisualState.Allocated => new Color(.08f, .52f, 1f),
            _ => new Color(1f, .08f, .06f)
        };
        float amount = state == PassiveNodeVisualState.Hover ? .12f : .34f;
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i].a > 0f) pixels[i] = Color.Lerp(pixels[i], new Color(tint.r, tint.g, tint.b, pixels[i].a), amount);
    }

    static Sprite Create(Color[] pixels, int size, string spriteName)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = spriteName ?? "Runtime Passive Node",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect);
    }
}
