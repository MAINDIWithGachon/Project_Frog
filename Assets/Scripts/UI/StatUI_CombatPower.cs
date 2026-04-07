using TMPro;
using UnityEngine;

public class StatUI_CombatPower : MonoBehaviour
{
    [Header("# Reference")]
    [SerializeField] private CombatPowerData combatPowerData;
    [SerializeField] private PlayerStatController playerStatController;

    [Header("# UI")]
    public TMP_Text combatPower_Text;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playerStatController != null)
            playerStatController.OnStatsRecalculated += RefreshCombatPowerUI;

        RefreshCombatPowerUI();
    }

    private void OnDisable()
    {
        if (playerStatController != null)
            playerStatController.OnStatsRecalculated -= RefreshCombatPowerUI;
    }

    public void RefreshCombatPowerUI()
    {
        if (combatPowerData == null)
            return;

        ChangeCombatPowerToABC(combatPowerData.currentCombatPower);
    }

    public void ChangeCombatPowerToABC(float value)
    {
        if (combatPower_Text == null)
            return;

        value = Mathf.Max(0f, value);

        if (value < 1000f)
        {
            combatPower_Text.text = Mathf.FloorToInt(value).ToString();
            return;
        }

        int suffixIndex = 0;
        float unitValue = 1000f;

        while (value >= unitValue * 100f)
        {
            unitValue *= 100f;
            suffixIndex++;
        }

        float displayValue = value / unitValue;
        displayValue = Mathf.Round(displayValue * 100f) / 100f;

        if (displayValue >= 100f)
        {
            unitValue *= 100f;
            suffixIndex++;
            displayValue = value / unitValue;
            displayValue = Mathf.Round(displayValue * 100f) / 100f;
        }

        combatPower_Text.text = $"{displayValue:0.##}{GetAlphabetSuffix(suffixIndex)}";
    }

    private string GetAlphabetSuffix(int index)
    {
        index = Mathf.Max(0, index);

        string suffix = string.Empty;

        do
        {
            suffix = (char)('A' + (index % 26)) + suffix;
            index = (index / 26) - 1;
        }
        while (index >= 0);

        return suffix;
    }

    private void ResolveReferences()
    {
        if (combatPowerData == null)
            combatPowerData = FindAnyObjectByType<CombatPowerData>();

        if (playerStatController == null)
            playerStatController = FindAnyObjectByType<PlayerStatController>();
    }
}
