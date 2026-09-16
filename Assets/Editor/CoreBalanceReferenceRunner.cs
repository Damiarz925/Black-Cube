using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BlackCube
{
    [Serializable] public sealed class CoreBalanceReferenceRow
    {
        public int seed,sample,playerLevel,combatLevel,gearTrack,relicTrack;
        public PlayerSkillId archetype;
        public bool autoBaseline;
        public string enemyRole;
        public float playerLife,playerMana,manaRegeneration,playerSpeed,playerNormalHit,playerBasicDps;
        public float fireRes,coldRes,lightRes,voidRes,armour,implicitScore,explicitScore;
        public float enemyLife,enemySpeed,enemyNormalHit,enemyDps,normalTtk,normalTtd;
        public float enemyIntrinsicDamageFactor,enemyWeaponBaseDamage;
        public int enemyGearPieces,enemyRarity;
        public float skillCost,skillDirectHit,skillAilmentDps,skillBurst10,skillSustain30,engagedTtk,ailmentShare;
        public float basicAilmentDps,skillDirectDps;
        public float eventEngagedWinRate,eventIdleWinRate,eventVictorySeconds,eventIdleVictorySeconds;
        public float eventDeathSeconds,eventAilmentShare,eventManaRemaining,eventSkillCasts;
        public int selectedGear,implicitCount,explicitCount,passiveCount,craftedItems;
        public int resistanceImplicitCount,resistanceExplicitCount,allResCount;
        public float resistanceImplicitPoints;
        public int prefixCount,suffixCount,topTierExplicitCount;
    }

    [Serializable] public sealed class CoreBalanceReferenceReport
    {
        public string methodology="Production actor stats, legal rolled equipment/implicits/passives and enemy generation. "
            +"Expected-hit TTK/TTD and 10/30-second throughput are diagnostic; three seeded event-timed duels per row also model automatic attacks, engaged skills, mana recovery, DOT turns, Shock and Chill. "
            +"Engaged casting uses a declared 1.5-second human cadence, not a runtime cooldown; projectile travel and ranged skill-hit weapon variance remain estimates.";
        public int seed,samples;
        public CoreBalanceReferenceRow[] rows;
        public double elapsedSeconds;
    }

    // Step 13 reference pass. All player items are production rolls and the same
    // stat operations used by EquipmentManager; no synthetic player level factor.
    public static class CoreBalanceReferenceRunner
    {
        const string DatabasePath="Assets/Prefabs/Scriptable Objects/ModDatabase.asset";
        static readonly int[] Checkpoints={1,10,25,50,75,100,110,125,150,200,300};
        static readonly PlayerSkillId[] Archetypes={PlayerSkillId.HeavyStrike,PlayerSkillId.HeavyStrike,PlayerSkillId.IceStrike,
            PlayerSkillId.LightningStrike,PlayerSkillId.Fireball,PlayerSkillId.Envenom,
            PlayerSkillId.Shiv,PlayerSkillId.Immolate};
        static readonly LootManager.GearType[] Slots={LootManager.GearType.Weapons,LootManager.GearType.Helmets,
            LootManager.GearType.BodyArmours,LootManager.GearType.Gloves,LootManager.GearType.Boots,
            LootManager.GearType.Amulets,LootManager.GearType.Rings,LootManager.GearType.Belts};
        const int DuelTrials=3;

        [MenuItem("Black Cube/Balance/Run Step 13 Real Reference Quick")]
        public static void RunQuick()=>RunFromCommandLine(5);
        public static void RunBaseline()=>RunFromCommandLine(25);

        public static void RunFromCommandLine(int defaultSamples=25)
        {
            int seed=ArgumentInt("-coreBalanceSeed",13001);
            int samples=Mathf.Clamp(ArgumentInt("-coreBalanceSamples",defaultSamples),1,1000);
            string output=ArgumentString("-coreBalanceOutput","Logs/Balance/Step13-real-reference.json");
            var report=Run(seed,samples);
            string path=Path.IsPathRooted(output)?output:Path.GetFullPath(Path.Combine(Application.dataPath,"..",output));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path,JsonUtility.ToJson(report,true));
            UnityEngine.Debug.Log($"Step 13 real reference: {report.rows.Length} rows, seed {seed}, {report.elapsedSeconds:F1}s, {path}");
        }

        public static CoreBalanceReferenceReport Run(int seed,int samples)
        {
            if(samples<1)throw new ArgumentOutOfRangeException(nameof(samples));
            var saved=UnityEngine.Random.state;
            var clock=Stopwatch.StartNew();
            var created=new List<UnityEngine.Object>();
            try
            {
                var database=AssetDatabase.LoadAssetAtPath<ModDatabase>(DatabasePath);
                if(database==null)throw new InvalidOperationException("Missing production ModDatabase.");
                database.Initialize();
                var rollerHost=new GameObject("Core balance isolated roller");rollerHost.SetActive(false);created.Add(rollerHost);
                var roller=rollerHost.AddComponent<ModManager>();roller.ConfigureForIsolatedRolling(database);
                var lootHost=new GameObject("Core balance loot rarity table");lootHost.SetActive(false);created.Add(lootHost);
                var loot=lootHost.AddComponent<LootManager>();
                var battle=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaperBattle/PaperBattle.prefab")
                    ?.GetComponentInChildren<BattleManager>(true);
                if(battle==null)throw new InvalidOperationException("Missing production PaperBattle prefab.");
                var serialized=new SerializedObject(battle);
                var normal=serialized.FindProperty("normalEnemyPrefab")?.objectReferenceValue as GameObject;
                var boss=serialized.FindProperty("bossEnemyPrefab")?.objectReferenceValue as GameObject;
                if(normal==null||boss==null)throw new InvalidOperationException("Missing assigned production enemy prefabs.");
                StatusEffects[] ailments=CreateAilments(created);
                var rows=new List<CoreBalanceReferenceRow>();
                foreach(int combatLevel in Checkpoints)
                {
                    int playerLevel=Mathf.Min(100,combatLevel);
                    for(int track=0;track<3;track++)
                    for(int archetypeIndex=0;archetypeIndex<Archetypes.Length;archetypeIndex++)
                    for(int sample=0;sample<samples;sample++)
                    {
                        PlayerSkillId archetype=Archetypes[archetypeIndex];
                        bool autoBaseline=archetypeIndex==0;
                        int rowSeed=unchecked(seed*397+combatLevel*100003+track*1009+archetypeIndex*79+sample);
                        UnityEngine.Random.InitState(rowSeed);
                        var player=CreatePlayer(roller,loot,database,playerLevel,combatLevel,track,archetype,autoBaseline,created,
                            out int selected,out int implicits,out int explicits,out int crafted,
                            out float implicitScore,out float explicitScore,out int passives);
                        RelicInventory previousRelics=RelicInventory.Instance;
                        var relicHost=new GameObject("Core balance isolated relics",typeof(RelicInventory));
                        var relics=relicHost.GetComponent<RelicInventory>();
                        SetRelicInstance(relics);
                        GameObject normalActor=null,bossActor=null;
                        try
                        {
                            normalActor=SpawnEnemy(normal,roller,combatLevel,false);
                            bossActor=SpawnEnemy(boss,roller,combatLevel,true);
                            for(int relicTrack=0;relicTrack<3;relicTrack++)
                            {
                                if(relicTrack==1)AddAverageRelic(relics,0);
                                else if(relicTrack==2)
                                    for(int slot=1;slot<RelicInventory.ActiveSlotCount;slot++)AddAverageRelic(relics,slot);
                                player.GetComponent<PlayerController>().NotifyRelicChanged();
                                int activeRelics=relicTrack==2?4:relicTrack;
                                rows.Add(Capture(player,normalActor,ailments,archetype,autoBaseline,playerLevel,combatLevel,track,activeRelics,seed,sample,
                                    selected,implicits,explicits,crafted,implicitScore,explicitScore,passives,"normal"));
                                rows.Add(Capture(player,bossActor,ailments,archetype,autoBaseline,playerLevel,combatLevel,track,activeRelics,seed,sample,
                                    selected,implicits,explicits,crafted,implicitScore,explicitScore,passives,"boss"));
                            }
                        }
                        finally
                        {
                            SetRelicInstance(previousRelics);
                            if(bossActor!=null)UnityEngine.Object.DestroyImmediate(bossActor);
                            if(normalActor!=null)UnityEngine.Object.DestroyImmediate(normalActor);
                            UnityEngine.Object.DestroyImmediate(relicHost);
                            UnityEngine.Object.DestroyImmediate(player);
                        }
                    }
                }
                clock.Stop();
                return new CoreBalanceReferenceReport{seed=seed,samples=samples,rows=rows.ToArray(),
                    elapsedSeconds=clock.Elapsed.TotalSeconds};
            }
            finally
            {
                for(int i=created.Count-1;i>=0;i--)if(created[i]!=null)UnityEngine.Object.DestroyImmediate(created[i]);
                UnityEngine.Random.state=saved;
            }
        }

        static GameObject CreatePlayer(ModManager roller,LootManager loot,ModDatabase database,
            int playerLevel,int combatLevel,int track,PlayerSkillId archetype,bool autoBaseline,List<UnityEngine.Object> created,
            out int selected,out int implicits,out int explicits,out int crafted,
            out float implicitScore,out float explicitScore,out int passives)
        {
            var player=new GameObject("Core balance real player",typeof(StatsComponent),typeof(HealthComponent),
                typeof(ManaComponent),typeof(PlayerStatSetup),typeof(PlayerController),
                typeof(PlayerSkillController),typeof(PassiveKeystoneState));
            InvokeAwake(player.GetComponent<PlayerStatSetup>());
            InvokeAwake(player.GetComponent<HealthComponent>());
            InvokeAwake(player.GetComponent<PlayerController>());
            var stats=player.GetComponent<StatsComponent>();
            var controller=player.GetComponent<PlayerController>();
            var progressionHost=new GameObject("Core balance legal progression",typeof(PlayerProgression));
            progressionHost.transform.SetParent(player.transform,false);
            var progression=progressionHost.GetComponent<PlayerProgression>();
            SetField(progression,"boundStats",stats);
            SetField(progression,"boundKeystones",player.GetComponent<PassiveKeystoneState>());
            passives=0;
            selected=implicits=explicits=crafted=0;implicitScore=explicitScore=0f;
            stats.BeginUpdate();
            try
            {
                if(playerLevel==1)
                {
                    Gear starter=StarterWeapon(player,roller,database);
                    ApplyGear(starter,stats,controller,database,ref selected,ref implicits,ref explicits,
                        ref implicitScore,ref explicitScore);
                }
                else
                {
                    foreach(var slot in Slots)
                    {
                        Gear item=SelectLegalGear(player,roller,loot,database,slot,combatLevel,track,archetype);
                        if(item==null)throw new InvalidOperationException($"No legal {slot} gear at level {combatLevel}.");
                        if(track>0 && item.ItemRarity==LootManager.GearRarity.Normal
                            && UnityEngine.Random.value<(track==1?.35f:.65f)
                            && EquipmentCrafting.TryApply(CraftingCurrencyType.NormalToMagic,item,roller))crafted++;
                        if(track==2 && item.ItemRarity==LootManager.GearRarity.Magic
                            && UnityEngine.Random.value<.35f
                            && EquipmentCrafting.TryApply(CraftingCurrencyType.MagicToRare,item,roller))crafted++;
                        ApplyGear(item,stats,controller,database,ref selected,ref implicits,ref explicits,
                            ref implicitScore,ref explicitScore);
                    }
                }
            }
            finally{stats.EndUpdate();}
            int[] ranks=BuildLegalRanks(playerLevel,archetype,controller.EquippedWeaponElement);
            foreach(int rank in ranks)passives+=rank;
            if(!progression.RestoreProgression(playerLevel,0d,playerLevel-1-passives,ranks))
                throw new InvalidOperationException("Reference passives rejected by progression authority.");
            player.GetComponent<HealthComponent>().RestoreFullLife();
            InvokeAwake(player.GetComponent<ManaComponent>());
            var skills=player.GetComponent<PlayerSkillController>();InvokeAwake(skills);
            if(!skills.RestoreSelection(!autoBaseline,archetype))throw new InvalidOperationException($"Missing production skill {archetype}.");
            return player;
        }

        static Gear StarterWeapon(GameObject player,ModManager roller,ModDatabase database)
        {
            var go=new GameObject("Authored starter weapon",typeof(Gear));go.transform.SetParent(player.transform,false);
            var gear=go.GetComponent<Gear>();gear.Initialize(LootManager.GearType.Weapons,
                LootManager.GearRarity.Normal,1,Element.Phys);
            gear.BaseDamage=80f;gear.BaseDamageMin=64f;gear.BaseDamageMax=96f;
            gear.BaseAttackSpeed=1.2f;gear.BaseCritChance=.05f;
            RolledMod implicitMod=null;
            for(int attempt=0;attempt<64&&implicitMod==null;attempt++)
            {
                var mods=roller.RollEquipmentModsForItem(LootManager.GearType.Weapons,
                    LootManager.GearRarity.Normal,1,Element.Phys);
                implicitMod=mods?.Find(mod=>mod.lockedOriginal&&!Gear.IsWeaponBaseStat(mod.statType));
            }
            if(implicitMod==null)throw new InvalidOperationException("Production starter implicit construction failed.");
            gear.ApplyMods(new List<RolledMod>{implicitMod});
            ValidateGear(gear,database);return gear;
        }

        static Gear SelectLegalGear(GameObject player,ModManager roller,LootManager loot,ModDatabase database,
            LootManager.GearType slot,int level,int track,PlayerSkillId archetype)
        {
            int attempts=CandidateCount(slot,track);
                Gear best=null;float bestScore=float.NegativeInfinity;
            for(int candidate=0;candidate<attempts;candidate++)
            {
                var rarity=loot.RollItemRarity(level);
                Element element=loot.RollItemElement(slot==LootManager.GearType.Weapons);
                int itemLevel=Mathf.Max(1,level-UnityEngine.Random.Range(0,Mathf.Max(2,level/10)));
                List<RolledMod> mods=null;
                for(int retry=0;retry<64&&mods==null;retry++)
                    mods=roller.RollEquipmentModsForItem(slot,rarity,itemLevel,element);
                if(mods==null)throw new InvalidOperationException($"Production could not roll {rarity} {slot} at {itemLevel}.");
                var go=new GameObject($"Legal {slot} candidate",typeof(Gear));go.transform.SetParent(player.transform,false);
                var gear=go.GetComponent<Gear>();gear.Initialize(slot,rarity,itemLevel,element);gear.ApplyMods(mods);
                ValidateGear(gear,database);
                float score=Score(gear,archetype,player.GetComponent<StatsComponent>(),track);
                if(score>bestScore)
                {
                    if(best!=null)UnityEngine.Object.DestroyImmediate(best.gameObject);
                    best=gear;bestScore=score;
                }
                else UnityEngine.Object.DestroyImmediate(go);
            }
            return best;
        }

        static int CandidateCount(LootManager.GearType slot,int track)
        {
            int baseCount=slot switch
            {
                LootManager.GearType.Weapons or LootManager.GearType.Helmets
                    or LootManager.GearType.BodyArmours=>3,
                LootManager.GearType.Gloves or LootManager.GearType.Boots=>2,
                _=>1
            };
            return baseCount*(track==0?1:track==1?3:6);
        }

        static float Score(Gear gear,PlayerSkillId archetype,StatsComponent currentStats,int track)
        {
            float score=0f;
            if(gear.ItemType==LootManager.GearType.Weapons)
            {
                score+=gear.GetEffectiveBaseDamage()*.08f+gear.BaseAttackSpeed*4f;
                Element wanted=archetype switch
                {
                    PlayerSkillId.IceStrike=>Element.Cold,PlayerSkillId.LightningStrike=>Element.Light,
                    PlayerSkillId.Fireball or PlayerSkillId.Immolate=>Element.Fire,
                    PlayerSkillId.Envenom=>Element.Void,_=>Element.Phys
                };
                if(gear.BaseElement==wanted)score+=5f;
            }
            foreach(var mod in gear.rolledMods)
            {
                if(Gear.IsWeaponBaseStat(mod.statType))continue;
                float value=mod.value;
                float resistanceNeed=1f;
                if(track>0&&currentStats!=null)
                {
                    StatTypes resistance=mod.statType;
                    if(resistance is StatTypes.FireRes or StatTypes.ColdRes or StatTypes.LightRes or StatTypes.VoidRes)
                        resistanceNeed=currentStats.GetStat(resistance)
                            +currentStats.GetStat(StatTypes.AllRes)<.75f?1f:.1f;
                    else if(resistance==StatTypes.AllRes)
                    {
                        resistanceNeed=0f;
                        foreach(var core in new[]{StatTypes.FireRes,StatTypes.ColdRes,
                            StatTypes.LightRes,StatTypes.VoidRes})
                            if(currentStats.GetStat(core)+currentStats.GetStat(StatTypes.AllRes)<.75f)
                                resistanceNeed+=.25f;
                    }
                }
                if(track==2)resistanceNeed*=1.4f;
                score+=mod.statType switch
                {
                    StatTypes.FireRes or StatTypes.ColdRes or StatTypes.LightRes or StatTypes.VoidRes=>value*.18f*resistanceNeed,
                    StatTypes.AllRes=>value*.58f*resistanceNeed,
                    StatTypes.Life=>value*.05f,
                    StatTypes.LifePercent=>value*.36f,
                    StatTypes.Mana or StatTypes.ManaPercent=>value*.05f,
                    StatTypes.GenericDmg or StatTypes.PhysDmg or StatTypes.FireDmg or StatTypes.ColdDmg
                        or StatTypes.LightDmg or StatTypes.VoidDmg=>value*.14f,
                    StatTypes.PoisonChance when archetype==PlayerSkillId.Envenom=>value*.35f,
                    StatTypes.BleedChance when archetype==PlayerSkillId.Shiv=>value*.35f,
                    StatTypes.IgniteChance when archetype==PlayerSkillId.Immolate=>value*.35f,
                    StatTypes.PoisonDmg or StatTypes.PoisonMult or StatTypes.GenericDotMult
                        when archetype==PlayerSkillId.Envenom=>value*.24f,
                    StatTypes.BleedDmg or StatTypes.BleedMult or StatTypes.GenericDotMult
                        when archetype==PlayerSkillId.Shiv=>value*.24f,
                    StatTypes.IgniteDmg or StatTypes.IgniteMult or StatTypes.GenericDotMult
                        when archetype==PlayerSkillId.Immolate=>value*.24f,
                    StatTypes.ShockChance when archetype==PlayerSkillId.LightningStrike=>value*.32f,
                    _=>value*.025f
                };
            }
            return score;
        }

        static void ValidateGear(Gear gear,ModDatabase database)
        {
            var errors=new List<string>();
            ItemizationValidator.ValidateRolledMods(gear.ItemType,gear.ItemRarity,gear.ItemLevel,
                gear.rolledMods,database,errors,true);
            if(errors.Count>0)throw new InvalidOperationException($"Illegal reference gear: {string.Join("; ",errors)}");
        }

        static void ApplyGear(Gear gear,StatsComponent stats,PlayerController controller,ModDatabase database,
            ref int selected,ref int implicits,ref int explicits,ref float implicitScore,ref float explicitScore)
        {
            ValidateGear(gear,database);
            selected++;
            foreach(var mod in gear.rolledMods)
            {
                if(Gear.IsWeaponBaseStat(mod.statType))continue;
                if(mod.lockedOriginal){implicits++;implicitScore+=mod.value;}
                else{explicits++;explicitScore+=mod.value;}
            }
            foreach(var mod in gear.globalRolledMods)
                stats.AddModifier(new StatModifier(mod.statType,
                    StatMappings.GetRolledModifierOperation(mod.statType),mod.value,gear));
            if(gear.ItemType==LootManager.GearType.Weapons)controller.EquipWeapon(gear);
        }

        static void AddAverageRelic(RelicInventory relics,int slot)
        {
            RelicData relic=relics.BeginNewCycle();
            if(UnityEngine.Random.value<.7f)
                AncientRelicCrafting.TryApply(CraftingCurrencyType.AncientNormalToMagic,relic,relics);
            if(slot>0&&relic.rarity==LootManager.GearRarity.Magic&&UnityEngine.Random.value<.35f)
                AncientRelicCrafting.TryApply(CraftingCurrencyType.AncientMagicToRare,relic,relics);
            if(!relics.Equip(relic,slot))throw new InvalidOperationException("Average relic could not be equipped.");
        }

        static void SetRelicInstance(RelicInventory value)=>typeof(RelicInventory)
            .GetProperty("Instance",BindingFlags.Public|BindingFlags.Static)
            .GetSetMethod(true).Invoke(null,new object[]{value});

        static GameObject SpawnEnemy(GameObject prefab,ModManager roller,int level,bool boss)
        {
            var enemy=UnityEngine.Object.Instantiate(prefab);
            enemy.hideFlags=HideFlags.HideAndDontSave;
            enemy.GetComponent<HealthComponent>().SetEnemyRole(boss);
            enemy.GetComponent<EnemyAI>().GenerateIsolatedBuild(level,roller,EnemyAI.EnemyRarity.Normal);
            return enemy;
        }

        static StatusEffects[] CreateAilments(List<UnityEngine.Object> created)
        {
            StatusEffects Make(string name,StatusEffects.AilmentKind kind,ElementMask elements,
                float magnitude,int duration,int maxStacks,StatusEffects.StackPolicy policy)
            {
                var effect=ScriptableObject.CreateInstance<StatusEffects>();created.Add(effect);
                effect.ConfigureRuntime(name,StatusEffects.StatusType.DamageOverTime,kind,elements,
                    magnitude,duration,maxStacks,policy,2);
                return effect;
            }
            return new[]{
                Make("Core Poison",StatusEffects.AilmentKind.Poison,ElementMask.All,.1f,4,0,
                    StatusEffects.StackPolicy.StackIndependently),
                Make("Core Bleed",StatusEffects.AilmentKind.Bleed,ElementMask.Phys,.5f,5,5,
                    StatusEffects.StackPolicy.StackIndependently),
                Make("Core Ignite",StatusEffects.AilmentKind.Ignite,ElementMask.Fire,.8f,2,1,
                    StatusEffects.StackPolicy.ReplaceIfStronger)
            };
        }

        static CoreBalanceReferenceRow Capture(GameObject player,GameObject enemy,StatusEffects[] ailments,
            PlayerSkillId archetype,bool autoBaseline,int playerLevel,int combatLevel,int track,int relicTrack,int seed,int sample,
            int selected,int implicits,int explicits,int crafted,float implicitScore,float explicitScore,
            int passives,string role)
        {
            {
                var health=enemy.GetComponent<HealthComponent>();
                var ai=enemy.GetComponent<EnemyAI>();
                var enemyStats=enemy.GetComponent<StatsComponent>();
                var playerStats=player.GetComponent<StatsComponent>();
                var controller=player.GetComponent<PlayerController>();
                var skills=player.GetComponent<PlayerSkillController>();
                var mana=player.GetComponent<ManaComponent>();
                var playerHealth=player.GetComponent<HealthComponent>();
                var normal=controller.BuildNonCriticalAttackContext();
                float playerCrit=Mathf.Clamp01(controller.GetFinalCritChance());
                float playerCritMult=CombatCalculator.BaseCriticalMultiplier+playerStats.GetStat(StatTypes.CritMult);
                float playerHit=CombatCalculator.CalculateFinalDamage(normal,playerStats,enemyStats);
                float playerSpeed=controller.GetFinalAttackSpeed();
                float enemySpeed=ai.GetFinalAttackSpeed();
                float hitTwice=1f+Mathf.Clamp01(playerStats.GetStat(StatTypes.ChanceToHitTwice));
                float critFactor=1f+playerCrit*(playerCritMult-1f);
                float playerDps=playerHit*(1f+playerCrit*(playerCritMult-1f))
                    *hitTwice*playerSpeed;
                int resistanceImplicitCount=0,resistanceExplicitCount=0,allResCount=0;
                int prefixCount=0,suffixCount=0,topTierExplicitCount=0;
                float resistanceImplicitPoints=0f;
                foreach(var gear in player.GetComponentsInChildren<Gear>(true))
                    foreach(var mod in gear.rolledMods)
                    {
                        if(mod==null||Gear.IsWeaponBaseStat(mod.statType))continue;
                        bool resistance=mod.statType is StatTypes.FireRes or StatTypes.ColdRes
                            or StatTypes.LightRes or StatTypes.VoidRes or StatTypes.AllRes;
                        if(mod.statType==StatTypes.AllRes)allResCount++;
                        if(mod.lockedOriginal)
                        {
                            if(resistance){resistanceImplicitCount++;resistanceImplicitPoints+=mod.value;}
                        }
                        else
                        {
                            if(resistance)resistanceExplicitCount++;
                            if(AffixPolicy.Side(mod.statType)==AffixSide.Prefix)prefixCount++;
                            else suffixCount++;
                            if(mod.tierIndex==1)topTierExplicitCount++;
                        }
                    }
                var enemyNormal=ai.BuildNonCriticalAttackContext();
                float enemyHit=CombatCalculator.CalculateFinalDamage(enemyNormal,enemyStats,playerStats);
                float enemyDps=enemyHit*(1f+Mathf.Clamp01(ai.GetFinalCritChance())
                    *(CombatCalculator.BaseCriticalMultiplier+enemyStats.GetStat(StatTypes.CritMult)-1f))
                    *(1f+Mathf.Clamp01(enemyStats.GetStat(StatTypes.ChanceToHitTwice)))
                    *enemySpeed;
                var skill=skills.SelectedSkill;
                float skillHit=0f,cost=0f;int hitCount=0;
                DamageContext direct=default,specialized=default;
                if(skill!=null)
                {
                    var converted=controller.BuildNonCriticalConvertedAttackContext(skill.conversionElement,
                        skill.nonMatchingConversion);converted.Scopes=skill.DamageScopes;
                    float levelFactor=PlayerSkillController.SkillDamageLevelFactor(skills.EffectiveSkillLevel(skill));
                    direct=Scale(converted,skill.hitDamageMultiplier*levelFactor);
                    specialized=skill.id switch
                    {
                        PlayerSkillId.Envenom or PlayerSkillId.Immolate=>Scale(converted,
                            skill.ailmentBasisMultiplier*levelFactor),
                        PlayerSkillId.Shiv=>Scale(direct,skill.ailmentBasisMultiplier),
                        _=>direct
                    };
                    skillHit=CombatCalculator.CalculateFinalDamage(direct,playerStats,enemyStats)
                        *critFactor;
                    cost=skills.ManaCost(skill);
                    hitCount=skill.projectile?BattleManager.CalculateProjectileCount(
                        playerStats.GetRawStat(StatTypes.ProjectileAmount),
                        player.GetComponent<PassiveKeystoneState>().ProjectileAmountBonus)
                        :Mathf.Max(1,skill.baseHitCount);
                }
                float regen=Mathf.Max(0f,playerStats.GetStat(StatTypes.ManaRegeneration));
                float burstCasts=cost>0?Mathf.Min(10f/1.5f,(mana.MaxMana+regen*10f)/cost):10f/1.5f;
                float sustainedCasts=cost>0?Mathf.Min(30f/1.5f,(mana.MaxMana+regen*30f)/cost):30f/1.5f;
                skillHit*=hitCount*hitTwice;
                float basicAilment=ExpectedAilmentDps(normal,default,null,ailments,playerStats,enemyStats,
                    playerSpeed*hitTwice,playerSpeed,enemySpeed,critFactor);
                float skillAilment=skill!=null?ExpectedAilmentDps(direct,specialized,skill,ailments,
                    playerStats,enemyStats,sustainedCasts/30f*hitCount*hitTwice,playerSpeed,enemySpeed,critFactor):0f;
                float burstAilment=skill!=null?ExpectedAilmentDps(direct,specialized,skill,ailments,
                    playerStats,enemyStats,burstCasts/10f*hitCount*hitTwice,playerSpeed,enemySpeed,critFactor):0f;
                float burst=playerDps+basicAilment+skillHit*burstCasts/10f+burstAilment;
                float sustained=playerDps+basicAilment+skillHit*sustainedCasts/30f+skillAilment;
                float skillDirectDps=skillHit*sustainedCasts/30f;
                var playerLow=controller.BuildNonCriticalAttackContextAtRangeEnd(false);
                var playerHigh=controller.BuildNonCriticalAttackContextAtRangeEnd(true);
                var enemyLow=ai.BuildNonCriticalAttackContextAtRangeEnd(false);
                var enemyHigh=ai.BuildNonCriticalAttackContextAtRangeEnd(true);
                int engagedWins=0,idleWins=0,engagedLosses=0;
                float engagedWinSeconds=0f,idleWinSeconds=0f,deathSeconds=0f;
                float eventAilment=0f,eventMana=0f,eventCasts=0f;
                for(int trial=0;trial<DuelTrials;trial++)
                {
                    int duelSeed=unchecked(seed*131071+combatLevel*8191+sample*1013
                        +track*173+(int)archetype*41+(autoBaseline?7:0)
                        +(role=="boss"?29:0)+trial*113);
                    var idle=BalanceCombatSimulator.Simulate(normal,playerStats,
                        playerHealth.MaxLife,playerSpeed,playerCrit,enemyNormal,enemyStats,
                        health.MaxLife,enemySpeed,Mathf.Clamp01(ai.GetFinalCritChance()),
                        ailments,duelSeed,120f,playerLow,playerHigh,enemyLow,enemyHigh);
                    if(idle.Winner==1){idleWins++;idleWinSeconds+=idle.Seconds;}
                    var engaged=idle;
                    if(skill!=null)
                    {
                        var basis=specialized.Hits==null?direct:specialized;
                        var plan=new BalanceCombatSimulator.ActiveSkillPlan(skill,direct,basis,
                            cost,mana.MaxMana,regen,hitCount);
                        engaged=BalanceCombatSimulator.SimulateWithSkill(normal,playerStats,
                            playerHealth.MaxLife,playerSpeed,playerCrit,enemyNormal,enemyStats,
                            health.MaxLife,enemySpeed,Mathf.Clamp01(ai.GetFinalCritChance()),
                            ailments,plan,duelSeed,120f,playerLow,playerHigh,enemyLow,enemyHigh);
                    }
                    if(engaged.Winner==1){engagedWins++;engagedWinSeconds+=engaged.Seconds;}
                    else if(engaged.Winner==-1){engagedLosses++;deathSeconds+=engaged.Seconds;}
                    eventAilment+=engaged.AilmentEnemyDamage/
                        Mathf.Max(.0001f,engaged.AilmentEnemyDamage+engaged.DirectEnemyDamage);
                    eventMana+=engaged.ManaRemaining;eventCasts+=engaged.SkillCasts;
                }
                return new CoreBalanceReferenceRow
                {
                    seed=seed,sample=sample,playerLevel=playerLevel,combatLevel=combatLevel,gearTrack=track,relicTrack=relicTrack,
                    archetype=archetype,autoBaseline=autoBaseline,enemyRole=role,playerLife=playerHealth.MaxLife,
                    playerMana=mana.MaxMana,manaRegeneration=regen,
                    playerSpeed=playerSpeed,playerNormalHit=playerHit,playerBasicDps=playerDps+basicAilment,
                    basicAilmentDps=basicAilment,skillDirectDps=skillDirectDps,
                    fireRes=playerStats.GetStat(StatTypes.FireRes)+playerStats.GetStat(StatTypes.AllRes),
                    coldRes=playerStats.GetStat(StatTypes.ColdRes)+playerStats.GetStat(StatTypes.AllRes),
                    lightRes=playerStats.GetStat(StatTypes.LightRes)+playerStats.GetStat(StatTypes.AllRes),
                    voidRes=playerStats.GetStat(StatTypes.VoidRes)+playerStats.GetStat(StatTypes.AllRes),
                    armour=playerStats.GetStat(StatTypes.FlatArmour)
                        *(1f+playerStats.GetStat(StatTypes.ArmourPercent)),
                    implicitScore=implicitScore,explicitScore=explicitScore,
                    enemyLife=health.MaxLife,enemySpeed=ai.GetFinalAttackSpeed(),enemyNormalHit=enemyHit,
                    enemyIntrinsicDamageFactor=ai.IntrinsicDamageFactor,
                    enemyWeaponBaseDamage=ai.EquippedWeaponBaseDamage,
                    enemyGearPieces=ai.EquippedItems.Count,enemyRarity=(int)ai.CurrentRarity,
                    enemyDps=enemyDps,normalTtk=health.MaxLife/Mathf.Max(.0001f,playerDps+basicAilment),
                    normalTtd=playerHealth.MaxLife/Mathf.Max(.0001f,enemyDps),skillCost=cost,
                    skillDirectHit=skillHit,skillAilmentDps=skillAilment,skillBurst10=burst,
                    skillSustain30=sustained,engagedTtk=health.MaxLife/Mathf.Max(.0001f,sustained),
                    ailmentShare=(basicAilment+skillAilment)/Mathf.Max(.0001f,sustained),
                    eventEngagedWinRate=engagedWins/(float)DuelTrials,
                    eventIdleWinRate=idleWins/(float)DuelTrials,
                    eventVictorySeconds=engagedWins>0?engagedWinSeconds/engagedWins:0f,
                    eventIdleVictorySeconds=idleWins>0?idleWinSeconds/idleWins:0f,
                    eventDeathSeconds=engagedLosses>0?deathSeconds/engagedLosses:0f,
                    eventAilmentShare=eventAilment/DuelTrials,
                    eventManaRemaining=eventMana/DuelTrials,eventSkillCasts=eventCasts/DuelTrials,
                    selectedGear=selected,implicitCount=implicits,explicitCount=explicits,
                    passiveCount=passives,craftedItems=crafted,
                    resistanceImplicitCount=resistanceImplicitCount,
                    resistanceExplicitCount=resistanceExplicitCount,
                    allResCount=allResCount,resistanceImplicitPoints=resistanceImplicitPoints,
                    prefixCount=prefixCount,suffixCount=suffixCount,
                    topTierExplicitCount=topTierExplicitCount
                };
            }
        }

        static DamageContext Scale(DamageContext original,float factor)
        {
            var result=new DamageContext(original.Hits?.Count??1){Scopes=original.Scopes};
            if(original.Hits!=null)foreach(var hit in original.Hits)result.AddDamage(hit.Element,hit.Amount*factor);
            return result;
        }

        static float ExpectedAilmentDps(DamageContext direct,DamageContext specialized,
            PlayerSkillDefinition skill,StatusEffects[] ailments,StatsComponent attacker,
            StatsComponent defender,float applicationsPerSecond,float playerSpeed,float enemySpeed,float critFactor)
        {
            if(applicationsPerSecond<=0f)return 0f;
            float total=0f;
            foreach(StatusEffects effect in ailments)
            {
                DamageContext basis=skill!=null&&skill.specializedAilment==effect.Ailment
                    ?specialized:direct;
                if(basis.Hits==null||AilmentCalculator.GetSourceHitDamage(effect,basis)<=0f)continue;
                StatTypes chanceStat=effect.Ailment switch
                {
                    StatusEffects.AilmentKind.Poison=>StatTypes.PoisonChance,
                    StatusEffects.AilmentKind.Bleed=>StatTypes.BleedChance,
                    _=>StatTypes.IgniteChance
                };
                float chance=BattleManager.AdjustForApplicationResistance(effect,
                    BattleManager.AdjustedChance(attacker,chanceStat),attacker,defender);
                if(skill!=null&&skill.specializedAilment==effect.Ailment)
                    chance+=Mathf.Max(0,skill.guaranteedAilmentApplications);
                if(chance<=0f)continue;
                float magnitude=skill!=null&&skill.specializedAilment==StatusEffects.AilmentKind.Ignite
                    ?skill.absoluteAilmentCoefficient:-1f;
                AilmentCalculator.ComputeAilmentFromHit(effect,basis,attacker,
                    out float perTick,out int ticks,out int interval,magnitude);
                if(perTick<=0f||ticks<=0)continue;
                float finalTick=CombatCalculator.CalculateAilmentTickDamage(perTick,effect,attacker,defender)
                    *critFactor;
                float turns=effect.Ailment==StatusEffects.AilmentKind.Poison
                    ?playerSpeed+enemySpeed:enemySpeed;
                float tickFrequency=Mathf.Max(.01f,turns/Mathf.Max(1,interval));
                float applicationRate=applicationsPerSecond*chance;
                int cap=effect.Ailment switch
                {
                    StatusEffects.AilmentKind.Poison=>int.MaxValue,
                    StatusEffects.AilmentKind.Bleed=>5,
                    _=>1
                };
                var keystones=attacker.GetComponent<PassiveKeystoneState>();
                if(cap!=int.MaxValue&&keystones!=null)cap=keystones.EffectiveAilmentStackCap(effect);
                if(effect.Ailment==StatusEffects.AilmentKind.Bleed)
                    cap+=RelicInventory.Instance?.MaximumBleedStackBonus??0;
                else if(effect.Ailment==StatusEffects.AilmentKind.Ignite)
                    cap+=RelicInventory.Instance?.MaximumIgniteStackBonus??0;
                float expectedStacks=applicationRate*ticks/tickFrequency;
                float activeStacks=cap==int.MaxValue?expectedStacks
                    :cap==1?1f-Mathf.Exp(-expectedStacks):Mathf.Min(cap,expectedStacks);
                total+=finalTick*tickFrequency*activeStacks;
            }
            return total;
        }

        static int[] BuildLegalRanks(int level,PlayerSkillId archetype,Element weaponElement)
        {
            var ranks=new int[PassiveTreeDefinition.NodeCount];
            int budget=level-1;
            PassiveBranch focus=archetype switch
            {
                PlayerSkillId.IceStrike=>PassiveBranch.Cold,
                PlayerSkillId.LightningStrike=>PassiveBranch.Lightning,
                PlayerSkillId.Fireball or PlayerSkillId.Immolate=>PassiveBranch.Fire,
                PlayerSkillId.Envenom=>PassiveBranch.Poison,_=>PassiveBranch.Physical
            };
            PassiveBranch utility=archetype switch
            {
                PlayerSkillId.IceStrike=>PassiveBranch.ChillChance,
                PlayerSkillId.LightningStrike=>PassiveBranch.ShockChance,
                PlayerSkillId.Fireball=>PassiveBranch.Projectile,
                PlayerSkillId.Envenom=>PassiveBranch.PoisonChance,
                PlayerSkillId.Shiv=>PassiveBranch.BleedChance,
                PlayerSkillId.Immolate=>PassiveBranch.IgniteChance,_=>PassiveBranch.AttackSpeed
            };
            foreach(PassiveBranch branch in new[]{focus,utility,PassiveBranch.Life,PassiveBranch.Defense,
                PassiveBranch.Mana,PassiveBranch.AttackSpeed})
            {
                if(branch==focus&&budget>=13&&(focus!=PassiveBranch.Physical||weaponElement==Element.Phys)
                    &&focus!=PassiveBranch.Poison)
                    AllocateTowards(PassiveTreeDefinition.KeystoneNodeId(branch),ranks,ref budget);
                for(int p=0;p<PassiveTreeDefinition.NodesInBranch(branch)&&budget>0;p++)
                    AllocateTowards(PassiveTreeDefinition.NodeId(branch,p),ranks,ref budget);
            }
            return ranks;
        }

        static void AllocateTowards(int target,int[] ranks,ref int budget)
        {
            if(ranks[target]!=0||budget<=0)return;
            var queue=new Queue<int>();var previous=new int[ranks.Length];
            for(int i=0;i<previous.Length;i++)previous[i]=-2;
            for(int i=0;i<ranks.Length;i++)if(ranks[i]!=0||PassiveTreeDefinition.IsRootConnected(i))
            {previous[i]=-1;queue.Enqueue(i);}
            while(queue.Count>0&&previous[target]==-2)
            {
                int current=queue.Dequeue();
                foreach(int next in PassiveTreeDefinition.AdjacentNodeIds(current))
                    if(previous[next]==-2){previous[next]=current;queue.Enqueue(next);}
            }
            if(previous[target]==-2)throw new InvalidOperationException("Passive graph target is unreachable.");
            var path=new Stack<int>();
            for(int current=target;current>=0&&ranks[current]==0;current=previous[current])path.Push(current);
            if(path.Count>budget)return;
            while(path.Count>0){int id=path.Pop();if(ranks[id]==0){ranks[id]=1;budget--;}}
        }

        static void InvokeAwake(Component component)=>component.GetType()
            .GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(component,null);
        static void SetField(object target,string name,object value)=>target.GetType()
            .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(target,value);
        static int ArgumentInt(string flag,int fallback)=>int.TryParse(ArgumentString(flag,null),out int value)?value:fallback;
        static string ArgumentString(string flag,string fallback)
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]==flag)return args[i+1];
            return fallback;
        }
    }
}
