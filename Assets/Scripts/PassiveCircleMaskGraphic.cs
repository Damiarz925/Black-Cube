using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class PassiveCircleMaskGraphic:MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        mesh.AddVert(rectTransform.rect.center,Color.white,Vector2.zero);
        for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;mesh.AddVert(rectTransform.rect.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,Color.white,Vector2.zero);if(i>0)mesh.AddTriangle(0,i,i+1);}
    }
}
