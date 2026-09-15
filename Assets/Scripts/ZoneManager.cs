// Developer map: Selects the six paper forest images in ten-level blocks repeating every sixty levels. Also retains optional legacy 3D scenery generation and future scaling hooks.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    [Header("Stage Progression")]
    [SerializeField] private string[] zoneNames = { "Forest", "Desert", "Tundra" };
    [SerializeField, Min(1)] private int stagesPerZone = 10;
    [Header("Paper Forest Backgrounds")]
    [SerializeField] private SpriteRenderer paperBackground;
    [SerializeField] private Sprite[] forestBackgrounds;
    private bool HasForestCycle => forestBackgrounds != null && forestBackgrounds.Length == 6;
    public static int ForestBackgroundIndex(int level) => ((Mathf.Max(1, level) - 1) / 10) % 6;
    public string LocationLabel => HasForestCycle
        ? ZoneName + " · Level " + Mathf.Max(1, zoneLevel)
        : ZoneName + " " + StageNumber;
    public int StageNumber => (Mathf.Max(1, zoneLevel) - 1) % Mathf.Max(1, stagesPerZone) + 1;
    public string ZoneName
    {
        get
        {
            if (HasForestCycle) return "Forest " + (ForestBackgroundIndex(zoneLevel) + 1);
            if (zoneNames == null || zoneNames.Length == 0) return "Forest";
            int index = Mathf.Min((Mathf.Max(1, zoneLevel) - 1) / Mathf.Max(1, stagesPerZone), zoneNames.Length - 1);
            return string.IsNullOrWhiteSpace(zoneNames[index]) ? "Zone " + (index + 1) : zoneNames[index];
        }
    }

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
        // Encounter/stage 10 is the boss: nine normal kills precede it.
        // This quota is independent of ten combat levels per forest background.
        Debug.Log($"ZoneManager: GetEnemiesToKillBeforeBoss(zoneLevel={zoneLevel}) -> 9");
        return 9;
    }

    public void GenerateZone()
    {
        ApplyPaperBackground();
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

    public void ConfigurePaperBackgrounds(SpriteRenderer renderer, Sprite[] backgrounds)
    {
        paperBackground = renderer;
        forestBackgrounds = backgrounds;
        ApplyPaperBackground();
    }

    private void ApplyPaperBackground()
    {
        if (paperBackground == null || !HasForestCycle) return;
        var sprite = forestBackgrounds[ForestBackgroundIndex(zoneLevel)];
        if (sprite != null) paperBackground.sprite = sprite;
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

    // Extension point for ordinary combat-level-clear rewards; currently no reward is defined.
    public void RewardZoneClear(int zoneLevel)
    {
        // TODO: grant loot / xp / currency here
    }
}
