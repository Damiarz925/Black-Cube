// Developer map: Unity/dependency stubs and assertions compiled with actual combat/actor sources by verify_combat_animation.ps1. This is not an in-engine renderer or full-project build.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
// File-only substitutes: compiles the actual BattleManager and PaperSpriteActor.
// No Unity process, scene, renderer or asset import is started.
namespace UnityEngine {
    public class SerializeField : System.Attribute {}
    public class HeaderAttribute : System.Attribute { public HeaderAttribute(string s) {} }
    public class MinAttribute : System.Attribute { public MinAttribute(float f) {} }
    public class RangeAttribute : System.Attribute { public RangeAttribute(float a,float b) {} }
    public class TooltipAttribute : System.Attribute { public TooltipAttribute(string s){} }
    public class CreateAssetMenuAttribute : System.Attribute { public string fileName,menuName; }
    public class Object {
        public string name; public bool destroyed;
        public static implicit operator bool(Object o) { return o != null && !o.destroyed; }
    }
    public class Component : Object {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T:Component { return gameObject.GetComponent<T>(); }
    }
    public class ScriptableObject : Object {}
    public class MonoBehaviour : Component {
        protected static void Destroy(Object o) { if(o!=null)o.destroyed=true; }
        protected static T FindFirstObjectByType<T>() where T:Component { return null; }
        protected static GameObject Instantiate(GameObject o,Vector3 p,Quaternion q) { return CombatTimingHarness.MakeEnemy(); }
    }
    public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c) { x=a;y=b;z=c; }
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    }
    public struct Quaternion {
        public float angle;
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{angle=z};
        public static Vector3 operator *(Quaternion q,Vector3 p){double r=q.angle*System.Math.PI/180;return new Vector3((float)(p.x*System.Math.Cos(r)-p.y*System.Math.Sin(r)),(float)(p.x*System.Math.Sin(r)+p.y*System.Math.Cos(r)),p.z);}
    }
    public class Transform : Component {
        public Vector3 position,localPosition,localScale; public Quaternion rotation,localRotation;
        public void SetParent(Transform p,bool world=true) {}
        public Vector3 TransformPoint(Vector3 p) { return p; }
    }
    public class GameObject : Object {
        public Transform transform;
        private System.Collections.Generic.Dictionary<System.Type,Component> parts=new System.Collections.Generic.Dictionary<System.Type,Component>();
        public GameObject(string n, params System.Type[] types) {
            name=n;transform=new Transform {gameObject=this};parts[typeof(Transform)]=transform;
            foreach(var t in types) { var c=(Component)System.Activator.CreateInstance(t);c.gameObject=this;parts[t]=c; }
        }
        public T GetComponent<T>() where T:Component { Component c; return parts.TryGetValue(typeof(T),out c)?(T)c:null; }
        public T AddComponent<T>() where T:Component,new() { var c=new T {gameObject=this};parts[typeof(T)]=c;return c; }
        public static GameObject FindGameObjectWithTag(string s) { return null; }
    }
    public class Sprite : Object {}
    public class Material : Object {}
    public class SpriteRenderer : Component {
        public Sprite sprite;public bool flipX,enabled=true;public Material sharedMaterial;public int sortingLayerID,sortingOrder;
    }
    public class Animator : Component { public void SetFloat(string s,float f){} public void SetTrigger(string s){} }
    public static class Debug { public static void Log(object s,params Object[] o){} public static void LogWarning(object s,params Object[] o){} public static void LogError(object s,params Object[] o){} }
    public static class Time { public static float time,deltaTime,timeScale=1f;public static int frameCount; }
    public static class Random { public static float value=1f; }
    public static class Mathf {
        public static float Max(float a,float b) {return System.Math.Max(a,b);}
        public static int Min(int a,int b) {return System.Math.Min(a,b);}
        public static int Clamp(int v,int a,int b) {return System.Math.Max(a,System.Math.Min(v,b));}
        public static float Clamp(float v,float a,float b) {return System.Math.Max(a,System.Math.Min(v,b));}
        public static float Repeat(float x,float n) {return x-(float)System.Math.Floor(x/n)*n;}
    }
}
public static class SkillTreeUI { public static bool IsOpen; }
public class HealthComponent : UnityEngine.MonoBehaviour {
    public float CurrentLife=100000;
    public void ReviveToFullLife(){CurrentLife=100000;}
    public void RestoreLife(float amount){CurrentLife+=amount;}
    public void SetEnemyRole(bool b){}
}
public enum StatTypes { PoisonChance,BleedChance,IgniteChance,ChillChance,ShockChance,LifeOnHit }
public class StatsComponent : UnityEngine.MonoBehaviour { public float GetStat(StatTypes t){return 0;} }
public class StatusEffects : UnityEngine.Object {}
public class StatusController : UnityEngine.MonoBehaviour {
    public System.Action onTick;
    public void TickStatuses(){if(onTick!=null)onTick();}
    public void ApplyAilmentFromHit(StatusEffects e,DamageContext c,StatsComponent s,int stacksPerHit){}
}
public struct Hit { public float Amount; }
public struct DamageContext {
    public System.Collections.Generic.List<Hit> Hits;public bool IsCrit;public float CritMultiplier;
}
public class PlayerController : UnityEngine.MonoBehaviour {
    public float speed=1;public int attacks;
    private Gear equippedWeapon;
    public event System.Action AttackChanged;
    // PLAYER_EQUIPMENT_GETTER
    // PLAYER_EQUIP_METHOD
    public float GetFinalAttackSpeed(){return speed;}
    public DamageContext BuildAttackContext(){attacks++;return new DamageContext {Hits=new System.Collections.Generic.List<Hit>{new Hit {Amount=1}}};}
}
public class Gear : UnityEngine.MonoBehaviour {
    public PaperWeaponVisual WeaponVisual {get;private set;}
    public void SetWeaponVisual(PaperWeaponVisual visual){WeaponVisual=visual;}
}
public class EnemyAI : UnityEngine.MonoBehaviour {
    public float speed;
    public float GetFinalAttackSpeed(){return speed;}
    public void InitializeEnemy(int level){}
    public DamageContext BuildAttackContext(){return new DamageContext {Hits=new System.Collections.Generic.List<Hit>{new Hit {Amount=1}}};}
}
public class DamageReceiver : UnityEngine.MonoBehaviour {
    public int hits;public System.Action onHit;
    public void TakeDamage(float amount,DamageContext c){hits++;GetComponent<HealthComponent>().CurrentLife-=amount;if(onHit!=null)onHit();}
}
public class ZoneManager : UnityEngine.MonoBehaviour {public int zoneLevel=1;}
public static class CombatCalculator {public static float CalculateFinalDamage(DamageContext c,StatsComponent a,StatsComponent b){return 1;}}
public static class AilmentCalculator {public static float GetSourceHitDamage(StatusEffects e,DamageContext c){return 0;}}

public static class CombatTimingHarness {
    static BattleManager manager;static PaperSpriteActor actor;static UnityEngine.SpriteRenderer body;
    static UnityEngine.GameObject player;static PlayerController controller;static HealthComponent hp;
    static System.Collections.Generic.List<DamageReceiver> receivers;
    static System.Collections.Generic.List<float> hitTimes;
    static System.Collections.Generic.HashSet<string> seen;
    static int assertions;
    static void Check(bool b,string s){assertions++;if(!b)throw new System.Exception(s);}
    static void Set(object o,string f,object v){o.GetType().GetField(f,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(o,v);}
    static T Get<T>(object o,string f){return (T)o.GetType().GetField(f,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(o);}
    static void Invoke(object o,string m){o.GetType().GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(o,null);}
    public static UnityEngine.GameObject MakeEnemy(){
        var e=new UnityEngine.GameObject("test enemy");e.AddComponent<EnemyAI>();e.AddComponent<HealthComponent>();e.AddComponent<StatsComponent>();e.AddComponent<StatusController>();
        var r=e.AddComponent<DamageReceiver>();receivers.Add(r);
        r.onHit=()=>{
            Check(body.sprite.name=="Attack4","Damage did not coincide with impact frame4");
            var weapon=Get<UnityEngine.SpriteRenderer>(actor,"attackWeapon");
            Check(weapon!=null&&weapon.enabled&&weapon.sprite.name=="Weapon4","Weapon did not match damage frame");
            hitTimes.Add(UnityEngine.Time.time);
        };
        return e;
    }
    static void BindEnemy(UnityEngine.GameObject e){
        Set(manager,"currentEnemy",e);Set(manager,"enemyAI",e.GetComponent<EnemyAI>());Set(manager,"enemyHealth",e.GetComponent<HealthComponent>());
        Set(manager,"enemyStats",e.GetComponent<StatsComponent>());Set(manager,"enemyStatusCont",e.GetComponent<StatusController>());Set(manager,"enemyDamageReceiver",e.GetComponent<DamageReceiver>());
    }
    static void Setup(float speed){
        UnityEngine.Time.time=0;UnityEngine.Time.deltaTime=0;UnityEngine.Time.timeScale=1;UnityEngine.Time.frameCount=0;SkillTreeUI.IsOpen=false;
        receivers=new System.Collections.Generic.List<DamageReceiver>();hitTimes=new System.Collections.Generic.List<float>();seen=new System.Collections.Generic.HashSet<string>();
        manager=new UnityEngine.GameObject("manager").AddComponent<BattleManager>();
        player=new UnityEngine.GameObject("player");controller=player.AddComponent<PlayerController>();controller.speed=speed;hp=player.AddComponent<HealthComponent>();
        var stats=player.AddComponent<StatsComponent>();var status=player.AddComponent<StatusController>();var damage=player.AddComponent<DamageReceiver>();
        body=player.AddComponent<UnityEngine.SpriteRenderer>();actor=player.AddComponent<PaperSpriteActor>();
        var unique=new UnityEngine.Sprite[4];for(int i=0;i<4;i++)unique[i]=new UnityEngine.Sprite {name="Idle"+(i+1)};
        actor.ConfigureIdle(body,new[]{unique[0],unique[1],unique[2],unique[3],unique[2],unique[1]},1.5f);
        var attacks=new UnityEngine.Sprite[8];var weapons=new UnityEngine.Sprite[8];
        for(int i=0;i<8;i++){attacks[i]=new UnityEngine.Sprite {name="Attack"+(i+1)};weapons[i]=new UnityEngine.Sprite {name="Weapon"+(i+1)};}
        actor.ConfigureAttack(attacks,weapons);Invoke(actor,"Awake");
        Set(manager,"player",player);Set(manager,"playerController",controller);Set(manager,"playerHealth",hp);Set(manager,"playerStats",stats);
        Set(manager,"playerStatusCont",status);Set(manager,"playerDamageReceiver",damage);Set(manager,"playerSprite",actor);
        Set(manager,"normalEnemyPrefab",new UnityEngine.GameObject("prefab"));Set(manager,"enemySpawnPoint",new UnityEngine.GameObject("spawn").transform);
        BindEnemy(MakeEnemy());
    }
    static void Step(float dt){
        UnityEngine.Time.frameCount++;UnityEngine.Time.deltaTime=dt;UnityEngine.Time.time+=dt*UnityEngine.Time.timeScale;
        int before=hitTimes.Count;Invoke(manager,"Update");Invoke(actor,"LateUpdate");seen.Add(body.sprite.name);
        if(hitTimes.Count>before)Check(body.sprite.name=="Attack4","LateUpdate erased damage-frame impact");
    }
    static int Hits(){int count=0;foreach(var r in receivers)count+=r.hits;return count;}
    static void RunCadence(float speed){
        Setup(speed);const int steps=2003;const float dt=1f/240f;
        for(int i=0;i<steps;i++)Step(dt);
        int expected=(int)System.Math.Floor(speed*steps/240.0);
        Check(Hits()==expected,"Wrong hit count at speed "+speed+": "+Hits()+" vs "+expected);
        Check(controller.attacks==Hits(),"Attack context duplicated or damage omitted");
        Check(System.Math.Abs(hitTimes[0]-1f/speed)<=dt*1.1f,"First-hit cadence changed");
        for(int i=1;i<hitTimes.Count;i++)Check(System.Math.Abs((hitTimes[i]-hitTimes[i-1])-1f/speed)<dt*1.1f,"Hit interval drift");
        if(speed<=5)for(int i=1;i<=8;i++)Check(seen.Contains("Attack"+i),"Missing authored frame "+i);
    }
    static void RunWeaponAttachments(){
        Setup(1.25f);
        var sword=new PaperWeaponVisual{sprite=new UnityEngine.Sprite{name="WholeSword"},scale=1};
        var idle=new PaperWeaponGrip[6];var attack=new PaperWeaponGrip[8];
        for(int i=0;i<idle.Length;i++)idle[i]=new PaperWeaponGrip{position=new UnityEngine.Vector2(i*.1f,1),angle=-145};
        for(int i=0;i<8;i++)attack[i]=new PaperWeaponGrip{position=new UnityEngine.Vector2(i*.1f,2+i*.2f),angle=i*15,inFront=i==6};
        actor.ConfigureWeaponAttachments(sword,idle,attack);
        var weapon=Get<UnityEngine.SpriteRenderer>(actor,"attackWeapon");
        Check(!weapon.enabled&&weapon.sprite==null,"Fallback visible with empty equipment slot");
        var first=new UnityEngine.GameObject("sword gear").AddComponent<Gear>();
        controller.EquipWeapon(first);Check(weapon.enabled&&weapon.sprite==sword.sprite,"Equip did not attach complete fallback sword");
        for(int i=0;i<8;i++){
            UnityEngine.Time.frameCount++;
            actor.ShowAttackGauge(UnityEngine.Mathf.Repeat(i/8f-3/8f,1),true);
            Check(body.sprite.name=="Attack"+(i+1),"Wrong body frame in attachment cycle");
            Check(System.Math.Abs(weapon.transform.localPosition.x-attack[i].position.x)<.0001&&System.Math.Abs(weapon.transform.localPosition.y-attack[i].position.y)<.0001,"Weapon missed frame grip");
            Check(weapon.transform.localRotation.angle==attack[i].angle,"Wrong weapon angle");
            Check(weapon.sortingOrder==body.sortingOrder+(attack[i].inFront?1:-1),"Weapon layer ignored grip");
            var unchanged=body.sprite;controller.EquipWeapon(null);Check(!weapon.enabled&&weapon.sprite==null&&body.sprite==unchanged,"Unequip retained weapon or changed body");
            controller.EquipWeapon(first);Check(weapon.enabled&&weapon.sprite==sword.sprite,"Re-equip failed during attack");
        }
        var alternate=new PaperWeaponVisual{sprite=new UnityEngine.Sprite{name="AlternateWholeWeapon"},scale=2,gripOffset=new UnityEngine.Vector2(.2f,.1f),rotationOffset=30};
        var second=new UnityEngine.GameObject("different weapon").AddComponent<Gear>();second.SetWeaponVisual(alternate);
        attack[4]=new PaperWeaponGrip{position=new UnityEngine.Vector2(2,3),angle=60};
        UnityEngine.Time.frameCount++;actor.ShowAttackGauge(.125f,true);controller.EquipWeapon(second);
        Check(weapon.sprite==alternate.sprite&&weapon.transform.localScale.x==2,"Different equipped weapon did not select profile");
        Check(System.Math.Abs(weapon.transform.localPosition.x-2.2f)<.0001&&System.Math.Abs(weapon.transform.localPosition.y-2.6f)<.0001,"Custom sprite grip/rotation/scale offset missed hand");
        UnityEngine.Time.timeScale=0;controller.EquipWeapon(null);Check(!weapon.enabled,"Paused unequip left weapon visible");controller.EquipWeapon(first);Check(weapon.enabled,"Paused re-equip failed");UnityEngine.Time.timeScale=1;
        Invoke(actor,"OnDisable");controller.EquipWeapon(second);Check(!weapon.enabled,"Disabled actor stayed subscribed");Invoke(actor,"OnEnable");Invoke(actor,"LateUpdate");Check(weapon.sprite==alternate.sprite&&weapon.enabled,"Re-enabled actor did not rebind equipment");
        controller.EquipWeapon(first);actor.CancelAttack();Invoke(actor,"LateUpdate");Check(weapon.enabled&&weapon.sprite==sword.sprite,"Idle failed to retain equipped weapon");
        controller.EquipWeapon(null);Invoke(actor,"LateUpdate");Check(!weapon.enabled&&weapon.sprite==null,"Idle unequip retained weapon pixels");
        controller.EquipWeapon(first);
        receivers[0].onHit=()=>{Check(body.sprite.name=="Attack4","Attachment setup changed impact timing");Check(weapon.sprite==sword.sprite&&weapon.enabled,"Whole sword missing at damage");hitTimes.Add(UnityEngine.Time.time);};
        for(int i=0;i<801;i++)Step(.01f);
        Check(Hits()==10,"Attached-weapon combat cadence changed");
    }
    static void RunChibiAnimationSet(){
        Setup(0);
        var config=new PaperPlayerAnimationSet {
            idleFrames=new UnityEngine.Sprite[8], attackFrames=new UnityEngine.Sprite[8],
            idleFramesPerSecond=2, idleWeaponGrips=new PaperWeaponGrip[8], attackWeaponGrips=new PaperWeaponGrip[8],
            defaultWeaponVisual=new PaperWeaponVisual {sprite=new UnityEngine.Sprite {name="ConfiguredSword"},scale=1}
        };
        for(int i=0;i<8;i++){
            config.idleFrames[i]=new UnityEngine.Sprite {name="ChibiIdle"+i};
            config.attackFrames[i]=new UnityEngine.Sprite {name="Attack"+(i+1)};
            config.idleWeaponGrips[i]=new PaperWeaponGrip {position=new UnityEngine.Vector2(i,1),angle=-145};
        }
        Set(actor,"animationSet",config);Invoke(actor,"Awake");
        controller.EquipWeapon(new UnityEngine.GameObject("equipped").AddComponent<Gear>());
        var weapon=Get<UnityEngine.SpriteRenderer>(actor,"attackWeapon");
        for(int i=0;i<=8;i++){
            if(i>0)Step(.5f);
            Check(body.sprite==config.idleFrames[i%8],"Configured eight-pose idle order/wrap failed");
            Check(weapon.enabled&&weapon.sprite==config.defaultWeaponVisual.sprite&&weapon.transform.localPosition.x==i%8,"Configured idle grip did not follow frame");
        }
        Check(Hits()==0,"Idle config generated damage");
        controller.EquipWeapon(null);Check(!weapon.enabled&&weapon.sprite==null,"Configured idle retained unequipped sword");
    }
    static void RunEnemyAnimation(){
        foreach(float speed in new[]{.5f,1.2f,5f,80f}){
            Setup(0);
            var enemy=Get<UnityEngine.GameObject>(manager,"currentEnemy");
            enemy.GetComponent<EnemyAI>().speed=speed;
            var enemyBody=enemy.AddComponent<UnityEngine.SpriteRenderer>();
            var enemyActor=enemy.AddComponent<PaperSpriteActor>();
            var animation=new PaperEnemyAnimationSet {displayName="Goblin",idleFrames=new[]{new UnityEngine.Sprite{name="EnemyRest"}},attackFrames=new UnityEngine.Sprite[8]};
            for(int i=0;i<8;i++)animation.attackFrames[i]=new UnityEngine.Sprite{name="EnemyAttack"+(i+1)};
            enemyActor.ConfigureIdle(enemyBody,animation.idleFrames,2);
            Set(enemyActor,"enemyAnimationSet",animation);Invoke(enemyActor,"Awake");Set(manager,"enemySprite",enemyActor);
            Check(enemyActor.DisplayName=="Goblin","Enemy profile display name lost");
            var playerDamage=player.GetComponent<DamageReceiver>();
            playerDamage.onHit=()=>Check(enemyBody.sprite==animation.attackFrames[3],"Enemy damage did not coincide with impact4");
            var visited=new System.Collections.Generic.HashSet<UnityEngine.Sprite>();
            for(int i=0;i<2003;i++){
                Step(1f/240f);Invoke(enemyActor,"LateUpdate");visited.Add(enemyBody.sprite);
            }
            int expected=(int)System.Math.Floor(speed*2003/240.0);
            Check(playerDamage.hits==expected,"Enemy animation changed hit cadence");
            if(speed<=5)foreach(var pose in animation.attackFrames)Check(visited.Contains(pose),"Enemy skipped authored pose");
            int hits=playerDamage.hits;var frozen=enemyBody.sprite;
            UnityEngine.Time.timeScale=0;Step(1);Invoke(enemyActor,"LateUpdate");Check(playerDamage.hits==hits&&enemyBody.sprite==frozen,"Enemy pause advanced pose/damage");
            UnityEngine.Time.timeScale=1;SkillTreeUI.IsOpen=true;Step(1);Invoke(enemyActor,"LateUpdate");Check(playerDamage.hits==hits&&enemyBody.sprite==frozen,"Skill panel advanced enemy");SkillTreeUI.IsOpen=false;
            enemy.GetComponent<EnemyAI>().speed=0;Step(.1f);Invoke(enemyActor,"LateUpdate");Check(enemyBody.sprite==animation.idleFrames[0]&&playerDamage.hits==hits,"Zero-speed enemy failed to cancel");
            enemy.GetComponent<EnemyAI>().speed=1;Step(.5f);Check(playerDamage.hits==hits,"Enemy zero-speed restart inherited gauge");Step(.5f);Check(playerDamage.hits==hits+1,"Enemy restart missed hit");
            enemy.GetComponent<StatusController>().onTick=()=>enemy.GetComponent<HealthComponent>().CurrentLife=0;
            UnityEngine.Time.frameCount++;enemyActor.CancelAttack();
            Invoke(manager,"ResolveEnemyTurn");Check(playerDamage.hits==hits+1&&enemyBody.sprite!=animation.attackFrames[3],"Status-killed enemy still struck");
        }
    }
    static void RunHitReaction(){
        var go=new UnityEngine.GameObject("hit goblin");
        var health=go.AddComponent<HealthComponent>();
        var hitBody=go.AddComponent<UnityEngine.SpriteRenderer>();
        var hitActor=go.AddComponent<PaperSpriteActor>();
        var idle=new UnityEngine.Sprite{name="HitIdle"};
        var animation=new PaperEnemyAnimationSet {
            displayName="Goblin",idleFrames=new[]{idle},attackFrames=new UnityEngine.Sprite[8],
            hitFrames=new UnityEngine.Sprite[5],hitReactionDuration=.21f
        };
        for(int i=0;i<8;i++)animation.attackFrames[i]=new UnityEngine.Sprite{name="OwnAttack"+(i+1)};
        for(int i=0;i<5;i++)animation.hitFrames[i]=new UnityEngine.Sprite{name="Hit"+(i+1)};
        Set(hitActor,"body",hitBody);Set(hitActor,"enemyAnimationSet",animation);Invoke(hitActor,"Awake");
        Check(hitBody.sprite==idle,"Goblin hit actor did not initialize idle");
        hitActor.PlayHitReaction();Check(hitBody.sprite==animation.hitFrames[0]&&hitActor.IsHitReacting,"Hit did not begin immediately from idle");
        UnityEngine.Time.deltaTime=.043f;Invoke(hitActor,"LateUpdate");Check(hitBody.sprite==animation.hitFrames[1],"Hit reaction did not advance");
        hitActor.PlayHitReaction();Check(hitBody.sprite==animation.hitFrames[0],"Re-hit queued instead of restarting impact frame");
        for(int i=0;i<5;i++){UnityEngine.Time.deltaTime=.043f;Invoke(hitActor,"LateUpdate");}
        Check(!hitActor.IsHitReacting&&hitBody.sprite==idle,"Hit reaction did not recover to idle near .21 seconds");

        hitActor.ShowAttackGauge(.625f,false);Check(hitBody.sprite==animation.attackFrames[0],"Attack wind-up setup failed");
        hitActor.PlayHitReaction();Check(hitBody.sprite==animation.attackFrames[0]&&!hitActor.IsHitReacting,"Hit interrupted attack wind-up");
        hitActor.ShowAttackGauge(0f,true);Check(hitBody.sprite==animation.attackFrames[3],"Pending hit replaced contact frame");
        hitActor.ShowAttackGauge(.125f,true);Check(hitBody.sprite==animation.hitFrames[0]&&hitActor.IsHitReacting,"Pending hit did not begin at recovery-safe frame");
        hitActor.Strike();Check(hitBody.sprite==animation.attackFrames[3]&&!hitActor.IsHitReacting,"Own strike did not supersede visual flinch");
        UnityEngine.Time.frameCount++;
        hitActor.ShowAttackGauge(.125f,true);Check(hitBody.sprite==animation.hitFrames[0],"Interrupted flinch did not resume after contact");

        health.CurrentLife=0;UnityEngine.Time.deltaTime=.01f;Invoke(hitActor,"LateUpdate");
        Check(!hitActor.IsHitReacting&&hitBody.sprite==idle,"Death did not supersede hit reaction");
        health.CurrentLife=100;hitActor.PlayHitReaction();hitActor.CancelAttack();
        Check(!hitActor.IsHitReacting&&hitBody.sprite==idle,"Reset did not restore idle after hit reaction");
    }
    public static string Run(){
        assertions=0;foreach(float speed in new[]{.5f,1.2f,5f,80f})RunCadence(speed);
        Setup(1);Step(.75f);float gauge=Get<float>(manager,"playerGauge");string pose=body.sprite.name;
        UnityEngine.Time.timeScale=0;Step(1);Check(Hits()==0&&Get<float>(manager,"playerGauge")==gauge&&body.sprite.name==pose,"Pause advanced combat");
        UnityEngine.Time.timeScale=1;SkillTreeUI.IsOpen=true;Step(1);Check(Hits()==0&&Get<float>(manager,"playerGauge")==gauge&&body.sprite.name==pose,"Skill tree advanced combat");SkillTreeUI.IsOpen=false;Step(.25f);Check(Hits()==1,"Resume lost or doubled hit");
        Setup(1);Step(.5f);controller.speed=4;Step(.124f);Check(Hits()==0,"Speed change hit early");Step(.001f);Check(Hits()==1,"Speed change failed to scale remaining windup");
        Setup(1);Step(.875f);controller.speed=0;Step(.5f);Check(Hits()==0&&body.sprite.name.StartsWith("Idle"),"Zero speed retained attack");controller.speed=1;Step(.5f);Check(Hits()==0,"Canceled zero-speed windup leaked damage");Step(.5f);Check(Hits()==1,"Restart after zero speed failed");
        Setup(1);Step(.875f);var old=receivers[0];manager.SpawnNextEnemy();Step(.5f);Check(Hits()==0,"Old target windup transferred");Step(.5f);Check(Hits()==1&&old.hits==0,"Target switch hit old enemy");
        Setup(1);Step(.875f);Set(manager,"currentEnemy",null);Step(1);Check(Hits()==0,"Invalid target took damage");
        Setup(1);Step(.875f);hp.CurrentLife=0;Step(1);Check(Hits()==0,"Dead player attacked");hp.ReviveToFullLife();Step(.5f);Check(Hits()==0,"Death preserved stale windup");Step(.5f);Check(Hits()==1,"Revived attack did not restart");
        Setup(1);var dying=receivers[0];var observe=dying.onHit;dying.onHit=()=>{observe();manager.SpawnNextEnemy();};Step(1);Check(Hits()==1&&body.sprite.name=="Attack4","Synchronous enemy replacement hid impact or hit twice");Step(.5f);Check(Hits()==1,"New target consumed stale time");Step(.5f);Check(Hits()==2,"New target cadence failed");
        Setup(1);player.GetComponent<StatusController>().onTick=()=>hp.CurrentLife=0;Step(1);Check(Hits()==0,"Status death did not cancel damage");
        Setup(1);Get<StatusController>(manager,"enemyStatusCont").onTick=()=>manager.SpawnNextEnemy();Step(1);Check(Hits()==0,"Status target replacement leaked hit");
        Setup(80);Step(.5f);Check(Hits()==10,"Catch-up guard changed");Step(0);Step(0);Step(0);Check(Hits()==40&&controller.attacks==40,"Catch-up lost or duplicated hits");Step(0);Check(Hits()==40,"Catch-up replayed damage");
        Setup(0);for(int i=0;i<32;i++)Step(.125f);Check(body.sprite.name=="Idle1","Four-second idle did not wrap");
        var legacy=new UnityEngine.Sprite[4];for(int i=0;i<4;i++)legacy[i]=new UnityEngine.Sprite {name="Ghoul"+i};
        var ghoul=new UnityEngine.GameObject("ghoul");var gbody=ghoul.AddComponent<UnityEngine.SpriteRenderer>();var gactor=ghoul.AddComponent<PaperSpriteActor>();gactor.Configure(gbody,legacy);gactor.Strike();Check(gbody.sprite==legacy[3],"Legacy ghoul strike regressed");
        RunWeaponAttachments();
        RunChibiAnimationSet();
        RunEnemyAnimation();
        RunHitReaction();
        return "PASS: "+assertions+" assertions against actual BattleManager + PaperSpriteActor + player/enemy/weapon profiles + PlayerController equip method. Player and enemy cadence at0.5/1.2/5/80 attacks/sec, frame4 impact, speed changes, pause/death/targets/catch-up, enemy eight-pose cycle/zero-speed restart/status-death cancellation, five-frame visual-only hit reaction/re-hit/defer/death/reset, idle/ghoul legacy, empty-slot hiding, equip/unequip/re-equip across8 poses and idle, different weapon profile, grip/angle/layer/scale offsets and disabled-event lifecycle. Unity runtime/import not tested.";
    }
}
