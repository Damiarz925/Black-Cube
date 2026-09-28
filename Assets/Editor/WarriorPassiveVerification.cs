using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

// Focused UI verification only: no combat simulation, survey, or balance changes.
public static class WarriorPassiveVerification
{
    public static void BuildWindows()
    {
        string output = Path.GetFullPath("Builds/WarriorPassiveWindows/BlackCube.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
            locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode
        });
        Directory.CreateDirectory("Logs/WarriorReauthoring");
        File.WriteAllText("Logs/WarriorReauthoring/BuildResult.txt", $"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\noutput={output}\n");
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Warrior Windows build failed.");
    }

    [MenuItem("Black-Cube/Passive Tree Authoring/Capture Warrior UI Verification")]
    public static void Capture()
    {
        var canvasObject = new GameObject("Verification Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var cameraObject = new GameObject("Verification Camera", typeof(Camera));
        var player = new GameObject("Verification Warrior");
        var host = new GameObject("Verification Controller");
        var render = new RenderTexture(1920, 1080, 24);
        GameObject root = null;
        try
        {
            var camera = cameraObject.GetComponent<Camera>();
            camera.targetTexture = render; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -100);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath), canvasObject.transform, false);
            root.SetActive(true);
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            player.AddComponent<PlayerIdentityState>().BeginNewGame(PlayerClassIds.Warrior);
            var progression = player.AddComponent<PlayerProgression>();
            progression.RestoreProgression(30, 0, 30, new int[PassiveTreeDefinition.NodeCount]);
            var view = root.GetComponent<PassiveTreeView>();
            var presentation = root.AddComponent<PassiveTreePresentation>(); presentation.Initialize(view, progression);
            var controller = host.AddComponent<SkillTreeUI>();
            void Set(string name, object value) => typeof(SkillTreeUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, value);
            void Call(string name) => typeof(SkillTreeUI).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            Set("authoredView", view); Set("panel", root); Set("points", view.points); Set("xp", view.progression); Set("details", view.details); Set("scroll", view.scroll); Set("progression", progression); Set("presentation", presentation);
            typeof(SkillTreeUI).GetProperty("IsOpen").SetValue(null, true);
            Canvas.ForceUpdateCanvases(); Call("BindAuthoredTree"); Call("Refresh");
            void Shot(string name)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var line in view.connections) line.UpdateGeometry();
                Canvas.ForceUpdateCanvases(); camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = render;
                var image = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
                try { image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply(); Directory.CreateDirectory("Logs/WarriorReauthoring/Captures"); File.WriteAllBytes("Logs/WarriorReauthoring/Captures/" + name + ".png", image.EncodeToPNG()); }
                finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
            }
            Shot("01-empty-tree");
            progression.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior, 1)); Call("Refresh");
            var slot = view.branches.First(x => x.RouteId == PlayerClassIds.Warrior).Tiers[0].leftSlot;
            slot.button.onClick.Invoke(); Shot("02-choice-popup");
            view.choicePopup.options[0].onClick.Invoke(); Shot("03-selected-life");
            for (int tier = 2; tier <= 10; tier++) progression.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior, tier));
            Call("Refresh"); Shot("04-sword-unlocked");
            Debug.Log("WARRIOR UI CAPTURE: four production-prefab states rendered.");
        }
        finally
        {
            typeof(SkillTreeUI).GetProperty("IsOpen").SetValue(null, false);
            if(root != null) UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(canvasObject); UnityEngine.Object.DestroyImmediate(cameraObject);
            render.Release(); UnityEngine.Object.DestroyImmediate(render);
        }
    }
}
