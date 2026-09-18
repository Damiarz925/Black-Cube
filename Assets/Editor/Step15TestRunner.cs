using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class Step15TestRunner
{
    static ResultCallbacks callbacks;
    public static void RunFocused()=>Run("Step15WorldProgressionTests","Logs/Step15-focused-tests.txt");
    public static void RunAll()=>Run(null,"Logs/Step15-editmode-tests.txt");
    public static void RunWorldValidation()=>RunAndExit(WorldContentValidationRunner.ValidateReferenceContent);
    public static void RunReferences()=>RunAndExit(BaselineVerificationRunner.ValidateReferences);
    public static void BuildWindows()=>RunAndExit(BaselineVerificationRunner.BuildStep15Windows);

    static void RunAndExit(Action action)
    {
        try{action();EditorApplication.Exit(0);}
        catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
    }

    static void Run(string testName,string reportPath)
    {
        Directory.CreateDirectory("Logs");callbacks=new ResultCallbacks(reportPath);
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(callbacks);
        var filter=new Filter{testMode=TestMode.EditMode};if(!string.IsNullOrWhiteSpace(testName))filter.testNames=new[]{testName};
        api.Execute(new ExecutionSettings(filter));
    }

    sealed class ResultCallbacks:ICallbacks
    {
        readonly string reportPath;public ResultCallbacks(string path)=>reportPath=path;
        public void RunStarted(ITestAdaptor testsToRun){}public void TestStarted(ITestAdaptor test){}public void TestFinished(ITestResultAdaptor result){}
        public void RunFinished(ITestResultAdaptor result)
        {
            File.WriteAllText(reportPath,$"result={result.TestStatus}\npassed={result.PassCount}\nfailed={result.FailCount}\nskipped={result.SkipCount}\nduration={result.Duration:0.###}\n");
            EditorApplication.Exit(result.FailCount==0?0:1);
        }
    }
}
