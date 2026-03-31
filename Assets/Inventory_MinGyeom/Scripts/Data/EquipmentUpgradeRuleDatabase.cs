using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 희귀도와 현재 레벨에 따른 장비 강화 비용 규칙을 관리하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(
    fileName = "EquipmentUpgradeRuleDatabase",
    menuName = "Project Frog/Equipment/Equipment Upgrade Rule Database")]
public class EquipmentUpgradeRuleDatabase : ScriptableObject
{
    [Serializable]
    public class RuleEntry
    {
        public EquipmentRarity rarity;
        [Min(1)] public int currentLevel = 1;
        [Min(0)] public int requiredDuplicateCount = 1;
        [Min(0)] public int requiredGold = 100;
        [Min(0)] public int requiredUpgradeStone = 5;
    }

    [Header("Rules")]
    [SerializeField] private List<RuleEntry> rules = new();

    [Header("Default Rule Generation")]
    [SerializeField] [Min(1)] private int defaultMaxLevel = 10;
    [SerializeField] [Min(0)] private int defaultDuplicateCount = 10;

    public IReadOnlyList<RuleEntry> Rules => rules;

    public bool TryGetRule(EquipmentRarity rarity, int currentLevel, out RuleEntry rule)
    {
        int normalizedLevel = Mathf.Max(1, currentLevel);

        for (int i = 0; i < rules.Count; i++)
        {
            RuleEntry candidate = rules[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.rarity != rarity || candidate.currentLevel != normalizedLevel)
            {
                continue;
            }

            rule = candidate;
            return true;
        }

        rule = null;
        return false;
    }

    public void SetRule(
        EquipmentRarity rarity,
        int currentLevel,
        int requiredDuplicateCount,
        int requiredGold,
        int requiredUpgradeStone)
    {
        int normalizedLevel = Mathf.Max(1, currentLevel);
        RuleEntry entry = FindOrCreateRule(rarity, normalizedLevel);
        entry.requiredDuplicateCount = Mathf.Max(0, requiredDuplicateCount);
        entry.requiredGold = Mathf.Max(0, requiredGold);
        entry.requiredUpgradeStone = Mathf.Max(0, requiredUpgradeStone);
    }

    [ContextMenu("Populate Default Rules")]
    public void PopulateDefaultRules()
    {
        rules.Clear();

        Array rarityValues = Enum.GetValues(typeof(EquipmentRarity));
        for (int rarityIndex = 0; rarityIndex < rarityValues.Length; rarityIndex++)
        {
            EquipmentRarity rarity = (EquipmentRarity)rarityValues.GetValue(rarityIndex);

            for (int level = 1; level <= defaultMaxLevel; level++)
            {
                rules.Add(new RuleEntry
                {
                    rarity = rarity,
                    currentLevel = level,
                    requiredDuplicateCount = defaultDuplicateCount,
                    requiredGold = GetDefaultGold(rarity, level),
                    requiredUpgradeStone = GetDefaultUpgradeStone(rarity, level)
                });
            }
        }

        SortRules();
    }

    private RuleEntry FindOrCreateRule(EquipmentRarity rarity, int currentLevel)
    {
        for (int i = 0; i < rules.Count; i++)
        {
            RuleEntry candidate = rules[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.rarity != rarity || candidate.currentLevel != currentLevel)
            {
                continue;
            }

            return candidate;
        }

        RuleEntry created = new()
        {
            rarity = rarity,
            currentLevel = currentLevel
        };

        rules.Add(created);
        SortRules();
        return created;
    }

    private void SortRules()
    {
        rules.Sort((left, right) =>
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int rarityCompare = left.rarity.CompareTo(right.rarity);
            if (rarityCompare != 0)
            {
                return rarityCompare;
            }

            return left.currentLevel.CompareTo(right.currentLevel);
        });
    }

    private void Reset()
    {
        PopulateDefaultRules();
    }

    private void OnValidate()
    {
        for (int i = 0; i < rules.Count; i++)
        {
            RuleEntry rule = rules[i];
            if (rule == null)
            {
                continue;
            }

            rule.currentLevel = Mathf.Max(1, rule.currentLevel);
            rule.requiredDuplicateCount = Mathf.Max(0, rule.requiredDuplicateCount);
            rule.requiredGold = Mathf.Max(0, rule.requiredGold);
            rule.requiredUpgradeStone = Mathf.Max(0, rule.requiredUpgradeStone);
        }

        SortRules();
    }

    private static int GetDefaultGold(EquipmentRarity rarity, int currentLevel)
    {
        int rarityMultiplier = rarity switch
        {
            EquipmentRarity.Common => 1,
            EquipmentRarity.Magic => 2,
            EquipmentRarity.Rare => 4,
            EquipmentRarity.Epic => 7,
            EquipmentRarity.Legendary => 11,
            _ => 1
        };

        return 100 * rarityMultiplier * Mathf.Max(1, currentLevel);
    }

    private static int GetDefaultUpgradeStone(EquipmentRarity rarity, int currentLevel)
    {
        int rarityMultiplier = rarity switch
        {
            EquipmentRarity.Common => 1,
            EquipmentRarity.Magic => 2,
            EquipmentRarity.Rare => 3,
            EquipmentRarity.Epic => 5,
            EquipmentRarity.Legendary => 8,
            _ => 1
        };

        return 5 * rarityMultiplier * Mathf.Max(1, currentLevel);
    }
}
