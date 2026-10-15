// Developer map: Minimal Unity stubs and level-boundary assertions compiled with actual ZoneManager by verify_forest_cycle.ps1.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
// File-only substitutes for exercising the actual ZoneManager without Unity.
namespace UnityEngine {
    public class SerializeField:System.Attribute{}
    public class HeaderAttribute:System.Attribute{public HeaderAttribute(string s){}}
    public class MinAttribute:System.Attribute{public MinAttribute(int n){}}
    public class MonoBehaviour {protected static T FindFirstObjectByType<T>() where T:class{return null;}}
    public class Sprite {public string name;}
    public class SpriteRenderer {public Sprite sprite;}
    public class Material {}
    public struct Vector3{}
    public struct Quaternion{}
    public class Transform {public Vector3 position,localScale;public Quaternion rotation;}
    public class GameObject {public Transform transform=new Transform();public void SetActive(bool b){}}
    public static class Mathf {
        public static int Max(int a,int b)=>System.Math.Max(a,b);
        public static float Max(float a,float b)=>System.Math.Max(a,b);
        public static int Min(int a,int b)=>System.Math.Min(a,b);
    }
    public static class Random {public static void InitState(int n){}public static int Range(int a,int b)=>a;public static float value=.5f;}
    public static class Debug {public static void Log(string s,params object[] o){}public static void LogWarning(string s,params object[] o){}public static void LogError(string s,params object[] o){}}
    public static class RenderSettings {public static Material skybox;}
    public static class DynamicGI {public static void UpdateEnvironment(){}}
}
public class ThemeDefinition {public float Weight=1;public UnityEngine.Material SkyboxMaterial;}
public struct ForcedThemeEntry {public int LevelIndex;public ThemeDefinition Theme;}
public class ThemeSet {public System.Collections.Generic.List<ForcedThemeEntry> ForcedThemes;public System.Collections.Generic.List<ThemeDefinition> Themes;}
public class SpawnAnchorGroup{}
public struct LevelBounds{public float MinZ,MaxZ,GroundY;}
public struct Placement {public UnityEngine.GameObject Prefab;public string PoolKey;public UnityEngine.Vector3 Position,Scale;public UnityEngine.Quaternion Rotation;}
public class LevelPlan {public ThemeDefinition Theme;public System.Collections.Generic.List<Placement> TreePlacements,RockPlacements,TerrainPlacements;}
public class LevelGenerator {public LevelPlan BuildLevelPlan(int level,int seed,ThemeDefinition theme,SpawnAnchorGroup a,LevelBounds b)=>new LevelPlan();}
public class Pool {public UnityEngine.GameObject Get(string s,UnityEngine.GameObject p)=>p;public void Release(UnityEngine.GameObject g){}}

public static class ForestCycleHarness {
    static int count;
    static void Check(bool b,string s){count++;if(!b)throw new System.Exception(s);}
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(o,v);
    public static string Run(){
        var zone=new ZoneManager();var renderer=new UnityEngine.SpriteRenderer();var sprites=new UnityEngine.Sprite[6];
        for(int i=0;i<6;i++)sprites[i]=new UnityEngine.Sprite{name="Forest"+(i+1)};
        Set(zone,"generate3DScenery",false);zone.ConfigurePaperBackgrounds(renderer,sprites);
        Check(renderer.sprite==sprites[0],"Startup/default level did not select Forest1");
        for(int repeat=0;repeat<3;repeat++)for(int forest=0;forest<6;forest++)for(int step=1;step<=10;step++){
            int level=repeat*60+forest*10+step;zone.zoneLevel=level;zone.GenerateZone();
            Check(renderer.sprite==sprites[forest],"Wrong background at level"+level);
            Check(zone.ZoneName=="Forest "+(forest+1),"Wrong forest label at"+level);
            Check(zone.StageNumber==step,"Stage counter changed at"+level);
            Check(zone.LocationLabel=="Forest "+(forest+1)+" · Level "+level,"Wrong level label at"+level);
        }
        foreach(int invalid in new[]{0,-1,int.MinValue}){zone.zoneLevel=invalid;zone.GenerateZone();Check(renderer.sprite==sprites[0],"Invalid level not clamped");}
        zone.zoneLevel=61;zone.GenerateZone();Check(renderer.sprite==sprites[0],"60→61 wrap failed");
        zone.zoneLevel=51;zone.GenerateZone();Check(renderer.sprite==sprites[5],"Direct load of Forest6 failed");
        zone.zoneLevel=1;zone.GenerateZone();Check(renderer.sprite==sprites[0],"New-run reset failed");
        var legacy=new ZoneManager();legacy.zoneLevel=11;Check(legacy.ZoneName=="Desert"&&legacy.LocationLabel=="Desert 1","Unconfigured legacy behavior changed");
        zone.ConfigurePaperBackgrounds(null,sprites);zone.GenerateZone();
        zone.ConfigurePaperBackgrounds(renderer,new UnityEngine.Sprite[0]);zone.GenerateZone();
        Check(ZoneManager.ForestBackgroundIndex(int.MaxValue)>=0&&ZoneManager.ForestBackgroundIndex(int.MaxValue)<6,"Large level overflow");
        return "PASS: "+count+" assertions on actual ZoneManager: every level1–180, ten-level boundaries,60-level loops, direct loads, reset, invalid/large levels, renderer updates with3D generation disabled, HUD labels and legacy fallback. No Unity runtime launched.";
    }
}
