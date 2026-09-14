using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MenuLoadIntegrationTests
{
    string savedValue;
    bool hadSave;

    [SetUp]
    public void SetUp()
    {
        hadSave=PlayerPrefs.HasKey(GamePersistence.SaveKey);
        savedValue=hadSave?PlayerPrefs.GetString(GamePersistence.SaveKey):null;
        GamePersistence.RequestNewGame();
    }

    [TearDown]
    public void TearDown()
    {
        GamePersistence.RequestNewGame();
        if(hadSave)PlayerPrefs.SetString(GamePersistence.SaveKey,savedValue);
        else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);
        PlayerPrefs.Save();
        if(CurrencyInventory.Instance!=null)Object.DestroyImmediate(CurrencyInventory.Instance.gameObject);
    }

    [Test]
    public void NewGame_ClearsPendingLoadRequest()
    {
        PlayerPrefs.SetString(GamePersistence.SaveKey,"{}");
        Assert.That(GamePersistence.RequestLoad(),Is.True);
        Assert.That(GamePersistence.LoadRequested,Is.True);
        GamePersistence.RequestNewGame();
        Assert.That(GamePersistence.LoadRequested,Is.False);
    }

    [Test]
    public void LoadRequest_RestoresExistingSaveAndCanOnlyBeConsumedOnce()
    {
        var data=new GameSaveData();
        data.currencies.Add(new CurrencyStackData(CraftingCurrencyType.MagicToRare,7));
        PlayerPrefs.SetString(GamePersistence.SaveKey,JsonUtility.ToJson(data));

        Assert.That(GamePersistence.RequestLoad(),Is.True);
        Assert.That(GamePersistence.RestoreRequestedGame(out bool restored),Is.True);
        Assert.That(restored,Is.True);
        Assert.That(GamePersistence.LoadRequested,Is.False);
        Assert.That(GamePersistence.RestoreRequestedGame(out _),Is.False);
    }

    [Test]
    public void SceneRoutes_MatchEnabledBuildScenesAndDeathMenuPrefab()
    {
        string[] enabled=EditorBuildSettings.scenes.Where(scene=>scene.enabled).Select(scene=>scene.path).ToArray();
        CollectionAssert.Contains(enabled,"Assets/Scenes/"+GameSceneNames.MainMenu+".unity");
        CollectionAssert.Contains(enabled,"Assets/Scenes/"+GameSceneNames.Gameplay+".unity");

        GameObject root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/PaperBattle/PaperBattle.prefab");
        try
        {
            var menu=root.GetComponentInChildren<DeathMenuUI>(true);
            Assert.That(menu,Is.Not.Null);
            var serialized=new SerializedObject(menu);
            Assert.That(serialized.FindProperty("mainMenuSceneName").stringValue,Is.EqualTo(GameSceneNames.MainMenu));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    [Test]
    public void MainMenuScene_HasNewGameAndAnyAuthoredLoadButtonMustBeWired()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");
        var menu=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MainMenuUI>(true)).Single();
        var serialized=new SerializedObject(menu);
        Assert.That(serialized.FindProperty("newGameButton").objectReferenceValue,Is.Not.Null);
        var authoredLoad=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            .FirstOrDefault(button=>button.name=="Load Game Button");
        if(authoredLoad==null)Assert.That(serialized.FindProperty("loadGameButton").objectReferenceValue,Is.Null);
        else Assert.That(serialized.FindProperty("loadGameButton").objectReferenceValue,Is.SameAs(authoredLoad),
            "The authored Load Game Button must be assigned to MainMenuUI.loadGameButton.");
    }
}
