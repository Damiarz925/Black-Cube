using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor assembly only. Every persistent mutation requires its own explicit button.
public sealed class SystemsRedesignDevTools:EditorWindow
{
    LootManager.GearType slot;LootManager.GearRarity rarity=LootManager.GearRarity.Legendary;
    Element element;int weapon,level=100,uniqueIndex,rollMode=1,playerLevel=100,combatLevel=60,advance=10,relicLevel=100,rewardDepth=340;
    bool curatedRelic,crafted;LootManager.GearRarity relicRarity=LootManager.GearRarity.Legendary;
    readonly List<StatTypes> stats=new(){StatTypes.Life};readonly List<int> tiers=new(){1};
    readonly List<RelicModifierType> relicMods=new(){RelicModifierType.MoreDamage};
    readonly List<int> relicTiers=new(){1};Vector2 scroll;string feedback;
    static readonly string[] weaponIds={WeaponTypeIds.Sword,WeaponTypeIds.TwoHandedAxe,WeaponTypeIds.Bow,WeaponTypeIds.Staff,WeaponTypeIds.Sceptre,WeaponTypeIds.Dagger};
    static readonly string[] rolls={"MINIMUM","AVERAGE","MAXIMUM","RANDOM"};
    [MenuItem("Black-Cube/Development/Systems Redesign Test Tools")]
    static void Open()=>GetWindow<SystemsRedesignDevTools>("Systems Test Tools");
    float? FixedRoll=>rollMode==3?null:rollMode*.5f;
    float Roll()=>FixedRoll??UnityEngine.Random.value;
    void OnGUI()
    {
        scroll=EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("Play Mode only. Buttons deliberately modify the current character. Temporary overrides are not saved and reset when entering Play Mode. Nothing runs automatically.",MessageType.Warning);
        using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            GUILayout.Label("CURATED EQUIPMENT",EditorStyles.boldLabel);
            slot=(LootManager.GearType)EditorGUILayout.EnumPopup("Item type",slot);weapon=EditorGUILayout.Popup("Weapon type",weapon,weaponIds);element=(Element)EditorGUILayout.EnumPopup("Base element",element);rarity=(LootManager.GearRarity)EditorGUILayout.EnumPopup("Rarity",rarity);level=EditorGUILayout.IntSlider("Item level",level,1,100);rollMode=EditorGUILayout.Popup("Roll position",rollMode,rolls);
            GUILayout.Label("First row is the permanent implicit. Remaining rows are explicits.");
            for(int i=0;i<stats.Count;i++){EditorGUILayout.BeginHorizontal();stats[i]=(StatTypes)EditorGUILayout.EnumPopup(stats[i]);tiers[i]=EditorGUILayout.IntSlider(tiers[i],1,5);EditorGUILayout.EndHorizontal();}
            if(GUILayout.Button("ADD AFFIX ROW")&&stats.Count<7){stats.Add(StatTypes.FireRes);tiers.Add(1);}if(GUILayout.Button("REMOVE LAST AFFIX ROW")&&stats.Count>1){stats.RemoveAt(stats.Count-1);tiers.RemoveAt(tiers.Count-1);}
            if(GUILayout.Button("ADD CURATED ITEM TO INVENTORY"))Attempt(SpawnItem);
            uniqueIndex=EditorGUILayout.Popup("Unique identity",uniqueIndex,UniqueCatalog.All.Select(x=>x.name).ToArray());
            if(GUILayout.Button("ADD UNIQUE TO INVENTORY"))Attempt(()=>{var gear=UniqueCatalog.Create(UniqueCatalog.All[uniqueIndex].id,level,fixedRoll:FixedRoll);if(gear==null)throw new InvalidOperationException("Selected Unique requires item level 20 or higher.");Inventory.Instance.Add(gear);feedback="Added immutable "+gear.name;});
            GUILayout.Space(10);GUILayout.Label("PROGRESSION",EditorStyles.boldLabel);
            playerLevel=EditorGUILayout.IntSlider("Player level",playerLevel,1,100);
            if(GUILayout.Button("SET PLAYER LEVEL / CLEAR PASSIVES"))Attempt(()=>{var p=FindAnyObjectByType<PlayerProgression>();p.ResetProgression();if(!p.RestoreProgression(playerLevel,0,playerLevel,new int[PassiveTreeDefinition.NodeCount]))throw new InvalidOperationException("Progression rejected reset.");feedback="Player level set; passive points refunded.";});
            combatLevel=EditorGUILayout.IntField("Combat level",combatLevel);advance=EditorGUILayout.IntField("Advance X stages",advance);
            if(GUILayout.Button("SET COMBAT LEVEL"))Jump(combatLevel);if(GUILayout.Button("ADVANCE X STAGES"))Jump((GameManager.Instance?.CurrentCombatLevel??1)+Mathf.Max(1,advance));
            EditorGUILayout.BeginHorizontal();foreach(int milestone in new[]{6,60,100,340,350})if(GUILayout.Button("JUMP "+milestone))Jump(milestone);EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);GUILayout.Label("TEMPORARY SESSION OVERRIDES",EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();foreach(float multiplier in new[]{1f,5f,10f,100f})if(GUILayout.Button("XP ×"+multiplier))DevelopmentOverrides.experience=multiplier;EditorGUILayout.EndHorizontal();
            DevelopmentOverrides.enemyId=EditorGUILayout.TextField("Enemy ID scope (empty=all)",DevelopmentOverrides.enemyId);
            DevelopmentOverrides.gearDrops=EditorGUILayout.FloatField("Gear drop multiplier",DevelopmentOverrides.gearDrops);DevelopmentOverrides.currencyDrops=EditorGUILayout.FloatField("Currency drop multiplier",DevelopmentOverrides.currencyDrops);
            DevelopmentOverrides.enemyRarity=EditorGUILayout.FloatField("Enemy rarity increase (fraction)",DevelopmentOverrides.enemyRarity);DevelopmentOverrides.itemRarity=EditorGUILayout.FloatField("Item rarity increase (fraction)",DevelopmentOverrides.itemRarity);
            DevelopmentOverrides.restrictItemType=EditorGUILayout.Toggle("Scope item rarity to item type",DevelopmentOverrides.restrictItemType);DevelopmentOverrides.itemType=(LootManager.GearType)EditorGUILayout.EnumPopup("Item rarity scope",DevelopmentOverrides.itemType);
            if(GUILayout.Button("RESET ALL TEMPORARY OVERRIDES"))DevelopmentOverrides.Reset();
            GUILayout.Space(10);GUILayout.Label("RELIC TESTING",EditorStyles.boldLabel);
            relicLevel=EditorGUILayout.IntSlider("Relic level",relicLevel,1,100);relicRarity=(LootManager.GearRarity)EditorGUILayout.EnumPopup("Relic rarity",relicRarity);crafted=EditorGUILayout.Toggle("Mark Crafted (otherwise Pristine)",crafted);curatedRelic=EditorGUILayout.Toggle("Use selected modifiers",curatedRelic);
            for(int i=0;i<relicMods.Count;i++){EditorGUILayout.BeginHorizontal();relicMods[i]=(RelicModifierType)EditorGUILayout.EnumPopup(relicMods[i]);relicTiers[i]=EditorGUILayout.IntSlider(relicTiers[i],1,5);EditorGUILayout.EndHorizontal();}
            if(GUILayout.Button("ADD RELIC MODIFIER ROW")&&relicMods.Count<6){relicMods.Add(RelicModifierType.MoreAttackSpeed);relicTiers.Add(1);}if(GUILayout.Button("REMOVE LAST RELIC ROW")&&relicMods.Count>1){relicMods.RemoveAt(relicMods.Count-1);relicTiers.RemoveAt(relicTiers.Count-1);}
            if(GUILayout.Button("GRANT RELIC"))Attempt(GrantRelic);
            rewardDepth=EditorGUILayout.IntSlider("Reward depth",rewardDepth,60,350);var weights=RelicProgressionRules.RarityWeights(rewardDepth);
            EditorGUILayout.LabelField("REWARD PREVIEW",$"{RelicProgressionRules.RewardCount(rewardDepth)} Relics / L{RelicInventory.LevelForZone(rewardDepth)} / N,M,R,L weights {weights}");
            if(GUILayout.Button("GRANT REWARD MILESTONE (NO REBIRTH)"))Attempt(()=>{var inv=RelicInventory.Instance;for(int i=0;i<RelicProgressionRules.RewardCount(rewardDepth);i++)inv.Grant(RelicProgressionRules.Generate(i==0&&rewardDepth>=340?LootManager.GearRarity.Legendary:RelicProgressionRules.RollRarity(rewardDepth,UnityEngine.Random.value),RelicInventory.LevelForZone(rewardDepth),Mathf.Max(1,inv.CurrentCycle)));feedback="Granted previewed milestone rewards without resetting run.";});
            if(GUILayout.Button("GRANT ONE FORGE OPPORTUNITY (TEST ONLY)"))RelicInventory.Instance?.AwardForgeOpportunity();
            if(GUILayout.Button("SAVE CURRENT CHARACTER EXPLICITLY"))GamePersistence.Save();
        }
        EditorGUILayout.HelpBox(feedback??"No action yet.",MessageType.Info);EditorGUILayout.EndScrollView();
    }
    void Attempt(Action action){try{if(Inventory.Instance==null)throw new InvalidOperationException("Gameplay inventory is not available. Start a character first.");action();}catch(Exception ex){feedback=ex.Message;}}
    void Jump(int target)=>Attempt(()=>{GameManager.Instance.StartZone(Mathf.Clamp(target,1,1000000));feedback="Combat level and encounter state reset together.";});
    void SpawnItem()
    {
        if(rarity==LootManager.GearRarity.Unique)throw new InvalidOperationException("Use the dedicated Unique selector.");
        if(element is Element.Poison or Element.Count)throw new InvalidOperationException("Choose a production element.");
        var database=ModManager.Instance?.Database;if(database==null)throw new InvalidOperationException("Modifier catalog unavailable.");
        var gear=new GameObject("Curated development item").AddComponent<Gear>();gear.Initialize(slot,rarity,level,element,slot==LootManager.GearType.Weapons?weaponIds[weapon]:null);
        try
        {
            var mods=new List<RolledMod>();
            for(int i=0;i<stats.Count;i++)
            {var tier=ModManager.ApplicableTiers(database.GetDefinition(stats[i]),slot,gear.WeaponTypeId).Find(x=>x.tierIndex==tiers[i]&&x.minItemLevel<=level);if(tier==null)throw new InvalidOperationException("Tier is unavailable for "+stats[i]);var mod=tier.pairedDamage?new RolledMod(stats[i],tier.tierIndex,Mathf.Lerp(tier.minValue,tier.maxValue,Roll()),Mathf.Lerp(tier.minHighValue,tier.maxHighValue,Roll()),i==0):new RolledMod(stats[i],tier.tierIndex,Mathf.Lerp(tier.minValue,tier.maxValue,Roll()),i==0);mods.Add(mod);}
            gear.ApplyMods(mods);
            if(slot==LootManager.GearType.Weapons){gear.BaseDamage=gear.BaseDamageMin=gear.BaseDamageMax=4+level*.8f;gear.BaseAttackSpeed=1;gear.BaseCritChance=.05f;LootManager.ApplyNaturalWeaponProfile(gear);}
            var snapshot=GearSnapshotData.Capture(gear);
            if(!GamePersistence.IsValidCuratedGear(snapshot))throw new InvalidOperationException("Illegal affix family, element, side capacity or implicit. Nothing added.");
            Inventory.Instance.Add(gear);feedback="Curated item added. Normal ownership and save rules apply.";
        }
        catch{Destroy(gear.gameObject);throw;}
    }
    void GrantRelic()
    {
        if(relicRarity==LootManager.GearRarity.Unique)throw new InvalidOperationException("Unique Relics require the four-item forge.");
        var inv=RelicInventory.Instance;var result=RelicProgressionRules.Generate(relicRarity,relicLevel,Mathf.Max(1,inv.CurrentCycle));
        if(curatedRelic)
        {
            if(relicMods.Distinct().Count()!=relicMods.Count||relicMods.Count<AncientRelicCrafting.Minimum(relicRarity)||relicMods.Count>AncientRelicCrafting.Maximum(relicRarity))throw new InvalidOperationException("Modifier count or duplicate selection is invalid.");result.modifiers.Clear();
            for(int i=0;i<relicMods.Count;i++){var d=RelicModifierDefinitions.Get(relicMods[i]);if(d==null||RelicModifierDefinitions.IsRetired(d.Id))throw new InvalidOperationException("Retired modifier cannot be newly authored.");var t=d.Tiers.FirstOrDefault(x=>x.TierIndex==relicTiers[i]&&x.MinimumRelicLevel<=relicLevel);if(t==null)throw new InvalidOperationException("Relic tier not eligible at this level.");result.modifiers.Add(new RelicModifier(d.Id,Mathf.Lerp(t.Minimum,t.Maximum,Roll()),i==0,t.TierIndex));}
        }
        result.crafted=crafted;inv.Grant(result);feedback="Relic granted: "+(crafted?"Crafted":"Pristine");
    }
}
