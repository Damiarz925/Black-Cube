using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    public sealed class BalanceWorkbenchChart
    {
        Vector2 pan;float zoom=1f;readonly Dictionary<string,bool> visible=new();
        public void Draw(Rect rect,IReadOnlyList<CurveSeries> series,string xLabel,string yLabel)
        {
            GUI.Box(rect,GUIContent.none,EditorStyles.helpBox);if(series==null||series.Count==0||series.All(s=>s.points.Count==0)){GUI.Label(rect,"No chart data",Centered());return;}
            var points=series.SelectMany(s=>s.points).ToArray();double xmin=points.Min(x=>x.x),xmax=points.Max(x=>x.x),ymin=points.Min(x=>x.mean),ymax=points.Max(x=>x.mean);if(xmax<=xmin)xmax=xmin+1;if(ymax<=ymin)ymax=ymin+1;
            Rect plot=new(rect.x+48,rect.y+18,rect.width-62,rect.height-48);GUI.Box(plot,GUIContent.none);Handles.BeginGUI();foreach(var s in series){if(!visible.ContainsKey(s.name))visible[s.name]=s.visible;if(!visible[s.name]||s.points.Count<1)continue;Handles.color=s.color;Vector3[] line=s.points.Select(p=>Map(p.x,p.mean,plot,xmin,xmax,ymin,ymax)).ToArray();if(line.Length>1)Handles.DrawAAPolyLine(2.5f,line);foreach(var p in line)Handles.DrawSolidDisc(p,Vector3.forward,2.5f);}Handles.EndGUI();
            GUI.Label(new Rect(plot.x,rect.y,plot.width,18),$"{yLabel}   {ymin:0.###} … {ymax:0.###}");GUI.Label(new Rect(plot.x,plot.yMax+3,plot.width,18),$"{xLabel}: {xmin:0.##} … {xmax:0.##}",Centered());
            float lx=plot.x;foreach(var s in series){bool v=visible.TryGetValue(s.name,out bool on)&&on;GUI.color=s.color;bool next=GUI.Toggle(new Rect(lx,rect.yMax-20,120,18),v,s.name);GUI.color=Color.white;visible[s.name]=next;lx+=122;}
            var e=Event.current;if(plot.Contains(e.mousePosition)){if(e.type==EventType.ScrollWheel){zoom=Mathf.Clamp(zoom*(1-e.delta.y*.08f),.25f,8f);e.Use();}if(e.type==EventType.MouseDrag&&e.button==2){pan+=e.delta;e.Use();}}
        }
        Vector3 Map(double x,double y,Rect r,double xmin,double xmax,double ymin,double ymax){float nx=(float)((x-xmin)/(xmax-xmin)-.5)*zoom+.5f+pan.x/r.width;float ny=(float)((y-ymin)/(ymax-ymin)-.5)*zoom+.5f-pan.y/r.height;return new Vector3(Mathf.Lerp(r.xMin,r.xMax,nx),Mathf.Lerp(r.yMax,r.yMin,ny));}
        public void Reset(){pan=Vector2.zero;zoom=1;}
        public string ExportPng(IReadOnlyList<CurveSeries> series,string name,int width=1200,int height=700)
        {
            Directory.CreateDirectory(WorkbenchExports.Root);var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);var bg=new Color32(24,25,30,255);var pixels=Enumerable.Repeat(bg,width*height).ToArray();tex.SetPixels32(pixels);if(series!=null){var pts=series.SelectMany(x=>x.points).ToArray();if(pts.Length>0){double xmin=pts.Min(x=>x.x),xmax=pts.Max(x=>x.x),ymin=pts.Min(x=>x.mean),ymax=pts.Max(x=>x.mean);if(xmax<=xmin)xmax=xmin+1;if(ymax<=ymin)ymax=ymin+1;foreach(var s in series){for(int i=1;i<s.points.Count;i++){int x0=(int)((s.points[i-1].x-xmin)/(xmax-xmin)*(width-1)),y0=(int)((s.points[i-1].mean-ymin)/(ymax-ymin)*(height-1));int x1=(int)((s.points[i].x-xmin)/(xmax-xmin)*(width-1)),y1=(int)((s.points[i].mean-ymin)/(ymax-ymin)*(height-1));Line(tex,x0,y0,x1,y1,s.color);}}}}tex.Apply();string path=$"{WorkbenchExports.Root}/{name}_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.Refresh();return path;
        }
        static void Line(Texture2D t,int x0,int y0,int x1,int y1,Color c){int dx=Mathf.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Mathf.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;while(true){if(x0>=0&&y0>=0&&x0<t.width&&y0<t.height)t.SetPixel(x0,y0,c);if(x0==x1&&y0==y1)break;int e=2*err;if(e>=dy){err+=dy;x0+=sx;}if(e<=dx){err+=dx;y0+=sy;}}}
        static GUIStyle Centered(){var s=new GUIStyle(EditorStyles.centeredGreyMiniLabel){alignment=TextAnchor.MiddleCenter};return s;}
    }
}
