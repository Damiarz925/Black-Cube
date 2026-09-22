using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class ExperimentMetadata
    {
        public string experimentType,timestampUtc,gitCommit,dataFingerprint,scenario;
        public long seed; public int sampleCount,startLevel,endLevel,increment;
        public static ExperimentMetadata Create(string type,long seed,int count,int start,int end,string scenario)
            =>new(){experimentType=type,timestampUtc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture),seed=seed,sampleCount=count,startLevel=start,endLevel=end,increment=1,scenario=scenario,gitCommit=ProductionBalanceAdapters.GitCommit(),dataFingerprint=ProductionBalanceAdapters.DataFingerprint()};
    }

    [Serializable] public sealed class MetricSummary
    {
        public int count; public double minimum,maximum,mean,median,standardDeviation,p01,p05,p10,p25,p50,p75,p90,p95,p99;
        public static MetricSummary From(IEnumerable<double> source)
        {
            double[] a=source?.Where(double.IsFinite).OrderBy(x=>x).ToArray()??Array.Empty<double>();var r=new MetricSummary{count=a.Length};if(a.Length==0)return r;
            r.minimum=a[0];r.maximum=a[^1];r.mean=a.Average();r.median=r.p50=P(a,.50);r.p01=P(a,.01);r.p05=P(a,.05);r.p10=P(a,.10);r.p25=P(a,.25);r.p75=P(a,.75);r.p90=P(a,.90);r.p95=P(a,.95);r.p99=P(a,.99);
            double sum=0;foreach(double v in a){double d=v-r.mean;sum+=d*d;}r.standardDeviation=Math.Sqrt(sum/a.Length);return r;
        }
        // R-7 / Excel PERCENTILE.INC: linear interpolation at (n-1)*p.
        public static double P(double[] sorted,double p){if(sorted==null||sorted.Length==0)return 0;p=Math.Clamp(p,0,1);double x=(sorted.Length-1)*p;int lo=(int)Math.Floor(x),hi=(int)Math.Ceiling(x);return sorted[lo]+(sorted[hi]-sorted[lo])*(x-lo);}
    }

    [Serializable] public sealed class HistogramBin { public double minimum,maximum; public int count; }
    public static class WorkbenchStatistics
    {
        public static List<HistogramBin> Histogram(IEnumerable<double> source,int bins=24)
        {
            double[] a=source?.Where(double.IsFinite).ToArray()??Array.Empty<double>();var result=new List<HistogramBin>();if(a.Length==0)return result;double min=a.Min(),max=a.Max();if(max<=min)max=min+1;bins=Math.Clamp(bins,1,200);double width=(max-min)/bins;for(int i=0;i<bins;i++)result.Add(new HistogramBin{minimum=min+i*width,maximum=min+(i+1)*width});foreach(double v in a)result[Math.Min(bins-1,(int)((v-min)/width))].count++;return result;
        }
    }

    public enum ItemLabMode{Weapon,AnyEquipment,SpecificSlot}
    [Serializable] public sealed class ItemLabRequest
    {
        public ItemLabMode mode=ItemLabMode.Weapon;public LootManager.GearType slot=LootManager.GearType.Weapons;public string weaponTypeId="Any";public int itemLevel=50,sampleCount=1000;public LootManager.GearRarity rarity=LootManager.GearRarity.Rare;public bool naturalRarity;public long seed=1001;
    }
    [Serializable] public sealed class ItemSample
    {
        public int index,itemLevel,affixCount;public string slot,weaponType,rarity,element,detail;public double minimumDamage,maximumDamage,averageHit,attackSpeed,crit,weaponDps,gearScore;
    }
    [Serializable] public sealed class ItemLabResult
    {
        public ExperimentMetadata metadata;public List<ItemSample> samples=new();public MetricSummary weaponDps,gearScore,minimumDamage,maximumDamage,attackSpeed,crit,affixCount;
        public void Summarize(){weaponDps=MetricSummary.From(samples.Select(x=>x.weaponDps));gearScore=MetricSummary.From(samples.Select(x=>x.gearScore));minimumDamage=MetricSummary.From(samples.Select(x=>x.minimumDamage));maximumDamage=MetricSummary.From(samples.Select(x=>x.maximumDamage));attackSpeed=MetricSummary.From(samples.Select(x=>x.attackSpeed));crit=MetricSummary.From(samples.Select(x=>x.crit));affixCount=MetricSummary.From(samples.Select(x=>(double)x.affixCount));}
    }

    [Serializable] public sealed class EnemyLabRequest
    {
        public string archetypeId="Any";public int level=80,sampleCount=100,corruptionPercentage;public EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Rare;public bool productionRarity;public bool forcePrimaryDamage;public Element primaryDamage=Element.Phys;public long seed=2001;
        [NonSerialized] public bool useScalingOverride;[NonSerialized] public EnemyScalingValues scalingOverride;[NonSerialized] public CorruptionMechanicProfile corruptionOverride;[NonSerialized] public EnemyRarityProfile rarityOverride;
    }
    [Serializable] public sealed class EnemySample
    {
        public int index,level,slotCount,candidateCount,rarity;public string archetype,primaryDamage,equipment;public double life,damagePerHit,attackSpeed,dps,armour,fireResistance,coldResistance,lightningResistance,voidResistance,lifeRegeneration,gearScore,weaponDps,critChance,critMultiplier;
    }
    [Serializable] public sealed class EnemyLabResult
    {
        public ExperimentMetadata metadata;public List<EnemySample> samples=new();public MetricSummary life,dps,gearScore,armour,slots;
        public void Summarize(){life=MetricSummary.From(samples.Select(x=>x.life));dps=MetricSummary.From(samples.Select(x=>x.dps));gearScore=MetricSummary.From(samples.Select(x=>x.gearScore));armour=MetricSummary.From(samples.Select(x=>x.armour));slots=MetricSummary.From(samples.Select(x=>(double)x.slotCount));}
    }

    [Serializable] public sealed class DropLabRequest
    {
        public int level=80,sampleCount=10000;public EnemyAI.EnemyRarity rarity=EnemyAI.EnemyRarity.Rare;public bool boss,generateActualEnemyGear;public string archetypeId="Any";public bool forcePrimaryDamage;public Element primaryDamage=Element.Phys;public float fixedGearQuality=1f;public long seed=3001;
    }
    [Serializable] public sealed class CurrencyDropMetric
    {public string stableId,name;public float weight;public int minimumLevel,qualityTier;public double chancePerKill,averagePerKill,perHundred,perThousand,expectedKillsPerDrop;}
    [Serializable] public sealed class DropLabResult
    {
        public ExperimentMetadata metadata;public double averageGearItems,averageCurrencyRolls,chanceAnyCurrency,averageCurrencyStacks;public int maximumGearItems;public List<CurrencyDropMetric> currencies=new();public MetricSummary gearCount,currencyRolls;
    }

    [Serializable] public sealed class CurvePoint { public double x,mean,p10,p50,p90,p99; }
    [Serializable] public sealed class CurveSeries { public string name;public Color color=Color.white;public bool visible=true;public List<CurvePoint> points=new(); }

    public sealed class WorkbenchSession:IDisposable
    {
        public readonly ModManager Roller;readonly GameObject rollerObject,itemObject;public readonly Gear ReusableItem;
        public WorkbenchSession()
        {
            var db=AssetDatabase.LoadAssetAtPath<ModDatabase>("Assets/Prefabs/Scriptable Objects/ModDatabase.asset");if(db==null)throw new InvalidOperationException("Missing production ModDatabase.asset");db.Initialize();
            rollerObject=new GameObject("Balance Workbench Roller"){hideFlags=HideFlags.HideAndDontSave};rollerObject.SetActive(false);Roller=rollerObject.AddComponent<ModManager>();Roller.ConfigureForIsolatedRolling(db);
            itemObject=new GameObject("Balance Workbench Reusable Item"){hideFlags=HideFlags.HideAndDontSave};ReusableItem=itemObject.AddComponent<Gear>();
        }
        public void Dispose(){if(itemObject!=null)UnityEngine.Object.DestroyImmediate(itemObject);if(rollerObject!=null)UnityEngine.Object.DestroyImmediate(rollerObject);}
    }

    public static class ProductionBalanceAdapters
    {
        public static ItemLabResult RunItems(ItemLabRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var result=new ItemLabResult{metadata=ExperimentMetadata.Create("Weapon / Item Lab",request.seed,request.sampleCount,request.itemLevel,request.itemLevel,JsonUtility.ToJson(request))};var rng=new SeededSimulationRandomSource(request.seed);
            using var session=new WorkbenchSession();for(int i=0;i<request.sampleCount;i++){if(cancelled?.Invoke()==true)break;result.samples.Add(GenerateItem(request,i,rng,session));if((i&63)==0)progress?.Invoke((i+1f)/request.sampleCount);}result.metadata.sampleCount=result.samples.Count;result.Summarize();return result;
        }
        static ItemSample GenerateItem(ItemLabRequest request,int index,ILootRandomSource rng,WorkbenchSession session)
        {
            LootManager.GearType type=request.mode==ItemLabMode.Weapon?LootManager.GearType.Weapons:request.mode==ItemLabMode.SpecificSlot?request.slot:(LootManager.GearType)rng.Range(0,Enum.GetValues(typeof(LootManager.GearType)).Length);
            var rarity=request.naturalRarity?RollRarity(request.itemLevel,rng):request.rarity;Element element=type==LootManager.GearType.Weapons?RollElement(rng):(Element)rng.Range(0,(int)Element.Count);
            string weapon=type==LootManager.GearType.Weapons?(request.weaponTypeId=="Any"?LootManager.RollWeaponTypeId(rng):request.weaponTypeId):null;var gear=session.ReusableItem;gear.Initialize(type,rarity,request.itemLevel,element,weapon);
            List<RolledMod> mods=null;for(int attempt=0;attempt<64&&mods==null;attempt++)mods=session.Roller.RollEquipmentModsForItem(type,rarity,request.itemLevel,element,gear.WeaponTypeId,rng);if(mods==null)throw new InvalidOperationException($"Production generator could not build {rarity} {type} at level {request.itemLevel}.");gear.ApplyMods(mods);if(type==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(gear);
            gear.GetEffectiveBaseDamageRange(out float low,out float high);double dps=gear.GetAverageWeaponDps();double score=type==LootManager.GearType.Weapons?Math.Max(.0001,dps)*(1+gear.GetEffectiveBaseCrit()):mods.Sum(x=>x==null?0:Math.Abs(x.value)+(x.hasSecondaryValue?Math.Abs(x.secondaryValue):0));
            return new ItemSample{index=index,itemLevel=request.itemLevel,slot=type.ToString(),weaponType=gear.WeaponTypeId,rarity=rarity.ToString(),element=element.ToString(),minimumDamage=low,maximumDamage=high,averageHit=(low+high)*.5,attackSpeed=gear.GetEffectiveAttackSpeed(),crit=gear.GetEffectiveBaseCrit(),weaponDps=dps,gearScore=score,affixCount=gear.CraftingModCount,detail=Describe(gear)};
        }
        static string Describe(Gear gear)=>$"{gear.ItemRarity} {gear.ItemType} L{gear.ItemLevel} {gear.WeaponTypeId}\n"+string.Join("\n",gear.rolledMods.Where(x=>x!=null).Select(x=>$"{x.statType} T{x.tierIndex}: {x.value:0.###}"+(x.hasSecondaryValue?$"–{x.secondaryValue:0.###}":"")));
        static LootManager.GearRarity RollRarity(int level,ILootRandomSource rng){Vector4 r=LootManager.RarityRatesForLevel(level);float v=rng.Value()*100;return v<r.x?LootManager.GearRarity.Normal:v<r.x+r.y?LootManager.GearRarity.Magic:v<r.x+r.y+r.z?LootManager.GearRarity.Rare:LootManager.GearRarity.Legendary;}
        static Element RollElement(ILootRandomSource rng){int v=rng.Range(0,5);return v<(int)Element.Poison?(Element)v:Element.Void;}

        public static EnemyLabResult RunEnemies(EnemyLabRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var result=new EnemyLabResult{metadata=ExperimentMetadata.Create("Enemy Gear Lab",request.seed,request.sampleCount,request.level,request.level,JsonUtility.ToJson(request))};var rng=new SeededSimulationRandomSource(request.seed);var prefabs=BlackCube.BalanceSimulationRunner.DiscoverEnemies();if(prefabs.Count==0)throw new InvalidOperationException("No production enemy prefab resolved.");var db=WorldContentCatalog.Reference;
            using var session=new WorkbenchSession();for(int i=0;i<request.sampleCount;i++){if(cancelled?.Invoke()==true)break;var archetype=ResolveArchetype(request,rng,db);GameObject prefab=archetype?.prefab!=null?archetype.prefab:prefabs[rng.Range(0,prefabs.Count)];var actor=UnityEngine.Object.Instantiate(prefab);actor.hideFlags=HideFlags.HideAndDontSave;try{var ai=actor.GetComponent<EnemyAI>();var corruption=db.corruptionTiers.OrderBy(x=>Math.Abs(x.percentage-request.corruptionPercentage)).FirstOrDefault();var biome=EnemyAuthoringAdapters.BiomeFor(db,archetype?.stableId);var location=biome?.locations.FirstOrDefault();if(archetype!=null)ai.ConfigureWorldContent(db,archetype,null,corruption,location,request.corruptionOverride,request.rarityOverride);var rarity=request.productionRarity?RollEnemyRarity(rng,db):request.rarity;ai.GenerateIsolatedBuild(request.level,session.Roller,rarity,rng,request.forcePrimaryDamage?request.primaryDamage:null,request.useScalingOverride?request.scalingOverride:null);result.samples.Add(CaptureEnemy(i,request.level,archetype,ai,actor));}finally{UnityEngine.Object.DestroyImmediate(actor);}if((i&7)==0)progress?.Invoke((i+1f)/request.sampleCount);}result.metadata.sampleCount=result.samples.Count;result.Summarize();return result;
        }
        static EnemyArchetypeDefinition ResolveArchetype(EnemyLabRequest r,ILootRandomSource rng,WorldContentDatabase db){if(db?.enemyArchetypes==null||db.enemyArchetypes.Count==0)return null;if(r.archetypeId!="Any")return db.Enemy(r.archetypeId);return db.enemyArchetypes[rng.Range(0,db.enemyArchetypes.Count)];}
        static EnemyAI.EnemyRarity RollEnemyRarity(ILootRandomSource r,WorldContentDatabase db){var profiles=db?.enemyRarityProfiles?.Where(x=>x!=null&&x.spawnWeight>0).ToList();if(profiles?.Count>0){int total=profiles.Sum(x=>x.spawnWeight),roll=r.Range(0,total);foreach(var profile in profiles){if(roll<profile.spawnWeight)return profile.rarity;roll-=profile.spawnWeight;}}int v=r.Range(0,71);return v<40?EnemyAI.EnemyRarity.Normal:v<60?EnemyAI.EnemyRarity.Magic:v<70?EnemyAI.EnemyRarity.Rare:EnemyAI.EnemyRarity.Legendary;}
        static EnemySample CaptureEnemy(int index,int level,EnemyArchetypeDefinition archetype,EnemyAI ai,GameObject actor)
        {
            var stats=actor.GetComponent<StatsComponent>();var health=actor.GetComponent<HealthComponent>();var hit=ai.BuildNonCriticalAttackContext();double amount=hit.Hits==null?0:hit.Hits.Sum(x=>(double)x.Amount);double aps=ai.GetFinalAttackSpeed();var weapon=ai.EquippedWeapon;
            return new EnemySample{index=index,level=level,archetype=archetype?.displayName??actor.name,rarity=(int)ai.CurrentRarity,primaryDamage=(archetype?.primaryElement??ai.WeaponMainElement).ToString(),slotCount=ai.EquippedItems.Count,candidateCount=EnemyBuildOptimizer.CandidateCountForLevel(level),life=health?.MaxLife??stats.GetStat(StatTypes.Life),damagePerHit=amount,attackSpeed=aps,dps=amount*aps,armour=stats.GetStat(StatTypes.FlatArmour),fireResistance=stats.GetStat(StatTypes.FireRes),coldResistance=stats.GetStat(StatTypes.ColdRes),lightningResistance=stats.GetStat(StatTypes.LightRes),voidResistance=stats.GetStat(StatTypes.VoidRes),lifeRegeneration=stats.GetStat(StatTypes.LifeRegeneration),gearScore=EnemyBuildOptimizer.CanonicalGearScore(ai.LastBuildEvaluation),weaponDps=weapon!=null?weapon.GetAverageWeaponDps():0,critChance=stats.GetStat(StatTypes.CritChance),critMultiplier=stats.GetStat(StatTypes.CritMult),equipment=string.Join(" | ",ai.EquippedItems.Select(x=>$"{x.ItemType}:{x.ItemRarity}:{x.WeaponTypeId}"))};
        }

        public static DropLabResult RunDrops(DropLabRequest request,Action<float> progress=null,Func<bool> cancelled=null)
        {
            var result=new DropLabResult{metadata=ExperimentMetadata.Create("Drop Simulator",request.seed,request.sampleCount,request.level,request.level,JsonUtility.ToJson(request))};var rng=new SeededSimulationRandomSource(request.seed);var gearCounts=new List<double>(request.sampleCount);var rollCounts=new List<double>(request.sampleCount);var counts=new Dictionary<CraftingCurrencyType,int>();var hits=new Dictionary<CraftingCurrencyType,int>();int any=0,stacks=0,max=0;float expected=EnemyLootProfile.ExpectedGearScore(request.level,request.rarity,1);var qualities=new List<float>{request.fixedGearQuality};
            if(request.generateActualEnemyGear){var enemies=RunEnemies(new EnemyLabRequest{level=request.level,sampleCount=Math.Min(request.sampleCount,1000),rarity=request.rarity,archetypeId=request.archetypeId,forcePrimaryDamage=request.forcePrimaryDamage,primaryDamage=request.primaryDamage,seed=request.seed+7919});qualities=enemies.samples.Select(x=>Mathf.Clamp((float)(x.gearScore/Math.Max(.001,EnemyLootProfile.ExpectedGearScore(request.level,request.rarity,x.slotCount))),.6f,3f)).ToList();if(qualities.Count==0)qualities.Add(1f);}
            for(int i=0;i<request.sampleCount;i++){if(cancelled?.Invoke()==true)break;float quality=qualities[i%qualities.Count];var power=new EnemyLootPowerSnapshot(EnemyLootProfile.LevelFactor(request.level),EnemyLootProfile.RarityMultiplier(request.rarity,request.boss),expected*quality,expected,quality);int gear=1+EnemyLootProfile.StochasticRound(power.ExtraGearBudget,EnemyLootProfile.MaximumExtraGear,rng);int rolls=EnemyLootProfile.StochasticRound(power.CurrencyRollBudget,EnemyLootProfile.MaximumCurrencyRolls,rng);gearCounts.Add(gear);rollCounts.Add(rolls);max=Math.Max(max,gear);var seen=new HashSet<CraftingCurrencyType>();for(int j=0;j<rolls;j++){var entry=CurrencyLootTable.Choose(request.level,request.rarity,request.boss,request.level>=RebirthManager.RequiredZone,rng);if(entry==null)continue;int amount=rng.Range(entry.StackMinimum,entry.StackMaximum+1);counts[entry.Currency]=counts.TryGetValue(entry.Currency,out int old)?old+amount:amount;seen.Add(entry.Currency);stacks++;}if(seen.Count>0)any++;foreach(var currency in seen)hits[currency]=hits.TryGetValue(currency,out int old)?old+1:1;if((i&255)==0)progress?.Invoke((i+1f)/request.sampleCount);}
            int n=gearCounts.Count;result.metadata.sampleCount=n;result.gearCount=MetricSummary.From(gearCounts);result.currencyRolls=MetricSummary.From(rollCounts);result.averageGearItems=result.gearCount.mean;result.averageCurrencyRolls=result.currencyRolls.mean;result.maximumGearItems=max;result.chanceAnyCurrency=n==0?0:any/(double)n;result.averageCurrencyStacks=n==0?0:stacks/(double)n;
            foreach(var e in CurrencyLootTable.Entries){int total=counts.TryGetValue(e.Currency,out int c)?c:0,hit=hits.TryGetValue(e.Currency,out int h)?h:0;double avg=n==0?0:total/(double)n,chance=n==0?0:hit/(double)n;result.currencies.Add(new CurrencyDropMetric{stableId=e.StableCurrencyId,name=CurrencyPresentation.Name(e.Currency),weight=e.BaseWeight,minimumLevel=e.MinimumCombatLevel,qualityTier=(int)e.QualityTier,averagePerKill=avg,chancePerKill=chance,perHundred=avg*100,perThousand=avg*1000,expectedKillsPerDrop=chance>0?1/chance:double.PositiveInfinity});}return result;
        }

        public static List<CurveSeries> IntrinsicCurves(int start,int end,int step,float authoredLife=100)
        {CurveSeries life=new(){name="Intrinsic Life",color=new Color(.25f,.85f,.4f)};CurveSeries damage=new(){name="Intrinsic Damage",color=new Color(1f,.35f,.2f)};CurveSeries armour=new(){name="Intrinsic Armour",color=new Color(.4f,.65f,1f)};CurveSeries res=new(){name="Intrinsic Resistance",color=new Color(.8f,.55f,1f)};for(int l=Math.Max(1,start);l<=Math.Max(start,end);l+=Math.Max(1,step)){var x=EnemyScalingMath.Calculate(l);life.points.Add(new CurvePoint{x=l,mean=x.ScaledLife(authoredLife)});damage.points.Add(new CurvePoint{x=l,mean=x.DamageFactor});armour.points.Add(new CurvePoint{x=l,mean=x.Armour});res.points.Add(new CurvePoint{x=l,mean=x.ResistancePoints});}return new(){life,damage,armour,res};}
        public static string DataFingerprint(){var paths=new List<string>{"Assets/Prefabs/Scriptable Objects/ModDatabase.asset","Assets/Resources/EnemyScalingProfile.asset","Assets/Resources/LootBalanceProfile.asset","Assets/Resources/GameData/PassiveTree/SO_PassiveTreeDatabase.asset","Assets/Resources/GameData/WorldContentDatabase.asset","Assets/Resources/PlayerSkills.asset"};foreach(string filter in new[]{"t:PassiveClassBranchSO","t:PassiveWeaponBranchSO","t:PlayerGearProfileSO"})paths.AddRange(AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath));using var sha=SHA256.Create();var bytes=new List<byte>();foreach(string p in paths.Distinct().OrderBy(x=>x,StringComparer.Ordinal))if(File.Exists(p)){bytes.AddRange(Encoding.UTF8.GetBytes(p));bytes.AddRange(File.ReadAllBytes(p));}return BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-",string.Empty).Substring(0,16);}
        public static string GitCommit(){try{var psi=new System.Diagnostics.ProcessStartInfo("git","rev-parse --short HEAD"){WorkingDirectory=Directory.GetParent(Application.dataPath).FullName,RedirectStandardOutput=true,UseShellExecute=false,CreateNoWindow=true};using var p=System.Diagnostics.Process.Start(psi);return p.StandardOutput.ReadToEnd().Trim();}catch{return "unavailable";}}
    }

    public static class WorkbenchExports
    {
        public const string Root="ReviewCaptures/BalanceWorkbench";
        public static string SaveJson(string name,object value){Directory.CreateDirectory(Root);string path=$"{Root}/{Safe(name)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";File.WriteAllText(path,JsonUtility.ToJson(value,true));AssetDatabase.Refresh();return path;}
        public static string SaveItemCsv(ItemLabResult r){var rows=new List<string>{Metadata(r.metadata),"Index,Level,Slot,Weapon,Rarity,Element,Min,Max,APS,Crit,DPS,GearScore,Affixes"};rows.AddRange(r.samples.Select(x=>$"{x.index},{x.itemLevel},{Q(x.slot)},{Q(x.weaponType)},{x.rarity},{x.element},{x.minimumDamage:R},{x.maximumDamage:R},{x.attackSpeed:R},{x.crit:R},{x.weaponDps:R},{x.gearScore:R},{x.affixCount}"));return SaveCsv("items",rows);}
        public static string SaveEnemyCsv(EnemyLabResult r){var rows=new List<string>{Metadata(r.metadata),"Index,Level,Archetype,Rarity,Life,Hit,APS,DPS,Armour,GearScore,Slots"};rows.AddRange(r.samples.Select(x=>$"{x.index},{x.level},{Q(x.archetype)},{x.rarity},{x.life:R},{x.damagePerHit:R},{x.attackSpeed:R},{x.dps:R},{x.armour:R},{x.gearScore:R},{x.slotCount}"));return SaveCsv("enemies",rows);}
        public static string SaveDropCsv(DropLabResult r){var rows=new List<string>{Metadata(r.metadata),"StableId,Name,Weight,MinLevel,Quality,ChancePerKill,AveragePerKill,Per100,Per1000,ExpectedKills"};rows.AddRange(r.currencies.Select(x=>$"{Q(x.stableId)},{Q(x.name)},{x.weight:R},{x.minimumLevel},{x.qualityTier},{x.chancePerKill:R},{x.averagePerKill:R},{x.perHundred:R},{x.perThousand:R},{x.expectedKillsPerDrop:R}"));return SaveCsv("drops",rows);}
        public static string SaveBuildCsv(PlayerBuildSnapshot b,PlayerBuildMetrics m){var rows=new List<string>{"Level,CombatLevel,Class,Subclass,Weapon,BasicDPS,AverageHit,APS,Life,Armour,Mana,GearScore,PassivePoints",$"{b.playerLevel},{b.combatLevel},{Q(b.classId)},{Q(b.subclassId)},{Q(b.weaponTypeId)},{m.basicDps:R},{m.averageHit:R},{m.attacksPerSecond:R},{m.life:R},{m.armour:R},{m.mana:R},{m.totalGearScore:R},{b.passiveStableIds.Count}"};return SaveCsv("player_build",rows);}
        public static string SavePassiveCsv(PassiveOptimizationResult r){var rows=new List<string>{"Point,StableId,Name,Branch,PrimaryDelta,SecondaryDelta,ObjectiveDelta,ObjectivePercent"};rows.AddRange(r.sequence.Select(x=>$"{x.point},{Q(x.stableId)},{Q(x.name)},{Q(x.branch)},{x.primaryDelta:R},{x.secondaryDelta:R},{x.scoreDelta:R},{x.scorePercent:R}"));return SaveCsv("passive_allocation",rows);}
        public static string SaveSweepCsv(ScenarioSweepResult r,OptimizationObjective objective){var rows=new List<string>{"PlayerLevel,CombatLevel,GearProfile,Primary,Secondary,ObjectiveScore,BasicDPS,AverageHit,APS,Life,Armour,EHPPhysical,Mana,CooldownReduction,GearScore,PassivePoints,Seed,Fingerprint"};rows.AddRange(r.points.Select(x=>$"{x.playerLevel},{x.combatLevel},{Q(x.profile)},{OptimizationMetricCatalog.Get(objective.primary).Value(x.metrics):R},{OptimizationMetricCatalog.Get(objective.secondary).Value(x.metrics):R},{x.objectiveScore:R},{x.metrics.basicDps:R},{x.metrics.averageHit:R},{x.metrics.attacksPerSecond:R},{x.metrics.life:R},{x.metrics.armour:R},{x.metrics.ehpPhysical:R},{x.metrics.mana:R},{x.metrics.cooldownReduction:R},{x.metrics.totalGearScore:R},{x.build.passiveStableIds.Count},{x.seed},{Q(r.dataFingerprint)}"));return SaveCsv("player_sweep",rows);}
        public static string SaveBehaviorCsv(EnemyBehaviorPreviewResult r){var rows=new List<string>{"Action,Skill,Rule,Reason"};rows.AddRange(r.timeline.Select(x=>$"{x.action},{Q(x.skill)},{Q(x.ruleId)},{Q(x.reason)}"));return SaveCsv("enemy_behavior",rows);}
        public static string SaveEnemyBatchCsv(EnemyBatchResult r){var rows=new List<string>{"Enemy,StableId,Biome,MeanLife,P90Life,MeanDPS,P90DPS,Armour,PrimaryResistance,GearScore,Skills,Behavior,Warnings"};rows.AddRange(r.rows.Select(x=>$"{Q(x.enemy)},{Q(x.stableId)},{Q(x.biome)},{x.meanLife:R},{x.p90Life:R},{x.meanDps:R},{x.p90Dps:R},{x.armour:R},{x.primaryResistance:R},{x.gearScore:R},{x.skillCount},{Q(x.behavior)},{Q(string.Join(" | ",x.warnings))}"));return SaveCsv("enemy_archetypes",rows);}
        public static string SaveEnemyComparisonCsv(IReadOnlyList<EnemyPreviewSnapshot> snapshots){var rows=new List<string>{"Enemy,ArchetypeId,Biome,Level,Rarity,Corruption,Seed,Life,Hit,APS,DPS,Armour,FireRes,ColdRes,LightningRes,VoidRes,Regen,GearScore,Equipment"};foreach(var p in snapshots){var x=p.sample;rows.Add($"{Q(p.displayName)},{Q(p.archetypeId)},{Q(p.biome)},{p.level},{p.rarity},{p.corruption},{p.seed},{x.life:R},{x.damagePerHit:R},{x.attackSpeed:R},{x.dps:R},{x.armour:R},{x.fireResistance:R},{x.coldResistance:R},{x.lightningResistance:R},{x.voidResistance:R},{x.lifeRegeneration:R},{x.gearScore:R},{Q(x.equipment)}");}return SaveCsv("enemy_comparison",rows);}
        public static string SaveScalingCsv(IReadOnlyList<CurveSeries> series){var rows=new List<string>{"Series,Level,Value"};foreach(var s in series)rows.AddRange(s.points.Select(x=>$"{Q(s.name)},{x.x:R},{x.mean:R}"));return SaveCsv("enemy_scaling",rows);}
        public static string SaveCombatTimelineCsv(BlackCube.CombatSimulation.CombatSimulationResult r){var rows=new List<string>{"Index,Time,Type,Category,Source,Target,Action,Reason,Amount,LifeBefore,LifeAfter,ManaBefore,ManaAfter,RageBefore,RageAfter,Rule,Phase,Projectile,Stacks"};rows.AddRange(r.trace.Select(x=>$"{x.index},{x.time:R},{x.type},{x.category},{Q(x.source)},{Q(x.target)},{Q(x.action)},{Q(x.reason)},{x.amount:R},{x.lifeBefore:R},{x.lifeAfter:R},{x.manaBefore:R},{x.manaAfter:R},{x.rageBefore:R},{x.rageAfter:R},{Q(x.ruleId)},{Q(x.phase)},{x.projectileIndex},{x.stackCount}"));return SaveCsv("combat_timeline",rows);}
        public static string SaveCombatCurveCsv(CombatCurveResult r){var rows=new List<string>{"LevelOrCategory,WinRate,P50Duration,P90Duration,RemainingLife,ManaStarvation,AilmentShare,Fingerprint"};rows.AddRange(r.points.Select(x=>$"{x.level},{x.winRate:R},{x.p50Duration:R},{x.p90Duration:R},{x.remainingLife:R},{x.manaStarvation:R},{x.ailmentShare:R},{Q(r.fingerprint)}"));return SaveCsv("combat_curve",rows);}
        public static string SaveCombatBatchCsv(CombatBatchResult r){var rows=new List<string>{"Fights,Wins,Losses,Timeouts,Errors,WinRate,LossRate,TimeoutRate,DurationMean,DurationP10,DurationP50,DurationP90,DurationP99,PlayerLifeP50,EnemyLifeP50,DPSMean,ManaStarvationMean,Fingerprint",$"{r.fights},{r.wins},{r.losses},{r.timeouts},{r.errors},{r.winRate:R},{r.lossRate:R},{r.timeoutRate:R},{r.duration.mean:R},{r.duration.p10:R},{r.duration.p50:R},{r.duration.p90:R},{r.duration.p99:R},{r.playerLifeOnWin.p50:R},{r.enemyLifeOnLoss.p50:R},{r.playerDps.mean:R},{r.manaStarvation.mean:R},{Q(r.fingerprint)}","Source,Damage,Effective,Overheal,Count"};rows.AddRange(r.damage.Select(x=>$"{Q(x.id)},{x.total:R},{x.effective:R},{x.overheal:R},{x.count}"));return SaveCsv("combat_batch",rows);}
        public static string SaveCombatMatrixCsv(CombatMatrixResult r){var rows=new List<string>{"Player,Enemy,WinRate,MedianDuration,MedianPlayerLife,TimeoutRate,Seed,Fingerprint"};rows.AddRange(r.cells.Select(x=>$"{Q(x.player)},{Q(x.enemy)},{x.winRate:R},{x.medianDuration:R},{x.medianPlayerLife:R},{x.timeoutRate:R},{x.seed},{Q(r.fingerprint)}"));return SaveCsv("combat_matrix",rows);}
        public static string ItemCsv(ItemLabResult r){var rows=new List<string>{Metadata(r.metadata),"Index,Level,Slot,Weapon,Rarity,Element,Min,Max,APS,Crit,DPS,GearScore,Affixes"};rows.AddRange(r.samples.Select(x=>$"{x.index},{x.itemLevel},{Q(x.slot)},{Q(x.weaponType)},{x.rarity},{x.element},{x.minimumDamage:R},{x.maximumDamage:R},{x.attackSpeed:R},{x.crit:R},{x.weaponDps:R},{x.gearScore:R},{x.affixCount}"));return string.Join("\n",rows);}
        public static string EnemyCsv(EnemyLabResult r){var rows=new List<string>{Metadata(r.metadata),"Index,Level,Archetype,Rarity,Life,Hit,APS,DPS,Armour,GearScore,Slots"};rows.AddRange(r.samples.Select(x=>$"{x.index},{x.level},{Q(x.archetype)},{x.rarity},{x.life:R},{x.damagePerHit:R},{x.attackSpeed:R},{x.dps:R},{x.armour:R},{x.gearScore:R},{x.slotCount}"));return string.Join("\n",rows);}
        public static string DropCsv(DropLabResult r){var rows=new List<string>{Metadata(r.metadata),"StableId,Name,Weight,MinLevel,Quality,ChancePerKill,AveragePerKill,Per100,Per1000,ExpectedKills"};rows.AddRange(r.currencies.Select(x=>$"{Q(x.stableId)},{Q(x.name)},{x.weight:R},{x.minimumLevel},{x.qualityTier},{x.chancePerKill:R},{x.averagePerKill:R},{x.perHundred:R},{x.perThousand:R},{x.expectedKillsPerDrop:R}"));return string.Join("\n",rows);}
        static string SaveCsv(string name,List<string> rows){Directory.CreateDirectory(Root);string path=$"{Root}/{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";File.WriteAllLines(path,rows);AssetDatabase.Refresh();return path;}
        static string Metadata(ExperimentMetadata m)=>$"# Experiment={Q(m.experimentType)},Timestamp={m.timestampUtc},Seed={m.seed},Samples={m.sampleCount},Git={m.gitCommit},Data={m.dataFingerprint}";
        static string Safe(string v)=>string.Concat((v??"experiment").Select(c=>char.IsLetterOrDigit(c)?c:'_'));
        static string Q(string v)=>"\""+(v??string.Empty).Replace("\"","\"\"")+"\"";
    }
}
