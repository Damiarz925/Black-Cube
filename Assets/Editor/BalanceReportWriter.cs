using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BlackCube
{
    public static class BalanceReportWriter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private static readonly string[] Metrics =
        {
            "intrinsicLife", "damageFactor", "intrinsicArmour", "intrinsicResistance",
            "finalLife", "outgoingHit", "expectedDps", "attackSpeed", "critChance",
            "critMultiplier", "armour", "fireRes", "coldRes", "lightRes", "voidRes",
            "hitTwice", "shockCapability", "chillCapability", "optimizerOffense",
            "optimizerDefense", "rarity", "slotCount", "modifierCount", "hitsToKill",
            "secondsToKill", "hitsToDeath", "secondsToDeath", "physicalReference",
            "fireReference", "coldReference", "lightningReference", "voidReference", "poisonVoidTick"
        };

        public static void Write(BalanceResult result)
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string root = Path.GetFullPath(Path.Combine(project, result.config.outputDirectory));
            if (!root.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Balance output must remain within the project workspace");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Step11_12_EnemyScaling.csv"), Csv(result));
            File.WriteAllText(Path.Combine(root, "Step11_12_EnemyScaling.json"), JsonUtility.ToJson(result, true));
            File.WriteAllText(Path.Combine(root, "Step11_12_BalanceSummary.md"), Markdown(result));
        }

        private static string Csv(BalanceResult result)
        {
            FieldInfo[] fields = typeof(BalanceRow).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var output = new StringBuilder();
            output.AppendLine(string.Join(",", fields.Select(field => field.Name)));
            foreach (BalanceRow row in result.rows)
                output.AppendLine(string.Join(",", fields.Select(field => Escape(field.GetValue(row)))));
            return output.ToString();
        }

        private static string Escape(object value)
        {
            string text = Convert.ToString(value, Culture) ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        public static string Markdown(BalanceResult result)
        {
            var output = new StringBuilder();
            output.AppendLine("# Step 11 + 12 enemy scaling baseline").AppendLine();
            output.AppendLine($"Seed {result.config.seed}; {result.config.sampleCount} generated builds per level/archetype; "
                + $"runtime {result.elapsedSeconds:F1}s. Profile: Assets/Resources/EnemyScalingProfile.asset.").AppendLine();
            output.AppendLine(result.warning).AppendLine();
            output.AppendLine($"Captured starter: Life {result.playerLevelOneLife:F1}, noncritical hit "
                + $"{result.playerLevelOneHit:F2}, attack speed {result.playerLevelOneAttackSpeed:F2}/s.").AppendLine();
            output.AppendLine("Reference player Life/damage factor: 1.035^(level-1) through 100; "
                + "1.035^99 × 1.018^(level-100) afterward. Reference Armour +6/level and "
                + "Fire/Cold/Lightning/Void resistance +0.25 points/level, capped at 30 points. "
                + "These are comparison assumptions, not gameplay settings.").AppendLine();
            output.AppendLine("TTK/TTD use real hit mitigation and expected basic-hit DPS; they exclude "
                + "ailment ramp and active skills, so they are shape diagnostics rather than final targets.").AppendLine();
            foreach (var group in result.rows.GroupBy(row => (row.archetype, row.level))
                         .OrderBy(group => group.Key.archetype).ThenBy(group => group.Key.level))
            {
                BalanceRow first = group.First();
                BalanceRow[] rows = group.ToArray();
                output.AppendLine($"## {group.Key.archetype} — level {group.Key.level}").AppendLine();
                output.AppendLine($"Authored Life {first.authoredLife:F1}; intrinsic Life factor {first.lifeFactor:F3}; "
                    + $"Damage factor {first.damageFactor:F3}; intrinsic Armour +{first.intrinsicArmour:F1}; "
                    + $"elemental/Void resistance +{first.intrinsicResistance:F2} points.").AppendLine();
                output.AppendLine("| Metric | Min | P10 | P50 | P90 | Max | Mean |");
                output.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");
                foreach (string metric in Metrics)
                {
                    double[] values = rows.Select(row => Convert.ToDouble(typeof(BalanceRow)
                        .GetField(metric).GetValue(row), Culture)).OrderBy(value => value).ToArray();
                    output.AppendLine($"| {metric} | {Fmt(values[0])} | {Fmt(Percentile(values, .1))} | "
                        + $"{Fmt(Percentile(values, .5))} | {Fmt(Percentile(values, .9))} | "
                        + $"{Fmt(values[values.Length - 1])} | {Fmt(values.Average())} |");
                }
                output.AppendLine();
                output.AppendLine($"Dominant weapon element: {Top(rows.Select(row => row.dominantElement))}. "
                    + $"Approximate equipment-signature diversity: {rows.Select(row => row.equipmentSignature).Distinct().Count()}/{rows.Length}. "
                    + $"Top affixes: {Top(rows.SelectMany(row => row.affixSignature.Split('|')))}.");
                string rarityMix = string.Join(", ", rows.GroupBy(row => row.rarity)
                    .OrderBy(g => g.Key).Select(g => $"{(EnemyAI.EnemyRarity)g.Key}={g.Count()}"));
                string slots = string.Join(", ", rows.SelectMany(row => row.equipmentSignature.Split('|'))
                    .Where(slot => slot.Length > 0).Select(slot => slot.Split(':')[0])
                    .GroupBy(slot => slot).OrderByDescending(g => g.Count()).Take(5)
                    .Select(g => $"{g.Key}={g.Count()}"));
                output.AppendLine($"Rarity mix: {rarityMix}. Slots: {slots}.").AppendLine();
                BalanceRow[] duels = rows.Where(row => row.sample < 25).ToArray();
                double[] durations = duels.Select(row => (double)row.monteCarloDuration)
                    .OrderBy(value => value).ToArray();
                output.AppendLine($"Seeded snapshot duels ({duels.Length}): reference-player wins "
                    + $"{duels.Count(row => row.monteCarloWinner == 1)}, enemy wins "
                    + $"{duels.Count(row => row.monteCarloWinner == -1)}, horizon/draw "
                    + $"{duels.Count(row => row.monteCarloWinner == 0)}; median duration "
                    + $"{Fmt(Percentile(durations, .5))}s; ailment ticks "
                    + $"{duels.Sum(row => row.monteCarloAilmentTicks)}, Shock triggers "
                    + $"{duels.Sum(row => row.monteCarloShockTriggers)}, Chill applications "
                    + $"{duels.Sum(row => row.monteCarloChillApplications)}.").AppendLine();
            }
            return output.ToString();
        }

        private static string Top(IEnumerable<string> values) => string.Join(", ", values
            .Where(value => !string.IsNullOrEmpty(value)).GroupBy(value => value)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key)
            .Take(5).Select(group => $"{group.Key}={group.Count()}"));

        private static double Percentile(double[] values, double quantile)
        {
            double index = quantile * (values.Length - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);
            return values[lower] + (values[upper] - values[lower]) * (index - lower);
        }

        private static string Fmt(double value) => value.ToString("F2", Culture);
    }
}
