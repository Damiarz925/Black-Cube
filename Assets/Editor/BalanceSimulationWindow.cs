using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube
{
    public sealed class BalanceSimulationWindow : EditorWindow
    {
        private int seed = 11012;
        private int sampleCount = 250;
        private string levels = "1,10,25,50,75,100,150,200,300";
        private string outputDirectory = "Logs/Balance";

        [MenuItem("Black Cube/Balance/Open Balance Lab")]
        public static void Open() => GetWindow<BalanceSimulationWindow>("Balance Lab");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Deterministic enemy-build baseline", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Seed", seed);
            sampleCount = EditorGUILayout.IntField("Samples per level/archetype", sampleCount);
            levels = EditorGUILayout.TextField("Levels (comma-separated)", levels);
            outputDirectory = EditorGUILayout.TextField("Project-relative output", outputDirectory);
            EditorGUILayout.HelpBox("Reference player values are synthetic placeholder comparison values and are not the game's final player progression model.",
                MessageType.Info);
            if (GUILayout.Button("Run configured")) Run(sampleCount);
            if (GUILayout.Button("Run quick (25)")) Run(25);
            if (GUILayout.Button("Run baseline (250)")) Run(250);
            if (GUILayout.Button("Open last report"))
            {
                string file = Path.Combine(Application.dataPath, "..", outputDirectory,
                    "Step11_12_BalanceSummary.md");
                if (File.Exists(file)) EditorUtility.RevealInFinder(file);
                else Debug.LogWarning($"Balance report not found: {file}");
            }
        }

        private void Run(int samples)
        {
            try
            {
                int[] parsed = levels.Split(',').Select(value => int.Parse(value.Trim())).ToArray();
                var config = new BalanceSimulationConfig
                {
                    seed = seed, sampleCount = samples, levels = parsed,
                    outputDirectory = outputDirectory
                };
                BalanceResult result = BalanceSimulationRunner.Run(config);
                BalanceReportWriter.Write(result);
                Debug.Log($"Balance Lab: {result.rows.Length} rows in {result.elapsedSeconds:F1}s");
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
