using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PassiveTreePrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/UI/PassiveTreePanel.prefab";
    static readonly Color Background = new(.025f, .03f, .04f, 1f), Viewport = new(.045f, .052f, .062f, 1f), Connection = new(.32f, .29f, .25f, 1f);

    [MenuItem("Black-Cube/UI Authoring/Build Passive Tree Prefab From Authored Data")]
    public static void Build()
    {
        PassiveTreeDatabaseSO database = AssetDatabase.LoadAssetAtPath<PassiveTreeDatabaseSO>(PassiveTreeAuthoringMigration.DatabasePath);
        if (database == null) throw new InvalidOperationException("Generate the Passive Tree authoring assets first.");
        EnsureFolder("Assets/Prefabs/UI");
        GameObject root = RectObject("Passive Tree Panel", null, typeof(Image), typeof(Canvas), typeof(GraphicRaycaster), typeof(PassiveTreeView), typeof(UIAuthoringScreen));
        Stretch(root.GetComponent<RectTransform>()); root.GetComponent<Image>().color = Background; root.GetComponent<Canvas>().overrideSorting = true; root.GetComponent<Canvas>().sortingOrder = 100;
        UIVisualLibrarySO visuals = AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>("Assets/GameData/UI/Libraries/SO_UIVisualLibrary.asset");
        root.GetComponent<UIAuthoringScreen>().Configure(UIAuthoringScreenKind.PassiveTree, "screen.passive-tree", visuals);
        TMP_Text title = Text(root.transform, "Title", "WANDERER  /  PASSIVE SKILL TREE", new Vector2(.035f, .9f), new Vector2(.65f, .98f), 30);
        TMP_Text points = Text(root.transform, "Available Points", string.Empty, new Vector2(.035f, .83f), new Vector2(.78f, .91f), 17);
        TMP_Text progression = Text(root.transform, "Progression", string.Empty, new Vector2(.035f, .785f), new Vector2(.78f, .835f), 14);
        Button close = Button(root.transform, "Close", "RETURN TO BATTLE", new Vector2(.82f, .905f), new Vector2(.97f, .97f), out _);
        Button refund = Button(root.transform, "Refund All", "REFUND ALL", new Vector2(.66f, .905f), new Vector2(.81f, .97f), out TMP_Text refundLabel);
        GameObject viewportObject = RectObject("Tree Viewport", root.transform, typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(PassiveTreeViewportInput));
        SetAnchors(viewportObject.GetComponent<RectTransform>(), new Vector2(.025f, .145f), new Vector2(.975f, .825f)); viewportObject.GetComponent<Image>().color = Viewport;
        GameObject contentObject = RectObject("Radial Tree Content", viewportObject.transform); RectTransform content = contentObject.GetComponent<RectTransform>(); content.anchorMin = content.anchorMax = content.pivot = Vector2.one * .5f; content.sizeDelta = Vector2.one * 10000f;
        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>(); scroll.horizontal = scroll.vertical = true; scroll.inertia = true; scroll.decelerationRate = .08f; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 0; scroll.viewport = viewportObject.GetComponent<RectTransform>(); scroll.content = content;
        GameObject hub = RectObject("Central Hub", content, typeof(Image)); SetCentered(hub.GetComponent<RectTransform>(), Vector2.zero, Vector2.one * 130); hub.GetComponent<Image>().color = new Color(.12f, .14f, .18f, 1); hub.GetComponent<Image>().raycastTarget = false;
        TMP_Text details = Text(root.transform, "Node Details", "Hover or select a node for details. Click to allocate, right-click to refund. Drag or use the mouse wheel to explore.", new Vector2(.035f, .025f), new Vector2(.965f, .13f), 15); details.alignment = TextAlignmentOptions.MidlineLeft;

        var branches = new List<PassiveBranchBinding>(); var connections = new List<PassiveConnectionBinding>();
        foreach (PassiveClassBranchSO branch in database.ClassBranches) branches.Add(BuildClassBranch(content, branch, database.IconLibrary, connections));
        foreach (PassiveWeaponBranchSO branch in database.WeaponBranches) branches.Add(BuildWeaponBranch(content, branch, database.IconLibrary, connections));
        PassiveTreeView view = root.GetComponent<PassiveTreeView>(); view.Configure(root, scroll, content, points, progression, details, close, refund, refundLabel, branches, connections);
        root.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); Debug.Log("PASSIVE TREE PREFAB: " + PrefabPath);
    }

    static PassiveBranchBinding BuildClassBranch(RectTransform parent, PassiveClassBranchSO data, PassiveNodeIconLibrarySO icons, List<PassiveConnectionBinding> allConnections)
    {
        int classIndex = PassiveTreeDefinition.ClassIndex(data.ClassId); float angle = new[] { 90f, 30f, -30f, -90f, -150f, 150f }[classIndex];
        GameObject branchObject = BranchRoot(parent, data.name.Replace("SO_", string.Empty) + "View", angle); var tierViews = new List<PassiveTierViewBinding>(); RectTransform previous = null; string previousId = null;
        for (int i = 0; i < data.Tiers.Count; i++)
        {
            PassiveClassTierData tier = data.Tiers[i]; float y = 430 + i * 255; var view = new PassiveTierViewBinding { tier = i + 1 };
            view.spine = Node(branchObject.transform, tier.Spine, new Vector2(0, y), icons, "spine");
            if (previous != null) allConnections.Add(Line(branchObject.transform, previous, view.spine.transform as RectTransform, previousId, tier.Spine.LogicalSlotId, tier.Spine.LogicalSlotId));
            previous = view.spine.transform as RectTransform; previousId = tier.Spine.LogicalSlotId;
            view.left = ClassGroup(branchObject.transform, tier.Left, tier.Spine, view.spine.transform as RectTransform, y, false, icons, allConnections);
            view.right = ClassGroup(branchObject.transform, tier.Right, tier.Spine, view.spine.transform as RectTransform, y, true, icons, allConnections);
            tierViews.Add(view);
        }
        PassiveBranchBinding binding = branchObject.AddComponent<PassiveBranchBinding>(); binding.Configure(data.ClassId, data, tierViews); return binding;
    }

    static PassiveChoiceGroupBinding ClassGroup(Transform parent, PassiveChoiceSideData data, PassiveAuthoredNode spineData, RectTransform spine, float y, bool right, PassiveNodeIconLibrarySO icons, List<PassiveConnectionBinding> all)
    {
        int sign = right ? 1 : -1; string side = right ? "right" : "left";
        GameObject junctionObject = RectObject($"T{spineData.LogicalSlotId.Split('.').Reverse().Skip(1).FirstOrDefault()} {side} Junction", parent, typeof(Image)); RectTransform junction = junctionObject.GetComponent<RectTransform>(); SetCentered(junction, new Vector2(sign * 145, y), Vector2.one * 26); junctionObject.GetComponent<Image>().color = new Color(1, .38f, .08f); junctionObject.GetComponent<Image>().raycastTarget = false;
        var result = new PassiveChoiceGroupBinding { junction = junction };
        result.nativeLayoutRoot = RectObject("Native Four Choice Layout", parent); StretchZero(result.nativeLayoutRoot.GetComponent<RectTransform>());
        result.genericLayoutRoot = RectObject("Generic Three Choice Layout", parent); StretchZero(result.genericLayoutRoot.GetComponent<RectTransform>()); result.genericLayoutRoot.SetActive(false);
        result.a = Node(result.nativeLayoutRoot.transform, data.A, new Vector2(sign * 260, y + 80), icons, side + ".native.a");
        result.b = Node(result.nativeLayoutRoot.transform, data.B, new Vector2(sign * 350, y + 40), icons, side + ".native.b");
        result.c = Node(result.nativeLayoutRoot.transform, data.C, new Vector2(sign * 350, y - 40), icons, side + ".native.c");
        result.subclassSlot = Node(result.nativeLayoutRoot.transform, data.SubclassA, new Vector2(sign * 260, y - 80), icons, side + ".native.subclass");
        result.genericA = Node(result.genericLayoutRoot.transform, data.A, new Vector2(sign * 260, y + 80), icons, side + ".generic.a");
        result.genericB = Node(result.genericLayoutRoot.transform, data.B, new Vector2(sign * 330, y), icons, side + ".generic.b");
        result.genericC = Node(result.genericLayoutRoot.transform, data.C, new Vector2(sign * 260, y - 80), icons, side + ".generic.c");
        all.Add(Line(parent, spine, junction, spineData.LogicalSlotId, string.Empty, data.A.LogicalSlotId, true));
        foreach (PassiveNodeBinding node in new[] { result.a, result.b, result.c, result.subclassSlot }) all.Add(Line(result.nativeLayoutRoot.transform, junction, node.transform as RectTransform, spineData.LogicalSlotId, node.LogicalSlotId, node.LogicalSlotId));
        foreach (PassiveNodeBinding node in new[] { result.genericA, result.genericB, result.genericC }) all.Add(Line(result.genericLayoutRoot.transform, junction, node.transform as RectTransform, spineData.LogicalSlotId, node.LogicalSlotId, node.LogicalSlotId));
        return result;
    }

    static PassiveBranchBinding BuildWeaponBranch(RectTransform parent, PassiveWeaponBranchSO data, PassiveNodeIconLibrarySO icons, List<PassiveConnectionBinding> allConnections)
    {
        int classIndex = PassiveTreeDefinition.ClassIndex(data.OwningClassId); float angle = new[] { 90f, 30f, -30f, -90f, -150f, 150f }[classIndex];
        GameObject branchObject = BranchRoot(parent, data.name.Replace("SO_", string.Empty) + "View", angle); var tierViews = new List<PassiveTierViewBinding>(); RectTransform previous = null; string previousId = null;
        for (int i = 0; i < data.Tiers.Count; i++)
        {
            PassiveWeaponTierData tier = data.Tiers[i]; float y = 2980 + i * 255; var view = new PassiveTierViewBinding { tier = i + 1 };
            view.spine = Node(branchObject.transform, tier.Spine, new Vector2(0, y), icons, "spine");
            if (previous != null) allConnections.Add(Line(branchObject.transform, previous, view.spine.transform as RectTransform, previousId, tier.Spine.LogicalSlotId, tier.Spine.LogicalSlotId));
            previous = view.spine.transform as RectTransform; previousId = tier.Spine.LogicalSlotId;
            view.left = WeaponGroup(branchObject.transform, tier.Left, tier.Spine, view.spine.transform as RectTransform, y, false, icons, allConnections);
            view.right = WeaponGroup(branchObject.transform, tier.Right, tier.Spine, view.spine.transform as RectTransform, y, true, icons, allConnections); tierViews.Add(view);
        }
        PassiveBranchBinding binding = branchObject.AddComponent<PassiveBranchBinding>(); binding.Configure(data.WeaponId, data, tierViews); return binding;
    }

    static PassiveChoiceGroupBinding WeaponGroup(Transform parent, PassiveChoiceSideData data, PassiveAuthoredNode spineData, RectTransform spine, float y, bool right, PassiveNodeIconLibrarySO icons, List<PassiveConnectionBinding> all)
    {
        int sign = right ? 1 : -1; string side = right ? "right" : "left"; GameObject junctionObject = RectObject(side + " Junction", parent, typeof(Image)); RectTransform junction = junctionObject.GetComponent<RectTransform>(); SetCentered(junction, new Vector2(sign * 145, y), Vector2.one * 26); junctionObject.GetComponent<Image>().color = new Color(1, .38f, .08f); junctionObject.GetComponent<Image>().raycastTarget = false;
        var result = new PassiveChoiceGroupBinding { junction = junction, nativeLayoutRoot = null, genericLayoutRoot = null };
        result.a = Node(parent, data.A, new Vector2(sign * 260, y + 80), icons, side + ".a"); result.b = Node(parent, data.B, new Vector2(sign * 330, y), icons, side + ".b"); result.c = Node(parent, data.C, new Vector2(sign * 260, y - 80), icons, side + ".c");
        all.Add(Line(parent, spine, junction, spineData.LogicalSlotId, string.Empty, data.A.LogicalSlotId, true)); foreach (PassiveNodeBinding node in new[] { result.a, result.b, result.c }) all.Add(Line(parent, junction, node.transform as RectTransform, spineData.LogicalSlotId, node.LogicalSlotId, node.LogicalSlotId)); return result;
    }

    static GameObject BranchRoot(RectTransform parent, string name, float angle)
    { GameObject root = RectObject(name, parent); RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = Vector2.one * .5f; rect.pivot = Vector2.one * .5f; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero; rect.localRotation = Quaternion.Euler(0, 0, angle - 90); return root; }

    static PassiveNodeBinding Node(Transform parent, PassiveAuthoredNode data, Vector2 position, PassiveNodeIconLibrarySO icons, string visualToken)
    {
        GameObject go = RectObject(data.DisplayName, parent, typeof(Image), typeof(Button), typeof(PassiveNodeBinding), typeof(PassiveNodeView)); float size = data.Size == PassiveNodeSize.Medium ? 78 : data.Size == PassiveNodeSize.Large ? 108 : 58; SetCentered(go.GetComponent<RectTransform>(), position, Vector2.one * size);
        Image image = go.GetComponent<Image>(); image.sprite = icons.Resolve(data); image.preserveAspect = true; Button button = go.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
        TMP_Text magnitude = Text(go.transform, "Magnitude", data.Effects.Count > 0 ? data.Effects[0].Value.ToString("+0.##;-0.##;0") : string.Empty, new Vector2(.05f, -.3f), new Vector2(.95f, .12f), 10); magnitude.alignment = TextAlignmentOptions.Center;
        TMP_Text label = Text(go.transform, "Authoring Label", visualToken.ToUpperInvariant(), new Vector2(-.5f, 1.05f), new Vector2(1.5f, 1.45f), 10); label.alignment = TextAlignmentOptions.Center; label.gameObject.SetActive(false);
        PassiveNodeBinding binding = go.GetComponent<PassiveNodeBinding>(); binding.Configure("view." + data.LogicalSlotId + "." + visualToken, data.LogicalSlotId, button, image, magnitude, label); return binding;
    }

    static PassiveConnectionBinding Line(Transform parent, RectTransform from, RectTransform to, string fromId, string toId, string visibleId, bool stem = false)
    {
        GameObject go = RectObject("Connection", parent, typeof(Image), typeof(PassiveConnectionBinding)); Image image = go.GetComponent<Image>(); image.color = Connection; image.raycastTarget = false; PassiveConnectionBinding binding = go.GetComponent<PassiveConnectionBinding>(); binding.Configure(from, to, go.GetComponent<RectTransform>(), fromId, toId, visibleId, stem); go.transform.SetAsFirstSibling(); return binding;
    }

    static GameObject RectObject(string name, Transform parent, params Type[] extra)
    { var types = new List<Type> { typeof(RectTransform) }; types.AddRange(extra); GameObject go = new(name, types.ToArray()); if (parent != null) go.transform.SetParent(parent, false); return go; }
    static TMP_Text Text(Transform parent, string name, string value, Vector2 min, Vector2 max, float size) { GameObject go = RectObject(name, parent, typeof(TextMeshProUGUI)); SetAnchors(go.GetComponent<RectTransform>(), min, max); TMP_Text text = go.GetComponent<TMP_Text>(); text.text = value; text.fontSize = size; text.color = Color.white; text.raycastTarget = false; return text; }
    static Button Button(Transform parent, string name, string value, Vector2 min, Vector2 max, out TMP_Text label) { GameObject go = RectObject(name, parent, typeof(Image), typeof(Button), typeof(UIAuthoringElement), typeof(UIButtonVisualController)); SetAnchors(go.GetComponent<RectTransform>(), min, max); Image image = go.GetComponent<Image>(); image.color = new Color(.12f, .15f, .18f); Button button = go.GetComponent<Button>(); button.targetGraphic = image; label = Text(go.transform, "Label", value, Vector2.zero, Vector2.one, 16); label.alignment = TextAlignmentOptions.Center; go.GetComponent<UIAuthoringElement>().Configure("button.passive-tree." + name.ToLowerInvariant().Replace(' ', '-'), name, go.GetComponent<RectTransform>(), image, label); return button; }
    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static void SetCentered(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = position; rect.sizeDelta = size; }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static void StretchZero(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static void EnsureFolder(string path) { string[] parts = path.Split('/'); string current = parts[0]; for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
}
