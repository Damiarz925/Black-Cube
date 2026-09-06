using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private bool generate3DScenery = true;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private ThemeSet themeSet;
    [SerializeField] private SpawnAnchorGroup anchors;
    [SerializeField] private Pool pool;

    [Header("Bounds")]
    public float groundY = 0f;
    public float minZ = -20f;
    public float maxZ = 200f;

    private readonly List<GameObject> activeObjects = new();
    public int zoneLevel;

    private void Start()
    {
        GenerateZone();
    }

    public int GetEnemiesToKillBeforeBoss(int zoneLevel)
    {
        Debug.Log($"ZoneManager: GetEnemiesToKillBeforeBoss(zoneLevel={zoneLevel}) -> 10");
        return 10;
    }

    public void GenerateZone()
    {
        // The paper scene supplies a painted background. Keep generation reusable.
        if (!generate3DScenery) return;
        if (!EnsureReferences())
            return;

        int seed = System.Environment.TickCount;

        ThemeDefinition theme = PickTheme(zoneLevel, seed);
        if (theme == null)
        {
            Debug.LogWarning("ZoneManager: No theme available; skipping zone visual generation.", this);
            return;
        }

        LevelBounds bounds = new LevelBounds
        {
            MinZ = minZ,
            MaxZ = maxZ,
            GroundY = groundY
        };
        
        LevelPlan plan = levelGenerator.BuildLevelPlan(zoneLevel, seed, theme, anchors, bounds);

        ApplyThemeVisuals(plan.Theme);
        ApplyLevelPlan(plan);
    }

    private ThemeDefinition PickTheme(int levelIndex, int seed)
    {
        if (themeSet == null)
            return null;

        if (themeSet.ForcedThemes != null)
        {
            foreach (var entry in themeSet.ForcedThemes)
                if (entry.LevelIndex == levelIndex && entry.Theme != null)
                    return entry.Theme;
        }

        Random.InitState(seed ^ 0x6A09E667);
        return PickWeighted(themeSet.Themes);
    }

    private ThemeDefinition PickWeighted(List<ThemeDefinition> themes)
    {
        if (themes == null || themes.Count == 0) return null;
        if (themes.Count == 1) return themes[0];

        float total = 0f;
        foreach (var t in themes)
            if (t != null) total += Mathf.Max(0f, t.Weight);

        if (total <= 0f)
        {
            return themes[Random.Range(0, themes.Count)];
        }

        float roll = Random.value * total;
        foreach (var t in themes)
        {
            if (t == null) continue;
            roll -= Mathf.Max(0f, t.Weight);
            if (roll <= 0f) return t;
        }

        return themes[themes.Count - 1];
    }

    public void ApplyLevelPlan(LevelPlan plan)
    {
        if (plan == null)
            return;

        ClearActiveObjects();

        SpawnPlacements(plan.TreePlacements);
        SpawnPlacements(plan.RockPlacements);
        SpawnPlacements(plan.TerrainPlacements);
    }

    private void SpawnPlacements(List<Placement> placements)
    {
        if (placements == null || pool == null)
            return;

        foreach (var p in placements)
        {
            if (p.Prefab == null)
                continue;

            GameObject obj = pool.Get(p.PoolKey, p.Prefab);
            if (obj == null)
                continue;

            obj.transform.position = p.Position;
            obj.transform.rotation = p.Rotation;
            obj.transform.localScale = p.Scale;

            obj.SetActive(true);

            activeObjects.Add(obj);
        }
    }

    void ApplyThemeVisuals(ThemeDefinition theme)
    {
        if (theme == null)
            return;

        if (theme.SkyboxMaterial != null)
        {
            RenderSettings.skybox = theme.SkyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }

    void ClearActiveObjects()
    {
        if (pool == null)
        {
            activeObjects.Clear();
            return;
        }

        foreach (var obj in activeObjects)
            if (obj != null)
                pool.Release(obj);

        activeObjects.Clear();
    }

    private bool EnsureReferences()
    {
        if (levelGenerator == null)
            levelGenerator = FindFirstObjectByType<LevelGenerator>();

        if (pool == null)
            pool = FindFirstObjectByType<Pool>();

        if (levelGenerator == null)
        {
            Debug.LogError("ZoneManager: LevelGenerator is missing; cannot generate zone.", this);
            return false;
        }

        if (themeSet == null)
        {
            Debug.LogError("ZoneManager: ThemeSet is missing; cannot generate zone.", this);
            return false;
        }

        if (anchors == null)
        {
            Debug.LogError("ZoneManager: SpawnAnchorGroup is missing; cannot generate zone.", this);
            return false;
        }

        if (pool == null)
        {
            Debug.LogError("ZoneManager: Pool is missing; cannot generate zone.", this);
            return false;
        }

        return true;
    }

    //This will be used for deciding what enemies spawn in the zone, and what list of environment assets are used in the zone
    public int GetSeedForZone(int zoneLevel)
    {
        return zoneLevel;
    }

    //This will likely perform operations based on the zone level and return an HP multiplier for the enemies in the zone based upon that operation.
    public float GetEnemyHpMultiplier(int zoneLevel)
    {
        return zoneLevel;
    }

    //Similar to the HP mult, it will return the multiplier based on the zone level, math will be performed in this function.
    public float GetEnemyDamageMultiplier(int zoneLevel)
    {
        return zoneLevel;
    }

    //This will check if the zone level is high enough to allow the player to prestige when completed. Likely display an animation to make it clear.
    //If this is called regardless of whether showPrestige is already true in the class that calls it, add one later so that it is only offered and animated once, then remains visible after until prestige occurs
    public bool ShouldOfferPrestige(int zoneLevel)
    {
        return zoneLevel >= 10; // Example threshold, adjust as needed
    }

    //Rewards the player based on the level of the zone, passed in zoneLevel is likely redundant, as the zoneManager should instantiate its own zoneLevel.
    public void RewardZoneClear(int zoneLevel)
    {
        // TODO: grant loot / xp / currency here
    }

    //This function doesn't feel fitting for this script, likely add a separate prestige script that couples rewards granted with other prestige related actions.
    public void GrantPrestigeRewards(int highestZoneThisRun)
    {
        // TODO: grant meta rewards here
    }
}
