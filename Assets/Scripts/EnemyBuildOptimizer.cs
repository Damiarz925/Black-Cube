// Developer map: Pure enemy-build evaluation and bounded beam selection. Candidate
// generation stays in EnemyAI; this class never rolls or mutates live actor stats.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyBuildOptimizer
{
    // Search tuning. The global fill plus per-archetype reservations never exceeds BeamWidth.
    public const int BeamWidth = 40;
    public const int ArchetypeQuota = 4;

    // Evaluation tuning. These are explicit reference-target assumptions because an
    // enemy's future player target is unknown when its gear is generated.
    public const float CombatHorizonSeconds = 10f;
    public const float ReferenceTargetAttackSpeed = 1f;
    public const float ReferenceElementalResistance = .25f;
    public const float ReferenceAilmentResistance = .25f;
    public const float ReferencePhysicalArmour = 100f;
    public const float ReferenceIncomingHit = 100f;
    public const float OffenseWeight = .75f;
    public const float DefenseWeight = .25f;

    public enum SearchBucket
    {
        Global,
        PhysicalHit,
        FireHit,
        ColdHit,
        LightningHit,
        VoidHit,
        Critical,
        Poison,
        Ignite,
        Bleed,
        Shock,
        Chill
    }

    public sealed class CandidateSlot
    {
        public readonly LootManager.GearType Slot;
        public readonly IReadOnlyList<Gear> Candidates;

        public CandidateSlot(LootManager.GearType slot, IReadOnlyList<Gear> candidates)
        {
            Slot = slot;
            Candidates = candidates ?? Array.Empty<Gear>();
        }
    }

    public readonly struct Evaluation
    {
        public readonly float Offense;
        public readonly float Defense;
        public readonly float Score;
        public readonly float PhysicalHit;
        public readonly float FireHit;
        public readonly float ColdHit;
        public readonly float LightningHit;
        public readonly float VoidHit;
        public readonly float Critical;
        public readonly float Poison;
        public readonly float Ignite;
        public readonly float Bleed;
        public readonly float Shock;
        public readonly float Chill;

        public Evaluation(float offense, float defense, float physicalHit, float fireHit,
            float coldHit, float lightningHit, float voidHit, float critical, float poison, float ignite, float bleed,
            float shock, float chill)
        {
            Offense = Mathf.Max(.0001f, offense);
            Defense = Mathf.Max(.0001f, defense);
            Score = OffenseWeight * Mathf.Log(Offense) + DefenseWeight * Mathf.Log(Defense);
            PhysicalHit = physicalHit;
            FireHit = fireHit;
            ColdHit = coldHit;
            LightningHit = lightningHit;
            VoidHit = voidHit;
            Critical = critical;
            Poison = poison;
            Ignite = ignite;
            Bleed = bleed;
            Shock = shock;
            Chill = chill;
        }

        public float BucketValue(SearchBucket bucket) => bucket switch
        {
            SearchBucket.PhysicalHit => PhysicalHit,
            SearchBucket.FireHit => FireHit,
            SearchBucket.ColdHit => ColdHit,
            SearchBucket.LightningHit => LightningHit,
            SearchBucket.VoidHit => VoidHit,
            SearchBucket.Critical => Critical,
            SearchBucket.Poison => Poison,
            SearchBucket.Ignite => Ignite,
            SearchBucket.Bleed => Bleed,
            SearchBucket.Shock => Shock,
            SearchBucket.Chill => Chill,
            _ => Score
        };
    }

    public sealed class BuildResult
    {
        public readonly IReadOnlyList<Gear> Items;
        public readonly IReadOnlyList<int> CandidateIndices;
        public readonly Evaluation Evaluation;

        internal BuildResult(List<Gear> items, int[] indices, Evaluation evaluation)
        {
            Items = items;
            CandidateIndices = indices;
            Evaluation = evaluation;
        }
    }

    private sealed class SearchState
    {
        public readonly List<Gear> Items;
        public readonly int[] Path;
        public readonly Evaluation Evaluation;

        public SearchState(List<Gear> items, int[] path, Evaluation evaluation)
        {
            Items = items;
            Path = path;
            Evaluation = evaluation;
        }
    }

    private sealed class StatSnapshot
    {
        private readonly float[] raw;

        public StatSnapshot(float[] baseRaw)
        {
            int count = Enum.GetValues(typeof(StatTypes)).Length;
            raw = new float[count];
            if (baseRaw != null)
                Array.Copy(baseRaw, raw, Mathf.Min(baseRaw.Length, raw.Length));
        }

        public void Add(Gear gear)
        {
            if (ReferenceEquals(gear, null) || gear.globalRolledMods == null) return;
            for (int i = 0; i < gear.globalRolledMods.Count; i++)
            {
                RolledMod mod = gear.globalRolledMods[i];
                if (mod == null) continue;
                int index = (int)mod.statType;
                if (index < 0 || index >= raw.Length) continue;
                if (IsIndependentMore(mod.statType))
                    raw[index] = ((1f + raw[index] / 100f) * (1f + mod.value / 100f) - 1f) * 100f;
                else
                    raw[index] += mod.value;
            }
        }

        public float Raw(StatTypes stat)
        {
            int index = (int)stat;
            return index >= 0 && index < raw.Length ? raw[index] : 0f;
        }
        public float Get(StatTypes stat) => StatsComponent.IsPercentStat(stat) ? Raw(stat) / 100f : Raw(stat);
    }

    public static int CandidateCountForLevel(int level)
    {
        int decade = ((Mathf.Max(1, level) - 1) / 10) + 1;
        return Mathf.Clamp(decade, 1, 10);
    }

    public static float[] CaptureBaseStats(StatsComponent stats)
    {
        int count = Enum.GetValues(typeof(StatTypes)).Length;
        float[] values = new float[count];
        if (stats == null) return values;
        for (int i = 0; i < count; i++)
            values[i] = stats.GetRawStat((StatTypes)i);
        return values;
    }

    public static BuildResult SelectBestBuild(IReadOnlyList<CandidateSlot> slots,
        float[] baseRawStats, float fallbackAttackSpeed, float intrinsicDamageFactor = 1f)
    {
        if (slots == null || slots.Count == 0) return null;

        List<SearchState> beam = new List<SearchState> { new SearchState(new List<Gear>(), Array.Empty<int>(), default) };
        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            CandidateSlot slot = slots[slotIndex];
            if (slot == null || slot.Candidates.Count == 0) return null;
            List<SearchState> expanded = new List<SearchState>(beam.Count * slot.Candidates.Count);
            for (int stateIndex = 0; stateIndex < beam.Count; stateIndex++)
            {
                SearchState state = beam[stateIndex];
                for (int candidateIndex = 0; candidateIndex < slot.Candidates.Count; candidateIndex++)
                {
                    Gear candidate = slot.Candidates[candidateIndex];
                    if (ReferenceEquals(candidate, null)) continue;
                    List<Gear> items = new List<Gear>(state.Items.Count + 1);
                    items.AddRange(state.Items);
                    items.Add(candidate);
                    int[] path = new int[state.Path.Length + 1];
                    Array.Copy(state.Path, path, state.Path.Length);
                    path[path.Length - 1] = candidateIndex;
                    expanded.Add(new SearchState(items, path, Evaluate(items, baseRawStats, fallbackAttackSpeed, intrinsicDamageFactor)));
                }
            }
            beam = Prune(expanded);
            if (beam.Count == 0) return null;
        }

        beam.Sort(CompareGlobal);
        SearchState winner = beam[0];
        return new BuildResult(winner.Items, winner.Path, winner.Evaluation);
    }

    public static Evaluation Evaluate(IReadOnlyList<Gear> items, float[] baseRawStats, float fallbackAttackSpeed,
        float intrinsicDamageFactor = 1f)
    {
        StatSnapshot stats = new StatSnapshot(baseRawStats);
        Gear weapon = null;
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Gear item = items[i];
                if (ReferenceEquals(item, null)) continue;
                stats.Add(item);
                if (item.ItemType == LootManager.GearType.Weapons) weapon = item;
            }
        }

        float[] normalPreMitigation = new float[(int)Element.Count];
        if (!ReferenceEquals(weapon, null))
        {
            Element main = weapon.BaseElement == Element.Poison ? Element.Void : weapon.BaseElement;
            normalPreMitigation[(int)main] += ScaleHit(
                weapon.GetEffectiveBaseDamage() + stats.Get(StatMappings.GetFlatDamageStat(main))
                    + AddedAttributeFlat(stats, main), main, stats);
            AddOffElementFlat(normalPreMitigation, Element.Phys, main, stats);
            AddOffElementFlat(normalPreMitigation, Element.Fire, main, stats);
            AddOffElementFlat(normalPreMitigation, Element.Cold, main, stats);
            AddOffElementFlat(normalPreMitigation, Element.Light, main, stats);
            AddOffElementFlat(normalPreMitigation, Element.Void, main, stats);
        }
        for (int i = 0; i < normalPreMitigation.Length; i++)
            normalPreMitigation[i] *= intrinsicDamageFactor;

        float attackSpeed = !ReferenceEquals(weapon, null)
            ? weapon.GetEffectiveAttackSpeed() * (1f + stats.Get(StatTypes.AttackSpeed) + AttributeAttackSpeed(stats))
            : fallbackAttackSpeed * (1f + stats.Get(StatTypes.AttackSpeed) + AttributeAttackSpeed(stats));
        attackSpeed = Mathf.Max(0f, attackSpeed);
        float hitTwiceChance = Mathf.Clamp01(stats.Get(StatTypes.ChanceToHitTwice));
        float hitsPerSecond = attackSpeed * (1f + hitTwiceChance);
        float critChance = !ReferenceEquals(weapon, null)
            ? Mathf.Clamp01(weapon.GetEffectiveBaseCrit(stats.Get(StatTypes.BaseCritChance)) * (1f + stats.Get(StatTypes.CritChance)))
            : 0f;
        float critExtra = CombatCalculator.BaseCriticalMultiplier - 1f
            + Mathf.Max(0f, stats.Get(StatTypes.CritMult));

        float normalFinal = 0f;
        float criticalFinal = 0f;
        float[] elementExpectedDps = new float[(int)Element.Count];
        for (int i = 0; i < normalPreMitigation.Length; i++)
        {
            float normal = MitigateReferenceHit(normalPreMitigation[i], (Element)i, stats);
            float critical = MitigateReferenceHit(normalPreMitigation[i] * (1f + critExtra), (Element)i, stats);
            float expected = Mathf.Lerp(normal, critical, critChance);
            normalFinal += normal;
            criticalFinal += expected;
            elementExpectedDps[i] = expected * hitsPerSecond;
        }
        float hitDps = criticalFinal * hitsPerSecond;
        float critDps = Mathf.Max(0f, (criticalFinal - normalFinal) * hitsPerSecond);

        float poison = AilmentDps(StatusEffects.AilmentKind.Poison, normalPreMitigation, stats, hitsPerSecond, critChance, critExtra);
        float ignite = AilmentDps(StatusEffects.AilmentKind.Ignite, normalPreMitigation, stats, hitsPerSecond, critChance, critExtra);
        float bleed = AilmentDps(StatusEffects.AilmentKind.Bleed, normalPreMitigation, stats, hitsPerSecond, critChance, critExtra);
        float shock = ShockDps(elementExpectedDps[(int)Element.Light], stats);
        float chill = ChillDefenseMultiplier(normalPreMitigation[(int)Element.Cold], stats, hitsPerSecond);
        float offense = hitDps + poison + ignite + bleed + shock;
        float defense = DefensePower(stats, hitsPerSecond) * chill;

        return new Evaluation(offense, defense,
            elementExpectedDps[(int)Element.Phys], elementExpectedDps[(int)Element.Fire],
            elementExpectedDps[(int)Element.Cold], elementExpectedDps[(int)Element.Light],
            elementExpectedDps[(int)Element.Void],
            critDps, poison, ignite, bleed, shock, chill - 1f);
    }

    private static List<SearchState> Prune(List<SearchState> expanded)
    {
        if (expanded.Count <= BeamWidth)
        {
            expanded.Sort(CompareGlobal);
            return expanded;
        }

        List<SearchState> kept = new List<SearchState>(BeamWidth);
        HashSet<string> paths = new HashSet<string>();
        SearchBucket[] archetypes =
        {
            SearchBucket.PhysicalHit, SearchBucket.FireHit, SearchBucket.ColdHit,
            SearchBucket.LightningHit, SearchBucket.VoidHit, SearchBucket.Critical, SearchBucket.Poison,
            SearchBucket.Ignite, SearchBucket.Bleed, SearchBucket.Shock, SearchBucket.Chill
        };
        for (int b = 0; b < archetypes.Length; b++)
        {
            SearchBucket bucket = archetypes[b];
            expanded.Sort((a, c) => CompareBucket(a, c, bucket));
            int added = 0;
            for (int i = 0; i < expanded.Count && added < ArchetypeQuota; i++)
            {
                if (expanded[i].Evaluation.BucketValue(bucket) <= 0f) break;
                if (AddUnique(kept, paths, expanded[i])) added++;
            }
        }

        expanded.Sort(CompareGlobal);
        for (int i = 0; i < expanded.Count && kept.Count < BeamWidth; i++)
            AddUnique(kept, paths, expanded[i]);
        kept.Sort(CompareGlobal);
        return kept;
    }

    private static bool AddUnique(List<SearchState> kept, HashSet<string> paths, SearchState state)
    {
        string key = string.Join(".", state.Path);
        if (!paths.Add(key)) return false;
        kept.Add(state);
        return true;
    }

    private static int CompareGlobal(SearchState a, SearchState b)
    {
        int score = b.Evaluation.Score.CompareTo(a.Evaluation.Score);
        return score != 0 ? score : ComparePath(a.Path, b.Path);
    }

    private static int CompareBucket(SearchState a, SearchState b, SearchBucket bucket)
    {
        int bucketScore = b.Evaluation.BucketValue(bucket).CompareTo(a.Evaluation.BucketValue(bucket));
        if (bucketScore != 0) return bucketScore;
        return CompareGlobal(a, b);
    }

    private static int ComparePath(int[] a, int[] b)
    {
        int count = Mathf.Min(a.Length, b.Length);
        for (int i = 0; i < count; i++)
        {
            int comparison = a[i].CompareTo(b[i]);
            if (comparison != 0) return comparison;
        }
        return a.Length.CompareTo(b.Length);
    }

    private static bool IsIndependentMore(StatTypes stat) =>
        StatsComponent.IsPercentStat(stat) && stat.ToString().EndsWith("Mult", StringComparison.Ordinal);

    private static float ScaleHit(float baseAmount, Element element, StatSnapshot stats)
    {
        if (baseAmount <= 0f) return 0f;
        return CombatCalculator.ScaleOutgoingDamage(baseAmount,
            stats.Get(StatTypes.GenericDmg) + AttributeGlobalDamage(stats),
            stats.Get(StatMappings.GetIncDamageStat(element)) + AttributeElementDamage(stats, element),
            stats.Get(StatTypes.GenericMult), stats.Get(StatMappings.GetMoreDamageStat(element)));
    }

    private static void AddOffElementFlat(float[] hits, Element element, Element weaponElement, StatSnapshot stats)
    {
        if (element == weaponElement) return;
        float flat = stats.Get(StatMappings.GetFlatDamageStat(element)) + AddedAttributeFlat(stats, element);
        if (flat > 0f) hits[(int)element] += ScaleHit(flat, element, stats);
    }

    private static float MitigateReferenceHit(float damage, Element element, StatSnapshot attacker)
    {
        if (damage <= 0f) return 0f;
        if (element == Element.Phys)
        {
            return CombatCalculator.ApplyArmourValue(
                damage, ReferencePhysicalArmour, attacker.Get(StatTypes.PhysPenetration));
        }
        float resistance = ReferenceElementalResistance;
        StatTypes penetration = StatMappings.GetPenetrationStat(element);
        return CombatCalculator.ApplyResistanceValue(damage, resistance, attacker.Get(penetration));
    }

    private static float AilmentDps(StatusEffects.AilmentKind kind, float[] hits, StatSnapshot stats,
        float attacksPerSecond, float critChance, float critExtra)
    {
        StatTypes chanceStat;
        StatTypes increasedStat;
        StatTypes moreStat;
        StatTypes durationStat;
        StatTypes speedStat;
        StatTypes penetrationStat;
        float magnitude;
        int baseTicks;
        int baseInterval;
        int maxStacks;
        float source;
        switch (kind)
        {
            case StatusEffects.AilmentKind.Poison:
                chanceStat = StatTypes.PoisonChance; increasedStat = StatTypes.PoisonDmg;
                moreStat = StatTypes.PoisonMult; durationStat = StatTypes.PoisonDuration;
                speedStat = StatTypes.PoisonTickRate; penetrationStat = StatTypes.PoisonPenetration;
                magnitude = .1f; baseTicks = 2; baseInterval = 4; maxStacks = 100;
                float voidFactor = (1f + stats.Get(StatTypes.VoidDmg)
                    + AttributeElementDamage(stats, Element.Void)) * (1f + stats.Get(StatTypes.VoidMult));
                source = hits[(int)Element.Void];
                source += (hits[(int)Element.Phys] + hits[(int)Element.Fire]
                    + hits[(int)Element.Cold] + hits[(int)Element.Light]) * voidFactor;
                break;
            case StatusEffects.AilmentKind.Ignite:
                chanceStat = StatTypes.IgniteChance; increasedStat = StatTypes.IgniteDmg;
                moreStat = StatTypes.IgniteMult; durationStat = StatTypes.IgniteDuration;
                speedStat = StatTypes.IgniteTickRate; penetrationStat = StatTypes.IgnitePenetration;
                magnitude = .8f; baseTicks = 2; baseInterval = 2; maxStacks = 1;
                source = hits[(int)Element.Fire];
                break;
            default:
                chanceStat = StatTypes.BleedChance; increasedStat = StatTypes.BleedDmg;
                moreStat = StatTypes.BleedMult; durationStat = StatTypes.BleedDuration;
                speedStat = StatTypes.BleedTickRate; penetrationStat = StatTypes.BleedPenetration;
                magnitude = .5f; baseTicks = 2; baseInterval = 1; maxStacks = 4;
                source = hits[(int)Element.Phys];
                break;
        }

        float chance = Mathf.Max(0f, stats.Get(chanceStat)); // intentionally not capped: expected stacks above 100%
        if (kind == StatusEffects.AilmentKind.Poison)
        {
            float applicationResistance = Mathf.Clamp(ReferenceAilmentResistance
                - stats.Get(StatTypes.PoisonPenetration), -.9f, .9f);
            chance *= 1f - applicationResistance;
        }
        if (source <= 0f || chance <= 0f || attacksPerSecond <= 0f) return 0f;
        source *= 1f + critChance * critExtra;
        float perTick = source * magnitude * (1f + stats.Get(increasedStat))
            * (1f + stats.Get(StatTypes.GenericDotMult)) * (1f + AttributeDotMore(stats))
            * (1f + stats.Get(moreStat)) / baseTicks;
        int ticks = Mathf.Max(1, baseTicks + Mathf.RoundToInt(stats.Raw(durationStat)));
        int interval = baseInterval - Mathf.RoundToInt(stats.Raw(speedStat));
        float globalTurnsPerSecond = attacksPerSecond + ReferenceTargetAttackSpeed;
        float ticksPerSecond = interval > 0 ? globalTurnsPerSecond / interval : globalTurnsPerSecond * (1 - interval);
        float lifetime = ticks / Mathf.Max(.0001f, ticksPerSecond);
        float applicationRate = attacksPerSecond * chance;
        float rampWindow = Mathf.Min(lifetime, CombatHorizonSeconds);
        float averageActiveStacks = applicationRate * (rampWindow - rampWindow * rampWindow / (2f * CombatHorizonSeconds));
        averageActiveStacks = Mathf.Clamp(averageActiveStacks, 0f, maxStacks);
        float resistance = kind == StatusEffects.AilmentKind.Poison
            ? Mathf.Min(ReferenceElementalResistance, CombatCalculator.BaseMaximumResistance)
                - stats.Get(StatTypes.VoidPenetration)
            : ReferenceAilmentResistance - stats.Get(penetrationStat);
        resistance = Mathf.Clamp(resistance, -.9f,
            kind == StatusEffects.AilmentKind.Poison ? CombatCalculator.BaseMaximumResistance : .9f);
        return perTick * ticksPerSecond * averageActiveStacks * (1f - resistance);
    }

    private static float DefensePower(StatSnapshot stats, float attacksPerSecond)
    {
        float life = Mathf.Max(1f, (stats.Get(StatTypes.Life)
            + stats.Raw(StatTypes.LifePerStrength) * Attribute(stats, StatTypes.Strength))
            * (1f + stats.Get(StatTypes.LifePercent) + Attribute(stats, StatTypes.Strength) / 1000f));
        float armour = Mathf.Max(0f, stats.Get(StatTypes.FlatArmour) * (1f + stats.Get(StatTypes.ArmourPercent)));
        float physicalMultiplier = CombatCalculator.ApplyArmourValue(ReferenceIncomingHit, armour, 0f) / ReferenceIncomingHit;
        float maxAll = stats.Get(StatTypes.MaxAllRes);
        float fireMax = Mathf.Clamp(CombatCalculator.BaseMaximumResistance + maxAll + stats.Get(StatTypes.MaxFireRes), 0f, CombatCalculator.HardMaximumResistance);
        float coldMax = Mathf.Clamp(CombatCalculator.BaseMaximumResistance + maxAll + stats.Get(StatTypes.MaxColdRes), 0f, CombatCalculator.HardMaximumResistance);
        float lightMax = Mathf.Clamp(CombatCalculator.BaseMaximumResistance + maxAll + stats.Get(StatTypes.MaxLightRes), 0f, CombatCalculator.HardMaximumResistance);
        float voidMax = Mathf.Clamp(CombatCalculator.BaseMaximumResistance + maxAll + stats.Get(StatTypes.MaxVoidRes), 0f, CombatCalculator.HardMaximumResistance);
        float fireMultiplier = 1f - Mathf.Clamp(Mathf.Min(stats.Get(StatTypes.FireRes) + stats.Get(StatTypes.AllRes), fireMax), -.9f, fireMax);
        float coldMultiplier = 1f - Mathf.Clamp(Mathf.Min(stats.Get(StatTypes.ColdRes) + stats.Get(StatTypes.AllRes), coldMax), -.9f, coldMax);
        float lightMultiplier = 1f - Mathf.Clamp(Mathf.Min(stats.Get(StatTypes.LightRes) + stats.Get(StatTypes.AllRes), lightMax), -.9f, lightMax);
        float voidMultiplier = 1f - Mathf.Clamp(Mathf.Min(stats.Get(StatTypes.VoidRes) + stats.Get(StatTypes.AllRes), voidMax), -.9f, voidMax);
        float allAilment = stats.Get(StatTypes.AllAilmentRes);
        float poisonApplicationMultiplier = 1f - Mathf.Clamp(stats.Get(StatTypes.PoisonRes) + allAilment, -.9f, .9f);
        float bleedMultiplier = 1f - Mathf.Clamp(stats.Get(StatTypes.BleedRes) + allAilment, -.9f, .9f);
        float igniteMultiplier = 1f - Mathf.Clamp(stats.Get(StatTypes.IgniteRes) + allAilment, -.9f, .9f);
        // Equal reference exposure keeps unlike defensive units comparable without
        // claiming knowledge of the future player's build.
        float averageTaken = Mathf.Max(.05f, (physicalMultiplier + fireMultiplier + coldMultiplier
            + lightMultiplier + voidMultiplier + poisonApplicationMultiplier * voidMultiplier
            + bleedMultiplier + igniteMultiplier) / 8f);
        float effectiveLife = life / averageTaken;
        float recovery = (Mathf.Max(0f, stats.Get(StatTypes.LifeRegeneration))
            + Mathf.Max(0f, stats.Get(StatTypes.LifeOnHit)) * Mathf.Max(0f, attacksPerSecond))
            * CombatHorizonSeconds;
        return effectiveLife + recovery;
    }

    private static float Attribute(StatSnapshot stats, StatTypes attribute)
    {
        StatTypes percent = attribute switch
        {
            StatTypes.Strength => StatTypes.StrengthPercent,
            StatTypes.Dexterity => StatTypes.DexterityPercent,
            _ => StatTypes.IntelligencePercent
        };
        return DerivedStatCalculator.FinalAttribute(stats.Raw(attribute), stats.Get(percent));
    }

    private static float AttributeGlobalDamage(StatSnapshot stats)
    {
        float strengthGroups = Attribute(stats, StatTypes.Strength) / 10f;
        float lowestGroups = Mathf.Min(Attribute(stats, StatTypes.Strength),
            Mathf.Min(Attribute(stats, StatTypes.Dexterity), Attribute(stats, StatTypes.Intelligence))) / 10f;
        return stats.Get(StatTypes.DamagePerStrength) * strengthGroups
            + stats.Get(StatTypes.DmgPerLowestStat) * lowestGroups;
    }

    private static float AttributeElementDamage(StatSnapshot stats, Element element) =>
        element == Element.Phys ? Attribute(stats, StatTypes.Strength) / 1000f
        : element is Element.Fire or Element.Cold or Element.Light or Element.Void or Element.Poison
            ? Attribute(stats, StatTypes.Intelligence) / 1000f : 0f;

    private static float AttributeAttackSpeed(StatSnapshot stats)
    {
        float dexterity = Attribute(stats, StatTypes.Dexterity);
        return dexterity / 1000f + stats.Get(StatTypes.AttackSpeedPerDexterity) * dexterity;
    }

    private static float AttributeDotMore(StatSnapshot stats) =>
        stats.Get(StatTypes.DoTMultPerIntelligence) * Attribute(stats, StatTypes.Intelligence);

    private static float AddedAttributeFlat(StatSnapshot stats, Element element) => element switch
    {
        Element.Fire => stats.Raw(StatTypes.FlatFirePerStrength) * Attribute(stats, StatTypes.Strength),
        Element.Cold => stats.Raw(StatTypes.FlatColdPerDexterity) * Attribute(stats, StatTypes.Dexterity),
        Element.Light => stats.Raw(StatTypes.FlatLightPerIntelligence) * Attribute(stats, StatTypes.Intelligence),
        _ => 0f
    };

    private static float ShockDps(float lightningDps, StatSnapshot stats)
    {
        if (lightningDps <= 0f) return 0f;
        float chance = Mathf.Max(0f, stats.Get(StatTypes.ShockChance))
            * (1f - Mathf.Clamp(ReferenceAilmentResistance, -.9f, .9f));
        float coefficient = Mathf.Min(1f, .5f * (1f + stats.Get(StatTypes.ShockEffect)));
        return lightningDps * chance / 5f * coefficient;
    }

    private static float ChillDefenseMultiplier(float coldPreMitigation, StatSnapshot stats, float hitsPerSecond)
    {
        float coldDealt = MitigateReferenceHit(coldPreMitigation, Element.Cold, stats);
        if (coldDealt <= 0f) return 1f;
        float chance = Mathf.Max(0f, stats.Get(StatTypes.ChillChance))
            * (1f - Mathf.Clamp(ReferenceAilmentResistance, -.9f, .9f));
        float slow = BattleManager.CalculateChillSlow(coldDealt, 1000f, stats.Get(StatTypes.ChillEffect));
        float duration = Mathf.Max(1f, 4f + stats.Raw(StatTypes.ChillDuration));
        float uptime = Mathf.Clamp01(chance * hitsPerSecond * duration / (duration + 1f));
        return 1f / Mathf.Max(.7f, 1f - slow * uptime);
    }
}
