using UnityEngine;

[CreateAssetMenu(fileName = "EquippedSkillDatabase", menuName = "Skills/Equipped Skill Database")]
public class EquippedSkillDatabase : ScriptableObject
{
    [Min(1)] public int slotCount = 3;
    public int[] defaultEquippedSkillIds = { 1, 0, 0 };
}
