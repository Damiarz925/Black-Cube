using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Command-line-safe EditMode runner for Step 14.5 verification.</summary>
public static class Step14_5TestRunner
{
    static ResultCallbacks callbacks;

    public static void RunFocused() => Run("Step14_5FoundationTests", "Logs/Step14_5-focused-tests.txt");
    public static void RunAll() => Run(null, "Logs/Step14_5-editmode-tests.txt");
    public static void RunItemization() => RunAndExit(ItemizationValidationRunner.RunStep14_5);
    public static void RunReferences() => RunAndExit(BaselineVerificationRunner.ValidateReferences);
    public static void RunValidation() => RunAndExit(() => { ItemizationValidationRunner.RunStep14_5(); BaselineVerificationRunner.ValidateReferences(); });
    public static void BuildWindows() => RunAndExit(BaselineVerificationRunner.BuildStep14_5Windows);

    static void RunAndExit(Action action)
    {
        try { action(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    static void Run(string testName, string reportPath)
    {
        Directory.CreateDirectory("Logs");
        callbacks = new ResultCallbacks(reportPath);
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(callbacks);
        var filter = new Filter { testMode = TestMode.EditMode };
        if (!string.IsNullOrWhiteSpace(testName)) filter.testNames = new[] { testName };
        api.Execute(new ExecutionSettings(filter));
    }

    sealed class ResultCallbacks : ICallbacks
    {
        readonly string reportPath;
        public ResultCallbacks(string path) => reportPath = path;
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            string output = $"result={result.TestStatus}\npassed={result.PassCount}\nfailed={result.FailCount}\nskipped={result.SkipCount}\nduration={result.Duration:0.###}\n";
            File.WriteAllText(reportPath, output);
            EditorApplication.Exit(result.FailCount == 0 ? 0 : 1);
        }
    }
}
