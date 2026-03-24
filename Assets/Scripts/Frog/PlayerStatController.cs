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
    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private FinalStatData finalStatData;
    [SerializeField] private Health health;

    [Header("# Base Stat")]
    [SerializeField] private PlayerBaseStatData baseStatData;

    [Header("# Stat Growth Per Level")]
    [SerializeField] private float attackPerLevel = 5f;
    [SerializeField] private float hpPerLevel = 20f;
    [SerializeField] private float hpRegenPerLevel = 0.5f;
    [SerializeField] private float critChancePerLevel = 1f;
    [SerializeField] private float critDamagePerLevel = 5f;

    private void Awake()
    {
        if (runtimeData == null)
            runtimeData = GetComponent<RuntimeData>();

        if (runtimeData == null)
            runtimeData = FindAnyObjectByType<RuntimeData>();

        if (finalStatData == null)
            finalStatData = GetComponent<FinalStatData>();

        if (finalStatData == null)
            finalStatData = FindAnyObjectByType<FinalStatData>();

        if (baseStatData == null)
            baseStatData = GetComponent<PlayerBaseStatData>();

        if (health == null)
            health = GetComponent<Health>();
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

        var root = runtimeData.GetRoot();
        if (root == null || root.statLevels == null)
        {
            Debug.LogError("[PlayerStatController] RuntimeData root or statLevels is null.");
            return;
        }

        float previousMaxHp = finalStatData.maxHp;

        // 공격력 계산
        finalStatData.attack = CalculateAttack(root.statLevels.attackLevel);

        // 최대 체력 계산
        finalStatData.maxHp = CalculateMaxHp(root.statLevels.hpLevel);

        // 초당 체력 회복 계산
        finalStatData.hpRegenPerSecond = CalculateHpRegen(root.statLevels.hpRegenLevel);

        // 치명타 확률 계산
        finalStatData.critChance = CalculateCritChance(root.statLevels.critChanceLevel);

        // 치명타 공격력 계산
        finalStatData.critDamage = CalculateCritDamage(root.statLevels.critDamageLevel);

        if (health != null)
        {
            health.ApplyStatChanged(previousMaxHp);
        }
    }

    private float CalculateAttack(int attackLevel)
    {
        float baseValue = baseStatData.baseAttack;
        float statValue = attackLevel * attackPerLevel;
        float equipmentValue = GetEquipmentAttack();
        float recipeValue = GetRecipeAttack();
        float buffValue = GetBuffAttack();

        return baseValue + statValue + equipmentValue + recipeValue + buffValue;
    }

    private float CalculateMaxHp(int hpLevel)
    {
        float baseValue = baseStatData.baseMaxHp;
        float statValue = hpLevel * hpPerLevel;
        float equipmentValue = GetEquipmentHp();
        float recipeValue = GetRecipeHp();
        float buffValue = GetBuffHp();

        return baseValue + statValue + equipmentValue + recipeValue + buffValue;
    }

    private float CalculateHpRegen(int hpRegenLevel)
    {
        float baseValue = baseStatData.baseHpRegenPerSecond;
        float statValue = hpRegenLevel * hpRegenPerLevel;
        float equipmentValue = GetEquipmentHpRegen();
        float recipeValue = GetRecipeHpRegen();
        float buffValue = GetBuffHpRegen();

        return baseValue + statValue + equipmentValue + recipeValue + buffValue;
    }

    private float CalculateCritChance(int critChanceLevel)
    {
        float baseValue = baseStatData.baseCritChance;
        float statValue = critChanceLevel * critChancePerLevel;
        float equipmentValue = GetEquipmentCritChance();
        float recipeValue = GetRecipeCritChance();
        float buffValue = GetBuffCritChance();

        float finalValue = baseValue + statValue + equipmentValue + recipeValue + buffValue;

        // 치명타 확률은 최대 100% 제한
        return Mathf.Clamp(finalValue, 0f, 100f);
    }

    private float CalculateCritDamage(int critDamageLevel)
    {
        float baseValue = baseStatData.baseCritDamage;
        float statValue = critDamageLevel * critDamagePerLevel;
        float equipmentValue = GetEquipmentCritDamage();
        float recipeValue = GetRecipeCritDamage();
        float buffValue = GetBuffCritDamage();

        return baseValue + statValue + equipmentValue + recipeValue + buffValue;
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
