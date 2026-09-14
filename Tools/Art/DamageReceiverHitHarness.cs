// Minimal stubs for compiling and exercising the actual DamageReceiver source.
namespace UnityEngine {
    public class SerializeField : System.Attribute {}
    public class RequireComponent : System.Attribute { public RequireComponent(System.Type t){} }
    public class Object {}
    public class Transform : Component {}
    public class GameObject : Object {
        readonly System.Collections.Generic.Dictionary<System.Type,Component> parts=new System.Collections.Generic.Dictionary<System.Type,Component>();
        public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};parts[typeof(T)]=c;return c;}
        public T GetComponent<T>() where T:Component {Component c;return parts.TryGetValue(typeof(T),out c)?(T)c:null;}
    }
    public class Component : Object {
        public GameObject gameObject; public Transform transform=>GetComponent<Transform>();
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { protected static T FindFirstObjectByType<T>() where T:Component{return null;} }
}
public enum Element { Phys,Fire }
public class StatusEffects : UnityEngine.Object {}
public struct Hit { public Element Element; public float Amount; }
public struct DamageContext { public System.Collections.Generic.List<Hit> Hits; }
public class HealthComponent : UnityEngine.MonoBehaviour {
    public float CurrentLife{get;private set;}=10;
    public void LoseLife(float value){CurrentLife=System.Math.Max(0,CurrentLife-value);}
}
public class DamagePopup : UnityEngine.MonoBehaviour {
    public void Spawn(float d,UnityEngine.Transform t,StatusEffects e){}
    public void Spawn(float d,UnityEngine.Transform t,Element e){}
}
public class PaperSpriteActor : UnityEngine.MonoBehaviour {
    public int reactions; public float lifeWhenNotified;
    public void PlayHitReaction(){reactions++;lifeWhenNotified=GetComponent<HealthComponent>().CurrentLife;}
}
public static class DamageReceiverHitHarness {
    static void Check(bool condition,string message){if(!condition)throw new System.Exception(message);}
    public static string Run(){
        var go=new UnityEngine.GameObject();go.AddComponent<UnityEngine.Transform>();
        var health=go.AddComponent<HealthComponent>();var actor=go.AddComponent<PaperSpriteActor>();var receiver=go.AddComponent<DamageReceiver>();
        typeof(DamageReceiver).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(receiver,null);
        receiver.TakeDamage(2,Element.Phys);Check(health.CurrentLife==8&&actor.reactions==1&&actor.lifeWhenNotified==8,"Damage must resolve before visual hit notification");
        receiver.TakeDamage(1,Element.Fire);Check(actor.reactions==2,"Repeated nonlethal hit did not retrigger");
        receiver.TakeDamage(0,Element.Phys);Check(actor.reactions==2&&health.CurrentLife==7,"Zero damage triggered a reaction");
        receiver.TakeDamage(20,Element.Phys);Check(health.CurrentLife==0&&actor.reactions==2,"Lethal hit reaction was not suppressed");
        return "PASS: actual DamageReceiver applies damage first, retriggers nonlethal hits, ignores zero damage, and suppresses lethal reactions.";
    }
}
