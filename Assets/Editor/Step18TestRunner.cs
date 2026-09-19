using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Step18TestRunner
{
    public static void RunValidation()=>Run(()=>
    {
        if(GamePersistence.SchemaVersion!=10)throw new InvalidOperationException("Step 18 requires schema 10.");
        if(PassiveTreeDefinition.NodeCount!=366)throw new InvalidOperationException("Passive Tree V2 topology changed.");
        if(SubclassCatalog.All.Count!=12||PlayerSkillDefinition.CreateProductionDefaults().Count(x=>!string.IsNullOrEmpty(x.stableId))!=12)throw new InvalidOperationException("Production catalog count mismatch.");
        foreach(var weapon in WeaponTypeCatalog.All)if(WeaponSkillBindings.For(weapon.Id).Count!=2)throw new InvalidOperationException("Weapon skill binding mismatch: "+weapon.Id);
        Step17TestRunner.WritePassiveTreeReport();
        ItemizationValidationRunner.RunStep14_5();
        WorldContentValidationRunner.ValidateReferenceContent();
        BaselineVerificationRunner.ValidateReferences();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step18Validation.txt","PASS\nschema=10\npassiveNodes=366\nskills=12\nsubclasses=12\nBalanceLab=NOT RUN\n");
    });
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep18Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
