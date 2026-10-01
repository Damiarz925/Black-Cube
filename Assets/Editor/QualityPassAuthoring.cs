using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Focused, idempotent authoring for the playtest-quality UI additions. Existing
// authored screens are edited in place; their other controls are preserved.
public static class QualityPassAuthoring
{
    public static void ApplyAll()
    {
        Apply();
        AuditClassNodePremium();
        RepairMainMenuDuplicates();
    }

    // The historical Main Menu builder once ran with an unbound view and
    // appended duplicates. Keep the first authored child and discard only the
    // later same-name siblings created by that run.
    public static void RepairMainMenuDuplicates()
    {
        const string scenePath="Assets/Scenes/Main Menu.unity";
        var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        var view=menu!=null?menu.GetComponent<MainMenuView>():null;
        if(view?.authoredRoot==null)throw new InvalidOperationException("Main Menu authored view missing; duplicate repair aborted.");
        Transform root=view.authoredRoot.transform;
        GameObject KeepFirst(string name)
        {
            var matches=root.Cast<Transform>().Where(x=>x.name==name).ToArray();
            if(matches.Length==0)throw new InvalidOperationException("Missing original Main Menu child: "+name);
            for(int i=1;i<matches.Length;i++)UnityEngine.Object.DestroyImmediate(matches[i].gameObject);
            return matches[0].gameObject;
        }
        var optionButton=KeepFirst("Options Button");
        var options=KeepFirst("Options Panel");
        var overwrite=KeepFirst("New Game Overwrite Confirmation");
        var classes=KeepFirst("New Game Class Selection");
        var slots=KeepFirst("Character Slot Selection");
        Button FindButton(GameObject parent,string name)=>parent.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name);
        TMP_Text FindText(GameObject parent,string name)=>parent.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(x=>x.name==name);
        view.optionsButton=optionButton.GetComponent<Button>();view.optionsPanel=options;view.overwriteConfirmation=overwrite;view.classSelectionPanel=classes;view.slotSelectionPanel=slots;
        view.pausePassiveTreeButton=FindButton(options,"Pause Passive Tree Toggle");view.pausePassiveTreeLabel=view.pausePassiveTreeButton?.GetComponentInChildren<TMP_Text>(true);
        view.optionsBackButton=FindButton(options,"Options Back Button");
        view.confirmOverwriteButton=FindButton(overwrite,"Confirm Start New Game");view.cancelOverwriteButton=FindButton(overwrite,"Cancel New Game");
        view.classSelectionLabel=FindText(classes,"SELECT A CLASS Label");view.beginSelectedClassButton=FindButton(classes,"Begin Selected Class");view.cancelClassButton=FindButton(classes,"Cancel Class Selection");
        view.classButtons.Clear();foreach(var definition in PlayerClassCatalog.All)view.classButtons.Add(FindButton(classes,"Choose "+definition.DisplayName));
        view.slotButtons.Clear();for(int i=0;i<GamePersistence.CharacterSlotCount;i++)view.slotButtons.Add(FindButton(slots,"Character Slot "+(i+1)));
        view.cancelSlotsButton=FindButton(slots,"Cancel Slot Selection");
        if(view.classSelectionLabel==null)throw new InvalidOperationException("Original class description label missing; repair aborted.");
        var labelRect=view.classSelectionLabel.rectTransform;
        labelRect.anchoredPosition=new Vector2(0,-178);labelRect.sizeDelta=new Vector2(900,104);
        view.classSelectionLabel.fontSize=18;view.classSelectionLabel.textWrappingMode=TextWrappingModes.Normal;
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        PrefabUtility.SaveAsPrefabAsset(view.authoredRoot,MenuAuthoringBuilder.MainMenuPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(view.slotSelectionPanel,MenuAuthoringBuilder.CharacterSlotsPrefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("QUALITY PASS AUTHORING: Rebound original Main Menu panels and removed duplicate siblings.");
    }

    public static void ReportMainMenuHierarchy()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity",OpenSceneMode.Single);
        var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        var root=menu.GetComponent<MainMenuView>().authoredRoot.transform;
        foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name is "Options Panel" or "Options Button" or "New Game Class Selection"))
        {
            string path=t.name;for(var p=t.parent;p!=null;p=p.parent)path=p.name+"/"+path;
            Debug.Log("QUALITY MENU PATH "+path);
        }
    }

    [MenuItem("Black-Cube/UI Authoring/Apply Quality Pass UI")]
    public static void Apply()
    {
        const string gameplay="Assets/Prefabs/PaperBattle/PaperBattle.prefab";
        GameObject root=PrefabUtility.LoadPrefabContents(gameplay);
        try
        {
            var hud=root.GetComponentInChildren<PaperBattleHUD>(true);
            var inventory=root.GetComponentInChildren<InventoryUI>(true);
            if(hud==null||inventory==null)throw new InvalidOperationException("Production gameplay prefab is missing HUD or inventory.");
            var pause=hud.GetComponent<PauseMenuUI>();
            if(pause==null)throw new InvalidOperationException("Production HUD has no pause menu.");
            pause.AddAutoRestartAuthoring();
            var confirm=inventory.GetComponent<DismantleConfirmationUI>()??inventory.gameObject.AddComponent<DismantleConfirmationUI>();
            confirm.BuildAuthoring();
            EditorUtility.SetDirty(pause);EditorUtility.SetDirty(confirm);
            PrefabUtility.SaveAsPrefabAsset(inventory.gameObject,"Assets/Prefabs/UI/InventoryPanel.prefab");
            var view=hud.GetComponent<PauseMenuView>();
            if(view!=null&&view.authoredRoot!=null)PrefabUtility.SaveAsPrefabAsset(view.authoredRoot,"Assets/Prefabs/UI/PauseMenu.prefab");
            PrefabUtility.SaveAsPrefabAsset(root,gameplay);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        Debug.Log("QUALITY PASS AUTHORING: Pause options and safe dismantle modal updated in production prefabs.");
    }

    [MenuItem("Black-Cube/Passive Tree Authoring/Audit Class Node Premium")]
    public static void AuditClassNodePremium()
    {
        var report=new List<string>{"Class | Node | Stat | Old | New | Decision"};
        foreach(var path in AssetDatabase.FindAssets("t:PassiveClassBranchSO",new[]{"Assets/GameData/PassiveTree/Branches/Class"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var branch=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(path);
            if(branch==null)continue;
            bool changed=false;
            foreach(var node in branch.AllAuthoredNodes())
            {
                if(node.Kind==PassiveNodeKind.Spine||node.Effects.Count==0)continue;
                foreach(var effect in node.Effects)
                {
                    if(effect.Kind!=PassiveEffectKind.Stat)continue;
                    float old=effect.Value,next=old;
                    // These recovery entries fell below the ordinary weapon-tree
                    // reference (.8 Life on Hit, .45 Mana on Hit, 3 Mana Regen).
                    // Other class and subclass values remain untouched by design.
                    int tier=0;var match=System.Text.RegularExpressions.Regex.Match(node.StableId,@"\.t(\d{2})\.");
                    if(match.Success)int.TryParse(match.Groups[1].Value,out tier);
                    if(node.Kind==PassiveNodeKind.SubclassChoice&&branch.ClassId==PlayerClassIds.Priest&&node.StableId.Contains("priest-light")&&effect.Stat==StatTypes.LifeOnHit)next=Mathf.Max(old,1.2f+.08f*((tier-1)/3));
                    else if(node.Kind==PassiveNodeKind.SubclassChoice&&branch.ClassId==PlayerClassIds.Warrior&&effect.Stat==StatTypes.LifeOnHit)next=Mathf.Max(old,1.2f+.08f*((tier-2)/3));
                    else if(node.Kind==PassiveNodeKind.SubclassChoice&&branch.ClassId==PlayerClassIds.Mage&&effect.Stat==StatTypes.ManaOnHit)next=Mathf.Max(old,.675f+.045f*((tier-2)/3));
                    else if(node.Kind==PassiveNodeKind.SubclassChoice&&branch.ClassId==PlayerClassIds.Mage&&effect.Stat==StatTypes.ManaRegeneration)next=Mathf.Max(old,4.5f+.3f*((tier-1)/3));
                    if(next>old+0.0001f){effect.SetStat(effect.Stat,next);changed=true;}
                    report.Add($"{branch.ClassId} | {node.StableId} | {effect.Stat} | {old:0.###} | {next:0.###} | {(next>old+0.0001f?"Premium recovery correction":"Healthy / retained")}");
                }
            }
            if(changed)EditorUtility.SetDirty(branch);
        }
        Directory.CreateDirectory("Logs/QualityPass");File.WriteAllLines("Logs/QualityPass/class-node-audit.txt",report);
        AssetDatabase.SaveAssets();
        Debug.Log("QUALITY PASS: Audited all authored class nodes. Detail: Logs/QualityPass/class-node-audit.txt");
    }
}
