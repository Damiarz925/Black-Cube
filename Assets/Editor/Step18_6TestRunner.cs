using System;
using System.IO;
using UnityEditor;

public static class Step18_6TestRunner
{
    public static void RunValidation()=>Run(()=>
    {
        if(GamePersistence.SchemaVersion!=10)throw new InvalidOperationException("Step 18.6 must not change schema 10.");
        if(LootRandomSourceFactory.CreateProduction() is not ProductionLootRandomSource)throw new InvalidOperationException("Production loot RNG factory is not entropy-backed.");
        if(!UnityEngine.Mathf.Approximately(PlayerLevelDamageProfile.IncreasedDamage(100),.2475f))throw new InvalidOperationException("Player level damage formula drifted.");
        ItemizationValidationRunner.RunStep14_5();Step17TestRunner.WritePassiveTreeReport();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step18_6Validation.txt","PASS\nschema=10\nlootRng=cryptographic-per-death\nweaponAttributeScaling=centralized\nlevelDamageAt100=24.75%\nBalanceLab=NOT RUN\n");
    });
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep18_6Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){UnityEngine.Debug.LogException(ex);EditorApplication.Exit(1);}}
}
