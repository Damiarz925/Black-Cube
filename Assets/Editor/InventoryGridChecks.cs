// Developer map: Opt-in Play fixtures for responsive inventory, pickup filters, gear damage and ailment regression.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class InventoryGridChecks
{
    const string Key="BlackCube.GridChecks",Report="Logs/Step12_5-InventoryGrid-check.txt";
    static IEnumerator run; static double ready;
    static InventoryGridChecks()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s==PlayModeStateChange.ExitingPlayMode)run=null;
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);ready=EditorApplication.timeSinceStartup+2;run=SessionState.GetBool(Key+"Preview",false)?Preview():Run();SessionState.SetBool(Key+"Preview",false);
        };
        EditorApplication.update+=()=>
        {
            if(run==null || EditorApplication.timeSinceStartup<ready)return;
            try {if(run.MoveNext())return;}catch(Exception e){Write("FAIL: "+e);}
            run=null;Time.timeScale=0;
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify Inventory Grid Filter and Gear Damage")]
    static void Begin()
    {if(EditorApplication.isPlaying)return;File.WriteAllText(Report,"Grid / pickup filter / nonweapon damage\n");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    [MenuItem("Black Cube/Play Checks/Preview Compact Inventory")]
    static void BeginPreview()
    {if(EditorApplication.isPlaying)return;SessionState.SetBool(Key+"Preview",true);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static IEnumerator Preview()
    {
        Time.timeScale=0;var inv=Inventory.Instance;
        var source=Item();inv.Add(source);inv.TryDismantle(source);
        for(int i=0;i<6;i++){var g=Item(LootManager.GearType.Weapons,(LootManager.GearRarity)(i%4),i+1);g.BaseElement=(Element)i;inv.Add(g);}
        for(int i=0;i<32;i++)inv.Add(Item((LootManager.GearType)(i%8),(LootManager.GearRarity)(i%4),20+i));
        Object.FindFirstObjectByType<PaperBattleHUD>().inventoryPanel.SetActive(true);
        yield return null;
    }
    static void Write(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Write("PASS: "+s);}
    static Gear Item(LootManager.GearType type=LootManager.GearType.Rings,LootManager.GearRarity rarity=LootManager.GearRarity.Magic,int level=10)
    {var g=new GameObject("Grid fixture").AddComponent<Gear>();g.Initialize(type,rarity,level,Element.Phys);return g;}
    static Gear Modified(LootManager.GearType type,params RolledMod[] mods)
    {var g=Item(type);g.ApplyMods(new List<RolledMod>(mods));Inventory.Instance.Add(g);return g;}
    static float Hit=>Object.FindFirstObjectByType<PlayerController>().BasicAttackDamage;
    static void Damage(float expected,string label)
    {
        var player=Object.FindFirstObjectByType<PlayerController>();
        Check(Mathf.Abs(Hit-expected)<.01f,label+$" damage={Hit:0.###}");
        Check(Mathf.Abs(player.BuildAttackContext().Hits.Sum(h=>h.Amount)-expected)<.01f,label+" real attack matches");
        var neutral=new GameObject("Neutral damage defender").AddComponent<StatsComponent>();
        Check(Mathf.Abs(CombatCalculator.CalculateFinalDamage(player.BuildAttackContext(),player.GetComponent<StatsComponent>(),neutral)-expected)<.01f,label+" neutral combat damage matches");Object.Destroy(neutral.gameObject);
        var rows=Object.FindFirstObjectByType<PlayerStatsPanelUI>().GetComponentsInChildren<StatRowUI>()
            .Where(r=>r.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.EndsWith(" / Hit (Noncritical)")));
        float value=rows.Sum(r=>PreviewAverage(r.GetComponentsInChildren<TMP_Text>().First(t=>!t.text.EndsWith(" / Hit (Noncritical)")).text));
        Check(Mathf.Abs(value-expected)<.02f,label+" immediate displayed typed damage sum");
    }
    static float PreviewAverage(string text)
    {
        int start=text.IndexOf("Average ",StringComparison.Ordinal);
        if(start>=0)
        { start+=8;return float.Parse(text.Substring(start,text.IndexOf(')',start)-start),System.Globalization.CultureInfo.InvariantCulture); }
        return float.Parse(text,System.Globalization.CultureInfo.InvariantCulture);
    }
    static IEnumerator Run()
    {
        Time.timeScale=0;var inv=Inventory.Instance;var eq=EquipmentManager.Instance;
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.statsPanel.SetActive(true);hud.inventoryPanel.SetActive(true);
        yield return null;
        var player=Object.FindFirstObjectByType<PlayerController>();var stats=player.GetComponent<StatsComponent>();
        VerifyItemIconBands();
        VerifyModHighlightFiltering();
        stats.SetBaseStat(StatTypes.GenericDmg,0);stats.SetBaseStat(StatTypes.GenericMult,0);
        var weapon=Item(LootManager.GearType.Weapons);weapon.BaseDamage=100;weapon.BaseAttackSpeed=1;weapon.BaseCritChance=0;inv.Add(weapon);eq.Equip(weapon);
        Damage(100,"Baseline");
        var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");
        foreach(var type in new[]{LootManager.GearType.Rings,LootManager.GearType.Helmets})
        foreach(var stat in new[]{StatTypes.GenericDmg,StatTypes.GenericMult})
        {
            Check(GearStatLists.BuildDefaultStatPools()[type].Contains(stat),type+" supports "+stat);
            var def=db.GetDefinition(stat);var tier=def.tiers.First();float value=(tier.minValue+tier.maxValue)*.5f;
            var g=Modified(type,new RolledMod(stat,tier.tierIndex,value));eq.Equip(g);
            Damage(100*(1+value/100),type+" real tier midpoint "+stat+" "+value);
            eq.Unequip(type);Damage(100,type+" removal");
        }
        var ring=Modified(LootManager.GearType.Rings,new RolledMod(StatTypes.GenericDmg,1,25));eq.Equip(ring);Damage(125,"Ring increased25");
        var helmet=Modified(LootManager.GearType.Helmets,new RolledMod(StatTypes.GenericDmg,1,20));eq.Equip(helmet);Damage(145,"Combined increased25+20 additive");
        var moreRing=Modified(LootManager.GearType.Rings,new RolledMod(StatTypes.GenericMult,1,20));eq.Equip(moreRing);Damage(144,"Ring more20 plus helmet increased20");
        var moreHelmet=Modified(LootManager.GearType.Helmets,new RolledMod(StatTypes.GenericMult,1,30));eq.Equip(moreHelmet);Damage(156,"Combined more20 and more30 multiply to56% effective");
        Check(Mathf.Abs(stats.GetStat(StatTypes.GenericMult)-.56f)<.001f,"More bucket exposes56 percent");
        stats.SetBaseStat(StatTypes.PhysMult,10);Damage(171.6f,"Generic factors1.2*1.3 times matching Physical1.1");
        stats.SetBaseStat(StatTypes.FlatFire,10);Damage(187.2f,"Existing Fire flat hit uses generic1.56 only");
        stats.SetBaseStat(StatTypes.FlatFire,0);stats.SetBaseStat(StatTypes.PhysMult,0);
        eq.Unequip(LootManager.GearType.Rings);Damage(130,"More ring removal");eq.Unequip(LootManager.GearType.Helmets);Damage(100,"Both removed");
        stats.SetBaseStat(StatTypes.GenericDmg,50);stats.SetBaseStat(StatTypes.GenericMult,20);Damage(180,"Increased50 and more20 categories multiply");stats.SetBaseStat(StatTypes.GenericDmg,0);stats.SetBaseStat(StatTypes.GenericMult,0);
        var basePercent=new GameObject("Percent base fixture").AddComponent<StatsComponent>();basePercent.SetBaseStat(StatTypes.GenericMult,10);basePercent.AddModifier(new StatModifier(StatTypes.GenericMult,StatOp.Multiplicative,20,basePercent));
        Check(Mathf.Abs(basePercent.GetStat(StatTypes.GenericMult)-.32f)<.001f,"Base more10 with source20 multiplies to32 percent");Object.Destroy(basePercent.gameObject);
        var liveFilter=inv.ModHighlightFilter;if(liveFilter.Mode!=ModFilterMode.Simple)liveFilter.SetMode(ModFilterMode.Simple);liveFilter.ClearCurrentMode();
        liveFilter.Toggle(ModFilterCategory.Poison);liveFilter.SetRequiredMatches(1);inv.FilterModMismatchEnabled=true;
        var wanted=Item();wanted.ApplyMods(new List<RolledMod>{new(StatTypes.PoisonChance,1,10)});
        var unwanted=Item();unwanted.ApplyMods(new List<RolledMod>{new(StatTypes.Mana,1,10)});
        Check(inv.Pickup(wanted)&&inv.Items.Contains(wanted),"Mod-matching pickup survives auto-dismantle");
        Check(inv.Pickup(unwanted)&&!inv.Items.Contains(unwanted),"Mod-mismatch pickup auto-dismantles");
        Check(!inv.Pickup(unwanted)&&!inv.TryDismantle(unwanted),"Mod-mismatch pickup cannot be processed twice");
        inv.FilterModMismatchEnabled=false;liveFilter.ClearCurrentMode();
        VerifyDamageTypesAndAilments(player, stats, weapon);
        Check(!inv.FilterLevelEnabled && !inv.FilterRarityEnabled,"Filter defaults OFF");
        var kept=Item();Check(inv.Pickup(kept) && inv.Items.Contains(kept),"Default pickup kept");
        inv.FilterLevelEnabled=true;inv.FilterLevel=10;Check(inv.Items.Contains(kept),"Enabling does not retroactively scrap");
        var lowLegend=Item(rarity:LootManager.GearRarity.Legendary);
        Check(inv.Pickup(lowLegend) && !inv.Items.Contains(lowLegend),"Inclusive level10 Legendary pickup auto-dismantles");
        Check(!inv.Pickup(lowLegend) && !inv.TryDismantle(lowLegend),"Duplicate pickup/dismantle rejected");
        var high=Item(level:11);Check(inv.Pickup(high) && inv.Items.Contains(high),"Level11 above cutoff kept");
        inv.FilterRarityEnabled=true;inv.FilterRarity=LootManager.GearRarity.Magic;
        var highMagic=Item(level:99);Check(inv.Pickup(highMagic) && !inv.Items.Contains(highMagic),"OR rule high-level Magic auto-dismantles");
        var highRare=Item(rarity:LootManager.GearRarity.Rare,level:99);Check(inv.Pickup(highRare) && inv.Items.Contains(highRare),"Above both cutoffs kept");
        inv.FilterLevelEnabled=false;var lowRare=Item(rarity:LootManager.GearRarity.Rare,level:1);Check(inv.Pickup(lowRare)&&inv.Items.Contains(lowRare),"Disabled level cutoff ignored");
        foreach(var rarity in new[]{LootManager.GearRarity.Normal,LootManager.GearRarity.Magic,LootManager.GearRarity.Rare,LootManager.GearRarity.Legendary})
        {inv.FilterRarity=rarity;var g=Item(rarity:rarity);Check(inv.Pickup(g)&&!inv.Items.Contains(g),"Rarity inclusive "+rarity+" auto-dismantles");}
        eq.Equip(kept);eq.Equip(high);Check(inv.Items.Contains(kept),"Equipment swap returns bypass active filter");
        Check(!inv.Pickup(high)&&!inv.TryDismantle(high),"Equipped item protected");
        inv.FilterRarityEnabled=false;eq.Unequip(LootManager.GearType.Rings);
        for(int i=0;i<48;i++){var g=Item((LootManager.GearType)(i%8),(LootManager.GearRarity)(i%4),20+i);g.BaseElement=(Element)(i%6);inv.Add(g);}
        yield return null;Canvas.ForceUpdateCanvases();
        var grid=hud.inventoryPanel.GetComponentInChildren<GridLayoutGroup>();Check(grid.constraintCount>=3,"Compact populated grid has multiple columns");
        var scroll=grid.GetComponentInParent<ScrollRect>();Check(scroll.content.rect.height>scroll.viewport.rect.height,"Populated grid scrolls beyond viewport");
        Check(hud.inventoryPanel.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="LV 20"),"Cells use LV label");
        Write("Visual fixture paused: filter OFF with many gear cells. Verify equip, dismantle, filter controls, scrolling and tooltips. Exit Play afterward.");
    }

    static void VerifyModHighlightFiltering()
    {
        var filter=new InventoryModFilter();
        var mixed=Item();mixed.ApplyMods(new List<RolledMod>{new(StatTypes.PoisonChance,1,10),new(StatTypes.Mana,1,20)});
        filter.Toggle(ModFilterCategory.Poison);filter.Toggle(ModFilterCategory.Mana);filter.SetRequiredMatches(2);
        Check(filter.CountMatches(mixed)==2 && filter.Matches(mixed),"Simple mod filter combines poison and mana affixes");
        var hybrid=Item();hybrid.ApplyMods(new List<RolledMod>{new(StatTypes.FireRes,1,10)});
        filter.ClearCurrentMode();filter.Toggle(ModFilterCategory.Fire);filter.Toggle(ModFilterCategory.ElementalResistance);
        Check(filter.CountMatches(hybrid)==1,"One hybrid affix counts once across selected categories");
        filter.SetMode(ModFilterMode.Advanced);
        Check(!filter.HasSelection,"Changing mode erases prior simple selections");
        filter.Toggle(StatTypes.PoisonChance);filter.SetRequiredMatches(1);
        Check(filter.CountMatches(mixed)==1 && filter.Matches(mixed),"Advanced mod filter matches exact stat only");
        filter.SetMode(ModFilterMode.Simple);
        Check(!filter.HasSelection && filter.AdvancedSelection.Count==0,"Simple and advanced selections cannot coexist");
        Check(InventoryModFilter.SelectableStats.All(stat=>InventoryModFilter.CategoriesFor(stat)!=ModFilterCategory.None),"Every rollable affix has a simple category");
        Object.Destroy(mixed.gameObject);Object.Destroy(hybrid.gameObject);
    }

    static void VerifyItemIconBands()
    {
        void Band(int level,string theme,int set,bool corrupted)
        {
            var selection=ItemIconCatalog.ForLevel(level);
            Check(selection.Theme==theme&&selection.Set==set&&selection.Corrupted==corrupted,
                $"Level {level} uses {(corrupted?"corrupted ":"")}{theme} set {set}");
        }
        Band(1,"forest",1,false);Band(11,"desert",1,false);Band(41,"city",1,false);
        Band(51,"forest",2,false);Band(101,"forest",3,false);Band(141,"city",3,false);
        Band(151,"forest",1,true);Band(201,"forest",2,true);Band(251,"forest",3,true);
        Band(291,"city",3,true);Band(300,"city",3,true);Band(999,"city",3,true);
        Check(ItemIconCatalog.Get(LootManager.GearType.Helmets,1)!=null,"Extracted item icon resources load");
        Check(ItemIconCatalog.HighlightBorder!=null,"Mod-highlight border resource loads");
        for(int stage=0;stage<6;stage++)
            Check(ItemIconCatalog.GetCorruptionBorder(stage)!=null,$"Area-corruption border stage {stage} loads");
    }

    static void VerifyDamageTypesAndAilments(PlayerController player, StatsComponent stats, Gear weapon)
    {
        foreach(Element element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Poison,Element.Void})
        {
            weapon.BaseElement=element;
            var inc=StatMappings.GetIncDamageStat(element);var more=StatMappings.GetMoreDamageStat(element);
            stats.SetBaseStat(StatTypes.GenericDmg,25);stats.SetBaseStat(inc,25);
            stats.AddModifier(new StatModifier(StatTypes.GenericMult,StatOp.Multiplicative,20,weapon));
            stats.AddModifier(new StatModifier(more,StatOp.Multiplicative,30,weapon));
            Damage(234,element+" increased25+25 / more1.2*1.3");
            stats.SetBaseStat(StatTypes.GenericDmg,0);stats.SetBaseStat(inc,0);stats.RemoveModifiersFromSource(weapon);
        }
        weapon.BaseElement=Element.Phys;
        var health=player.GetComponent<HealthComponent>();var controller=player.GetComponent<StatusController>();
        float life=stats.GetRawStat(StatTypes.Life);stats.SetBaseStat(StatTypes.Life,10000);health.RestoreFullLife();
        stats.SetBaseStat(StatTypes.VoidRes,0);stats.SetBaseStat(StatTypes.AllRes,0);
        stats.SetBaseStat(StatTypes.VoidDmg,0);stats.SetBaseStat(StatTypes.VoidMult,0);
        foreach(var name in new[]{"Bleed","Poison","Ignite"})
        {
            var effect=AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/"+name+"Status.asset");
            var inc=(StatTypes)Enum.Parse(typeof(StatTypes),name+"Dmg");
            var more=(StatTypes)Enum.Parse(typeof(StatTypes),name+"Mult");
            var duration=(StatTypes)Enum.Parse(typeof(StatTypes),name+"Duration");
            var res=(StatTypes)Enum.Parse(typeof(StatTypes),name+"Res");
            stats.SetBaseStat(res,0);stats.SetBaseStat(StatTypes.AllAilmentRes,0);
            weapon.BaseElement=name=="Ignite"?Element.Fire:Element.Phys;
            for(int scenario=0;scenario<5;scenario++)
            {
                bool hitScaled=scenario==1||scenario>=3,ailmentScaled=scenario>=2;
                stats.SetBaseStat(StatTypes.GenericDmg,hitScaled?50:0);stats.SetBaseStat(StatTypes.GenericMult,hitScaled?20:0);
                stats.AddModifier(new StatModifier(inc,StatOp.Additive,ailmentScaled?20:0,effect));
                stats.AddModifier(new StatModifier(inc,StatOp.Additive,ailmentScaled?30:0,effect));
                stats.AddModifier(new StatModifier(more,StatOp.Multiplicative,ailmentScaled?20:0,effect));
                stats.AddModifier(new StatModifier(StatTypes.GenericDotMult,StatOp.Multiplicative,ailmentScaled?30:0,effect));
                stats.SetBaseStat(duration,scenario==4?2:0);
                var ctx=player.BuildAttackContext();
                // Void is unrelated to Bleed/Ignite but is first-class Poison source damage.
                if(name=="Poison")ctx.AddDamage(Element.Poison,hitScaled?90:50);
                ctx.AddDamage(Element.Void,777);
                float eligible=(hitScaled?180:100)+(name=="Poison"?(hitScaled?90:50)+777:0);
                Check(Mathf.Abs(AilmentCalculator.GetSourceHitDamage(effect,ctx)-eligible)<.001f,name+" eligible scaled source scenario"+scenario);
                AilmentCalculator.ComputeAilmentFromHit(effect,ctx,stats,out float tick,out int count,out int interval);
                int baseTicks=name=="Bleed"?5:name=="Poison"?4:2;
                float expected=eligible*effect.Magnitude*(ailmentScaled?2.34f:1f)/baseTicks;
                Check(Mathf.Abs(tick-expected)<.001f && count==baseTicks+(scenario==4?2:0) && interval==2,
                    name+" "+new[]{"baseline","hit scaling only","ailment scaling only","layered hit and ailment scaling","duration +2 preserves tick"}[scenario]+$" tick={tick:0.###} ticks={count} total={tick*count:0.###}");
                controller.ClearStatuses();float before=health.CurrentLife;
                controller.ApplyAilmentFromHit(effect,ctx,stats);
                for(int turn=0;turn<count*interval;turn++)controller.TickStatuses();
                Check(Mathf.Abs(before-health.CurrentLife-expected*count)<.02f,name+" actual scheduled health loss scenario"+scenario);
                Check(!controller.GetStatusSummaries().Any(),name+" expires without lingering status scenario"+scenario);
                stats.RemoveModifiersFromSource(effect);stats.SetBaseStat(duration,0);
            }
        }
        controller.ClearStatuses();stats.SetBaseStat(StatTypes.Life,life);health.RestoreFullLife();
        stats.SetBaseStat(StatTypes.GenericDmg,0);stats.SetBaseStat(StatTypes.GenericMult,0);weapon.BaseElement=Element.Phys;
        Damage(100,"Damage fixtures restored");
    }
}
