using UnityEngine;

public enum Element     //Element enum explicitly ties each element to a numeric value
{
    Phys = 0,
    Fire = 1,
    Cold = 2,
    Light = 3,
    Poison = 4,
    Void = 5,
    Count = 6
}

[System.Flags]
public enum ElementMask     //bitmask used to combine elements. Left shifts to create binary for each so they can be combined with bitwise OR
{
    None = 0,
    Phys = 1 << 0,
    Fire = 1 << 1,
    Cold = 1 << 2,
    Light = 1 << 3,
    Poison = 1 << 4,
    Void = 1 << 5,
    All = Phys | Fire | Cold | Light | Poison | Void
}

[CreateAssetMenu(fileName = "StatusEffects", menuName = "Scriptable Objects/StatusEffects")]
public class StatusEffects : ScriptableObject   //Status effects SO
{
    public enum StackPolicy { StackAndRefresh, StackIndependently, ReplaceIfStronger, ReplaceAlways }       //Define the types of stack policies in an enum
    public enum StatusType { DamageOverTime, Shock, Chill }     //Define the status types in an enum

    public enum AilmentKind     //Define the ailment types in an enum
    {
        None,
        Poison,
        Bleed,
        Ignite,
        GenericDot  // fallback if a generic DoT effect is desired later
    }

    [SerializeField] protected string effectName;           //Name of the status effect
    [SerializeField] protected float effectMagnitude = 1f; //Multiplier to hit damage that results in the base ailment effect
    [SerializeField] protected int tickDuration = 1;       //Base # of ticks before the effect falls off
    [SerializeField] protected int maxStacks = 1;           //Base Max Stacks
    [SerializeField] protected StackPolicy stackPolicy;
    [SerializeField] protected StatusType statusType;

    [Header("Element / Visuals")]
    [SerializeField] protected ElementMask elements = ElementMask.Phys;
    [SerializeField] protected Color damageColor = Color.white;         //Damage color

    [Header("Ailment")]
    [SerializeField] protected AilmentKind ailmentKind = AilmentKind.None;      //Ailment type

    [Tooltip("Base number of turns between ticks. Example: Poison = 4, Ignite = 1.")]
    [SerializeField] protected int baseTurnInterval = 1;        //Base number of turns between ticks

    public string Name => effectName;       //Getters for protected variables
    public float Magnitude => effectMagnitude;
    public int TickDuration => tickDuration;
    public int MaxStacks => maxStacks;
    public StackPolicy _StackPolicy => stackPolicy;
    public StatusType _StatusType => statusType;
    public ElementMask Elements => elements;
    public Color DamageColor => damageColor;
    public AilmentKind Ailment => ailmentKind;
    public int BaseTurnInterval => Mathf.Max(1, baseTurnInterval);

    public virtual void OnApply(DamageContext context, StatusController controller, StatusInstance stack) { }   //OnApply Function called when ailments do something the moment they are applied
    public virtual void OnTick(DamageContext context, StatusController controller, StatusInstance stack) { }    //OnTick Function called when ailments do something on tick
    public virtual void OnExpire(DamageContext context, StatusController controller, StatusInstance stack) { }  //OnExpire Function called when ailments do something on expire
    public virtual void OnModifyOutgoingDamage(DamageContext context, StatusController controller, StatusInstance stack) { }    //On modify outgoing damage Function called for when ailments modify the outgoing damage of the attacker
}
