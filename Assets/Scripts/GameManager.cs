// Developer map: Session run coordinator: nine normal kills lead to stage10 boss, whose death advances directly to the next combat level. Claims enemy death once before XP/loot/spawn callbacks; encounter restart retains PlayerProgression.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerProgression))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }    //Public getter/private setter for instance

    [Header("Core References")]         //References for levelGenerator, zoneManager and lootManager
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private LootManager lootManager;
    [SerializeField] private DeathMenuUI deathMenuUI;

    [Header("Run State")]       //Fields for the current zone level, the number of enemies killed in the current zone, and the number of enemies to kill before the next enemy spawned will be a boss, as well as a bool for whether the boss has spawned or not
    [SerializeField] private int currentZoneLevel = 1;
    [SerializeField] private int enemiesKilledInZone = 0;
    [SerializeField] private int enemiesToKillBeforeBoss = 9;
    [SerializeField] private bool bossSpawned = false;

    private bool playerDeathHandled;
    private ulong initializedGameplaySceneHandle = ulong.MaxValue;
    public int GameplayInitializationCount { get; private set; }
    public int CurrentCombatLevel => currentZoneLevel;
    public int NormalKills => enemiesKilledInZone;
    public int NormalKillsRequired => enemiesToKillBeforeBoss;
    public bool BossActive => bossSpawned;
    // Encounter stage within one combat level; forest image progression is separate.
    public int EncounterStage => bossSpawned ? enemiesToKillBeforeBoss + 1 : enemiesKilledInZone + 1;

    private void Awake()    //Logic for DDoL singleton in Awake
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Authored manager objects are children of the scene prefab. Unity only
        // honors DontDestroyOnLoad for roots, so detach the authority first.
        transform.SetParent(null, true);
        if (GetComponent<PlayerProgression>() == null) gameObject.AddComponent<PlayerProgression>();
        if (GetComponent<RelicInventory>() == null) gameObject.AddComponent<RelicInventory>();
        if (GetComponent<RebirthManager>() == null) gameObject.AddComponent<RebirthManager>();
        if (GetComponent<PlayerIdentityState>() == null) gameObject.AddComponent<PlayerIdentityState>();
        if (GetComponent<GamePersistenceHost>() == null) gameObject.AddComponent<GamePersistenceHost>();
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Instance = null;
    }

    private void Start()
    {
        InitializeGameplayScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InitializeGameplayScene(scene);

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != GameSceneNames.Gameplay) return;
        ClearSceneReferences();
        GetComponent<PlayerProgression>()?.ReleaseSceneReferences();
        EquipmentManager.Instance?.ReleaseSceneReferences();
    }

    private void InitializeGameplayScene(Scene scene)
    {
        ulong sceneHandle=scene.handle.GetRawData();
        if (scene.name != GameSceneNames.Gameplay || initializedGameplaySceneHandle == sceneHandle) return;
        initializedGameplaySceneHandle = sceneHandle;
        GameplayInitializationCount++;
        Debug.Log("GameManager: Initializing gameplay scene.");
        BindSceneReferences(scene);
        EquipmentManager.Instance?.BindSceneReferences(scene);
        var scenePlayer = FindInScene<PlayerController>(scene);
        if (scenePlayer != null && scenePlayer.GetComponent<PlayerSkillController>() == null)
            scenePlayer.gameObject.AddComponent<PlayerSkillController>();
        deathMenuUI?.Hide();
        ResetRunStateForGameplayEntry();
        if (GamePersistence.LoadRequested)
        {
            GamePersistence.RestoreRequestedGame(out bool restored);
            Debug.Log(restored
                ? "GameManager: Restored the requested saved game during gameplay initialization."
                : "GameManager: Load was requested, but the current save could not be restored.");
            if (!restored)
            {
                SceneManager.LoadScene(GameSceneNames.MainMenu);
                return;
            }
        }
        else if (!StartFreshGame(GamePersistence.ConsumeConfirmedNewGameRequest()))
            SceneManager.LoadScene(GameSceneNames.MainMenu);
    }

    private void ResetRunStateForGameplayEntry()
    {
        RebirthManager.Instance?.Cancel();
        EquipmentManager.Instance?.ResetForNewRun();
        Inventory.Instance?.ResetForNewRun();
        CurrencyInventory.Instance?.ResetForNewGame();
        RelicInventory.Instance?.ResetForNewGame();
    }

    private bool StartFreshGame(bool commit)
    {
        GamePersistence.BeginFreshRunIdentity();
        GetComponent<PlayerIdentityState>()?.BeginNewGame(GameLaunchSelection.ConsumeOrDefault());
        var player = FindAnyObjectByType<PlayerController>();
        player?.GetComponent<PlayerSkillController>()?.RestoreSelection(false, default);
        player?.GetComponent<StatusController>()?.ClearStatuses();
        player?.EnsureStarterWeapon();
        StartNewRun();
        if (commit && !GamePersistence.CommitConfirmedNewGame())
        {
            Debug.LogError("GameManager: the confirmed New Game could not commit its initial checkpoint.");
            return false;
        }
        return true;
    }

    public void StartNewRun()   //Starts a fresh run, at zone lvl 1, 0 enemies killed and no boss spawned, debug log for dev feedback, calls start zone passing in the currentzonelevel after resetting the state
    {
        GetComponent<PlayerProgression>().ResetProgression();
        currentZoneLevel = 1;
        enemiesKilledInZone = 0;
        bossSpawned = false;
        playerDeathHandled = false;

        Debug.Log("GameManager: Starting new run at zone 1.");
        StartZone(currentZoneLevel);
    }

    public void StartZone(int zoneLevel)        //sets currentzonelevel with passed in zone level, enemies killed set to 0, boss spawned false, enemies to kill pulled from function in zone manager, passing in zone level
    {
        EnsureSceneReferences();

        if (zoneManager == null)
        {
            Debug.LogError("GameManager: Cannot start zone because ZoneManager is missing.");
            return;
        }

        if (BattleManager.Instance == null)
        {
            Debug.LogError("GameManager: Cannot start zone because BattleManager.Instance is missing.");
            return;
        }

        zoneLevel = Mathf.Max(1, zoneLevel);
        currentZoneLevel = zoneLevel;
        enemiesKilledInZone = 0;
        bossSpawned = false;
        playerDeathHandled = false;

        enemiesToKillBeforeBoss = zoneManager.GetEnemiesToKillBeforeBoss(zoneLevel);
        zoneManager.zoneLevel = zoneLevel;
        zoneManager.GenerateZone();     //generate the zone, which will spawn the environment

        Debug.Log($"GameManager: ZoneManager.zoneLevel set to {zoneManager.zoneLevel}");

        Debug.Log($"GameManager: Starting zone {zoneLevel}. " +
                  $"Enemies to kill before boss = {enemiesToKillBeforeBoss}.");

        int seed = zoneManager.GetSeedForZone(zoneLevel);                                           //grab the zone seed from the zonemanager script, passing in the zone level
        Debug.Log($"GameManager: Generating level with seed {seed}.");

        Debug.Log("GameManager: Spawning first enemy for this zone.");
        GamePersistence.GenerateDeterministicEncounter(currentZoneLevel, enemiesKilledInZone, false,
            () => BattleManager.Instance.BeginZone(zoneLevel, startWithBoss: false));
    }

    public bool RestoreRunState(int zoneLevel, int completedNormalEncounters, bool bossEncounter)
    {
        EnsureSceneReferences();
        if (zoneManager == null || BattleManager.Instance == null || zoneLevel < 1 || completedNormalEncounters < 0) return false;
        int quota = zoneManager.GetEnemiesToKillBeforeBoss(zoneLevel);
        if (completedNormalEncounters > quota || (bossEncounter && completedNormalEncounters != quota)) return false;
        currentZoneLevel = zoneLevel;
        enemiesToKillBeforeBoss = quota;
        enemiesKilledInZone = completedNormalEncounters;
        bossSpawned = bossEncounter;
        playerDeathHandled = false;
        zoneManager.zoneLevel = zoneLevel;
        zoneManager.GenerateZone();
        GamePersistence.GenerateDeterministicEncounter(zoneLevel, completedNormalEncounters, bossEncounter,
            () => BattleManager.Instance.SpawnNextEnemy(bossEncounter));
        return BattleManager.Instance.CurrentEnemyAI != null;
    }

    public void OnEnemyKilled(HealthComponent enemyHealth, bool wasBoss)                            //on enemy killed function defines what happens when an enemy is killed
    {
        var currentEnemy = BattleManager.Instance != null ? BattleManager.Instance.CurrentEnemyAI : null;
        if (enemyHealth == null || currentEnemy == null ||
            currentEnemy.GetComponent<HealthComponent>() != enemyHealth || !enemyHealth.TryClaimEnemyDeath()) return;
        // Trust the spawned actor's role, not a caller-supplied flag.
        wasBoss = enemyHealth.IsBoss;
        if (!wasBoss) enemiesKilledInZone++;
        var enemyAI = enemyHealth.GetComponent<EnemyAI>();                                          //grab the killed enemy's script
        var rarity = enemyAI != null ? enemyAI.CurrentRarity : EnemyAI.EnemyRarity.Normal;          //check if the enemy script is null, if it isn't grab the enemy's rarity, if it is set the rarity to normal
        RestorePlayerKillResources();
        BattleManager.Instance?.NotifyEnemyDied(enemyHealth);
        GetComponent<PlayerProgression>().AwardEnemy(enemyAI != null ? enemyAI.EnemyLevel : currentZoneLevel, rarity, wasBoss);

        EnsureSceneReferences();

        EnemyDropResult drops=(enemyAI!=null?enemyAI.DropTable:new EnemyDropTable()).Roll(isBoss:wasBoss);
        Transform pickupTarget=FindAnyObjectByType<PlayerController>()?.transform;
        foreach(var currency in drops.currencies)CurrencyWorldPickup.Spawn(currency,enemyHealth.transform.position,pickupTarget);

        Gear loot = drops.equipment && lootManager != null ? lootManager.GenerateLoot(rarity) : null; //equipment is an independent 50% roll
        if (drops.equipment && lootManager == null)
        {
            Debug.LogWarning("GameManager: LootManager is null; no loot generated.");
        }

        if (loot != null && Inventory.Instance != null)                                             //if the inventory isn't null, add the loot generated to the inventory
        {
            Inventory.Instance.Pickup(loot);
            Debug.Log($"GameManager: Loot generated and added. EnemyRarity={rarity}, LootName={(loot != null ? loot.name : "null")}");
        }
        else if (drops.equipment && loot == null)
        {
            Debug.LogWarning("GameManager: Generated loot is null; nothing added to inventory.");
        }
        else
        {
            Debug.LogWarning("GameManager: Inventory.Instance is null; generated loot not added.");
        }

        Debug.Log($"GameManager: Enemy killed. Boss={wasBoss}, zoneKills={enemiesKilledInZone}/{enemiesToKillBeforeBoss}");

        if (wasBoss)                                                                                //if the enemy was a boss, call on zone cleared and return from function
        {
            OnZoneCleared();
            return;
        }

        bool shouldSpawnBoss = !bossSpawned && enemiesKilledInZone >= enemiesToKillBeforeBoss;      //define shouldspawn boss as boss not spawned, and the enemies killed is greater than or equal to the enemies to kill before boss
        if (shouldSpawnBoss)                                                                        //if should spawn boss is true, set boss spawned to true, and call spawnnextenemy from battlemanager setting spawnBoss to true
        {
            bossSpawned = true;
            Debug.Log("GameManager: Conditions met, spawning boss next.");
            if (BattleManager.Instance != null)
                GamePersistence.GenerateDeterministicEncounter(currentZoneLevel, enemiesKilledInZone, true,
                    () => BattleManager.Instance.SpawnNextEnemy(spawnBoss: true));
            else
                Debug.LogWarning("GameManager: BattleManager.Instance is null; cannot spawn boss.");
        }
        else
        {
            if (BattleManager.Instance != null)
                GamePersistence.GenerateDeterministicEncounter(currentZoneLevel, enemiesKilledInZone, false,
                    () => BattleManager.Instance.SpawnNextEnemy(spawnBoss: false));
            else
                Debug.LogWarning("GameManager: BattleManager.Instance is null; cannot spawn next enemy.");
        }
        GamePersistence.Save();
    }

    public void OnPlayerKilled(HealthComponent hc)
    {
        if (playerDeathHandled)
        {
            return;
        }

        playerDeathHandled = true;

        var battleManager = BattleManager.Instance;
        EnemyAI killer = battleManager != null ? battleManager.CurrentEnemyAI : null;

        int killerLevel = killer != null ? killer.EnemyLevel : currentZoneLevel;
        EnemyAI.EnemyRarity killerRarity = killer != null ? killer.CurrentRarity : EnemyAI.EnemyRarity.Normal;
        Element killerWeaponElement = killer != null ? killer.WeaponMainElement : Element.Phys;

        Debug.Log($"GameManager: Player died. Killer level={killerLevel}, rarity={killerRarity}, weaponElement={killerWeaponElement}.");

        if (deathMenuUI != null)
        {
            Debug.Log("GameManager: Showing death menu.");
            deathMenuUI.Show(killerLevel, killerRarity, killerWeaponElement);
        }
        else
        {
            Debug.LogWarning("GameManager: deathMenuUI is null; cannot show death menu.");
        }
    }

    public void RestartCurrentLevelAfterDeath()
    {
        Debug.Log($"GameManager: Restarting current zone {currentZoneLevel} after death.");

        enemiesKilledInZone = 0;
        bossSpawned = false;
        playerDeathHandled = false;

        if (deathMenuUI != null)
        {
            deathMenuUI.Hide();
        }

        if (BattleManager.Instance != null)
        {
            Debug.Log("GameManager: Calling BattleManager.RespawnPlayerAtLevelStart().");
            BattleManager.Instance.RespawnPlayerAtLevelStart();
        }
        else
        {
            Debug.LogWarning("GameManager: BattleManager.Instance is null on restart.");
        }
    }

    private void OnZoneCleared()        //Logic for what happens when the zone is cleared.
    {
        Debug.Log($"GameManager: Zone {currentZoneLevel} cleared.");

        if (zoneManager == null)
        {
            Debug.LogWarning("GameManager: ZoneManager is null during zone clear; starting next zone without zone rewards.");
        }
        else
        {
            zoneManager.RewardZoneClear(currentZoneLevel);
        }

        StartZone(NextCombatLevelAfterBoss(currentZoneLevel));
        GamePersistence.Save();
    }

    private static void RestorePlayerKillResources()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        StatsComponent stats = player.GetComponent<StatsComponent>();
        if (stats == null) return;
        player.GetComponent<HealthComponent>()?.RestoreLife(stats.GetStat(StatTypes.LifeOnKill));
        player.GetComponent<ManaComponent>()?.Restore(stats.GetStat(StatTypes.ManaOnKill));
    }

    private static int NextCombatLevelAfterBoss(int clearedLevel) => Mathf.Max(1, clearedLevel + 1);

    private void EnsureSceneReferences()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != GameSceneNames.Gameplay) { ClearSceneReferences(); return; }
        if (!BelongsTo(levelGenerator, scene) || !BelongsTo(zoneManager, scene) ||
            !BelongsTo(lootManager, scene) || !BelongsTo(deathMenuUI, scene))
            BindSceneReferences(scene);
    }

    private void BindSceneReferences(Scene scene)
    {
        ClearSceneReferences();
        levelGenerator = FindInScene<LevelGenerator>(scene);
        zoneManager = FindInScene<ZoneManager>(scene);
        lootManager = FindInScene<LootManager>(scene);
        deathMenuUI = FindInScene<DeathMenuUI>(scene);
    }

    private void ClearSceneReferences()
    {
        levelGenerator = null;
        zoneManager = null;
        lootManager = null;
        deathMenuUI = null;
    }

    private static bool BelongsTo(Component component, Scene scene) =>
        component != null && component.gameObject.scene == scene;

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
