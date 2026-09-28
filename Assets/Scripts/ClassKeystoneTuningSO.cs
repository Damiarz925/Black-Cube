using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/Passive Tree/Class Keystone Tuning")]
public sealed class ClassKeystoneTuningSO : ScriptableObject
{
    public List<ClassKeystoneDefinition> definitions = new();
    [HideInInspector] public bool percentageRegenerationAuthored;
}
