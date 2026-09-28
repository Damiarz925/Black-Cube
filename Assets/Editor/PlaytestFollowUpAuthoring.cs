using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PlaytestFollowUpAuthoring
{
    [MenuItem("Black-Cube/UI Authoring/Apply Playtest Follow-Up")]
    public static void Apply()
    {
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        if(db.playtestSustainRevision<1)
        {
            foreach(var tier in db.GetDefinition(StatTypes.LifeRegeneration).tiers){tier.minValue*=3;tier.maxValue*=3;}
            db.playtestSustainRevision=1;EditorUtility.SetDirty(db);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:PassiveBranchDataSO"))
        {
            var branch=AssetDatabase.LoadAssetAtPath<PassiveBranchDataSO>(AssetDatabase.GUIDToAssetPath(guid));
            if(branch.playtestSustainRevision>=1)continue;
            foreach(var node in branch.AllAuthoredNodes())
                if(node!=null)foreach(var effect in node.Effects)
                {
                    if(effect.Kind!=PassiveEffectKind.Stat)continue;
                    float factor=effect.Stat switch{StatTypes.LifeOnHit=>.1f,StatTypes.ManaOnHit=>.15f,StatTypes.LifeRegeneration=>3f,_=>1};
                    if(factor!=1)effect.SetStat(effect.Stat,effect.Value*factor);
                }
            branch.playtestSustainRevision=1;EditorUtility.SetDirty(branch);
        }
        var skills=Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        var flurry=skills.skills.FirstOrDefault(s=>s.id==PlayerSkillId.SwordRapidFlurry);
        if(flurry!=null){flurry.manaCost=25;EditorUtility.SetDirty(skills);}
        // Historical selected-skill catalog has seven entries. Production weapon
        // skills intentionally come from CreateProductionDefaults in that case.
        Upgrade(PersistentUIAuthoringInstaller.GameplayPrefabPath,true);
        Upgrade(InventoryAuthoringBuilder.PrefabPath,false);
        UpgradeItemSlot();UpgradeRows();
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYTEST FOLLOW-UP AUTHORING: sustain data, advanced filters, options, skill tooltips and effects bindings applied.");
    }
    static void Upgrade(string path,bool production)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var inventory in root.GetComponentsInChildren<InventoryUI>(true))
            {
                var filter=inventory.GetComponent<AdvancedLootFilterUI>()??inventory.gameObject.AddComponent<AdvancedLootFilterUI>();filter.BuildAuthoring();filter.CompactAuthoring();
                var rect=(RectTransform)inventory.transform;
                if(rect.sizeDelta.x>650){float ratio=560f/rect.sizeDelta.x;rect.sizeDelta*=ratio;}
                var view=inventory.GetComponent<InventoryView>();var grid=view.itemGridRoot.GetComponent<GridLayoutGroup>();
                grid.cellSize=new Vector2(45,45);grid.spacing=new Vector2(13,7);
                var scroll=grid.GetComponentInParent<ScrollRect>(true);
                if(scroll!=null)
                {
                    scroll.content=view.itemGridRoot;scroll.viewport=view.itemGridRoot.parent as RectTransform;
                    if(scroll.viewport.GetComponent<RectMask2D>()==null)scroll.viewport.gameObject.AddComponent<RectMask2D>();
                }
            }
            if(production)
            {
                var death=root.GetComponentInChildren<DeathMenuUI>(true);
                if(death!=null)
                {
                    var data=new SerializedObject(death);var details=(TMP_Text)data.FindProperty("detailsText").objectReferenceValue;
                    if(details!=null)
                    {
                        details.enableAutoSizing=true;details.fontSizeMin=11;details.fontSizeMax=17;
                        ((RectTransform)details.transform.parent).sizeDelta=new Vector2(760,680);
                        details.rectTransform.anchorMin=new Vector2(.05f,.45f);details.rectTransform.anchorMax=new Vector2(.95f,.73f);
                        details.rectTransform.offsetMin=details.rectTransform.offsetMax=Vector2.zero;
                    }
                }
                var hud=root.GetComponentInChildren<PaperBattleHUD>(true);hud.GetComponent<PauseMenuUI>().AddPlaytestSettingsAuthoring(hud);
                var statsRect=(RectTransform)hud.statsPanel.transform;statsRect.anchorMin=statsRect.anchorMax=new Vector2(1,1);statsRect.pivot=new Vector2(1,1);statsRect.anchoredPosition=new Vector2(-24,-160);statsRect.sizeDelta=new Vector2(480,630);
                var statsScroll=hud.statsPanel.transform.Find("Stats scroll") as RectTransform;
                if(statsScroll!=null){statsScroll.anchorMin=new Vector2(.04f,.055f);statsScroll.anchorMax=new Vector2(.96f,.83f);statsScroll.offsetMin=statsScroll.offsetMax=Vector2.zero;}
                var status=root.GetComponentInChildren<StatusHUD>(true);var statusView=root.GetComponentInChildren<StatusHUDView>(true);
                var serialized=new SerializedObject(status);serialized.FindProperty("authoredView").objectReferenceValue=statusView;serialized.ApplyModifiedPropertiesWithoutUndo();
                if(statusView!=null)foreach(var strip in new[]{statusView.playerStrip,statusView.enemyStrip})strip.SetAsLastSibling();
                var skillView=hud.GetComponent<SkillSelectionView>();
                Transform tooltip=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Weapon Skill Hover Details");
                if(tooltip==null)
                {
                    var panel=new GameObject("Weapon Skill Hover Details",typeof(RectTransform),typeof(Image));panel.transform.SetParent(hud.GetComponentInParent<Canvas>().transform,false);
                    var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,105);rect.sizeDelta=new Vector2(420,320);panel.GetComponent<Image>().color=new Color(.025f,.035f,.045f,.98f);panel.GetComponent<Image>().raycastTarget=false;
                    var text=new GameObject("Details",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(panel.transform,false);var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(12,12);tr.offsetMax=new Vector2(-12,-12);var body=text.GetComponent<TextMeshProUGUI>();body.fontSize=14;body.raycastTarget=false;
                    for(int i=0;i<skillView.skillButtons.Count;i++){var tip=skillView.skillButtons[i].gameObject.AddComponent<WeaponSkillTooltip>();tip.skillIndex=i;tip.panel=panel;tip.body=body;}panel.SetActive(false);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void UpgradeItemSlot()
    {
        const string path="Assets/Prefabs/Other Prefabs/Item Slot.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var data=new SerializedObject(root.GetComponent<ItemSlotUI>());
            var icon=(Image)data.FindProperty("iconImage").objectReferenceValue;
            if(icon!=null&&icon.transform!=root.transform)
            {
                icon.rectTransform.anchorMin=new Vector2(.15f,.24f);icon.rectTransform.anchorMax=new Vector2(.85f,.92f);
                icon.rectTransform.offsetMin=icon.rectTransform.offsetMax=Vector2.zero;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void UpgradeRows()
    {
        const string path="Assets/Prefabs/PaperBattle/StatRow.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var layout=root.GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=23;
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=14;text.enableAutoSizing=true;text.fontSizeMin=11;text.fontSizeMax=14;}
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
