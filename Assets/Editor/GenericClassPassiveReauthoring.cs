using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GenericClassPassiveReauthoring
{
    public const string BranchFolder="Assets/GameData/PassiveTree/Branches/Class/";
    public static readonly Dictionary<string,string[]> Rotations=new()
    {
        [PlayerClassIds.Barbarian]=Rows("Life Regen AllRes RageGen Heavy Decay;Revenge LifeKill Bleed RageEffect Heavy Fire;Life Revenge Decay RageGen RageEffect Str;Regen LifeKill Fire Heavy Decay Bleed;Life LifeKill AllRes RageGen Heavy RageEffect;Regen Revenge Bleed RageEffect Decay Fire;Life Regen Str RageGen Decay AllRes;Revenge LifeKill Fire Heavy RageEffect Bleed;Life Revenge AllRes RageGen Heavy Decay;Regen LifeKill Str RageEffect Decay Fire"),
        [PlayerClassIds.Ranger]=Rows("Life Leech ManaHit Precision ProjectileSpeed Speed;ReducedShock ReducedChill AllRes Poison Dex RageGen;Life ReducedShock Speed Precision Poison ManaHit;Leech ReducedChill RageGen ProjectileSpeed Dex AllRes;Life ReducedChill ManaHit Precision Dex Speed;Leech ReducedShock AllRes ProjectileSpeed Poison RageGen;Life Leech Speed Precision ProjectileSpeed ManaHit;ReducedShock ReducedChill RageGen Poison Dex AllRes;Life ReducedShock ManaHit Precision Poison Speed;Leech ReducedChill AllRes ProjectileSpeed Dex RageGen"),
        [PlayerClassIds.Mage]=Rows("Mana ManaDefense Int Fire CDR Shock;ReducedShock ReducedChill AllRes Echo Lightning ManaRegen;Mana ReducedShock Shock Fire Echo Int;ManaRegen ReducedChill ManaDefense Lightning CDR AllRes;Mana ReducedChill Int Fire Lightning Shock;ManaRegen ReducedShock AllRes Echo CDR ManaDefense;Mana ManaRegen Shock Fire CDR Int;ReducedShock ReducedChill ManaDefense Echo Lightning AllRes;Mana ReducedShock Int Fire Echo Shock;ManaRegen ReducedChill AllRes Lightning CDR ManaDefense"),
        [PlayerClassIds.Priest]=Rows("Life Recovery ManaRegen Aura Void CDR;VoidRes MaxRes AllRes Ailments Decay LifeKill;Life Recovery CDR Aura Void ManaRegen;VoidRes MaxRes LifeKill Ailments Decay AllRes;Life MaxRes ManaRegen Aura Decay CDR;Recovery VoidRes AllRes Void Ailments LifeKill;Life Recovery CDR Aura Void ManaRegen;VoidRes MaxRes LifeKill Ailments Decay AllRes;Life VoidRes ManaRegen Aura Ailments CDR;Recovery MaxRes AllRes Void Decay LifeKill"),
        [PlayerClassIds.Thief]=Rows("LifeMana LifeKill Speed Crit PhysVoid CDR;ArmourRes ManaKill PoisonBleedDamage CritMulti PoisonBleedChance Multistrike;LifeMana ArmourRes CDR Crit CritMulti Speed;LifeKill ManaKill Multistrike PhysVoid PoisonBleedChance PoisonBleedDamage;LifeMana ManaKill Speed Crit PhysVoid CDR;ArmourRes LifeKill PoisonBleedDamage CritMulti PoisonBleedChance Multistrike;LifeMana ArmourRes CDR Crit CritMulti Speed;LifeKill ManaKill Multistrike PhysVoid PoisonBleedChance PoisonBleedDamage;LifeMana LifeKill Speed Crit PhysVoid CDR;ArmourRes ManaKill PoisonBleedDamage CritMulti PoisonBleedChance Multistrike")
    };
    static string[] Rows(string text)=>text.Split(';');
    static readonly Dictionary<string,StatTypes[]> Stats=new()
    {
        ["ManaDefense"]=new[]{StatTypes.DamageTakenFromManaBeforeLife,StatTypes.LifeRegeneration},
        ["Life"]=new[]{StatTypes.LifePercent},["Mana"]=new[]{StatTypes.ManaPercent},["Regen"]=new[]{StatTypes.LifeRegeneration},["ManaRegen"]=new[]{StatTypes.ManaRegeneration},
        ["AllRes"]=new[]{StatTypes.AllRes},["LifeKill"]=new[]{StatTypes.LifeOnKill},["ManaKill"]=new[]{StatTypes.ManaOnKill},["ManaHit"]=new[]{StatTypes.ManaOnHit},
        ["RageGen"]=new[]{StatTypes.RageGeneration},["RageEffect"]=new[]{StatTypes.RageEffect},["Decay"]=new[]{StatTypes.RageDecayReduction},["Revenge"]=new[]{StatTypes.RevengeEffect},
        ["Fire"]=new[]{StatTypes.FireDmg},["Cold"]=new[]{StatTypes.ColdDmg},["Echo"]=new[]{StatTypes.SpellEchoChance},["Lightning"]=new[]{StatTypes.LightDmg},["Void"]=new[]{StatTypes.VoidDmg},["VoidRes"]=new[]{StatTypes.VoidRes},
        ["Bleed"]=new[]{StatTypes.BleedChance},["Poison"]=new[]{StatTypes.PoisonChance},["Shock"]=new[]{StatTypes.ShockChance},["Str"]=new[]{StatTypes.StrengthPercent},["Dex"]=new[]{StatTypes.DexterityPercent},["Int"]=new[]{StatTypes.IntelligencePercent},
        ["Crit"]=new[]{StatTypes.CritChance},["CritMulti"]=new[]{StatTypes.CritMult},["CDR"]=new[]{StatTypes.CooldownReduction},["Speed"]=new[]{StatTypes.AttackSpeed},["Multistrike"]=new[]{StatTypes.ChanceToHitTwice},
        ["Leech"]=new[]{StatTypes.PoisonLifeLeech},["Precision"]=new[]{StatTypes.ProjectilePrecisionChance},["ProjectileSpeed"]=new[]{StatTypes.ProjectileSpeed},
        ["ReducedShock"]=new[]{StatTypes.ReducedShockEffect},["ReducedChill"]=new[]{StatTypes.ReducedChillEffect},["Recovery"]=new[]{StatTypes.LifeRecoveryEffect},["MaxRes"]=new[]{StatTypes.MaxAllRes},
        ["Aura"]=new[]{StatTypes.AuraEffect},["Ailments"]=new[]{StatTypes.AllDamagingAilmentChance},["Heavy"]=new[]{StatTypes.PhysDmg,StatTypes.AttackSpeed},
        ["LifeMana"]=new[]{StatTypes.LifePercent,StatTypes.ManaPercent},["ArmourRes"]=new[]{StatTypes.ArmourPercent,StatTypes.AllRes},["PhysVoid"]=new[]{StatTypes.PhysDmg,StatTypes.VoidDmg},
        ["PoisonBleedChance"]=new[]{StatTypes.PoisonChance,StatTypes.BleedChance},["PoisonBleedDamage"]=new[]{StatTypes.PoisonDmg,StatTypes.BleedDmg}
    };
    static readonly Dictionary<StatTypes,float> Special=new()
    {
        [StatTypes.SpellEchoChance]=2,[StatTypes.ReducedShockEffect]=8,[StatTypes.ReducedChillEffect]=8,
        [StatTypes.RageGeneration]=15,[StatTypes.RageEffect]=10,[StatTypes.RageDecayReduction]=5,[StatTypes.RevengeEffect]=10,[StatTypes.PoisonLifeLeech]=2,
        [StatTypes.ProjectilePrecisionChance]=8,[StatTypes.ProjectileSpeed]=15,[StatTypes.LifeRecoveryEffect]=10,[StatTypes.MaxAllRes]=1,[StatTypes.AuraEffect]=10,[StatTypes.AllDamagingAilmentChance]=10,[StatTypes.CooldownReduction]=10
    };
    public static PassiveClassBranchSO Branch(string id)=>AssetDatabase.LoadAssetAtPath<PassiveClassBranchSO>(BranchFolder+"SO_"+id.Replace("class.","").Substring(0,1).ToUpperInvariant()+id.Replace("class.","").Substring(1)+"_Branch.asset");
    public static float Round(float value)=>(float)Math.Round(value*2,MidpointRounding.AwayFromZero)/2;
    public static float Value(ModDatabase db,StatTypes stat,float factor=.5f)
    {
        if(Special.TryGetValue(stat,out float custom))return custom;
        var tier=db.GetDefinition(stat)?.tiers.FirstOrDefault(x=>x.tierIndex==1);
        if(tier==null)throw new InvalidOperationException("No T1 source or explicit custom value for "+stat);
        float value=(tier.minValue+tier.maxValue)*.5f*factor;
        return stat==StatTypes.LifeRegeneration?Round(value*100f)*.01f:
            Round(value)*(stat==StatTypes.LifeOnHit?.1f:stat==StatTypes.ManaOnHit?.15f:1);
    }
    public static PassiveEffect[] Effects(ModDatabase db,string key)
    {
        if(key=="ManaDefense")return new[]{new PassiveEffect(StatTypes.DamageTakenFromManaBeforeLife,7),new PassiveEffect(StatTypes.LifeRegeneration,.75f)};
        if(key=="Heavy")return new[]{new PassiveEffect(StatTypes.PhysDmg,Value(db,StatTypes.PhysDmg,.75f)),new PassiveEffect(StatTypes.AttackSpeed,-Value(db,StatTypes.AttackSpeed,.25f))};
        var stats=Stats[key];return stats.Select(s=>new PassiveEffect(s,Value(db,s,stats.Length>1?.30f:.50f))).ToArray();
    }
    public static PassiveEffect[] Spine(string id)=>id switch
    {
        PlayerClassIds.Barbarian=>new[]{new PassiveEffect(StatTypes.Strength,10)},PlayerClassIds.Ranger=>new[]{new PassiveEffect(StatTypes.Dexterity,10)},PlayerClassIds.Mage=>new[]{new PassiveEffect(StatTypes.Intelligence,10)},
        PlayerClassIds.Priest=>new[]{new PassiveEffect(StatTypes.Strength,5),new PassiveEffect(StatTypes.Intelligence,5)},_=>new[]{new PassiveEffect(StatTypes.Dexterity,5),new PassiveEffect(StatTypes.Intelligence,5)}
    };
    public static string Description(string key)=>key switch
    {
        "ManaDefense"=>ManaBeforeLifeRules.Description+" Also regenerates 0.75% Maximum Life per second.",
        "Revenge"=>"Unlocks Revenge. Most recent hit's actual Life loss / Maximum Life empowers the next attack event: 1 + 2 × fraction × (1 + increased Revenge Effect). Consumed once; damage over time does not charge it.",
        "Leech"=>"Recover Life from actual mitigated Poison tick damage dealt. Not Life on Hit; overkill does not grant extra leech.",
        "Heavy"=>"Heavy physical hits at the cost of attack frequency. Increased Physical Damage with reduced Attack Speed.",
        "Aura"=>"Increases each active aura's matching-type damage and secondary effects. Does not grant aura access or permanent bonuses.",
        "Ailments"=>"Adds Bleed, Ignite and Poison chance only. Does not include Shock or Chill.",
        "MaxRes"=>"Maximum Fire, Cold and Lightning resistance. Does not include Void; the production hard cap still applies.",
        "AllRes"=>"Fire, Cold and Lightning resistance only; excludes Void.",
        "Multistrike"=>"Melee weapons only: Sword, Two-Handed Axe, Dagger and Sceptre. No Bow or Staff repeat hits.",
        "Recovery"=>"Increases Life recovery, including regeneration, hit/kill recovery and poison leech. Does not scale Mana recovery.",
        _=>Stats[key].Length>1?"Hybrid: each component uses 30% of its T1 midpoint, not two full single-stat passives.":string.Empty
    };
    static string Name(string key)=>key switch {"ManaDefense"=>"Mana Ward and Recovery","Heavy"=>"Heavy Physical Damage","LifeMana"=>"Life and Mana","ArmourRes"=>"Armour and Elemental Resistance","PhysVoid"=>"Physical and Void Damage","PoisonBleedChance"=>"Poison and Bleed Chance","PoisonBleedDamage"=>"Poison and Bleed Damage",_=>StatDisplayFormatting.ToFriendlyName(Stats[key][0])};
    static PassiveBranch Theme(StatTypes stat)=>stat switch {StatTypes.Strength or StatTypes.StrengthPercent=>PassiveBranch.Strength,StatTypes.Dexterity or StatTypes.DexterityPercent=>PassiveBranch.Dexterity,StatTypes.Intelligence or StatTypes.IntelligencePercent=>PassiveBranch.Intelligence,StatTypes.LifePercent=>PassiveBranch.Life,StatTypes.ManaPercent=>PassiveBranch.Mana,StatTypes.FireDmg=>PassiveBranch.Fire,StatTypes.ColdDmg=>PassiveBranch.Cold,StatTypes.LightDmg=>PassiveBranch.Lightning,StatTypes.VoidDmg=>PassiveBranch.Poison,StatTypes.CritChance=>PassiveBranch.CriticalChance,StatTypes.CritMult=>PassiveBranch.CriticalMultiplier,_=>PassiveBranch.Defense};
    static StatTypes IconStat(StatTypes stat)=>stat switch {StatTypes.RevengeEffect=>StatTypes.PhysDmg,StatTypes.PoisonLifeLeech=>StatTypes.PoisonChance,StatTypes.LifeRecoveryEffect=>StatTypes.LifeRegeneration,StatTypes.AllDamagingAilmentChance=>StatTypes.BleedChance,StatTypes.GrantsPhysicalAura=>StatTypes.PhysDmg,_=>stat};

    [MenuItem("Black-Cube/Passive Tree Authoring/Apply Five Generic Class Reworks")]
    public static void Apply()
    {
        Directory.CreateDirectory("Logs/GenericClassRework/Baseline");
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        foreach(var stat in Stats.Values.SelectMany(x=>x).Distinct())Value(db,stat);
        var library=PassiveTreeDefinition.Database.IconLibrary;
        var mappings=library.Mappings.ToList();var crit=WarriorPassiveReauthoring.Sprite("CriticalStrikeChance");
        foreach(var map in mappings.Where(x=>x.Effect is StatTypes.CritChance or StatTypes.CritMult)){map.Configure(map.Effect,crit,crit,crit,crit);}
        library.Configure(library.GenericFallback,mappings);EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
        foreach(var pair in Rotations)
        {
            var branch=Branch(pair.Key);string baseline="Logs/GenericClassRework/Baseline/"+branch.name+".asset";
            if(!File.Exists(baseline))File.Copy(AssetDatabase.GetAssetPath(branch),baseline);
            for(int t=0;t<10;t++)
            {
                var tier=branch.Tiers[t];var spine=Spine(pair.Key);tier.Spine.Configure(tier.Spine.StableId,string.Join(" + ",spine.Select(x=>StatDisplayFormatting.ToFriendlyName(x.Stat))),"Uniform class attribute travel.",Theme(spine[0].Stat),PassiveNodeSize.Medium,PassiveNodeKind.Spine,PassiveKeystone.None,spine,tier.Spine.LogicalSlotId);tier.Spine.SetIcon(null);
                var choices=tier.Left.GenericNodes.Concat(tier.Right.GenericNodes).ToArray();var keys=pair.Value[t].Split(' ');
                for(int i=0;i<6;i++){var node=choices[i];var effects=Effects(db,keys[i]);node.Configure(node.StableId,Name(keys[i]),Description(keys[i]),Theme(effects[0].Stat),PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,effects,node.LogicalSlotId);var theme=new PassiveAuthoredNode();theme.Configure("icon","icon","",Theme(effects[0].Stat),PassiveNodeSize.Small,PassiveNodeKind.Choice,PassiveKeystone.None,new[]{new PassiveEffect(IconStat(effects[0].Stat),0)});node.SetIcon(library.Resolve(theme));}
            }
            EditorUtility.SetDirty(branch);AssetDatabase.SaveAssetIfDirty(branch);
        }
        AddAmuletAuraDefinitions(db);ConvertPrefab();WriteReport(db);PassiveTreeV3Validation.RunStructure();
        Debug.Log("GENERIC CLASS REWORK: five class data sets converted; subclass/weapon numeric data untouched.");
    }

    static void AddAmuletAuraDefinitions(ModDatabase db)
    {
        var serialized=new SerializedObject(db);var list=serialized.FindProperty("allAffixes");
        for(int i=0;i<5;i++)
        {
            int id=(int)GenericPassiveMechanics.AuraGrant(i);SerializedProperty entry=null;
            for(int n=0;n<list.arraySize;n++){var candidate=list.GetArrayElementAtIndex(n);if(candidate.FindPropertyRelative("statType").intValue==id){entry=candidate;break;}}
            if(entry==null){list.InsertArrayElementAtIndex(list.arraySize);entry=list.GetArrayElementAtIndex(list.arraySize-1);}
            entry.FindPropertyRelative("statType").intValue=id;entry.FindPropertyRelative("displayName").stringValue=StatDisplayFormatting.ToFriendlyName((StatTypes)id);
            entry.FindPropertyRelative("side").enumValueIndex=(int)AffixSide.Prefix;entry.FindPropertyRelative("empowerable").boolValue=false;entry.FindPropertyRelative("hasCustomEmpoweredRange").boolValue=false;
            var slots=entry.FindPropertyRelative("allowedSlots");slots.arraySize=1;slots.GetArrayElementAtIndex(0).enumValueIndex=(int)LootManager.GearType.Amulets;
            entry.FindPropertyRelative("allowedWeaponTypeIds").arraySize=0;var groups=entry.FindPropertyRelative("groups");groups.arraySize=1;groups.GetArrayElementAtIndex(0).stringValue="grant.aura."+i;
            var tiers=entry.FindPropertyRelative("tiers");tiers.arraySize=1;var tier=tiers.GetArrayElementAtIndex(0);tier.FindPropertyRelative("tierIndex").intValue=1;tier.FindPropertyRelative("minValue").floatValue=tier.FindPropertyRelative("maxValue").floatValue=1;tier.FindPropertyRelative("minItemLevel").intValue=70;tier.FindPropertyRelative("weight").intValue=5;tier.FindPropertyRelative("pairedDamage").boolValue=false;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(db);
    }
    static void ConvertPrefab()
    {
        var root=PrefabUtility.LoadPrefabContents(PassiveTreePrefabBuilder.PrefabPath);
        try
        {
            var view=root.GetComponent<PassiveTreeView>();
            foreach(string id in Rotations.Keys)
            {
                var branch=view.branches.First(x=>x.RouteId==id);
                if(branch.Tiers.Any(x=>x.leftSlot==null))foreach(var line in view.connections.ToArray())if(line!=null&&line.transform.IsChildOf(branch.transform)&&!line.ToSlotId.EndsWith(".spine",StringComparison.Ordinal)){view.connections.Remove(line);UnityEngine.Object.DestroyImmediate(line.gameObject);}
                for(int t=0;t<10;t++)
                {
                    var tier=branch.Tiers[t];if(tier.leftSlot!=null)continue;
                    float y=300+t*180;((RectTransform)tier.spine.transform).anchoredPosition=new Vector2(0,y);
                    Remove(tier.left);Remove(tier.right);tier.left=new();tier.right=new();
                    string prefix=$"tree.v3.{id}.t{t+1:00}.";
                    tier.leftSlot=Slot(branch.transform,prefix+"left",new Vector2(-240,y-25));tier.rightSlot=Slot(branch.transform,prefix+"right",new Vector2(240,y-25));
                    view.connections.Add(Line(branch.transform,(RectTransform)tier.spine.transform,(RectTransform)tier.leftSlot.transform,tier.spine.LogicalSlotId,prefix+"left.a"));view.connections.Add(Line(branch.transform,(RectTransform)tier.spine.transform,(RectTransform)tier.rightSlot.transform,tier.spine.LogicalSlotId,prefix+"right.a"));
                }
                if(branch.transform.Find("Hub To Class")==null){var line=Line(branch.transform,(RectTransform)view.classBadges[0].transform,(RectTransform)branch.Tiers[0].spine.transform,branch.Tiers[0].spine.LogicalSlotId,branch.Tiers[0].spine.LogicalSlotId);line.name="Hub To Class";view.connections.Add(line);}
                var data=(PassiveClassBranchSO)branch.Data;var weapon=view.branches.First(x=>x.RouteId==data.SignatureWeaponId);
                if(weapon.transform.Find("Class To Weapon")==null){var line=Line(weapon.transform,(RectTransform)branch.Tiers[9].spine.transform,(RectTransform)weapon.Tiers[0].spine.transform,branch.Tiers[9].spine.LogicalSlotId,weapon.Tiers[0].spine.LogicalSlotId);line.name="Class To Weapon";view.connections.Add(line);}
            }
            foreach(var node in root.GetComponentsInChildren<PassiveNodeBinding>(true))
            {
                var authored=PassiveTreeDefinition.AuthoredNode(PassiveTreeDefinition.Node(PassiveTreeDefinition.NodeId(node.LogicalSlotId)));
                if(authored!=null&&(Rotations.Keys.Any(id=>node.LogicalSlotId.StartsWith("tree.v3."+id+".",StringComparison.Ordinal))||authored.Effects.Any(x=>x.Stat is StatTypes.CritChance or StatTypes.CritMult))){var icon=PassiveCircularIconAuthoring.Wrap(node.Icon);var binding=new SerializedObject(node);binding.FindProperty("icon").objectReferenceValue=icon;binding.ApplyModifiedPropertiesWithoutUndo();node.Button.targetGraphic=icon;}
            }
            foreach(var slot in root.GetComponentsInChildren<PassiveChoiceSlotView>(true)){slot.icon=PassiveCircularIconAuthoring.Wrap(slot.icon);slot.button.targetGraphic=slot.icon;}
            if(view.choicePopup!=null)for(int i=0;i<view.choicePopup.icons.Count;i++)view.choicePopup.icons[i]=PassiveCircularIconAuthoring.Wrap(view.choicePopup.icons[i]);
            foreach(var branch in view.branches)branch.RefreshAuthoringPreview();PrefabUtility.SaveAsPrefabAsset(root,PassiveTreePrefabBuilder.PrefabPath);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Remove(PassiveChoiceGroupBinding group){if(group.nativeLayoutRoot!=null)UnityEngine.Object.DestroyImmediate(group.nativeLayoutRoot);if(group.genericLayoutRoot!=null)UnityEngine.Object.DestroyImmediate(group.genericLayoutRoot);if(group.junction!=null)UnityEngine.Object.DestroyImmediate(group.junction.gameObject);}
    static RectTransform Rect(string name,Transform parent,params Type[] components){var types=new List<Type>{typeof(RectTransform)};types.AddRange(components);var go=new GameObject(name,types.ToArray());go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;return r;}
    static PassiveChoiceSlotView Slot(Transform parent,string group,Vector2 position)
    {
        var rect=Rect(group+" Slot",parent,typeof(Image),typeof(Button),typeof(PassiveChoiceSlotView),typeof(PassiveChoiceSlotInput));rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*88;
        var slot=rect.GetComponent<PassiveChoiceSlotView>();slot.choiceGroupId=group;slot.icon=rect.GetComponent<Image>();slot.icon.enabled=false;slot.button=rect.GetComponent<Button>();slot.button.transition=Selectable.Transition.None;
        var glow=Rect("Empty Glow",rect,typeof(CanvasRenderer),typeof(EmptyPassiveSlotGraphic));glow.anchorMin=Vector2.zero;glow.anchorMax=Vector2.one;glow.offsetMin=glow.offsetMax=Vector2.zero;slot.emptyGlow=glow.GetComponent<EmptyPassiveSlotGraphic>();
        var label=Rect("Label",rect,typeof(TextMeshProUGUI));label.anchorMin=new Vector2(-.5f,-.35f);label.anchorMax=new Vector2(1.5f,.02f);label.offsetMin=label.offsetMax=Vector2.zero;slot.label=label.GetComponent<TMP_Text>();slot.label.text="CHOOSE";slot.label.fontSize=12;slot.label.alignment=TextAlignmentOptions.Center;slot.label.raycastTarget=false;return slot;
    }
    static PassiveConnectionBinding Line(Transform parent,RectTransform from,RectTransform to,string fromId,string toId){var rect=Rect("Connection",parent,typeof(Image),typeof(PassiveConnectionBinding));rect.GetComponent<Image>().raycastTarget=false;rect.SetAsFirstSibling();var line=rect.GetComponent<PassiveConnectionBinding>();line.Configure(from,to,rect,fromId,toId,toId);return line;}
    static void WriteReport(ModDatabase db)
    {
        var text=new List<string>{"# Five generic class trees — actual authored values","","Values are stored percent points except flat regeneration/on-hit/on-kill and attributes. Hybrid components use 30% of the T1 midpoint; Heavy Physical uses 75% with a penalty of 25% of the Attack Speed midpoint.",""};
        foreach(var pair in Rotations){text.Add("## "+pair.Key);text.Add("");text.Add("| Tier | Spine | Left A | Left B | Left C | Right A | Right B | Right C |");text.Add("|---|---|---|---|---|---|---|---|");for(int t=0;t<10;t++){string EffectText(IEnumerable<PassiveAuthoredEffect> effects)=>string.Join(" + ",effects.Select(e=>$"{e.Value:+0.##;-0.##;0}{(StatsComponent.IsPercentStat(e.Stat)?"%":"")} {StatDisplayFormatting.ToFriendlyName(e.Stat)}"));var tier=Branch(pair.Key).Tiers[t];text.Add($"| {t+1} | {EffectText(tier.Spine.Effects)} | "+string.Join(" | ",tier.Left.GenericNodes.Concat(tier.Right.GenericNodes).Select(x=>EffectText(x.Effects)))+" |");}text.Add("");}
        text.Add("## Value audit");text.Add("");text.Add("| Stat | T1 range | Midpoint | Normal 50% target | Final single | Hybrid 30% |");text.Add("|---|---|---|---|---|---|");foreach(var stat in Stats.Values.SelectMany(x=>x).Distinct()){var tier=db.GetDefinition(stat)?.tiers.FirstOrDefault(x=>x.tierIndex==1);text.Add($"| {StatDisplayFormatting.ToFriendlyName(stat)} | {(tier!=null?$"{tier.minValue}–{tier.maxValue}":"none")} | {(tier!=null?((tier.minValue+tier.maxValue)/2).ToString():"—")} | {(tier!=null?((tier.minValue+tier.maxValue)/4).ToString():"—")} | {Value(db,stat)}{(Special.ContainsKey(stat)?" custom":"")} | {(tier!=null?Value(db,stat,.3f).ToString():"—")} |");}
        Directory.CreateDirectory("Docs");File.WriteAllLines("Docs/GENERIC_CLASS_PASSIVE_TABLES.md",text);
    }
}
