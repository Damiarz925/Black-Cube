using UnityEditor;
using UnityEngine;

public sealed class WorldContentInspectionWindow:EditorWindow
{
    Vector2 scroll;int biomeIndex,locationIndex,corruptionIndex,stage=1;
    [MenuItem("Black Cube/Content/V1 World Inspector")]public static void Open()=>GetWindow<WorldContentInspectionWindow>("V1 World Content");
    void OnGUI()
    {
        var db=WorldContentCatalog.Reference;if(db==null){EditorGUILayout.HelpBox("World content is unavailable.",MessageType.Error);return;}
        EditorGUILayout.LabelField("Step 19 Production Catalog",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("48 enemies · 60 main bosses · 6 challenge bosses · 3,600 stage resolutions");
        biomeIndex=EditorGUILayout.Popup("Biome",biomeIndex,db.biomes.ConvertAll(x=>x.displayName).ToArray());
        locationIndex=EditorGUILayout.IntSlider("Location",locationIndex,0,9);corruptionIndex=EditorGUILayout.IntSlider("Corruption tier",corruptionIndex,0,5);stage=EditorGUILayout.IntSlider("Stage",stage,1,10);
        int level=biomeIndex*60+corruptionIndex*10+locationIndex+1;var world=WorldProgression.Resolve(level,stage,db);
        EditorGUILayout.Space();EditorGUILayout.LabelField($"Level {level} · {world.LocationLabel} · {world.CorruptionLabel}",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Encounter",world.Encounter?.stableId??"missing");
        if(world.Encounter?.kind==EncounterKind.Boss){var boss=db.Boss(world.Encounter.bossId);EditorGUILayout.LabelField("Boss",boss?.displayName??"missing");EditorGUILayout.LabelField("Phase profile",boss?.phaseProfileId??"missing");}
        else{var enemy=db.Enemy(world.Encounter?.enemyArchetypeId);EditorGUILayout.LabelField("Enemy",enemy?.displayName??"missing");EditorGUILayout.LabelField("Rank",enemy?.rank.ToString()??"missing");EditorGUILayout.LabelField("Loadout",enemy?.skillLoadoutId??"missing");}
        scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.Space();EditorGUILayout.LabelField("Challenges",EditorStyles.boldLabel);foreach(var c in db.challengeEncounters)EditorGUILayout.LabelField($"{c.displayName}: {c.entryResourceId} → {c.rewardResourceId}");EditorGUILayout.EndScrollView();
    }
}
