// Passive Tree V3 structural validator and real-UI capture harness.
using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;
public static class PassiveTreeV3Validation
{
 public static List<string> Validate()
 {
  var errors=new List<string>();
  if(PassiveTreeDefinition.ClassIds.Count!=6)errors.Add("Expected six class routes.");if(PassiveTreeDefinition.WeaponIds.Count!=6)errors.Add("Expected six weapon routes.");
  if(PassiveTreeDefinition.Nodes.Any(x=>x.StableId.StartsWith("tree.v2.",StringComparison.Ordinal)))errors.Add("Active V2 stable ID found.");
  if(PassiveTreeDefinition.Nodes.Select(x=>x.StableId).Distinct().Count()!=PassiveTreeDefinition.NodeCount)errors.Add("Duplicate V3 stable ID.");
  foreach(string c in PassiveTreeDefinition.ClassIds)
  {
   var route=PassiveTreeDefinition.RouteNodes(c).Select(PassiveTreeDefinition.Node).ToArray();if(route.Count(x=>x.Kind==PassiveNodeKind.Spine)!=10)errors.Add(c+" spine count is not 10.");
   for(int t=1;t<=10;t++){var tier=route.Where(x=>x.Tier==t).ToArray();if(tier.Count(x=>x.Kind==PassiveNodeKind.Choice)!=6||tier.Count(x=>x.IsSubclassChoice)!=2||tier.Where(x=>x.IsChoice).Select(x=>x.ChoiceGroupId).Distinct().Count()!=2)errors.Add($"{c} tier {t} choice structure invalid.");}
  }
  foreach(string w in PassiveTreeDefinition.WeaponIds)
  {
   var route=PassiveTreeDefinition.RouteNodes(null,w).Select(PassiveTreeDefinition.Node).ToArray();if(route.Count(x=>x.Kind==PassiveNodeKind.WeaponSpine)!=5||route.Count(x=>x.Kind==PassiveNodeKind.Choice)!=30)errors.Add(w+" route structure invalid.");if(route.Any(x=>x.IsSubclassChoice||x.WeaponTypeRestriction!=w))errors.Add(w+" restriction/subclass metadata invalid.");
  }
  if(PassiveTreeDefinition.Edges.Count!=PassiveTreeDefinition.NodeCount-6)errors.Add("V3 route edge count invalid.");
  for(int a=0;a<PassiveTreeDefinition.NodeCount;a++)for(int b=a+1;b<PassiveTreeDefinition.NodeCount;b++)if(Vector2.Distance(PassiveTreeDefinition.Node(a).LayoutPosition,PassiveTreeDefinition.Node(b).LayoutPosition)<58f){errors.Add($"Node overlap: {PassiveTreeDefinition.Node(a).StableId} / {PassiveTreeDefinition.Node(b).StableId}");return errors;}
  return errors;
 }
 [MenuItem("Black Cube/Validation/Passive Tree V3 Structure")]
 public static void RunStructure(){var errors=Validate();Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/PassiveTreeV3Structure.txt",errors.Count==0?new[]{"PASS: Passive Tree V3 structure, IDs, choices, weapon restrictions, and route counts are valid."}:errors);if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));Debug.Log("PASSIVE TREE V3 STRUCTURE: PASS");}
}

[InitializeOnLoad]public static class PassiveTreeVisualValidation
{
 const string Key="BlackCube.PassiveTreeV3.Visual",Folder="ReviewCaptures";static int frame,phase;static SkillTreeUI tree;static PlayerProgression progression;static ScrollRect scroll;
 static PassiveTreeVisualValidation(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Wait;}
 [MenuItem("Black Cube/Validation/Passive Tree V3 Captures")]public static void Run(){var errors=PassiveTreeV3Validation.Validate();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));Directory.CreateDirectory(Folder);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.EnterPlaymode();}
 static void Wait(){if(!EditorApplication.isPlaying)return;EditorApplication.update-=Wait;frame=phase=0;EditorApplication.update+=Tick;}
 static void Tick(){if(++frame<25)return;try{if(tree==null){tree=UnityEngine.Object.FindFirstObjectByType<SkillTreeUI>();progression=UnityEngine.Object.FindFirstObjectByType<PlayerProgression>();if(tree==null||progression==null){if(frame<300)return;throw new InvalidOperationException("Runtime passive UI did not initialize.");}var identity=GameManager.Instance.GetComponent<PlayerIdentityState>();identity.BeginNewGame(PlayerClassIds.Warrior);identity.CompleteMilestone(PlayerIdentityState.StoryCompletionMilestoneId);identity.SelectSubclass(SubclassIds.WarriorBleed);AssertHierarchy();tree.Toggle();scroll=(ScrollRect)typeof(SkillTreeUI).GetField("scroll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tree);SetZoom(.45f);Focus(PlayerClassIds.Warrior,1450);frame=0;return;}if(frame<20)return;switch(phase++){case 0:Capture("PassiveTreeV3-WarriorNative.png");Focus(PlayerClassIds.Mage,1450);break;case 1:Capture("PassiveTreeV3-WarriorViewingMage.png");AllocateWarriorAndSword();Focus(PlayerClassIds.Warrior,3400);break;case 2:Capture("PassiveTreeV3-SwordRoute.png");SetZoom(.14f);scroll.content.anchoredPosition=Vector2.zero;break;case 3:Capture("PassiveTreeV3-Overview.png");Finish(0);return;}frame=0;}catch(Exception ex){Debug.LogException(ex);Finish(1);}}
 static void AssertHierarchy(){foreach(var n in PassiveTreeDefinition.Nodes){var b=tree.NodeButton(n.Id);if(b==null||b.image==null||b.image.sprite==null)throw new InvalidOperationException("Missing node UI "+n.StableId);float expected=PassiveTreeDefinition.IsKeystone(n.Id)?156:n.Size==PassiveNodeSize.Small?58:n.Size==PassiveNodeSize.Medium?78:108;if(Vector2.Distance(((RectTransform)b.transform).sizeDelta,Vector2.one*expected)>.1f)throw new InvalidOperationException("Node size mismatch "+n.StableId);}if(tree.Panel.transform.Find("Tree Viewport/Radial Tree Content/Central Hub")==null)throw new InvalidOperationException("Non-allocatable central hub missing.");if(tree.JunctionCount!=180)throw new InvalidOperationException($"Expected 180 non-allocatable branch junctions, found {tree.JunctionCount}.");var content=tree.Panel.transform.Find("Tree Viewport/Radial Tree Content");int junctionCount=0;foreach(Transform child in content)if(child.name.StartsWith("Branch Junction ",StringComparison.Ordinal)){junctionCount++;var image=child.GetComponent<Image>();if(image==null||image.raycastTarget)throw new InvalidOperationException("Branch junction must be a non-interactive image: "+child.name);}if(junctionCount!=180)throw new InvalidOperationException($"Expected 180 rendered branch junctions, found {junctionCount}.");}
 static void AllocateWarriorAndSword(){typeof(PlayerProgression).GetField("availablePoints",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progression,100);for(int t=1;t<=10;t++)if(!progression.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Warrior,t)))throw new InvalidOperationException("Warrior spine allocation failed at "+t);for(int t=1;t<=5;t++)if(!progression.TrySpend(PassiveTreeDefinition.WeaponSpineNode(WeaponTypeIds.Sword,t)))throw new InvalidOperationException("Sword spine allocation failed at "+t);}
 static void Focus(string classId,float radius){Vector2 d=PassiveTreeDefinition.Node(PassiveTreeDefinition.ClassSpineNode(classId,1)).LayoutPosition.normalized;scroll.content.anchoredPosition=-d*radius;Canvas.ForceUpdateCanvases();}
 static void SetZoom(float value){typeof(SkillTreeUI).GetField("treeZoom",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(tree,value);scroll??=(ScrollRect)typeof(SkillTreeUI).GetField("scroll",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tree);scroll.content.localScale=Vector3.one*value;Canvas.ForceUpdateCanvases();}
 static void Capture(string name){string path=Path.GetFullPath(Path.Combine(Folder,name));ScreenCapture.CaptureScreenshot(path,1);Debug.Log("PASSIVE TREE V3 CAPTURE: "+path);}
 static void Finish(int code){SessionState.EraseBool(Key);EditorApplication.update-=Tick;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
}
