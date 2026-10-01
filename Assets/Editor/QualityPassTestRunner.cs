using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.Build.Reporting;
using System;
using UnityEngine;

public static class QualityPassTestRunner
{
    static TestRunnerApi api;
    static Results callback;
    public static void BuildWindows()
    {
        try
        {
            const string destination="Builds/QualityPassWindows/BlackCube.exe";
            Directory.CreateDirectory("Builds/QualityPassWindows");Directory.CreateDirectory("Logs/QualityPass");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled),s=>s.path),
                locationPathName=destination,target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.None});
            File.WriteAllText("Logs/QualityPass/windows-build.txt",$"result={report.summary.result}\nerrors={report.summary.totalErrors}\noutput={destination}\n");
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    public static void RunFocused()
    {
        Directory.CreateDirectory("Logs/QualityPass");
        api=ScriptableObject.CreateInstance<TestRunnerApi>();
        callback=new Results();api.RegisterCallbacks(callback);
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,
            testNames=new[]{"QualityPassTests"}}));
    }
    sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun){}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result)
        {if(result.TestStatus.ToString()=="Failed")Debug.LogError($"QUALITY TEST FAILED {result.Name}: {result.Message}\n{result.StackTrace}");}
        public void RunFinished(ITestResultAdaptor result)
        {
            File.WriteAllText("Logs/QualityPass/focused-tests.txt",$"result={result.TestStatus}\npassed={result.PassCount}\nfailed={result.FailCount}\nskipped={result.SkipCount}\n");
            EditorApplication.Exit(result.FailCount==0?0:1);
        }
    }
}
