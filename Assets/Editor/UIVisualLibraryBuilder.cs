using System;
using UnityEditor;
using UnityEngine;

public static class UIVisualLibraryBuilder
{
    public const string LibraryPath = "Assets/GameData/UI/Libraries/SO_UIVisualLibrary.asset";
    public const string DefaultButtonPath = "Assets/GameData/UI/Libraries/SO_DefaultButtonVisualStyle.asset";

    [MenuItem("Black-Cube/UI Authoring/Create Default Visual Libraries")]
    public static void Build()
    {
        UIVisualLibrarySO existing = AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(LibraryPath);
        if (existing != null) { Selection.activeObject = existing; Debug.Log("UI VISUAL LIBRARY: Existing author-owned library preserved."); return; }
        UIButtonVisualStyleSO style = ScriptableObject.CreateInstance<UIButtonVisualStyleSO>();
        style.normal.color = Color.white; style.hovered.color = new Color(1f, .86f, .58f); style.pressed.color = new Color(.85f, .62f, .28f); style.disabled.color = new Color(.42f, .42f, .42f); style.selected.color = new Color(.92f, .62f, .2f);
        style.hovered.scaleMultiplier = Vector2.one * 1.03f; style.pressed.scaleMultiplier = Vector2.one * .98f; style.selected.scaleMultiplier = Vector2.one;
        AssetDatabase.CreateAsset(style, DefaultButtonPath);
        UIVisualLibrarySO library = ScriptableObject.CreateInstance<UIVisualLibrarySO>(); library.defaultButtonStyle = style; library.hudButtonStyle = style; library.inventoryToggleStyle = style; AssetDatabase.CreateAsset(library, LibraryPath);
        Texture2D baseTexture = Resources.Load<Texture2D>("UI/TopHUD/TopHUDBarEmpty"); library.hudPanel = AddSprite(library, baseTexture, TopHUDLayout.Artwork, "Top HUD Panel"); library.missingSprite = library.hudPanel;
        foreach (TopHUDButtonKind kind in Enum.GetValues(typeof(TopHUDButtonKind)))
        {
            Texture2D sheet = Resources.Load<Texture2D>(TopHUDLayout.ResourceName(kind)); var set = new HUDButtonVisualSet { kind = kind };
            set.normal = AddSprite(library, sheet, TopHUDLayout.StateSlice(kind, 0), kind + " Normal"); set.hover = AddSprite(library, sheet, TopHUDLayout.StateSlice(kind, 1), kind + " Hover"); set.pressed = AddSprite(library, sheet, TopHUDLayout.StateSlice(kind, 2), kind + " Pressed"); library.hudButtons.Add(set);
        }
        EditorUtility.SetDirty(style); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Selection.activeObject = library;
        Debug.Log("UI VISUAL LIBRARY: Created " + LibraryPath + " with persistent top-HUD and button-state sprites.");
    }

    static Sprite AddSprite(UnityEngine.Object container, Texture2D texture, RectInt topLeft, string name)
    {
        if (texture == null) return null; Rect rect = new(topLeft.x, texture.height - topLeft.yMax, topLeft.width, topLeft.height); Sprite sprite = Sprite.Create(texture, rect, Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect); sprite.name = name; AssetDatabase.AddObjectToAsset(sprite, container); return sprite;
    }
}
