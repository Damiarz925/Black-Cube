using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MenuAuthoringBuilder
{
    public const string PausePrefabPath="Assets/Prefabs/UI/PauseMenu.prefab";
    public const string ModListPrefabPath="Assets/Prefabs/UI/ModListPanel.prefab";
    public const string MainMenuPrefabPath="Assets/Prefabs/UI/MainMenu.prefab";
    public const string CharacterSlotsPrefabPath="Assets/Prefabs/UI/CharacterSlots.prefab";

    [MenuItem("Black-Cube/UI Authoring/Build Pause and Codex Menus")]
    public static void BuildPause()
    {
        GameObject root=PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            PaperBattleHUD hud=root.GetComponentInChildren<PaperBattleHUD>(true);if(hud==null)throw new InvalidOperationException("PaperBattleHUD is missing.");
            PauseMenuUI pause=hud.GetComponent<PauseMenuUI>()??hud.gameObject.AddComponent<PauseMenuUI>();
            PauseMenuView existing=hud.GetComponent<PauseMenuView>();
            if(existing==null||existing.authoredRoot==null)pause.BuildAuthoring(hud);
            PauseMenuView view=hud.GetComponent<PauseMenuView>();
            ConfigureScreen(view.authoredRoot,UIAuthoringScreenKind.PauseMenu,"screen.pause");
            Configure(view.resumeButton,"pause.resume");Configure(view.optionsButton,"pause.options");Configure(view.codexButton,"pause.codex");Configure(view.saveAndMainMenuButton,"pause.save-main-menu");Configure(view.saveAndQuitButton,"pause.save-quit");Configure(view.optionsBackButton,"pause.options.back");Configure(view.pausePassiveTreeButton,"pause.options.passive-tree");
            PrefabUtility.SaveAsPrefabAsset(view.authoredRoot,PausePrefabPath);
            if(view.codex!=null)
            {
                Transform page=view.authoredRoot.transform.Find("Mod List Page");
                if(page!=null)PrefabUtility.SaveAsPrefabAsset(page.gameObject,ModListPrefabPath);
            }
            EditorUtility.SetDirty(pause);EditorUtility.SetDirty(view);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("MENU AUTHORING: Built Pause and Codex/Mod List prefabs.");
    }

    [MenuItem("Black-Cube/UI Authoring/Build Main Menu")]
    public static void BuildMainMenu()
    {
        const string scenePath="Assets/Scenes/Main Menu.unity";
        Scene scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        MainMenuUI menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);if(menu==null)throw new InvalidOperationException("Main Menu scene has no MainMenuUI.");
        menu.BuildAuthoring();MainMenuView view=menu.GetComponent<MainMenuView>();if(view==null||view.authoredRoot==null)throw new InvalidOperationException("MainMenuView generation failed.");
        ConfigureScreen(view.authoredRoot,UIAuthoringScreenKind.MainMenu,"screen.main-menu");
        Configure(view.newGameButton,"main-menu.new-game");Configure(view.loadGameButton,"main-menu.load-game");Configure(view.achievementsButton,"main-menu.achievements");Configure(view.optionsButton,"main-menu.options");Configure(view.pausePassiveTreeButton,"main-menu.options.passive-tree");Configure(view.optionsBackButton,"main-menu.options.back");Configure(view.confirmOverwriteButton,"main-menu.new-game.confirm-overwrite");Configure(view.cancelOverwriteButton,"main-menu.new-game.cancel-overwrite");Configure(view.beginSelectedClassButton,"main-menu.class.begin");Configure(view.cancelClassButton,"main-menu.class.cancel");Configure(view.cancelSlotsButton,"main-menu.slots.cancel");
        for(int i=0;i<view.classButtons.Count;i++)Configure(view.classButtons[i],"main-menu.class."+PlayerClassCatalog.All[i].Id);for(int i=0;i<view.slotButtons.Count;i++)Configure(view.slotButtons[i],"main-menu.slot."+(i+1));
        PrefabUtility.SaveAsPrefabAsset(view.authoredRoot,MainMenuPrefabPath);if(view.slotSelectionPanel!=null)PrefabUtility.SaveAsPrefabAsset(view.slotSelectionPanel,CharacterSlotsPrefabPath);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("MENU AUTHORING: Built Main Menu and Character Slots prefabs and saved the production scene.");
    }
    static void ConfigureScreen(GameObject root,UIAuthoringScreenKind kind,string id){UIAuthoringScreen screen=root.GetComponent<UIAuthoringScreen>()??root.AddComponent<UIAuthoringScreen>();screen.Configure(kind,id,AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath));EditorUtility.SetDirty(screen);}
    static void Configure(Button button,string id){if(button==null)return;UIAuthoringElement element=button.GetComponent<UIAuthoringElement>()??button.gameObject.AddComponent<UIAuthoringElement>();element.Configure(id,button.name,button.transform as RectTransform,button.targetGraphic as Image,button.GetComponentInChildren<TMP_Text>(true));EditorUtility.SetDirty(element);}
}
