using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public LevelPlan BuildLevelPlan(int levelIndex, int seed, ThemeDefinition theme, SpawnAnchorGroup anchors, LevelBounds bounds)
    {
        Random.InitState(seed);

        LevelPlan plan = new LevelPlan
        {
            LevelIndex = levelIndex,
            Seed = seed,
            Theme = theme,
            Bounds = bounds,

            TreePlacements = new List<Placement>(),
            RockPlacements = new List<Placement>(),
            TerrainPlacements = new List<Placement>(),

        };

        // Trees
        plan.TreePlacements.AddRange(BuildPlacements(anchors.ForegroundTreeAnchors, theme.ForegroundTreePrefabs, theme.ForegroundTreeRules, seedSalt: 101));

        plan.TreePlacements.AddRange(BuildPlacements(anchors.MidgroundTreeAnchors, theme.MidgroundTreePrefabs, theme.MidgroundTreeRules, seedSalt: 102));

        plan.TreePlacements.AddRange(BuildPlacements(anchors.BackgroundTreeAnchors, theme.BackgroundTreePrefabs, theme.BackgroundTreeRules, seedSalt: 103));

        plan.TreePlacements.AddRange(BuildPlacements(anchors.ForegroundTreeAnchors, theme.ForegroundTreePrefabsRare, theme.ForeGroundTreeRulesRare, seedSalt: 111));

        // Rocks
        plan.RockPlacements.AddRange(BuildPlacements(anchors.CameraRockAnchors, theme.CameraRockPrefabs, theme.CameraRockRules, seedSalt: 201));

        plan.RockPlacements.AddRange(BuildPlacements(anchors.ForegroundRockAnchors, theme.ForegroundRockPrefabs, theme.ForegroundRockRules, seedSalt: 202));

        plan.RockPlacements.AddRange(BuildPlacements(anchors.MidgroundRockAnchors, theme.MidgroundRockPrefabs, theme.MidgroundRockRules, seedSalt: 203));

        // Terrain
        plan.TerrainPlacements.AddRange(BuildPlacements(anchors.ForegroundTerrainAnchors, theme.ForegroundTerrainPrefabs, theme.ForegroundTerrainRules, seedSalt: 301));

        plan.TerrainPlacements.AddRange(BuildPlacements(anchors.BackgroundTerrainAnchors, theme.BackgroundTerrainPrefabs, theme.BackgroundTerrainRules, seedSalt: 302));

        return plan;
    }

    private List<Placement> BuildPlacements(List<Transform> anchors, List<GameObject> prefabs, ThemeSpawnRules rules, int seedSalt)
    {
        var placements = new List<Placement>();

        if (!rules.Enabled) return placements;
        if (anchors == null || anchors.Count == 0) return placements;
        if (prefabs == null || prefabs.Count == 0) return placements;

        // Salt the random stream per category to reduce cross-category coupling while keeping determinisim per level seed.
        // NOTE: Random.InitState affects global UnityEngine.Random, which is fine if this method is called in a controlled place (generation only).
        Random.InitState(Random.Range(int.MinValue, int.MaxValue) ^ seedSalt);

        // Decide how many anchors to use (optional)
        // If CountRange is (0,0) we interpret as "use all anchors (subject to skip chance)"
        int minCount = rules.CountRange.x;
        int maxCount = rules.CountRange.y;

        int targetCount = (minCount == 0 && maxCount == 0) ? anchors.Count : Mathf.Clamp(Random.Range(minCount, maxCount + 1), 0, anchors.Count);

        // Build a shuffled list of anchor indices so we can take "targetCount"
        var indices = new List<int>(anchors.Count);
        for (int i = 0; i < anchors.Count; i++) indices.Add(i);

        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Apply density multiplier by reducing targetCount
        float density = Mathf.Max(0f, rules.densityMultiplier);
        int densityCount = Mathf.RoundToInt(targetCount * density);
        densityCount = Mathf.Clamp(densityCount, 0, anchors.Count);

        // Track positions to enforce MinSeparation (optional)
        var placedPositions = new List<Vector3>();

        for (int k = 0; k < densityCount; k++)
        {
            Transform anchor = anchors[indices[k]];

            if (Random.value < rules.SkipChance)
                continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];

            // Position jitter (rules.PositionJitter is Vector2)
            Vector3 pos = anchor.position;
            pos.x += Random.Range(-rules.PositionJitter.x, rules.PositionJitter.x);
            pos.z += Random.Range(-rules.PositionJitter.y, rules.PositionJitter.y);

            // Vertical offset
            pos.y += Random.Range(rules.YOffsetRange.x, rules.YOffsetRange.y);

            if (rules.MinSeparation > 0f)
            {
                bool tooClose = false;
                for (int i = 0; i < placedPositions.Count; i++)
                {
                    if (Vector3.Distance(pos, placedPositions[i]) < rules.MinSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;
            }

            float yaw = Random.Range(rules.RotationRangeDeg.x, rules.RotationRangeDeg.y);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            float s = Random.Range(rules.ScaleRange.x, rules.ScaleRange.y);
            Vector3 scale = Vector3.one * s;

            placements.Add(new Placement
            {
                Prefab = prefab,
                Position = pos,
                Rotation = rot,
                Scale = scale,
                SortingOrder = 0,
                PoolKey = prefab.name
            });

            placedPositions.Add(pos);
        }


        return placements;
    }
}
