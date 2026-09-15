using System;
using System.Linq;
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

        public static BalanceSimulationConfig FromCommandLine(int defaultSamples = 250)
        {
            var config = new BalanceSimulationConfig { sampleCount = defaultSamples };
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-balanceSeed": config.seed = int.Parse(args[++i]); break;
                    case "-balanceSamples": config.sampleCount = int.Parse(args[++i]); break;
                    case "-balanceLevels": config.levels = args[++i].Split(',')
                        .Select(value => int.Parse(value.Trim())).ToArray(); break;
                    case "-balanceOutput": config.outputDirectory = args[++i]; break;
                }
            }
            config.Validate();
            return config;
        }
    }
}
