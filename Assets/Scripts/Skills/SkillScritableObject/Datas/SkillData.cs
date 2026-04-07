using UnityEngine;

/// <summary>
/// 스킬의 고정 원본 데이터를 담는 ScriptableObject.
///
/// 이 클래스는 런타임 중 변하는 값(현재 쿨타임, 현재 타겟, 시전자 등)을 저장하지 않고,
/// 스킬 자체가 원래 가지고 있는 설정값만 보관한다.
///
/// 현재 포함하는 정보:
/// - 스킬 고유 ID
/// - 스킬 아이콘
/// - 이름/설명 로컬라이징 키
/// - 공격력 계수 퍼센트 관련 값
/// - 쿨타임 관련 값
///
/// 중요한 점:
/// 이 클래스의 데미지 값은 "고정 피해량"이 아니라
/// "현재 공격력의 몇 %로 피해를 입히는가"를 의미한다.
///
/// 예:
/// - damagePercent = 100 -> 공격력의 100%
/// - damagePercent = 200 -> 공격력의 200%
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Object/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("# Basic Info")]

    /// <summary>
    /// 스킬의 고유 ID.
    ///
    /// 용도:
    /// - RuntimeData에서 스킬 레벨 조회
    /// - SkillManager에서 SkillData 검색
    /// - 버튼/오토/저장 데이터와 연결
    ///
    /// 주의:
    /// 프로젝트 내에서 중복되지 않도록 관리해야 한다.
    /// </summary>
    public int id;

    /// <summary>
    /// 스킬 아이콘 이미지.
    ///
    /// 나중에 스킬 버튼, 툴팁, 스킬 상세 UI 등에 사용할 수 있다.
    /// </summary>
    public Sprite icon;

    [Header("# Localization Key")]

    /// <summary>
    /// 스킬 이름 로컬라이징 키.
    ///
    /// 예:
    /// "skill.onion_slice.name"
    /// </summary>
    public string nameKey;

    /// <summary>
    /// 스킬 설명 로컬라이징 키.
    ///
    /// 예:
    /// "skill.onion_slice.desc"
    /// </summary>
    public string descriptionKey;

    [Header("# Damage Percent Info")]

    /// <summary>
    /// 스킬의 기본 공격력 계수 퍼센트.
    ///
    /// 예:
    /// - 100 = 공격력의 100%
    /// - 200 = 공격력의 200%
    ///
    /// 값 예시:
    /// baseDamagePercent = 100
    /// damagePercentGrowth = 20
    /// level = 3
    /// -> 100 + (20 * 3) = 160
    /// -> 공격력의 160% 피해
    /// </summary>
    public float baseDamagePercent;

    /// <summary>
    /// 스킬 레벨이 오를 때마다 증가하는 공격력 계수 퍼센트.
    ///
    /// 현재 공식:
    /// finalDamagePercent = baseDamagePercent + (damagePercentGrowth * level)
    ///
    /// 예:
    /// - baseDamagePercent = 100
    /// - damagePercentGrowth = 25
    /// - level = 2
    /// -> 150
    /// -> 공격력의 150% 피해
    /// </summary>
    public float damagePercentGrowth;

    [Header("# Cooldown Info")]

    /// <summary>
    /// 스킬의 기본 쿨타임.
    ///
    /// 실제 최종 쿨타임은 GetCooldown(level) 에서 성장값과 함께 계산된다.
    /// </summary>
    public float baseCooldown;

    /// <summary>
    /// 스킬 레벨에 따른 쿨타임 성장값.
    ///
    /// 현재 공식:
    /// finalCooldown = baseCooldown + (cooldownGrowth * level)
    ///
    /// 예:
    /// - 음수면 레벨업할수록 쿨타임 감소
    /// - 양수면 레벨업할수록 쿨타임 증가
    /// - 0이면 고정 쿨타임
    /// </summary>
    public float cooldownGrowth;

    /// <summary>
    /// 쿨타임의 최소 제한값.
    ///
    /// 레벨이 높아져도 쿨타임이 0 이하로 내려가지 않도록 보정한다.
    /// </summary>
    public float minCooldown = 0.1f;

    [Header("# UI Detail Info")]
    public int uiCurrentValue;
    public int uiMaxValue = 100;

    [Header("# Combat Power Info")]
    public float combatPowerBaseContribution;
    public float combatPowerPerLevelContribution = 5f;

    /// <summary>
    /// 전달받은 스킬 레벨 기준으로 최종 공격력 계수 퍼센트를 계산한다.
    ///
    /// 현재 공식:
    /// baseDamagePercent + (damagePercentGrowth * level)
    ///
    /// 예:
    /// - 반환값 100 -> 공격력의 100%
    /// - 반환값 250 -> 공격력의 250%
    /// </summary>
    /// <param name="level">현재 스킬 레벨</param>
    /// <returns>레벨이 반영된 공격력 계수 퍼센트</returns>
    public float GetDamagePercent(int level)
    {
        return baseDamagePercent + (damagePercentGrowth * level);
    }

    /// <summary>
    /// 전달받은 스킬 레벨 기준으로 최종 쿨타임을 계산한다.
    ///
    /// 현재 공식:
    /// baseCooldown + (cooldownGrowth * level)
    ///
    /// 계산 후에는 minCooldown보다 작아지지 않도록 보정한다.
    /// </summary>
    /// <param name="level">현재 스킬 레벨</param>
    /// <returns>레벨이 반영된 최종 쿨타임</returns>
    public float GetCooldown(int level)
    {
        float cooldown = baseCooldown + (cooldownGrowth * level);
        return Mathf.Max(minCooldown, cooldown);
    }

    public float GetCombatPowerContribution(
        int level,
        float defaultBaseContribution,
        float defaultPerLevelContribution)
    {
        float baseContribution = combatPowerBaseContribution != 0f
            ? combatPowerBaseContribution
            : defaultBaseContribution;

        float perLevelContribution = combatPowerPerLevelContribution != 0f
            ? combatPowerPerLevelContribution
            : defaultPerLevelContribution;

        return baseContribution + (perLevelContribution * level);
    }
}
