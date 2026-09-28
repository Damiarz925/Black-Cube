using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class RebirthReturnSlotUI:MonoBehaviour,IDropHandler,IPointerClickHandler
{
    [SerializeField] int index;
    [SerializeField] TMP_Text label;
    public void Configure(int slot,TMP_Text text){index=slot;label=text;}
    void Update()
    {
        var limits=RelicInventory.Instance?.ReturnItemLevelLimits();
        if(label==null)return;
        string value="";
        if(limits!=null&&index<limits.Count)
        {
            var items=RebirthManager.Instance?.ReturningItems;var item=items!=null&&index<items.Count?items[index]:null;
            value=$"MAX ITEM LEVEL {limits[index]}\n{(item!=null?item.ItemRarity+" "+item.ItemType:"DRAG AN ITEM HERE")}";
        }
        if(label.text!=value)label.text=value;
    }
    public void OnDrop(PointerEventData data)
    {var item=data.pointerDrag?.GetComponent<ItemSlotUI>()?.Item;if(item!=null)RebirthManager.Instance?.SelectReturnItem(index,item);}
    public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Right)RebirthManager.Instance?.SelectReturnItem(index,null);}
}
