using System;
using UnityEngine;

namespace BlackCube
{
    [Serializable]
    public sealed class BalanceSimulationConfig
    {
        public int seed = 11012;
        public int sampleCount = 250;
        public int[] levels = { 1, 10, 25, 50, 75, 100, 150, 200, 300 };
        public string outputDirectory = "Logs/Balance";

        public void Validate()
        {
            sampleCount = Mathf.Clamp(sampleCount, 1, 1000);
            if (levels == null || levels.Length == 0) levels = new[] { 1 };
            for (int i = 0; i < levels.Length; i++) levels[i] = Mathf.Max(1, levels[i]);
            if (string.IsNullOrWhiteSpace(outputDirectory)) outputDirectory = "Logs/Balance";
        }
    }
}
