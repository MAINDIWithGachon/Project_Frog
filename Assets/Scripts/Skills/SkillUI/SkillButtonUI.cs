using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    private static readonly int CompleteTrigger = Animator.StringToHash("Complete");

    [SerializeField] private SkillManager skillManager;
    [SerializeField] private int skillId;
    [SerializeField] private Image coolTimeBG;
    [SerializeField] private Animator anim;

    private SkillData skillData;
    private bool wasCoolingDown;

    private void Start()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (skillManager == null)
        {
            Debug.LogError("[SkillButtonUI] SkillManager reference is missing.");
            return;
        }

        skillData = skillManager.GetSkillDataForUI(skillId);

        if (skillData == null)
            Debug.LogError($"[SkillButtonUI] SkillData not found. skillId: {skillId}");
    }

    private void Update()
    {
        if (skillManager == null || coolTimeBG == null || skillData == null)
            return;

        float remain = skillManager.GetRemainingCooldown(skillId);
        float maxCooldown = skillData.GetCooldown(skillManager.GetSkillLevelForUI(skillId));
        bool isCoolingDown = remain > 0f;

        if (maxCooldown <= 0f)
        {
            coolTimeBG.fillAmount = 0f;
            wasCoolingDown = false;
            return;
        }

        coolTimeBG.fillAmount = remain / maxCooldown;

        if (wasCoolingDown && !isCoolingDown && anim != null)
        {
            anim.ResetTrigger(CompleteTrigger);
            anim.SetTrigger(CompleteTrigger);
        }

        wasCoolingDown = isCoolingDown;
    }

    public void OnClickSkillButton()
    {
        Debug.Log("일단 눌림");
        if (skillManager == null)
        {
            Debug.LogError("[SkillButtonUI] SkillManager reference is missing.");
            return;
        }

       // if (skillId <= 0)
            //return;

        if (skillManager.TryCast(skillId, out SkillCastResult castResult))
        {
            wasCoolingDown = castResult.cooldown > 0f;
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
