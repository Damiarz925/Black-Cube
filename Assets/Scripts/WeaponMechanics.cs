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
    public const float RageDecayDelay=3f;
    public const float RageDecayPerSecond=10f;
    public const float RageFinisherMoreMultiplier=2f;
    public const float RageDamagePerPoint=.002f;
    public const float RageDefensePerPoint=.001f;
    public const float MaximumRageDefense=.10f;
    public const float FullRageMoreBonus=.15f;
    public static float ProjectileTravelTime(float increasedSpeed)=>Mathf.Max(MinimumProjectileTravelTime,BaseProjectileTravelTime/(1f+Mathf.Max(0f,increasedSpeed)));
    public static float PrecisionChance(float addedChance)=>Mathf.Clamp01(BaseBowPrecisionChance+Mathf.Max(0f,addedChance));
    public static float PrecisionMultiplier(float increasedDamage)=>BaseBowPrecisionMultiplier*(1+Mathf.Max(0f,increasedDamage));
}

[RequireComponent(typeof(PlayerController),typeof(StatsComponent))]
public sealed class RageState:MonoBehaviour
{
    public const float MaximumRage=100f;
    PlayerController player;StatsComponent stats;float sinceGain;
    public float Rage{get;private set;}public bool FinisherArmed{get;private set;}
    public bool IsSupportedWeapon=>player?.EquippedWeapon!=null&&WeaponTypeCatalog.TryGet(player.EquippedWeapon.WeaponTypeId,out var w)&&w.SupportsRage;
    public bool FinisherAvailable=>IsSupportedWeapon&&Rage>=MaximumRage&&GetComponent<PassiveKeystoneState>()?.Has(PassiveKeystone.RageFinisher)==true;
    public event System.Action Changed;
    void Awake(){player=GetComponent<PlayerController>();stats=GetComponent<StatsComponent>();player.AttackChanged+=OnWeaponChanged;}
    void OnDestroy(){if(player!=null)player.AttackChanged-=OnWeaponChanged;}
    void Update(){Tick(Time.deltaTime);}
    void OnWeaponChanged(){if(IsSupportedWeapon)return;Rage=0;FinisherArmed=false;sinceGain=0;Changed?.Invoke();}
    public void Tick(float delta){if(!IsSupportedWeapon||Rage<=0||delta<=0)return;float before=Mathf.Max(0,sinceGain-WeaponMechanicProfile.RageDecayDelay);sinceGain+=delta;float after=Mathf.Max(0,sinceGain-WeaponMechanicProfile.RageDecayDelay);float decayTime=after-before;if(decayTime<=0)return;float reduction=Mathf.Clamp01(stats.GetStat(StatTypes.RageDecayReduction));SetRage(Rage-WeaponMechanicProfile.RageDecayPerSecond*(1f-reduction)*decayTime);}
    public void GainFromDamageDealt(float damage,float targetMaximumLife,float eventMultiplier=1f){if(!IsSupportedWeapon||damage<=0)return;Gain((5+Mathf.Min(10,Mathf.Floor(damage/Mathf.Max(1,targetMaximumLife)*50)))*Mathf.Max(0,eventMultiplier));}
    public void GainFromDamageTaken(float damage,float playerMaximumLife){if(!IsSupportedWeapon||damage<=0)return;Gain(5+Mathf.Min(10,Mathf.Floor(damage/Mathf.Max(1,playerMaximumLife)*50)));}
    void Gain(float amount){float multiplier=1+Mathf.Max(0,stats.GetStat(StatTypes.RageGeneration));sinceGain=0;SetRage(Rage+amount*multiplier);}
    void SetRage(float value){float next=Mathf.Clamp(value,0,MaximumRage);if(Mathf.Approximately(next,Rage))return;Rage=next;if(Rage<MaximumRage)FinisherArmed=false;Changed?.Invoke();}
    public bool TryArmFinisher(){if(!FinisherAvailable||FinisherArmed)return false;FinisherArmed=true;Changed?.Invoke();return true;}
    public float BeginAttackEventMultiplier(){return FinisherArmed&&IsSupportedWeapon?WeaponMechanicProfile.RageFinisherMoreMultiplier:1f;}
    public void CompleteAttackEvent(bool resolved){if(!resolved||!FinisherArmed)return;FinisherArmed=false;Rage=0;sinceGain=0;Changed?.Invoke();}
    public float SustainedDamageMultiplier{get{if(!IsSupportedWeapon)return 1;float effect=1+Mathf.Max(0,stats.GetStat(StatTypes.RageEffect));return 1+Rage*WeaponMechanicProfile.RageDamagePerPoint*effect+(Rage>=MaximumRage?WeaponMechanicProfile.FullRageMoreBonus*effect:0);}}
    public float IncomingDamageMultiplier=>IsSupportedWeapon?1-Mathf.Min(WeaponMechanicProfile.MaximumRageDefense,Rage*WeaponMechanicProfile.RageDefensePerPoint*(1+Mathf.Max(0,stats.GetStat(StatTypes.RageEffect)))):1f;
}
