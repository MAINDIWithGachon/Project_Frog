using TMPro;
using UnityEngine;

public class GrowthUpgradeCard : MonoBehaviour
{
    public enum GrowthStatType
    {
        Attack,
        Hp,
        CritDamage
    }

    [SerializeField] private GrowthStatType statType;

    public TMP_Text Lv_Text;
    public TMP_Text stat_Text;
    public TMP_Text statName_Text;

    private GrowthStatManager growthStatManager;

    public GrowthStatType StatType => statType;

    public void Init(GrowthStatManager manager, GrowthStatType type)
    {
        growthStatManager = manager;
        statType = type;
        Refresh();
    }

    public void Init(GrowthStatManager manager)
    {
        growthStatManager = manager;
        Refresh();
    }

    public void Init()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (growthStatManager == null)
            growthStatManager = FindAnyObjectByType<GrowthStatManager>();

        if (growthStatManager == null)
            return;

        int level = growthStatManager.GetGrowthStatLevel(statType);
        float value = growthStatManager.GetGrowthStatBonus(statType);

        if (Lv_Text != null)
            Lv_Text.text = $"Lv. {level}";

        if (stat_Text != null)
            stat_Text.text = FormatValue(statType, value);

        if (statName_Text != null)
            statName_Text.text = GetDisplayName(statType);
    }

    public void Upgrade()
    {
        if (growthStatManager == null)
            growthStatManager = FindAnyObjectByType<GrowthStatManager>();

        growthStatManager?.TryUpgradeStat(statType);
    }

    private static string GetDisplayName(GrowthStatType type)
    {
        switch (type)
        {
            case GrowthStatType.Attack:
                return "공격력";
            case GrowthStatType.Hp:
                return "체력";
            case GrowthStatType.CritDamage:
                return "치명타 피해";
            default:
                return string.Empty;
        }
    }

    private static string FormatValue(GrowthStatType type, float value)
    {
        switch (type)
        {
            case GrowthStatType.CritDamage:
                return $"+{value:0.#}%";
            default:
                return $"+{value:0.#}";
        }
    }
}
