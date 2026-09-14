// Developer map: Opt-in Play fixture for deterministic damage previews, status gating and legendary tooltips.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class AttackStatsChecks
{
    const string Key="BlackCube.AttackStatsChecks", Report="ReviewCaptures/attack-stats-check.txt";
    static IEnumerator routine;
    static double ready;
    static int errors;
    static readonly StatTypes[] Chances={StatTypes.PoisonChance,StatTypes.BleedChance,StatTypes.IgniteChance,StatTypes.ChillChance,StatTypes.ShockChance};
    static AttackStatsChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.ExitingPlayMode){routine=null;Application.logMessageReceived-=Log;}
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);ready=EditorApplication.timeSinceStartup+2;errors=0;
            Application.logMessageReceived+=Log;routine=SessionState.GetBool(Key+"Legendary",false)?PreviewLegendary():Run();
            SessionState.SetBool(Key+"Legendary",false);
        };
        EditorApplication.update += () =>
        {
            if(routine==null || EditorApplication.timeSinceStartup<ready)return;
            try{if(routine.MoveNext())return;}catch(Exception e){Write("FAIL: "+e);}
            routine=null;Time.timeScale=0;Application.logMessageReceived-=Log;
        };
    }
    [MenuItem("Black Cube/Play Checks/Verify Attack Stats and Status Gating")]
    static void Begin()
    {
        if(EditorApplication.isPlaying)return;
        File.WriteAllText(Report,"Attack / stats / Legendary / element gating\n");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [MenuItem("Black Cube/Play Checks/Preview Legendary Tooltip")]
    static void BeginLegendary()
    {
        if(EditorApplication.isPlaying)return;
        SessionState.SetBool(Key+"Legendary",true);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static IEnumerator PreviewLegendary()
    {
        Time.timeScale=0;
        var materialSource=Gear(LootManager.GearType.Rings,Element.Phys,0);Inventory.Instance.TryDismantle(materialSource);
        var legend=Gear(LootManager.GearType.Rings,Element.Phys,0);legend.Initialize(LootManager.GearType.Rings,LootManager.GearRarity.Legendary,1,Element.Phys);
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.inventoryPanel.SetActive(true);Canvas.ForceUpdateCanvases();
        var slot=hud.inventoryPanel.GetComponentsInChildren<ItemSlotUI>().Single(s=>s.Item==legend);
        hud.inventoryPanel.GetComponent<InventoryUI>().ShowTooltip(slot);
        Write("Legendary-only visual fixture: dismantling must remove the ring and award the current consolidated crafting currency.");
        yield return null;
    }
    static void Log(string message,string trace,LogType type)
    {if(type==LogType.Error || type==LogType.Exception){errors++;Write("CONSOLE: "+message);}}
    static void Write(string s){File.AppendAllText(Report,s+"\n");Debug.Log(s);}
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Write("PASS: "+s);}
    static void Near(float a,float b,string s)=>Check(Mathf.Abs(a-b)<.01f,s+$" ({a:0.##})");
    static Gear Gear(LootManager.GearType type,Element element,float damage)
    {
        var g=new GameObject("Attack stats fixture").AddComponent<Gear>();
        g.Initialize(type,LootManager.GearRarity.Normal,1,element);g.BaseDamage=damage;g.BaseAttackSpeed=1.2f;
        Inventory.Instance.Add(g);return g;
    }
    static string Value(PlayerStatsPanelUI panel,string name)
    {
        if(name=="Damage / Hit (Noncritical)")
            return panel.GetComponentsInChildren<StatRowUI>()
                .Where(r=>r.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.EndsWith(" / Hit (Noncritical)")))
                .Sum(r=>float.Parse(r.GetComponentsInChildren<TMP_Text>().First(t=>!t.text.EndsWith(" / Hit (Noncritical)")).text)).ToString("0.##");
        var row=panel.GetComponentsInChildren<StatRowUI>().FirstOrDefault(r=>r.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==name));
        return row == null ? null : row.GetComponentsInChildren<TMP_Text>().First(t=>t.text!=name).text;
    }
    static HealthComponent Enemy=>BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();
    static void Turn()=>typeof(BattleManager).GetMethod("ResolvePlayerTurn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(BattleManager.Instance,null);
    static StatusEffects Effect(string name,StatusEffects.StatusType type)
    {
        var e=ScriptableObject.CreateInstance<StatusEffects>();e.name=name;
        var so=new SerializedObject(e);so.FindProperty("statusType").enumValueIndex=(int)type;so.ApplyModifiedPropertiesWithoutUndo();return e;
    }
    static IEnumerator Run()
    {
        Time.timeScale=0;
        var player=Object.FindFirstObjectByType<PlayerController>();var stats=player.GetComponent<StatsComponent>();
        var game=GameManager.Instance;var equipment=EquipmentManager.Instance;var xp=game.GetComponent<PlayerProgression>();
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();hud.statsPanel.SetActive(true);
        var panel=hud.statsPanel.GetComponent<PlayerStatsPanelUI>();panel.SetTarget(stats);
        var weapon=Gear(LootManager.GearType.Weapons,Element.Phys,80);equipment.Equip(weapon);
        stats.SetBaseStat(StatTypes.FlatPhys,20);stats.SetBaseStat(StatTypes.PhysDmg,50);stats.SetBaseStat(StatTypes.GenericDmg,25);
        stats.SetBaseStat(StatTypes.PhysMult,20);stats.SetBaseStat(StatTypes.GenericMult,10);
        Near(player.BasicAttackDamage,231f,"Shared outgoing pipeline applies flat, matching increased and independent more factors");
        Near(player.BuildAttackContext().Hits.Sum(h=>h.Amount),231f,"Real noncritical attack context equals display calculation");
        Check(Value(panel,"Damage / Hit (Noncritical)")=="231","Open stats panel immediately refreshed on stat changes");
        var neutral=new GameObject("Neutral defender").AddComponent<StatsComponent>();
        Near(CombatCalculator.CalculateFinalDamage(player.BuildAttackContext(),stats,neutral),231f,"Actual combat mitigation pipeline with neutral target matches preview");Object.Destroy(neutral.gameObject);
        var random=UnityEngine.Random.state;float next=UnityEngine.Random.value;UnityEngine.Random.state=random;
        float unused=player.BasicAttackDamage;
        Check(UnityEngine.Random.value==next,"Preview does not consume critical/random rolls");
        Check(Value(panel,"Unarmed Damage")==null,"Zero unarmed stat hidden");
        stats.SetBaseStat(StatTypes.UnarmedDamage,7);Check(Value(panel,"Unarmed Damage")=="7","Nonzero unarmed appears immediately");stats.SetBaseStat(StatTypes.UnarmedDamage,0);
        Check(StatDisplayFormatting.ShouldDisplay(stats,StatTypes.Life),"Life remains a meaningful stat");
        var zero=new GameObject("Zero HP formatting").AddComponent<StatsComponent>();zero.SetBaseStat(StatTypes.Life,0);
        Check(StatDisplayFormatting.ShouldDisplay(zero,StatTypes.Life),"HP zero remains displayable");Object.Destroy(zero.gameObject);
        panel.SectionButton("Damage").onClick.Invoke();Check(!panel.IsSectionExpanded("Damage"),"Damage category collapses");
        var armour=Gear(LootManager.GearType.BodyArmours,Element.Phys,0);armour.globalRolledMods.Add(new RolledMod(StatTypes.GenericDmg,1,25));equipment.Equip(armour);
        Check(!panel.IsSectionExpanded("Damage") && Value(panel,"Damage / Hit (Noncritical)")=="264","Gear refresh preserves collapsed state and updates primary damage");
        xp.AddExperience(xp.RequiredXp);Check(xp.TrySpend(PassiveTreeDefinition.NodeId(PassiveBranch.Poison,0)),"Poison passive purchased with a deterministically earned point");
        Near(stats.GetRawStat(StatTypes.PoisonDmg),5f,"Poison passive updates only Poison damage");
        Near(player.BasicAttackDamage,264f,"Poison passive does not alter a physical hit");Check(Value(panel,"Damage / Hit (Noncritical)")=="264","Scoped passive refresh preserves unrelated hit damage");
        var other=Gear(LootManager.GearType.Weapons,Element.Fire,40);equipment.Equip(other);
        Near(player.BasicAttackDamage,118.8f,"Existing off-element flat contribution retained without adding a new system");
        Check(Value(panel,"Damage / Hit (Noncritical)")=="118.8","Weapon swap refreshes immediately");
        stats.SetBaseStat(StatTypes.PoisonChance,17);stats.SetBaseStat(StatTypes.PoisonDmg,30);
        Check(Value(panel,"Poison Chance")=="17%","Nonmatching weapon does not hide nonzero status chance");
        panel.SectionButton("Status Effects").onClick.Invoke();stats.SetBaseStat(StatTypes.PoisonChance,18);
        Check(!panel.IsSectionExpanded("Status Effects") && Value(panel,"Poison Chance")==null,"Status category stays collapsed through value refresh");
        panel.SectionButton("Status Effects").onClick.Invoke();Check(Value(panel,"Poison Chance")=="18%","Status category expands to current value");

        var inv=Inventory.Instance;int CurrencyTotal()=>EnemyDropTable.OrdinaryTypes().Sum(type=>CurrencyInventory.Instance.Count(type));
        foreach(var rarity in new[]{LootManager.GearRarity.Normal,LootManager.GearRarity.Magic,LootManager.GearRarity.Rare,LootManager.GearRarity.Legendary})
        {
            var g=Gear(LootManager.GearType.Rings,Element.Phys,0);g.Initialize(LootManager.GearType.Rings,rarity,1,Element.Phys);
            int beforeCurrency=CurrencyTotal();int amount=Inventory.ScrapYield(g);
            Check(inv.TryDismantle(g) && !inv.Items.Contains(g) && CurrencyTotal()==beforeCurrency+amount,rarity+" dismantle uses consolidated currency yield "+amount);
            Check(!inv.TryDismantle(g),rarity+" duplicate callback rejected");
        }
        Check(!inv.TryDismantle(other),"Equipped-item safeguard retained");

        hud.statsPanel.SetActive(false);equipment.Unequip(LootManager.GearType.BodyArmours);xp.ResetProgression();
        foreach(var t in new[]{StatTypes.FlatPhys,StatTypes.PhysDmg,StatTypes.GenericDmg,StatTypes.PhysMult,StatTypes.GenericMult,StatTypes.PoisonDmg})stats.SetBaseStat(t,0);
        foreach(var t in Chances)stats.SetBaseStat(t,100);
        var chill=Effect("Fixture Chill",StatusEffects.StatusType.Chill);var shock=Effect("Fixture Shock",StatusEffects.StatusType.Shock);
        typeof(BattleManager).GetField("chillEffect",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(BattleManager.Instance,chill);
        typeof(BattleManager).GetField("shockEffect",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(BattleManager.Instance,shock);
        var poison=AssetDatabase.LoadAssetAtPath<StatusEffects>("Assets/Prefabs/Scriptable Objects/PoisonStatus.asset");
        foreach(var element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Poison,Element.Void})
        {
            game.StartZone(1);var g=Gear(LootManager.GearType.Weapons,element,20);equipment.Equip(g);yield return null;
            Turn();var summaries=Enemy.GetComponent<StatusController>().GetStatusSummaries();
            string expected=element switch{Element.Phys=>"Bleed,Poison",Element.Fire=>"Burn",Element.Cold=>"Chill",Element.Light=>"Shock",Element.Poison=>"Poison",_=>""};
            Check(string.Join(",",summaries.Select(s=>s.DisplayName).OrderBy(s=>s))==expected,"Actual player turn "+element+" applies only "+(expected==""?"no status":expected)+" with all chances100%");
        }
        var context=new DamageContext(3);context.AddDamage(Element.Fire,100);context.AddDamage(Element.Poison,7);
        Near(AilmentCalculator.GetSourceHitDamage(poison,context),7,"Status source strength excludes unrelated damage");
        context.AddDamage(Element.Phys,5);Near(AilmentCalculator.GetSourceHitDamage(poison,context),12,"Poison source includes both Physical and Poison");
        game.StartZone(1);equipment.Equip(Gear(LootManager.GearType.Weapons,Element.Poison,20));yield return null;
        stats.SetBaseStat(StatTypes.PoisonChance,0);Turn();
        Check(Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Matching Poison damage still requires chance roll");
        stats.SetBaseStat(StatTypes.PoisonChance,100);
        Enemy.LoseLife(Enemy.CurrentLife-1);Turn();Check(Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Lethal matching hit cannot apply statuses to successor");yield return null;
        var old=Enemy;old.GetComponent<StatusController>().ApplyStatus(poison,1,100000,1,stats,1);Turn();
        Check(Enemy!=old && Enemy.CurrentLife==Enemy.MaxLife && Enemy.GetComponent<StatusController>().GetStatusSummaries().Count==0,"Lethal DOT still stops turn before replacement hit/status");
        yield return null;Check(errors==0,"No new gameplay errors during scoped fixture");

        foreach(var t in Chances)stats.SetBaseStat(t,17);
        stats.SetBaseStat(StatTypes.PhysDmg,50);stats.SetBaseStat(StatTypes.GenericDmg,25);stats.SetBaseStat(StatTypes.PoisonDmg,30);stats.SetBaseStat(StatTypes.PoisonMult,20);stats.SetBaseStat(StatTypes.PoisonDuration,2);
        stats.SetBaseStat(StatTypes.FlatArmour,40);equipment.Equip(weapon);
        hud.statsPanel.SetActive(true);panel.Refresh();
        if(!panel.IsSectionExpanded("Damage"))panel.SectionButton("Damage").onClick.Invoke();
        var legend=Gear(LootManager.GearType.Rings,Element.Phys,0);legend.Initialize(LootManager.GearType.Rings,LootManager.GearRarity.Legendary,1,Element.Phys);
        Write("Visual fixture paused: primary damage140; categories populated; all status chances17% visible regardless of Physical weapon. Dismantle rewards were recorded in the consolidated currency inventory. Exit Play after UI checks.");
    }
}
