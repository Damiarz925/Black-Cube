using UnityEngine;

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
    [SerializeField] private int enemiesToKillBeforeBoss = 10;
    [SerializeField] private bool bossSpawned = false;

    private bool playerDeathHandled;

    private void Awake()    //Logic for DDoL singleton in Awake
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()    //On start, debug log for dev feedback, and call start new run function
    {
        Debug.Log("GameManager: Auto-starting new run.");

        if (deathMenuUI == null)
        {
            deathMenuUI = FindFirstObjectByType<DeathMenuUI>();
            Debug.Log($"GameManager: DeathMenuUI auto-find result = {(deathMenuUI != null ? deathMenuUI.name : "null")}");
        }

        if (deathMenuUI != null)
        {
            deathMenuUI.Hide();
        }

        StartNewRun();
    }

    public void StartNewRun()   //Starts a fresh run, at zone lvl 1, 0 enemies killed and no boss spawned, debug log for dev feedback, calls start zone passing in the currentzonelevel after resetting the state
    {
        currentZoneLevel = 1;
        enemiesKilledInZone = 0;
        bossSpawned = false;
        playerDeathHandled = false;

        Debug.Log("GameManager: Starting new run at zone 1.");
        StartZone(currentZoneLevel);
    }

    public void StartZone(int zoneLevel)        //sets currentzonelevel with passed in zone level, enemies killed set to 0, boss spawned false, enemies to kill pulled from function in zone manager, passing in zone level
    {
        currentZoneLevel = zoneLevel;
        enemiesKilledInZone = 0;
        bossSpawned = false;
        playerDeathHandled = false;

        enemiesToKillBeforeBoss = zoneManager.GetEnemiesToKillBeforeBoss(zoneLevel);
        zoneManager.zoneLevel = zoneLevel;

        Debug.Log($"GameManager: ZoneManager.zoneLevel set to {zoneManager.zoneLevel}");

        Debug.Log($"GameManager: Starting zone {zoneLevel}. " +
                  $"Enemies to kill before boss = {enemiesToKillBeforeBoss}.");

        int seed = zoneManager.GetSeedForZone(zoneLevel);                                           //grab the zone seed from the zonemanager script, passing in the zone level
        Debug.Log($"GameManager: Generating level with seed {seed}.");
        //levelGenerator.GenerateLevel(seed);                                                         //generate the level with the levelGenerator script, passing in the level's seed

        Debug.Log("GameManager: Spawning first enemy for this zone.");
        BattleManager.Instance.BeginZone(zoneLevel, startWithBoss: false);                          //call beginzone from battle manager, passing in the zone level and whether we start with the boss or not
    }

    public void OnEnemyKilled(HealthComponent enemyHealth, bool wasBoss)                            //on enemy killed function defines what happens when an enemy is killed
    {
        enemiesKilledInZone++;                                                                      //increment enemies killed in zone

        var enemyAI = enemyHealth.GetComponent<EnemyAI>();                                          //grab the killed enemy's script
        var rarity = enemyAI != null ? enemyAI.CurrentRarity : EnemyAI.EnemyRarity.Normal;          //check if the enemy script is null, if it isn't grab the enemy's rarity, if it is set the rarity to normal

        Gear loot = lootManager.GenerateLoot(rarity);                                               //generate loot passing in the rarity
        if (Inventory.Instance != null)                                                             //if the inventory isn't null, add the loot generated to the inventory
        {
            Inventory.Instance.Add(loot);
            Debug.Log($"GameManager: Loot generated and added. EnemyRarity={rarity}, LootName={(loot != null ? loot.name : "null")}");
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
            BattleManager.Instance.SpawnNextEnemy(spawnBoss: true);
        }
        else
        {
            BattleManager.Instance.SpawnNextEnemy(spawnBoss: false);        //if shouldspawn boss was false, still spawn next enemy, but with spawnBoss set to false
        }
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

        zoneManager.RewardZoneClear(currentZoneLevel);      //call reward zone clear from zone manager (Might just put this in game manager instead. Doesn't make sense in zone manager)

        if (zoneManager.ShouldOfferPrestige(currentZoneLevel))      //check if prestige should be offered. (Might just put that in game manager instead, doesn't really make sense to be in zone manager
        {
            ShowPrestigeMenu();     //Show prestige menu if necessary
        }
        else
        {
            StartZone(currentZoneLevel + 1);        //Increment the zone level by 1 and start the next zone
        }
    }

    private void ShowPrestigeMenu()     //Not currently implemented (I'll be putting this in the prestige script later.) Prestige will be accessed through a menu button. The initial show prestige will simply make it visible, selectable and highlight it
    {
        // TODO: open prestige UI, let player choose.
        Debug.Log("GameManager: Showing prestige menu (placeholder: auto-continue).");
        StartZone(currentZoneLevel + 1);
    }

    public void PerformPrestige()   //Not currently implemented (probably redundant, I'll create a separate script for managing prestige)
    {
        Debug.Log($"GameManager: Performing prestige at zone {currentZoneLevel}.");
        zoneManager.GrantPrestigeRewards(currentZoneLevel);
        StartNewRun();
    }
}
