using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RebirthConfirmationUI
{
    [SerializeField] TMP_Dropdown startingWeapon;
    [SerializeField] List<RebirthReturnSlotUI> returnSlots=new();
    readonly List<string> weaponChoices=new();
    void RefreshSetup()
    {
        if(startingWeapon==null)return;
        var manager=RebirthManager.Instance;var inventory=RelicInventory.Instance;
        string classDefault=GameManager.Instance?.GetComponent<PlayerIdentityState>()?.ClassDefinition?.SignatureWeaponTypeId??WeaponTypeCatalog.HistoricalDefaultId;
        weaponChoices.Clear();weaponChoices.AddRange(inventory?.StartingWeaponChoices(classDefault)??new[]{classDefault});
        startingWeapon.ClearOptions();startingWeapon.AddOptions(weaponChoices.Select(id=>id.Replace("weapon.","").Replace('_',' ')).ToList());
        startingWeapon.SetValueWithoutNotify(0);manager?.SelectStartingWeapon(weaponChoices[0]);
        startingWeapon.onValueChanged.RemoveAllListeners();startingWeapon.onValueChanged.AddListener(i=>manager?.SelectStartingWeapon(weaponChoices[i]));
        int count=inventory?.ReturnItemLevelLimits().Count??0;for(int i=0;i<returnSlots.Count;i++)returnSlots[i].gameObject.SetActive(i<count);
        int level=GameManager.Instance?.CurrentCombatLevel??60;
        if(authoredView?.message!=null)authoredView.message.text=$"REBIRTH — {RelicProgressionRules.RewardCount(level)} RELICS / RELIC LEVEL {RelicInventory.LevelForZone(level)}\nITEMS RETURNING WITH YOU\nOther ordinary items and run progress reset. Relics remain.";
    }
#if UNITY_EDITOR
    public void AuthorSystemsSetup()
    {
        if(authoredView==null)authoredView=GetComponent<RebirthView>();if(authoredView?.confirmationPanel==null)return;
        var parent=authoredView.confirmationPanel.transform;
        if(startingWeapon!=null)return;
        var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name="Starting weapon type";go.transform.SetParent(parent,false);
        startingWeapon=go.GetComponent<TMP_Dropdown>();Place((RectTransform)go.transform,.06f,.72f,.94f,.80f);
        Place(authoredView.message.rectTransform,.06f,.82f,.94f,.98f);
        for(int i=0;i<8;i++)
        {
            var slot=new GameObject("Return item "+(i+1),typeof(RectTransform),typeof(Image),typeof(RebirthReturnSlotUI));slot.transform.SetParent(parent,false);
            slot.GetComponent<Image>().color=new Color(.12f,.1f,.16f,.98f);
            float x=.06f+(i%4)*.225f,y=.48f-(i/4)*.19f;Place((RectTransform)slot.transform,x,y,x+.21f,y+.17f);
            var label=Label(slot.transform,"Return Item",11);Place(label.rectTransform,.03f,.03f,.97f,.97f);
            slot.GetComponent<RebirthReturnSlotUI>().Configure(i,label);returnSlots.Add(slot.GetComponent<RebirthReturnSlotUI>());
        }
        Place((RectTransform)authoredView.confirmButton.transform,.08f,.03f,.47f,.13f);
        Place((RectTransform)authoredView.cancelButton.transform,.53f,.03f,.92f,.13f);
    }
#endif
}
