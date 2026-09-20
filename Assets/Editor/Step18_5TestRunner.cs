using System;
using System.IO;
using System.Linq;
using UnityEditor;

public static class Step18_5TestRunner
{
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep18_5Windows);
    public static void RunValidation()=>Run(()=>
    {
        if(GamePersistence.SchemaVersion!=10)throw new InvalidOperationException("Step 18.5 must not change schema 10.");
        if(WeaponTypeCatalog.All.Count!=6||CurrencyLootTable.Entries.Count<12)throw new InvalidOperationException("Step 18.5 production tables are incomplete.");
        if(!CurrencyLootTable.Entries.Any(x=>CurrencyInventory.IsAncient(x.Currency)))throw new InvalidOperationException("Ancient currency has no gated production source.");
        ItemizationValidationRunner.RunStep14_5();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step18_5Validation.txt","PASS\nschema=10\nailmentResolver=centralized\nweaponTypes=6\ncurrencyEntries="+CurrencyLootTable.Entries.Count+"\nBalanceLab=NOT RUN\n");
    });
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){UnityEngine.Debug.LogException(ex);EditorApplication.Exit(1);}}
}
