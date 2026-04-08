using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillOnOffTogle : MonoBehaviour
{
    [SerializeField] private bool isAuto;
    [SerializeField] private GameObject[] onOff;//0은 on, 1은 off
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
        if (onOff == null || onOff.Length < 2)
            return;

        if (onOff[0] != null)
            onOff[0].SetActive(isAuto);

        if (onOff[1] != null)
            onOff[1].SetActive(!isAuto);
    }

}
