// Developer map: Dependency stubs for actual GameManager with production quota/death-claim methods. Verifies nine normals then stage10 boss, duplicate reward guards, direct loads and restart across121 combat levels.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
// Dependency stubs for the actual GameManager plus extracted production quota
// and death-claim methods. This exercises encounter sequencing, not rendering.
namespace UnityEngine {
    public class SerializeField:System.Attribute{}
    public class HeaderAttribute:System.Attribute{public HeaderAttribute(string s){}}
    public class RequireComponent:System.Attribute{public RequireComponent(System.Type t){}}
    public class Object {
        public static readonly System.Collections.Generic.List<object> All=new System.Collections.Generic.List<object>();
        public static T FindFirstObjectByType<T>() where T:class {foreach(var x in All)if(x is T)return (T)x;return null;}
        public static void Destroy(object o){} public static void DontDestroyOnLoad(object o){}
    }
    public class GameObject:Object {
        public string name;public GameObject(string n=""){name=n;}
        readonly System.Collections.Generic.Dictionary<System.Type,object> parts=new System.Collections.Generic.Dictionary<System.Type,object>();
        public T AddComponent<T>() where T:MonoBehaviour,new(){var c=new T();c.gameObject=this;parts[typeof(T)]=c;All.Add(c);return c;}
        public T GetComponent<T>() where T:class {object c;return parts.TryGetValue(typeof(T),out c)?c as T:null;}
    }
    public class MonoBehaviour:Object {public GameObject gameObject;public string name=>gameObject.name;public T GetComponent<T>() where T:class{return gameObject.GetComponent<T>();}}
    public static class Mathf {public static int Max(int a,int b){return System.Math.Max(a,b);}}
    public static class Debug {public static void Log(object o,params object[] args){}public static void LogWarning(object o,params object[] args){}public static void LogError(object o,params object[] args){}}
}
public enum Element {Phys}
public class LevelGenerator:UnityEngine.MonoBehaviour{}
public class ZoneManager:UnityEngine.MonoBehaviour {
    public int zoneLevel;public void GenerateZone(){}public int GetSeedForZone(int l){return l;}
    public void RewardZoneClear(int l){} public bool ShouldOfferPrestige(int l){return false;}public void GrantPrestigeRewards(int l){}
    // PRODUCTION_QUOTA
}
public class PlayerProgression:UnityEngine.MonoBehaviour {public int awards;public void ResetProgression(){awards=0;}public void AwardEnemy(int l,bool b){awards++;}}
public class Gear:UnityEngine.MonoBehaviour{}
public class Inventory:UnityEngine.MonoBehaviour {public static Inventory Instance;public void Pickup(Gear g){}}
public class LootManager:UnityEngine.MonoBehaviour {public int awards;public Gear GenerateLoot(EnemyAI.EnemyRarity r){awards++;return null;}}
public class DeathMenuUI:UnityEngine.MonoBehaviour {public void Hide(){}public void Show(int l,EnemyAI.EnemyRarity r,Element e){}}
public class EnemyAI:UnityEngine.MonoBehaviour {public enum EnemyRarity{Normal} public EnemyRarity CurrentRarity;public int EnemyLevel;public Element WeaponMainElement;}
public class HealthComponent:UnityEngine.MonoBehaviour {
    private bool isEnemy=true,isDead,deathRewardClaimed;
    public bool IsBoss;public void MarkDead(){isDead=true;}
    // PRODUCTION_CLAIM
}
public class BattleManager:UnityEngine.MonoBehaviour {
    public static BattleManager Instance;public EnemyAI CurrentEnemyAI;public int spawns;
    public void BeginZone(int l,bool startWithBoss=false){SpawnNextEnemy(startWithBoss);}
    public void SpawnNextEnemy(bool spawnBoss=false){
        var go=new UnityEngine.GameObject(spawnBoss?"Hobgoblin":"Goblin");
        var hp=go.AddComponent<HealthComponent>();hp.IsBoss=spawnBoss;
        CurrentEnemyAI=go.AddComponent<EnemyAI>();CurrentEnemyAI.EnemyLevel=GameManager.Instance.CurrentCombatLevel;spawns++;
    }
    public void RespawnPlayerAtLevelStart(){SpawnNextEnemy(false);}
}
public static class BossCadenceHarness {
    static int assertions;static void Check(bool ok,string message){assertions++;if(!ok)throw new System.Exception(message);}
    static void Invoke(object target,string name){target.GetType().GetMethod(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(target,null);}
    static HealthComponent Current(){return BattleManager.Instance.CurrentEnemyAI.GetComponent<HealthComponent>();}
    public static string Run(){
        var zone=new UnityEngine.GameObject().AddComponent<ZoneManager>();
        var loot=new UnityEngine.GameObject().AddComponent<LootManager>();
        BattleManager.Instance=new UnityEngine.GameObject().AddComponent<BattleManager>();
        var go=new UnityEngine.GameObject();var xp=go.AddComponent<PlayerProgression>();var game=go.AddComponent<GameManager>();Invoke(game,"Awake");game.StartNewRun();
        for(int level=1;level<=121;level++){
            Check(game.CurrentCombatLevel==level&&zone.zoneLevel==level,"Combat level advance mismatch");
            Check(game.NormalKillsRequired==9,"Nine normal kills must precede stage10");
            for(int encounter=1;encounter<=10;encounter++){
                var health=Current();Check(game.EncounterStage==encounter,"HUD encounter number mismatch");
                Check(health.IsBoss==(encounter==10)&&game.BossActive==(encounter==10),"Boss appeared outside stage10");
                int spawns=BattleManager.Instance.spawns,awards=xp.awards;
                game.OnEnemyKilled(health,!health.IsBoss);Check(BattleManager.Instance.spawns==spawns&&xp.awards==awards,"Live death callback granted progression");
                health.MarkDead();game.OnEnemyKilled(health,!health.IsBoss);
                Check(xp.awards==awards+1&&loot.awards==xp.awards,"One reward per actual death");
                var replacement=Current();int nextLevel=game.CurrentCombatLevel;
                game.OnEnemyKilled(health,health.IsBoss);
                Check(Current()==replacement&&game.CurrentCombatLevel==nextLevel&&xp.awards==awards+1,"Duplicate death advanced encounter");
            }
        }
        foreach(int level in new[]{1,10,11,20,21,50,51,60,61,120,121}){
            game.StartZone(level);
            for(int i=0;i<9;i++){var h=Current();h.MarkDead();game.OnEnemyKilled(h,false);}
            Check(Current().IsBoss&&game.EncounterStage==10,"Direct-load boss boundary failed");
            int saved=xp.awards;game.RestartCurrentLevelAfterDeath();
            Check(game.CurrentCombatLevel==level&&game.EncounterStage==1&&!Current().IsBoss&&xp.awards==saved,"Boss death/restart did not reset encounter only");
        }
        return "PASS: "+assertions+" assertions using actual GameManager + production ZoneManager quota and HealthComponent death claim. Levels1-121: stages1-9 normal, stage10 boss, single rewards/duplicate guards, next-level reset, direct boundary loads and boss restart. Unity runtime not tested.";
    }
}
