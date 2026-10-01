using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Authored collapsed control; only its granted-skill contents are dynamic.
public sealed class RelicTriggerPanelUI:MonoBehaviour
{
    [SerializeField] Button expand,template;
    [SerializeField] GameObject contents;
    readonly List<Button> buttons=new();
    PlayerSkillController controller;
    string signature;
    void Start(){controller=FindAnyObjectByType<PlayerSkillController>();expand.onClick.AddListener(()=>contents.SetActive(!contents.activeSelf));contents.SetActive(false);template.gameObject.SetActive(false);}
    void Update()
    {
        var ids=RelicInventory.Instance?.TriggeredSkills().ToArray()??System.Array.Empty<PlayerSkillId>();
        expand.gameObject.SetActive(ids.Length>0);
        string next=string.Join(",",ids);if(next!=signature){signature=next;foreach(var b in buttons)Destroy(b.gameObject);buttons.Clear();foreach(var id in ids){var copy=Instantiate(template,template.transform.parent);copy.gameObject.SetActive(true);copy.onClick.RemoveAllListeners();copy.onClick.AddListener(()=>controller?.ToggleRelicTrigger(id));buttons.Add(copy);}}
        for(int i=0;i<buttons.Count;i++){var skill=controller?.Skills.FirstOrDefault(s=>s.id==ids[i]);buttons[i].GetComponentInChildren<TMP_Text>().text=$"{skill?.displayName??ids[i].ToString()}: {(controller?.RelicTriggerEnabled(ids[i])!=false?"ON":"OFF")}";}
    }
#if UNITY_EDITOR
    public void Author()
    {
        var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-20,110);rect.sizeDelta=new Vector2(285,32);
        expand=Make(transform,"RELIC SKILL TRIGGERS");var er=(RectTransform)expand.transform;er.anchorMin=Vector2.zero;er.anchorMax=Vector2.one;er.offsetMin=er.offsetMax=Vector2.zero;
        contents=new GameObject("Trigger List",typeof(RectTransform),typeof(Image),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));contents.transform.SetParent(transform,false);
        var cr=(RectTransform)contents.transform;cr.anchorMin=cr.anchorMax=new Vector2(0,1);cr.pivot=new Vector2(0,0);cr.sizeDelta=new Vector2(285,0);cr.anchoredPosition=new Vector2(0,4);
        contents.GetComponent<Image>().color=new Color(.04f,.03f,.05f,.98f);var layout=contents.GetComponent<VerticalLayoutGroup>();layout.spacing=3;layout.childControlHeight=true;layout.childForceExpandHeight=false;contents.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        template=Make(contents.transform,"Trigger Template");template.gameObject.AddComponent<LayoutElement>().preferredHeight=30;template.gameObject.SetActive(false);contents.SetActive(false);
    }
    static Button Make(Transform parent,string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=new Color(.17f,.10f,.20f,1);
        var text=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var r=(RectTransform)text.transform;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var t=text.GetComponent<TextMeshProUGUI>();t.text=name;t.fontSize=13;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return go.GetComponent<Button>();
    }
#endif
}
