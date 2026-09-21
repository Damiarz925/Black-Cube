using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
public static class Step17TestRunner
{
 public static void RunValidation()=>Run(()=>{WritePassiveTreeReport();ItemizationValidationRunner.RunStep14_5();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();});
 public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildPassiveTreeV3Windows);
 [MenuItem("Black Cube/Validation/Write Passive Tree V3 Report")]public static void WritePassiveTreeReport()
 {
  var errors=PassiveTreeV3Validation.Validate();var lines=new[]{"# Passive Tree V3 Generated Layout Report","",$"Definitions: {PassiveTreeDefinition.NodeCount}",$"Edges: {PassiveTreeDefinition.Edges.Count}",$"Class routes: {PassiveTreeDefinition.ClassIds.Count}",$"Weapon routes: {PassiveTreeDefinition.WeaponIds.Count}",$"Class spine tiers: {PassiveTreeDefinition.ClassTierCount}",$"Weapon spine tiers: {PassiveTreeDefinition.WeaponTierCount}",$"Weapon premium: {PassiveTreeDefinition.WeaponSpecificEfficiencyMultiplier:0.00}","",errors.Count==0?"PASS":"FAIL: "+string.Join("; ",errors)};Directory.CreateDirectory("ReviewCaptures");File.WriteAllLines("ReviewCaptures/PassiveTreeV3LayoutReport.md",lines);AssetDatabase.Refresh();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));Debug.Log("PASSIVE TREE V3 REPORT: "+Path.GetFullPath("ReviewCaptures/PassiveTreeV3LayoutReport.md"));
 }
 static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
