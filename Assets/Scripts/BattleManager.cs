using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Enemy Prefabs & Spawn")]
    [SerializeField] private GameObject normalEnemyPrefab;
    [SerializeField] private GameObject bossEnemyPrefab;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("Ailment References")]
    [SerializeField] private StatusEffects poisonEffect;
    [SerializeField] private StatusEffects bleedEffect;
    [SerializeField] private StatusEffects igniteEffect;


    private PlayerController playerController;
    private StatusController playerStatusCont;
    private StatsComponent playerStats;
    private HealthComponent playerHealth;

    private EnemyAI enemyAI;
    public EnemyAI CurrentEnemyAI => enemyAI;
    private StatusController enemyStatusCont;
    private StatsComponent enemyStats;
    private HealthComponent enemyHealth;
    private GameObject currentEnemy;

    [Header("Speed / Gauge Settings")]
    [SerializeField] private float turnThreshold = 100f;        //Turn threshold is basically the size of the gauge that is filled before a player or enemy attacks, the speed at which it fills is determined by attack speed value
    private float playerGauge;
    private float enemyGauge;

    private int globalTurnCounter;

    private void Awake()        //Logic to make this a singleton in awake
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("BattleManager: duplicate instance, destroying this one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // You can add DontDestroyOnLoad later if you want it to persist between scenes.
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()        //On start, check if player is null, if so, return
    {
        if (player == null)
        {
            Debug.LogError("BattleManager: Player reference not set.");
            return;
        }

        playerController = player.GetComponent<PlayerController>();     //Grab the player statuscont, stats, controller, and health components
        playerStatusCont = player.GetComponent<StatusController>();
        playerStats = player.GetComponent<StatsComponent>();
        playerHealth = player.GetComponent<HealthComponent>();

        if (playerController == null || playerStats == null || playerHealth == null)        //if any of these (except statuscont) are null, give error
        {
            Debug.LogError("BattleManager: Player is missing required components.", player);
        }

        Debug.Log($"BattleManager: Start refs -> player={player.name}, playerSpawnPoint={(playerSpawnPoint != null ? playerSpawnPoint.name : "null")}, enemySpawnPoint={(enemySpawnPoint != null ? enemySpawnPoint.name : "null")}");
    }

    /// <summary>
    /// Called by GameManager whenever a new zone starts.
    /// </summary>
    public void BeginZone(int zoneLevel, bool startWithBoss = false)    //debug log and calls spawnnextenemy function, passing in startwithboss (defaulted to false)
    {
        Debug.Log($"BattleManager: BeginZone(level={zoneLevel}, startWithBoss={startWithBoss})");
        SpawnNextEnemy(startWithBoss);
    }

    /// <summary>
    /// Called by GameManager (or by itself) when the next enemy in this zone should be spawned.
    /// </summary>
    public void SpawnNextEnemy(bool spawnBoss = false)
    {
        if (enemySpawnPoint == null)        //If the enemy spawn point is null, return
        {
            Debug.LogError("BattleManager: enemySpawnPoint not set.");
            return;
        }

        GameObject prefab = null;       //create a variable prefab set to null

        if (spawnBoss)
        {
            prefab = bossEnemyPrefab != null ? bossEnemyPrefab : normalEnemyPrefab;     //if spawn boss is true, set the prefab to bossenemyprefab if it isn't null, if it is, use a normal enemy prefab
        }
        else
        {
            prefab = normalEnemyPrefab;     //if spawn boss is false, set the prefab to use the normal enemy prefab
        }

        if (prefab == null)     //if prefab is still null, give error message and return
        {
            Debug.LogError("BattleManager: No enemy prefab assigned.");
            return;
        }

        if (currentEnemy != null)       //if current enemy isn't null, destroy it
        {
            Destroy(currentEnemy);
        }

        currentEnemy = Instantiate(prefab, enemySpawnPoint.position, enemySpawnPoint.rotation);     //instantiate a new enemy object using the spawn point and prefab, and assign that to the currentEnemy variable

        enemyAI = currentEnemy.GetComponent<EnemyAI>();     //grab the enemyAI, statuscont, stats, and health component of the newly instantiated enemy
        enemyStatusCont = currentEnemy.GetComponent<StatusController>();
        enemyStats = currentEnemy.GetComponent<StatsComponent>();
        enemyHealth = currentEnemy.GetComponent<HealthComponent>();

        if (enemyAI == null || enemyStatusCont == null || enemyStats == null || enemyHealth == null)       //if any of these (except statuscont) are null, give error and return
        {
            Debug.LogError("BattleManager: Enemy is missing required components (EnemyAI/StatsComponent/HealthComponent).", currentEnemy);
            return;
        }

        var zoneManager = FindFirstObjectByType<ZoneManager>();     //find the zone manager for the current zone
        int zoneLevel = zoneManager != null ? zoneManager.zoneLevel : 1;        //get the zone level from the zone manager, if the zone manager is null, set zone level to 1

        Debug.Log($"BattleManager: Spawning {(spawnBoss ? "BOSS" : "normal")} enemy at zone level {zoneLevel}. " +
                  $"normalPrefab={(normalEnemyPrefab ? normalEnemyPrefab.name : "null")}, " +
                  $"bossPrefab={(bossEnemyPrefab ? bossEnemyPrefab.name : "null")}");

        enemyAI.InitializeEnemy(zoneLevel);     //call initialize enemy, passing in the zone level

        enemyGauge = 0f;        //set player and enemy gauges to 0, and global turn counter to 0.
        playerGauge = 0f;
        globalTurnCounter = 0;
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.CurrentLife <= 0f) return;     //if player health is ever null or player life is ever 0 or less, return
        if (currentEnemy == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;       //if enemy or enemy health is ever null, or enemy life is ever 0 or less, return

        float playerSpeed = playerController.GetFinalAttackSpeed() * 100f;      //get the player and enemy final attack speed and multiply it by 100f
        float enemySpeed = enemyAI.GetFinalAttackSpeed() * 100f;

        playerGauge += playerSpeed * Time.deltaTime;        //add to the gauge based on player and enemy speed every second
        enemyGauge += enemySpeed * Time.deltaTime;

        int safety = 10;

        while ((playerGauge >= turnThreshold || enemyGauge >= turnThreshold) && safety-- > 0)       //if player gauge or enemygauge exceeeds the turn threshold, and the safety is greater than 0 (decrement safety after each loop)
        {
            bool playerActs = playerGauge >= turnThreshold && playerGauge >= enemyGauge;    //player acts if playergauge exceeds the turn threshold and is greater than or equal to the enemy gauge

            if (playerActs)     //if player acts is true, subtract the turn threshold from the player's gauge and call resolve player turn, otherwise, subtract from enemy gauge and call resolve enemy turn
            {
                playerGauge -= turnThreshold;
                ResolvePlayerTurn();
            }
            else
            {
                enemyGauge -= turnThreshold;
                ResolveEnemyTurn();
            }

            if (currentEnemy == null || enemyHealth.CurrentLife <= 0f) return;      //if current enemy is null, or enemy life is less than or equal to 0, return
        }
    }

    private void ResolvePlayerTurn()
    {
        if (currentEnemy == null || enemyStatusCont == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;       //if there is no enemy, or it has no hp, or its HP is less than or equal to 0, return

        globalTurnCounter++;        //increment global turn counter

        playerStatusCont.TickStatuses();       //tick statuses on player and enemy
        enemyStatusCont.TickStatuses();

        DamageContext ctx = playerController.BuildAttackContext();      //generate damage context for the player

        // Debug: raw damage before defenses
        float rawTotal = 0f;
        foreach (var hit in ctx.Hits)       //calculate the total raw damage from the context
        {
            rawTotal += hit.Amount;
        }

        float damageTaken = CombatCalculator.CalculateFinalDamage(ctx, playerStats, enemyStats);        //calculate the final damage using the context, player stats, and enemy stats

        Debug.Log($"[Turn {globalTurnCounter}] Player hits enemy. " +
                  $"Raw={rawTotal:F1}, Final(after res/armour)={damageTaken:F1}, " +
                  $"Crit={ctx.IsCrit}, CritMult={ctx.CritMultiplier:F2}");

        enemyHealth.LoseLife(damageTaken);      //call lose life on the enemy script, passing in the damage taken value calculated previously

        ApplyOnHitEffects(ctx, playerStats, enemyStatusCont);       //call apply on hit effects, passing in the context, player stats, and enemystatuscont
    }

    private void ResolveEnemyTurn()
    {
        if (currentEnemy == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;       //if enemy, or their health is null or less than or equal to 0, return
        if (playerHealth == null || playerStatusCont == null || playerHealth.CurrentLife <= 0f) return;     //if player health is null or less than or equal to 0, return

        globalTurnCounter++;        //increment global turn counter

        playerStatusCont.TickStatuses();       //tick player and enemy statuses
        enemyStatusCont.TickStatuses();

        DamageContext ctx = enemyAI.BuildAttackContext();       //generate damage context from the enemy

        float rawTotal = 0f;
        foreach (var hit in ctx.Hits)       //calculate the raw damage total using the damage context
        {
            rawTotal += hit.Amount;
        }

        float damageTaken = CombatCalculator.CalculateFinalDamage(ctx, enemyStats, playerStats);        //calculate damage taken by passing in context, enemy stats, and player stats

        Debug.Log($"[Turn {globalTurnCounter}] Enemy hits player. " +
                  $"Raw={rawTotal:F1}, Final(after res/armour)={damageTaken:F1}, " +
                  $"Crit={ctx.IsCrit}, CritMult={ctx.CritMultiplier:F2}");

        playerHealth.LoseLife(damageTaken);     //call lose life in player script, passing in damage taken

        ApplyOnHitEffects(ctx, enemyStats, playerStatusCont);       //call apply on hit effects, passing in context, player stats, and enemy status controller

    }

    public void RespawnPlayerAtLevelStart()
    {
        if (player == null)
        {
            Debug.LogError("BattleManager: Player reference not set for respawn.");
            return;
        }

        if (playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
            player.transform.rotation = playerSpawnPoint.rotation;
            Debug.Log($"BattleManager: Player moved to spawn point {playerSpawnPoint.position}.");
        }
        else
        {
            Debug.LogWarning("BattleManager: playerSpawnPoint is null; player position unchanged during respawn.");
        }

        if (playerHealth != null)
        {
            playerHealth.ReviveToFullLife();
            Debug.Log($"BattleManager: Player revived to full life ({playerHealth.CurrentLife}).");
        }
        else
        {
            Debug.LogWarning("BattleManager: playerHealth is null; cannot revive player.");
        }

        Debug.Log("BattleManager: Spawning new normal enemy for restarted level.");
        SpawnNextEnemy(spawnBoss: false);
    }

    /// <summary>
    /// Called by HealthComponent on enemy death to let the battle manager clean up if needed.
    /// </summary>
    public void NotifyEnemyDied(HealthComponent enemyHealthComponent)   //Unused currently, notifies of enemy death and sets related fields to null
    {
        if (enemyHealthComponent == enemyHealth)
        {
            Debug.Log("BattleManager: Current enemy died.");
            currentEnemy = null;
            enemyAI = null;
            enemyStats = null;
            enemyHealth = null;
        }
    }

    //Note that I'll later likely implement the logic allowing for ailment chance stacking over 100% here. Such that it rolls for random < ailmentchance, then subtracts 100 from ailmentchance, increments a counter, and rolls again until failed, then applies (counter) number of stacks 
    private void ApplyOnHitEffects(DamageContext ctx, StatsComponent attackerStats, StatusController targetStatusCont)
    {
        if (attackerStats == null || targetStatusCont == null) return;      //if attacker stats or target status cont is null, return
        if (ctx.Hits == null || ctx.Hits.Count == 0) return;        //if context.hits is null, or the context has nothing in the hits list, return

        // Poison
        if (poisonEffect != null)       //if poisoneffect is not null
        {
            float poisonChance = attackerStats.GetStat(StatTypes.PoisonChance); //grab the poison chance stat from the attacker's stats
            if (Random.value < poisonChance)        //get a random value, if it is less than the poison chance, call applyailmentfromhit on the target's status cont, passing in the poison effect, context, attacker stats, and stacksperhit (default 1)
            {
                targetStatusCont.ApplyAilmentFromHit(
                    poisonEffect,
                    ctx,
                    attackerStats,
                    stacksPerHit: 1);
            }
        }

        // Bleed
        if (bleedEffect != null)    //If bleed effect is not null
        {
            float bleedChance = attackerStats.GetStat(StatTypes.BleedChance);   //grab the bleed chance from attacker stats
            if (Random.value < bleedChance)     //get a random value and check if it's less than the bleed chance, if it is, apply bleed using the bleed effect, context, and attacker stats, as well as the stacks per hit
            {
                targetStatusCont.ApplyAilmentFromHit(
                    bleedEffect,
                    ctx,
                    attackerStats,
                    1);
            }
        }

        // Ignite
        if (igniteEffect != null)   //if ignite is not null
        {
            float igniteChance = attackerStats.GetStat(StatTypes.IgniteChance);     //grab the attacker's ignite chance
            if (Random.value < igniteChance)        //get a random value, if it is less than the ignite chance, apply ailment from hit
            {
                targetStatusCont.ApplyAilmentFromHit(
                    igniteEffect,
                    ctx,
                    attackerStats,
                    1);
            }
        }
    }

}
