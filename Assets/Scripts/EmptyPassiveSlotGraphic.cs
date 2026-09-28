using UnityEngine;
using UnityEngine.UI;

// An empty geometric glow, not an invented passive icon or modified supplied art.
[RequireComponent(typeof(CanvasRenderer))]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class EmptyPassiveSlotGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.46f;
        Ring(mesh,radius*.75f,radius,new Color(1,.42f,.05f,.13f));Ring(mesh,radius*.81f,radius*.86f,new Color(1,.72f,.18f,1));Ring(mesh,radius*.86f,radius*.96f,new Color(1,.48f,.06f,.2f));
    }
    static void Ring(VertexHelper mesh,float inner,float outer,Color tint)
    {
        const int segments=64;int start=mesh.currentVertCount;
        for(int i=0;i<=segments;i++){float angle=i*Mathf.PI*2/segments;Vector2 direction=new(Mathf.Cos(angle),Mathf.Sin(angle));mesh.AddVert(direction*inner,tint,Vector2.zero);mesh.AddVert(direction*outer,tint,Vector2.zero);if(i>0){int n=start+i*2;mesh.AddTriangle(n-2,n-1,n);mesh.AddTriangle(n-1,n+1,n);}}
    }
}
