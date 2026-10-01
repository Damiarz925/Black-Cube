using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Targeted upgrade: never reset the user's menu rectangles or rebuild the passive prefab.
public static class SystemsRedesignAuthoring
{
    [MenuItem("Black-Cube/UI Authoring/Fix Relic Actions Placement")]
    public static void FixRelicActions()
    {
        foreach(string path in new[]{PersistentUIAuthoringInstaller.GameplayPrefabPath,InventoryAuthoringBuilder.PrefabPath})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var inventory in root.GetComponentsInChildren<InventoryUI>(true))PlaceRelicActions(inventory);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RELIC ACTIONS: authored only inside the Relic inventory, with reserved list space.");
    }
    static void PlaceRelicActions(InventoryUI inventory)
    {
        var relicRoot=inventory.GetComponent<InventoryView>().relicRoot;
        if(relicRoot==null)throw new System.InvalidOperationException("Missing Relic inventory root.");
        var toolbar=relicRoot.Find("Relic actions") as RectTransform;
        if(toolbar==null)
        {
            toolbar=(RectTransform)new GameObject("Relic actions",typeof(RectTransform),typeof(LayoutElement)).transform;
            toolbar.SetParent(relicRoot,false);
        }
        toolbar.GetComponent<LayoutElement>().ignoreLayout=true;
        toolbar.anchorMin=new Vector2(0,1);toolbar.anchorMax=Vector2.one;toolbar.pivot=new Vector2(.5f,1);
        toolbar.anchoredPosition=Vector2.zero;toolbar.sizeDelta=new Vector2(0,44);
        var layout=relicRoot.GetComponent<VerticalLayoutGroup>();
        if(layout!=null)layout.padding.top=Mathf.Max(layout.padding.top,52);
        Component[] actions={inventory.GetComponent<RelicFusionUI>(),inventory.GetComponent<UniqueRelicForgeUI>()};
        for(int i=0;i<actions.Length;i++)
        {
            var data=new SerializedObject(actions[i]);var button=(Button)data.FindProperty("open").objectReferenceValue;
            var rect=(RectTransform)button.transform;rect.SetParent(toolbar,false);
            rect.anchorMin=new Vector2(i*.5f,0);rect.anchorMax=new Vector2((i+1)*.5f,1);
            rect.offsetMin=new Vector2(4,4);rect.offsetMax=new Vector2(-4,-4);
        }
    }
    [MenuItem("Black-Cube/UI Authoring/Apply Systems Redesign Regression Fixes")]
    public static void Apply()
    {
        UpgradeMage();
        UpgradeInventory(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        UpgradeInventory(InventoryAuthoringBuilder.PrefabPath);
        var tooltip=PrefabUtility.LoadPrefabContents(TooltipAuthoringBuilder.ItemTooltipPrefabPath);
        try{tooltip.GetComponent<ItemTooltipUI>().AuthorEstimatedDps();PrefabUtility.SaveAsPrefabAsset(tooltip,TooltipAuthoringBuilder.ItemTooltipPrefabPath);}
        finally{PrefabUtility.UnloadPrefabContents(tooltip);}
        AssetDatabase.SaveAssets();
        Debug.Log("SYSTEMS REDESIGN REGRESSIONS: Mage data and Advanced Loot authoring upgraded.");
    }
    public static void BuildWindows()
    {
        var report=UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            locationPathName="Builds/SystemsRedesignWindows/BlackCube.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode
        });
        Debug.Log($"SYSTEMS REDESIGN BUILD: {report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}");
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.InvalidOperationException("Windows build failed.");
    }
    public static void UpgradeMage()
    {
        var branch=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Mage);
        var nodes=branch.Tiers.SelectMany(t=>t.Left.GenericNodes.Concat(t.Right.GenericNodes)).ToArray();
        foreach(var stat in new[]{StatTypes.ReducedShockEffect,StatTypes.ReducedChillEffect,StatTypes.SpellEchoChance})
        {
            var matching=nodes.Where(n=>n.Effects.Any(e=>e.Kind==PassiveEffectKind.Stat&&(e.Stat==stat||(stat==StatTypes.SpellEchoChance&&e.Stat==StatTypes.ColdDmg)))).ToArray();
            if(matching.Length==0)throw new System.InvalidOperationException("Missing Mage family: "+stat);
            float value=(stat==StatTypes.SpellEchoChance?10f:40f)/matching.Length;
            foreach(var node in matching)
            {
                var effects=node.Effects.Select(e=>new PassiveEffect(e.Stat==StatTypes.ColdDmg?StatTypes.SpellEchoChance:e.Stat,
                    e.Stat==stat||(stat==StatTypes.SpellEchoChance&&e.Stat==StatTypes.ColdDmg)?value:e.Value)).ToArray();
                node.Configure(node.StableId,StatDisplayFormatting.ToFriendlyName(stat),stat==StatTypes.SpellEchoChance?"Magic casts can echo at full strength, paying progressively more Mana. Effective chance is capped at 60%.":"Reduces incoming ailment effectiveness.",
                    node.Branch,node.Size,node.Kind,node.Keystone,effects,node.LogicalSlotId);
                if(stat==StatTypes.SpellEchoChance)node.SetIcon(null);
            }
        }
        EditorUtility.SetDirty(branch);
    }
    static void UpgradeInventory(string path)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var inventory in root.GetComponentsInChildren<InventoryUI>(true))
            {
                var filter=inventory.GetComponent<AdvancedLootFilterUI>();filter.UpgradeAuthoring();
                (inventory.GetComponent<UniqueRelicForgeUI>()??inventory.gameObject.AddComponent<UniqueRelicForgeUI>()).BuildAuthoring();
                (inventory.GetComponent<RelicFusionUI>()??inventory.gameObject.AddComponent<RelicFusionUI>()).BuildAuthoring();
                PlaceRelicActions(inventory);
                var old=inventory.GetComponent<InventoryModHighlightUI>();
                if(old!=null)
                {
                    var data=new SerializedObject(old);var button=(Button)data.FindProperty("openButton").objectReferenceValue;
                    var advanced=new SerializedObject(filter);var open=(Button)advanced.FindProperty("open").objectReferenceValue;
                    if(button!=null&&open!=null)
                    {
                        var a=(RectTransform)open.transform;var b=(RectTransform)button.transform;
                        a.anchorMin=b.anchorMin;a.anchorMax=b.anchorMax;a.pivot=b.pivot;a.anchoredPosition=b.anchoredPosition;a.sizeDelta=b.sizeDelta;
                        Object.DestroyImmediate(button.gameObject);
                    }
                    foreach(string field in new[]{"panel","confirmation"})
                    {var obj=data.FindProperty(field).objectReferenceValue as GameObject;if(obj!=null)Object.DestroyImmediate(obj);}
                    Object.DestroyImmediate(old);
                }
                var legacy=inventory.GetComponent<InventoryFilterUI>();
                if(legacy!=null)
                {
                    var data=new SerializedObject(legacy);
                    foreach(string field in new[]{"filterButton","panel"})
                    {var obj=data.FindProperty(field)?.objectReferenceValue;if(obj is Component c)Object.DestroyImmediate(c.gameObject);else if(obj is GameObject go)Object.DestroyImmediate(go);}
                    Object.DestroyImmediate(legacy);
                }
                var view=inventory.GetComponent<InventoryView>();view.filterRoot=null;view.modFilterRoot=null;
            }
            foreach(var strip in root.GetComponentsInChildren<RelicEquipmentUI>(true))strip.Build();
            foreach(var rebirth in root.GetComponentsInChildren<RebirthConfirmationUI>(true))rebirth.AuthorSystemsSetup();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
