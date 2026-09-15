using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BlackCube
{
    [Serializable]
    public sealed class BalanceRow
    {
        public string archetype;
        public int level;
        public int sample;
        public float authoredLife, lifeFactor, intrinsicLife, damageFactor, intrinsicArmour, intrinsicResistance;
        public float finalLife, outgoingHit, expectedDps, attackSpeed, critChance, critMultiplier;
        public float armour, fireRes, coldRes, lightRes, voidRes, hitTwice;
        public float optimizerOffense, optimizerDefense, referencePlayerLife, referencePlayerHit;
        public float hitsToKill, secondsToKill, hitsToDeath, secondsToDeath;
        public float physicalReference, fireReference, coldReference, lightningReference, voidReference, poisonVoidTick;
        public float monteCarloDuration;
        public int monteCarloWinner, monteCarloHits, monteCarloAilmentTicks, monteCarloShockTriggers, monteCarloChillApplications;
        public int shockCapability, chillCapability, rarity, slotCount, modifierCount;
        public string dominantElement, equipmentSignature, affixSignature;
    }

    [Serializable]
    public sealed class BalanceResult
    {
        public string warning = "Reference player values are synthetic placeholder comparison values and are not the game's final player progression model.";
        public BalanceSimulationConfig config;
        public float playerLevelOneLife, playerLevelOneHit, playerLevelOneAttackSpeed;
        public string[] archetypes;
        public BalanceRow[] rows;
        public double elapsedSeconds;
    }

    // Editor-only, isolated production gear/build sampling. The global Unity RNG is scoped
    // solely because legacy production rolls use UnityEngine.Random; all state is restored.
    public static class BalanceSimulationRunner
    {
        private const string DatabasePath = "Assets/Prefabs/Scriptable Objects/ModDatabase.asset";
        private static readonly string[] PrefabRoots = { "Assets/Prefabs/PaperBattle" };

        [MenuItem("Black Cube/Balance/Run Baseline Simulation")]
        public static void RunBaseline()
        {
            var config = new BalanceSimulationConfig();
            config.Validate();
            BalanceResult result = Run(config);
            BalanceReportWriter.Write(result);
            UnityEngine.Debug.Log($"Balance baseline: {result.rows.Length} rows in {result.elapsedSeconds:F1}s, output {config.outputDirectory}");
        }

        [MenuItem("Black Cube/Balance/Run Quick Simulation")]
        public static void RunQuick()
        {
            var config = new BalanceSimulationConfig { sampleCount = 25 };
            BalanceResult result = Run(config);
            BalanceReportWriter.Write(result);
            UnityEngine.Debug.Log($"Balance quick: {result.rows.Length} rows in {result.elapsedSeconds:F1}s");
        }

        [MenuItem("Black Cube/Balance/Rewrite Last Summary")]
        public static void RewriteLastSummary()
        {
            string file = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs",
                "Balance", "Step11_12_EnemyScaling.json"));
            if (!File.Exists(file)) throw new FileNotFoundException("Run the balance simulation first", file);
            var result = JsonUtility.FromJson<BalanceResult>(File.ReadAllText(file));
            BalanceReportWriter.Write(result);
            UnityEngine.Debug.Log("Balance summary rewritten from last deterministic JSON result");
        }

        public static BalanceResult Run(BalanceSimulationConfig config)
        {
            config.Validate();
            UnityEngine.Random.State savedRandomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(config.seed);
            var clock = Stopwatch.StartNew();
            var created = new List<UnityEngine.Object>();
            try
            {
                ModManager roller = PrepareRoller(created);
                PrepareStatPools(created);
                var player = PrepareStarterPlayer(created);
                var playerStats = player.GetComponent<StatsComponent>();
                var controller = player.GetComponent<PlayerController>();
                var playerContext = controller.BuildNonCriticalAttackContext();
                float playerHitSeed = Sum(playerContext);
                float playerLifeSeed = playerStats.GetStat(StatTypes.Life);
                float playerSpeedSeed = controller.GetFinalAttackSpeed();
                var reference = new GameObject("Balance reference target", typeof(StatsComponent));
                created.Add(reference);
                StatsComponent referenceStats = reference.GetComponent<StatsComponent>();
                var prefabs = DiscoverEnemies();
                if (prefabs.Count == 0) throw new InvalidOperationException("No first-party enemy prefabs found");
                var rows = new List<BalanceRow>(prefabs.Count * config.levels.Length * config.sampleCount);
                var poison = ScriptableObject.CreateInstance<StatusEffects>();
                created.Add(poison);
                poison.ConfigureRuntime("Balance Poison", StatusEffects.StatusType.DamageOverTime,
                    StatusEffects.AilmentKind.Poison, ElementMask.All, .1f, 2, 100,
                    StatusEffects.StackPolicy.StackAndRefresh, 4);
                var bleed = ScriptableObject.CreateInstance<StatusEffects>();
                created.Add(bleed);
                bleed.ConfigureRuntime("Balance Bleed", StatusEffects.StatusType.DamageOverTime,
                    StatusEffects.AilmentKind.Bleed, ElementMask.Phys, .5f, 2, 4,
                    StatusEffects.StackPolicy.StackAndRefresh, 1);
                var ignite = ScriptableObject.CreateInstance<StatusEffects>();
                created.Add(ignite);
                ignite.ConfigureRuntime("Balance Ignite", StatusEffects.StatusType.DamageOverTime,
                    StatusEffects.AilmentKind.Ignite, ElementMask.Fire, .8f, 2, 1,
                    StatusEffects.StackPolicy.ReplaceIfStronger, 2);
                var ailments = new[] { poison, bleed, ignite };
                foreach (int level in config.levels)
                {
                    ConfigureReference(referenceStats, playerLifeSeed, level);
                    float playerFactor = ReferenceFactor(level);
                    DamageContext scaledPlayer = CopyAndScale(playerContext, playerFactor);
                    foreach (GameObject prefab in prefabs)
                    {
                        for (int sample = 0; sample < config.sampleCount; sample++)
                        {
                            GameObject actor = UnityEngine.Object.Instantiate(prefab);
                            actor.hideFlags = HideFlags.HideAndDontSave;
                            try
                            {
                                EnemyAI ai = actor.GetComponent<EnemyAI>();
                                var rarity = RollRarity();
                                ai.GenerateIsolatedBuild(level, roller, rarity);
                                int duelSeed = unchecked(config.seed + level * 1000003 + sample * 97 + StableHash(prefab.name));
                                rows.Add(Capture(prefab.name, level, sample, ai, actor,
                                    scaledPlayer, playerSpeedSeed, controller.GetFinalCritChance(),
                                    referenceStats, playerStats, ailments, duelSeed));
                            }
                            finally { UnityEngine.Object.DestroyImmediate(actor); }
                        }
                    }
                }
                clock.Stop();
                return new BalanceResult
                {
                    config = config,
                    archetypes = prefabs.Select(p => p.name).ToArray(),
                    playerLevelOneLife = playerLifeSeed,
                    playerLevelOneHit = playerHitSeed,
                    playerLevelOneAttackSpeed = playerSpeedSeed,
                    rows = rows.ToArray(),
                    elapsedSeconds = clock.Elapsed.TotalSeconds
                };
            }
            finally
            {
                foreach (var item in created)
                {
                    if (item is GameObject go)
                    {
                        go.GetComponent<ModManager>()?.ReleaseIsolatedRolling();
                        go.GetComponent<GearStatLists>()?.ReleaseIsolatedRolling();
                    }
                }
                for (int i = created.Count - 1; i >= 0; i--)
                    if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
                UnityEngine.Random.state = savedRandomState;
            }
        }

        private static ModManager PrepareRoller(List<UnityEngine.Object> created)
        {
            var database = AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
            if (database == null) throw new InvalidOperationException($"Missing {DatabasePath}");
            database.Initialize(); // lazy lookup cache only; no serialized asset edits.
            var roller = ModManager.Instance;
            if (roller != null) return roller;
            var go = new GameObject("Balance isolated mod roller", typeof(ModManager));
            created.Add(go);
            roller = go.GetComponent<ModManager>();
            roller.ConfigureForIsolatedRolling(database);
            return roller;
        }

        private static void PrepareStatPools(List<UnityEngine.Object> created)
        {
            if (GearStatLists.Instance != null) return;
            var go = new GameObject("Balance isolated stat pools", typeof(GearStatLists));
            created.Add(go);
            go.GetComponent<GearStatLists>().ConfigureForIsolatedRolling();
        }

        private static GameObject PrepareStarterPlayer(List<UnityEngine.Object> created)
        {
            var go = new GameObject("Balance starter player", typeof(StatsComponent), typeof(HealthComponent),
                typeof(PlayerStatSetup), typeof(PlayerController));
            created.Add(go);
            var setup = go.GetComponent<PlayerStatSetup>();
            typeof(PlayerStatSetup).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(setup, null);
            var controller = go.GetComponent<PlayerController>();
            typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(controller, null);
            Gear starter = (Gear)typeof(PlayerController).GetMethod("CreateStarterWeapon",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            var stats = go.GetComponent<StatsComponent>();
            foreach (RolledMod mod in starter.globalRolledMods)
                stats.AddModifier(new StatModifier(mod.statType,
                    StatMappings.GetRolledModifierOperation(mod.statType), mod.value, starter));
            controller.EquipWeapon(starter); // avoids EquipmentManager/Inventory singleton mutation
            return go;
        }

        public static List<GameObject> DiscoverEnemies()
        {
            var results = new List<GameObject>();
            var battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/PaperBattle/PaperBattle.prefab");
            var battle = battlePrefab != null ? battlePrefab.GetComponentInChildren<BattleManager>(true) : null;
            if (battle != null)
            {
                var serialized = new SerializedObject(battle);
                foreach (string field in new[] { "normalEnemyPrefab", "bossEnemyPrefab" })
                {
                    var active = serialized.FindProperty(field)?.objectReferenceValue as GameObject;
                    if (active != null && active.GetComponent<EnemyAI>() != null
                        && active.GetComponent<EnemyStatSetup>() != null && !results.Contains(active))
                        results.Add(active);
                }
            }
            if (results.Count > 0)
            {
                results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                return results;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabRoots))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.GetComponent<EnemyAI>() != null
                    && prefab.GetComponent<EnemyStatSetup>() != null && prefab.GetComponent<HealthComponent>() != null)
                    results.Add(prefab);
            }
            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }

        private static EnemyAI.EnemyRarity RollRarity()
        {
            int roll = UnityEngine.Random.Range(0, 71);
            if (roll < 40) return EnemyAI.EnemyRarity.Normal;
            if (roll < 60) return EnemyAI.EnemyRarity.Magic;
            if (roll < 70) return EnemyAI.EnemyRarity.Rare;
            return EnemyAI.EnemyRarity.Legendary;
        }

        public static float ReferenceFactor(int level)
        {
            level = Math.Max(1, level);
            return (float)(Math.Pow(1.035, Math.Min(level - 1, 99)) * Math.Pow(1.018, Math.Max(0, level - 100)));
        }

        private static void ConfigureReference(StatsComponent stats, float lifeSeed, int level)
        {
            stats.SetBaseStat(StatTypes.Life, lifeSeed * ReferenceFactor(level));
            stats.SetBaseStat(StatTypes.FlatArmour, 6f * (level - 1));
            float resist = Mathf.Min(30f, .25f * (level - 1));
            stats.SetBaseStat(StatTypes.FireRes, resist);
            stats.SetBaseStat(StatTypes.ColdRes, resist);
            stats.SetBaseStat(StatTypes.LightRes, resist);
            stats.SetBaseStat(StatTypes.VoidRes, resist);
        }

        private static BalanceRow Capture(string name, int level, int sample, EnemyAI ai, GameObject actor,
            DamageContext playerAttack, float playerSpeed, float playerCrit, StatsComponent reference,
            StatsComponent playerStats, StatusEffects[] ailments, int duelSeed)
        {
            var stats = actor.GetComponent<StatsComponent>();
            var health = actor.GetComponent<HealthComponent>();
            var setup = actor.GetComponent<EnemyStatSetup>();
            var intrinsic = setup.Intrinsic;
            DamageContext enemyAttack = ai.BuildNonCriticalAttackContext();
            float enemyHit = CombatCalculator.CalculateFinalDamage(enemyAttack, stats, reference);
            float playerHit = CombatCalculator.CalculateFinalDamage(playerAttack, playerStats, stats);
            float enemyCrit = ai.GetFinalCritChance();
            float enemyCritMult = 1f + stats.GetStat(StatTypes.CritMult);
            float expectedHit = enemyHit * (1f + enemyCrit * (enemyCritMult - 1f));
            float hitTwice = Mathf.Clamp01(stats.GetStat(StatTypes.ChanceToHitTwice));
            float enemySpeed = Mathf.Max(.0001f, ai.GetFinalAttackSpeed());
            float dps = expectedHit * enemySpeed * (1f + hitTwice);
            float playerDps = playerHit * Mathf.Max(.0001f, playerSpeed);
            float[] typed = new float[5];
            Element[] elements = { Element.Phys, Element.Fire, Element.Cold, Element.Light, Element.Void };
            for (int i = 0; i < elements.Length; i++)
            {
                var profile = new DamageContext(1);
                profile.AddDamage(elements[i], 100f);
                typed[i] = CombatCalculator.CalculateFinalDamage(profile, null, stats);
            }
            var poisonContext = new DamageContext(1);
            poisonContext.AddDamage(Element.Void, 100f);
            AilmentCalculator.ComputeAilmentFromHit(ailments[0], poisonContext, stats,
                out float poisonTick, out _, out _);
            poisonTick = CombatCalculator.CalculateAilmentTickDamage(poisonTick, ailments[0], stats, reference);
            Gear[] items = ai.EquippedItems.Where(g => g != null).ToArray();
            string signature = string.Join("|", items.Select(g => $"{g.ItemType}:{g.BaseElement}:{g.ItemRarity}"));
            string affixes = string.Join("|", items.SelectMany(g => g.rolledMods)
                .Where(m => m != null && !Gear.IsWeaponBaseStat(m.statType)).Select(m => m.statType.ToString()));
            float maxLife = health.MaxLife;
            float referenceLife = reference.GetStat(StatTypes.Life);
            var row = new BalanceRow
            {
                archetype = name, level = level, sample = sample,
                authoredLife = setup.AuthoredLevelOneLife, lifeFactor = intrinsic.LifeFactor,
                intrinsicLife = intrinsic.ScaledLife(setup.AuthoredLevelOneLife), damageFactor = intrinsic.DamageFactor,
                intrinsicArmour = intrinsic.Armour, intrinsicResistance = intrinsic.ResistancePoints,
                finalLife = maxLife, outgoingHit = Sum(enemyAttack) * (1f + enemyCrit * (enemyCritMult - 1f)),
                expectedDps = dps, attackSpeed = enemySpeed, critChance = enemyCrit,
                critMultiplier = enemyCritMult, armour = stats.GetStat(StatTypes.FlatArmour)
                    * (1f + stats.GetStat(StatTypes.ArmourPercent)),
                fireRes = stats.GetStat(StatTypes.FireRes) + stats.GetStat(StatTypes.AllRes),
                coldRes = stats.GetStat(StatTypes.ColdRes) + stats.GetStat(StatTypes.AllRes),
                lightRes = stats.GetStat(StatTypes.LightRes) + stats.GetStat(StatTypes.AllRes),
                voidRes = stats.GetStat(StatTypes.VoidRes) + stats.GetStat(StatTypes.AllRes),
                hitTwice = hitTwice, optimizerOffense = ai.LastBuildEvaluation.Offense,
                optimizerDefense = ai.LastBuildEvaluation.Defense,
                referencePlayerLife = referenceLife, referencePlayerHit = playerHit,
                hitsToKill = playerHit > 0f ? Mathf.Ceil(maxLife / playerHit) : float.PositiveInfinity,
                secondsToKill = playerDps > 0f ? maxLife / playerDps : float.PositiveInfinity,
                hitsToDeath = expectedHit > 0f ? Mathf.Ceil(referenceLife / expectedHit) : float.PositiveInfinity,
                secondsToDeath = dps > 0f ? referenceLife / dps : float.PositiveInfinity,
                physicalReference = typed[0], fireReference = typed[1], coldReference = typed[2],
                lightningReference = typed[3], voidReference = typed[4], poisonVoidTick = poisonTick,
                shockCapability = stats.GetStat(StatTypes.ShockChance) > 0f
                    && enemyAttack.Hits.Any(h => h.Element == Element.Light && h.Amount > 0f) ? 1 : 0,
                chillCapability = stats.GetStat(StatTypes.ChillChance) > 0f
                    && enemyAttack.Hits.Any(h => h.Element == Element.Cold && h.Amount > 0f) ? 1 : 0,
                rarity = (int)ai.CurrentRarity, slotCount = items.Length,
                modifierCount = items.Sum(g => g.CraftingModCount),
                dominantElement = ai.WeaponMainElement.ToString(), equipmentSignature = signature,
                affixSignature = affixes
            };
            if (sample < 25)
            {
                var outcome = BalanceCombatSimulator.Simulate(playerAttack, playerStats,
                    referenceLife, playerSpeed, playerCrit, enemyAttack, stats, maxLife,
                    enemySpeed, enemyCrit, ailments, duelSeed);
                row.monteCarloDuration = outcome.Seconds;
                row.monteCarloWinner = outcome.Winner;
                row.monteCarloHits = outcome.Hits;
                row.monteCarloAilmentTicks = outcome.AilmentTicks;
                row.monteCarloShockTriggers = outcome.ShockTriggers;
                row.monteCarloChillApplications = outcome.ChillApplications;
            }
            return row;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char character in text) hash = (hash ^ character) * 16777619;
                return hash;
            }
        }

        private static float Sum(DamageContext context)
        {
            float sum = 0f;
            if (context.Hits != null) foreach (var hit in context.Hits) sum += hit.Amount;
            return sum;
        }

        private static DamageContext CopyAndScale(DamageContext source, float factor)
        {
            var copy = new DamageContext(source.Hits.Count) { Scopes = source.Scopes };
            foreach (var hit in source.Hits) copy.AddDamage(hit.Element, hit.Amount);
            EnemyScalingMath.ScaleOutgoing(copy, factor);
            return copy;
        }
    }
}
