using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayPanelAuthoringBuilder
{
    public const string SkillPrefabPath="Assets/Prefabs/UI/SkillSelectionPanel.prefab";
    public const string SubclassPrefabPath="Assets/Prefabs/UI/SubclassPanel.prefab";
    public const string RebirthPrefabPath="Assets/Prefabs/UI/RebirthPanel.prefab";
    public const string ChallengePrefabPath="Assets/Prefabs/UI/ChallengePanel.prefab";
    public const string CraftingPrefabPath="Assets/Prefabs/UI/EndgameCraftingPanel.prefab";
    public const string StatsPrefabPath="Assets/Prefabs/UI/StatsPanel.prefab";
    public const string EnemyInspectionPrefabPath="Assets/Prefabs/UI/EnemyInspectionPanel.prefab";

    [MenuItem("Black-Cube/UI Authoring/Build Skill and Subclass Panels")]
    public static void Build()
    {
        GameObject root=PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        try
        {
            PaperBattleHUD hud=root.GetComponentInChildren<PaperBattleHUD>(true);if(hud==null)throw new InvalidOperationException("PaperBattleHUD is missing.");
            PlayerSkillMenuUI skills=hud.GetComponent<PlayerSkillMenuUI>()??hud.gameObject.AddComponent<PlayerSkillMenuUI>();SkillSelectionView skillView=hud.GetComponent<SkillSelectionView>();if(skillView==null||skillView.informationPanel==null)skills.BuildAuthoring();skillView=hud.GetComponent<SkillSelectionView>();
            SubclassMenuUI subclass=hud.GetComponent<SubclassMenuUI>()??hud.gameObject.AddComponent<SubclassMenuUI>();SubclassView subclassView=hud.GetComponent<SubclassView>();if(subclassView==null||subclassView.panel==null)subclass.BuildAuthoring();subclassView=hud.GetComponent<SubclassView>();
            RebirthConfirmationUI rebirth=hud.GetComponent<RebirthConfirmationUI>()??hud.gameObject.AddComponent<RebirthConfirmationUI>();RebirthView rebirthView=hud.GetComponent<RebirthView>();if(rebirthView==null||rebirthView.confirmationPanel==null)rebirth.BuildAuthoring();rebirthView=hud.GetComponent<RebirthView>();
            ChallengeLauncherUI challenge=hud.GetComponent<ChallengeLauncherUI>()??hud.gameObject.AddComponent<ChallengeLauncherUI>();ChallengeView challengeView=hud.GetComponent<ChallengeView>();if(challengeView==null||challengeView.panel==null)challenge.BuildAuthoring();challengeView=hud.GetComponent<ChallengeView>();
            EndgameItemizationUI crafting=hud.GetComponent<EndgameItemizationUI>()??hud.gameObject.AddComponent<EndgameItemizationUI>();EndgameCraftingView craftingView=hud.GetComponent<EndgameCraftingView>();if(craftingView==null||craftingView.panel==null)crafting.BuildAuthoring();craftingView=hud.GetComponent<EndgameCraftingView>();
            StatsView statsView=hud.statsPanel.GetComponent<StatsView>()??hud.statsPanel.AddComponent<StatsView>();statsView.authoredRoot=hud.statsPanel.transform as RectTransform;statsView.statsController=hud.statsPanel.GetComponent<PlayerStatsPanelUI>();statsView.contentRoot=hud.statsPanel.GetComponentInChildren<ScrollRect>(true)?.content;statsView.headerPrefab=AssetDatabase.LoadAssetAtPath<StatHeaderUI>("Assets/Prefabs/PaperBattle/StatHeader.prefab");statsView.rowPrefab=AssetDatabase.LoadAssetAtPath<StatRowUI>("Assets/Prefabs/PaperBattle/StatRow.prefab");
            EnemyInspectionPanelUI enemy=hud.GetComponent<EnemyInspectionPanelUI>()??hud.gameObject.AddComponent<EnemyInspectionPanelUI>();EnemyInspectionView enemyView=hud.GetComponent<EnemyInspectionView>();if(enemyView==null||enemyView.panel==null)enemy.BuildAuthoring(hud);enemyView=hud.GetComponent<EnemyInspectionView>();
            ConfigureScreen(skillView.informationPanel,UIAuthoringScreenKind.SkillSelection,"screen.skills");ConfigureScreen(subclassView.panel,UIAuthoringScreenKind.Subclass,"screen.subclass");
            ConfigureScreen(rebirthView.confirmationPanel,UIAuthoringScreenKind.Rebirth,"screen.rebirth");ConfigureScreen(challengeView.panel,UIAuthoringScreenKind.Challenges,"screen.challenges");ConfigureScreen(craftingView.panel,UIAuthoringScreenKind.Crafting,"screen.endgame-crafting");
            ConfigureScreen(hud.statsPanel,UIAuthoringScreenKind.Stats,"screen.stats");ConfigureScreen(enemyView.panel,UIAuthoringScreenKind.Stats,"screen.enemy-inspection");
            for(int i=0;i<skillView.skillButtons.Count;i++)Configure(skillView.skillButtons[i],"skills.weapon."+(i+1));Configure(skillView.rageFinisherButton,"skills.rage-finisher");Configure(skillView.closeButton,"skills.close");Configure(subclassView.openButton,"subclass.open");Configure(subclassView.projectileModeButton,"subclass.projectile-mode");for(int i=0;i<subclassView.choiceButtons.Count;i++)Configure(subclassView.choiceButtons[i],"subclass.choice."+(i+1));
            PrefabUtility.SaveAsPrefabAsset(skillView.informationPanel,SkillPrefabPath);PrefabUtility.SaveAsPrefabAsset(subclassView.panel,SubclassPrefabPath);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rebirthView.confirmationPanel,RebirthPrefabPath);PrefabUtility.SaveAsPrefabAsset(challengeView.panel,ChallengePrefabPath);PrefabUtility.SaveAsPrefabAsset(craftingView.panel,CraftingPrefabPath);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(hud.statsPanel,StatsPrefabPath);PrefabUtility.SaveAsPrefabAsset(enemyView.panel,EnemyInspectionPrefabPath);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("GAMEPLAY PANEL AUTHORING: Built skill and subclass panels.");
    }
    static void ConfigureScreen(GameObject root,UIAuthoringScreenKind kind,string id){UIAuthoringScreen screen=root.GetComponent<UIAuthoringScreen>()??root.AddComponent<UIAuthoringScreen>();screen.Configure(kind,id,AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath));}
    static void Configure(Button button,string id){if(button==null)return;UIAuthoringElement element=button.GetComponent<UIAuthoringElement>()??button.gameObject.AddComponent<UIAuthoringElement>();element.Configure(id,button.name,button.transform as RectTransform,button.targetGraphic as Image,button.GetComponentInChildren<TMP_Text>(true));}
}
