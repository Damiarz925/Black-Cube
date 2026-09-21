// Serialized presentation bindings. Layout remains on authored RectTransforms.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIAuthoringScreenKind { GameplayHUD, Inventory, PassiveTree, PauseMenu, Stats, Crafting, Challenges, Subclass, SkillSelection, MainMenu, CharacterSlots, ModList, Options, Rebirth, Tooltip }

[Serializable]
public sealed class PassiveChoiceGroupBinding
{
    public RectTransform junction;
    public GameObject nativeLayoutRoot, genericLayoutRoot;
    public PassiveNodeBinding a, b, c, subclassSlot;
    public PassiveNodeBinding genericA, genericB, genericC;
    public IEnumerable<PassiveNodeBinding> Nodes { get { yield return a; yield return b; yield return c; if (subclassSlot != null) yield return subclassSlot; if (genericA != null) yield return genericA; if (genericB != null) yield return genericB; if (genericC != null) yield return genericC; } }
    public IEnumerable<PassiveNodeBinding> RuntimeNodes(bool native) { if (native) { yield return a; yield return b; yield return c; if (subclassSlot != null) yield return subclassSlot; } else { yield return genericA ?? a; yield return genericB ?? b; yield return genericC ?? c; } }
    public void SetNativeLayout(bool native) { if (nativeLayoutRoot != null) nativeLayoutRoot.SetActive(native); if (genericLayoutRoot != null) genericLayoutRoot.SetActive(!native); }
}

[Serializable]
public sealed class PassiveTierViewBinding
{
    [Range(1, 10)] public int tier = 1;
    public PassiveNodeBinding spine;
    public PassiveChoiceGroupBinding left = new(), right = new();
}
