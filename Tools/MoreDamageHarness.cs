// Focused engine-free checks. Formula classes/methods are loaded from current production sources.
// Stubs supply Unity metadata/math only, not combat/stat arithmetic.
namespace UnityEngine
{
    public class MonoBehaviour { }
    public class ScriptableObject { }
    public class SerializeField : System.Attribute { }
    public class HeaderAttribute : System.Attribute { public HeaderAttribute(string s){} }
    public class TooltipAttribute : System.Attribute { public TooltipAttribute(string s){} }
    public class CreateAssetMenuAttribute : System.Attribute { public string fileName, menuName; }
    public struct Color { public static Color white => new Color(); }
    public static class Mathf
    {
        public static float Max(float a,float b)=>System.Math.Max(a,b);
        public static int Max(int a,int b)=>System.Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>System.Math.Min(b,System.Math.Max(a,x));
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static int RoundToInt(float x)=>(int)System.Math.Round(x);
    }
    public static class Random { public static float value=.5f; public static int Range(int a,int b)=>a; }
    public static class Debug { public static void Log(object s){} }
}
public class LootManager
{
    public enum GearType { Helmets,Amulets,BodyArmours,Gloves,Boots,Rings,Belts,Weapons }
    public enum GearRarity { Normal,Magic,Rare,Legendary }
}
public class StatusController { }
public class StatusInstance { }
public class PaperWeaponVisual { }
public class PlayerController
{
    public StatsComponent stats;
    public Gear equippedWeapon;
    // PLAYER_ATTACK_METHODS
}
public class EnemyAI
{
    public StatsComponent stats;
    public Gear equippedWeapon;
    // ENEMY_ATTACK_METHODS
}
public class MoreDamageEffect : StatusEffects
{
    public MoreDamageEffect(AilmentKind kind)
    { ailmentKind=kind;effectMagnitude=.5f;tickDuration=2;baseTurnInterval=4;elements=ElementMask.All; }
}
public static class MoreDamageHarness
{
    static int checks;
    static readonly System.Text.StringBuilder report=new System.Text.StringBuilder();
    static void Near(float actual,float expected,string label)
    {
        if(System.Math.Abs(actual-expected)>.001f)throw new System.Exception(label+": expected "+expected+", got "+actual);
        checks++;report.AppendLine("PASS: "+label+" = "+actual.ToString("0.####"));
    }
    static Gear Item(StatTypes type,float value,LootManager.GearType slot=LootManager.GearType.Rings)
    {var gear=new Gear();gear.Initialize(slot,LootManager.GearRarity.Magic,1,Element.Cold);gear.ApplyMods(new List<RolledMod>{new RolledMod(type,1,value)});return gear;}
    static void Apply(StatsComponent stats,Gear gear)
    {foreach(var mod in gear.globalRolledMods) stats.AddModifier(new StatModifier(mod.statType,StatOp.Multiplicative,mod.value,gear));}
    static float Hit(DamageContext ctx,Element element)=>ctx.Hits.Where(h=>h.Element==element).Sum(h=>h.Amount);
    public static string Run()
    {
        foreach(var element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Poison,Element.Void})
        {
            var stats=new StatsComponent();var weapon=new Gear{BaseDamage=100,BaseElement=element};
            var player=new PlayerController{stats=stats,equippedWeapon=weapon};var enemy=new EnemyAI{stats=stats,equippedWeapon=weapon};
            var inc=StatMappings.GetIncDamageStat(element);var more=StatMappings.GetMoreDamageStat(element);
            stats.SetBaseStat(inc,40);stats.SetBaseStat(StatTypes.GenericDmg,30);
            var a=Item(more,20);var b=Item(StatTypes.GenericMult,10,LootManager.GearType.Helmets);Apply(stats,a);Apply(stats,b);
            Near(Hit(player.BuildNonCriticalAttackContext(),element),224.4f,element+" player 100*1.7*1.2*1.1");
            Near(Hit(enemy.BuildNonCriticalAttackContext(),element),224.4f,element+" enemy agreement");
            var unrelated=more==StatTypes.FireMult?StatTypes.ColdMult:StatTypes.FireMult;stats.SetBaseStat(unrelated,900);
            Near(Hit(player.BuildAttackContext(),element),224.4f,element+" real attack excludes nonmatching more");
            Near(Hit(enemy.BuildAttackContext(),element),224.4f,element+" enemy real attack excludes nonmatching more");
            var c=Item(more,20,LootManager.GearType.Belts);Apply(stats,c);
            Near(stats.GetStat(more),.44f,element+" separate identical stat rolls expose44% effective");
            Near(Hit(player.BuildNonCriticalAttackContext(),element),269.28f,element+" duplicate20 rolls remain independent");
            stats.RemoveModifiersFromSource(c);Near(Hit(player.BuildNonCriticalAttackContext(),element),224.4f,element+" removing one item invalidates cache");
            stats.SetBaseStat(more,10);Near(Hit(player.BuildNonCriticalAttackContext(),element),246.84f,element+" base10 is another independent factor");
            stats.SetBaseStat(more,0);stats.SetBaseStat(unrelated,0);
            var extra=element==Element.Cold?Element.Fire:Element.Cold;
            stats.SetBaseStat(StatMappings.GetFlatDamageStat(extra),10);stats.SetBaseStat(StatMappings.GetIncDamageStat(extra),40);
            stats.SetBaseStat(StatMappings.GetMoreDamageStat(extra),20);
            Near(Hit(player.BuildNonCriticalAttackContext(),extra),22.44f,element+" off-element player flat uses independent factors");
            Near(Hit(enemy.BuildNonCriticalAttackContext(),extra),22.44f,element+" off-element enemy agreement");
        }
        var s=new StatsComponent();var one=Item(StatTypes.GenericMult,20);var two=Item(StatTypes.GenericMult,20,LootManager.GearType.Helmets);Apply(s,one);Apply(s,two);
        Near((1+s.GetStat(StatTypes.GenericMult))*100,144,"Two actual separate gear20 rolls give144 from100");
        s.AddModifier(new StatModifier(StatTypes.GenericMult,StatOp.Override,50,"override"));Near(s.GetRawStat(StatTypes.GenericMult),50,"Final override contract preserved");
        s.RemoveModifiersFromSource("override");Near(s.GetRawStat(StatTypes.GenericMult),44,"Removing override restores compounded rolls");
        var v=new StatValue(10);v.AddModifier(new StatModifier(StatTypes.Life,StatOp.Flat,5));v.AddModifier(new StatModifier(StatTypes.Life,StatOp.Additive,5));v.AddModifier(new StatModifier(StatTypes.Life,StatOp.Multiplicative,.5f));Near(v.GetValue(),30,"Ordinary flat/additive/multiplicative behavior unchanged");
        foreach(var op in new[]{StatOp.Flat,StatOp.Additive,StatOp.Multiplicative})
        {var st=new StatsComponent();st.AddModifier(new StatModifier(StatTypes.GenericMult,op,20));st.AddModifier(new StatModifier(StatTypes.GenericMult,op,20));Near(st.GetRawStat(StatTypes.GenericMult),44,"Legacy "+op+" more point rolls each compound");}
        foreach(var kind in new[]{StatusEffects.AilmentKind.Bleed,StatusEffects.AilmentKind.Poison,StatusEffects.AilmentKind.Ignite,StatusEffects.AilmentKind.GenericDot})
        {
            var st=new StatsComponent();var ele=kind==StatusEffects.AilmentKind.Ignite?Element.Fire:Element.Phys;
            var player=new PlayerController{stats=st,equippedWeapon=new Gear{BaseElement=ele,BaseDamage=100}};
            st.SetBaseStat(StatTypes.GenericDmg,30);st.SetBaseStat(StatMappings.GetIncDamageStat(ele),40);
            st.SetBaseStat(StatTypes.GenericMult,10);st.SetBaseStat(StatMappings.GetMoreDamageStat(ele),20);
            var effect=new MoreDamageEffect(kind);var context=player.BuildNonCriticalAttackContext();
            float eligible=224.4f;if(kind==StatusEffects.AilmentKind.Poison){context.AddDamage(Element.Poison,50);eligible+=50;}
            if(kind!=StatusEffects.AilmentKind.GenericDot)context.AddDamage(Element.Void,999);
            Apply(st,Item(StatTypes.GenericDotMult,20));Apply(st,Item(StatTypes.GenericDotMult,20));
            float expected=eligible*.5f*1.44f/2;
            if(kind!=StatusEffects.AilmentKind.GenericDot)
            {
                var ailMore=(StatTypes)System.Enum.Parse(typeof(StatTypes),kind+"Mult");var ailInc=(StatTypes)System.Enum.Parse(typeof(StatTypes),kind+"Dmg");
                st.AddModifier(new StatModifier(ailInc,StatOp.Additive,40));st.AddModifier(new StatModifier(ailInc,StatOp.Additive,30));
                Apply(st,Item(ailMore,10));Apply(st,Item(ailMore,20));expected*=1.7f*1.1f*1.2f;
                // Nonmatching ailment modifiers cannot change this tick.
                st.SetBaseStat(kind==StatusEffects.AilmentKind.Ignite?StatTypes.BleedMult:StatTypes.IgniteMult,900);
            }
            AilmentCalculator.ComputeAilmentFromHit(effect,context,st,out float tick,out int count,out int interval);
            Near(tick,expected,kind+" independent DOT/ailment rolls; hit scaling applied only once");Near(count,2,kind+" base tick count");Near(interval,4,kind+" interval unchanged");
            if(kind!=StatusEffects.AilmentKind.GenericDot)
            {st.SetBaseStat((StatTypes)System.Enum.Parse(typeof(StatTypes),kind+"Duration"),2);AilmentCalculator.ComputeAilmentFromHit(effect,context,st,out tick,out count,out interval);Near(tick,expected,kind+" duration preserves strength");Near(count,4,kind+" duration adds ticks");}
        }
        var critStats=new StatsComponent();critStats.SetBaseStat(StatTypes.BaseCritChance,2);critStats.SetBaseStat(StatTypes.CritChance,100);
        var crit=new PlayerController{stats=critStats,equippedWeapon=new Gear{BaseCritChance=.05f,LocalIncCrit=1}};
        Near(crit.GetFinalCritChance(),.28f,"Crit flat/local/global formula unchanged");
        return report.ToString()+"COMPLETE: "+checks+" focused production-source assertions passed. Engine lifecycle/rendering are stubbed.\n";
    }
}
