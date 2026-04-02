using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private int skillId;
    [SerializeField] private Image coolTimeBG;

    private SkillData skillData;

    private void Start()
    {
        if (skillManager == null)
        {
            Debug.LogError("[SkillButtonUI] SkillManager reference is missing.");
            return;
        }

        if (skillId <= 0)
            return;

        skillData = skillManager.GetSkillDataForUI(skillId);

        if (skillData == null)
            Debug.LogError($"[SkillButtonUI] SkillData not found. skillId: {skillId}");
    }

    private void Update()
    {
        if (skillId <= 0 || skillManager == null || coolTimeBG == null || skillData == null)
            return;

        float remain = skillManager.GetRemainingCooldown(skillId);
        float maxCooldown = skillData.GetCooldown(skillManager.GetSkillLevelForUI(skillId));

        if (maxCooldown <= 0f)
        {
            coolTimeBG.fillAmount = 0f;
            return;
        }

        coolTimeBG.fillAmount = remain / maxCooldown;
    }

    public void OnClickSkillButton()
    {
        if (skillManager == null)
        {
            Debug.LogError("[SkillButtonUI] SkillManager reference is missing.");
            return;
        }

        if (skillId <= 0)
            return;

        if (skillManager.TryCast(skillId, out SkillCastResult castResult))
        {
            Debug.Log(
                $"[SkillButtonUI] 스킬 사용 성공. skillId: {skillId}, " +
                $"skillLevel: {castResult.skillLevel}, cooldown: {castResult.cooldown}");
            return;
        }

        Debug.Log(
            $"[SkillButtonUI] 스킬 사용 실패. skillId: {skillId}, " +
            $"remainingCooldown: {skillManager.GetRemainingCooldown(skillId)}");

    }
    
}
