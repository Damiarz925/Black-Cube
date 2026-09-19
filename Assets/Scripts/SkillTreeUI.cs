// Developer map: Builds the complete data-driven radial passive menu over PaperBattleHUD.
// Dragging and wheel zoom navigate the 7,000px spoke/bridge/ring/keystone tree; pause behavior follows GameplayOptions.
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

    PaperBattleHUD hud;
    GameObject panel;
    TMP_Text points;
    TMP_Text xp;
    TMP_Text details;
    Button refundAll;
    TMP_Text refundAllLabel;
    Button transformModeButton;TMP_Text transformModeLabel;bool transformMode;
    bool refundAllConfirmation;
    ScrollRect scroll;
    readonly Button[] nodes = new Button[PassiveTreeDefinition.NodeCount];
    readonly List<PassiveConnectionView> connections = new();
    readonly Dictionary<PassiveBranch, TMP_Text> branchLabels = new();
    PlayerProgression progression;
    int selectedNode = -1;
    float treeZoom = 1f;

    const float MinimumTreeZoom = .11f;
    const float MaximumTreeZoom = 1.35f;
    const float TreeZoomStep = .08f;

    public GameObject Panel => panel;
    public Button NodeButton(int index) => index >= 0 && index < nodes.Length ? nodes[index] : null;

    sealed class PassiveConnectionView
    {
        public int A;
        public int B;
        public Image Image;
        public Outline Outline;
    }

    void Start()
    {
        hud = GetComponent<PaperBattleHUD>();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) { Debug.LogError("SkillTreeUI requires a parent Canvas.", this); return; }

        xp = Text(parentCanvas.transform, "Progression", Vector2.zero, Vector2.zero, 12);
        xp.color = new Color(.95f, .8f, .45f);
        hud?.PositionBelowArtwork(xp.rectTransform,18,28,430,22);

        panel = Box(parentCanvas.transform, "Passive Skill Tree", Vector2.zero, Vector2.one, new Color(.025f, .03f, .04f, 1f));
        var canvas = panel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        panel.AddComponent<GraphicRaycaster>();

        var title = Text(panel.transform, "Title", new Vector2(.035f, .9f), new Vector2(.65f, .98f), 30);
        title.text = "WANDERER  /  PASSIVE SKILL TREE";
        points = Text(panel.transform, "Available points", new Vector2(.035f, .83f), new Vector2(.78f, .91f), 17);
        var close = Button(panel.transform, "Close", new Vector2(.82f, .905f), new Vector2(.97f, .97f), out var closeText);
        closeText.text = "RETURN TO BATTLE";
        close.onClick.AddListener(Close);
        refundAll=Button(panel.transform,"Refund All",new Vector2(.66f,.905f),new Vector2(.81f,.97f),out refundAllLabel);
        refundAllLabel.text="REFUND ALL";refundAll.onClick.AddListener(ConfirmRefundAll);
        transformModeButton=Button(panel.transform,"Subclass Transform",new Vector2(.49f,.905f),new Vector2(.65f,.97f),out transformModeLabel);transformModeLabel.text="SUBCLASS SIGIL";transformModeButton.onClick.AddListener(()=>{transformMode=!transformMode;Refresh();});
        if(GetComponent<SubclassMenuUI>()==null)gameObject.AddComponent<SubclassMenuUI>();

        var viewport = Box(panel.transform, "Tree Viewport", new Vector2(.025f, .145f), new Vector2(.975f, .825f), new Color(.045f, .052f, .062f, 1f));
        viewport.AddComponent<RectMask2D>();
        scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = true;
        scroll.inertia = true;
        scroll.decelerationRate = .08f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        // Wheel input is reserved for zoom; pointer dragging remains ScrollRect pan.
        scroll.scrollSensitivity = 0f;
        viewport.AddComponent<PassiveTreeViewportInput>().Initialize(this);

        var content = new GameObject("Radial Tree Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = Vector2.one * .5f;
        contentRect.sizeDelta = new Vector2(7000f, 7000f);
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = contentRect;

        BuildTree(content.transform);
        details = Text(panel.transform, "Node Details", new Vector2(.035f, .025f), new Vector2(.965f, .13f), 15);
        details.alignment = TextAlignmentOptions.MidlineLeft;
        details.text = "Hover or select a node for details. Click to allocate, right-click to refund. Drag or use the mouse wheel to explore.";

        panel.SetActive(false);
        IsOpen = false;
        Bind();
    }

    void BuildTree(Transform content)
    {
        var positions = new Vector2[PassiveTreeDefinition.NodeCount];
        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
            positions[node.Id] = CalculateNodePosition(node);

        foreach (PassiveTreeEdge edge in PassiveTreeDefinition.Edges)
        {
            Vector2 from = positions[edge.A];
            Image image = Line(content, from, positions[edge.B], out Outline outline);
            connections.Add(new PassiveConnectionView { A = edge.A, B = edge.B, Image = image, Outline = outline });
        }

        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
        {
            Vector2 position = positions[node.Id];
            nodes[node.Id] = CreateNodeButton(content, node, position);
            int nodeId = node.Id;
            nodes[node.Id].onClick.AddListener(() => SelectAndSpend(nodeId));
            nodes[node.Id].gameObject.AddComponent<PassiveNodeView>().Initialize(this, nodeId);

            if(node.Kind==PassiveNodeKind.ClassStart){var label=Text(content,node.DisplayName+" Label",Vector2.one*.5f,Vector2.one*.5f,20);label.rectTransform.sizeDelta=new Vector2(300,60);label.rectTransform.anchoredPosition=position+position.normalized*110;label.alignment=TextAlignmentOptions.Center;label.text=node.DisplayName.ToUpperInvariant();label.color=BranchColor(node.Branch);}
        }
    }

    public static Vector2 CalculateNodePosition(PassiveNodeDefinition node)
    {
        return node.LayoutPosition;
    }

    Button CreateNodeButton(Transform parent, PassiveNodeDefinition node, Vector2 position)
    {
        float size = PassiveTreeDefinition.IsKeystone(node.Id) ? 156f
            : node.Size switch { PassiveNodeSize.Small => 58f, PassiveNodeSize.Medium => 78f, _ => 108f };
        string objectName = node.DisplayName;
        var go = Box(parent, objectName, Vector2.one * .5f, Vector2.one * .5f, Color.white);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = Vector2.one * size;
        rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();
        image.sprite = node.Kind==PassiveNodeKind.ClassStart?PassiveTreeIconAtlas.GetStart():PassiveTreeDefinition.IsKeystone(node.Id)
            ? PassiveTreeIconAtlas.GetKeystone(node.Keystone, PassiveNodeVisualState.Inactive)
            : PassiveTreeIconAtlas.Get(node.Branch, node.Size, PassiveNodeVisualState.Inactive);
        image.preserveAspect = true;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        var amount = Text(go.transform, "Magnitude", new Vector2(.05f, -.3f), new Vector2(.95f, .12f), node.Size == PassiveNodeSize.Small ? 10 : 12);
        amount.alignment = TextAlignmentOptions.Center;
        amount.text = node.Effects.Length==0 || PassiveTreeDefinition.IsKeystone(node.Id)
            ? string.Empty : MagnitudeText(node.Branch, node.Magnitude);
        amount.color = new Color(.98f, .88f, .65f);
        return button;
    }

    void Update()
    {
        Bind();
        hud?.PositionBelowArtwork(xp != null ? xp.rectTransform : null,18,28,430,22);
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
        var identity=GameManager.Instance?.GetComponent<PlayerIdentityState>();if(transformModeButton!=null){bool enabled=identity!=null&&identity.HasSubclassSigil&&!string.IsNullOrEmpty(identity.SelectedSubclassId);transformModeButton.interactable=enabled;if(!enabled)transformMode=false;transformModeLabel.text=transformMode?$"TRANSFORMING  {progression.TransformedCount}/{SubclassTransformationProfile.MaximumTransformedNodes}":"SUBCLASS SIGIL";}
        if (!IsOpen) return;

        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
        {
            bool allocated = progression.IsAllocated(node.Id);
            bool canSpend = progression.CanSpend(node.Id);
            nodes[node.Id].interactable = !allocated && canSpend;
            ApplyNodeVisual(node.Id, false);
        }

        foreach (PassiveConnectionView connection in connections)
        {
            bool allocated = (PassiveTreeDefinition.IsClassStart(connection.A)?connection.A==progression.ActiveStartNodeId:progression.IsAllocated(connection.A))
                && (PassiveTreeDefinition.IsClassStart(connection.B)?connection.B==progression.ActiveStartNodeId:progression.IsAllocated(connection.B));
            connection.Image.color = allocated ? ConnectionAllocated : ConnectionInactive;
            connection.Outline.enabled = allocated;
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
        nodes[nodeId].image.sprite = PassiveTreeDefinition.IsKeystone(node.Id)
            ? PassiveTreeIconAtlas.GetKeystone(node.Keystone, state)
            : PassiveTreeIconAtlas.Get(node.Branch, node.Size, state);
        nodes[nodeId].image.color = progression.IsTransformed(nodeId)?new Color(.75f,.35f,1f):transformMode&&progression.CanTransform(nodeId)?new Color(.55f,1f,.75f):Color.white;
    }

    public void ShowDetails(int nodeId)
    {
        if (progression == null || details == null || nodeId < 0 || nodeId >= PassiveTreeDefinition.NodeCount) return;
        selectedNode = nodeId;
        PassiveNodeDefinition node = PassiveTreeDefinition.Node(nodeId);
        string state;
        if(node.Kind==PassiveNodeKind.ClassStart)state=node.Id==progression.ActiveStartNodeId?"ACTIVE CLASS ORIGIN — COSTS NO POINTS":"INACTIVE CLASS LANDMARK";
        else if (progression.IsAllocated(nodeId)) state = progression.CanRefund(nodeId)
            ? "ALLOCATED — RIGHT-CLICK TO REFUND"
            : "ALLOCATED — REFUND WOULD DISCONNECT ANOTHER NODE";
        else if (!HasAllocatedConnection(nodeId)) state = "LOCKED — REQUIRES AN ADJACENT NODE";
        else if (progression.AvailablePoints < PassiveTreeDefinition.PointCost) state = "UNAVAILABLE — NEEDS 1 PASSIVE POINT";
        else state = "AVAILABLE — CLICK TO ALLOCATE (COST: 1 POINT)";
        if (PassiveTreeDefinition.IsKeystone(nodeId))
        {
            details.text = $"<b>{PassiveTreeDefinition.KeystoneName(node.Keystone).ToUpperInvariant()} / KEYSTONE</b>\n{PassiveTreeDefinition.KeystoneEffect(node.Keystone)}\n<size=13>{state}</size>";
            return;
        }
        string bonus = node.Effects.Length==0
            ? "NO STAT BONUS"
            : $"{MagnitudeText(node.Branch, node.Magnitude)} {PassiveTreeDefinition.GameplayMeaning(node.Branch)}";
        details.text = $"<b>{PassiveTreeDefinition.DisplayName(node.Branch).ToUpperInvariant()} / {node.Size.ToString().ToUpperInvariant()} NODE</b>     {bonus}\n<size=13>{state}     /     BRANCH TOTAL: {TotalText(node.Branch, progression.GetBonus(node.Branch))}</size>";
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
        if (progression != null){if(transformMode){if(progression.IsTransformed(nodeId))progression.TryRemoveTransformation(nodeId);else progression.TryTransform(nodeId);}else progression.TrySpend(nodeId);}
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

    public void Close() { IsOpen = false;transformMode=false;refundAllConfirmation=false;if(refundAllLabel!=null)refundAllLabel.text="REFUND ALL";if (panel != null) panel.SetActive(false); }

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
        if (panel != null) Destroy(panel);
        if (xp != null) Destroy(xp.gameObject);
    }

    static Color BranchColor(PassiveBranch branch) => branch switch
    {
        PassiveBranch.Defense => new Color(.36f, .85f, .68f),
        PassiveBranch.Life => new Color(1f, .46f, .58f),
        PassiveBranch.Mana => new Color(.35f, .55f, 1f),
        PassiveBranch.Magic => new Color(.72f, .42f, 1f),
        PassiveBranch.Lightning => new Color(.25f, .67f, 1f),
        PassiveBranch.Fire => new Color(1f, .3f, .12f),
        PassiveBranch.Poison => new Color(.25f, .9f, .54f),
        PassiveBranch.Projectile => new Color(1f, .68f, .2f),
        PassiveBranch.Physical => new Color(.82f, .64f, .46f),
        PassiveBranch.Cold => new Color(.3f, .78f, 1f),
        PassiveBranch.IncreasedProjectileAmount => new Color(.96f, .78f, .28f),
        PassiveBranch.AttackSpeed => new Color(.5f, .82f, 1f),
        PassiveBranch.BleedChance => new Color(.92f, .2f, .28f),
        PassiveBranch.PoisonChance => new Color(.34f, .9f, .48f),
        PassiveBranch.ChillChance => new Color(.28f, .8f, 1f),
        PassiveBranch.IgniteChance => new Color(1f, .38f, .12f),
        PassiveBranch.ShockChance => new Color(.46f, .64f, 1f),
        PassiveBranch.ChanceToHitTwice => new Color(1f, .58f, .16f),
        PassiveBranch.LifeRegeneration => new Color(.32f, .92f, .5f),
        PassiveBranch.ManaRegeneration => new Color(.3f, .58f, 1f),
        PassiveBranch.EmptyTravel => new Color(.68f, .66f, .62f),
        _ => new Color(.9f, .86f, .78f)
    };

    static GameObject Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static TMP_Text Text(Transform parent, string name, Vector2 min, Vector2 max, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size; text.color = new Color(.92f, .93f, .95f); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    static Button Button(Transform parent, string name, Vector2 min, Vector2 max, out TMP_Text label)
    {
        var go = Box(parent, name, min, max, new Color(.12f, .15f, .18f));
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        label = Text(go.transform, "Label", Vector2.zero, Vector2.one, 16);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    static Image Line(Transform parent, Vector2 from, Vector2 to, out Outline outline)
    {
        var go = Box(parent, "Connection", Vector2.one * .5f, Vector2.one * .5f, ConnectionInactive);
        var rect = (RectTransform)go.transform;
        rect.anchoredPosition = (from + to) * .5f;
        rect.sizeDelta = new Vector2(Vector2.Distance(from, to), 4f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(.08f, .42f, 1f, .65f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.enabled = false;
        go.transform.SetAsFirstSibling();
        return image;
    }
}

public sealed class PassiveNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IPointerClickHandler
{
    SkillTreeUI owner;
    int nodeId;
    public void Initialize(SkillTreeUI tree, int id) { owner = tree; nodeId = id; }
    public void OnPointerEnter(PointerEventData eventData) => owner?.SetHovered(nodeId, true);
    public void OnPointerExit(PointerEventData eventData) => owner?.SetHovered(nodeId, false);
    public void OnSelect(BaseEventData eventData) => owner?.ShowDetails(nodeId);
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) owner?.Refund(nodeId);
    }
}

public sealed class PassiveTreeViewportInput : MonoBehaviour, IScrollHandler
{
    SkillTreeUI owner;
    public void Initialize(SkillTreeUI tree) => owner = tree;
    public void OnScroll(PointerEventData eventData) => owner?.AdjustZoom(eventData);
}

public enum PassiveNodeVisualState
{
    Inactive,
    Hover,
    Allocated,
    Unavailable
}

static class PassiveTreeIconAtlas
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
        PassiveBranch.CastSpeed=>3,PassiveBranch.ProjectileSpeed or PassiveBranch.PrecisionChance or PassiveBranch.PrecisionDamage=>7,
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
