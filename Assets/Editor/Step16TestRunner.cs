using System;
using UnityEditor;

public static class Step16TestRunner
{
    public static void RunValidation()=>Run(()=>{ItemizationValidationRunner.RunStep14_5();BaselineVerificationRunner.ValidateReferences();});
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep16Windows);
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception exception){UnityEngine.Debug.LogException(exception);EditorApplication.Exit(1);}}
}
