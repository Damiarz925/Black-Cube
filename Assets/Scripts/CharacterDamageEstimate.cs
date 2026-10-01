using System.Linq;
using UnityEngine;

// Cheap neutral-target basic-attack estimate. No fights, RNG, skills scheduler,
// transient opening windows or uncapped ailment-stack simulation per item cell.
public static class CharacterDamageEstimate
{
    public static DamageContext SearchHit(DamageContext source,StatsComponent stats)
    {
        var keys=stats.GetComponent<PassiveKeystoneState>();var result=new DamageContext(source.Hits.Count);
        float more=UniqueCatalog.HitMultiplier(stats)*(keys?.GenericMoreMultiplier??1)*CombatCalculator.ScopedDamageMultiplier(source.Scopes,stats);
        if(keys?.Has(PassiveKeystone.ThiefOpener)==true)more*=ClassKeystoneMechanics.TargetLifeMultiplier(false);
        foreach(var hit in source.Hits)
        {
            Element element=CombatCalculator.ResolvedElement(hit.Element,stats);
            if(keys?.Has(PassiveKeystone.MageFire)==true&&element!=Element.Fire)continue;
            result.AddDamage(element,hit.Amount*more*(stats.GetComponent<SubclassCombatState>()?.AuraDamageMultiplier(hit.Element)??1)
                *(keys?.Has(PassiveKeystone.MageFire)==true?PassiveKeystoneState.Value(PassiveKeystone.MageFire):1));
        }
        return result;
    }
    public static float ExpectedDirectFactor(float critChance,float critMultiplier,float multistrikeChance,bool consolidated,float extraStrikeMultiplier=1)
        => (1+Mathf.Clamp01(critChance)*(critMultiplier-1))*(1+Mathf.Clamp01(multistrikeChance)*(consolidated?1.10f:1)*extraStrikeMultiplier);
    public static float AilmentMagnitude(StatsComponent stats,Element element,float eligibleHit)
    {
        var increased=element==Element.Phys?StatTypes.BleedDmg:element==Element.Fire?StatTypes.IgniteDmg:StatTypes.PoisonDmg;
        var more=element==Element.Phys?StatTypes.BleedMult:element==Element.Fire?StatTypes.IgniteMult:StatTypes.PoisonMult;
        return eligibleHit*(element==Element.Phys?.20f:element==Element.Fire?.50f:.05f)*(1+stats.GetStat(increased))*(1+stats.GetStat(more))*(1+stats.GetStat(StatTypes.GenericDotMult))*UniqueCatalog.AilmentMultiplier(stats);
    }
    public static float AilmentDurationFactor(StatsComponent stats,Element element)
    {
        var kind=element==Element.Phys?StatusEffects.AilmentKind.Bleed:element==Element.Fire?StatusEffects.AilmentKind.Ignite:StatusEffects.AilmentKind.Poison;
        AilmentTimingRules.Timing(kind,stats,out float duration,out float interval);
        return AilmentTimingRules.TickCount(duration,interval)/(float)AilmentTimingRules.BaseTicks(kind);
    }
    public static float Calculate(PlayerController player,bool includeDots=true)
    {
        if(player==null)return 0;
        var stats=player.GetComponent<StatsComponent>();var keys=player.GetComponent<PassiveKeystoneState>();
        var basic=player.BuildNonCriticalAttackContext();
        bool bow=player.EquippedWeapon?.WeaponTypeId==WeaponTypeIds.Bow;
        bool projectile=player.EquippedWeapon!=null&&WeaponTypeCatalog.Get(player.EquippedWeapon.WeaponTypeId).IsRanged;
        if(projectile)basic.Scopes|=DamageScope.Projectile;
        var context=SearchHit(basic,stats);float speed=player.GetFinalAttackSpeed();
        float repeats=GenericPassiveMechanics.SupportsMultistrike(player.EquippedWeapon?.WeaponTypeId)?stats.GetStat(StatTypes.ChanceToHitTwice):0;
        float factor=ExpectedDirectFactor(player.GetFinalCritChance(),CombatCalculator.BaseCriticalMultiplier+stats.GetStat(StatTypes.CritMult),repeats,keys?.Has(PassiveKeystone.WarriorConsolidation)==true,1+UniqueCatalog.Power(stats,UniquePower.MultistrikeDamage));
        if(projectile)
        {
            int count=BattleManager.CalculateProjectileCount(stats.GetRawStat(StatTypes.ProjectileAmount),0);
            float projectileFactor=count;
            var subclass=player.GetComponent<SubclassCombatState>();
            if(subclass!=null)projectileFactor=subclass.FinalProjectileCount(count,out float more)==1?more:count;
            factor*=projectileFactor;
            if(bow)
            {
                float precision=WeaponMechanicProfile.PrecisionMultiplier(stats.GetStat(StatTypes.ProjectilePrecisionMultiplier));
                factor*=keys?.Has(PassiveKeystone.RangerPrecision)==true?.85f*precision*1.15f:
                    1+WeaponMechanicProfile.PrecisionChance(stats.GetStat(StatTypes.ProjectilePrecisionChance))*(precision-1);
                if(keys?.Has(PassiveKeystone.RangerSplit)==true)factor*=3*.33f*1.15f;
            }
        }
        float total=0;
        foreach(var hit in context.Hits)
        {
            float direct=hit.Amount;
            total+=direct*factor*speed;
            if(!includeDots)continue;
            StatTypes chance=hit.Element switch{Element.Phys=>StatTypes.BleedChance,Element.Fire=>StatTypes.IgniteChance,Element.Void=>StatTypes.PoisonChance,_=>StatTypes.UnarmedDamage};
            if(chance==StatTypes.UnarmedDamage)continue;
            total+=AilmentMagnitude(stats,hit.Element,direct*factor)*speed*Mathf.Max(0,stats.GetStat(chance))*AilmentDurationFactor(stats,hit.Element);
        }
        return Mathf.Max(0,total);
    }
    public static float Replacement(PlayerController live,Gear candidate)
    {
        if(live==null||candidate==null)return 0;
        var actor=new GameObject("Estimated DPS preview");actor.SetActive(false);actor.hideFlags=HideFlags.HideAndDontSave;
        try
        {
            var stats=actor.AddComponent<StatsComponent>();var player=actor.AddComponent<PlayerController>();
            var unique=actor.AddComponent<UniqueLoadoutState>();
            if(EquipmentManager.Instance!=null)
                foreach(var item in EquipmentManager.Instance.EquippedItems.Values)
                    if(item!=null&&item.ItemType!=candidate.ItemType&&item.UniqueData!=null)
                        unique.powers.AddRange(item.UniqueData.powers.Select(x=>x.Copy()));
            if(candidate.UniqueData!=null)unique.powers.AddRange(candidate.UniqueData.powers.Select(x=>x.Copy()));
            if(RelicInventory.Instance!=null)
                foreach(var relic in Enumerable.Range(0,RelicInventory.ActiveSlotCount).Select(RelicInventory.Instance.Active).Where(x=>x!=null))
                    if(relic.uniqueRelic)unique.powers.AddRange(relic.forgedPowers.Select(x=>x.Copy()));
            // Preserve current aura intensity without ticking or mutating live combat.
            actor.AddComponent<SubclassCombatState>().CopyPreviewFrom(live.GetComponent<SubclassCombatState>());
            var progression=GameManager.Instance?.GetComponent<PlayerProgression>()??live.GetComponent<PlayerProgression>();
            live.GetComponent<StatsComponent>().CopyToPreview(stats,EquipmentManager.Instance?.GetEquipped(candidate.ItemType),progression);
            foreach(var mod in candidate.globalRolledMods)stats.AddModifier(new StatModifier(mod.statType,StatMappings.GetRolledModifierOperation(mod.statType),mod.value,candidate));
            Gear weapon=candidate.ItemType==LootManager.GearType.Weapons?candidate:live.EquippedWeapon;
            progression?.PopulateItemPreview(stats,weapon?.WeaponTypeId);
            var keys=actor.AddComponent<PassiveKeystoneState>();
            if(progression!=null)keys.Apply(progression);
            var mana=actor.AddComponent<ManaComponent>();mana.RestoreCheckpointMana(live.GetComponent<ManaComponent>()?.CurrentMana??0);
            player.ConfigureItemPreview(live.CurrentPlayerLevel,weapon);
            return Calculate(player);
        }
        finally{if(Application.isPlaying)Object.Destroy(actor);else Object.DestroyImmediate(actor);}
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static float MarginalContribution(PlayerController live,Gear candidate,int modifierIndex,float fullCandidateDps)
    {
        if(live==null||candidate==null||modifierIndex<0||modifierIndex>=candidate.rolledMods.Count)return 0;
        var snapshot=GearSnapshotData.Capture(candidate);
        snapshot.mods.RemoveAt(modifierIndex);
        Gear without=snapshot.Create("Marginal DPS preview gear");without.gameObject.hideFlags=HideFlags.HideAndDontSave;
        try
        {
            float withoutDps=Replacement(live,without);
            return withoutDps>0?(fullCandidateDps/withoutDps-1f)*100f:fullCandidateDps>0?100f:0f;
        }
        finally{if(Application.isPlaying)Object.Destroy(without.gameObject);else Object.DestroyImmediate(without.gameObject);}
    }
#endif
}
