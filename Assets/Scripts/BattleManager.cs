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
    private RageState playerRage;
    private SubclassCombatState subclassState;
    private float currentAttackEventMultiplier=1f;
    private BossSpecialEffectRuntime specialEffects;

    private Animator playerAnimator;
    private Animator enemyAnimator;
    private PaperSpriteActor playerSprite;
    private PaperSpriteActor enemySprite;

    private EnemyAI enemyAI;
    public EnemyAI CurrentEnemyAI => enemyAI;
    public EncounterDefinition CurrentEncounter { get; private set; }
    ChallengeEncounterDefinition pendingChallenge;
    public event System.Action<EnemyAI> CurrentEnemyChanged;
    private StatusController enemyStatusCont;
    private StatsComponent enemyStats;
    private HealthComponent enemyHealth;
    private DamageReceiver enemyDamageReceiver;
    private GameObject currentEnemy;
    public Transform CurrentEnemyTransform => currentEnemy != null ? currentEnemy.transform : null;
    public bool CanCastPlayerSkill => playerHealth != null && playerHealth.CurrentLife > 0f
        && currentEnemy != null && enemyHealth != null && enemyHealth.CurrentLife > 0f
        && !SkillTreeUI.IsOpen && !PlayerSkillMenuUI.IsOpen && Time.timeScale > 0f && RebirthManager.Instance?.IsSetup!=true;

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
        playerRage=player.GetComponent<RageState>()??player.AddComponent<RageState>();
        subclassState=player.GetComponent<SubclassCombatState>()??player.AddComponent<SubclassCombatState>();
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
        stealthReady=false;
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

        var zoneManager = FindFirstObjectByType<ZoneManager>();
        int zoneLevel = zoneManager != null ? zoneManager.zoneLevel : GameManager.Instance?.CurrentCombatLevel ?? 1;
        int stage = spawnBoss ? WorldProgression.BossStage : GameManager.Instance?.EncounterStage ?? 1;
        WorldContentDatabase content = zoneManager != null ? zoneManager.WorldContent : WorldContentCatalog.Reference;
        WorldPosition world = WorldProgression.Resolve(zoneLevel, stage, content,GamePersistence.CurrentRunSeed);
        ChallengeEncounterDefinition challenge=pendingChallenge;pendingChallenge=null;
        CurrentEncounter = challenge!=null?new EncounterDefinition{stableId=challenge.stableContentId,kind=EncounterKind.Boss,stage=WorldProgression.BossStage,bossId=challenge.bossId,futureRewardHooks=new(){challenge.rewardResourceId}}:world.Encounter;
        GameObject prefab;
        if (spawnBoss)
        {
            BossDefinition boss = content?.Boss(CurrentEncounter?.bossId);
            prefab = boss?.prefab != null ? boss.prefab : bossEnemyPrefab != null ? bossEnemyPrefab : normalEnemyPrefab;
        }
        else
        {
            EnemyArchetypeDefinition enemy = content?.Enemy(CurrentEncounter?.enemyArchetypeId);
            prefab = enemy?.prefab != null ? enemy.prefab : normalEnemyPrefab;
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
            playerStatusCont?.ReleaseSource(enemyStats);
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

        Debug.Log($"BattleManager: Spawning {(spawnBoss ? "BOSS" : "normal")} enemy at zone level {zoneLevel}. " +
                  $"encounterId={CurrentEncounter?.stableId ?? "unresolved"}, " +
                  $"normalPrefab={(normalEnemyPrefab ? normalEnemyPrefab.name : "null")}, " +
                  $"bossPrefab={(bossEnemyPrefab ? bossEnemyPrefab.name : "null")}");

        enemyHealth.SetEnemyRole(spawnBoss);
        enemyAI.ConfigureWorldContent(content,
            spawnBoss ? null : content?.Enemy(CurrentEncounter?.enemyArchetypeId),
            spawnBoss ? content?.Boss(CurrentEncounter?.bossId) : null,
            world.Corruption, world.Location);
        enemyAI.InitializeEnemy(zoneLevel);     //call initialize enemy, passing in the zone level

        UniqueCombatRuntime.For(playerStats)?.BindEnemy(enemyHealth);
        enemyGauge = 0f;        //set player and enemy gauges to 0, and global turn counter to 0.
        enemyHasLandedTurn = false;
        if (enemySprite != null) enemySprite.CancelAttack();
        playerGauge = 0f;
        globalTurnCounter = 0;
        CurrentEnemyChanged?.Invoke(enemyAI);
        subclassState?.ResetForEnemy();
        GamePersistence.RecordEncounterStart(playerHealth, GetPlayerMana());
        if(subclassState?.Has(SubclassIds.ThiefAssassin)==true)ResolvePlayerTurn();
    }

    public bool TrySpawnChallenge(ChallengeEncounterDefinition challenge)
    {
        if(challenge==null||WorldContentCatalog.Reference?.Boss(challenge.bossId)==null)return false;
        pendingChallenge=challenge;SpawnNextEnemy(true);
        return currentEnemy!=null&&CurrentEncounter!=null&&CurrentEncounter.stableId==challenge.stableContentId;
    }

    public void RestoreWorldEncounter()
    {
        pendingChallenge=null;int level=GameManager.Instance?.CurrentCombatLevel??1;int completed=GameManager.Instance?.NormalKills??0;bool boss=GameManager.Instance?.BossActive??false;
        GamePersistence.GenerateDeterministicEncounter(level,completed,boss,()=>SpawnNextEnemy(boss));
    }

    public void ReturnFromChallengeDefeat()
    {
        player?.GetComponent<PlayerSkillController>()?.ClearQueuedSkill();player?.GetComponent<StatusController>()?.ClearStatuses();
        if(playerSpawnPoint!=null&&player!=null){player.transform.position=playerSpawnPoint.position;player.transform.rotation=playerSpawnPoint.rotation;}
        playerHealth?.ReviveToFullLife();GetPlayerMana()?.RestoreFull();RestoreWorldEncounter();
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
        if (SkillTreeUI.PausesGameplay || PlayerSkillMenuUI.IsOpen || (Time.timeScale <= 0f||RebirthManager.Instance?.IsSetup==true)) return;
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

        var skillController = player != null ? player.GetComponent<PlayerSkillController>() : null;
        skillController?.PrepareAutomaticReplacement();
        subclassState?.BeginAttackOpportunity();
        specialEffects??=player!=null?player.GetComponent<BossSpecialEffectRuntime>()??player.AddComponent<BossSpecialEffectRuntime>():null;specialEffects?.BeginAttackEvent();currentAttackEventMultiplier=(playerRage!=null?playerRage.BeginAttackEventMultiplier():1f)*(player!=null?player.GetComponent<RevengeState>()?.ConsumeAttack()??1:1);bool resolved=false;
        try{if (skillController != null && skillController.TryConsumeQueuedForAttack(out var queued, out float manaSpent))
        {
            // Resolve against the enemy that exists at the scheduled attack event,
            // not the target that happened to exist when the button was pressed.
            if (!TryCastPlayerSkill(queued))
            {
                skillController.Mana.Restore(manaSpent);
                playerSprite?.Strike();
                ResolveBasicPlayerAttack(originalTarget, originalStatuses);resolved=true;
            }
            else {resolved=true;skillController.NotifyQueuedSkillResolved(queued);}
        }
        else {playerSprite?.Strike();ResolveBasicPlayerAttack(originalTarget, originalStatuses);resolved=true;}}
        finally{if(resolved)subclassState?.CompleteAttackOpportunity();playerRage?.CompleteAttackEvent(resolved);currentAttackEventMultiplier=1f;
            if(resolved&&IsSameLivingEnemyAttacker(originalTarget))ResolveRelicSkillTriggers(skillController);}
    }

    bool resolvingRelicTriggers;
    void ResolveRelicSkillTriggers(PlayerSkillController controller)
    {
        if(resolvingRelicTriggers||controller==null||RelicInventory.Instance==null||RebirthManager.Instance?.IsSetup==true)return;
        resolvingRelicTriggers=true;
        try { foreach(var id in RelicInventory.Instance.TriggeredSkills())
        {
            if(!controller.RelicTriggerEnabled(id))continue;
            PlayerSkillDefinition definition=null;foreach(var candidate in controller.Skills)if(candidate.id==id){definition=candidate;break;}
            if(definition==null||enemyHealth==null||enemyHealth.CurrentLife<=0||playerHealth.CurrentLife<=0)continue;
            float cost=controller.ManaCost(definition);if(!controller.Mana.TrySpend(cost))continue;
            if(!TryCastPlayerSkill(definition))controller.Mana.Restore(cost);
        } } finally {resolvingRelicTriggers=false;}
    }
    private void ResolveBasicPlayerAttack(HealthComponent target, StatusController statuses)
    {
        if(playerController?.EquippedWeapon!=null&&WeaponTypeCatalog.TryGet(playerController.EquippedWeapon.WeaponTypeId,out var profile)&&profile.IsRanged)
        {
            int count=SplitProjectileCount(CalculateProjectileCount(playerStats.GetRawStat(StatTypes.ProjectileAmount),0));float travel=WeaponMechanicProfile.ProjectileTravelTime(playerStats.GetStat(StatTypes.ProjectileSpeed));
            for(int i=0;i<count;i++){DamageContext snapshot=ApplyPrecision(ApplyWeaponMechanics(playerController.BuildAttackContext()),profile.SupportsPrecision);snapshot.Scopes|=DamageScope.Projectile;int index=i;SkillProjectile.Launch(player.transform,currentEnemy.transform,playerController.EquippedWeaponElement,()=>ResolvePlayerProjectile(null,target,statuses,snapshot,null),(i-(count-1)*.5f)*.18f,travel,index*WeaponMechanicProfile.ProjectileBarrageSpacing,profile.Id==WeaponTypeIds.Staff?"Magic Bolt":"Basic Projectile");}return;
        }
        ResolvePlayerLogicalHit(null, target, statuses, showImpact: true);
        // Hit Twice is one independent bonus hit, and deliberately does not recurse.
        if (CanPlayerMultistrike && !HasClassKeystone(PassiveKeystone.WarriorConsolidation) && IsSameLivingEnemy(target) && Random.value < Mathf.Clamp01(AdjustedChance(playerStats, StatTypes.ChanceToHitTwice)))
            ResolvePlayerLogicalHit(null, target, statuses, showImpact: true, extraStrike:true);
    }

    bool CanPlayerMultistrike => playerController?.EquippedWeapon!=null&&GenericPassiveMechanics.SupportsMultistrike(playerController.EquippedWeapon.WeaponTypeId);
    bool HasClassKeystone(PassiveKeystone key)=>player!=null&&player.GetComponent<PassiveKeystoneState>()?.Has(key)==true;
    bool stealthReady;

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

        // A dodged attack still advances global turns, aura lifetimes and DOTs.
        if(enemyStatusCont!=null&&enemyStatusCont.ConsumeFrozenAttackSkip()){specialEffects?.OnFreezeConsumed(playerHealth,GetPlayerMana());return;}
        if(stealthReady){stealthReady=false;if(Random.value<ClassKeystoneCatalog.Get(PassiveKeystone.ThiefStealth).secondary)return;}

        EnemySkillDefinition authoredSkill=enemyAI.BeginAuthoredTurn();
        int authoredHits=enemyAI.CurrentAuthoredHitCount;
        for(int authoredHit=0;authoredHit<authoredHits;authoredHit++)
        {
            if(!IsSameLivingEnemyAttacker(originalAttacker)||!IsSameLivingPlayer(originalTarget))break;
            ResolveEnemyLogicalHit(originalAttacker, originalTarget);
        }

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
        if(playerStats!=null&&Random.value<Mathf.Clamp01(playerStats.GetStat(StatTypes.ChanceToBlock)))
            ctx=TransformContext(ctx,WarriorSubclassRules.BlockedHitMultiplier,Element.Phys,0);

        float rawTotal = 0f;
        foreach (var hit in ctx.Hits)       //calculate the raw damage total using the damage context
        {
            rawTotal += hit.Amount;
        }

        float damageTaken = CombatCalculator.CalculateFinalDamage(ctx, enemyStats, playerStats)
            * (playerRage?.IncomingDamageMultiplier ?? 1f);        // Rage defense applies after ordinary mitigation.

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
playerDamageReceiver.TakeDamage(damageTaken, ctx, attacker:enemyStats);     //call lose life in player script, passing in damage taken
            if(damageTaken>0)subclassState?.EnemySuccessfulAttack();
            if(damageTaken>0)specialEffects?.NotifyPlayerTookDirectHit();
            playerRage?.GainFromDamageTaken(damageTaken,playerHealth.MaxLife);

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

        // A restart restores a clean encounter boundary. Queued attacks are
        // transient combat intent and must never survive that boundary.
        player.GetComponent<PlayerSkillController>()?.ClearQueuedSkill();
        stealthReady=false;

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
        if(!CastPlayerSkillOnce(skill))return false;
        if(!skill.magic)return true;
        var controller=player.GetComponent<PlayerSkillController>();
        float chance=SpellEchoRules.EffectiveChance(playerStats.GetStat(StatTypes.SpellEchoChance)+(RelicInventory.Instance?.SkillEchoChance(skill.id)??0));
        SpellEchoRules.CastEchoes(chance,controller.ManaCost(skill),()=>Random.value,cost=>
        {
            if(!CanCastPlayerSkill||!controller.Mana.TrySpend(cost))return false;
            if(!CastPlayerSkillOnce(skill)){controller.Mana.Restore(cost);return false;}
            UniqueCombatRuntime.For(playerStats)?.EchoSelfHit(SumDamage(playerController.BuildNonCriticalAttackContext()));
            return true;
        });
        return true;
    }

    // Echo invokes this primitive directly, never the whole attack/proc scheduler.
    bool CastPlayerSkillOnce(PlayerSkillDefinition skill)
    {
        if (skill == null || !CanCastPlayerSkill) return false;
        playerSprite?.Strike(); // One presentation event per cast, never per impact.
        HealthComponent target = enemyHealth;
        StatusController statuses = enemyStatusCont;
        DamageContext boltBasis=HasClassKeystone(PassiveKeystone.MageSelfBolt)&&(skill.DamageScopes&DamageScope.Magic)!=0
            ?ApplyWeaponMechanics(playerController.BuildNonCriticalAttackContext(skill.conversionElement,skill.nonMatchingConversion,skill.DamageScopes)):default;

        if (skill.projectile)
        {
            int projectileCount = GetPlayerProjectileCount(skill);
            if(skill.effect==WeaponSkillEffect.DoubleProjectiles)projectileCount*=2;
            float focusedMultiplier=1f;if(subclassState!=null)projectileCount=subclassState.FinalProjectileCount(projectileCount,out focusedMultiplier);
            projectileCount=SplitProjectileCount(projectileCount);
            for (int i = 0; i < projectileCount; i++)
            {
                float offset = (i - (projectileCount - 1) * .5f) * .18f;
                // Build at cast time: current-mana scaling is locked after the
                // cost has been paid and cannot drift while the projectile flies.
                DamageContext snapshot = ApplyPrecision(ApplyWeaponMechanics(playerController.BuildAttackContext(
                    skill.conversionElement, skill.nonMatchingConversion, skill.DamageScopes)),CanPrecision(skill));
                if(resolvingRelicTriggers)snapshot.EventTags|=CombatEventTags.RelicTriggeredSkill;
                if(!Mathf.Approximately(focusedMultiplier,1f))snapshot=TransformContext(snapshot,focusedMultiplier,Element.Phys,0);
                // A thrown Dagger is a projectile, not a melee event. Equipped weapon
                // classification alone must never grant projectile Multistrikes.
                DamageContext? bonusSnapshot = null;
                SkillProjectile.Launch(player.transform, currentEnemy.transform, skill.conversionElement,
                    () => ResolvePlayerProjectile(skill, target, statuses, snapshot, bonusSnapshot), offset,
                    WeaponMechanicProfile.ProjectileTravelTime(playerStats.GetStat(StatTypes.ProjectileSpeed),skill.baseProjectileSpeed),i*WeaponMechanicProfile.ProjectileBarrageSpacing);
            }
        }
        else
        {
            ResolvePlayerSkill(skill, target, statuses);
        }
        if(boltBasis.Hits!=null)ResolveKeystoneSpellBolt(skill,target,statuses,boltBasis);
        return true;
    }

    int SplitProjectileCount(int count)=>HasClassKeystone(PassiveKeystone.RangerSplit)?count*Mathf.RoundToInt(PassiveKeystoneState.Value(PassiveKeystone.RangerSplit)):count;
    void ResolveKeystoneSpellBolt(PlayerSkillDefinition skill,HealthComponent target,StatusController statuses,DamageContext basis)
    {
        int level=player.GetComponent<PlayerSkillController>()?.EffectiveSkillLevel(skill)??1;
        float amount=SumDamage(basis)*skill.hitDamageMultiplier*PlayerSkillController.SkillDamageLevelFactor(level)*PassiveKeystoneState.Value(PassiveKeystone.MageSelfBolt);
        var bolt=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers};bolt.AddDamage(Element.Light,amount);
        if(IsSameLivingEnemy(target))
        {
            float dealt=CombatCalculator.CalculateFinalDamage(bolt,playerStats,enemyStats);
            float actual=Mathf.Min(target.CurrentLife,dealt);
            enemyDamageReceiver.TakeDamage(dealt,bolt,attacker:playerStats);
            if(actual>0)playerHealth.RestoreLife(actual*(player.GetComponent<PassiveKeystoneState>()?.DamageRecoveryFraction??0),HealingSource.SubclassDamage);
            if(dealt>0){playerHealth.RestoreLife(playerStats.GetStat(StatTypes.LifeOnHit),HealingSource.LifeOnHit);GetPlayerMana()?.Restore(playerStats.GetStat(StatTypes.ManaOnHit));}
            if(IsSameLivingEnemy(target))ApplyOnHitEffects(bolt,playerStats,statuses);
        }
        // Incoming, not an offensive player hit: no player penetration or on-hit rewards.
        var incoming=new DamageContext(1){IncomingSelfHit=true,EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers};incoming.AddDamage(Element.Light,amount);
        float selfDamage=CombatCalculator.CalculateFinalDamage(incoming,null,playerStats)*(playerRage?.IncomingDamageMultiplier??1f);
        playerDamageReceiver??=GetOrAddDamageReceiver(player);playerDamageReceiver.TakeDamage(selfDamage,incoming);
        playerRage?.GainFromDamageTaken(selfDamage,playerHealth.MaxLife);
        if(selfDamage>0){specialEffects?.NotifyPlayerTookDirectHit();if(RollOverflowApplications(AdjustedChance(playerStats,StatTypes.ShockChance))>0)playerStatusCont.AddShockInstance(ShockRules.Effect(playerStats.GetStat(StatTypes.ShockEffect),ShockRules.BaseMaximumEffect+(RelicInventory.Instance?.MaximumShockEffectIncrease??0),playerStats.GetStat(StatTypes.ReducedShockEffect)),ShockRules.Duration(playerStats.GetRawStat(StatTypes.ShockDuration)/100f));}
    }

    public bool TryCastImmediatePlayerSkill(PlayerSkillDefinition skill)
    {
        if(skill==null||skill.castMode!=PlayerSkillCastMode.ImmediateCooldown||!CanCastPlayerSkill)return false;
        return TryCastPlayerSkill(skill);
    }

    private void ResolvePlayerProjectile(PlayerSkillDefinition skill, HealthComponent target,
        StatusController statuses, DamageContext snapshot, DamageContext? bonusSnapshot)
    {
        if (!IsSameLivingEnemy(target)) return;
        if(CanPrecision(skill)&&HasClassKeystone(PassiveKeystone.RangerPrecision)&&Random.value<PassiveKeystoneState.Value(PassiveKeystone.RangerPrecision))return;
        ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, normalSnapshot: snapshot);
        if (IsSameLivingEnemy(target) && bonusSnapshot.HasValue)
            ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, normalSnapshot: bonusSnapshot.Value);
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
        int baseHits = skill.effect==WeaponSkillEffect.ShockBarrage
                ?ShockRules.BarrageHits(statuses?.ConsumeShocks()??0f)
                :Mathf.Max(1, skill.baseHitCount);
        int hits = baseHits;
        if (skill.additionalHitsFromShockChance && skill.effect!=WeaponSkillEffect.ShockBarrage)
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
                && CanPlayerMultistrike && !HasClassKeystone(PassiveKeystone.WarriorConsolidation) && Random.value < Mathf.Clamp01(AdjustedChance(playerStats, StatTypes.ChanceToHitTwice)))
                ResolvePlayerLogicalHit(skill, target, statuses, showImpact: true, shockTriggered: i >= baseHits, extraStrike:true);
        }
    }

    private void ResolvePlayerLogicalHit(PlayerSkillDefinition skill, HealthComponent target,
        StatusController statuses, bool showImpact, bool shockTriggered = false,
        DamageContext? normalSnapshot = null, int consolidatedRepeats = 0, bool extraStrike=false)
    {
        if (!IsSameLivingEnemy(target)) return;
        DamageContext normal = normalSnapshot ?? (skill == null
            ? playerController.BuildAttackContext()
            : playerController.BuildAttackContext(skill.conversionElement, skill.nonMatchingConversion, skill.DamageScopes));
        normal=ApplyWeaponMechanics(normal);
        normal=ApplySubclassOutgoing(normal,target);
        if(extraStrike&&subclassState?.Has(SubclassIds.WarriorMultihit)==true)
            normal=TransformContext(normal,WarriorSubclassRules.EscalatingMultiplier(1,playerStats.GetStat(StatTypes.EscalatingMultistrike)),Element.Phys,0);
        var keystones = player != null ? player.GetComponent<PassiveKeystoneState>() : null;
        if (shockTriggered && keystones != null && !Mathf.Approximately(keystones.ShockTriggeredHitMultiplier, 1f))
            normal = TransformContext(normal, keystones.ShockTriggeredHitMultiplier, Element.Phys, 0f);
        bool poisonTransmutation = keystones != null && keystones.TransmutesHitsToPoison;
        PlayerSkillController skillController = player != null ? player.GetComponent<PlayerSkillController>() : null;
        int skillLevel = skill != null && skillController != null ? skillController.EffectiveSkillLevel(skill) : 1;
        float skillLevelFactor = PlayerSkillController.SkillDamageLevelFactor(skillLevel);
        float skillMultiplier=skill!=null&&skill.effect==WeaponSkillEffect.ArmourStrike
            ?WeaponMechanicProfile.ArmourStrikeMultiplier(playerStats.GetStat(StatTypes.FlatArmour)*(1f+playerStats.GetStat(StatTypes.ArmourPercent)))
            :skill?.hitDamageMultiplier??1f;
        DamageContext direct = poisonTransmutation
            ? new DamageContext(1) { Scopes = normal.Scopes, IsCrit = normal.IsCrit, CritMultiplier = normal.CritMultiplier,IsPrecision=normal.IsPrecision,PrecisionMultiplier=normal.PrecisionMultiplier }
            : skill == null ? normal : TransformContext(normal, skillMultiplier * skillLevelFactor, skill.conversionElement, 0f);
        direct.EventTags|=normal.EventTags;
        direct.EventTags|=skill==null?CombatEventTags.NormalAttack:CombatEventTags.WeaponSkill;
        if(resolvingRelicTriggers)direct.EventTags|=CombatEventTags.RelicTriggeredSkill;
        if(skill?.projectile==true)direct.EventTags|=CombatEventTags.Projectile;
        bool projectile=skill?.projectile==true||(skill==null&&playerController?.EquippedWeapon!=null&&WeaponTypeCatalog.TryGet(playerController.EquippedWeapon.WeaponTypeId,out var basicProfile)&&basicProfile.IsRanged);
        if(!projectile&&CanPlayerMultistrike&&HasClassKeystone(PassiveKeystone.WarriorConsolidation))
        {
            int extra=consolidatedRepeats;
            for(int strike=0;strike<=consolidatedRepeats;strike++)
                if(Random.value<Mathf.Clamp01(AdjustedChance(playerStats,StatTypes.ChanceToHitTwice)))extra++;
            for(int strike=0;strike<extra;strike++)
            {
                var additional=skill==null?playerController.BuildAttackContext():playerController.BuildAttackContext(skill.conversionElement,skill.nonMatchingConversion,skill.DamageScopes);
                additional=ApplySubclassOutgoing(ApplyWeaponMechanics(additional),target);
                if(skill!=null)additional=TransformContext(additional,skillMultiplier*skillLevelFactor,Element.Phys,0);
                float positional=subclassState?.Has(SubclassIds.WarriorMultihit)==true
                    ?WarriorSubclassRules.EscalatingMultiplier(strike+1,playerStats.GetStat(StatTypes.EscalatingMultistrike)):1f;
                foreach(var hit in additional.Hits)direct.AddDamage(hit.Element,hit.Amount*positional*ClassKeystoneCatalog.Get(PassiveKeystone.WarriorConsolidation).primary*(1+UniqueCatalog.Power(playerStats,UniquePower.MultistrikeDamage)));
                direct.IsCrit|=additional.IsCrit;
            }
        }
        if(HasClassKeystone(PassiveKeystone.PriestAura)&&direct.Hits.Count>2)
        {direct=TransformContext(direct,1,Element.Phys,0);while(direct.Hits.Count>2)direct.Hits.RemoveAt(Random.Range(0,direct.Hits.Count));}
        if(extraStrike)direct=TransformContext(direct,1+UniqueCatalog.Power(playerStats,UniquePower.MultistrikeDamage),Element.Phys,0);
        bool lightningShatter=HasClassKeystone(PassiveKeystone.MageShatter)&&statuses?.IsFrozen==true&&!statuses.IsFractured&&direct.Hits.Exists(h=>h.Element==Element.Light&&h.Amount>0);
        if(lightningShatter){direct=TransformContext(direct,1,Element.Phys,0);for(int i=0;i<direct.Hits.Count;i++){var h=direct.Hits[i];if(h.Element==Element.Light)h.Amount*=ClassKeystoneCatalog.Get(PassiveKeystone.MageShatter).secondary;direct.Hits[i]=h;}}
        specialEffects??=player!=null?player.GetComponent<BossSpecialEffectRuntime>()??player.AddComponent<BossSpecialEffectRuntime>():null;
        direct=specialEffects!=null?specialEffects.BeforeHit(direct,statuses,target):direct;

        DamageContext specialized = default;
        if (skill != null && skill.specializedAilment != StatusEffects.AilmentKind.None)
        {
            if (skill.id == PlayerSkillId.Envenom)
                specialized = TransformContext(normal, skill.ailmentBasisMultiplier * skillLevelFactor, skill.conversionElement, 0f);
            else if (skill.id == PlayerSkillId.Immolate)
                specialized = TransformContext(normal, skill.ailmentBasisMultiplier * skillLevelFactor, skill.conversionElement, 0f);
            else
                specialized = skill.effect is WeaponSkillEffect.EmpoweredBleed or WeaponSkillEffect.VirtualPoison
                    ?TransformContext(normal,skill.ailmentBasisMultiplier*skillLevelFactor,skill.conversionElement,0f)
                    :TransformContext(direct, skill.ailmentBasisMultiplier, skill.conversionElement, 0f);
        }

        float rawTotal = SumDamage(direct);
        float damageTaken = CombatCalculator.CalculateFinalDamage(direct, playerStats, enemyStats);
        string attackName = skill == null ? "Basic Attack" : skill.displayName;
        Debug.Log($"[{attackName}] Player hits enemy. Raw={rawTotal:F1}, Final={damageTaken:F1}, " +
                  $"Crit={normal.IsCrit}, CritMult={normal.CritMultiplier:F2}");

        if (enemyDamageReceiver == null) enemyDamageReceiver = GetOrAddDamageReceiver(currentEnemy);
        if (skill != null && skill.id == PlayerSkillId.HeavyStrike) PlayHeavyStrikeFeedback();
        if (damageTaken > 0f && enemyDamageReceiver != null)
        {
            float actualDamage=Mathf.Min(target.CurrentLife,damageTaken);
            var auraDefender=enemyStats;
            float auraMaximum=target.MaxLife;
            subclassState?.RecordMitigatedTypedDamage(direct,auraDefender,auraMaximum);
            enemyDamageReceiver.TakeDamage(damageTaken, direct,attacker:playerStats);
            playerRage?.GainFromDamageDealt(damageTaken,target.MaxLife,skill?.effect==WeaponSkillEffect.RageStrike?1.5f:1f);
            playerHealth.RestoreLife(playerStats.GetStat(StatTypes.LifeOnHit),HealingSource.LifeOnHit);
            if(statuses?.HasAilment(StatusEffects.AilmentKind.Bleed)==true)
                playerHealth.RestoreLife(playerStats.GetStat(StatTypes.LifeOnHitVsBleeding),HealingSource.LifeOnHit);
            GetPlayerMana()?.Restore(playerStats.GetStat(StatTypes.ManaOnHit));
            if(skill?.effect==WeaponSkillEffect.HealFromDamage)playerHealth.RestoreLife(actualDamage*skill.secondaryMultiplier,HealingSource.WeaponSkill);
            if(keystones!=null)playerHealth.RestoreLife(actualDamage*keystones.DamageRecoveryFraction,HealingSource.SubclassDamage);
            if(subclassState?.Has(SubclassIds.PriestLight)==true)playerHealth.RestoreLife(damageTaken*.10f,HealingSource.SubclassDamage);
            subclassState?.PlayerHit();
            specialEffects?.AfterHit(direct,rawTotal,statuses,target,enemyDamageReceiver,playerStats,enemyStats);
            if(subclassState?.Has(SubclassIds.BarbarianFire)==true&&Random.value<SubclassBalanceProfile.EruptionChance&&IsSameLivingEnemy(target))
            {
                var eruption=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.SubclassProc|CombatEventTags.Eruption};eruption.AddDamage(Element.Fire,rawTotal*SubclassBalanceProfile.EruptionMagnitude);float eruptionDamage=CombatCalculator.CalculateFinalDamage(eruption,playerStats,enemyStats);if(eruptionDamage>0){float lost=Mathf.Min(target.CurrentLife,eruptionDamage);enemyDamageReceiver.TakeDamage(eruptionDamage,eruption,attacker:playerStats);playerHealth.RestoreLife(lost*(keystones?.DamageRecoveryFraction??0),HealingSource.SubclassDamage);}if(IsSameLivingEnemy(target))ApplyConfiguredStatus(igniteEffect,StatTypes.IgniteChance,eruption,playerStats,statuses);
            }
        }

        if (IsSameLivingEnemy(target))
        {
            if(lightningShatter&&statuses.TryConsumeFreeze(out _))
            {var shatter=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.Shatter|CombatEventTags.NoSecondaryTriggers};shatter.AddDamage(Element.Cold,target.MaxLife*ClassKeystoneCatalog.Get(PassiveKeystone.MageShatter).tertiary);float burst=CombatCalculator.CalculateFinalDamage(shatter,playerStats,enemyStats);float lost=Mathf.Min(target.CurrentLife,burst);enemyDamageReceiver.TakeDamage(burst,shatter,attacker:playerStats);playerHealth.RestoreLife(lost*(keystones?.DamageRecoveryFraction??0),HealingSource.SubclassDamage);if(!IsSameLivingEnemy(target))return;}
            DamageContext poisonTransmutationBasis = poisonTransmutation
                ? TransformContext(PassiveKeystoneState.AsPoisonBasis(normal), skillLevelFactor, Element.Phys, 0f)
                : specialized;
            UniqueCombatRuntime.For(playerStats)?.Rupture(target,statuses);
            if(!IsSameLivingEnemy(target))return;
            ApplyOnHitEffects(direct, playerStats, statuses, skill,
                poisonTransmutationBasis, poisonTransmutation);
            if(skill?.effect==WeaponSkillEffect.FrostJudgment)
            {
                if(statuses.TryConsumeFreeze(out float frozenStrength))
                {
                    float fracture=BossSpecialEffectRuntime.PlayerHas("fracture")?1.5f:1f;
                    var shatter=TransformContext(normal,WeaponMechanicProfile.ShatterMultiplier(frozenStrength)*fracture,Element.Cold,1f);
                    shatter.EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.Shatter|CombatEventTags.NoSecondaryTriggers;
                    float burst=CombatCalculator.CalculateFinalDamage(shatter,playerStats,enemyStats);
                    if(burst>0&&enemyDamageReceiver!=null){float lost=Mathf.Min(target.CurrentLife,burst);enemyDamageReceiver.TakeDamage(burst,shatter,attacker:playerStats);playerHealth.RestoreLife(lost*(keystones?.DamageRecoveryFraction??0),HealingSource.SubclassDamage);}
                }
                else if(!HasClassKeystone(PassiveKeystone.PriestFracture)&&statuses.CurrentChillSlow>0&&Random.value<Mathf.Clamp01(statuses.CurrentChillSlow))statuses.ApplyFreeze(statuses.CurrentChillSlow);
            }
            if(subclassState?.Has(SubclassIds.WarriorBleed)==true
                &&WarriorSubclassRules.CanRupture(statuses.AilmentStackCount(StatusEffects.AilmentKind.Bleed),statuses.BleedStackCap(playerStats),subclassState.RupturedThisAttack)
                &&subclassState.TryMarkRupture())
            {float rupture=WarriorSubclassRules.RuptureTotal(statuses.ConsumeRemainingAilmentDamage(StatusEffects.AilmentKind.Bleed),playerStats.GetStat(StatTypes.RuptureDamage));if(rupture>0){var ruptureEvent=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.Rupture|CombatEventTags.NoSecondaryTriggers};ruptureEvent.AddDamage(Element.Phys,rupture);float lost=Mathf.Min(target.CurrentLife,rupture);enemyDamageReceiver?.TakeDamage(rupture,ruptureEvent);playerHealth.RestoreLife(lost*(keystones?.DamageRecoveryFraction??0),HealingSource.SubclassDamage);}}
            if(subclassState?.Has(SubclassIds.ThiefAssassin)==true&&target.CurrentLife>0&&target.CurrentLife<=target.MaxLife*.10f)
            {if(!target.IsBoss)target.LoseLife(target.CurrentLife);}
            // Direct and ailment culling share DamageReceiver's post-damage probability check.
        }
    }

    DamageContext ApplySubclassOutgoing(DamageContext source,HealthComponent target)
    {
        if(subclassState==null||source.Hits==null)return source;var result=source;
        if(subclassState.Has(SubclassIds.BarbarianFire))
        {float physical=0;foreach(var hit in source.Hits)if(hit.Element==Element.Phys)physical+=hit.Amount;if(physical>0){result=TransformContext(source,1,Element.Phys,0);result.AddDamage(Element.Fire,physical*SubclassBalanceProfile.AddedFireFromPhysical);}}
        if(subclassState.Has(SubclassIds.PriestLight)){var clean=new DamageContext(result.Hits.Count){IsCrit=result.IsCrit,CritMultiplier=result.CritMultiplier,Scopes=result.Scopes,IsPrecision=result.IsPrecision,PrecisionMultiplier=result.PrecisionMultiplier,WeaponMechanicsApplied=result.WeaponMechanicsApplied,EventTags=result.EventTags};foreach(var hit in result.Hits)if(hit.Element!=Element.Void)clean.AddDamage(hit.Element,hit.Amount);result=clean;}
        if(subclassState.Has(SubclassIds.PriestDark)){int corruption=FindFirstObjectByType<ZoneManager>()?.CorruptionPercentage??0;float more=1+corruption*.002f;var dark=new DamageContext(result.Hits.Count){IsCrit=result.IsCrit,CritMultiplier=result.CritMultiplier,Scopes=result.Scopes,IsPrecision=result.IsPrecision,PrecisionMultiplier=result.PrecisionMultiplier,WeaponMechanicsApplied=result.WeaponMechanicsApplied,EventTags=result.EventTags};foreach(var hit in result.Hits)dark.AddDamage(hit.Element,hit.Element==Element.Void?hit.Amount*more:hit.Amount);result=dark;}
        bool full=target!=null&&target.CurrentLife>=target.MaxLife-.001f;bool injured=playerHealth!=null&&playerHealth.CurrentLife<=playerHealth.MaxLife*.5f;bool bossLow=target!=null&&target.IsBoss&&target.CurrentLife<=target.MaxLife*.10f;float multiplier=subclassState.BeforePlayerHitMultiplier(full,injured,bossLow);return Mathf.Approximately(multiplier,1)?result:TransformContext(result,multiplier,Element.Phys,0);
    }

    public void ApplyTriggerlessVoidDamage(float amount)
    {
        if(amount<=0||enemyHealth==null||enemyHealth.CurrentLife<=0||enemyDamageReceiver==null)return;var context=new DamageContext(1){EventTags=CombatEventTags.TriggerlessDamage|CombatEventTags.HealConvertedDamage|CombatEventTags.NoSecondaryTriggers};context.AddDamage(Element.Void,amount);float final=CombatCalculator.CalculateFinalDamage(context,playerStats,enemyStats);if(final>0)enemyDamageReceiver.TakeDamage(final,context,attacker:playerStats);
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
            ,IsPrecision=source.IsPrecision,PrecisionMultiplier=source.PrecisionMultiplier,WeaponMechanicsApplied=source.WeaponMechanicsApplied,EventTags=source.EventTags
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

    bool CanPrecision(PlayerSkillDefinition skill)
    {
        if(skill?.supportsPrecision==true)return true;return playerController?.EquippedWeapon!=null&&WeaponTypeCatalog.TryGet(playerController.EquippedWeapon.WeaponTypeId,out var profile)&&profile.SupportsPrecision;
    }
    DamageContext ApplyPrecision(DamageContext source,bool capable)
    {
        if(HasClassKeystone(PassiveKeystone.RangerSplit)){var split=ClassKeystoneCatalog.Get(PassiveKeystone.RangerSplit);source=TransformContext(source,split.secondary*split.tertiary,Element.Phys,0);}
        if(!capable)return source;float chance=HasClassKeystone(PassiveKeystone.RangerPrecision)?1:WeaponMechanicProfile.PrecisionChance(playerStats.GetStat(StatTypes.ProjectilePrecisionChance));if(Random.value>=chance)return source;
        float multiplier=WeaponMechanicProfile.PrecisionMultiplier(playerStats.GetStat(StatTypes.ProjectilePrecisionMultiplier))*(HasClassKeystone(PassiveKeystone.RangerPrecision)?ClassKeystoneCatalog.Get(PassiveKeystone.RangerPrecision).secondary:1);var result=TransformContext(source,multiplier,Element.Phys,0);result.IsPrecision=true;result.PrecisionMultiplier=multiplier;return result;
    }
    DamageContext ApplyWeaponMechanics(DamageContext source)
    {
        if(source.WeaponMechanicsApplied)return source;float multiplier=currentAttackEventMultiplier*(playerRage?.SustainedDamageMultiplier??1f)*UniqueCatalog.HitMultiplier(playerStats);var result=Mathf.Approximately(multiplier,1)?source:TransformContext(source,multiplier,Element.Phys,0);if(playerStatusCont?.CombinedShockEffect>0){result=TransformContext(result,1,Element.Phys,0);for(int i=0;i<result.Hits.Count;i++){var hit=result.Hits[i];if(hit.Element==Element.Light)hit.Amount*=1+UniqueCatalog.Power(playerStats,UniquePower.ShockedLightningMore);result.Hits[i]=hit;}}result.WeaponMechanicsApplied=true;return result;
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
            playerStatusCont?.ReleaseSource(enemyStats);
            if(HasClassKeystone(PassiveKeystone.ThiefStealth))stealthReady=true;
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

    private bool ApplyOnHitEffects(DamageContext ctx, StatsComponent attackerStats,
        StatusController targetStatusCont, PlayerSkillDefinition skill = null, DamageContext specialized = default,
        bool poisonTransmutation = false)
    {
        if (attackerStats == null || targetStatusCont == null) return false;

        DamageContext poisonBasis = poisonTransmutation ? specialized
            : skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Poison
            ? specialized : ctx;
        if(skill?.effect==WeaponSkillEffect.VirtualPoison || skill?.id==PlayerSkillId.Envenom)
            poisonBasis.EventTags|=CombatEventTags.FullAilmentBasis;
        DamageContext bleedBasis = skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Bleed
            ? specialized : ctx;
        DamageContext igniteBasis = skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Ignite
            ? specialized : ctx;
        int specializedGuarantee=skill!=null?Mathf.Max(0,skill.guaranteedAilmentApplications):0;

        int poisonApplications=ApplyConfiguredStatus(poisonEffect, StatTypes.PoisonChance, poisonBasis, attackerStats, targetStatusCont,
            poisonTransmutation ? 1 : skill!=null&&skill.specializedAilment==StatusEffects.AilmentKind.Poison
                ?specializedGuarantee:0, poisonTransmutation ? 1f : -1f, poisonTransmutation);
        if(poisonApplications>0&&attackerStats.GetComponent<PlayerController>()!=null&&BossSpecialEffectRuntime.PlayerHas("toxic-echo")&&Random.value<.15f)
        {poisonBasis.EventTags|=CombatEventTags.TriggeredDamage|CombatEventTags.ToxicEcho|CombatEventTags.NoSecondaryTriggers;targetStatusCont.ApplyAilmentFromHit(poisonEffect,poisonBasis,attackerStats,1,.05f);}
        int bleedApplications=ApplyConfiguredStatus(bleedEffect, StatTypes.BleedChance, bleedBasis, attackerStats, targetStatusCont,
            skill!=null&&skill.specializedAilment==StatusEffects.AilmentKind.Bleed?specializedGuarantee:0,
            applicationMultiplier:skill?.effect==WeaponSkillEffect.RendingStrike?2:1);
        int igniteApplications=ApplyConfiguredStatus(igniteEffect, StatTypes.IgniteChance, igniteBasis, attackerStats, targetStatusCont,
            skill!=null&&skill.specializedAilment==StatusEffects.AilmentKind.Ignite?specializedGuarantee:0,
            skill != null && skill.specializedAilment == StatusEffects.AilmentKind.Ignite
                ? skill.absoluteAilmentCoefficient : -1f);
        if(attackerStats.GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.PriestSacrifice)==true)
        {
            int distinct=(poisonApplications>0?1:0)+(bleedApplications>0?1:0)+(igniteApplications>0?1:0);
            var health=targetStatusCont.GetComponent<HealthComponent>();
            if(distinct>0&&health!=null&&health.CurrentLife>0)
            {
                float rawTrueDamage=health.MaxLife*distinct*PassiveKeystoneState.Value(PassiveKeystone.PriestSacrifice);
                float lost=Mathf.Min(health.CurrentLife,rawTrueDamage);
                var trueHit=new DamageContext(1){EventTags=CombatEventTags.TriggeredDamage|CombatEventTags.NoSecondaryTriggers|CombatEventTags.SubclassProc};
                trueHit.AddDamage(Element.True,rawTrueDamage);
                (health.GetComponent<DamageReceiver>()??health.gameObject.AddComponent<DamageReceiver>()).TakeDamage(rawTrueDamage,trueHit,attacker:attackerStats);
                if(skill?.effect==WeaponSkillEffect.HealFromDamage)attackerStats.GetComponent<HealthComponent>()?.RestoreLife(lost*skill.secondaryMultiplier,HealingSource.WeaponSkill);
                var keys=attackerStats.GetComponent<PassiveKeystoneState>();attackerStats.GetComponent<HealthComponent>()?.RestoreLife(lost*keys.DamageRecoveryFraction,HealingSource.SubclassDamage);
            }
            poisonApplications=bleedApplications=igniteApplications=0;
        }
        ApplyChillFromHit(ctx, attackerStats, targetStatusCont,
            skill != null ? skill.guaranteedAdditionalChill : 0);
        ApplyShockFromHit(ctx, attackerStats, targetStatusCont);
        attackerStats.GetComponent<SubclassCombatState>()?.RecordDamagingAilments(bleedApplications>0,igniteApplications>0,poisonApplications>0);
        return bleedApplications>0;
    }

    private void ApplyShockFromHit(DamageContext context, StatsComponent attacker, StatusController target)
    {
        if (shockEffect == null || attacker == null || target == null) return;
        StatsComponent defender = target.GetComponent<StatsComponent>();
        var subclassState=attacker.GetComponent<SubclassCombatState>();
        DamageContext eligible=AilmentEligibilityResolver.Filter(shockEffect,context,attacker);
        float lightningDealt=CombatCalculator.CalculateFinalDamage(eligible,attacker,defender);
        if (lightningDealt <= 0f) return;

        float chance = AdjustedChance(attacker, StatTypes.ShockChance);
        int stacks = RollOverflowApplications(chance);
        if (stacks <= 0) return;

        float duration=ShockRules.Duration(attacker.GetRawStat(StatTypes.ShockDuration)/100f);
        float auraShock=subclassState!=null?subclassState.AuraSecondary(3,.20f):0f;
        float increased=attacker.GetStat(StatTypes.ShockEffect)+auraShock
            +(attacker.GetComponent<StatusController>()?.CombinedShockEffect>0?UniqueCatalog.Power(attacker,UniquePower.ShockedShockEffect):0);
        float maximum=ShockRules.BaseMaximumEffect+(attacker.GetComponent<PlayerController>()!=null?RelicInventory.Instance?.MaximumShockEffectIncrease??0:0);
        float strength=ShockRules.Effect(increased,maximum,defender?.GetStat(StatTypes.ReducedShockEffect)??0);
        for(int i=0;i<stacks;i++)target.AddShockInstance(strength,duration,subclassState!=null?subclassState.MaximumShockInstances:1);
    }

    private void ApplyChillFromHit(DamageContext context, StatsComponent attacker,
        StatusController target, int guaranteedApplications)
    {
        if (chillEffect == null || attacker == null || target == null) return;
        StatsComponent defender = target.GetComponent<StatsComponent>();
        HealthComponent targetHealth = target.GetComponent<HealthComponent>();
        if (targetHealth == null || targetHealth.MaxLife <= 0f) return;
        var subclass=attacker.GetComponent<SubclassCombatState>();
        DamageContext eligible=AilmentEligibilityResolver.Filter(chillEffect,context,attacker);
        float coldDealt=CombatCalculator.CalculateFinalDamage(eligible,attacker,defender);
        if (coldDealt <= 0f) return;

        float chance = AdjustedChance(attacker, StatTypes.ChillChance);
        if (guaranteedApplications <= 0 && RollOverflowApplications(chance) <= 0) return;

        float effectFactor = 1f - Mathf.Clamp01(defender != null
            ? defender.GetStat(StatTypes.ReducedChillEffect) : 0f);
        PassiveKeystoneState keystones = attacker.GetComponent<PassiveKeystoneState>();
        float effectiveness = keystones != null ? keystones.ChillEffectMultiplier : 1f;
        float cap = .3f + (keystones != null ? keystones.DeepFreezeMaximumEffectIncrease : 0f)
            + (attacker.GetComponent<PlayerController>()!=null?RelicInventory.Instance?.MaximumChillSlowIncrease??0f:0f);
        float auraChill=subclass!=null?subclass.AuraSecondary(2,.20f):0f;
        float slow = CalculateChillSlow(coldDealt, targetHealth.MaxLife,
            attacker.GetStat(StatTypes.ChillEffect)+auraChill, effectiveness, cap) * effectFactor;
        int duration = Mathf.Max(1, Mathf.RoundToInt(4f
            + attacker.GetRawStat(StatTypes.ChillDuration)));
        target.ApplyChill(chillEffect, Mathf.Min(cap, slow), duration,cap);
        float uncapped=ClassKeystoneMechanics.UncappedChill(coldDealt,targetHealth.MaxLife,attacker.GetStat(StatTypes.ChillEffect)+auraChill,effectiveness)*effectFactor;
        if(keystones?.Has(PassiveKeystone.PriestFracture)==true)target.ApplyKeystoneFreeze(uncapped,PassiveKeystone.PriestFracture);
        else if(keystones?.Has(PassiveKeystone.MageShatter)==true)target.ApplyKeystoneFreeze(uncapped,PassiveKeystone.MageShatter);
    }

    public static float CalculateChillSlow(float coldDamageDealt, float targetMaximumLife,
        float chillEffect, float effectivenessMultiplier = 1f, float maximumSlow = .3f)
    {
        if (coldDamageDealt <= 0f || targetMaximumLife <= 0f) return 0f;
        float raw = Mathf.Clamp(.05f + coldDamageDealt / targetMaximumLife, .05f, .3f);
        return Mathf.Clamp(raw * Mathf.Max(0f, 1f + chillEffect)
            * Mathf.Max(0f, effectivenessMultiplier), 0f, Mathf.Max(0f, maximumSlow));
    }

    private static int ApplyConfiguredStatus(StatusEffects effect, StatTypes chance, DamageContext ctx,
        StatsComponent attacker, StatusController target, int guaranteedApplications = 0,
        float magnitudeOverride = -1f, bool guaranteedOnly = false,int applicationMultiplier=1)
    {
        if (effect == null || ctx.Hits == null || ctx.Hits.Count == 0
            || AilmentCalculator.GetSourceHitDamage(effect, ctx,attacker) <= 0f) return 0;
        float adjustedChance = AdjustedChance(attacker, chance);
        StatsComponent defender = target != null ? target.GetComponent<StatsComponent>() : null;
        adjustedChance = AdjustForApplicationResistance(effect, adjustedChance, attacker, defender);
        int applications = guaranteedApplications
            + (guaranteedOnly ? 0 : RollOverflowApplications(adjustedChance));
        applications*=Mathf.Max(1,applicationMultiplier);
        if (applications > 0 && attacker?.GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.PriestSacrifice)!=true)
            target.ApplyAilmentFromHit(effect, ctx, attacker, applications, magnitudeOverride);
        return applications;
    }

    public static int RollOverflowApplications(float chance)
        => BlackCube.CombatSimulation.CombatDeterministicRules.RollOverflowApplications(chance,()=>Random.value);

    public static float AdjustedChance(StatsComponent stats, StatTypes chance)
    {
        if (stats == null) return 0f;
        var keystones = stats.GetComponent<PassiveKeystoneState>();
        return stats.GetStat(chance) * (keystones != null ? keystones.ChanceMultiplier(chance) : 1f);
    }

    public static float AdjustForApplicationResistance(StatusEffects effect, float chance,
        StatsComponent attacker, StatsComponent defender)
    {
        // Legacy call site retained for compatibility. Ordinary resistances
        // mitigate damage, while reduced Shock/Chill effect scales magnitude.
        return Mathf.Max(0f, chance);
    }
}
