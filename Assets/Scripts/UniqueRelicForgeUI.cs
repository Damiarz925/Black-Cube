using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UniqueRelicForgeUI:MonoBehaviour
{
    [SerializeField] Button open,forge,close;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text feedback;
    [SerializeField] List<TMP_Dropdown> items=new(),choices=new();
    readonly List<Gear> candidates=new();
    void Awake(){open.onClick.AddListener(Open);close.onClick.AddListener(()=>panel.SetActive(false));forge.onClick.AddListener(Forge);for(int i=0;i<2;i++){int index=i;items[i].onValueChanged.AddListener(_=>RefreshChoice(index));}}
    Gear Selected(int index)=>items[index].value>0&&items[index].value<=candidates.Count?candidates[items[index].value-1]:null;
    void RefreshChoice(int index){choices[index].ClearOptions();choices[index].AddOptions(UniqueRelicForge.Choices(Selected(index)).ToList());}
    void Open()
    {
        candidates.Clear();if(Inventory.Instance!=null)candidates.AddRange(Inventory.Instance.Items.Where(x=>x!=null&&!x.IsLocked&&(x.UniqueData!=null||x.ItemRarity==LootManager.GearRarity.Legendary&&x.CraftingModCount==6)));
        var labels=new List<string>{"Choose ingredient"};labels.AddRange(candidates.Select(x=>$"{(x.UniqueData!=null?UniqueCatalog.Get(x.UniqueData.definitionId)?.name:x.ItemType.ToString())} / {x.ItemRarity} / L{x.ItemLevel} / {x.PersistentId.Substring(0,6)}"));
        foreach(var dropdown in items){dropdown.ClearOptions();dropdown.AddOptions(labels);dropdown.SetValueWithoutNotify(0);}for(int i=0;i<2;i++)RefreshChoice(i);
        feedback.text=$"Forge opportunities: {RelicInventory.Instance?.ForgeOpportunities??0}. Choose two Uniques, then two six-explicit Legendaries. One actual modifier from each Legendary is chosen randomly. All four items will be consumed.";panel.SetActive(true);
    }
    void Forge()
    {
        if(!UniqueRelicForge.TryForge(RelicInventory.Instance,Selected(0),Selected(1),Selected(2),Selected(3),choices[0].value,choices[1].value,null,out var result,out var error)){feedback.text=error;return;}
        Open();feedback.text="Forged a Unique Relic. Only one may be equipped. Actual source values were retained.";
    }
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(open!=null)return;
        open=Button(transform,"UNIQUE RELIC FORGE",new Vector4(.17f,.50f,.36f,.535f));
        panel=new GameObject("Unique relic forge",typeof(RectTransform),typeof(Image));panel.transform.SetParent(transform,false);Place((RectTransform)panel.transform,new Vector4(.17f,.16f,.83f,.84f));panel.GetComponent<Image>().color=new Color(.06f,.03f,.025f,.99f);
        for(int i=0;i<4;i++){items.Add(Dropdown(panel.transform,"Ingredient "+(i+1),new Vector4(.04f,.8f-i*.16f,.60f,.90f-i*.16f)));if(i<2)choices.Add(Dropdown(panel.transform,"Chosen Unique modifier "+(i+1),new Vector4(.62f,.8f-i*.16f,.96f,.90f-i*.16f)));}
        var text=new GameObject("Recipe and feedback",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(panel.transform,false);feedback=text.GetComponent<TMP_Text>();feedback.fontSize=14;feedback.raycastTarget=false;Place(feedback.rectTransform,new Vector4(.04f,.1f,.96f,.28f));
        forge=Button(panel.transform,"FORGE / CONSUME FOUR INPUTS",new Vector4(.04f,.02f,.70f,.09f));close=Button(panel.transform,"CLOSE",new Vector4(.72f,.02f,.96f,.09f));panel.SetActive(false);
    }
    static TMP_Dropdown Dropdown(Transform parent,string name,Vector4 bounds){var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);Place((RectTransform)go.transform,bounds);return go.GetComponent<TMP_Dropdown>();}
    static Button Button(Transform parent,string name,Vector4 bounds){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,bounds);go.GetComponent<Image>().color=new Color(.3f,.15f,.08f);var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);Place((RectTransform)text.transform,new Vector4(0,0,1,1));var label=text.GetComponent<TMP_Text>();label.text=name;label.fontSize=12;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return go.GetComponent<Button>();}
    static void Place(RectTransform rect,Vector4 b){rect.anchorMin=new Vector2(b.x,b.y);rect.anchorMax=new Vector2(b.z,b.w);rect.offsetMin=rect.offsetMax=Vector2.zero;}
#endif
}
