using TMPro;
using System.Linq;
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
            value=$"MAX ITEM LEVEL {limits[index]}\n{(item!=null?item.ItemRarity+" "+item.ItemType:"CLICK TO SELECT / DRAG ITEM")}";
        }
        if(label.text!=value)label.text=value;
    }
    public void OnDrop(PointerEventData data)
    {var item=data.pointerDrag?.GetComponent<ItemSlotUI>()?.Item;if(item!=null)RebirthManager.Instance?.SelectReturnItem(index,item);}
    public void OnPointerClick(PointerEventData data)
    {
        var manager=RebirthManager.Instance;if(manager==null||manager.Phase!=RebirthPhase.Returns)return;
        if(data.button==PointerEventData.InputButton.Right){manager.SelectReturnItem(index,null);GamePersistence.MarkDirty();return;}
        var limits=RelicInventory.Instance.ReturnItemLevelLimits();
        var items=Inventory.Instance.Items.Concat(EquipmentManager.Instance.EquippedItems.Select(x=>x.Value)).Where(g=>g!=null&&g.ItemLevel<=limits[index]).Distinct().ToList();
        var current=index<manager.ReturningItems.Count?manager.ReturningItems[index]:null;int start=items.IndexOf(current);
        for(int n=1;n<=items.Count;n++){var candidate=items[(start+n)%items.Count];if(candidate!=current&&manager.SelectReturnItem(index,candidate)){GamePersistence.MarkDirty();return;}}
        manager.SelectReturnItem(index,null);GamePersistence.MarkDirty();
    }
}
