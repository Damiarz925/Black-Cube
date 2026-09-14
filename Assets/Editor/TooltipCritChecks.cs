// Developer map: Opt-in Play checks for typed damage rows, final critical chance and inventory/equipment tooltip interactions.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class TooltipCritChecks
{
    const string Key = "BlackCube.TooltipCritChecks", Report = "ReviewCaptures/tooltip-crit-check.txt";
    static IEnumerator routine;
    static double ready;
    static TooltipCritChecks()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.ExitingPlayMode) routine = null;
            if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false); ready = EditorApplication.timeSinceStartup + 2; routine = Run();
        };
        EditorApplication.update += () =>
        {
            if (routine == null || EditorApplication.timeSinceStartup < ready) return;
            try { if (routine.MoveNext()) return; } catch (Exception e) { Write("FAIL: " + e); }
            routine = null; Time.timeScale = 0;
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify Tooltip Crit and Typed Damage %&t")]
    static void Begin()
    {
        if (EditorApplication.isPlaying) return;
        File.WriteAllText(Report, "Tooltip / crit / typed damage final-code checks\n");
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Write(string s) { File.AppendAllText(Report, s + "\n"); Debug.Log(s); }
    static void Check(bool ok, string s) { if (!ok) throw new Exception(s); Write("PASS: " + s); }
    static void Near(float a, float b, string s) => Check(Mathf.Abs(a-b)<.0001f, s + $" ({a:0.####})");
    static float TooltipPercent(string text,string label)
    {
        string plain=Regex.Replace(text,"<[^>]+>",string.Empty);
        string row=plain.Split('\n').Single(line=>line.TrimStart().StartsWith(label+":",StringComparison.Ordinal));
        return float.Parse(row[(row.IndexOf(':')+1)..].Trim().TrimEnd('%'),CultureInfo.InvariantCulture);
    }
    static Gear Item(LootManager.GearType type, params RolledMod[] mods)
    {
        var g = new GameObject("Tooltip crit fixture").AddComponent<Gear>();
        g.Initialize(type, LootManager.GearRarity.Magic, 10, Element.Phys);
        g.BaseDamage = 100; g.BaseAttackSpeed = 1; g.BaseCritChance = .05f;
        g.ApplyMods(new List<RolledMod>(mods)); Inventory.Instance.Add(g); return g;
    }
    static string Row(string label) => Object.FindFirstObjectByType<PlayerStatsPanelUI>().GetComponentsInChildren<StatRowUI>()
        .Single(r => r.GetComponentsInChildren<TMP_Text>().Any(t => t.text == label)).GetComponentsInChildren<TMP_Text>().First(t => t.text != label).text;
    static Vector2 Center(RectTransform r) => RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
    static IEnumerator Run()
    {
        Time.timeScale = 0;
        var eq = EquipmentManager.Instance; var inv = Inventory.Instance;
        var p = Object.FindFirstObjectByType<PlayerController>(); var stats = p.GetComponent<StatsComponent>();
        var hud = Object.FindFirstObjectByType<PaperBattleHUD>();
        hud.inventoryPanel.SetActive(true); hud.statsPanel.SetActive(true);
        foreach (LootManager.GearType type in Enum.GetValues(typeof(LootManager.GearType))) eq.Unequip(type);
        foreach (var t in stats.GetTrackedStats().ToArray()) if (t != StatTypes.Life) stats.SetBaseStat(t,0);
        var weapon = Item(LootManager.GearType.Weapons, new RolledMod(StatTypes.WeaponBaseCrit,1,3.47f));
        Near(weapon.BaseCritChance,.0347f,"Old 3.47 raw roll that displayed 347% now stores .0347");
        Near(TooltipPercent(ItemTooltipUI.Describe(weapon),"Crit Chance"),weapon.BaseCritChance*100f,"Tooltip converts the stored crit fraction to percentage exactly once");
        var db = AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        Check(db.GetDefinition(StatTypes.WeaponBaseCrit).tiers.All(t=>t.minValue==5 && t.maxValue==10),"Production intrinsic tiers remain 5-10% at every level");
        var saved = UnityEngine.Random.state; UnityEngine.Random.InitState(947);
        for (int i=0;i<100;i++)
        {
            var rolled=ModManager.Instance.RollModsForItem(LootManager.GearType.Weapons,LootManager.GearRarity.Normal,1+i,0,Element.Phys);
            var g=Item(LootManager.GearType.Weapons,rolled.ToArray());
            if(g.BaseCritChance<.05f || g.BaseCritChance>.1f) throw new Exception("Production roll outside 5-10%");
            inv.Remove(g); Object.Destroy(g.gameObject);
        }
        UnityEngine.Random.state=saved; Check(true,"100 production ModManager rolls across levels have correct intrinsic units/range");
        weapon.BaseCritChance=.05f; weapon.ApplyMods(new List<RolledMod>{new RolledMod(StatTypes.CritChance,1,100)}); eq.Equip(weapon);
        var ring=Item(LootManager.GearType.Rings,new RolledMod(StatTypes.CritChance,1,100)); eq.Equip(ring);
        Near(stats.GetStat(StatTypes.CritChance),1,"Weapon local 100% excluded from global bucket");
        Near(p.GetFinalCritChance(),.2f,"5 x 2 x 2 = 20% without flat bonus");
        var belt=Item(LootManager.GearType.Belts,new RolledMod(StatTypes.BaseCritChance,1,2)); eq.Equip(belt);
        Near(p.GetFinalCritChance(),.28f,"(5 + belt2) x 2 x 2 = 28%");
        Check(Row("Critical Chance (Final)")=="28%","Immediate equipped stats display28%");
        saved=UnityEngine.Random.state;
        for(int i=0;i<100;i++)
        {
            UnityEngine.Random.InitState(i); bool expected=UnityEngine.Random.value<.28f;
            UnityEngine.Random.InitState(i); if(p.BuildAttackContext().IsCrit!=expected) throw new Exception("Attack roll disagrees");
        }
        UnityEngine.Random.state=saved; Check(true,"100 seeded real attacks use exact28% probability");
        weapon.LocalBaseCrit=.01f; p.EquipWeapon(weapon); Near(p.GetFinalCritChance(),.32f,"Weapon and belt flat points both precede both multipliers");
        weapon.LocalBaseCrit=0; eq.Unequip(LootManager.GearType.Belts); Near(p.GetFinalCritChance(),.2f,"Belt removal refresh");
        Check(Row("Critical Chance (Final)")=="20%","Removal immediate display20%"); eq.Equip(belt);
        var source=new object(); stats.AddModifier(new StatModifier(StatTypes.CritChance,StatOp.Additive,50,source));
        Near(p.GetFinalCritChance(),.35f,"Additional player source summed globally"); Check(Row("Critical Chance (Final)")=="35%","Player-source display refresh");
        stats.RemoveModifiersFromSource(source); stats.SetBaseStat(StatTypes.CritChance,10000); Near(p.GetFinalCritChance(),1,"Probability cap preserved");stats.SetBaseStat(StatTypes.CritChance,0);
        foreach(Element element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Poison,Element.Void})
        {
            weapon.BaseElement=element; p.EquipWeapon(weapon);
            var hits=p.BuildNonCriticalAttackContext().Hits;
            Check(hits.Count==1 && hits[0].Element==element,"Pure "+element+" real outgoing label");
            Check(Row(ItemTooltipUI.ElementName(element)+" / Hit (Noncritical)")=="100","Pure "+element+" only typed hit row");
            Check(Row("Weapon Base "+ItemTooltipUI.ElementName(element))=="100","Typed weapon base "+element);
        }
        weapon.BaseElement=Element.Phys; p.EquipWeapon(weapon);
        stats.SetBaseStat(StatTypes.FlatFire,10); stats.SetBaseStat(StatTypes.GenericDmg,50); stats.SetBaseStat(StatTypes.GenericMult,20); stats.SetBaseStat(StatTypes.PhysMult,30);
        Check(Row("Physical / Hit (Noncritical)")=="234" && Row("Fire / Hit (Noncritical)")=="18","Existing mixed flat contribution:234 Physical and18 Fire; more factors multiply");
        stats.SetBaseStat(StatTypes.FlatFire,0);stats.SetBaseStat(StatTypes.GenericDmg,0);stats.SetBaseStat(StatTypes.GenericMult,0);stats.SetBaseStat(StatTypes.PhysMult,0);
        var progression=Object.FindFirstObjectByType<PlayerProgression>();progression.AddExperience(progression.RequiredXp);Check(progression.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.Poison,0)),"Actual Poison passive purchased with a deterministically earned point");
        Check(stats.GetRawStat(StatTypes.PoisonDmg)==5 && Row("Physical / Hit (Noncritical)")=="100" && Row("Critical Chance (Final)")=="28%","Poison passive remains scoped and tooltip refresh preserves unrelated values");
        progression.ResetProgression();
        // Reuse the existing production DOT and six-type arithmetic regression fixtures.
        eq.Unequip(LootManager.GearType.Rings);eq.Unequip(LootManager.GearType.Belts);weapon.BaseCritChance=0;weapon.LocalIncCrit=0;
        File.AppendAllText("ReviewCaptures/inventory-grid-check.txt", "\nTOOLTIP-CRIT regression rerun " + DateTime.Now.ToString("s") + "\n");
        typeof(InventoryGridChecks).GetMethod("VerifyDamageTypesAndAilments",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
            .Invoke(null,new object[]{p,stats,weapon});
        Check(true,"Existing six-type and bleed/poison/ignite scaling, duration, health-loss regressions passed (inventory-grid-check.txt)");
        weapon.BaseCritChance=.05f;weapon.LocalIncCrit=1;eq.Equip(ring);eq.Equip(belt);p.EquipWeapon(weapon);
        yield return null; Canvas.ForceUpdateCanvases();
        var ui=hud.inventoryPanel.GetComponent<InventoryUI>();
        var spare=Item(LootManager.GearType.Gloves,new RolledMod(StatTypes.GenericDmg,1,25));
        var spare2=Item(LootManager.GearType.Helmets,new RolledMod(StatTypes.GenericMult,1,20));
        yield return null; Canvas.ForceUpdateCanvases();
        var slot=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Single(s=>s.Item==spare);
        var slot2=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Single(s=>s.Item==spare2);
        var data=new PointerEventData(EventSystem.current){position=Center((RectTransform)slot.transform)};
        slot.OnPointerEnter(data);var tip=ui.Tooltip;
        data.position=Vector2.zero;slot.OnPointerExit(data);Check(!tip.gameObject.activeSelf,"Leaving item away closes synchronously");
        data.position=Center((RectTransform)slot.transform);slot.OnPointerEnter(data);
        data.position=Center((RectTransform)tip.transform);slot.OnPointerExit(data);tip.OnPointerEnter(data);
        Check(tip.gameObject.activeSelf && tip.BodyText==ItemTooltipUI.Describe(spare),"Direct transfer into card keeps full actual stats");
        yield return null;Check(tip.gameObject.activeSelf,"Card stays open over pointer");
        data.position=Vector2.zero;tip.OnPointerExit(data);Check(!tip.gameObject.activeSelf,"Leaving card closes synchronously");
        slot.OnPointerEnter(data);slot2.OnPointerEnter(data);Check(tip.BodyText==ItemTooltipUI.Describe(spare2),"Another item replaces contents");
        var gridScroll=slot.GetComponentInParent<ScrollRect>();gridScroll.onValueChanged.Invoke(Vector2.zero);Check(!tip.gameObject.activeSelf,"Inventory scrolling closes tooltip");
        data.position=Center((RectTransform)slot.transform);slot.OnPointerEnter(data);tip.ScrapButton.onClick.Invoke();
        Check(!inv.Items.Contains(spare) && !tip.gameObject.activeSelf,"Tooltip Scrap removes item and closes");
        foreach(var hover in hud.statsPanel.GetComponentsInChildren<EquippedItemHoverUI>())
        {
            var gear=eq.GetEquipped(hover.Type); if(gear==null)continue;
            data.position=Center((RectTransform)hover.transform);hover.OnPointerEnter(data);
            Check(tip.gameObject.activeSelf && tip.BodyText==ItemTooltipUI.Describe(gear),"Equipped "+hover.Type+" full actual tooltip");
            Check(!tip.ScrapButton.interactable && !inv.TryDismantle(gear),"Equipped "+hover.Type+" no scrap");
            tip.ScrapButton.onClick.Invoke();Check(eq.GetEquipped(hover.Type)==gear,"Even direct action cannot scrap equipped "+hover.Type);
        }
        for(int i=0;i<30;i++)Item((LootManager.GearType)(i%8));
        tip.Hide();Write("COMPLETE: scoped assertions passed; disposable visual fixture ready. Exit Play without saving.");
    }
}
