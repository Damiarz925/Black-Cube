// Developer map: Inspector-editable catalog for active skill balance values.
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSkills", menuName = "Black Cube/Player Skill Catalog")]
public sealed class PlayerSkillCatalog : ScriptableObject
{
    public List<PlayerSkillDefinition> skills = new();
}
