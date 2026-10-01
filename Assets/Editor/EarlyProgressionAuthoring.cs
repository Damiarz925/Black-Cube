using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EarlyProgressionAuthoring
{
    public static void ApplyAndSmoke(){Apply();EarlyProgressionPlayCheck.Run();}
    public static void ApplyAndBuild(){Apply();SystemsRedesignAuthoring.BuildWindows();}
    [MenuItem("Black-Cube/Progression/Apply Early Progression and Rebirth UI")]
    public static void Apply()
    {
        const string profilePath="Assets/Resources/UniqueTierProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<UniqueTierProfileSO>(profilePath);
        if(profile==null){profile=ScriptableObject.CreateInstance<UniqueTierProfileSO>();AssetDatabase.CreateAsset(profile,profilePath);}
        profile.database=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        Swap(GenericClassPassiveReauthoring.Branch(PlayerClassIds.Mage),StatTypes.DamageTakenFromManaBeforeLife,StatTypes.ManaRegeneration);
        Swap(GenericClassPassiveReauthoring.Branch(PlayerClassIds.Priest),StatTypes.LifeRecoveryEffect,StatTypes.VoidRes);
        var ranger=GenericClassPassiveReauthoring.Branch(PlayerClassIds.Ranger);
        foreach(var n in ranger.AllAuthoredNodes())foreach(var e in n.Effects)
            if(e.Stat is StatTypes.ReducedShockEffect or StatTypes.ReducedChillEffect&&e.Value==0)e.SetStat(e.Stat,8);
        EditorUtility.SetDirty(ranger);AssetDatabase.SaveAssetIfDirty(ranger);
        foreach(string path in new[]{"Assets/Prefabs/PaperBattle/PaperBattle.prefab","Assets/Prefabs/UI/InventoryPanel.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var filter in root.GetComponentsInChildren<AdvancedLootFilterUI>(true))filter.UpgradeAuthoring();
                foreach(var rebirth in root.GetComponentsInChildren<RebirthConfirmationUI>(true))rebirth.AuthorSystemsSetup();
                if(path.Contains("PaperBattle")&&root.GetComponentInChildren<RelicTriggerPanelUI>(true)==null)
                {var canvas=root.GetComponentInChildren<Canvas>(true);var go=new GameObject("Relic Trigger Controls",typeof(RectTransform),typeof(RelicTriggerPanelUI));go.transform.SetParent(canvas.transform,false);go.GetComponent<RelicTriggerPanelUI>().Author();}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Report();Debug.Log("Early progression authoring complete.");
    }
    static void Swap(PassiveClassBranchSO branch,StatTypes incoming,StatTypes outgoing)
    {
        var early=branch.Tiers[0].Left.B;
        if(early.Effects.Any(e=>e.Stat==incoming))return;
        var late=branch.AllAuthoredNodes().First(n=>n.Effects.Any(e=>e.Stat==incoming));
        if(!early.Effects.Any(e=>e.Stat==outgoing))throw new InvalidOperationException("Unexpected early node: "+early.StableId);
        var a=JsonUtility.ToJson(early);var b=JsonUtility.ToJson(late);
        string aid=early.StableId,asid=early.LogicalSlotId,bid=late.StableId,bsid=late.LogicalSlotId;
        JsonUtility.FromJsonOverwrite(b,early);JsonUtility.FromJsonOverwrite(a,late);
        RestoreId(early,aid,asid);RestoreId(late,bid,bsid);
        EditorUtility.SetDirty(branch);AssetDatabase.SaveAssetIfDirty(branch);
    }
    static void RestoreId(PassiveAuthoredNode n,string id,string slot)
    {n.Configure(id,n.DisplayName,n.Description,n.Branch,n.Size,n.Kind,n.Keystone,n.Effects.Select(e=>new PassiveEffect(e.Stat,e.Value)).ToArray(),slot);}
    public static void Report()
    {
        var report=new StringBuilder("# Early progression / Unique numerical tables\n\nGenerated from production definitions. Values are immutable on newly rolled items; existing legacy Uniques retain their original rolls.\n\n## XP requirements\n\n| Level | Old | New |\n|---|---:|---:|\n");
        for(int i=1;i<=10;i++)report.AppendLine($"| {i} | {EarlyProgressionRules.LegacyRequirement(i)} | {EarlyProgressionRules.Requirement(i)} |");
        report.AppendLine("\nEnemy XP still uses the old curve; reward scaling is unchanged. This prevents reducing requirements and rewards together.\n\n## Earliest sustain choices\n");
        foreach(var id in PlayerClassCatalog.All.Select(c=>c.Id))
        {var b=GenericClassPassiveReauthoring.Branch(id);if(b==null)continue;report.AppendLine("### "+id+"\n");foreach(var t in b.Tiers.Take(2))foreach(var n in t.Left.GenericNodes.Concat(t.Right.GenericNodes))report.AppendLine($"- Tier {t.Tier}: {n.DisplayName}: {string.Join(", ",n.Effects.Select(e=>$"{e.Stat} {e.Value}"))}");}
        foreach(var d in UniqueCatalog.All)
        {
            report.AppendLine($"\n## {d.name}\n\nEligible item level 1–100. Bands T5: 1–19; T4: 20–39; T3: 40–59; T2: 60–79; T1: 80–100.\n\n| Stat / power | T5 | T4 | T3 | T2 | T1 |\n|---|---|---|---|---|---|");
            foreach(var s in d.stats)report.AppendLine($"| {s.stat} | {string.Join(" | ",new[]{1,20,40,60,80}.Select(l=>Range(UniqueTierRules.StatRange(s,d,l))))} |");
            foreach(var p in d.powers)report.AppendLine($"| {p.power} | {string.Join(" | ",new[]{1,20,40,60,80}.Select(l=>Range(UniqueTierRules.PowerRange(p,l))))} |");
            if(d.slot!=LootManager.GearType.Weapons)continue;
            report.AppendLine("\n| Item level | Unique base damage | APS | Base crit | Highest natural damage midpoint |\n|---|---|---|---|---|\n");
            foreach(int level in new[]{1,20,40,60,80,100})
            {var go=new GameObject();var gear=go.AddComponent<Gear>();UniqueTierRules.WeaponBase(gear,d,level);var t=UniqueTierRules.Best(StatTypes.WeaponBaseDmg,d.slot,d.weapon,level);var profile=WeaponTypeCatalog.Get(d.weapon);report.AppendLine($"| {level} | {gear.BaseDamageMin:0.##}–{gear.BaseDamageMax:0.##} | {gear.BaseAttackSpeed:0.###} | {gear.BaseCritChance:P1} | {(t.minValue+t.maxValue)*.5f*(profile.BaseDamageMin+profile.BaseDamageMax)*.5f/40:0.##} |");UnityEngine.Object.DestroyImmediate(go);}
        }
        report.AppendLine("\n## Weapon comparison methodology\n\nThe following figures use midpoint immutable Unique rolls and the highest unlocked natural tiers. Natural-only is an ordinary Normal weapon before its random implicit. Local-offense reference is a two-core-prefix / two-core-suffix ordinary weapon: matching flat damage + matching increased damage, local Attack Speed + local Crit Chance, all at the unlocked tier midpoint. This is not a perfect Legendary, excludes additional global/implicit/special stats, and does not credit any Unique's special mechanic. DPS includes the baseline 150% crit multiplier.\n\n| Unique | ilvl | Unique local damage / APS / crit | Unique crit-adjusted base DPS | Natural-only DPS | Local-offense reference DPS |\n|---|---:|---|---:|---:|---:|");
        foreach(var d in UniqueCatalog.All.Where(d=>d.slot==LootManager.GearType.Weapons))foreach(int level in new[]{1,40,100})
        {
            var g=UniqueCatalog.Create(d.id,level,fixedRoll:.5f);var p=WeaponTypeCatalog.Get(d.weapon);
            var damage=UniqueTierRules.Best(StatTypes.WeaponBaseDmg,d.slot,d.weapon,level);
            var speed=UniqueTierRules.Best(StatTypes.WeaponBaseAttackSpeed,d.slot,d.weapon,level);
            var crit=UniqueTierRules.Best(StatTypes.WeaponBaseCrit,d.slot,d.weapon,level);
            float natural=Mid(damage)*(p.BaseDamageMin+p.BaseDamageMax)*.5f/40,aps=Mid(speed)*p.AttacksPerSecond/.6f,critical=Mid(crit)*p.BaseCritChance/5;
            var flatStat=d.element switch{Element.Fire=>StatTypes.FlatFire,Element.Cold=>StatTypes.FlatCold,Element.Light=>StatTypes.FlatLight,Element.Void=>StatTypes.FlatVoid,_=>StatTypes.FlatPhys};
            var damageStat=d.element switch{Element.Fire=>StatTypes.FireDmg,Element.Cold=>StatTypes.ColdDmg,Element.Light=>StatTypes.LightDmg,Element.Void=>StatTypes.VoidDmg,_=>StatTypes.PhysDmg};
            var flat=UniqueTierRules.Best(flatStat,d.slot,d.weapon,level);float flatMid=flat==null?0:flat.pairedDamage?(flat.minValue+flat.maxValue+flat.minHighValue+flat.maxHighValue)*.25f:Mid(flat);
            float local=(natural+flatMid)*(1+Mid(UniqueTierRules.Best(damageStat,d.slot,d.weapon,level))/100);
            float localAps=aps*(1+Mid(UniqueTierRules.Best(StatTypes.AttackSpeed,d.slot,d.weapon,level))/100);
            float localCrit=Mathf.Clamp01(critical*(1+Mid(UniqueTierRules.Best(StatTypes.CritChance,d.slot,d.weapon,level))/100));
            report.AppendLine($"| {d.name} | {level} | {g.GetEffectiveBaseDamage():0.##} / {g.GetEffectiveAttackSpeed():0.###} / {g.GetEffectiveBaseCrit():P1} | {g.GetAverageWeaponDps()*(1+Mathf.Clamp01(g.GetEffectiveBaseCrit())*.5f):0.##} | {natural*aps*(1+critical*.5f):0.##} | {local*localAps*(1+localCrit*.5f):0.##} |");
            UnityEngine.Object.DestroyImmediate(g.gameObject);
        }
        Directory.CreateDirectory("Docs");File.WriteAllText("Docs/EARLY_PROGRESSION_NUMERICAL_TABLES.md",report.ToString());
    }
    static float Mid(AffixTier t)=>t==null?0:(t.minValue+t.maxValue)*.5f;
    static string Range(Vector2 v)=>$"{v.x:0.####}–{v.y:0.####}";
}
