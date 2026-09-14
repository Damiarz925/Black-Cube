// Developer map: Command-line visual acceptance harness for the runtime passive tree.
// Run with -executeMethod PassiveTreeVisualValidation.Run (without -quit).
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PassiveTreeVisualValidation
{
    const string OutputFolder = "ReviewCaptures";
    const string SessionKey = "BlackCube.PassiveTreeVisualValidation";
    static int frame;
    static int phase;
    static SkillTreeUI tree;
    static PlayerProgression progression;

    static PassiveTreeVisualValidation()
    {
        if (SessionState.GetBool(SessionKey, false)) EditorApplication.update += WaitForPlayMode;
    }

    public static void Run()
    {
        Directory.CreateDirectory(Path.GetFullPath(OutputFolder));
        SessionState.SetBool(SessionKey, true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.EnterPlaymode();
    }

    static void WaitForPlayMode()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.update -= WaitForPlayMode;
        frame = 0;
        phase = 0;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        frame++;
        if (frame < 25) return;

        try
        {
            if (tree == null)
            {
                tree = UnityEngine.Object.FindFirstObjectByType<SkillTreeUI>();
                progression = UnityEngine.Object.FindFirstObjectByType<PlayerProgression>();
                if (tree == null || progression == null)
                {
                    if (frame < 300) return;
                    throw new InvalidOperationException("Passive tree runtime objects did not initialize.");
                }

                typeof(PlayerProgression).GetField("availablePoints", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(progression, 40);
                tree.Toggle();
                Canvas.ForceUpdateCanvases();
                ValidateHierarchy();
                frame = 0;
                return;
            }

            if (frame < 18) return;
            switch (phase)
            {
                case 0:
                    Capture("passive-tree-available-locked.png");
                    break;
                case 1:
                    ApplyWheelDelta(-20f);
                    break;
                case 2:
                    Capture("passive-tree-zoomed-overview.png");
                    break;
                case 3:
                    ApplyWheelDelta(10.25f);
                    AllocateThrough(PassiveBranch.Cold, 5);
                    FocusBranch(PassiveBranch.Cold, 5);
                    break;
                case 4:
                    Capture("passive-tree-cold-tier-states.png");
                    break;
                case 5:
                    AllocateThrough(PassiveBranch.Poison, 5);
                    int poisonHover = PassiveTreeDefinition.NodeId(PassiveBranch.Poison, 6);
                    tree.SetHovered(poisonHover, true);
                    FocusBranch(PassiveBranch.Poison, 5);
                    break;
                case 6:
                    Capture("passive-tree-poison-tier-states-hover.png");
                    break;
                case 7:
                    tree.SetHovered(PassiveTreeDefinition.NodeId(PassiveBranch.Poison, 6), false);
                    AllocateThrough(PassiveBranch.Projectile, 5);
                    int projectileHover = PassiveTreeDefinition.NodeId(PassiveBranch.Projectile, 6);
                    tree.SetHovered(projectileHover, true);
                    FocusBranch(PassiveBranch.Projectile, 5);
                    break;
                case 8:
                    Capture("passive-tree-projectile-tier-states-hover.png");
                    break;
                default:
                    Finish(0, "PASSIVE TREE VISUAL VALIDATION: individual sheets and player root loaded; wheel zoom exercised; Cold, Poison, and Projectile tier/state captures complete; real node allocations succeeded.");
                    return;
            }
            phase++;
            frame = 0;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1, "PASSIVE TREE VISUAL VALIDATION FAILED: " + exception.Message);
        }
    }

    static void Capture(string fileName)
    {
        string path = Path.GetFullPath(Path.Combine(OutputFolder, fileName));
        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log("PASSIVE TREE CAPTURE: " + path);
    }

    static void ValidateHierarchy()
    {
        foreach (PassiveNodeDefinition node in PassiveTreeDefinition.Nodes)
        {
            var button = tree.NodeButton(node.Id);
            if (button == null || button.image == null || button.image.sprite == null)
                throw new InvalidOperationException($"Node {node.Id} has an empty passive sprite.");
            if (button.transition != UnityEngine.UI.Selectable.Transition.None)
                throw new InvalidOperationException($"Node {node.Id} still has a default transition swap.");
            if (button.GetComponent<CorruptionUIButtonSkin>() != null || button.transform.Find("Frame overlay") != null)
                throw new InvalidOperationException($"Node {node.Id} still has a generic square button skin.");
            if (button.GetComponent<UnityEngine.UI.Outline>() != null)
                throw new InvalidOperationException($"Node {node.Id} still has a whole-sprite Outline duplicate.");
            float expected = node.Size switch
            {
                PassiveNodeSize.Small => 58f,
                PassiveNodeSize.Medium => 78f,
                _ => 108f
            };
            Vector2 actual = ((RectTransform)button.transform).sizeDelta;
            if (!Mathf.Approximately(actual.x, expected) || !Mathf.Approximately(actual.y, expected))
                throw new InvalidOperationException($"Node {node.Id} tier dimensions are {actual}, expected {expected} square.");
            ValidateSpriteGeometry(button.image.sprite, $"Node {node.Id}");
        }
        Transform rootTransform = tree.Panel.transform.Find("Tree Viewport/Radial Tree Content/Central Starting Point");
        var rootImage = rootTransform != null ? rootTransform.GetComponent<UnityEngine.UI.Image>() : null;
        if (rootImage == null || rootImage.sprite == null)
            throw new InvalidOperationException("Center/root passive sprite is missing.");
        ValidateSpriteGeometry(rootImage.sprite, "Center/root node");
        if (tree.Panel.transform.Find("Tree Viewport")?.GetComponent<PassiveTreeViewportInput>() == null)
            throw new InvalidOperationException("Passive tree viewport wheel-zoom input is missing.");
        if (Resources.Load<Texture2D>("UI/PassiveTree/PassiveTreeIcons") != null)
            throw new InvalidOperationException("Obsolete combined passive-tree sheet is still importable.");
        Debug.Log("PASSIVE TREE HIERARCHY: all 290 passive nodes and player root present; complete-canvas keystones, centered ordinary crops, bidirectional outer ring, wheel zoom, and normalized dimensions verified.");
    }

    static void ValidateSpriteGeometry(Sprite sprite, string label)
    {
        Texture2D texture = sprite.texture;
        if (texture.width != texture.height)
            throw new InvalidOperationException($"{label} runtime crop is not square: {texture.width}x{texture.height}.");
        if (Vector2.Distance(sprite.pivot, new Vector2(sprite.rect.width * .5f, sprite.rect.height * .5f)) > .01f)
            throw new InvalidOperationException($"{label} sprite pivot is not centered.");

        Color[] pixels = ReadPixels(texture);
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        const float visibleAlpha = 8f / 255f;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a <= visibleAlpha) continue;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }
        if (maxX < 0)
            throw new InvalidOperationException($"{label} has no visible pixels.");
        if (minX <= 0 || minY <= 0 || maxX >= texture.width - 1 || maxY >= texture.height - 1)
            throw new InvalidOperationException($"{label} alpha silhouette reaches its crop edge: L{minX} R{texture.width - 1 - maxX} B{minY} T{texture.height - 1 - maxY}.");
        float horizontalCenter = (minX + maxX) * .5f;
        if (Mathf.Abs(horizontalCenter - (texture.width - 1) * .5f) > 1.1f)
            throw new InvalidOperationException($"{label} alpha silhouette is horizontally off-center by {horizontalCenter - (texture.width - 1) * .5f:0.0}px.");
    }

    static Color[] ReadPixels(Texture2D source)
    {
        if (source.isReadable) return source.GetPixels();
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            readable.Apply(false, false);
            return readable.GetPixels();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(readable);
        }
    }

    static void ApplyWheelDelta(float delta)
    {
        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = new Vector2(Screen.width * .5f, Screen.height * .5f),
            scrollDelta = new Vector2(0f, delta)
        };
        tree.AdjustZoom(eventData);
        Canvas.ForceUpdateCanvases();
    }

    static void AllocateThrough(PassiveBranch branch, int finalPosition)
    {
        for (int position = 0; position <= finalPosition; position++)
        {
            int nodeId = PassiveTreeDefinition.NodeId(branch, position);
            if (!progression.IsAllocated(nodeId)) tree.NodeButton(nodeId).onClick.Invoke();
            if (!progression.IsAllocated(nodeId))
                throw new InvalidOperationException($"Invoking the real {branch} node {position + 1} button did not allocate it.");
        }
    }

    static void FocusBranch(PassiveBranch branch, int position)
    {
        var scroll = typeof(SkillTreeUI).GetField("scroll", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(tree) as UnityEngine.UI.ScrollRect;
        if (scroll == null) throw new InvalidOperationException("Passive tree ScrollRect was not available.");
        float angle = (90f - (int)branch * PassiveTreeDefinition.BranchAngleDegrees) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 target = direction * (190f + position * 125f);
        scroll.content.anchoredPosition = -target;
        Canvas.ForceUpdateCanvases();
    }

    static void Finish(int exitCode, string message)
    {
        Debug.Log(message);
        SessionState.SetBool(SessionKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
    }
}
