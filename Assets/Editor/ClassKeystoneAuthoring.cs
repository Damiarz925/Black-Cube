using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ClassKeystoneAuthoring
{
    const string TuningPath="Assets/Resources/GameData/PassiveTree/SO_ClassKeystoneTuning.asset";
    [MenuItem("Black-Cube/Passive Tree Authoring/Install Class Keystone and Provisional Weapon Trees")]
    public static void Apply()
    {
        // Do not read PassiveTreeDefinition until all assets have the new shape:
        // its immutable topology is constructed from these assets on first use.
        var db=Resources.Load<PassiveTreeDatabaseSO>("GameData/PassiveTree/SO_PassiveTreeDatabase");
        if(db==null||db.ClassBranches.Count!=6||db.WeaponBranches.Count!=6)throw new InvalidOperationException("Missing authoritative branches.");
        string backup="Logs/ClassKeystones/Baseline";Directory.CreateDirectory(backup);
        foreach(var branch in db.ClassBranches.Cast<PassiveBranchDataSO>().Concat(db.WeaponBranches))
        {
            string source=AssetDatabase.GetAssetPath(branch),dest=Path.Combine(backup,Path.GetFileName(source));if(!File.Exists(dest))File.Copy(source,dest);
        }
        var tuning=AssetDatabase.LoadAssetAtPath<ClassKeystoneTuningSO>(TuningPath);
        if(tuning==null){tuning=ScriptableObject.CreateInstance<ClassKeystoneTuningSO>();tuning.definitions=ClassKeystoneCatalog.CreateDefaults();AssetDatabase.CreateAsset(tuning,TuningPath);}
        foreach(var branch in db.ClassBranches)
        {
            var keys=new List<PassiveAuthoredNode>();
            foreach(var definition in ClassKeystoneCatalog.ForClass(branch.ClassId))
            {
                var node=new PassiveAuthoredNode();node.Configure(definition.StableId,definition.name,definition.description,PassiveBranch.Physical,PassiveNodeSize.Large,PassiveNodeKind.Keystone,definition.id,Array.Empty<PassiveEffect>());
                node.SetIcon(db.IconLibrary.GenericFallback);keys.Add(node);
            }
            branch.ConfigureKeystones(keys);EditorUtility.SetDirty(branch);
        }
        foreach(var branch in db.WeaponBranches){branch.Configure(branch.WeaponId,branch.OwningClassId,ProvisionalWeaponTreeContent.Create(branch.WeaponId));EditorUtility.SetDirty(branch);}
        RetuneRegeneration(db,tuning);
        AuthorWeaponAffixes();
        AssetDatabase.SaveAssets();ConvertPrefab(db);Debug.Log("CLASS KEYSTONE / PROVISIONAL WEAPON AUTHORING: PASS");
    }
    public static void AuthorWeaponAffixes()
    {
        var mods=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        foreach(StatTypes stat in Enum.GetValues(typeof(StatTypes)))
        {
            string weapon=WeaponExclusiveAffixRules.Weapon(stat);if(weapon==null)continue;
            var definition=mods.GetDefinition(stat);
            if(definition==null)throw new InvalidOperationException("Missing stable weapon affix "+stat);
            definition.displayName=StatDisplayFormatting.ToFriendlyName(stat);
            definition.side=AffixSide.Suffix;
            // Eligibility is centralized and applies only on weapons: preserve any
            // existing nonweapon families (notably cooldown recovery).
            WeaponExclusiveAffixRules.TryGet(stat,LootManager.GearType.Weapons,weapon,out var tiers);
            if(stat==StatTypes.AxePhysicalRage||stat==StatTypes.CullingStrike)
            {definition.allowedSlots=new[]{LootManager.GearType.Weapons};definition.tiers=tiers;definition.empowerable=false;}
        }
        EditorUtility.SetDirty(mods);AssetDatabase.SaveAssets();
    }
    static void RetuneRegeneration(PassiveTreeDatabaseSO db,ClassKeystoneTuningSO tuning)
    {
        if(tuning.percentageRegenerationAuthored)return;
        foreach(var branch in db.ClassBranches)
        {
            foreach(var node in branch.AllAuthoredNodes())foreach(var effect in node.Effects)
                if(effect.Kind==PassiveEffectKind.Stat&&effect.Stat==StatTypes.LifeRegeneration)effect.SetStat(effect.Stat,effect.Value*.01f);
            EditorUtility.SetDirty(branch);
        }
        var mods=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        var definition=mods.GetDefinition(StatTypes.LifeRegeneration);
        foreach(var tier in definition.tiers){tier.minValue*=.01f;tier.maxValue*=.01f;}
        definition.empoweredMin*=.01f;definition.empoweredMax*=.01f;
        EditorUtility.SetDirty(mods);tuning.percentageRegenerationAuthored=true;EditorUtility.SetDirty(tuning);
        Debug.Log("Life regeneration authoring: all class/subclass sources and fallback gear tiers converted to percent Max Life/sec. Weapon profiles already use percentage points.");
    }
    static void ConvertPrefab(PassiveTreeDatabaseSO db)
    {
        var root=PrefabUtility.LoadPrefabContents(PassiveTreePrefabBuilder.PrefabPath);
        try
        {
            var view=root.GetComponent<PassiveTreeView>();
            // All old graph lines are replaced by explicit center-to-center sockets.
            foreach(var line in view.connections.ToArray())if(line!=null)UnityEngine.Object.DestroyImmediate(line.gameObject);view.connections.Clear();
            foreach(var branch in view.branches)
            {
                if(branch.Data is PassiveClassBranchSO cls)
                {
                    for(int i=0;i<10;i++){var tier=branch.Tiers[i];Vector2 p=new(0,300+i*180);((RectTransform)tier.spine.transform).anchoredPosition=p;((RectTransform)tier.leftSlot.transform).anchoredPosition=p+new Vector2(-240,-25);((RectTransform)tier.rightSlot.transform).anchoredPosition=p+new Vector2(240,-25);AddLine(view,branch,tier.spine.transform,tier.leftSlot.transform,tier.spine.LogicalSlotId,tier.leftSlot.choiceGroupId+".a");AddLine(view,branch,tier.spine.transform,tier.rightSlot.transform,tier.spine.LogicalSlotId,tier.rightSlot.choiceGroupId+".a");if(i>0)AddLine(view,branch,branch.Tiers[i-1].spine.transform,tier.spine.transform,branch.Tiers[i-1].spine.LogicalSlotId,tier.spine.LogicalSlotId);}
                    if(branch.keystoneSlot==null)branch.keystoneSlot=Slot(branch.transform,"tree.v3."+cls.ClassId+".keystone",new Vector2(0,2120),120);
                    AddLine(view,branch,branch.Tiers[9].spine.transform,branch.keystoneSlot.transform,branch.Tiers[9].spine.LogicalSlotId,cls.Keystones[0].LogicalSlotId);
                }
                else if(branch.Data is PassiveWeaponBranchSO weapon)
                {
                    var template=UnityEngine.Object.Instantiate(branch.Tiers[0].spine.gameObject);template.SetActive(false);
                    foreach(Transform child in branch.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                    var tiers=new List<PassiveTierViewBinding>();
                    for(int i=0;i<7;i++)
                    {
                        var nodeObject=UnityEngine.Object.Instantiate(template,branch.transform);nodeObject.SetActive(true);var node=nodeObject.GetComponent<PassiveNodeBinding>();string prefix=$"tree.v3.{weapon.WeaponId}.t{i+1:00}.";node.Configure(prefix+"spine",prefix+"spine",node.Button,node.Icon,node.Magnitude,null);nodeObject.name=prefix+"spine";var rect=(RectTransform)node.transform;rect.anchoredPosition=ProvisionalWeaponTreeContent.SpinePosition(weapon.WeaponId,i+1);rect.sizeDelta=Vector2.one*88;
                        float offset=ProvisionalWeaponTreeContent.SideOffset(weapon.WeaponId,i+1);var tier=new PassiveTierViewBinding{tier=i+1,spine=node,left=new(),right=new(),leftSlot=Slot(branch.transform,prefix+"left",rect.anchoredPosition+new Vector2(-offset,-30)),rightSlot=Slot(branch.transform,prefix+"right",rect.anchoredPosition+new Vector2(offset,-30))};tiers.Add(tier);AddLine(view,branch,node.transform,tier.leftSlot.transform,prefix+"spine",prefix+"left.a");AddLine(view,branch,node.transform,tier.rightSlot.transform,prefix+"spine",prefix+"right.a");if(i>0)AddLine(view,branch,tiers[i-1].spine.transform,node.transform,tiers[i-1].spine.LogicalSlotId,prefix+"spine");
                    }
                    UnityEngine.Object.DestroyImmediate(template);branch.Configure(weapon.WeaponId,weapon,tiers);
                }
                branch.RefreshAuthoringPreview();
            }
            Navigation(view);PrefabUtility.SaveAsPrefabAsset(root,PassiveTreePrefabBuilder.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static RectTransform Rect(string name,Transform parent,params Type[] types){var components=new List<Type>{typeof(RectTransform)};components.AddRange(types);var go=new GameObject(name,components.ToArray());go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;return rect;}
    static PassiveChoiceSlotView Slot(Transform parent,string group,Vector2 position,float size=88)
    {
        var rect=Rect(group+" Slot",parent,typeof(Image),typeof(Button),typeof(PassiveChoiceSlotView),typeof(PassiveChoiceSlotInput));rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*size;var slot=rect.GetComponent<PassiveChoiceSlotView>();slot.choiceGroupId=group;slot.icon=PassiveCircularIconAuthoring.Wrap(rect.GetComponent<Image>());slot.button=rect.GetComponent<Button>();slot.button.targetGraphic=slot.icon;slot.button.transition=Selectable.Transition.None;
        var glow=Rect("Empty Glow",rect,typeof(CanvasRenderer),typeof(EmptyPassiveSlotGraphic));glow.anchorMin=Vector2.zero;glow.anchorMax=Vector2.one;glow.offsetMin=glow.offsetMax=Vector2.zero;slot.emptyGlow=glow.GetComponent<EmptyPassiveSlotGraphic>();var label=Rect("Label",rect,typeof(TextMeshProUGUI));label.anchoredPosition=new Vector2(0,-size*.7f);label.sizeDelta=new Vector2(230,45);slot.label=label.GetComponent<TMP_Text>();slot.label.fontSize=13;slot.label.alignment=TextAlignmentOptions.Center;slot.label.raycastTarget=false;return slot;
    }
    static void AddLine(PassiveTreeView view,PassiveBranchBinding parent,Transform from,Transform to,string a,string b){var rect=Rect("Connection",parent.transform,typeof(Image),typeof(PassiveConnectionBinding));rect.SetAsFirstSibling();rect.GetComponent<Image>().raycastTarget=false;var line=rect.GetComponent<PassiveConnectionBinding>();line.Configure((RectTransform)from,(RectTransform)to,rect,a,b,b);line.UpdateGeometry();view.connections.Add(line);}
    static Button Button(Transform parent,string name,string label,Vector2 position){var rect=Rect(name,parent,typeof(Image),typeof(Button));rect.anchoredPosition=position;rect.sizeDelta=new Vector2(230,45);rect.GetComponent<Image>().color=new Color(.12f,.18f,.23f);var textRect=Rect("Label",rect,typeof(TextMeshProUGUI));textRect.sizeDelta=rect.sizeDelta;var text=textRect.GetComponent<TMP_Text>();text.text=label;text.fontSize=16;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return rect.GetComponent<Button>();}
    static void Navigation(PassiveTreeView view)
    {
        var navigation=view.GetComponent<PassiveTreeNavigation>()??view.gameObject.AddComponent<PassiveTreeNavigation>();
        if(navigation.chooseClass==null)navigation.chooseClass=Button(view.transform,"Choose Class","CHOOSE CLASS ROUTE",new Vector2(-280,-420));
        if(navigation.chooseWeapon==null)navigation.chooseWeapon=Button(view.transform,"Choose Weapon","CHOOSE WEAPON TREE",new Vector2(0,-420));
        if(navigation.back==null)navigation.back=Button(view.transform,"Weapon Back","BACK TO CLASS TREES",new Vector2(280,-420));
        navigation.hub=view.playerHub.GetComponent<Button>()??view.playerHub.gameObject.AddComponent<Button>();
        if(view.playerHub.TryGetComponent<Image>(out var hubImage))hubImage.raycastTarget=true;
        if(navigation.selectionRoot==null){var rect=Rect("Route Selection",view.transform,typeof(Image));rect.sizeDelta=new Vector2(760,480);rect.GetComponent<Image>().color=new Color(.04f,.07f,.09f,.98f);navigation.selectionRoot=rect.gameObject;var text=Rect("Warning",rect,typeof(TextMeshProUGUI));text.anchoredPosition=new Vector2(0,185);text.sizeDelta=new Vector2(700,70);navigation.warning=text.GetComponent<TMP_Text>();navigation.warning.fontSize=20;navigation.warning.alignment=TextAlignmentOptions.Center;for(int i=0;i<6;i++)navigation.options.Add(Button(rect,"Option "+i,"",new Vector2(i%2==0?-170:170,100-i/2*85)));navigation.cancel=Button(rect,"Cancel","CANCEL",new Vector2(0,-175));rect.gameObject.SetActive(false);}
    }
}
