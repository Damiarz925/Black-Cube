using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Manual destructive actions are confirmed at the inventory UI boundary.
// Inventory.TryDismantle remains the authority for ownership and lock checks.
public sealed class DismantleConfirmationUI : MonoBehaviour
{
    [SerializeField] GameObject modal;
    [SerializeField] TMP_Text message;
    [SerializeField] Button confirm,cancel;
    Gear pending;

    void Awake()
    {
        if(modal==null||message==null||confirm==null||cancel==null)return;
        confirm.onClick.RemoveAllListeners();confirm.onClick.AddListener(Confirm);
        cancel.onClick.RemoveAllListeners();cancel.onClick.AddListener(Cancel);
        modal.SetActive(false);
    }
    void OnDisable()=>Cancel();
    public void Request(Gear item)
    {
        if(Inventory.Instance==null||!Inventory.Instance.CanDismantle(item))return;
        if(item.ItemRarity is not (LootManager.GearRarity.Legendary or LootManager.GearRarity.Unique))
        {Inventory.Instance.TryDismantle(item);return;}
        if(modal==null){Debug.LogError("Dismantle confirmation is not authored; protected item was not destroyed.",this);return;}
        pending=item;
        message.text=item.ItemRarity==LootManager.GearRarity.Unique
            ?"DISMANTLE UNIQUE ITEM?\nThis item cannot be recovered."
            :"DISMANTLE LEGENDARY ITEM?\nThis cannot be undone.";
        modal.SetActive(true);modal.transform.SetAsLastSibling();
    }
    void Confirm()
    {
        Gear item=pending;Cancel();
        if(item!=null)Inventory.Instance?.TryDismantle(item);
    }
    void Cancel(){pending=null;if(modal!=null)modal.SetActive(false);}

#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(modal!=null)return;
        modal=new GameObject("Dismantle confirmation",typeof(RectTransform),typeof(Image),typeof(Canvas),typeof(GraphicRaycaster));
        modal.transform.SetParent(transform,false);var cover=(RectTransform)modal.transform;
        cover.anchorMin=Vector2.zero;cover.anchorMax=Vector2.one;cover.offsetMin=cover.offsetMax=Vector2.zero;
        modal.GetComponent<Image>().color=new Color(0,0,0,.78f);
        var canvas=modal.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=280;
        var box=new GameObject("Confirmation card",typeof(RectTransform),typeof(Image));box.transform.SetParent(modal.transform,false);
        var rect=(RectTransform)box.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(420,210);
        box.GetComponent<Image>().color=new Color(.05f,.06f,.075f,1);
        var label=new GameObject("Warning",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(box.transform,false);
        var lr=(RectTransform)label.transform;lr.anchorMin=new Vector2(.06f,.35f);lr.anchorMax=new Vector2(.94f,.94f);lr.offsetMin=lr.offsetMax=Vector2.zero;
        message=label.GetComponent<TMP_Text>();message.fontSize=19;message.alignment=TextAlignmentOptions.Center;message.color=Color.white;message.raycastTarget=false;
        confirm=MakeButton(box.transform,"Confirm dismantle",new Vector2(.08f,.08f),new Vector2(.47f,.32f),"CONFIRM");
        cancel=MakeButton(box.transform,"Cancel dismantle",new Vector2(.53f,.08f),new Vector2(.92f,.32f),"CANCEL");
        modal.SetActive(false);
    }
    static Button MakeButton(Transform parent,string name,Vector2 min,Vector2 max,string text)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        go.GetComponent<Image>().color=new Color(.17f,.20f,.24f,1);var b=go.GetComponent<Button>();b.targetGraphic=go.GetComponent<Image>();
        var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);
        var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var tmp=label.GetComponent<TMP_Text>();tmp.text=text;tmp.fontSize=16;tmp.alignment=TextAlignmentOptions.Center;tmp.color=Color.white;tmp.raycastTarget=false;
        return b;
    }
#endif
}
