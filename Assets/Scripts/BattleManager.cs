// Developer map: Owns combat clocks, target replacement and damage resolution. Attack speeds are attacks/second at threshold 100; sprite poses only visualize this clock and never cause damage.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public static event System.Action<BattleManager> InstanceChanged;

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
    [SerializeField] private StatusEffects chillEffect;
    [SerializeField] private StatusEffects shockEffect;


    private PlayerController playerController;
    private StatusController playerStatusCont;
    private StatsComponent playerStats;
    private HealthComponent playerHealth;
    private DamageReceiver playerDamageReceiver;

    private Animator playerAnimator;
    private Animator enemyAnimator;
    private PaperSpriteActor playerSprite;
    private PaperSpriteActor enemySprite;

    private EnemyAI enemyAI;
    public EnemyAI CurrentEnemyAI => enemyAI;
    public event System.Action<EnemyAI> CurrentEnemyChanged;
    private StatusController enemyStatusCont;
    private StatsComponent enemyStats;
    private HealthComponent enemyHealth;
    private DamageReceiver enemyDamageReceiver;
    private GameObject currentEnemy;
    public Transform CurrentEnemyTransform => currentEnemy != null ? currentEnemy.transform : null;
    public bool CanCastPlayerSkill => playerHealth != null && playerHealth.CurrentLife > 0f
        && currentEnemy != null && enemyHealth != null && enemyHealth.CurrentLife > 0f
        && !SkillTreeUI.IsOpen && !PlayerSkillMenuUI.IsOpen && Time.timeScale > 0f;

    [Header("Speed / Gauge Settings")]
    [SerializeField] private float turnThreshold = 100f;        //Turn threshold is basically the size of the gauge that is filled before a player or enemy attacks, the speed at which it fills is determined by attack speed value
    private float playerGauge;
    private float enemyGauge;
    private bool playerHasLandedTurn;
    private bool enemyHasLandedTurn;

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
        InstanceChanged?.Invoke(this);
        // You can add DontDestroyOnLoad later if you want it to persist between scenes.
        //DontDestroyOnLoad(gameObject);
    }

    private void Start() => EnsurePlayerReferences();

    public bool EnsurePlayerReferences()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject;
        }

        if (player == null)
        {
            Debug.LogError("BattleManager: Player reference not set.");
            return false;
        }

        playerController = player.GetComponent<PlayerController>();     //Grab the player statuscont, stats, controller, and health components
        playerStatusCont = player.GetComponent<StatusController>();
        playerStats = player.GetComponent<StatsComponent>();
        playerHealth = player.GetComponent<HealthComponent>();
        playerDamageReceiver = GetOrAddDamageReceiver(player);
        playerAnimator = player.GetComponent<Animator>();
        playerSprite = player.GetComponent<PaperSpriteActor>();
        EnsureAilmentDefinitions();

        if (playerController == null || playerStats == null || playerHealth == null)        //if any of these (except statuscont) are null, give error
        {
            Debug.LogError("BattleManager: Player is missing required components.", player);
        }

        Debug.Log($"BattleManager: Start refs -> player={player.name}, playerSpawnPoint={(playerSpawnPoint != null ? playerSpawnPoint.name : "null")}, enemySpawnPoint={(enemySpawnPoint != null ? enemySpawnPoint.name : "null")}");
        return playerController != null && playerStats != null && playerHealth != null;
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
        EnsurePlayerReferences();
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

        playerHasLandedTurn = false;
        if (playerSprite != null) playerSprite.CancelAttack();
        if (currentEnemy != null)       //if current enemy isn't null, destroy it
        {
            Destroy(currentEnemy);
        }

        currentEnemy = Instantiate(prefab, enemySpawnPoint.position, enemySpawnPoint.rotation);     //instantiate a new enemy object using the spawn point and prefab, and assign that to the currentEnemy variable

        enemyAI = currentEnemy.GetComponent<EnemyAI>();     //grab the enemyAI, statuscont, stats, and health component of the newly instantiated enemy
        enemyStatusCont = currentEnemy.GetComponent<StatusController>();
        enemyStats = currentEnemy.GetComponent<StatsComponent>();
        enemyHealth = currentEnemy.GetComponent<HealthComponent>();
        enemyDamageReceiver = GetOrAddDamageReceiver(currentEnemy);
        enemyAnimator = currentEnemy.GetComponent<Animator>();
        enemySprite = currentEnemy.GetComponent<PaperSpriteActor>();

        if (enemyAI == null || enemyStatusCont == null || enemyStats == null || enemyHealth == null)       //if any of these (except statuscont) are null, give error and return
        {
            Debug.LogError("BattleManager: Enemy is missing required components (EnemyAI/StatsComponent/HealthComponent).", currentEnemy);
            currentEnemy = null;
            enemyAI = null;
            enemyStatusCont = null;
            enemyStats = null;
            enemyHealth = null;
            enemyDamageReceiver = null;
            enemyAnimator = null;
            CurrentEnemyChanged?.Invoke(null);
            return;
        }

        var zoneManager = FindFirstObjectByType<ZoneManager>();     //find the zone manager for the current zone
        int zoneLevel = zoneManager != null ? zoneManager.zoneLevel : 1;        //get the zone level from the zone manager, if the zone manager is null, set zone level to 1

        Debug.Log($"BattleManager: Spawning {(spawnBoss ? "BOSS" : "normal")} enemy at zone level {zoneLevel}. " +
                  $"normalPrefab={(normalEnemyPrefab ? normalEnemyPrefab.name : "null")}, " +
                  $"bossPrefab={(bossEnemyPrefab ? bossEnemyPrefab.name : "null")}");

        enemyHealth.SetEnemyRole(spawnBoss);
        enemyAI.InitializeEnemy(zoneLevel);     //call initialize enemy, passing in the zone level

        enemyGauge = 0f;        //set player and enemy gauges to 0, and global turn counter to 0.
        enemyHasLandedTurn = false;
        if (enemySprite != null) enemySprite.CancelAttack();
        playerGauge = 0f;
        globalTurnCounter = 0;
        CurrentEnemyChanged?.Invoke(enemyAI);
        GamePersistence.RecordEncounterStart(playerHealth, GetPlayerMana());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        CurrentEnemyChanged?.Invoke(null);
        Instance = null;
        InstanceChanged?.Invoke(null);
    }

    private void Update()
    {
        if (SkillTreeUI.PausesGameplay || PlayerSkillMenuUI.IsOpen || Time.timeScale <= 0f) return;
        if (playerHealth == null || playerHealth.CurrentLife <= 0f
            || currentEnemy == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f)
        {
            playerGauge = 0f;
            playerHasLandedTurn = false;
            if (playerSprite != null) playerSprite.CancelAttack();
            if (enemySprite != null) enemySprite.CancelAttack();
            return;
        }
        if (playerController == null || enemyAI == null) return;
        if (turnThreshold <= 0f) return;
        var tickTarget = currentEnemy;

        float playerSpeed = playerController.GetFinalAttackSpeed()
            * (1f - (playerStatusCont != null ? playerStatusCont.CurrentChillSlow : 0f)) * 100f;
        float enemySpeed = enemyAI.GetFinalAttackSpeed()
            * (1f - (enemyStatusCont != null ? enemyStatusCont.CurrentChillSlow : 0f)) * 100f;

        if (playerSpeed <= 0f)
        {
            playerGauge = 0f;
            playerHasLandedTurn = false;
            if (playerSprite != null) playerSprite.CancelAttack();
        }
        playerGauge += Mathf.Max(0f, playerSpeed) * Time.deltaTime;
        if (enemySpeed <= 0f)
        {
            enemyGauge = 0f;
            enemyHasLandedTurn = false;
            if (enemySprite != null) enemySprite.CancelAttack();
        }
        enemyGauge += Mathf.Max(0f, enemySpeed) * Time.deltaTime;

        // Wind up just before the gauge fills; show strike on the exact damage turn.
        if (playerSprite != null) playerSprite.SetAnticipation(playerGauge >= turnThreshold - playerSpeed * 0.08f);
        if (playerSprite != null && playerSpeed > 0f)
            playerSprite.ShowAttackGauge(playerGauge / turnThreshold, playerHasLandedTurn);
        if (enemySprite != null) enemySprite.SetAnticipation(enemyGauge >= turnThreshold - enemySpeed * 0.08f);
        if (enemySprite != null && enemySpeed > 0f)
            enemySprite.ShowAttackGauge(enemyGauge / turnThreshold, enemyHasLandedTurn);

        int safety = 10;

        while ((playerGauge >= turnThreshold || enemyGauge >= turnThreshold) && safety-- > 0)       //if player gauge or enemygauge exceeeds the turn threshold, and the safety is greater than 0 (decrement safety after each loop)
        {
            bool playerActs = playerGauge >= turnThreshold && playerGauge >= enemyGauge;    //player acts if playergauge exceeds the turn threshold and is greater than or equal to the enemy gauge

            if (playerActs)     //if player acts is true, subtract the turn threshold from the player's gauge and call resolve player turn, otherwise, subtract from enemy gauge and call resolve enemy turn
            {
                playerGauge -= turnThreshold;

                playerHasLandedTurn = true;
                if (playerSprite == null) TriggerAttackAnimation(playerAnimator, playerSpeed);

                ResolvePlayerTurn();
            }
            else
            {
                enemyGauge -= turnThreshold;

                enemyHasLandedTurn = true;
                if (enemySprite == null) TriggerAttackAnimation(enemyAnimator, enemySpeed);

                ResolveEnemyTurn();
            }

            if (playerHealth.CurrentLife <= 0f || currentEnemy != tickTarget || currentEnemy == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;
        }
    }

    private void ResolvePlayerTurn()
    {
        if (currentEnemy == null || enemyStatusCont == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;       //if there is no enemy, or it has no hp, or its HP is less than or equal to 0, return

        var originalTarget = enemyHealth;
        var originalStatuses = enemyStatusCont;
        globalTurnCounter++;        //increment global turn counter

        TickStatusController(playerStatusCont,true); // Player's afflicted-actor turn.
        if (playerHealth.CurrentLife <= 0f) return;
        TickStatusController(enemyStatusCont,false);
        if (enemyHealth != originalTarget || originalTarget.CurrentLife <= 0f) return;

        ResolvePlayerLogicalHit(null, originalTarget, originalStatuses, showImpact: true);

        // Hit Twice is one independent bonus hit, and deliberately does not recurse.
        if (IsSameLivingEnemy(originalTarget) && Random.value < Mathf.Clamp01(AdjustedChance(playerStats, StatTypes.ChanceToHitTwice)))
            ResolvePlayerLogicalHit(null, originalTarget, originalStatuses, showImpact: true);
    }

    private void ResolveEnemyTurn()
    {
        if (currentEnemy == null || enemyHealth == null || enemyHealth.CurrentLife <= 0f) return;       //if enemy, or their health is null or less than or equal to 0, return
        if (playerHealth == null || playerStatusCont == null || playerHealth.CurrentLife <= 0f) return;     //if player health is null or less than or equal to 0, return

        var originalAttacker = enemyHealth;
        var originalTarget = playerHealth;
        globalTurnCounter++;        //increment global turn counter

        TickStatusController(playerStatusCont,false);
        if (playerHealth.CurrentLife <= 0f) return;
        TickStatusController(enemyStatusCont,true); // Enemy's afflicted-actor turn.
        if (enemyHealth != originalAttacker || originalAttacker.CurrentLife <= 0f) return;

        ResolveEnemyLogicalHit(originalAttacker, originalTarget);

        // Exactly one independent bonus hit. It neither recurses nor transfers to
        // a replacement/dead player or a replacement/dead enemy attacker.
        if (IsSameLivingEnemyAttacker(originalAttacker) && IsSameLivingPlayer(originalTarget)
            && Random.value < Mathf.Clamp01(AdjustedChance(enemyStats, StatTypes.ChanceToHitTwice)))
            ResolveEnemyLogicalHit(originalAttacker, originalTarget);
    }

    private void ResolveEnemyLogicalHit(HealthComponent originalAttacker, HealthComponent originalTarget)
    {
        if (!IsSameLivingEnemyAttacker(originalAttacker) || !IsSameLivingPlayer(originalTarget)) return;
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

        if (playerDamageReceiver == null)
            playerDamageReceiver = GetOrAddDamageReceiver(player);

        if (playerDamageReceiver != null)
        {
            // Status ticks may have killed/replaced the attacker above. Only show
            // impact once this particular enemy is actually applying its hit.
            if (enemySprite != null) enemySprite.Strike();
            playerDamageReceiver.TakeDamage(damageTaken, ctx);     //call lose life in player script, passing in damage taken

            if (damageTaken > 0f && originalAttacker != null && enemyStats != null)
                originalAttacker.RestoreLife(enemyStats.GetStat(StatTypes.LifeOnHit));
        }
        else
            Debug.LogWarning("BattleManager: Player DamageReceiver is missing; enemy hit was not applied.", player);

        if (IsSameLivingPlayer(originalTarget))
            ApplyOnHitEffects(ctx, enemyStats, playerStatusCont);       //call apply on hit effects, passing in context, player stats, and enemy status controller

    }

    private bool IsSameLivingEnemyAttacker(HealthComponent source) => source != null
        && source == enemyHealth && currentEnemy != null && source.CurrentLife > 0f;

    private bool IsSameLivingPlayer(HealthComponent target) => target != null
        && target == playerHealth && player != null && target.CurrentLife > 0f;

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
            GetPlayerMana()?.RestoreFull();
            Debug.Log($"BattleManager: Player revived to full life ({playerHealth.CurrentLife}).");
        }
        else
        {
            Debug.LogWarning("BattleManager: playerHealth is null; cannot revive player.");
        }

        Debug.Log("BattleManager: Spawning new normal enemy for restarted level.");
        SpawnNextEnemy(spawnBoss: false);
    }

    private DamageReceiver GetOrAddDamageReceiver(GameObject target)
    {
        if (target == null)
            return null;

        var receiver = target.GetComponent<DamageReceiver>();
        if (receiver == null)
            receiver = target.AddComponent<DamageReceiver>();

        return receiver;
    }

    private void TriggerAttackAnimation(Animator animator, float attackSpeed)
    {
        if (animator == null)
            return;

        animator.SetFloat("AttackSpeed", attackSpeed);
        animator.SetTrigger("Attacking");
    }

    private void TickStatusController(StatusController statusController,bool afflictedActorTurn)
    {
        if (statusController != null)
            statusController.TickStatuses(afflictedActorTurn);
    }

    public bool TryCastPlayerSkill(PlayerSkillDefinition skill)
    {
        if (skill == null || !CanCastPlayerSkill) return false;
        HealthComponent target = enemyHealth;
        StatusController statuses = enemyStatusCont;

        if (skill.projectile)
        {
            int projectileCount = GetPlayerProjectileCount(skill);
            for (int i = 0; i < projectileCount; i++)
            {
                float offset = (i - (projectileCount - 1) * .5f) * .18f;
                // Build at cast time: current-mana scaling is locked after the
                // cost has been paid and cannot drift while the projectile flies.
                DamageContext snapshot = playerController.BuildAttackContext(
                    skill.conversionElement, skill.nonMatchingConversion, skill.DamageScopes);
                SkillProjectile.Launch(player.transform, currentEnemy.transform, skill.conversionElement,
                    () => ResolvePlayerProjectile(skill, target, statuses, snapshot), offset);
            }
        }
        else
        {
            ResolvePlayerSkill(skill, target, statuses);
        }
        return true;
    }

    private void ResolvePlayerProjectile(PlayerSkillDefinition skill, HealthComponent target,
        StatusController statuses, DamageContext snapshot)
    {
        if (!IsSameLivingEnemy(target)) return;
        ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, normalSnapshot: snapshot);
        if (IsSameLivingEnemy(target)
            && Random.value < Mathf.Clamp01(AdjustedChance(playerStats, StatTypes.ChanceToHitTwice)))
            ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, normalSnapshot: snapshot);
    }

    public int GetPlayerProjectileCount(PlayerSkillDefinition skill)
    {
        if (skill == null || !skill.projectile) return 0;
        float rawAmount = playerStats != null ? playerStats.GetRawStat(StatTypes.ProjectileAmount) : 0f;
        PassiveKeystoneState state = player != null ? player.GetComponent<PassiveKeystoneState>() : null;
        return CalculateProjectileCount(rawAmount, state != null ? state.ProjectileAmountBonus : 0);
    }

    public static int CalculateProjectileCount(float rawAmount, int keystoneBonus) =>
        Mathf.Max(1, 1 + Mathf.FloorToInt(Mathf.Max(0f, rawAmount)) + Mathf.Max(0, keystoneBonus));

    private void ResolvePlayerSkill(PlayerSkillDefinition skill, HealthComponent target, StatusController statuses)
    {
        if (!IsSameLivingEnemy(target)) return;
        int baseHits = Mathf.Max(1, skill.baseHitCount);
        int hits = baseHits;
        if (skill.additionalHitsFromShockChance)
        {
            int applications = RollOverflowApplications(AdjustedChance(playerStats, StatTypes.ShockChance));
            var state = player != null ? player.GetComponent<PassiveKeystoneState>() : null;
            float requirement = state != null ? state.ShockStackRequirementMultiplier : 1f;
            hits += Mathf.FloorToInt(applications / Mathf.Max(.01f, requirement));
        }

        for (int i = 0; i < hits && IsSameLivingEnemy(target); i++)
        {
            ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, shockTriggered: i >= baseHits);
            if (IsSameLivingEnemy(target)
                && Random.value < Mathf.Clamp01(AdjustedChance(playerStats, StatTypes.ChanceToHitTwice)))
                ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, shockTriggered: i >= baseHits);
        }
    }

    private void ResolvePlayerLogicalHit(PlayerSkillDefinition skill, HealthComponent target,
        StatusController statuses, bool showImpact, bool shockTriggered = false,
        DamageContext? normalSnapshot = null)
    {
        if (!IsSameLivingEnemy(target)) return;
        DamageContext normal = normalSnapshot ?? (skill == null
            ? playerController.BuildAttackContext()
            : playerController.BuildAttackContext(skill.conversionElement, skill.nonMatchingConversion, skill.DamageScopes));
        var keystones = player != null ? player.GetComponent<PassiveKeystoneState>() : null;
        if (shockTriggered && keystones != null && !Mathf.Approximately(keystones.ShockTriggeredHitMultiplier, 1f))
            normal = TransformContext(normal, keystones.ShockTriggeredHitMultiplier, Element.Phys, 0f);
        bool poisonTransmutation = keystones != null && keystones.TransmutesHitsToPoison;
        PlayerSkillController skillController = player != null ? player.GetComponent<PlayerSkillController>() : null;
        int skillLevel = skill != null && skillController != null ? skillController.EffectiveSkillLevel(skill) : 1;
        float skillLevelFactor = PlayerSkillController.SkillDamageLevelFactor(skillLevel);
        DamageContext direct = poisonTransmutation
            ? new DamageContext(1) { Scopes = normal.Scopes, IsCrit = normal.IsCrit, CritMultiplier = normal.CritMultiplier }
            : skill == null ? normal : TransformContext(normal, skill.hitDamageMultiplier * skillLevelFactor, skill.conversionElement, 0f);

        DamageContext specialized = default;
        if (skill != null && skill.specializedAilment != StatusEffects.AilmentKind.None)
        {
            if (skill.id == PlayerSkillId.Envenom)
                specialized = TransformContext(normal, skill.ailmentBasisMultiplier * skillLevelFactor, skill.conversionElement, 0f);
            else if (skill.id == PlayerSkillId.Immolate)
                specialized = TransformContext(normal, skill.ailmentBasisMultiplier * skillLevelFactor, skill.conversionElement, 0f);
            else
                specialized = TransformContext(direct, skill.ailmentBasisMultiplier, skill.conversionElement, 0f);
        }

        float rawTotal = SumDamage(direct);
        float damageTaken = CombatCalculator.CalculateFinalDamage(direct, playerStats, enemyStats);
        string attackName = skill == null ? "Basic Attack" : skill.displayName;
        Debug.Log($"[{attackName}] Player hits enemy. Raw={rawTotal:F1}, Final={damageTaken:F1}, " +
                  $"Crit={normal.IsCrit}, CritMult={normal.CritMultiplier:F2}");

        if (enemyDamageReceiver == null) enemyDamageReceiver = GetOrAddDamageReceiver(currentEnemy);
        if (showImpact && playerSprite != null) playerSprite.Strike();
        if (skill != null && skill.id == PlayerSkillId.HeavyStrike) PlayHeavyStrikeFeedback();
        if (damageTaken > 0f && enemyDamageReceiver != null)
        {
            enemyDamageReceiver.TakeDamage(damageTaken, direct);
            playerHealth.RestoreLife(playerStats.GetStat(StatTypes.LifeOnHit));
            GetPlayerMana()?.Restore(playerStats.GetStat(StatTypes.ManaOnHit));
        }

        if (IsSameLivingEnemy(target))
        {
            DamageContext poisonTransmutationBasis = poisonTransmutation
                ? TransformContext(PassiveKeystoneState.AsPoisonBasis(normal), skillLevelFactor, Element.Phys, 0f)
                : specialized;
            ApplyOnHitEffects(direct, playerStats, statuses, skill,
                poisonTransmutationBasis, poisonTransmutation);
        }
    }

    private bool IsSameLivingEnemy(HealthComponent target)
    {
        return target != null && target == enemyHealth && currentEnemy != null && target.CurrentLife > 0f;
    }

    private ManaComponent GetPlayerMana()
    {
        return player != null ? player.GetComponent<ManaComponent>() : null;
    }

    private static float SumDamage(DamageContext context)
    {
        float total = 0f;
        if (context.Hits != null) foreach (var hit in context.Hits) total += hit.Amount;
        return total;
    }

    private static DamageContext TransformContext(DamageContext source, float multiplier,
        Element conversionElement, float conversion)
    {
        var result = new DamageContext(source.Hits != null ? source.Hits.Count + 1 : 1)
        {
            IsCrit = source.IsCrit,
            CritMultiplier = source.CritMultiplier,
            Scopes = source.Scopes
        };
        if (source.Hits == null || multiplier <= 0f) return result;
        conversion = Mathf.Clamp01(conversion);
        foreach (var hit in source.Hits)
        {
            float scaled = hit.Amount * multiplier;
            if (conversion <= 0f || hit.Element == conversionElement)
            {
                result.AddDamage(hit.Element, scaled);
                continue;
            }
            result.AddDamage(hit.Element, scaled * (1f - conversion));
            result.AddDamage(conversionElement, scaled * conversion);
        }
        return result;
    }

    private void PlayHeavyStrikeFeedback()
    {
        if (Camera.main != null) StartCoroutine(HeavyStrikeShake(Camera.main.transform));
        var source = player.GetComponent<AudioSource>();
        if (source == null) source = player.AddComponent<AudioSource>();
        source.pitch = .72f;
        source.PlayOneShot(SkillProjectile.HeavyImpactClip, .55f);
    }

    private System.Collections.IEnumerator HeavyStrikeShake(Transform cameraTransform)
    {
        Vector3 start = cameraTransform.localPosition;
        for (int i = 0; i < 4; i++)
        {
            cameraTransform.localPosition = start + (Vector3)Random.insideUnitCircle * .035f;
            yield return null;
        }
        cameraTransform.localPosition = start;
    }

    private void EnsureAilmentDefinitions()
    {
        if (poisonEffect == null) poisonEffect = RuntimeEffect("Poison", StatusEffects.StatusType.DamageOverTime,
            StatusEffects.AilmentKind.Poison, ElementMask.Phys | ElementMask.Fire | ElementMask.Cold | ElementMask.Light | ElementMask.Void, .1f, 4, 0,
            StatusEffects.StackPolicy.StackIndependently, 2, new Color(.3f, 1f, .22f));
        if (bleedEffect == null) bleedEffect = RuntimeEffect("Bleed", StatusEffects.StatusType.DamageOverTime,
            StatusEffects.AilmentKind.Bleed, ElementMask.Phys, .5f, 5, 5,
            StatusEffects.StackPolicy.StackIndependently, 2, new Color(1f, .16f, .25f));
        if (igniteEffect == null) igniteEffect = RuntimeEffect("Burn", StatusEffects.StatusType.DamageOverTime,
            StatusEffects.AilmentKind.Ignite, ElementMask.Fire, .8f, 2, 1,
            StatusEffects.StackPolicy.ReplaceIfStronger, 2, new Color(1f, .35f, .1f));
        if (chillEffect == null) chillEffect = RuntimeEffect("Chill", StatusEffects.StatusType.Chill,
            StatusEffects.AilmentKind.None, ElementMask.Cold, 1f, 4, 1,
            StatusEffects.StackPolicy.ReplaceIfStronger, 1, new Color(.2f, .75f, 1f));
        if (shockEffect == null) shockEffect = RuntimeEffect("Shock", StatusEffects.StatusType.Shock,
            StatusEffects.AilmentKind.None, ElementMask.Light, 1f, 5, 5,
            StatusEffects.StackPolicy.StackAndRefresh, 1, Color.yellow);
    }

    private static StatusEffects RuntimeEffect(string name, StatusEffects.StatusType type,
        StatusEffects.AilmentKind ailment, ElementMask elements, float magnitude, int duration, int maxStacks,
        StatusEffects.StackPolicy policy, int interval, Color color)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffects>();
        effect.hideFlags = HideFlags.DontSave;
        effect.ConfigureRuntime(name, type, ailment, elements, magnitude, duration, maxStacks, policy, interval, color);
        return effect;
    }

    /// <summary>
    /// Called by HealthComponent on enemy death to let the battle manager clean up if needed.
    /// </summary>
    public void NotifyEnemyDied(HealthComponent enemyHealthComponent)
    {
        if (enemyHealthComponent == enemyHealth)
        {
            Debug.Log("BattleManager: Current enemy died.");
            currentEnemy = null;
            enemyAI = null;
            enemyStats = null;
            enemyHealth = null;
            enemyStatusCont = null;
            enemyDamageReceiver = null;
            enemyAnimator = null;
            enemySprite = null;
            CurrentEnemyChanged?.Invoke(null);
        }
    }

    private void ApplyOnHitEffects(DamageContext ctx, StatsComponent attackerStats,
        StatusController targetStatusCont, PlayerSkillDefinition skill = null, DamageContext specialized = default,
        bool poisonTransmutation = false)
    {
        if (attackerStats == null || targetStatusCont == null) return;

        DamageContext poisonBasis = poisonTransmutation ? specialized
            : skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Poison
            ? specialized : ctx;
        DamageContext bleedBasis = skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Bleed
            ? specialized : ctx;
        DamageContext igniteBasis = skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Ignite
            ? specialized : ctx;

        ApplyConfiguredStatus(poisonEffect, StatTypes.PoisonChance, poisonBasis, attackerStats, targetStatusCont,
            poisonTransmutation ? 1 : 0, poisonTransmutation ? 1f : -1f, poisonTransmutation);
        ApplyConfiguredStatus(bleedEffect, StatTypes.BleedChance, bleedBasis, attackerStats, targetStatusCont);
        ApplyConfiguredStatus(igniteEffect, StatTypes.IgniteChance, igniteBasis, attackerStats, targetStatusCont,
            0, skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Ignite
                ? skill.absoluteAilmentCoefficient : -1f);
        ApplyChillFromHit(ctx, attackerStats, targetStatusCont,
            skill != null ? skill.guaranteedAdditionalChill : 0);
        ApplyShockFromHit(ctx, attackerStats, targetStatusCont);
    }

    private void ApplyShockFromHit(DamageContext context, StatsComponent attacker, StatusController target)
    {
        if (shockEffect == null || attacker == null || target == null) return;
        StatsComponent defender = target.GetComponent<StatsComponent>();
        float lightningDealt = CombatCalculator.CalculateFinalElementDamage(context, Element.Light, attacker, defender);
        if (lightningDealt <= 0f) return;

        float chance = AdjustForApplicationResistance(shockEffect,
            AdjustedChance(attacker, StatTypes.ShockChance), attacker, defender);
        int stacks = RollOverflowApplications(chance);
        if (stacks <= 0) return;

        PassiveKeystoneState keystones = attacker.GetComponent<PassiveKeystoneState>();
        int threshold = Mathf.Max(1, Mathf.CeilToInt(5f
            * (keystones != null ? keystones.ShockStackRequirementMultiplier : 1f)));
        if(attacker.GetComponent<PlayerController>()!=null&&RelicInventory.Instance!=null)
            threshold=Mathf.Max(3,threshold-RelicInventory.Instance.ShockThresholdReduction);
        int duration = Mathf.Max(1, 5 + Mathf.RoundToInt(attacker.GetRawStat(StatTypes.ShockDuration)));
        float coefficient = Mathf.Min(1f, .5f * (1f + attacker.GetStat(StatTypes.ShockEffect))
            * (keystones != null ? keystones.ShockTriggeredHitMultiplier : 1f));
        int triggers = target.AddShockStacks(shockEffect, stacks, duration, coefficient, threshold);
        if (triggers <= 0) return;

        HealthComponent targetHealth = target.GetComponent<HealthComponent>();
        DamageReceiver receiver = target.GetComponent<DamageReceiver>();
        if (receiver == null) receiver = target.gameObject.AddComponent<DamageReceiver>();
        float triggeredDamage = lightningDealt * coefficient;
        for (int i = 0; i < triggers && targetHealth != null && targetHealth.CurrentLife > 0f; i++)
            receiver.TakeDamage(triggeredDamage, Element.Light);
    }

    private void ApplyChillFromHit(DamageContext context, StatsComponent attacker,
        StatusController target, int guaranteedApplications)
    {
        if (chillEffect == null || attacker == null || target == null) return;
        StatsComponent defender = target.GetComponent<StatsComponent>();
        HealthComponent targetHealth = target.GetComponent<HealthComponent>();
        if (targetHealth == null || targetHealth.MaxLife <= 0f) return;
        float coldDealt = CombatCalculator.CalculateFinalElementDamage(context, Element.Cold, attacker, defender);
        if (coldDealt <= 0f) return;

        float chance = AdjustForApplicationResistance(chillEffect,
            AdjustedChance(attacker, StatTypes.ChillChance), attacker, defender);
        if (guaranteedApplications <= 0 && RollOverflowApplications(chance) <= 0) return;

        float resistance = defender != null
            ? defender.GetStat(StatTypes.ChillRes) + defender.GetStat(StatTypes.AllAilmentRes) : 0f;
        float resistanceFactor = 1f - Mathf.Clamp(resistance, -.9f, .9f);
        PassiveKeystoneState keystones = attacker.GetComponent<PassiveKeystoneState>();
        float effectiveness = keystones != null ? keystones.ChillEffectMultiplier : 1f;
        float cap = .3f + (keystones != null ? keystones.DeepFreezeMaximumEffectIncrease : 0f)
            + (attacker.GetComponent<PlayerController>()!=null?RelicInventory.Instance?.MaximumChillSlowIncrease??0f:0f);
        float slow = CalculateChillSlow(coldDealt, targetHealth.MaxLife,
            attacker.GetStat(StatTypes.ChillEffect), effectiveness, cap) * resistanceFactor;
        int duration = Mathf.Max(1, Mathf.RoundToInt((4f
            + attacker.GetRawStat(StatTypes.ChillDuration)) * resistanceFactor));
        target.ApplyChill(chillEffect, Mathf.Min(cap, slow), duration,cap);
    }

    public static float CalculateChillSlow(float coldDamageDealt, float targetMaximumLife,
        float chillEffect, float effectivenessMultiplier = 1f, float maximumSlow = .3f)
    {
        if (coldDamageDealt <= 0f || targetMaximumLife <= 0f) return 0f;
        float raw = Mathf.Clamp(.05f + coldDamageDealt / targetMaximumLife, .05f, .3f);
        return Mathf.Clamp(raw * Mathf.Max(0f, 1f + chillEffect)
            * Mathf.Max(0f, effectivenessMultiplier), 0f, Mathf.Max(0f, maximumSlow));
    }

    private static void ApplyConfiguredStatus(StatusEffects effect, StatTypes chance, DamageContext ctx,
        StatsComponent attacker, StatusController target, int guaranteedApplications = 0,
        float magnitudeOverride = -1f, bool guaranteedOnly = false)
    {
        if (effect == null || ctx.Hits == null || ctx.Hits.Count == 0
            || AilmentCalculator.GetSourceHitDamage(effect, ctx) <= 0f) return;
        float adjustedChance = AdjustedChance(attacker, chance);
        StatsComponent defender = target != null ? target.GetComponent<StatsComponent>() : null;
        adjustedChance = AdjustForApplicationResistance(effect, adjustedChance, attacker, defender);
        int applications = guaranteedApplications
            + (guaranteedOnly ? 0 : RollOverflowApplications(adjustedChance));
        if (applications > 0)
            target.ApplyAilmentFromHit(effect, ctx, attacker, applications, magnitudeOverride);
    }

    public static int RollOverflowApplications(float chance)
    {
        chance = Mathf.Max(0f, chance);
        int guaranteed = Mathf.FloorToInt(chance);
        float remainder = chance - guaranteed;
        return guaranteed + (remainder > 0f && Random.value < remainder ? 1 : 0);
    }

    public static float AdjustedChance(StatsComponent stats, StatTypes chance)
    {
        if (stats == null) return 0f;
        var keystones = stats.GetComponent<PassiveKeystoneState>();
        return stats.GetStat(chance) * (keystones != null ? keystones.ChanceMultiplier(chance) : 1f);
    }

    public static float AdjustForApplicationResistance(StatusEffects effect, float chance,
        StatsComponent attacker, StatsComponent defender)
    {
        chance = Mathf.Max(0f, chance);
        if (effect == null || defender == null) return chance;

        float resistance;
        if (effect.Ailment == StatusEffects.AilmentKind.Poison)
        {
            float penetration = attacker != null ? attacker.GetStat(StatTypes.PoisonPenetration) : 0f;
            resistance = defender.GetStat(StatTypes.PoisonRes)
                + defender.GetStat(StatTypes.AllAilmentRes) - penetration;
        }
        else if (effect._StatusType == StatusEffects.StatusType.Shock)
        {
            resistance = defender.GetStat(StatTypes.ShockRes) + defender.GetStat(StatTypes.AllAilmentRes);
        }
        else if (effect._StatusType == StatusEffects.StatusType.Chill)
        {
            resistance = defender.GetStat(StatTypes.ChillRes) + defender.GetStat(StatTypes.AllAilmentRes);
        }
        else return chance;

        return chance * (1f - Mathf.Clamp(resistance, -.9f, .9f));
    }
}
