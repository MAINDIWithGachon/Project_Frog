using UnityEngine;

[CreateAssetMenu(fileName = "CombatPowerTuningData", menuName = "Scriptable Object/CombatPowerTuningData")]
public class CombatPowerTuningData : ScriptableObject
{
    [Header("# Stat Weight")]
    public float attackWeight = 3f;
    public float maxHpWeight = 0.8f;
    public float hpRegenWeight = 10f;
    public float critChanceWeight = 2.5f;
    public float critDamageWeight = 1.5f;

    [Header("# Skill Fallback Weight")]
    public float defaultSkillBaseContribution = 10f;
    public float defaultSkillPerLevelContribution = 5f;
}
