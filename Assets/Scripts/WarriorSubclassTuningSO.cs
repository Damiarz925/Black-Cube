using UnityEngine;

[CreateAssetMenu(menuName="Black-Cube/Warrior Subclass Tuning",fileName="SO_WarriorSubclassTuning")]
public sealed class WarriorSubclassTuningSO : ScriptableObject
{
    [SerializeField,Range(0f,.95f)] float momentumLessDamage=.20f;
    public float MomentumLessDamage=>momentumLessDamage;
}
