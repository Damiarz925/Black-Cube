using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Step17TestRunner
{
    public static void RunValidation()=>Run(()=>{WritePassiveTreeReport();ItemizationValidationRunner.RunStep14_5();WorldContentValidationRunner.ValidateReferenceContent();BaselineVerificationRunner.ValidateReferences();});
    public static void BuildWindows()=>Run(BaselineVerificationRunner.BuildStep17Windows);
    [MenuItem("Black Cube/Validation/Write Passive Tree V2 Report")]
    public static void WritePassiveTreeReport()
    {
        var errors=new List<string>();var lines=new List<string>{"# Passive Tree V2 Generated Layout Report","",$"Total nodes: {PassiveTreeDefinition.NodeCount}",$"Edges: {PassiveTreeDefinition.Edges.Count}",$"Weapon-specific efficiency multiplier: {PassiveTreeDefinition.WeaponSpecificEfficiencyMultiplier:0.00}","","## Region composition",""};
        foreach(PassiveRegion region in Enum.GetValues(typeof(PassiveRegion))){var regionNodes=PassiveTreeDefinition.Nodes.Where(x=>x.Region==region).ToArray();lines.Add($"- {region}: {regionNodes.Length} nodes; average radius {(regionNodes.Length==0?0:regionNodes.Average(x=>x.LayoutPosition.magnitude)):0.0}");if(regionNodes.Length==0)errors.Add($"Empty region: {region}");}
        lines.AddRange(new[]{"","## Class starts",""});foreach(var cls in PlayerClassCatalog.All){var node=PassiveTreeDefinition.Node(PassiveTreeDefinition.StartNodeId(cls.Id));lines.Add($"- {cls.DisplayName}: `{node.StableId}` at ({node.LayoutPosition.x:0}, {node.LayoutPosition.y:0})");}
        lines.AddRange(new[]{"","## Keystones",""});foreach(var node in PassiveTreeDefinition.Nodes.Where(x=>x.Kind==PassiveNodeKind.Keystone))lines.Add($"- {node.DisplayName}: `{node.StableId}` — {node.Description}");
        lines.AddRange(new[]{"","## Weapon districts and clusters",""});foreach(string weapon in WeaponTypeCatalog.All.Select(x=>x.Id)){var list=PassiveTreeDefinition.Nodes.Where(x=>x.WeaponTypeRestriction==weapon).ToArray();var average=list.Aggregate(Vector2.zero,(sum,x)=>sum+x.LayoutPosition)/Mathf.Max(1,list.Length);lines.Add($"- {WeaponTypeCatalog.Get(weapon).DisplayName}: {list.Length} nodes; centroid ({average.x:0}, {average.y:0})");foreach(var group in list.GroupBy(x=>x.ExtensionMetadata.StableSectionId))lines.Add($"  - `{group.Key}`: {string.Join(", ",group.Select(x=>x.DisplayName))}");}
        int invalidEdges=0,oneWay=0;var duplicateEdges=new HashSet<string>();foreach(var edge in PassiveTreeDefinition.Edges){if(edge.A<0||edge.A>=PassiveTreeDefinition.NodeCount||edge.B<0||edge.B>=PassiveTreeDefinition.NodeCount||edge.A==edge.B){invalidEdges++;continue;}string key=Math.Min(edge.A,edge.B)+":"+Math.Max(edge.A,edge.B);if(!duplicateEdges.Add(key))errors.Add("Duplicate graph edge: "+key);if(!PassiveTreeDefinition.AdjacentNodeIds(edge.A).Contains(edge.B)||!PassiveTreeDefinition.AdjacentNodeIds(edge.B).Contains(edge.A))oneWay++;}
        int disconnected=PassiveTreeDefinition.Nodes.Count(x=>PassiveTreeDefinition.AdjacentNodeIds(x.Id).Count==0);var reached=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(PassiveTreeDefinition.StartNodeId(PlayerClassIds.Warrior));reached.Add(queue.Peek());while(queue.Count>0){foreach(int next in PassiveTreeDefinition.AdjacentNodeIds(queue.Dequeue()))if(reached.Add(next))queue.Enqueue(next);}int unreachable=PassiveTreeDefinition.NodeCount-reached.Count;
        int exactOverlaps=0,nearOverlaps=0;for(int a=0;a<PassiveTreeDefinition.NodeCount;a++)for(int b=a+1;b<PassiveTreeDefinition.NodeCount;b++){float distance=Vector2.Distance(PassiveTreeDefinition.Node(a).LayoutPosition,PassiveTreeDefinition.Node(b).LayoutPosition);if(distance<1f)exactOverlaps++;else if(distance<45f)nearOverlaps++;}
        lines.AddRange(new[]{"","## Graph and layout validation","",$"- Invalid/self edges: {invalidEdges}",$"- One-way adjacency errors: {oneWay}",$"- Disconnected nodes: {disconnected}",$"- Unreachable nodes: {unreachable}",$"- Exact overlaps (<1 unit): {exactOverlaps}",$"- Near overlaps (1–45 units, manual-review warning): {nearOverlaps}"});
        if(invalidEdges+oneWay+disconnected+unreachable+exactOverlaps>0)errors.Add("Graph/layout integrity counts are nonzero.");
        lines.AddRange(new[]{"","## All stable node IDs",""});foreach(var node in PassiveTreeDefinition.Nodes)lines.Add($"- `{node.StableId}` — {node.Region} / {node.Kind} / {node.DisplayName} / ({node.LayoutPosition.x:0}, {node.LayoutPosition.y:0})");
        lines.AddRange(new[]{"","## Result","",errors.Count==0?"PASS":"FAIL: "+string.Join("; ",errors)});Directory.CreateDirectory("ReviewCaptures");File.WriteAllLines("ReviewCaptures/PassiveTreeV2LayoutReport.md",lines);WriteTreeOverviewPng();AssetDatabase.Refresh();Debug.Log("PASSIVE TREE V2 REPORT: "+Path.GetFullPath("ReviewCaptures/PassiveTreeV2LayoutReport.md"));if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
    }
    static void WriteTreeOverviewPng()
    {
        const int size=1600;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var background=new Color32[size*size];for(int i=0;i<background.Length;i++)background[i]=new Color32(7,9,14,255);texture.SetPixels32(background);
        Vector2 Pixel(Vector2 p)=>new(size*.5f+p.x/5600f*(size-120),size*.5f+p.y/5600f*(size-120));
        foreach(var edge in PassiveTreeDefinition.Edges){Vector2 a=Pixel(PassiveTreeDefinition.Node(edge.A).LayoutPosition),b=Pixel(PassiveTreeDefinition.Node(edge.B).LayoutPosition);Line(texture,Mathf.RoundToInt(a.x),Mathf.RoundToInt(a.y),Mathf.RoundToInt(b.x),Mathf.RoundToInt(b.y),new Color32(50,58,73,255));}
        var colors=new[]{new Color32(214,81,72,255),new Color32(82,183,114,255),new Color32(152,83,204,255),new Color32(75,126,230,255),new Color32(229,215,114,255),new Color32(220,135,52,255)};
        foreach(var node in PassiveTreeDefinition.Nodes){Vector2 p=Pixel(node.LayoutPosition);Color32 color=node.Kind==PassiveNodeKind.ClassStart?new Color32(255,255,255,255):node.Kind==PassiveNodeKind.Keystone?new Color32(255,52,76,255):!string.IsNullOrEmpty(node.WeaponTypeRestriction)?new Color32(255,190,55,255):node.Region==PassiveRegion.Center?new Color32(78,212,220,255):colors[Mathf.Clamp((int)node.Region,0,5)];int radius=node.Kind is PassiveNodeKind.ClassStart or PassiveNodeKind.Keystone?9:node.Kind==PassiveNodeKind.Notable?6:4;Disc(texture,Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),radius,color);}
        texture.Apply(false,false);File.WriteAllBytes("ReviewCaptures/PassiveTreeV2Overview.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
    }
    static void Line(Texture2D texture,int x0,int y0,int x1,int y1,Color32 color){int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,error=dx+dy;while(true){if(x0>=0&&x0<texture.width&&y0>=0&&y0<texture.height)texture.SetPixel(x0,y0,color);if(x0==x1&&y0==y1)break;int twice=2*error;if(twice>=dy){error+=dy;x0+=sx;}if(twice<=dx){error+=dx;y0+=sy;}}}
    static void Disc(Texture2D texture,int cx,int cy,int radius,Color32 color){for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++)if(x*x+y*y<=radius*radius&&cx+x>=0&&cx+x<texture.width&&cy+y>=0&&cy+y<texture.height)texture.SetPixel(cx+x,cy+y,color);}
    static void Run(Action action){try{action();EditorApplication.Exit(0);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
}
