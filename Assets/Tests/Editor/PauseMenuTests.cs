using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PauseMenuTests
{
    GameObject canvas;
    GameObject player;
    PaperBattleHUD hud;

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        BuildFixture();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        if (canvas != null) Object.DestroyImmediate(canvas);
        if (player != null) Object.DestroyImmediate(player);
    }

    [Test]
    public void PauseButtonClosesGameplayPanelsAndResumeAndPlayUseOneClock()
    {
        hud.inventoryPanel.SetActive(true);
        hud.GetButton(TopHUDButtonKind.Pause).onClick.Invoke();
        Assert.That(hud.PauseMenu.IsOpen, Is.True);
        Assert.That(hud.inventoryPanel.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);

        hud.PauseMenu.ResumeButton.onClick.Invoke();
        Assert.That(hud.PauseMenu.IsOpen, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        hud.PauseGameplay();
        hud.GetButton(TopHUDButtonKind.Play).onClick.Invoke();
        Assert.That(hud.PauseMenu.IsOpen, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void OptionsAndBackKeepGameplayPausedAndReturnToPauseMenu()
    {
        bool previous = GameplayOptions.PausePassiveTree;
        try
        {
            hud.PauseGameplay();
            hud.PauseMenu.OptionsButton.onClick.Invoke();
            Assert.That(hud.PauseMenu.IsOptionsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            hud.PauseMenu.PausePassiveTreeButton.onClick.Invoke();
            Assert.That(GameplayOptions.PausePassiveTree, Is.EqualTo(!previous));
            Assert.That(Time.timeScale, Is.Zero);

            hud.PauseMenu.OptionsBackButton.onClick.Invoke();
            Assert.That(hud.PauseMenu.IsOpen, Is.True);
            Assert.That(hud.PauseMenu.IsOptionsOpen, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
        }
        finally { GameplayOptions.PausePassiveTree = previous; }
    }

    [Test]
    public void CodexModListNavigationAndBackNeverResumeGameplay()
    {
        hud.PauseGameplay();
        hud.PauseMenu.CodexButton.onClick.Invoke();
        Assert.That(hud.PauseMenu.Codex.IsCodexPageOpen,Is.True);
        Assert.That(Time.timeScale,Is.Zero);
        hud.PauseMenu.Codex.OpenModList();
        Assert.That(hud.PauseMenu.Codex.IsModListOpen,Is.True);
        hud.PauseMenu.Codex.SelectType(LootManager.GearType.Helmets);
        hud.PauseMenu.Codex.SelectFilter(CodexSideFilter.Suffixes);
        Assert.That(hud.PauseMenu.Codex.SelectedType,Is.EqualTo(LootManager.GearType.Helmets));
        Assert.That(hud.PauseMenu.Codex.SelectedFilter,Is.EqualTo(CodexSideFilter.Suffixes));
        Assert.That(hud.PauseMenu.Codex.Scroll,Is.Not.Null);
        Assert.That(Time.timeScale,Is.Zero);
        hud.PauseMenu.Codex.ReturnToCodex();
        Assert.That(hud.PauseMenu.Codex.IsCodexPageOpen,Is.True);
        hud.PauseMenu.ReturnFromCodex();
        Assert.That(hud.PauseMenu.IsOpen,Is.True);
        Assert.That(Time.timeScale,Is.Zero);
        hud.PauseMenu.ResumeButton.onClick.Invoke();
        Assert.That(Time.timeScale,Is.EqualTo(1f));
    }

    [Test]
    public void SaveAndMainMenuLeavesOnlyAfterSuccessfulCanonicalSave()
    {
        int saves = 0;
        string loaded = null;
        hud.PauseMenu.ConfigureActions(() => { saves++; return false; }, scene => loaded = scene, () => { });
        hud.PauseGameplay();
        LogAssert.Expect(LogType.Error, "PauseMenuUI: save failed; remaining in gameplay.");
        hud.PauseMenu.SaveAndMainMenuButton.onClick.Invoke();
        Assert.That(saves, Is.EqualTo(1));
        Assert.That(loaded, Is.Null);
        Assert.That(hud.PauseMenu.IsOpen, Is.True);
        Assert.That(Time.timeScale, Is.Zero);

        hud.PauseMenu.ConfigureActions(() => { saves++; return true; }, scene => loaded = scene, () => { });
        hud.PauseMenu.SaveAndMainMenuButton.onClick.Invoke();
        Assert.That(saves, Is.EqualTo(2));
        Assert.That(loaded, Is.EqualTo(GameSceneNames.MainMenu));
        Assert.That(hud.PauseMenu.IsOpen, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void SaveAndQuitRequestsQuitOnlyAfterSuccessfulCanonicalSave()
    {
        int saves = 0;
        int quits = 0;
        hud.PauseMenu.ConfigureActions(() => { saves++; return false; }, _ => { }, () => quits++);
        hud.PauseGameplay();
        LogAssert.Expect(LogType.Error, "PauseMenuUI: save failed; remaining in gameplay.");
        hud.PauseMenu.SaveAndQuitButton.onClick.Invoke();
        Assert.That(saves, Is.EqualTo(1));
        Assert.That(quits, Is.Zero);
        Assert.That(hud.PauseMenu.QuitRequested, Is.False);
        Assert.That(hud.PauseMenu.IsOpen, Is.True);

        hud.PauseMenu.ConfigureActions(() => { saves++; return true; }, _ => { }, () => quits++);
        hud.PauseMenu.SaveAndQuitButton.onClick.Invoke();
        Assert.That(saves, Is.EqualTo(2));
        Assert.That(quits, Is.EqualTo(1));
        Assert.That(hud.PauseMenu.QuitRequested, Is.True);
    }

    [Test]
    public void RepeatedPauseResumeDoesNotDuplicateButtonListeners()
    {
        for (int i = 0; i < 6; i++)
        {
            hud.GetButton(TopHUDButtonKind.Pause).onClick.Invoke();
            Assert.That(hud.PauseMenu.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            hud.PauseMenu.ResumeButton.onClick.Invoke();
            Assert.That(hud.PauseMenu.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
        Assert.That(hud.GetComponents<PauseMenuUI>().Length, Is.EqualTo(1));
        Assert.That(hud.PauseMenu.Root.transform.parent, Is.SameAs(canvas.transform));
    }

    [Test]
    public void DestroyingOpenPauseMenuReleasesPausedClock()
    {
        hud.PauseGameplay();
        Assert.That(Time.timeScale, Is.Zero);
        typeof(PaperBattleHUD).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        Object.DestroyImmediate(hud.gameObject);
        hud = null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(canvas.transform.Find("Pause Menu"), Is.Null);
    }

    [Test]
    public void CurrentCanonicalSaveUsesVersionedFilesAndRejectsIncompleteRuntimeCapture()
    {
        Assert.That(GamePersistence.TrySave(), Is.False);
        Assert.That(GamePersistence.LastError, Does.Contain("gameplay authorities"));
        Assert.That(GamePersistence.SchemaVersion, Is.EqualTo(7));
        Assert.That(GamePersistence.PrimaryPath, Does.EndWith(GamePersistence.PrimaryFileName));
    }

    void BuildFixture()
    {
        canvas = new GameObject("Canvas", typeof(Canvas));
        GameObject inventory = new("InventoryPanel", typeof(RectTransform)); inventory.transform.SetParent(canvas.transform, false); inventory.SetActive(false);
        GameObject statsPanel = new("Player Stat Screen", typeof(RectTransform)); statsPanel.transform.SetParent(canvas.transform, false); statsPanel.SetActive(false);
        player = new GameObject("Player");
        StatsComponent stats = player.AddComponent<StatsComponent>(); stats.SetBaseStat(StatTypes.Mana, 100f);
        ManaComponent mana = player.AddComponent<ManaComponent>(); typeof(ManaComponent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mana, null);
        HealthComponent health = player.AddComponent<HealthComponent>(); typeof(HealthComponent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);
        GameObject hudObject = new("Paper Battle HUD", typeof(RectTransform), typeof(Image)); hudObject.transform.SetParent(canvas.transform, false); hudObject.SetActive(false);
        hud = hudObject.AddComponent<PaperBattleHUD>(); hud.player = health; hud.inventoryPanel = inventory; hud.statsPanel = statsPanel;
        typeof(PaperBattleHUD).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        hudObject.SetActive(true);
        typeof(PaperBattleHUD).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
        Canvas.ForceUpdateCanvases();
    }
}
