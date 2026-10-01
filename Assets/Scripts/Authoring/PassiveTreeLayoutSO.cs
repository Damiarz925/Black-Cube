using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class PassiveClassBadge { public string classId; public Sprite sprite; }
[Serializable] public sealed class PassiveLayoutSlots { public int count; public Vector2[] anchors; public float[] rotations; }

[CreateAssetMenu(menuName = "Black-Cube/UI/Passive Tree Layout")]
public sealed class PassiveTreeLayoutSO : ScriptableObject
{
    public Sprite playerHub;
    public List<PassiveClassBadge> badges = new();
    public float hubSize = 360, badgeSize = 80, badgeRadius = 160;
    public Vector2 hubOffset;
    public float fitPadding = 100, maximumZoom = 1.35f, zoomStep = .08f, weaponGap = 220;
    public float classRowSpacing = 900f, classRowHeight = 760f;
    public List<PassiveLayoutSlots> layouts = new();
    public Sprite Badge(string id) => badges.Find(x => x.classId == id)?.sprite;
    public Vector2 Anchor(int count, int index)
    {
        var slots = layouts.Find(x => x.count == count);
        return slots != null && index < slots.anchors.Length ? slots.anchors[index] : new Vector2((index - (count - 1) * .5f) * 900, 90);
    }
    public float Rotation(int count, int index)
    { var slots = layouts.Find(x => x.count == count); return slots != null && slots.rotations != null && index < slots.rotations.Length ? slots.rotations[index] : 0; }
    public static List<string> PresentationOrder(IReadOnlyList<string> selected)
    {
        var result = new List<string>(selected);
        if (result.Count >= 3) { string native = result[0]; result.RemoveAt(0); result.Insert((result.Count + 1) / 2, native); }
        return result;
    }
}
