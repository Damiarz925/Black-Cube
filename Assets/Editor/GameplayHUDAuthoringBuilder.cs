using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayHUDAuthoringBuilder
{
    public const string PrefabPath = "Assets/Prefabs/UI/GameplayHUD.prefab";

    [MenuItem("Black-Cube/UI Authoring/Build Gameplay HUD")]
    public static void Build()
    {
        UIVisualLibrarySO library = AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath);
        if (library == null) { UIVisualLibraryBuilder.Build(); library = AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath); }
        GameObject root = PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            PaperBattleHUD hud = root.GetComponentInChildren<PaperBattleHUD>(true); if (hud == null) throw new InvalidOperationException("PaperBattleHUD is missing.");
            GameplayHUDView existing = hud.GetComponent<GameplayHUDView>();
            if (existing != null) { Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PersistentUIAuthoringInstaller.GameplayPrefabPath); Debug.Log("GAMEPLAY HUD AUTHORING: Existing authored layout preserved."); return; }
            Invoke(hud, "RemoveLegacyTopHUD"); Invoke(hud, "BuildTopHUD");
            Canvas canvas = hud.GetComponentInParent<Canvas>(); RectTransform artwork = hud.transform.Find("Top HUD Artwork") as RectTransform; if (canvas == null || artwork == null) throw new InvalidOperationException("Default HUD construction did not produce the expected authored roots.");
            Image artworkImage = artwork.GetComponent<Image>(); artworkImage.sprite = library.hudPanel;
            UIAuthoringScreen screen = hud.gameObject.GetComponent<UIAuthoringScreen>() ?? hud.gameObject.AddComponent<UIAuthoringScreen>(); screen.Configure(UIAuthoringScreenKind.GameplayHUD, "screen.gameplay-hud", library);
            GameplayHUDView view = hud.gameObject.AddComponent<GameplayHUDView>(); view.authoredRoot = hud.transform as RectTransform; view.artworkRoot = artwork; view.bottomActionBar = Find<RectTransform>(canvas.transform, "Bottom Action Bar");
            view.playerPortrait = Find<Image>(artwork, "Player Portrait"); view.enemyPortrait = Find<Image>(artwork, "Enemy Portrait"); view.playerName = Find<TMP_Text>(artwork, "Player Name"); view.enemyName = Find<TMP_Text>(artwork, "Enemy Species"); view.runSummary = Find<TMP_Text>(canvas.transform, "Run Summary");
            CaptureResource(artwork, "Player Health", out view.playerLifeFill, out view.playerLifeText); CaptureResource(artwork, "Player Mana", out view.playerManaFill, out view.playerManaText); CaptureResource(artwork, "Enemy Health", out view.enemyLifeFill, out view.enemyLifeText); CaptureResource(artwork, "Enemy Mana", out view.enemyManaFill, out view.enemyManaText);
            view.skillsButton = ConfigureButton(artwork, TopHUDButtonKind.Skills, library); view.passivesButton = ConfigureButton(artwork, TopHUDButtonKind.Passives, library); view.enemyButton = ConfigureButton(artwork, TopHUDButtonKind.Enemy, library); view.inventoryButton = ConfigureButton(artwork, TopHUDButtonKind.Inventory, library); view.statsButton = ConfigureButton(artwork, TopHUDButtonKind.Stats, library); view.pauseButton = ConfigureButton(artwork, TopHUDButtonKind.Pause, library); view.playButton = ConfigureButton(artwork, TopHUDButtonKind.Play, library);
            view.inventoryPanel = hud.inventoryPanel; view.statsPanel = hud.statsPanel; view.passiveTreePanel = canvas.GetComponentInChildren<PassiveTreeView>(true)?.gameObject;
            hud.SetAuthoredView(view); EditorUtility.SetDirty(hud); EditorUtility.SetDirty(view); EditorUtility.SetDirty(screen);
            PrefabUtility.SaveAsPrefabAsset(hud.gameObject, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log("GAMEPLAY HUD AUTHORING: Built persistent HUD in production prefab and reference prefab " + PrefabPath + ".");
    }

    static Button ConfigureButton(Transform root, TopHUDButtonKind kind, UIVisualLibrarySO library)
    {
        Button button = Find<Button>(root, kind + " Button"); if (button == null) throw new InvalidOperationException("Missing " + kind + " Button."); HUDSpriteState state = button.GetComponent<HUDSpriteState>(); HUDButtonVisualSet set = library.HUDButton(kind); if (state == null || set == null) throw new InvalidOperationException("Missing authored visual state for " + kind); state.Configure(button, set.normal, set.hover, set.pressed);
        UIAuthoringElement element = button.GetComponent<UIAuthoringElement>() ?? button.gameObject.AddComponent<UIAuthoringElement>(); element.Configure("hud.button." + kind.ToString().ToLowerInvariant(), kind + " Button", button.transform as RectTransform, button.targetGraphic as Image, null); return button;
    }
    static void CaptureResource(Transform root, string name, out HUDResourceBar fill, out TMP_Text text) { Transform group = FindTransform(root, name); fill = group != null ? Find<HUDResourceBar>(group, "Fill") : null; text = group != null ? Find<TMP_Text>(group, "Value") : null; }
    static T Find<T>(Transform root, string name) where T : Component { Transform found = FindTransform(root, name); return found != null ? found.GetComponent<T>() : null; }
    static Transform FindTransform(Transform root, string name) { foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
    static void Invoke(PaperBattleHUD hud, string name) { MethodInfo method = typeof(PaperBattleHUD).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic); if (method == null) throw new MissingMethodException(typeof(PaperBattleHUD).Name, name); method.Invoke(hud, null); }
}
