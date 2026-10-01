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
        var manager=RebirthManager.Instance;var inventory=RelicInventory.Instance;
        if(manager==null||authoredView==null)return;
        string classDefault=GameManager.Instance?.GetComponent<PlayerIdentityState>()?.ClassDefinition?.SignatureWeaponTypeId??WeaponTypeCatalog.HistoricalDefaultId;
        weaponChoices.Clear();weaponChoices.AddRange(inventory?.StartingWeaponChoices(classDefault)??new[]{classDefault});
        if(startingWeapon!=null)
        {
            startingWeapon.gameObject.SetActive(manager.Phase is RebirthPhase.Weapon or RebirthPhase.Review);
            startingWeapon.ClearOptions();startingWeapon.AddOptions(weaponChoices.Select(id=>WeaponTypeCatalog.Get(id).DisplayName).ToList());
            int selected=Mathf.Max(0,weaponChoices.IndexOf(manager.SelectedStartingWeaponType));
            startingWeapon.SetValueWithoutNotify(selected);
            if(manager.Phase==RebirthPhase.Weapon)manager.SelectStartingWeapon(weaponChoices[selected]);
            startingWeapon.onValueChanged.RemoveAllListeners();startingWeapon.onValueChanged.AddListener(i=>{manager.SelectStartingWeapon(weaponChoices[i]);GamePersistence.MarkDirty();});
        }
        int count=inventory?.ReturnItemLevelLimits().Count??0;
        for(int i=0;i<returnSlots.Count;i++)returnSlots[i].gameObject.SetActive(manager.Phase==RebirthPhase.Returns&&i<count);
        string text=manager.Phase switch
        {
            RebirthPhase.None=>$"CONFIRM REBIRTH?\nThis permanently ends the current run and grants {RelicProgressionRules.RewardCount(GameManager.Instance.CurrentCombatLevel)} relics. You cannot cancel afterwards.\nCraft, equip, select return items, choose a weapon, then review before starting.",
            RebirthPhase.Crafting=>$"CRAFT CURRENT-CYCLE RELICS\nGranted: {string.Join(", ",inventory.Relics.Where(r=>r.cycle==inventory.CurrentCycle).Select(r=>$"{r.rarity} level {r.relicLevel}"))}\nOpen Relic Inventory to use your earned Ancient currency. Only these new relics may be crafted. NEXT permanently closes this crafting window.",
            RebirthPhase.Equipment=>"EQUIP UP TO EIGHT ACTIVE RELICS\nOpen Relic Inventory and choose your loadout. Only one Unique Relic may be active. NEXT locks this loadout until the next Rebirth.",
            RebirthPhase.Returns=>"SELECT RETURN ITEMS\nClick a slot to cycle through eligible owned items, including equipped gear. Right-click to clear. Empty slots are allowed. All unselected ordinary gear will be removed.",
            RebirthPhase.Weapon=>"CHOOSE STARTING WEAPON\nChoices come from your class default and equipped relic unlocks. Starting element resolves from your active relic loadout.",
            _=>$"REVIEW — BEGIN NEXT RUN\nACTIVE RELICS:\n{string.Join("\n",Enumerable.Range(0,RelicInventory.ActiveSlotCount).Select(i=>inventory.Active(i)).Where(r=>r!=null).Select(r=>$"{r.rarity} L{r.relicLevel} / {r.id.Substring(0,Mathf.Min(8,r.id.Length))}"))}\nRETURNING ITEMS:\n{string.Join(", ",manager.ReturningItems.Where(g=>g!=null).Select(g=>$"{g.ItemRarity} {g.ItemType} ilvl {g.ItemLevel}"))}\nWeapon: {manager.SelectedStartingWeaponType}\nElement: {inventory.ResolveStarterElement(RelicProgressionRules.ClassElement(GameManager.Instance?.GetComponent<PlayerIdentityState>()?.ClassDefinition?.Id))}\nUnselected items and run currency will reset. All relics are now uncraftable."
        };
        authoredView.message.text=text;
        authoredView.confirmButton.GetComponentInChildren<TMP_Text>().text=manager.IsSetup?(manager.Phase==RebirthPhase.Review?"BEGIN NEXT RUN":"NEXT"): "CONFIRM REBIRTH";
        authoredView.cancelButton.GetComponentInChildren<TMP_Text>().text=manager.IsSetup?"HIDE SETUP":"CANCEL";

    }
    void OpenRelics()
    {
        var ui=FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if(ui==null)return;ui.gameObject.SetActive(true);ui.ShowRelicViewForCurrency();
        authoredView.confirmationPanel.SetActive(false);
    }
#if UNITY_EDITOR
    public void AuthorSystemsSetup()
    {
        if(authoredView==null)authoredView=GetComponent<RebirthView>();if(authoredView?.confirmationPanel==null)return;
        var parent=authoredView.confirmationPanel.transform;
        foreach(var duplicate in parent.Cast<Transform>().Where(t=>t.name=="OPEN RELIC INVENTORY").Skip(1).ToArray())DestroyImmediate(duplicate.gameObject);
        if(parent.Find("OPEN RELIC INVENTORY")==null)
        {
            var button=Button(parent,"OPEN RELIC INVENTORY",new Vector2(.08f,.14f),new Vector2(.92f,.23f),OpenRelics,out _);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,OpenRelics);
        }
        Place((RectTransform)parent,.22f,.12f,.78f,.85f);
        Place(authoredView.message.rectTransform,.06f,.56f,.94f,.97f);
        authoredView.message.enableAutoSizing=true;authoredView.message.fontSizeMin=12;authoredView.message.fontSizeMax=18;
        if(startingWeapon!=null)Place((RectTransform)startingWeapon.transform,.06f,.47f,.94f,.55f);
        for(int i=0;i<returnSlots.Count;i++){float x=.06f+(i%4)*.225f,y=.33f-(i/4)*.09f;Place((RectTransform)returnSlots[i].transform,x,y,x+.21f,y+.08f);}
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
