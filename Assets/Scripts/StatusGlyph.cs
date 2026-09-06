using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class StatusGlyph : MaskableGraphic
{
    public string kind;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(kind=="Chill")
        {for(int i=0;i<3;i++){float a=i*Mathf.PI/3;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.45f;Line(vh,new Vector2(.5f,.5f)-d,new Vector2(.5f,.5f)+d);}return;}
        Vector2[] points=kind=="Shock"?new[]{new Vector2(.65f,1),new Vector2(.12f,.4f),new Vector2(.48f,.4f),new Vector2(.35f,0),new Vector2(.88f,.6f),new Vector2(.52f,.6f)}:
            kind=="Burn"?new[]{new Vector2(.5f,1),new Vector2(.32f,.52f),new Vector2(.2f,.72f),new Vector2(.08f,.28f),new Vector2(.28f,.04f),new Vector2(.72f,.04f),new Vector2(.92f,.28f),new Vector2(.8f,.72f),new Vector2(.65f,.48f)}:
            new[]{new Vector2(.5f,1),new Vector2(.16f,.43f),new Vector2(.12f,.2f),new Vector2(.3f,.02f),new Vector2(.7f,.02f),new Vector2(.88f,.2f),new Vector2(.84f,.43f)};
        for(int i=0;i<points.Length;i++)Line(vh,points[i],points[(i+1)%points.Length]);
        if(kind=="Poison"){Line(vh,new Vector2(.35f,.24f),new Vector2(.42f,.32f));Line(vh,new Vector2(.6f,.24f),new Vector2(.67f,.32f));}
    }
    void Line(VertexHelper vh,Vector2 a,Vector2 b)
    {
        var r=rectTransform.rect;a=r.min+Vector2.Scale(a,r.size);b=r.min+Vector2.Scale(b,r.size);
        var n=new Vector2(-(b-a).y,(b-a).x).normalized*1.1f;int index=vh.currentVertCount;
        vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);
        vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
    }
}
