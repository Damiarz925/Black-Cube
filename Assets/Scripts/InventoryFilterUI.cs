// Developer map: Builds the pickup-filter controls and edits Inventory cutoff settings. Enabled level/rarity cutoffs combine with OR; existing inventory is not retroactively scrapped.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Session-only, independently enabled inclusive pickup filters.</summary>
public class InventoryFilterUI : MonoBehaviour
{
    GameObject panel;
    TMP_Text levelLabel, rarityLabel;
    Button levelToggle, rarityToggle;
    void Start()
    {
        foreach(var text in GetComponentsInChildren<TMP_Text>())
        {
            if(text.text=="RECOVERED EQUIPMENT") text.text="EQUIPMENT";
            if(text.text=="SELECT AN ITEM TO EQUIP") { text.text="LEFT: EQUIP / RIGHT: SCRAP\nHOVER: DETAILS"; text.fontSize=11; }
        }
        Button(transform,"FILTER",new Vector2(.55f,.83f),new Vector2(.88f,.88f),()=>{GetComponent<InventoryUI>().Tooltip?.Hide();Refresh();panel.SetActive(!panel.activeSelf);});
        panel = Box(transform,"Pickup filter",new Color(.04f,.055f,.065f,1));
        Place((RectTransform)panel.transform,new Vector2(.035f,.31f),new Vector2(.965f,.82f));
        Text(panel.transform,"AUTO-SCRAP / NEW LOOT ONLY",.83f,.98f,16);
        Text(panel.transform,"Either enabled cutoff matches.\nExisting gear and equipment returns are kept.\nBoth OFF = no automatic scrapping.",.61f,.83f,12);
        levelToggle = Button(panel.transform,"",new Vector2(.04f,.45f),new Vector2(.96f,.59f),()=>{Inventory.Instance.FilterLevelEnabled=!Inventory.Instance.FilterLevelEnabled;Refresh();});
        levelLabel = levelToggle.GetComponentInChildren<TMP_Text>();
        Button(panel.transform,"−",new Vector2(.05f,.32f),new Vector2(.27f,.44f),()=>{Inventory.Instance.FilterLevel=Mathf.Max(1,Inventory.Instance.FilterLevel-1);Refresh();});
        Button(panel.transform,"+",new Vector2(.73f,.32f),new Vector2(.95f,.44f),()=>{Inventory.Instance.FilterLevel=Mathf.Min(int.MaxValue-1,Inventory.Instance.FilterLevel)+1;Refresh();});
        rarityToggle = Button(panel.transform,"",new Vector2(.04f,.16f),new Vector2(.96f,.30f),()=>{Inventory.Instance.FilterRarityEnabled=!Inventory.Instance.FilterRarityEnabled;Refresh();});
        rarityLabel = rarityToggle.GetComponentInChildren<TMP_Text>();
        Button(panel.transform,"<",new Vector2(.05f,.02f),new Vector2(.27f,.14f),()=>{Inventory.Instance.FilterRarity=(LootManager.GearRarity)Mathf.Max(0,(int)Inventory.Instance.FilterRarity-1);Refresh();});
        Button(panel.transform,">",new Vector2(.73f,.02f),new Vector2(.95f,.14f),()=>{Inventory.Instance.FilterRarity=(LootManager.GearRarity)Mathf.Min(3,(int)Inventory.Instance.FilterRarity+1);Refresh();});
        Refresh(); panel.SetActive(false);
    }
    void OnDisable() { if(panel != null) panel.SetActive(false); }
    void Refresh()
    {
        var inv=Inventory.Instance;if(inv==null)return;
        levelLabel.text=$"{(inv.FilterLevelEnabled?"ON":"OFF")} / Level {inv.FilterLevel} and below";
        rarityLabel.text=$"{(inv.FilterRarityEnabled?"ON":"OFF")} / {inv.FilterRarity} and below";
    }
    static GameObject Box(Transform parent,string name,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go;}
    static void Place(RectTransform r,Vector2 min,Vector2 max)
    {r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    static TMP_Text Text(Transform parent,string text,float bottom,float top,int size)
    {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;Place(t.rectTransform,new Vector2(.04f,bottom),new Vector2(.96f,top));return t;}
    static Button Button(Transform parent,string label,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {var go=Box(parent,label,new Color(.14f,.19f,.22f));Place((RectTransform)go.transform,min,max);var b=go.AddComponent<Button>();b.targetGraphic=go.GetComponent<Image>();b.onClick.AddListener(action);Text(go.transform,label,0,1,13);return b;}
}
