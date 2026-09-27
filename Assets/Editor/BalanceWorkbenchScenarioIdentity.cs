using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public enum CombatLevelSweepPolicy
    {
        MatchPlayerLevel,
        Fixed,
        OffsetFromPlayerLevel,
        AdvanceFromStart
    }

    public static class ScenarioLevelPolicy
    {
        public static int CombatLevel(CombatLevelSweepPolicy policy,int playerLevel,
            int firstPlayerLevel,int selectedCombatLevel,int offset) => Mathf.Clamp(
            policy switch
            {
                CombatLevelSweepPolicy.MatchPlayerLevel=>playerLevel,
                CombatLevelSweepPolicy.Fixed=>selectedCombatLevel,
                CombatLevelSweepPolicy.OffsetFromPlayerLevel=>playerLevel+offset,
                CombatLevelSweepPolicy.AdvanceFromStart=>selectedCombatLevel+playerLevel-firstPlayerLevel,
                _=>throw new ArgumentOutOfRangeException(nameof(policy))
            },1,360);

        public static string Description(CombatLevelSweepPolicy policy)=>policy switch
        {
            CombatLevelSweepPolicy.MatchPlayerLevel=>"Combat Level matches each swept Player Level.",
            CombatLevelSweepPolicy.Fixed=>"Combat Level stays at the selected fixed value.",
            CombatLevelSweepPolicy.OffsetFromPlayerLevel=>"Combat Level is each Player Level plus the selected offset.",
            CombatLevelSweepPolicy.AdvanceFromStart=>"LEGACY: selected Combat Level is the first point and advances with Player Level.",
            _=>"Unknown Combat Level policy."
        };
    }

    public sealed class ScenarioInspection
    {
        public string resultId,gearHash,passiveHash,evaluationHash,fingerprint;
        public string profile,passiveAlgorithm,skillPolicy;
        public PlayerBuildSnapshot build;
        public PlayerBuildMetrics metrics;
        public OptimizationObjective objective;
        public List<BuildAblationContribution> contributions;
    }

    // Digests are integrity identifiers, not balance scores. A selected point is
    // re-evaluated from its own captured build and objective, never from UI controls.
    public static class ScenarioResultIdentity
    {
        public static string Hash(string value)
        {
            using var sha=SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value??"")))
                .Replace("-",string.Empty).Substring(0,16);
        }
        public static string BuildHash(PlayerBuildSnapshot build)=>Hash(JsonUtility.ToJson(build));
        public static string GearHash(PlayerBuildSnapshot build)=>Hash(string.Join("|",
            (build.equipment??new()).OrderBy(x=>x.slot).Select(x=>JsonUtility.ToJson(x))));
        public static string PassiveHash(PlayerBuildSnapshot build)=>Hash(string.Join("|",
            (build.passiveStableIds??new()).OrderBy(x=>x,StringComparer.Ordinal)));
        public static string MetricsHash(PlayerBuildMetrics metrics)=>Hash(JsonUtility.ToJson(metrics));

        public static void Stamp(PlayerCurvePoint point)
        {
            point.gearHash=GearHash(point.build);
            point.passiveHash=PassiveHash(point.build);
            point.evaluationHash=MetricsHash(point.metrics);
            point.buildHash=BuildHash(point.build);
            point.resultId=ResultId(point);
        }

        static string ResultId(PlayerCurvePoint point)=>Hash(string.Join("|",point.dataFingerprint,point.profileGuid,
                point.profileVersion,point.playerLevel,point.combatLevel,point.seed,
                JsonUtility.ToJson(point.objective),point.optimizePassives,point.progressive,
                point.freeRespec,point.passiveBeamWidth,point.buildHash,
                point.evaluationHash));

        public static ScenarioInspection Inspect(PlayerCurvePoint point)
        {
            if(point==null||point.build==null||point.metrics==null||point.objective==null)
                throw new InvalidOperationException("Scenario point has no complete result snapshot.");
            if(point.dataFingerprint!=ProductionBalanceAdapters.DataFingerprint())
                throw new InvalidOperationException("SCENARIO RESULT STALE: production data changed; rerun the sweep before inspecting contributions.");
            if(point.buildHash!=BuildHash(point.build)||point.gearHash!=GearHash(point.build)||
                point.passiveHash!=PassiveHash(point.build)||
                point.evaluationHash!=MetricsHash(point.metrics)||point.resultId!=ResultId(point))
                throw new InvalidOperationException("SCENARIO RESULT INTEGRITY FAILURE: stored build, gear, passives or metrics changed after capture.");
            var evaluated=PlayerBuildEvaluator.Evaluate(point.build);
            if(MetricsHash(evaluated)!=point.evaluationHash)
                throw new InvalidOperationException("SCENARIO RESULT INTEGRITY FAILURE: displayed metrics do not match the stored build evaluation.");
            var build=point.build.Clone();
            var objective=JsonUtility.FromJson<OptimizationObjective>(JsonUtility.ToJson(point.objective));
            return new ScenarioInspection
            {
                resultId=point.resultId,gearHash=point.gearHash,
                passiveHash=point.passiveHash,evaluationHash=point.evaluationHash,
                fingerprint=point.dataFingerprint,profile=point.profile,
                passiveAlgorithm=point.passiveAlgorithm,skillPolicy=point.metrics.selectedSkillPolicy,
                build=build,metrics=evaluated.Clone(),objective=objective,
                contributions=PlayerBuildContributionAnalyzer.Analyze(build,objective)
            };
        }
    }
}
