using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarriorPassiveSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith(WarriorPassiveReauthoring.ArtPath,StringComparison.Ordinal))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.alphaIsTransparency=true;t.mipmapEnabled=false;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=1024;
    }
}

public static class WarriorPassiveReauthoring
{
    public const string ArtPath="Assets/Art/UI/PassiveTree/Warrior/", BranchPath="Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset", LayoutPath="Assets/Resources/GameData/PassiveTree/SO_PassiveTreeLayout.asset";
    public static readonly StatTypes[,] Choices={
        {StatTypes.LifePercent,StatTypes.ArmourPercent,StatTypes.ManaOnHit,StatTypes.BleedChance,StatTypes.ChanceToHitTwice,StatTypes.CritChance},
        {StatTypes.AllRes,StatTypes.LifeOnHit,StatTypes.CritChance,StatTypes.BleedDmg,StatTypes.AttackSpeed,StatTypes.CooldownReduction},
        {StatTypes.LifePercent,StatTypes.AllRes,StatTypes.CooldownReduction,StatTypes.BleedChance,StatTypes.BleedDmg,StatTypes.StrengthPercent},
        {StatTypes.ArmourPercent,StatTypes.LifeOnHit,StatTypes.ManaOnHit,StatTypes.ChanceToHitTwice,StatTypes.AttackSpeed,StatTypes.CritChance},
        {StatTypes.LifePercent,StatTypes.LifeOnHit,StatTypes.CritChance,StatTypes.BleedChance,StatTypes.AttackSpeed,StatTypes.StrengthPercent},
        {StatTypes.ArmourPercent,StatTypes.AllRes,StatTypes.CooldownReduction,StatTypes.ChanceToHitTwice,StatTypes.BleedDmg,StatTypes.ManaOnHit},
        {StatTypes.LifePercent,StatTypes.ArmourPercent,StatTypes.ManaOnHit,StatTypes.BleedChance,StatTypes.ChanceToHitTwice,StatTypes.CooldownReduction},
        {StatTypes.AllRes,StatTypes.LifeOnHit,StatTypes.CritChance,StatTypes.BleedDmg,StatTypes.AttackSpeed,StatTypes.StrengthPercent},
        {StatTypes.LifePercent,StatTypes.AllRes,StatTypes.CooldownReduction,StatTypes.BleedChance,StatTypes.BleedDmg,StatTypes.CritChance},
        {StatTypes.ArmourPercent,StatTypes.LifeOnHit,StatTypes.ManaOnHit,StatTypes.ChanceToHitTwice,StatTypes.AttackSpeed,StatTypes.StrengthPercent}};
    public static readonly Dictionary<StatTypes,string> Icons=new(){
        {StatTypes.LifePercent,"Life"},{StatTypes.ArmourPercent,"Armour"},{StatTypes.AllRes,"AllElementalResistance"},{StatTypes.LifeOnHit,"LifeOnHit"},
        {StatTypes.BleedChance,"BleedChance"},{StatTypes.ChanceToHitTwice,"Multistrike"},{StatTypes.BleedDmg,"BleedDamage"},{StatTypes.AttackSpeed,"AttackSpeed"},
        {StatTypes.CritChance,"CriticalStrikeChance"},{StatTypes.ManaOnHit,"ManaOnHit"},{StatTypes.CooldownReduction,"CooldownReduction"},{StatTypes.StrengthPercent,"IncreasedStrengthPercent"}};
    public static float Value(ModDatabase db,StatTypes stat)
    {
        var tier=db.GetDefinition(stat)?.tiers?.FirstOrDefault(x=>x.tierIndex==1);
        if(tier==null){if(stat==StatTypes.CooldownReduction)return 10;throw new InvalidOperationException("Missing production T1: "+stat);}
        return (float)(Math.Round((tier.minValue+tier.maxValue)/4*2,MidpointRounding.AwayFromZero)/2);
    }
    public static string Name(StatTypes stat)=>stat switch {StatTypes.LifePercent=>"Maximum Life",StatTypes.ArmourPercent=>"Increased Armour",StatTypes.AllRes=>"All Elemental Resistance",StatTypes.BleedDmg=>"Increased Bleed Damage",StatTypes.StrengthPercent=>"Increased Strength",_=>StatDisplayFormatting.ToFriendlyName(stat)};
    public static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+"Warrior_"+name+".png");
    static PassiveBranch Branch(StatTypes stat)=>stat switch {StatTypes.LifePercent=>PassiveBranch.Life,StatTypes.ArmourPercent=>PassiveBranch.Defense,StatTypes.AllRes=>PassiveBranch.Resistances,StatTypes.LifeOnHit=>PassiveBranch.LifeOnHit,StatTypes.BleedChance=>PassiveBranch.BleedChance,StatTypes.ChanceToHitTwice=>PassiveBranch.ChanceToHitTwice,StatTypes.BleedDmg=>PassiveBranch.BleedDamage,StatTypes.AttackSpeed=>PassiveBranch.AttackSpeed,StatTypes.CritChance=>PassiveBranch.CriticalChance,StatTypes.ManaOnHit=>PassiveBranch.ManaOnHit,StatTypes.CooldownReduction=>PassiveBranch.CooldownReduction,_=>PassiveBranch.Strength};

    [MenuItem("Black-Cube/Passive Tree Authoring/Apply Warrior Reauthoring")]
    public static void Apply()
    {
        foreach(string name in Icons.Values.Concat(new[]{"PlayerHub","ClassBadge"}))if(Sprite(name)==null)throw new InvalidOperationException("Missing imported art: "+name);
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");var branch=AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(BranchPath);Undo.RecordObject(branch,"Reauthor Warrior generic passives");
        for(int i=0;i<10;i++)
        {
            var tier=branch.Tiers[i];tier.Spine.Configure(tier.Spine.StableId,"Strength","+10 flat Strength.",PassiveBranch.Strength,PassiveNodeSize.Medium,PassiveNodeKind.Spine,PassiveKeystone.None,new[]{new PassiveEffect(StatTypes.Strength,10)},tier.Spine.LogicalSlotId);tier.Spine.SetIcon(null);
            var nodes=tier.Left.GenericNodes.Concat(tier.Right.GenericNodes).ToArray();for(int n=0;n<6;n++){var node=nodes[n];StatTypes stat=Choices[i,n];node.Configure(node.StableId,Name(stat),stat==StatTypes.AllRes?"Fire, Cold and Lightning resistance only. Does not include Void.":string.Empty,Branch(stat),PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,new[]{new PassiveEffect(stat,Value(db,stat))},node.LogicalSlotId);node.SetIcon(Sprite(Icons[stat]));}
        }
        EditorUtility.SetDirty(branch);AssetDatabase.SaveAssetIfDirty(branch);
        var layout=AssetDatabase.LoadAssetAtPath<PassiveTreeLayoutSO>(LayoutPath);if(layout==null){layout=ScriptableObject.CreateInstance<PassiveTreeLayoutSO>();AssetDatabase.CreateAsset(layout,LayoutPath);layout.layouts=new(){new PassiveLayoutSlots{count=1,anchors=new[]{Vector2.zero},rotations=new[]{0f}}};}
        layout.playerHub=Sprite("PlayerHub");layout.badges=new(){new PassiveClassBadge{classId=PlayerClassIds.Warrior,sprite=Sprite("ClassBadge")}};EditorUtility.SetDirty(layout);AssetDatabase.SaveAssetIfDirty(layout);
        IntegratePrefab(layout);PassiveTreeV3Validation.RunStructure();WriteAudit(db);Debug.Log("WARRIOR REAUTHORING: applied; subclasses, other class values and weapon values preserved.");
    }
    static void IntegratePrefab(PassiveTreeLayoutSO layout)
    {
        string path=PassiveTreePrefabBuilder.PrefabPath;var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view=root.GetComponent<PassiveTreeView>();view.layout=layout;view.playerHub=view.content.Find("Central Hub") as RectTransform;if(view.playerHub==null)view.playerHub=Rect("Player Hub",view.content,typeof(Image));
            var hub=view.playerHub.GetComponent<Image>();hub.sprite=layout.playerHub;hub.color=Color.white;hub.preserveAspect=true;hub.raycastTarget=false;view.playerHub.sizeDelta=Vector2.one*layout.hubSize;
            if(view.classBadges.Count==0){var rect=Rect("Class Badge",view.content,typeof(Image));var image=rect.GetComponent<Image>();image.raycastTarget=false;Text(rect,"Class",Vector2.zero,Vector2.one);view.classBadges.Add(image);}
            if(view.choicePopup==null)BuildPopup(root.transform,view);
            var warrior=view.branches.First(x=>x.RouteId==PlayerClassIds.Warrior);float[] widths={340,260,210,190,175,160,145,130,110,85};
            if(warrior.Tiers.Any(x=>x.leftSlot==null))foreach(var line in view.connections.ToArray())if(line!=null&&line.transform.IsChildOf(warrior.transform)&&!line.ToSlotId.EndsWith(".spine",StringComparison.Ordinal)){view.connections.Remove(line);UnityEngine.Object.DestroyImmediate(line.gameObject);}
            for(int i=0;i<10;i++)
            {
                var tier=warrior.Tiers[i];
                if(tier.leftSlot!=null)
                {
                    foreach(var slot in new[]{tier.leftSlot,tier.rightSlot}){EmptyGlow(slot);if(!view.connections.Any(x=>x!=null&&x.To==slot.transform))view.connections.Add(Line(warrior.transform,(RectTransform)tier.spine.transform,(RectTransform)slot.transform,tier.spine.LogicalSlotId,slot.choiceGroupId+".a"));}
                    continue;
                }
                float y=300+i*180;((RectTransform)tier.spine.transform).anchoredPosition=new Vector2(0,y);
                RemoveGroup(tier.left);RemoveGroup(tier.right);tier.left=new();tier.right=new();
                string prefix=$"tree.v3.class.warrior.t{i+1:00}.";
                tier.leftSlot=Slot(warrior.transform,prefix+"left",new Vector2(-widths[i],y-30),layout);tier.rightSlot=Slot(warrior.transform,prefix+"right",new Vector2(widths[i],y-30),layout);
                view.connections.Add(Line(warrior.transform,(RectTransform)tier.spine.transform,(RectTransform)tier.leftSlot.transform,tier.spine.LogicalSlotId,prefix+"left.a"));
                view.connections.Add(Line(warrior.transform,(RectTransform)tier.spine.transform,(RectTransform)tier.rightSlot.transform,tier.spine.LogicalSlotId,prefix+"right.a"));
            }
            foreach(var b in view.branches)b.RefreshAuthoringPreview();
            string marker="Hub To Warrior";if(warrior.transform.Find(marker)==null){var line=Line(warrior.transform,(RectTransform)view.classBadges[0].transform,(RectTransform)warrior.Tiers[0].spine.transform,warrior.Tiers[0].spine.LogicalSlotId,warrior.Tiers[0].spine.LogicalSlotId);line.name=marker;view.connections.Add(line);}
            var sword=view.branches.First(x=>x.RouteId==WeaponTypeIds.Sword);if(sword.transform.Find("Warrior To Sword")==null){var line=Line(sword.transform,(RectTransform)warrior.Tiers[9].spine.transform,(RectTransform)sword.Tiers[0].spine.transform,warrior.Tiers[9].spine.LogicalSlotId,sword.Tiers[0].spine.LogicalSlotId);line.name="Warrior To Sword";view.connections.Add(line);}
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void RemoveGroup(PassiveChoiceGroupBinding group){foreach(var node in group.Nodes.ToArray())if(node!=null&&group.nativeLayoutRoot==null&&group.genericLayoutRoot==null)UnityEngine.Object.DestroyImmediate(node.gameObject);if(group.nativeLayoutRoot!=null)UnityEngine.Object.DestroyImmediate(group.nativeLayoutRoot);if(group.genericLayoutRoot!=null)UnityEngine.Object.DestroyImmediate(group.genericLayoutRoot);if(group.junction!=null)UnityEngine.Object.DestroyImmediate(group.junction.gameObject);}
    static PassiveChoiceSlotView Slot(Transform parent,string group,Vector2 position,PassiveTreeLayoutSO layout)
    {
        var rect=Rect(group+" Slot",parent,typeof(Image),typeof(Button),typeof(PassiveChoiceSlotView),typeof(PassiveChoiceSlotInput));rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*88;
        var slot=rect.GetComponent<PassiveChoiceSlotView>();slot.choiceGroupId=group;slot.button=rect.GetComponent<Button>();slot.icon=rect.GetComponent<Image>();slot.icon.preserveAspect=true;slot.button.transition=Selectable.Transition.None;slot.label=Text(rect,"CHOOSE",new Vector2(-.5f,-.35f),new Vector2(1.5f,.02f));slot.label.fontSize=12;EmptyGlow(slot);return slot;
    }
    static void EmptyGlow(PassiveChoiceSlotView slot)
    {
        slot.emptyIcon=null;slot.icon.enabled=false;
        if(slot.emptyGlow!=null){if(slot.emptyGlow.GetComponent<CanvasRenderer>()==null)slot.emptyGlow.gameObject.AddComponent<CanvasRenderer>();return;}var rect=Rect("Empty Glow",slot.transform,typeof(CanvasRenderer),typeof(EmptyPassiveSlotGraphic));rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.SetAsFirstSibling();slot.emptyGlow=rect.GetComponent<EmptyPassiveSlotGraphic>();slot.emptyGlow.raycastTarget=true;
    }
    static void BuildPopup(Transform parent,PassiveTreeView view)
    {
        var rect=Rect("Passive Choice Popup",parent,typeof(Image),typeof(PassiveChoicePopupView));rect.anchorMin=new Vector2(.25f,.2f);rect.anchorMax=new Vector2(.75f,.8f);rect.offsetMin=rect.offsetMax=Vector2.zero;rect.GetComponent<Image>().color=new Color(.025f,.035f,.05f,.99f);
        var popup=rect.GetComponent<PassiveChoicePopupView>();popup.title=Text(rect,"CHOOSE ONE PASSIVE",new Vector2(.04f,.88f),new Vector2(.96f,.99f));
        for(int i=0;i<4;i++){float top=.86f-i*.175f;var button=Button(rect,"Option "+i,"PASSIVE",new Vector2(.04f,top-.16f),new Vector2(.96f,top));var label=button.GetComponentInChildren<TMP_Text>();label.rectTransform.anchorMin=new Vector2(.18f,0);label.fontSize=16;popup.options.Add(button);var icon=Rect("Icon",button.transform,typeof(Image));icon.anchorMin=new Vector2(.02f,.1f);icon.anchorMax=new Vector2(.16f,.9f);icon.offsetMin=icon.offsetMax=Vector2.zero;icon.GetComponent<Image>().raycastTarget=false;popup.icons.Add(icon.GetComponent<Image>());}
        popup.close=Button(rect,"Close","CLOSE",new Vector2(.1f,.02f),new Vector2(.9f,.12f));view.choicePopup=popup;rect.gameObject.SetActive(false);
    }
    static PassiveConnectionBinding Line(Transform parent,RectTransform from,RectTransform to,string fromId,string visibleId){var r=Rect("Connection",parent,typeof(Image),typeof(PassiveConnectionBinding));r.GetComponent<Image>().raycastTarget=false;r.SetAsFirstSibling();var line=r.GetComponent<PassiveConnectionBinding>();line.Configure(from,to,r,fromId,visibleId,visibleId);return line;}
    static RectTransform Rect(string name,Transform parent,params Type[] components){var types=new List<Type>{typeof(RectTransform)};types.AddRange(components);var go=new GameObject(name,types.ToArray());go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;return r;}
    static TMP_Text Text(Transform parent,string value,Vector2 min,Vector2 max){var r=Rect("Label",parent,typeof(TextMeshProUGUI));r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var t=r.GetComponent<TMP_Text>();t.text=value;t.fontSize=18;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static Button Button(Transform parent,string name,string label,Vector2 min,Vector2 max){var r=Rect(name,parent,typeof(Image),typeof(Button));r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;r.GetComponent<Image>().color=new Color(.12f,.16f,.22f);Text(r,label,Vector2.zero,Vector2.one);return r.GetComponent<Button>();}
    static void WriteAudit(ModDatabase db)
    {
        var lines=new List<string>{"# Warrior T1 value audit","","Maximum Life uses percentage-Life, not flat Life. CDR has no production gear tier; 10% is a provisional exception. No per-tier inflation.","","| Stat | T1 range | Midpoint | Half midpoint | Final |","|---|---|---|---|---|"};
        foreach(var stat in Icons.Keys){var t=db.GetDefinition(stat)?.tiers.FirstOrDefault(x=>x.tierIndex==1);lines.Add(t==null?$"| {Name(stat)} | Not available | — | — | {Value(db,stat)} |":$"| {Name(stat)} | {t.minValue}–{t.maxValue} | {(t.minValue+t.maxValue)/2} | {(t.minValue+t.maxValue)/4} | {Value(db,stat)} |");}
        Directory.CreateDirectory("Logs/WarriorReauthoring");File.WriteAllLines("Logs/WarriorReauthoring/T1Audit.md",lines);
    }
}
