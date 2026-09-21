using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using UnityEditor;
using UnityEngine;

public static class Tooling1ValidationRunner
{
    public static void RunValidators()=>Run(()=>
    {
        var tooling=BalanceWorkbenchValidation.Validate();if(tooling.Length>0)throw new InvalidOperationException(string.Join("\n",tooling));
        ItemizationValidationRunner.RunStep14_5();Step17TestRunner.WritePassiveTreeReport();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Tooling1Validation.txt",$"PASS\nschema={GamePersistence.SchemaVersion}\nproduction-rng={LootRandomSourceFactory.CreateProduction().GetType().Name}\nweapons={WeaponTypeCatalog.All.Count}\nenemies={WorldContentCatalog.Reference.enemyArchetypes.Count}\ncurrencies={CurrencyLootTable.Entries.Count}\ndata={ProductionBalanceAdapters.DataFingerprint()}\nBalanceLab=NOT RUN\n");
    });
    public static void RunSmoke()=>Run(()=>
    {
        Directory.CreateDirectory("Logs");var lines=new List<string>{"TOOLING 1 EDITOR SMOKE"};BalanceWorkbenchWindow.Open();
        var itemRequest=new ItemLabRequest{sampleCount=1000,itemLevel=50,rarity=LootManager.GearRarity.Rare,weaponTypeId=WeaponTypeIds.Dagger,seed=1101};var items=ProductionBalanceAdapters.RunItems(itemRequest);lines.Add($"PASS weapon lab 1000: P50={items.weaponDps.p50:0.###} P90={items.weaponDps.p90:0.###}");
        var repeat=ProductionBalanceAdapters.RunItems(itemRequest);if(!items.samples.Select(x=>x.detail).SequenceEqual(repeat.samples.Select(x=>x.detail)))throw new InvalidOperationException("Item rerun changed with same seed.");lines.Add("PASS same-seed item rerun");
        var enemyRequest=new EnemyLabRequest{sampleCount=1000,level=80,productionRarity=true,seed=1201};var enemies=ProductionBalanceAdapters.RunEnemies(enemyRequest);lines.Add($"PASS enemy lab 1000: P50 life={enemies.life.p50:0.###} P90 DPS={enemies.dps.p90:0.###}");
        enemyRequest.sampleCount=100;enemyRequest.forcePrimaryDamage=true;enemyRequest.primaryDamage=Element.Light;var lightning=ProductionBalanceAdapters.RunEnemies(enemyRequest);lines.Add($"PASS forced Lightning enemy sample: P50 gear={lightning.gearScore.p50:0.###}");
        var dropRequest=new DropLabRequest{sampleCount=10000,level=80,rarity=EnemyAI.EnemyRarity.Normal,seed=1301};var drops=ProductionBalanceAdapters.RunDrops(dropRequest);lines.Add($"PASS drop 10000 normal: gear={drops.averageGearItems:0.###} currency={drops.averageCurrencyRolls:0.###}");
        foreach(EnemyAI.EnemyRarity rarity in Enum.GetValues(typeof(EnemyAI.EnemyRarity))){dropRequest.rarity=rarity;dropRequest.sampleCount=2000;ProductionBalanceAdapters.RunDrops(dropRequest);}dropRequest.rarity=EnemyAI.EnemyRarity.Legendary;dropRequest.boss=true;ProductionBalanceAdapters.RunDrops(dropRequest);lines.Add("PASS rarity matrix");
        for(int level=1;level<=360;level+=30){dropRequest.level=level;dropRequest.sampleCount=1000;ProductionBalanceAdapters.RunDrops(dropRequest);}lines.Add("PASS drop level sweep");
        var curves=ProductionBalanceAdapters.IntrinsicCurves(1,360,1);if(curves.Any(x=>x.points.Count!=360))throw new InvalidOperationException("Curve sweep incomplete.");lines.Add("PASS intrinsic curve 1-360 and table data");
        string csv=WorkbenchExports.SaveItemCsv(items);if(!File.Exists(csv))throw new InvalidOperationException("CSV export missing.");lines.Add("PASS CSV export: "+csv);
        Directory.CreateDirectory("Library/BlackCubeBalance");string preset="Library/BlackCubeBalance/Tooling1SmokePreset.json";File.WriteAllText(preset,JsonUtility.ToJson(itemRequest,true));var loaded=JsonUtility.FromJson<ItemLabRequest>(File.ReadAllText(preset));if(loaded.seed!=itemRequest.seed)throw new InvalidOperationException("Preset reload failed.");lines.Add("PASS preset save/reload");
        File.WriteAllLines("Logs/Tooling1EditorSmoke.txt",lines);Debug.Log(string.Join("\n",lines));
    });
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildTooling1Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
