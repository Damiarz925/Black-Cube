using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// First-pass, presentation-only passive tree. The structure is generated from the
/// category and phase tables below; allocation/gameplay behavior is intentionally absent.
/// </summary>
public sealed class PassiveSkillTreeView : MonoBehaviour
{
    public const int BranchCount = 10;
    public const int NodesPerBranch = 12;
    public const float BranchAngleStep = 360f / BranchCount;

    public enum NodeSize
    {
        Small,
        Medium,
        Large
    }

    public readonly struct PhaseDefinition
    {
        public readonly NodeSize Size;
        public readonly float Radius;

        public PhaseDefinition(NodeSize size, float radius)
        {
            Size = size;
            Radius = radius;
        }
    }

    public readonly struct CategoryDefinition
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly int AtlasColumn;
        public readonly int AtlasRow;

        public CategoryDefinition(string name, Color color, int atlasColumn, int atlasRow)
        {
            Name = name;
            Color = color;
            AtlasColumn = atlasColumn;
            AtlasRow = atlasRow;
        }
    }

    public static readonly PhaseDefinition[] Phases =
    {
        new(NodeSize.Small, .10f), new(NodeSize.Small, .16f),
        new(NodeSize.Small, .22f), new(NodeSize.Small, .28f),
        new(NodeSize.Small, .34f), new(NodeSize.Large, .46f),
        new(NodeSize.Medium, .57f), new(NodeSize.Medium, .65f),
        new(NodeSize.Medium, .73f), new(NodeSize.Medium, .81f),
        new(NodeSize.Medium, .89f), new(NodeSize.Large, 1f)
    };

    public static readonly CategoryDefinition[] Categories =
    {
        new("Damage",      new Color(.96f, .32f, .35f), 0, 0),
        new("Life",        new Color(1.00f, .48f, .58f), 1, 0),
        new("Defense",     new Color(.39f, .86f, .72f), 2, 0),
        new("Speed",       new Color(.36f, .67f, 1.00f), 3, 0),
        new("Magic",       new Color(.69f, .39f, 1.00f), 4, 0),
        new("Projectile",  new Color(1.00f, .68f, .21f), 0, 1),
        new("Cold/Nature", new Color(.26f, .85f, .52f), 1, 1),
        new("Fire",        new Color(1.00f, .30f, .12f), 2, 1),
        new("Lightning",   new Color(.30f, .67f, 1.00f), 3, 1),
        new("Minion",      new Color(.88f, .82f, .72f), 4, 1)
    };

    private const string AtlasResourcePath = "PassiveSkillTree/PassiveTreeIcons";
    private static readonly Color PanelColor = new(.025f, .022f, .019f, .985f);
    private static readonly Color Gold = new(.83f, .62f, .27f);

    private RectTransform treeArea;
    private RectTransform[] nodeRects;
    private TMP_Text[] categoryLabels;
    private PassiveSkillTreeConnections connections;
    private GameObject overlay;
    private Texture2D atlas;
    private Vector2 lastTreeSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInGameplayScene()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" || FindAnyObjectByType<PassiveSkillTreeView>() != null)
            return;

        var hud = FindAnyObjectByType<PaperBattleHUD>();
        var canvas = hud != null ? hud.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("PassiveSkillTreeView: gameplay canvas was not found.");
            return;
        }

        var host = new GameObject("Passive Skill Tree UI", typeof(RectTransform));
        host.transform.SetParent(canvas.transform, false);
        Stretch(host.GetComponent<RectTransform>());
        host.transform.SetAsLastSibling();
        host.AddComponent<PassiveSkillTreeView>();
    }

    private void Awake()
    {
        atlas = Resources.Load<Texture2D>(AtlasResourcePath);
        BuildLauncher();
        BuildOverlay();
        overlay.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!overlay.activeSelf)
            return;

        var size = treeArea.rect.size;
        if ((size - lastTreeSize).sqrMagnitude > .25f)
            LayoutTree();
    }

    private void BuildLauncher()
    {
        var button = CreateButton(transform, "PASSIVE TREE", new Vector2(168, 42));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-24, -24);
        button.onClick.AddListener(Open);
    }

    private void BuildOverlay()
    {
        overlay = new GameObject("Passive Tree Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = PanelColor;

        var title = CreateText(overlay.transform, "PASSIVE SKILL TREE", 27, FontStyles.Bold, Gold);
        SetAnchors(title.rectTransform, new Vector2(.25f, .925f), new Vector2(.75f, .985f));

        var subtitle = CreateText(overlay.transform, "FIRST PASS  •  10 CATEGORY PATHS  •  36° SPACING", 12, FontStyles.Normal, new Color(.72f, .69f, .63f));
        SetAnchors(subtitle.rectTransform, new Vector2(.25f, .89f), new Vector2(.75f, .93f));

        var close = CreateButton(overlay.transform, "CLOSE", new Vector2(108, 38));
        var closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-24, -22);
        close.onClick.AddListener(Close);

        var tree = new GameObject("Generated Tree", typeof(RectTransform));
        tree.transform.SetParent(overlay.transform, false);
        treeArea = tree.GetComponent<RectTransform>();
        treeArea.anchorMin = new Vector2(.02f, .035f);
        treeArea.anchorMax = new Vector2(.98f, .89f);
        treeArea.offsetMin = treeArea.offsetMax = Vector2.zero;

        var lineObject = new GameObject("Connections", typeof(RectTransform), typeof(PassiveSkillTreeConnections));
        lineObject.transform.SetParent(treeArea, false);
        Stretch(lineObject.GetComponent<RectTransform>());
        connections = lineObject.GetComponent<PassiveSkillTreeConnections>();
        connections.raycastTarget = false;

        nodeRects = new RectTransform[1 + BranchCount * NodesPerBranch];
        categoryLabels = new TMP_Text[BranchCount];
        nodeRects[0] = CreateNode(treeArea, "Starting Point", NodeSize.Large, default, true);

        for (var branch = 0; branch < BranchCount; branch++)
        {
            var category = Categories[branch];
            for (var node = 0; node < NodesPerBranch; node++)
            {
                var index = 1 + branch * NodesPerBranch + node;
                nodeRects[index] = CreateNode(treeArea, $"{category.Name} {node + 1:00}", Phases[node].Size, category, false);
            }

            var label = CreateText(treeArea, category.Name.ToUpperInvariant(), 12, FontStyles.Bold, category.Color);
            label.rectTransform.sizeDelta = new Vector2(120, 22);
            categoryLabels[branch] = label;
        }

        Canvas.ForceUpdateCanvases();
        LayoutTree();
    }

    private RectTransform CreateNode(Transform parent, string nodeName, NodeSize size, CategoryDefinition category, bool isStart)
    {
        var node = new GameObject(nodeName, typeof(RectTransform), typeof(PassiveSkillTreeNodeGraphic));
        node.transform.SetParent(parent, false);
        var rect = node.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = Vector2.one * NodePixelSize(size);

        var graphic = node.GetComponent<PassiveSkillTreeNodeGraphic>();
        graphic.Atlas = atlas;
        graphic.UvRect = isStart ? StartingPointUv() : CategoryUv(category, size);
        graphic.FallbackColor = isStart ? Gold : category.Color;
        graphic.raycastTarget = false;
        return rect;
    }

    private void LayoutTree()
    {
        if (treeArea == null || treeArea.rect.width <= 0 || treeArea.rect.height <= 0)
            return;

        lastTreeSize = treeArea.rect.size;
        var maximumRadius = Mathf.Max(150f, (Mathf.Min(lastTreeSize.x, lastTreeSize.y) - 118f) * .5f);
        nodeRects[0].anchoredPosition = Vector2.zero;

        var segments = new List<PassiveSkillTreeConnections.Segment>(BranchCount * NodesPerBranch);
        for (var branch = 0; branch < BranchCount; branch++)
        {
            // Damage starts at twelve o'clock and categories proceed clockwise.
            var radians = (90f - branch * BranchAngleStep) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var previous = Vector2.zero;

            for (var node = 0; node < NodesPerBranch; node++)
            {
                var position = direction * (maximumRadius * Phases[node].Radius);
                var index = 1 + branch * NodesPerBranch + node;
                nodeRects[index].anchoredPosition = position;
                segments.Add(new PassiveSkillTreeConnections.Segment(previous, position, Categories[branch].Color));
                previous = position;
            }

            categoryLabels[branch].rectTransform.anchoredPosition = previous + direction * 43f;
        }

        connections.SetSegments(segments);
    }

    private void Open()
    {
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutTree();
    }

    private void Close()
    {
        overlay.SetActive(false);
    }

    private static float NodePixelSize(NodeSize size)
    {
        return size switch
        {
            NodeSize.Small => 20f,
            NodeSize.Medium => 26f,
            _ => 42f
        };
    }

    private static Rect CategoryUv(CategoryDefinition category, NodeSize size)
    {
        // The supplied 1536x1024 sheet is a presentation sheet, so the usable inactive
        // category art is sampled here without destructive source-image slicing.
        var centerX = size switch
        {
            NodeSize.Small => 75f + 96f * category.AtlasColumn,
            NodeSize.Medium => 579f + 96f * category.AtlasColumn,
            _ => 1091f + 96f * category.AtlasColumn
        };
        var centerY = size switch
        {
            NodeSize.Small => category.AtlasRow == 0 ? 383f : 493f,
            NodeSize.Medium => category.AtlasRow == 0 ? 392f : 505f,
            _ => category.AtlasRow == 0 ? 393f : 511f
        };
        var crop = size switch
        {
            NodeSize.Small => 70f,
            NodeSize.Medium => 88f,
            // Category rows are tightly packed on the presentation sheet. Keeping
            // the large crop inside one 96px column avoids neighboring ornaments/text.
            _ => 94f
        };
        return PixelRectToUv(centerX, centerY, crop);
    }

    private static Rect StartingPointUv()
    {
        // Allocated large generic node from the state preview at the top of the sheet.
        return PixelRectToUv(1319f, 219f, 118f);
    }

    private static Rect PixelRectToUv(float centerX, float centerYFromTop, float size)
    {
        const float sourceWidth = 1536f;
        const float sourceHeight = 1024f;
        return new Rect(
            (centerX - size * .5f) / sourceWidth,
            (sourceHeight - centerYFromTop - size * .5f) / sourceHeight,
            size / sourceWidth,
            size / sourceHeight);
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(.12f, .095f, .06f, .98f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .82f, .48f);
        colors.pressedColor = new Color(.72f, .57f, .34f);
        button.colors = colors;

        var text = CreateText(go.transform, label, 13, FontStyles.Bold, new Color(.94f, .83f, .60f));
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string value, float size, FontStyles style, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one);
    }
}

/// <summary>Samples one circular node from the source atlas without creating 30 duplicate textures.</summary>
public sealed class PassiveSkillTreeNodeGraphic : MaskableGraphic
{
    public Texture2D Atlas { get; set; }
    public Rect UvRect { get; set; }
    public Color FallbackColor { get; set; } = Color.white;
    public override Texture mainTexture => Atlas != null ? Atlas : Texture2D.whiteTexture;

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        const int sides = 40;
        var radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
        var vertexColor = Atlas != null ? color : FallbackColor;
        var centerUv = UvRect.center;
        helper.AddVert(Vector2.zero, vertexColor, centerUv);

        for (var side = 0; side <= sides; side++)
        {
            var angle = side * Mathf.PI * 2f / sides;
            var unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var uv = centerUv + Vector2.Scale(unit, UvRect.size * .5f);
            helper.AddVert(unit * radius, vertexColor, uv);
        }

        for (var side = 0; side < sides; side++)
            helper.AddTriangle(0, side + 1, side + 2);
    }
}

/// <summary>Single batched graphic for all 120 adjacent-node connections.</summary>
public sealed class PassiveSkillTreeConnections : MaskableGraphic
{
    public readonly struct Segment
    {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly Color Color;

        public Segment(Vector2 start, Vector2 end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }
    }

    private readonly List<Segment> segments = new();

    public void SetSegments(IEnumerable<Segment> values)
    {
        segments.Clear();
        segments.AddRange(values);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        const float thickness = 2.4f;
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var vector = segment.End - segment.Start;
            if (vector.sqrMagnitude < .001f)
                continue;

            var normal = new Vector2(-vector.y, vector.x).normalized * (thickness * .5f);
            var color = segment.Color;
            color.a = .72f;
            var first = helper.currentVertCount;
            helper.AddVert(segment.Start - normal, color, Vector2.zero);
            helper.AddVert(segment.Start + normal, color, Vector2.zero);
            helper.AddVert(segment.End + normal, color, Vector2.zero);
            helper.AddVert(segment.End - normal, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
