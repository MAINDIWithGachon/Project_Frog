using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillOnOffTogle : MonoBehaviour
{
    [SerializeField] private bool isAuto;
    [SerializeField] private Image checkMark;
    [SerializeField] private TMP_Text onoffText;

    private void Start()
    {
        RefreshUI();
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
