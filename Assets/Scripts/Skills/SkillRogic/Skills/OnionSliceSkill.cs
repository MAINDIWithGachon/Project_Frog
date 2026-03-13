using UnityEngine;

public class OnionSliceSkill : MonoBehaviour, ISkillExecutable
{
    public int SkillId => 0;

    public void Execute(SkillData skillData, SkillCastResult castResult)
    {
        Debug.Log("[OnionSliceSkill] Execute 호출됨");
        Debug.Log($"skillId: {skillData.id}");
        Debug.Log($"skillLevel: {castResult.skillLevel}");
        Debug.Log($"damagePercent: {castResult.damagePercent}");
        Debug.Log($"cooldown: {castResult.cooldown}");
        Debug.Log($"currentAttack: {castResult.currentAttack}");
        Debug.Log($"currentCritChance: {castResult.currentCritChance}");
        Debug.Log($"currentCritDamage: {castResult.currentCritDamage}");

        float finalDamage = castResult.currentAttack * (castResult.damagePercent / 100f);
        Debug.Log($"finalDamage: {finalDamage}");
    }
}