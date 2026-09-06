using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable edge effects for any digit string; the glyph faces remain unobstructed.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class DamageNumberAccent : MaskableGraphic
{
    public int style;
    public static void Attach(TMP_Text number,int type,Color tint)
    {
        number.ForceMeshUpdate();
        var go=new GameObject("Damage type accent",typeof(RectTransform),typeof(DamageNumberAccent));
        go.transform.SetParent(number.transform,false);
        var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
        r.sizeDelta=new Vector2(Mathf.Max(18,number.textBounds.size.x),Mathf.Max(20,number.textBounds.size.y));
        r.localPosition=number.textBounds.center;
        var accent=go.GetComponent<DamageNumberAccent>();accent.style=type;accent.color=tint;accent.raycastTarget=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();float l=r.xMin-3,h=r.yMax,b=r.yMin,rr=r.xMax+3;
        switch(style)
        {
            case 0: // metallic corner glints
                Stroke(vh,new Vector2(l,h-3),new Vector2(l+5,h+2),1.2f);Stroke(vh,new Vector2(rr-4,b-1),new Vector2(rr,b+3),1.2f);break;
            case 1: // tongues of fire, outside the top of the digits
                for(int i=0;i<3;i++){float x=Mathf.Lerp(l+5,rr-5,i*.5f);Triangle(vh,new Vector2(x-3,h+1),new Vector2(x+1,h+8+i%2*3),new Vector2(x+3,h+1));}
                break;
            case 2: // ice crystals
                Crystal(vh,new Vector2(l-1,h-3));Crystal(vh,new Vector2(rr+1,b+4));break;
            case 3: // angular electrical arcs
                Zig(vh,new Vector2(l-5,b),new Vector2(l-9,b+8),new Vector2(l-3,b+7),new Vector2(l-7,h+5));
                Zig(vh,new Vector2(rr+7,h),new Vector2(rr+3,h-8),new Vector2(rr+9,h-7),new Vector2(rr+4,b-5));break;
            case 4: // rounded slime drips
                Drop(vh,new Vector2(l+5,b),8,2.4f);Drop(vh,new Vector2(rr-5,b),5,2);break;
            case 5: // longer blood drips
                Drop(vh,new Vector2(l+5,b),10,1.3f);Drop(vh,new Vector2(rr-5,b),7,1.7f);break;
            default: break;
        }
    }
    void Zig(VertexHelper vh,params Vector2[] p){for(int i=0;i<p.Length-1;i++)Stroke(vh,p[i],p[i+1],1.6f);}
    void Crystal(VertexHelper vh,Vector2 c)
    {for(int i=0;i<3;i++){float a=i*Mathf.PI/3;var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5;Stroke(vh,c-v,c+v,1.2f);}}
    void Drop(VertexHelper vh,Vector2 p,float length,float width)
    {
        Stroke(vh,p,p+Vector2.down*(length-width),width);
        var c=p+Vector2.down*(length-width);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6,z=(i+1)*Mathf.PI/6;Triangle(vh,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*width,c+new Vector2(Mathf.Cos(z),Mathf.Sin(z))*width);}
    }
    void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width)
    {
        var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
        Quad(vh,a-n*2.2f,a+n*2.2f,b+n*2.2f,b-n*2.2f,new Color(.02f,.02f,.025f));
        Quad(vh,a-n,a+n,b+n,b-n,color);
    }
    void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
    {int i=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c)
    {int i=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}
}
