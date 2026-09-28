using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RelicFusionUI:MonoBehaviour
{
    [SerializeField] Button open, fuse, close;
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text feedback;
    [SerializeField] List<TMP_Dropdown> inputs=new();
    readonly List<RelicData> candidates=new();
    void Awake(){open.onClick.AddListener(Open);fuse.onClick.AddListener(Fuse);close.onClick.AddListener(()=>panel.SetActive(false));}
    void Open()
    {
        candidates.Clear();if(RelicInventory.Instance!=null)candidates.AddRange(RelicInventory.Instance.Relics.Where(r=>r.Pristine&&r.rarity<LootManager.GearRarity.Legendary));
        var options=new List<string>{"Choose a Pristine Relic"};options.AddRange(candidates.Select(r=>$"{r.rarity} / L{r.relicLevel} / {r.id.Substring(0,Mathf.Min(6,r.id.Length))}"));
        foreach(var input in inputs){input.ClearOptions();input.AddOptions(options);input.SetValueWithoutNotify(0);}
        feedback.text="Five distinct, same-rarity Pristine Relics → one fresh higher rarity. Inputs are consumed, including equipped inputs.";panel.SetActive(true);
    }
    void Fuse()
    {
        var selected=new List<RelicData>();foreach(var input in inputs){if(input.value<=0||input.value>candidates.Count){feedback.text="Choose all five inputs.";return;}selected.Add(candidates[input.value-1]);}
        if(RelicInventory.Instance?.TryFuse(selected,out var result)!=true){feedback.text="Invalid recipe: five distinct owned Pristine Relics of the same rarity are required.";return;}
        Open();feedback.text=$"Created {result.rarity} Relic / L{result.relicLevel}.";GamePersistence.Save();
    }
#if UNITY_EDITOR
    public void BuildAuthoring()
    {
        if(open!=null)return;
        open=MakeButton(transform,"RELIC FUSION",new Vector4(.02f,.50f,.16f,.535f));
        panel=new GameObject("Relic fusion",typeof(RectTransform),typeof(Image));panel.transform.SetParent(transform,false);Place((RectTransform)panel.transform,new Vector4(.18f,.18f,.82f,.82f));panel.GetComponent<Image>().color=new Color(.04f,.025f,.065f,.99f);
        for(int i=0;i<5;i++)
        {
            var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name="Fusion input "+(i+1);go.transform.SetParent(panel.transform,false);Place((RectTransform)go.transform,new Vector4(.05f,.75f-i*.12f,.95f,.84f-i*.12f));inputs.Add(go.GetComponent<TMP_Dropdown>());
        }
        feedback=Text(panel.transform,"Feedback");Place(feedback.rectTransform,new Vector4(.05f,.1f,.95f,.24f));
        fuse=MakeButton(panel.transform,"FUSE / CONSUME INPUTS",new Vector4(.05f,.01f,.7f,.09f));close=MakeButton(panel.transform,"CLOSE",new Vector4(.72f,.01f,.95f,.09f));panel.SetActive(false);
    }
    static Button MakeButton(Transform parent,string label,Vector4 bounds){var go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,bounds);go.GetComponent<Image>().color=new Color(.22f,.12f,.3f);var text=Text(go.transform,label);text.text=label;Place(text.rectTransform,new Vector4(0,0,1,1));return go.GetComponent<Button>();}
    static TMP_Text Text(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();text.fontSize=12;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
    static void Place(RectTransform rect,Vector4 b){rect.anchorMin=new Vector2(b.x,b.y);rect.anchorMax=new Vector2(b.z,b.w);rect.offsetMin=rect.offsetMax=Vector2.zero;}
#endif
}
