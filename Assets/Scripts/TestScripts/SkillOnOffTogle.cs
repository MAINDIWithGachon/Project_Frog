using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillOnOffTogle : MonoBehaviour
{
    [SerializeField] private bool isAuto;
    [SerializeField] private Image checkMark;
    [SerializeField] private TMP_Text onoffText;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private int skillId;

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (!isAuto || skillManager == null)
            return;

        if (!skillManager.CanCast(skillId))
            return;

        skillManager.TryCast(skillId, out _);
    }

    public void SkillTogleOnOff()
    {
        isAuto = !isAuto;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (checkMark != null)
            checkMark.enabled = isAuto;

        if (onoffText != null)
            onoffText.text = isAuto ? "AUTO ON" : "AUTO OFF";
    }
}
