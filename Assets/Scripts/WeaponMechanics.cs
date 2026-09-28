// Step 17 centralized first-pass weapon mechanic profiles and Axe Rage authority.
using UnityEngine;

public static class WeaponMechanicProfile
{
    public const float MinimumCooldown=.20f;
    public const float RapidFlurrySpacing=.08f;
    public const int RapidFlurryBaseHits=3,RapidFlurryMaximumHits=8;
    public const float RapidFlurryAttackSpeedPerHit=.50f;
    public const float ArmourStrikeMorePer100Armour=.02f,ArmourStrikeMaximumMore=2f;
    public const float ShatterBaseMultiplier=3f,ShatterChillMultiplier=4f;
    public static int RapidFlurryHits(float bonusAttackSpeed)=>Mathf.Clamp(RapidFlurryBaseHits+Mathf.FloorToInt(Mathf.Max(0,bonusAttackSpeed)/RapidFlurryAttackSpeedPerHit),RapidFlurryBaseHits,RapidFlurryMaximumHits);
    public static float ArmourStrikeMultiplier(float armour)=>1.5f*(1+Mathf.Min(ArmourStrikeMaximumMore,Mathf.Max(0,armour)/100*ArmourStrikeMorePer100Armour));
    public static float ShatterMultiplier(float chillStrength)=>ShatterBaseMultiplier+ShatterChillMultiplier*Mathf.Clamp01(chillStrength);
    public const float BaseBowPrecisionChance=.10f;
    public const float BaseBowPrecisionMultiplier=1.50f;
    public const float BaseProjectileTravelTime=1f;
    public const float MinimumProjectileTravelTime=.10f;
    public const float ProjectileBarrageSpacing=.10f;
    public const float RageDecayDelay=0f;
    public const float RageDecayPerSecond=10f;
    public const float RageGenerationDuration=2f, RageAttackSpeedPerPoint=.003f;
    public static float DecayRate(float reduction,bool fullRageKeystone)=>RageDecayPerSecond*(1-Mathf.Clamp01(reduction))*(fullRageKeystone?3:1);
    public static float AdvanceRage(float rage,float rate,float activeSeconds,float delta,float reduction,bool crown)
    {
        float generating=Mathf.Min(Mathf.Max(0,activeSeconds),Mathf.Max(0,delta)),decay=DecayRate(reduction,crown);
        rage=Mathf.Clamp(rage+(rate-decay)*generating,0,100);
        return Mathf.Clamp(rage-decay*Mathf.Max(0,delta-generating),0,100);
    }
    public const float RageFinisherMoreMultiplier=2f;
    public const float RageDamagePerPoint=.002f;
    public const float RageDefensePerPoint=.001f;
    public const float MaximumRageDefense=.10f;
    public const float FullRageMoreBonus=0f;
    public static float ProjectileTravelTime(float increasedSpeed, float baseSpeed = 1f)=>Mathf.Max(MinimumProjectileTravelTime,BaseProjectileTravelTime/(Mathf.Max(.01f,baseSpeed)*(1f+Mathf.Max(0f,increasedSpeed))));
    public static float PrecisionChance(float addedChance)=>Mathf.Clamp01(BaseBowPrecisionChance+Mathf.Max(0f,addedChance));
    public static float PrecisionMultiplier(float increasedDamage)=>BaseBowPrecisionMultiplier*(1+Mathf.Max(0f,increasedDamage));
    public static float RageGainFromDamage(float damage,float maximumLife,float eventMultiplier=1f)
        =>damage<=0?0:(18+Mathf.Min(10,Mathf.Floor(damage/Mathf.Max(1,maximumLife)*50)))*Mathf.Max(0,eventMultiplier);
}

[RequireComponent(typeof(PlayerController),typeof(StatsComponent))]
public sealed class RageState:MonoBehaviour
{
    public const float MaximumRage=100f;
    PlayerController player;StatsComponent stats;float sinceGain,generationRate,generationRemaining;
    public float Rage{get;private set;}public bool FinisherArmed{get;private set;}
    public bool IsSupportedWeapon=>UniqueCatalog.Power(stats,UniquePower.AnyWeaponRage)>0||GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.BarbarianFullRage)==true||player?.EquippedWeapon!=null&&WeaponTypeCatalog.TryGet(player.EquippedWeapon.WeaponTypeId,out var w)&&w.SupportsRage;
    public bool FinisherAvailable=>IsSupportedWeapon&&Rage>=MaximumRage&&GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.RageFinisher)==true;
    public event System.Action Changed;
    void Awake(){player=GetComponent<PlayerController>();stats=GetComponent<StatsComponent>();player.AttackChanged+=OnWeaponChanged;}
    void OnDestroy(){if(player!=null)player.AttackChanged-=OnWeaponChanged;}
    void Update(){if(!SkillTreeUI.PausesGameplay&&!PlayerSkillMenuUI.IsOpen)Tick(Time.deltaTime);}
    void OnWeaponChanged(){if(IsSupportedWeapon)return;Rage=0;FinisherArmed=false;sinceGain=0;generationRate=0;generationRemaining=0;Changed?.Invoke();}
    public void Tick(float delta)
    {if(!IsSupportedWeapon||delta<=0)return;SetRage(WeaponMechanicProfile.AdvanceRage(Rage,generationRate,generationRemaining,delta,stats.GetStat(StatTypes.RageDecayReduction),GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.BarbarianFullRage)==true));generationRemaining=Mathf.Max(0,generationRemaining-delta);}
    public float IncreasedAttackSpeed=>IsSupportedWeapon?Rage*WeaponMechanicProfile.RageAttackSpeedPerPoint*(1+Mathf.Max(0,stats.GetStat(StatTypes.RageEffect))):0;
    public float GenerationRate=>generationRemaining>0?generationRate:0;
    public void GainFromDamageDealt(float damage,float targetMaximumLife,float eventMultiplier=1f){if(!IsSupportedWeapon||damage<=0)return;Gain(WeaponMechanicProfile.RageGainFromDamage(damage,targetMaximumLife,eventMultiplier));}
    public void GainFromDamageTaken(float damage,float playerMaximumLife){} // Taking damage feeds Revenge, not instant Rage.
    void Gain(float amount){float multiplier=1+Mathf.Max(0,stats.GetStat(StatTypes.RageGeneration));generationRate=Mathf.Max(generationRemaining>0?generationRate:0,amount*multiplier);generationRemaining=WeaponMechanicProfile.RageGenerationDuration;}
    void SetRage(float value){float next=Mathf.Clamp(value,0,MaximumRage);if(Mathf.Approximately(next,Rage))return;Rage=next;if(Rage<MaximumRage)FinisherArmed=false;Changed?.Invoke();}
    public void ResetTransient(){Rage=0;FinisherArmed=false;sinceGain=0;generationRate=0;generationRemaining=0;Changed?.Invoke();}
    public bool TryArmFinisher(){if(!FinisherAvailable||FinisherArmed)return false;FinisherArmed=true;Changed?.Invoke();return true;}
    public float BeginAttackEventMultiplier(){return FinisherArmed&&IsSupportedWeapon?WeaponMechanicProfile.RageFinisherMoreMultiplier:1f;}
    public void CompleteAttackEvent(bool resolved){if(!resolved||!FinisherArmed)return;FinisherArmed=false;Rage=0;sinceGain=0;generationRate=0;generationRemaining=0;Changed?.Invoke();}
    public float SustainedDamageMultiplier => !IsSupportedWeapon?1:ClassKeystoneMechanics.FullRageMultiplier(Rage,stats.GetStat(StatTypes.RageEffect),GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.BarbarianFullRage)==true);
    public float IncomingDamageMultiplier=>1f;
}
