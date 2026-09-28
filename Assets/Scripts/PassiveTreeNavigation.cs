using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public sealed class PassiveTreeNavigation:MonoBehaviour
{
    public Button chooseClass,chooseWeapon,back,hub,cancel;
    public GameObject selectionRoot;public TMP_Text warning;public List<Button> options=new();
    PassiveTreeView view;PassiveTreePresentation presentation;PlayerProgression progression;
    SkillTreeUI TreeUI => presentation != null ? presentation.GetComponent<SkillTreeUI>() : null;
    public void Initialize(PassiveTreeView authored,PassiveTreePresentation camera,PlayerProgression progress)
    {
        if(progression!=null)progression.Changed-=Refresh;
        view=authored;presentation=camera;progression=progress;
        Bind(chooseClass,()=>OpenSelection(false));Bind(chooseWeapon,()=>OpenSelection(true));Bind(back,Leave);Bind(hub,()=>{if(presentation.EnterWeaponFocus())TreeUI?.RefreshPresentation();Refresh();});Bind(cancel,()=>selectionRoot.SetActive(false));
        progression.Changed+=Refresh;Refresh();
    }
    static void Bind(Button button,UnityEngine.Events.UnityAction callback){if(button==null)return;button.onClick.RemoveAllListeners();button.onClick.AddListener(callback);}
    void OnDestroy(){if(progression!=null)progression.Changed-=Refresh;}
    void OnDisable(){if(selectionRoot!=null)selectionRoot.SetActive(false);}
    public void Refresh()
    {
        if(progression==null)return;
        if(chooseClass!=null){chooseClass.gameObject.SetActive(!presentation.WeaponFocused);chooseClass.interactable=progression.CanSelectAdditionalClass;}
        if(chooseWeapon!=null){chooseWeapon.gameObject.SetActive(!presentation.WeaponFocused&&string.IsNullOrEmpty(progression.SelectedWeaponTreeId));chooseWeapon.interactable=progression.CanSelectWeaponTree;}
        if(back!=null)back.gameObject.SetActive(presentation.WeaponFocused);
        if(hub!=null)hub.interactable=!string.IsNullOrEmpty(progression.SelectedWeaponTreeId);
    }
    public void OpenSelection(bool weapon)
    {
        if(selectionRoot==null||progression==null||(weapon?!progression.CanSelectWeaponTree:!progression.CanSelectAdditionalClass))return;
        var choices=(weapon?PassiveTreeDefinition.WeaponIds:PassiveTreeDefinition.ClassIds).Where(id=>weapon||id!=progression.ActiveClassId&&!progression.SelectedClassRoutes.Contains(id)).ToList();
        warning.text=weapon?"Only ONE Weapon Tree may be specialized in.\nYour unlocked specialization survives passive refunds.":"Choose your next class route.\nComplete its ten-node spine to choose another. Unlocks survive refunds.";
        for(int i=0;i<options.Count;i++){var button=options[i];button.gameObject.SetActive(i<choices.Count);if(i>=choices.Count)continue;string id=choices[i];button.GetComponentInChildren<TMP_Text>().text=id.Replace("weapon.","").Replace("class.","").Replace('_',' ').ToUpperInvariant();Bind(button,()=>{bool selected=weapon?progression.TrySelectWeaponTree(id):progression.TrySelectClassRoute(id);if(selected){selectionRoot.SetActive(false);TreeUI?.RefreshPresentation();Refresh();}});}
        selectionRoot.SetActive(true);selectionRoot.transform.SetAsLastSibling();
    }
    void Leave(){presentation.ExitWeaponFocus();TreeUI?.RefreshPresentation();Refresh();}
    void Update(){if(view?.panel?.activeInHierarchy!=true||presentation?.WeaponFocused!=true)return;if(Keyboard.current?.escapeKey.wasPressedThisFrame==true||Mouse.current?.rightButton.wasPressedThisFrame==true)Leave();}
}
