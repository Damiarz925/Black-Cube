using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class OptimizationConstraints
    {
        public bool minimumLifeEnabled,minimumArmourEnabled,minimumFireResistanceEnabled,minimumColdResistanceEnabled,minimumLightningResistanceEnabled,minimumVoidResistanceEnabled;
        public float minimumLife,minimumArmour,minimumFireResistance,minimumColdResistance,minimumLightningResistance,minimumVoidResistance;
        public List<string> lockedPassiveIds=new(),excludedPassiveIds=new();public List<LootManager.GearType> lockedSlots=new();public int maximumOffClassPoints=-1;
        public bool Accept(PlayerBuildMetrics m)=>m!=null&&(!minimumLifeEnabled||m.life>=minimumLife)&&(!minimumArmourEnabled||m.armour>=minimumArmour)&&(!minimumFireResistanceEnabled||m.fireResistance>=minimumFireResistance)&&(!minimumColdResistanceEnabled||m.coldResistance>=minimumColdResistance)&&(!minimumLightningResistanceEnabled||m.lightningResistance>=minimumLightningResistance)&&(!minimumVoidResistanceEnabled||m.voidResistance>=minimumVoidResistance);
    }

    [Serializable] public sealed class GearCandidateResult{public GearSnapshot item;public double primaryDelta,secondaryDelta,score;}
    [Serializable] public sealed class GearOptimizationDebug{public int generated,retained,statesEvaluated,statesPruned,beamWidth;}
    [Serializable] public sealed class GearOptimizationResult
    {
        public PlayerBuildSnapshot build;public PlayerBuildMetrics baseline,metrics;public double score;public List<GearCandidateResult> selected=new(),candidates=new();public GearOptimizationDebug debug=new();public string profileName,profileGuid;public int profileVersion;
    }

    public static class PlayerGearsetOptimizer
    {
        static readonly LootManager.GearType[] Slots=(LootManager.GearType[])Enum.GetValues(typeof(LootManager.GearType));
        public static GearOptimizationResult Optimize(PlayerBuildSnapshot source,PlayerGearProfileSO profile,OptimizationObjective objective,bool thorough,OptimizationConstraints constraints=null,Action<float> progress=null,Func<bool> cancelled=null)
        {
            if(source==null||profile==null)throw new ArgumentNullException();constraints??=new();var baselineBuild=source.Clone();baselineBuild.equipment??=new();var baseline=PlayerBuildEvaluator.Evaluate(baselineBuild);var result=new GearOptimizationResult{baseline=baseline,profileName=profile.name,profileVersion=profile.version,profileGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile))};var rng=new SeededSimulationRandomSource(source.seed);using var session=new WorkbenchSession();var bySlot=new Dictionary<LootManager.GearType,List<GearCandidateResult>>();int itemLevel=profile.ResolveItemLevel(source.playerLevel,source.combatLevel);
            for(int si=0;si<Slots.Length;si++)
            {
                var slot=Slots[si];if(constraints.lockedSlots.Contains(slot)&&source.equipment.Any(x=>x.slot==slot)){bySlot[slot]=new(){new GearCandidateResult{item=source.equipment.First(x=>x.slot==slot)}};continue;}var list=new List<GearCandidateResult>();for(int i=0;i<Math.Max(1,profile.candidatesPerSlot);i++){if(cancelled?.Invoke()==true)break;var item=Generate(slot,itemLevel,source.weaponTypeId,profile,rng,session);var b=source.Clone();b.equipment.RemoveAll(x=>x.slot==slot);b.equipment.Add(item);var metrics=PlayerBuildEvaluator.Evaluate(b);list.Add(new GearCandidateResult{item=item,primaryDelta=OptimizationMetricCatalog.Get(objective.primary).Value(metrics)-OptimizationMetricCatalog.Get(objective.primary).Value(baseline),secondaryDelta=OptimizationMetricCatalog.Get(objective.secondary).Value(metrics)-OptimizationMetricCatalog.Get(objective.secondary).Value(baseline),score=OptimizationMetricCatalog.Score(metrics,baseline,objective)});result.debug.generated++;}var generated=list.ToList();var ordered=list.OrderByDescending(x=>x.score).ThenBy(x=>x.item.Description,StringComparer.Ordinal).ToList();list=profile.selectionStrategy switch{PlayerGearSelectionStrategy.RandomLegal=>generated.Take(1).ToList(),PlayerGearSelectionStrategy.PercentileTarget=>new(){ordered[Mathf.Clamp(Mathf.RoundToInt((1-profile.targetObjectivePercentile)*(ordered.Count-1)),0,ordered.Count-1)]},_=>ordered.Take(Math.Max(1,profile.candidateRetention)).ToList()};result.debug.retained+=list.Count;bySlot[slot]=list;result.candidates.AddRange(ordered);progress?.Invoke((si+1f)/(Slots.Length+1));}
            var states=new List<List<GearSnapshot>>{source.equipment.Where(x=>constraints.lockedSlots.Contains(x.slot)).ToList()};int beam=thorough||profile.selectionStrategy==PlayerGearSelectionStrategy.FullGearsetBeamSearch?Math.Max(1,profile.gearsetBeamWidth):1;result.debug.beamWidth=beam;
            for(int si=0;si<Slots.Length&&cancelled?.Invoke()!=true;si++)
            {
                var slot=Slots[si];var expanded=new List<(List<GearSnapshot> gear,double score,string key)>();foreach(var state in states)foreach(var c in bySlot[slot]){var next=new List<GearSnapshot>(state);next.RemoveAll(x=>x.slot==slot);next.Add(c.item);var b=source.Clone();b.equipment=next;var m=PlayerBuildEvaluator.Evaluate(b);result.debug.statesEvaluated++;if(!constraints.Accept(m))continue;expanded.Add((next,OptimizationMetricCatalog.Score(m,baseline,objective),Signature(next)));}states=expanded.GroupBy(x=>x.key).Select(x=>x.OrderByDescending(y=>y.score).First()).OrderByDescending(x=>x.score).ThenBy(x=>x.key,StringComparer.Ordinal).Take(beam).Select(x=>x.gear).ToList();result.debug.statesPruned+=Math.Max(0,expanded.Count-states.Count);if(states.Count==0)states.Add(new List<GearSnapshot>());progress?.Invoke((si+1f)/Slots.Length);}
            result.build=source.Clone();result.build.equipment=states[0];result.build.gearProfileGuid=result.profileGuid;result.build.gearProfileVersion=profile.version;result.build.dataFingerprint=ProductionBalanceAdapters.DataFingerprint();result.metrics=PlayerBuildEvaluator.Evaluate(result.build);result.score=OptimizationMetricCatalog.Score(result.metrics,baseline,objective);
            foreach(var item in result.build.equipment){var without=result.build.Clone();without.equipment.RemoveAll(x=>x.slot==item.slot);var wm=PlayerBuildEvaluator.Evaluate(without);result.selected.Add(new GearCandidateResult{item=item,primaryDelta=OptimizationMetricCatalog.Get(objective.primary).Value(result.metrics)-OptimizationMetricCatalog.Get(objective.primary).Value(wm),secondaryDelta=OptimizationMetricCatalog.Get(objective.secondary).Value(result.metrics)-OptimizationMetricCatalog.Get(objective.secondary).Value(wm),score=result.score-OptimizationMetricCatalog.Score(wm,baseline,objective)});}return result;
        }
        static GearSnapshot Generate(LootManager.GearType slot,int itemLevel,string weapon,PlayerGearProfileSO p,ILootRandomSource rng,WorkbenchSession session)
        {
            for(int outer=0;outer<128;outer++)
            {
                LootManager.GearRarity rarity=p.useNaturalRarity?NaturalRarity(itemLevel,rng):(LootManager.GearRarity)rng.Range((int)p.minimumRarity,(int)p.maximumRarity+1);if(!p.allowNaturalLegendaries&&rarity==LootManager.GearRarity.Legendary)rarity=LootManager.GearRarity.Rare;Element element=slot==LootManager.GearType.Weapons?RandomElement(rng):Element.Phys;var g=session.ReusableItem;g.Initialize(slot,rarity,itemLevel,element,slot==LootManager.GearType.Weapons?weapon:null);List<RolledMod> mods=null;for(int attempt=0;attempt<64&&mods==null;attempt++)mods=session.Roller.RollEquipmentModsForItem(slot,rarity,itemLevel,element,g.WeaponTypeId,rng);if(mods==null)continue;if(!p.allowEmpoweredModifiers&&mods.Any(x=>x.isEmpowered)||!p.allowBossSpecialModifiers&&mods.Any(x=>x.isBossSpecial))continue;g.ApplyMods(mods);if(slot==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(g);return GearSnapshot.Capture(g);
            }throw new InvalidOperationException("Production item generation exhausted its bounded retry budget.");
        }
        static LootManager.GearRarity NaturalRarity(int level,ILootRandomSource rng){Vector4 r=LootManager.RarityRatesForLevel(level);float x=rng.Value()*100;return x<r.x?LootManager.GearRarity.Normal:x<r.x+r.y?LootManager.GearRarity.Magic:x<r.x+r.y+r.z?LootManager.GearRarity.Rare:LootManager.GearRarity.Legendary;}static Element RandomElement(ILootRandomSource r){int x=r.Range(0,5);return x<(int)Element.Poison?(Element)x:Element.Void;}static string Signature(IEnumerable<GearSnapshot> gear)=>string.Join("|",gear.OrderBy(x=>x.slot).Select(x=>$"{x.slot}:{x.rarity}:{string.Join(",",x.mods.Select(m=>$"{(int)m.stat}:{m.tier}:{m.value:R}"))}"));
    }

    [Serializable] public sealed class PassivePointResult{public int point,nodeId;public string stableId,name,branch,effect,alternative;public double primaryDelta,secondaryDelta,scoreDelta,scorePercent;}
    [Serializable] public sealed class PassiveSearchDebug{public int depth,candidateStates,uniqueStates,retainedStates,diversityGroups,statesEvaluated;public double bestScore;}
    [Serializable] public sealed class PassiveOptimizationResult
    {public PlayerBuildSnapshot build;public PlayerBuildMetrics baseline,metrics;public double score;public List<PassivePointResult> sequence=new();public List<PassiveSearchDebug> debug=new();public string algorithm;}
    [Serializable] public sealed class PassiveMarginalResult{public int nodeId,pathCost;public string stableId,name,branch,effect;public bool immediatelyLegal;public double primaryDelta,secondaryDelta,objectiveDelta,pathAdjustedValue,totalPackageGain;}

    public static class PassiveTreeOptimizer
    {
        static readonly ProfilerMarker SearchMarker=new("Workbench.PassiveSearch");
        static readonly ProfilerMarker LegalMarker=new("Workbench.PassiveLegalNext");
        static readonly ProfilerMarker EvaluationMarker=new("Workbench.PassiveBuildEvaluation");
        static readonly ProfilerMarker RetainMarker=new("Workbench.PassiveDiverseRetain");
        // Allocation is monotone: adding one node to a valid state can only violate that
        // node's prerequisite/choice rule or the explicit off-class budget. Keep the
        // authoritative full validator for the initial and final states.
        static readonly PassiveNodeDefinition[] compiledNodes=PassiveTreeDefinition.Nodes.ToArray();
        static readonly Dictionary<string,int[]> choiceGroups=compiledNodes.Where(n=>n.IsChoice).GroupBy(n=>n.ChoiceGroupId).ToDictionary(g=>g.Key,g=>g.Select(n=>n.Id).ToArray());
        sealed class State{public int[] ranks;public string bits,Key;public List<int> nodeIds;public List<string> ids;public double score;public List<PassivePointResult> sequence;public PlayerBuildMetrics metrics;}
        static string Bits(int[] ranks){var chars=new char[(ranks.Length+15)/16];for(int i=0;i<ranks.Length;i++)if(ranks[i]!=0)chars[i>>4]=(char)(chars[i>>4]|1<<(i&15));return new string(chars);}
        static string WithBit(string parent,int node){var chars=parent.ToCharArray();chars[node>>4]=(char)(chars[node>>4]|1<<(node&15));return new string(chars);}
        public static PassiveOptimizationResult Optimize(PlayerBuildSnapshot source,int points,OptimizationObjective objective,bool beamSearch,int beamWidth=500,OptimizationConstraints constraints=null,Action<float> progress=null,Func<bool> cancelled=null,bool dynamicDefense=false)
        {
            using var searchSample=SearchMarker.Auto();
            constraints??=new();points=Mathf.Clamp(points,0,100);var initial=source.Clone();foreach(string id in constraints.lockedPassiveIds)if(!initial.passiveStableIds.Contains(id))initial.passiveStableIds.Add(id);using var evaluation=new PlayerBuildEvaluation(initial);var baseline=evaluation.Metrics;var firstRanks=initial.AllocationRanks();if(!PlayerProgression.ValidateAllocationState(firstRanks,initial.classId,initial.subclassId))throw new InvalidOperationException("Initial/locked passive allocation is not production-legal.");var firstNodeIds=Enumerable.Range(0,firstRanks.Length).Where(i=>firstRanks[i]!=0).ToList();var initialScore=dynamicDefense?RealisticGearsetOptimizer.Score(baseline,baseline,objective,constraints):0;var states=new List<State>{new(){ranks=firstRanks,bits=Bits(firstRanks),nodeIds=firstNodeIds,Key=string.Join(",",firstNodeIds),ids=Ids(firstRanks),metrics=baseline,sequence=new(),score=initialScore}};var output=new PassiveOptimizationResult{baseline=baseline,algorithm=beamSearch?$"Beam {beamWidth}":"Greedy"};int target=Math.Max(firstRanks.Sum(),points);
            var progressClock=System.Diagnostics.Stopwatch.StartNew();long lastProgress=0;bool stopped=false;var candidateBuild=initial.Clone();
            for(int depth=firstRanks.Sum();depth<target&&cancelled?.Invoke()!=true;depth++)
            {
                var expanded=new List<State>();var seen=new HashSet<string>(StringComparer.Ordinal);int candidates=0,processed=0;
                foreach(var state in states)
                {
                    foreach(int node in LegalNext(state.ranks,initial.classId,initial.subclassId,constraints))
                    {
                        candidates++;string bits=WithBit(state.bits,node);if(!seen.Add(bits))continue;
                        var ranks=(int[])state.ranks.Clone();ranks[node]=1;var n=compiledNodes[node];var ids=new List<string>(state.ids){n.StableId};var nodeIds=new List<int>(state.nodeIds);int insertion=nodeIds.BinarySearch(node);nodeIds.Insert(insertion<0?~insertion:insertion,node);candidateBuild.passiveStableIds=ids;PlayerBuildMetrics metrics;using(EvaluationMarker.Auto())metrics=evaluation.ReevaluatePassives(candidateBuild);double score=dynamicDefense?RealisticGearsetOptimizer.Score(metrics,baseline,objective,constraints):OptimizationMetricCatalog.Score(metrics,baseline,objective);
                        var step=new PassivePointResult{point=depth+1,nodeId=node,stableId=n.StableId,name=n.DisplayName,branch=n.IsWeaponRoute?n.RouteWeaponId:n.RouteClassId,effect=n.Description,primaryDelta=OptimizationMetricCatalog.Get(objective.primary).Value(metrics)-OptimizationMetricCatalog.Get(objective.primary).Value(state.metrics),secondaryDelta=OptimizationMetricCatalog.Get(objective.secondary).Value(metrics)-OptimizationMetricCatalog.Get(objective.secondary).Value(state.metrics),scoreDelta=score-state.score,scorePercent=state.score==0?score*100:(score-state.score)/Math.Max(.000001,Math.Abs(state.score))*100};
                        expanded.Add(new State{ranks=ranks,bits=bits,nodeIds=nodeIds,Key=string.Join(",",nodeIds),ids=ids,metrics=metrics,score=score,sequence=new List<PassivePointResult>(state.sequence){step}});
                        if((++processed&31)==0&&progress!=null&&progressClock.ElapsedMilliseconds-lastProgress>=100){lastProgress=progressClock.ElapsedMilliseconds;progress((depth+(float)processed/Math.Max(1,states.Count*32))/Math.Max(1,target));if(cancelled?.Invoke()==true){stopped=true;break;}}
                    }
                    if(stopped)break;
                }
                if(stopped)break;
                int keep=beamSearch?Math.Max(1,beamWidth):1;List<State> retained;using(RetainMarker.Auto())retained=DiverseRetain(expanded,keep);output.debug.Add(new PassiveSearchDebug{depth=depth+1,candidateStates=candidates,uniqueStates=expanded.Count,retainedStates=retained.Count,diversityGroups=retained.Select(x=>Diversity(x.ranks)).Distinct().Count(),statesEvaluated=expanded.Count,bestScore=retained.Count>0?retained.Max(x=>x.score):0});states=retained;if(states.Count==0)break;progress?.Invoke((depth+1f)/Math.Max(1,target));
            }
            var best=states.OrderByDescending(x=>x.score).ThenBy(x=>x.Key,StringComparer.Ordinal).First();output.build=initial.Clone();output.build.passiveStableIds=Ids(best.ranks);if(!PlayerProgression.ValidateAllocationState(best.ranks,initial.classId,initial.subclassId))throw new InvalidOperationException("Optimized passive allocation failed production validation.");output.metrics=best.metrics;output.score=best.score;output.sequence=best.sequence;return output;
        }
        public static List<int> LegalNext(int[] ranks,string classId,string subclassId,OptimizationConstraints constraints=null)
        {
            using var legalSample=LegalMarker.Auto();
            constraints??=new();var excluded=new HashSet<string>(constraints.excludedPassiveIds??new());var result=new List<int>();
            bool Allocated(int id)=>id>=0&&id<ranks.Length&&ranks[id]!=0;
            var complete=PassiveTreeDefinition.ClassIds.ToDictionary(c=>c,c=>PassiveTreeDefinition.IsClassSpineComplete(c,Allocated));
            bool nativeComplete=complete.GetValueOrDefault(classId);
            int offClass=0;if(constraints.maximumOffClassPoints>=0)foreach(var n in compiledNodes)if(Allocated(n.Id)&&OffClass(n,classId))offClass++;
            foreach(var n in compiledNodes)
            {
                if(Allocated(n.Id)||excluded.Contains(n.StableId)||constraints.maximumOffClassPoints>=0&&offClass+(OffClass(n,classId)?1:0)>constraints.maximumOffClassPoints)continue;
                if(n.IsChoice)
                {
                    if(!Allocated(n.PrerequisiteId)||n.IsSubclassChoice&&(n.RouteClassId!=classId||string.IsNullOrEmpty(subclassId)))continue;
                    if(choiceGroups.TryGetValue(n.ChoiceGroupId,out var group)&&group.Any(Allocated))continue;
                }
                else if(n.Kind==PassiveNodeKind.Spine)
                {
                    if(n.Tier==1){if(n.RouteClassId!=classId&&!nativeComplete)continue;}
                    else if(!Allocated(PassiveTreeDefinition.ClassSpineNode(n.RouteClassId,n.Tier-1)))continue;
                }
                else if(n.Kind==PassiveNodeKind.WeaponSpine)
                {
                    if(n.Tier==1){if(!complete.GetValueOrDefault(PassiveTreeDefinition.WeaponClass(n.RouteWeaponId)))continue;}
                    else if(!Allocated(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,n.Tier-1)))continue;
                }
                else continue;
                result.Add(n.Id);
            }
            return result;
        }
        static bool OffClass(PassiveNodeDefinition n,string classId)=>n.IsClassRoute&&n.RouteClassId!=classId||n.IsWeaponRoute&&PassiveTreeDefinition.WeaponClass(n.RouteWeaponId)!=classId;
        public static List<PassiveMarginalResult> Analyze(PlayerBuildSnapshot build,OptimizationObjective objective,OptimizationConstraints constraints=null,bool includeLocked=true)
        {
            constraints??=new();var baseline=PlayerBuildEvaluator.Evaluate(build);var ranks=build.AllocationRanks();var results=new List<PassiveMarginalResult>();foreach(var n in PassiveTreeDefinition.Nodes){if(ranks[n.Id]!=0||constraints.excludedPassiveIds.Contains(n.StableId))continue;var package=MinimumLegalPackage(ranks,n.Id,build.classId,build.subclassId);if(package==null||(package.Count>1&&!includeLocked))continue;var b=build.Clone();var next=(int[])ranks.Clone();foreach(int x in package)next[x]=1;b.passiveStableIds=Ids(next);var m=PlayerBuildEvaluator.Evaluate(b);double score=OptimizationMetricCatalog.Score(m,baseline,objective);results.Add(new PassiveMarginalResult{nodeId=n.Id,stableId=n.StableId,name=n.DisplayName,branch=n.IsWeaponRoute?n.RouteWeaponId:n.RouteClassId,effect=n.Description,immediatelyLegal=package.Count==1,pathCost=package.Count,primaryDelta=OptimizationMetricCatalog.Get(objective.primary).Value(m)-OptimizationMetricCatalog.Get(objective.primary).Value(baseline),secondaryDelta=OptimizationMetricCatalog.Get(objective.secondary).Value(m)-OptimizationMetricCatalog.Get(objective.secondary).Value(baseline),objectiveDelta=score,totalPackageGain=score,pathAdjustedValue=score/package.Count});}return results.OrderByDescending(x=>x.pathAdjustedValue).ThenBy(x=>x.stableId,StringComparer.Ordinal).ToList();
        }
        public static List<int> MinimumLegalPackage(int[] current,int target,string classId,string subclassId)
        {
            if(target<0||target>=current.Length||current[target]!=0)return new();var next=(int[])current.Clone();var add=new HashSet<int>();void AddClass(string c,int tier){if(c!=classId&&!PassiveTreeDefinition.IsClassSpineComplete(classId,i=>next[i]!=0||add.Contains(i)))for(int t=1;t<=10;t++)add.Add(PassiveTreeDefinition.ClassSpineNode(classId,t));for(int t=1;t<=tier;t++)add.Add(PassiveTreeDefinition.ClassSpineNode(c,t));}
            var n=PassiveTreeDefinition.Node(target);if(n.IsClassRoute)AddClass(n.RouteClassId,n.Tier);if(n.IsWeaponRoute){string owner=PassiveTreeDefinition.WeaponClass(n.RouteWeaponId);AddClass(owner,10);for(int t=1;t<=n.Tier;t++)add.Add(PassiveTreeDefinition.WeaponSpineNode(n.RouteWeaponId,t));}add.Add(target);foreach(int id in add)next[id]=1;if(!PlayerProgression.ValidateAllocationState(next,classId,subclassId))return null;return add.Where(x=>current[x]==0).OrderBy(x=>PassiveTreeDefinition.Node(x).Tier).ThenBy(x=>x).ToList();
        }
        static List<State> DiverseRetain(List<State> states,int width){var ordered=states.OrderByDescending(x=>x.score).ThenBy(x=>x.Key,StringComparer.Ordinal).ToList();if(ordered.Count<=width)return ordered;var result=new List<State>();foreach(var group in ordered.GroupBy(x=>Diversity(x.ranks)).OrderByDescending(g=>g.Max(x=>x.score)))if(result.Count<width)result.Add(group.First());foreach(var s in ordered)if(result.Count<width&&!result.Contains(s))result.Add(s);return result;}
        static string Diversity(int[] ranks){var classes=PassiveTreeDefinition.ClassIds.Select(c=>Enumerable.Range(1,10).Where(t=>ranks[PassiveTreeDefinition.ClassSpineNode(c,t)]!=0).DefaultIfEmpty(0).Max());var weapons=PassiveTreeDefinition.WeaponIds.Select(w=>Enumerable.Range(1,5).Where(t=>ranks[PassiveTreeDefinition.WeaponSpineNode(w,t)]!=0).DefaultIfEmpty(0).Max());return string.Join(".",classes.Concat(weapons));}static List<string> Ids(int[] ranks)=>Enumerable.Range(0,ranks.Length).Where(i=>ranks[i]!=0).Select(i=>PassiveTreeDefinition.Node(i).StableId).ToList();
    }

    [Serializable] public sealed class PlayerCurvePoint
    {
        public int playerLevel,combatLevel,profileVersion,passiveBeamWidth,samplesPerLevel=1;
        public string profile,profileGuid,dataFingerprint,resultId,buildHash,gearHash,
            passiveHash,evaluationHash,passiveAlgorithm;
        public PlayerBuildSnapshot build;
        public PlayerBuildMetrics metrics;
        public OptimizationObjective objective;
        public double objectiveScore,passiveScore;
        public long seed;
        public bool optimizePassives,progressive,freeRespec;
    }
    [Serializable] public sealed class ScenarioSweepResult
    {
        public List<PlayerCurvePoint> points=new();
        public string dataFingerprint,requestSignature;
        public CombatLevelSweepPolicy combatLevelPolicy;
        public int combatLevelOffset,start,end,step;
        public bool optimizePassives,progressive,freeRespec;
        public List<CurveSeries> Curves(string metricId)
        {
            var groups=points.GroupBy(x=>x.profile);
            return groups.Select((g,i)=>new CurveSeries
            {
                name=g.Key,color=Color.HSVToRGB((i*.23f)%1,.7f,.95f),
                points=g.OrderBy(x=>x.playerLevel).Select(x=>new CurvePoint
                {x=x.playerLevel,mean=OptimizationMetricCatalog.Get(metricId).Value(x.metrics)}).ToList()
            }).ToList();
        }
    }
    public static class PlayerScenarioSweep
    {
        public static string RequestSignature(PlayerBuildSnapshot template,
            IEnumerable<PlayerGearProfileSO> profiles,OptimizationObjective objective,
            int start,int end,int step,bool optimizePassives,bool progressive,bool freeRespec,
            int passiveBeamWidth,CombatLevelSweepPolicy policy,int offset)
        {
            string profileData=string.Join("|",profiles.Where(x=>x!=null).Select(x=>
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x))+":"+
                JsonUtility.ToJson(x)));
            return ScenarioResultIdentity.Hash(string.Join("|",JsonUtility.ToJson(template),
                profileData,JsonUtility.ToJson(objective),start,end,step,optimizePassives,
                progressive,freeRespec,passiveBeamWidth,policy,offset));
        }

        public static ScenarioSweepResult Run(PlayerBuildSnapshot template,
            IEnumerable<PlayerGearProfileSO> profiles,OptimizationObjective objective,
            int start,int end,int step,bool optimizePassives,bool progressive,bool freeRespec,
            int passiveBeamWidth=100,Action<float> progress=null,Func<bool> cancelled=null,
            CombatLevelSweepPolicy combatLevelPolicy=CombatLevelSweepPolicy.MatchPlayerLevel,
            int combatLevelOffset=0,Action<string,float> detailedProgress=null)
        {
            if(template==null||profiles==null||objective==null)
                throw new ArgumentNullException("Scenario template, profiles, and objective are required.");
            if(start<1||end>100||end<start||step<=0)
                throw new ArgumentOutOfRangeException(nameof(step),"Use Player Levels 1–100, Start <= End, and Step > 0.");
            var list=profiles.Where(x=>x!=null).ToList();
            if(list.Count==0)throw new InvalidOperationException("Select at least one gear profile.");
            var result=new ScenarioSweepResult
            {
                dataFingerprint=ProductionBalanceAdapters.DataFingerprint(),
                combatLevelPolicy=combatLevelPolicy,combatLevelOffset=combatLevelOffset,
                start=start,end=end,step=step,optimizePassives=optimizePassives,
                progressive=progressive,freeRespec=freeRespec,
                requestSignature=RequestSignature(template,list,objective,start,end,step,
                    optimizePassives,progressive,freeRespec,passiveBeamWidth,
                    combatLevelPolicy,combatLevelOffset)
            };
            int total=list.Count*((end-start)/step+1),done=0;
            foreach(var profile in list)
            {
                PlayerBuildSnapshot previous=null;
                for(int level=start;level<=end;level+=step)
                {
                    if(cancelled?.Invoke()==true)throw new OperationCanceledException(
                        "Scenario Sweep cancelled; no partial result was published.");
                    float Fraction(float phase)=>Mathf.Clamp01((done+phase)/total);
                    void Phase(string name,float phase)
                    {
                        float fraction=Fraction(phase);
                        detailedProgress?.Invoke($"{profile.name} — L{level} / CL{ScenarioLevelPolicy.CombatLevel(combatLevelPolicy,level,start,template.combatLevel,combatLevelOffset)} — {name} — sample 1/1",fraction);
                        progress?.Invoke(fraction);
                    }
                    var build=progressive&&previous!=null?previous.Clone():template.Clone();
                    build.playerLevel=level;
                    build.combatLevel=ScenarioLevelPolicy.CombatLevel(combatLevelPolicy,level,
                        start,template.combatLevel,combatLevelOffset);
                    build.seed=template.seed+level*7919+profile.version*101;
                    Phase("gear",0);
                    var gear=PlayerGearsetOptimizer.Optimize(build,profile,objective,
                        profile.selectionStrategy==PlayerGearSelectionStrategy.FullGearsetBeamSearch,
                        null,p=>Phase("gear",p*.55f),cancelled);
                    if(cancelled?.Invoke()==true)throw new OperationCanceledException(
                        "Scenario Sweep cancelled during gear generation; no partial result was published.");
                    build=gear.build;
                    PassiveOptimizationResult passive=null;
                    if(optimizePassives)
                    {
                        if(freeRespec||!progressive)build.passiveStableIds.Clear();
                        Phase("passives",.55f);
                        passive=PassiveTreeOptimizer.Optimize(build,level,objective,
                            passiveBeamWidth>1,passiveBeamWidth,null,
                            p=>Phase("passives",.55f+p*.4f),cancelled);
                        if(cancelled?.Invoke()==true)throw new OperationCanceledException(
                            "Scenario Sweep cancelled during passive search; no partial result was published.");
                        build=passive.build;
                    }
                    Phase("evaluation",.95f);
                    var metrics=PlayerBuildEvaluator.Evaluate(build);
                    var point=new PlayerCurvePoint
                    {
                        playerLevel=level,combatLevel=build.combatLevel,
                        profile=profile.name,profileGuid=gear.profileGuid,
                        profileVersion=gear.profileVersion,dataFingerprint=result.dataFingerprint,
                        build=build.Clone(),metrics=metrics.Clone(),
                        objective=JsonUtility.FromJson<OptimizationObjective>(JsonUtility.ToJson(objective)),
                        objectiveScore=OptimizationMetricCatalog.Score(metrics,gear.baseline,objective),
                        passiveScore=passive?.score??0,passiveAlgorithm=passive?.algorithm??"None",
                        passiveBeamWidth=optimizePassives?passiveBeamWidth:0,
                        seed=build.seed,optimizePassives=optimizePassives,
                        progressive=progressive,freeRespec=freeRespec
                    };
                    ScenarioResultIdentity.Stamp(point);
                    result.points.Add(point);
                    previous=build.Clone();
                    done++;
                    Phase("complete",0);
                }
            }
            return result;
        }
        public static double AnalyticalTtk(PlayerBuildMetrics player,EnemySample enemy)=>enemy==null||player==null||player.basicDps<=0?double.PositiveInfinity:enemy.life/player.basicDps;public static double AnalyticalTtd(PlayerBuildMetrics player,EnemySample enemy)=>enemy==null||player==null||enemy.dps<=0?double.PositiveInfinity:player.life/enemy.dps;
    }
}
