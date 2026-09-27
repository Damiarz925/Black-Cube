using System;
using System.IO;
using System.Linq;
using BlackCube.BalanceWorkbench;
using UnityEditor;
using UnityEngine;

public static class WorkbenchTrustBaselineRunner
{
    public static void ReproduceLegacyScenarioLevelMapping()
    {
        PlayerGearProfileSO profile=null;
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<PlayerGearProfileSO>(
                "Assets/Balance/Profiles/SO_PlayerGearProfile_Low.asset");
            if(source==null)throw new InvalidOperationException("Missing Low gear profile.");
            profile=UnityEngine.Object.Instantiate(source);
            profile.name=source.name;
            profile.candidatesPerSlot=1;
            profile.candidateRetention=1;
            profile.gearsetBeamWidth=1;
            var template=new PlayerBuildSnapshot
            {
                playerLevel=50,combatLevel=50,classId=PlayerClassIds.Warrior,
                weaponTypeId=WeaponTypeIds.Sword,seed=41001
            };
            var objective=new OptimizationObjective
            {
                primary="total_dps",secondary="life",primaryWeight=.7f
            };
            var result=PlayerScenarioSweep.Run(template,new[]{profile},objective,
                10,50,10,false,true,true,1,null,null,
                CombatLevelSweepPolicy.AdvanceFromStart);
            Directory.CreateDirectory("Logs/BalanceWorkbenchPerformance");
            File.WriteAllLines("Logs/BalanceWorkbenchPerformance/scenario_mapping_legacy.txt",
                result.points.Select(x=>$"L{x.playerLevel}/CL{x.combatLevel} {x.profile} seed={x.seed}"));
            if(result.points.Count!=5)throw new InvalidOperationException("Expected five sweep points.");
            EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        finally{if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);}
    }
}
