using System;
using UnityEngine;

/// <summary>
/// 플레이어의 최종 능력치를 계산하고 FinalStatData를 최신화하는 컨트롤러.
/// 
/// 역할:
/// 1. RuntimeData에 저장된 스탯 레벨을 읽는다.
/// 2. PlayerBaseStatData의 기본 능력치를 읽는다.
/// 3. 스탯 / 장비 / 레시피 / 버프 값을 각각 계산한다.
/// 4. 모든 값을 합산해서 FinalStatData에 기록한다.
/// 
/// 주의:
/// 이 클래스는 "최종 스탯 최신화" 담당이다.
/// 실제 스킬 피해량 계산은 스킬 발동 시점에 FinalStatData를 참조하여 별도로 계산한다.
/// </summary>
public class PlayerStatController : MonoBehaviour
{
    public event Action OnStatsRecalculated;

    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private FinalStatData finalStatData;
    [SerializeField] private CombatPowerData combatPowerData;
    [SerializeField] private Health health;

    [Header("# Base Stat")]
    [SerializeField] private PlayerBaseStatData baseStatData;

    [Header("# Combat Power")]
    [SerializeField] private CombatPowerTuningData combatPowerTuningData;
    [SerializeField] private SkillData[] skillDatabase;

    [Header("# Stat Growth Per Level")]
    [SerializeField] private float attackPerLevel = 5f;
    [SerializeField] private float hpPerLevel = 20f;
    [SerializeField] private float hpRegenPerLevel = 0.5f;
    [SerializeField] private float critChancePerLevel = 1f;
    [SerializeField] private float critDamagePerLevel = 5f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (runtimeData != null)
            runtimeData.OnDataChanged += RecalculateStats;

    }

    private void OnDisable()
    {
        if (runtimeData != null)
            runtimeData.OnDataChanged -= RecalculateStats;

    }

    private void Start()
    {
        RecalculateStats();
    }

    /// <summary>
    /// 현재 기준으로 플레이어의 최종 능력치를 다시 계산하여 FinalStatData에 반영한다.
    /// 
    /// 이 함수는 다음 상황에서 호출하면 된다.
    /// - 게임 시작 시
    /// - 스탯 강화 시
    /// - 장비 변경 시
    /// - 레시피 적용 시
    /// - 버프 적용 / 해제 시
    /// </summary>
    public void RecalculateStats()
    {
        if (runtimeData == null)
        {
            Debug.LogError("[PlayerStatController] RuntimeData reference is missing.");
            return;
        }

        if (finalStatData == null)
        {
            Debug.LogError("[PlayerStatController] FinalStatData reference is missing.");
            return;
        }

        if (baseStatData == null)
        {
            Debug.LogError("[PlayerStatController] PlayerBaseStatData reference is missing.");
            return;
        }

        RuntimeData.RootData root = runtimeData.GetRoot();
        if (root == null || root.statLevels == null)
        {
            Debug.LogError("[PlayerStatController] RuntimeData root or statLevels is null.");
            return;
        }

        float previousMaxHp = finalStatData.maxHp;

        StatContribution attackContribution = CreateAttackContribution(root.statLevels.attackLevel);
        StatContribution maxHpContribution = CreateMaxHpContribution(root.statLevels.hpLevel);
        StatContribution hpRegenContribution = CreateHpRegenContribution(root.statLevels.hpRegenLevel);
        StatContribution critChanceContribution = CreateCritChanceContribution(root.statLevels.critChanceLevel);
        StatContribution critDamageContribution = CreateCritDamageContribution(root.statLevels.critDamageLevel);

        finalStatData.attack = attackContribution.FinalValue;
        finalStatData.maxHp = maxHpContribution.FinalValue;
        finalStatData.hpRegenPerSecond = hpRegenContribution.FinalValue;
        finalStatData.critChance = Mathf.Clamp(critChanceContribution.FinalValue, 0f, 100f);
        finalStatData.critDamage = critDamageContribution.FinalValue;

        UpdateCombatPowerData(
            root,
            attackContribution,
            maxHpContribution,
            hpRegenContribution,
            critChanceContribution,
            critDamageContribution);

        if (health != null)
        {
            health.ApplyStatChanged(previousMaxHp);
        }

        OnStatsRecalculated?.Invoke();
    }

    private void ResolveReferences()
    {
        if (runtimeData == null)
            runtimeData = GetComponent<RuntimeData>();

        if (runtimeData == null)
            runtimeData = FindAnyObjectByType<RuntimeData>();

        if (finalStatData == null)
            finalStatData = GetComponent<FinalStatData>();

        if (finalStatData == null)
            finalStatData = FindAnyObjectByType<FinalStatData>();

        if (combatPowerData == null)
            combatPowerData = GetComponent<CombatPowerData>();

        if (combatPowerData == null)
            combatPowerData = FindAnyObjectByType<CombatPowerData>();

        if (baseStatData == null)
            baseStatData = GetComponent<PlayerBaseStatData>();

        if (health == null)
            health = GetComponent<Health>();
    }

    private StatContribution CreateAttackContribution(int attackLevel)
    {
        float baseValue = baseStatData.baseAttack;
        float statValue = attackLevel * attackPerLevel;
        float equipmentValue = GetEquipmentAttack();
        float recipeValue = GetRecipeAttack();
        float buffValue = GetBuffAttack();

        return new StatContribution(baseValue, statValue, equipmentValue, recipeValue, buffValue);
    }

    private StatContribution CreateMaxHpContribution(int hpLevel)
    {
        float baseValue = baseStatData.baseMaxHp;
        float statValue = hpLevel * hpPerLevel;
        float equipmentValue = GetEquipmentHp();
        float recipeValue = GetRecipeHp();
        float buffValue = GetBuffHp();

        return new StatContribution(baseValue, statValue, equipmentValue, recipeValue, buffValue);
    }

    private StatContribution CreateHpRegenContribution(int hpRegenLevel)
    {
        float baseValue = baseStatData.baseHpRegenPerSecond;
        float statValue = hpRegenLevel * hpRegenPerLevel;
        float equipmentValue = GetEquipmentHpRegen();
        float recipeValue = GetRecipeHpRegen();
        float buffValue = GetBuffHpRegen();

        return new StatContribution(baseValue, statValue, equipmentValue, recipeValue, buffValue);
    }

    private StatContribution CreateCritChanceContribution(int critChanceLevel)
    {
        float baseValue = baseStatData.baseCritChance;
        float statValue = critChanceLevel * critChancePerLevel;
        float equipmentValue = GetEquipmentCritChance();
        float recipeValue = GetRecipeCritChance();
        float buffValue = GetBuffCritChance();

        return new StatContribution(baseValue, statValue, equipmentValue, recipeValue, buffValue);
    }

    private StatContribution CreateCritDamageContribution(int critDamageLevel)
    {
        float baseValue = baseStatData.baseCritDamage;
        float statValue = critDamageLevel * critDamagePerLevel;
        float equipmentValue = GetEquipmentCritDamage();
        float recipeValue = GetRecipeCritDamage();
        float buffValue = GetBuffCritDamage();

        return new StatContribution(baseValue, statValue, equipmentValue, recipeValue, buffValue);
    }

    private void UpdateCombatPowerData(
        RuntimeData.RootData root,
        StatContribution attackContribution,
        StatContribution maxHpContribution,
        StatContribution hpRegenContribution,
        StatContribution critChanceContribution,
        StatContribution critDamageContribution)
    {
        if (combatPowerData == null || combatPowerTuningData == null)
            return;

        float statCombatPower =
            (attackContribution.CombatPowerValue * combatPowerTuningData.attackWeight) +
            (maxHpContribution.CombatPowerValue * combatPowerTuningData.maxHpWeight) +
            (hpRegenContribution.CombatPowerValue * combatPowerTuningData.hpRegenWeight) +
            (Mathf.Clamp(critChanceContribution.CombatPowerValue, 0f, 100f) * combatPowerTuningData.critChanceWeight) +
            (critDamageContribution.CombatPowerValue * combatPowerTuningData.critDamageWeight);

        float skillCombatPower = CalculateSkillCombatPower(root);

        combatPowerData.SetData(
            attackContribution,
            maxHpContribution,
            hpRegenContribution,
            critChanceContribution,
            critDamageContribution,
            statCombatPower,
            skillCombatPower);
    }

    private float CalculateSkillCombatPower(RuntimeData.RootData root)
    {
        if (root == null || root.skillLevels == null || combatPowerTuningData == null)
            return 0f;

        float total = 0f;

        for (int i = 0; i < root.skillLevels.Length; i++)
        {
            RuntimeData.SkillLevelData skillLevelData = root.skillLevels[i];
            SkillData skillData = FindSkillData(skillLevelData.skillId);

            if (skillData != null)
            {
                total += skillData.GetCombatPowerContribution(
                    skillLevelData.level,
                    combatPowerTuningData.defaultSkillBaseContribution,
                    combatPowerTuningData.defaultSkillPerLevelContribution);
                continue;
            }

            total += combatPowerTuningData.defaultSkillBaseContribution +
                     (combatPowerTuningData.defaultSkillPerLevelContribution * skillLevelData.level);
        }

        return total;
    }

    private SkillData FindSkillData(int skillId)
    {
        if (skillDatabase == null)
            return null;

        for (int i = 0; i < skillDatabase.Length; i++)
        {
            SkillData skillData = skillDatabase[i];
            if (skillData != null && skillData.id == skillId)
                return skillData;
        }

        return null;
    }

    // =========================
    // Equipment Stat
    // =========================

    private float GetEquipmentAttack()
    {
        return 0f;
    }

    private float GetEquipmentHp()
    {
        return 0f;
    }

    private float GetEquipmentHpRegen()
    {
        return 0f;
    }

    private float GetEquipmentCritChance()
    {
        return 0f;
    }

    private float GetEquipmentCritDamage()
    {
        return 0f;
    }

    // =========================
    // Recipe Stat
    // =========================

    private float GetRecipeAttack()
    {
        return 0f;
    }

    private float GetRecipeHp()
    {
        return 0f;
    }

    private float GetRecipeHpRegen()
    {
        return 0f;
    }

    private float GetRecipeCritChance()
    {
        return 0f;
    }

    private float GetRecipeCritDamage()
    {
        return 0f;
    }

    // =========================
    // Buff Stat
    // =========================

    private float GetBuffAttack()
    {
        return 0f;
    }

    private float GetBuffHp()
    {
        return 0f;
    }

    private float GetBuffHpRegen()
    {
        return 0f;
    }

    private float GetBuffCritChance()
    {
        return 0f;
    }

    private float GetBuffCritDamage()
    {
        return 0f;
    }
}
