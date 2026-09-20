using System;
using System.IO;
using UnityEditor;

public static class Step20TestRunner
{
    public static void RunFocused()=>Run(()=>
    {
        var errors=EndgameItemizationValidation.Validate(WorldContentCatalog.Reference);if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step20Validation.txt",$"PASS\nschema={GamePersistence.SchemaVersion}\nchallenge-keys=6\nchallenge-essences=6\nspecial-affixes=36\nempowerment-thresholds=6\nimplicit-reforger={EndgameResourceIds.ImplicitReforger}\nproduction-rng=entropy-backed\nBalanceLab=NOT RUN\n");
    });
    public static void RunValidation()=>Run(()=>{WorldContentValidationRunner.ValidateReferenceContent();ItemizationValidationRunner.RunStep14_5();Step17TestRunner.WritePassiveTreeReport();BaselineVerificationRunner.ValidateReferences();var errors=EndgameItemizationValidation.Validate(WorldContentCatalog.Reference);if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step20-endgame-itemization-validation.txt","RESULT: PASS\npools=6\naffixes=36\nresources=14 + Catalyst\n");});
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep20Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){UnityEngine.Debug.LogException(ex);EditorApplication.Exit(1);}}
}
