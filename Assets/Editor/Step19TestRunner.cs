using System;
using System.IO;
using UnityEditor;

public static class Step19TestRunner
{
    public static void RunFocused()=>Run(()=>
    {
        if(GamePersistence.SchemaVersion!=10)throw new InvalidOperationException("Step 19 must preserve schema 10.");
        var errors=WorldContentValidation.Validate(WorldContentCatalog.Reference);
        if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        int resolved=0;for(int level=1;level<=360;level++)for(int stage=1;stage<=10;stage++){if(WorldProgression.Resolve(level,stage).Encounter==null)throw new InvalidOperationException($"Unresolved {level}/{stage}");resolved++;}
        WorldContentValidationRunner.ValidateReferenceContent();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Step19Validation.txt",$"PASS\nschema=10\nstructural-stage-resolutions={resolved}\nnormal-enemies=48\nmain-bosses=60\nchallenge-bosses=6\nBalanceLab=NOT RUN\n");
    });
    public static void RunValidation()=>Run(()=>{WorldContentValidationRunner.ValidateReferenceContent();ItemizationValidationRunner.RunStep14_5();Step17TestRunner.WritePassiveTreeReport();BaselineVerificationRunner.ValidateReferences();});
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep19Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){UnityEngine.Debug.LogException(ex);EditorApplication.Exit(1);}}
}
