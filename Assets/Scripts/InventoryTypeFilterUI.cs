using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Session-only inventory display filters. All nine item types start enabled.</summary>
public sealed class InventoryTypeFilterUI : MonoBehaviour
{
    [Serializable] public sealed class Binding{public LootManager.GearType type;public Button button;}
    [SerializeField] List<Binding> bindings=new();
    readonly HashSet<LootManager.GearType> visible=new();
    InventoryUI inventory;

    void Awake()
    {
        inventory=GetComponent<InventoryUI>();
        foreach(LootManager.GearType type in Enum.GetValues(typeof(LootManager.GearType)))visible.Add(type);
        foreach(var binding in bindings)if(binding?.button!=null){var copy=binding;copy.button.onClick.AddListener(()=>Toggle(copy.type));}
        RefreshButtons();
    }
    public bool IsVisible(LootManager.GearType type)=>visible.Contains(type);
    void Toggle(LootManager.GearType type){if(!visible.Add(type))visible.Remove(type);RefreshButtons();inventory?.RefreshTypeFilters();}
    void RefreshButtons(){foreach(var binding in bindings)if(binding?.button!=null){bool on=visible.Contains(binding.type);CorruptionUIButtonSkin.Ensure(binding.button)?.SetSelected(on);var label=binding.button.GetComponentInChildren<TMP_Text>(true);if(label!=null)label.color=on?Color.white:new Color(.45f,.45f,.45f);}}
#if UNITY_EDITOR
    public void SetBindings(List<Binding> value){bindings=value??new List<Binding>();}
#endif
}
