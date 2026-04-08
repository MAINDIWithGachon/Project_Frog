using UnityEngine;

public class StatUI : MonoBehaviour
{
    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private FinalStatData finalStatData;
    [SerializeField] private PlayerStatController playerStatController;

    [Header("# Contents")]
    [SerializeField] private StatContent[] statContents;

    [Header("# Upgrade Price")]
    [SerializeField] private int baseUpgradePrice = 100;
    [SerializeField] private int pricePerLevel = 25;

    private void Awake()
    {
        ResolveReferences();

        if (statContents == null || statContents.Length == 0)
            statContents = GetComponentsInChildren<StatContent>(true);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (playerStatController != null)
            playerStatController.OnStatsRecalculated += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (playerStatController != null)
            playerStatController.OnStatsRecalculated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (runtimeData == null || finalStatData == null || statContents == null)
            return;

        RuntimeData.RootData root = runtimeData.GetRoot();
        if (root == null || root.statLevels == null)
            return;

        for (int i = 0; i < statContents.Length; i++)
        {
            StatContent content = statContents[i];
            if (content == null)
                continue;

            int level = GetLevel(content.statType, root.statLevels);
            float value = GetFinalValue(content.statType);
            int price = GetUpgradePrice(level);

            if (content.text_Lv != null)
                content.text_Lv.text = $"Lv. {level}";

            if (content.text_Value != null)
                content.text_Value.text = FormatValue(content.statType, value);

            if (content.text_Price != null)
                content.text_Price.text = price.ToString();
        }
    }

    private void ResolveReferences()
    {
        if (runtimeData == null)
            runtimeData = FindAnyObjectByType<RuntimeData>();

        if (finalStatData == null)
            finalStatData = FindAnyObjectByType<FinalStatData>();

        if (playerStatController == null)
            playerStatController = FindAnyObjectByType<PlayerStatController>();
    }

    private int GetLevel(StatContent.StatType statType, RuntimeData.StatLevelData statLevels)
    {
        switch (statType)
        {
            case StatContent.StatType.ATK:
                return statLevels.attackLevel;
            case StatContent.StatType.HP:
                return statLevels.hpLevel;
            case StatContent.StatType.HpRegen:
                return statLevels.hpRegenLevel;
            case StatContent.StatType.CriChance:
                return statLevels.critChanceLevel;
            case StatContent.StatType.CriDamage:
                return statLevels.critDamageLevel;
            default:
                return 0;
        }
    }

    private float GetFinalValue(StatContent.StatType statType)
    {
        switch (statType)
        {
            case StatContent.StatType.ATK:
                return finalStatData.attack;
            case StatContent.StatType.HP:
                return finalStatData.maxHp;
            case StatContent.StatType.HpRegen:
                return finalStatData.hpRegenPerSecond;
            case StatContent.StatType.CriChance:
                return finalStatData.critChance;
            case StatContent.StatType.CriDamage:
                return finalStatData.critDamage;
            default:
                return 0f;
        }
    }

    private string FormatValue(StatContent.StatType statType, float value)
    {
        switch (statType)
        {
            case StatContent.StatType.CriChance:
            case StatContent.StatType.CriDamage:
                return $"{value:0.##}%";
            default:
                return value.ToString("0.##");
        }
    }

    private int GetUpgradePrice(int level)
    {
        return baseUpgradePrice + (level * pricePerLevel);
    }

    public void OnClickUpgradeAttack()
    {
        TryUpgradeStat(StatContent.StatType.ATK);
    }

    public void OnClickUpgradeHp()
    {
        TryUpgradeStat(StatContent.StatType.HP);
    }

    public void OnClickUpgradeHpRegen()
    {
        TryUpgradeStat(StatContent.StatType.HpRegen);
    }

    public void OnClickUpgradeCritChance()
    {
        TryUpgradeStat(StatContent.StatType.CriChance);
    }

    public void OnClickUpgradeCritDamage()
    {
        TryUpgradeStat(StatContent.StatType.CriDamage);
    }

    private void TryUpgradeStat(StatContent.StatType statType)
    {
        if (runtimeData == null)
        {
            Debug.LogWarning("[StatUI] RuntimeData reference is missing.");
            return;
        }

        RuntimeData.RootData root = runtimeData.GetRoot();
        if (root == null || root.statLevels == null)
        {
            Debug.LogWarning("[StatUI] Root data or statLevels is missing.");
            return;
        }

        int currentLevel = GetLevel(statType, root.statLevels);
        int cost = GetUpgradePrice(currentLevel);

        if (!runtimeData.SpendGold(cost))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }

        switch (statType)
        {
            case StatContent.StatType.ATK:
                root.statLevels.attackLevel++;
                break;
            case StatContent.StatType.HP:
                root.statLevels.hpLevel++;
                break;
            case StatContent.StatType.HpRegen:
                root.statLevels.hpRegenLevel++;
                break;
            case StatContent.StatType.CriChance:
                root.statLevels.critChanceLevel++;
                break;
            case StatContent.StatType.CriDamage:
                root.statLevels.critDamageLevel++;
                break;
            default:
                Debug.LogWarning($"[StatUI] Unsupported stat type: {statType}");
                return;
        }

        runtimeData.NotifyDataChanged();
    }
}
