using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent equipment pictograms, drawn in the inventory's own visual language.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class EquipmentGlyph : MaskableGraphic
{
    public LootManager.GearType gearType;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        switch (gearType)
        {
            case LootManager.GearType.Helmets:
                Path(vh, .2f,.25f,.2f,.6f,.32f,.8f,.68f,.8f,.8f,.6f,.8f,.25f,.62f,.25f,.62f,.48f,.38f,.48f,.38f,.25f,.2f,.25f);
                Path(vh,.26f,.59f,.74f,.59f); break;
            case LootManager.GearType.BodyArmours:
                Path(vh,.3f,.82f,.4f,.72f,.6f,.72f,.7f,.82f,.86f,.61f,.72f,.5f,.7f,.18f,.3f,.18f,.28f,.5f,.14f,.61f,.3f,.82f);
                Path(vh,.5f,.68f,.5f,.26f); Path(vh,.34f,.45f,.66f,.45f); break;
            case LootManager.GearType.Gloves:
                Path(vh,.3f,.18f,.25f,.42f,.13f,.55f,.2f,.62f,.34f,.5f,.34f,.79f,.43f,.79f,.46f,.53f,.48f,.85f,.57f,.85f,.59f,.53f,.63f,.79f,.71f,.77f,.72f,.51f,.78f,.69f,.86f,.65f,.8f,.35f,.7f,.18f,.3f,.18f); break;
            case LootManager.GearType.Boots:
                Path(vh,.32f,.82f,.67f,.82f,.63f,.43f,.83f,.28f,.83f,.18f,.24f,.18f,.24f,.36f,.32f,.45f,.32f,.82f);
                Path(vh,.35f,.67f,.62f,.67f);Path(vh,.34f,.55f,.6f,.55f); break;
            case LootManager.GearType.Rings:
                Circle(vh,.5f,.42f,.25f,.25f);Path(vh,.3f,.72f,.4f,.85f,.6f,.85f,.7f,.72f,.5f,.56f,.3f,.72f); break;
            case LootManager.GearType.Amulets:
                Path(vh,.25f,.83f,.31f,.58f,.5f,.35f,.69f,.58f,.75f,.83f);
                Path(vh,.5f,.44f,.65f,.28f,.5f,.12f,.35f,.28f,.5f,.44f); break;
            case LootManager.GearType.Belts:
                Path(vh,.12f,.65f,.88f,.65f,.88f,.35f,.12f,.35f,.12f,.65f);
                Path(vh,.4f,.7f,.65f,.7f,.65f,.3f,.4f,.3f,.4f,.7f);Path(vh,.51f,.5f,.73f,.5f); break;
            default:
                Path(vh,.18f,.14f,.27f,.1f,.78f,.73f,.78f,.9f,.65f,.83f,.18f,.14f);
                Path(vh,.22f,.43f,.47f,.22f); break;
        }
    }
    void Circle(VertexHelper vh,float x,float y,float rx,float ry)
    {
        for(int i=0;i<24;i++) {float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(vh,new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),new Vector2(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry));}
    }
    void Path(VertexHelper vh,params float[] p) { for(int i=0;i<p.Length-2;i+=2)Line(vh,new Vector2(p[i],p[i+1]),new Vector2(p[i+2],p[i+3])); }
    void Line(VertexHelper vh,Vector2 a,Vector2 b)
    {
        var r=GetPixelAdjustedRect();a=r.min+Vector2.Scale(a,r.size);b=r.min+Vector2.Scale(b,r.size);
        var n=new Vector2(-(b-a).y,(b-a).x).normalized*Mathf.Min(r.width,r.height)*.023f;
        int i=vh.currentVertCount;vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
}
