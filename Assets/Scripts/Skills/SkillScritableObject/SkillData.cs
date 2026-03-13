using UnityEngine;

/// <summary>
/// 스킬의 "고정 원본 데이터"를 담는 ScriptableObject.
/// 
/// 이 클래스는 런타임에서 변하는 값(현재 쿨타임 남은 시간, 현재 시전자, 현재 타겟 등)을 저장하지 않는다.
/// 오직 스킬 자체가 원래 가지고 있는 설정값만 보관한다.
/// 
/// 예:
/// - 스킬 고유 ID
/// - 아이콘
/// - 이름/설명 키
/// - 기본 데미지
/// - 데미지 성장값
/// - 기본 쿨타임
/// - 쿨타임 성장값
/// 
/// 실제 사용 시에는:
/// 1. RuntimeData에서 스킬 레벨을 가져오고
/// 2. 이 SkillData의 GetDamage(level), GetCooldown(level)으로 최종값을 계산한다.
/// 
/// 즉, SkillData는 "이 스킬이 원래 어떤 스킬인가?"를 정의하는 데이터 에셋이다.
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Object/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("# Basic Info")]

    /// <summary>
    /// 스킬의 고유 ID.
    /// 
    /// 용도:
    /// - RuntimeData에서 해당 스킬의 레벨을 찾을 때 사용
    /// - SkillManager가 어떤 스킬인지 구분할 때 사용
    /// - 버튼, 오토 모드, 저장 데이터와 연결할 때 사용
    /// 
    /// 주의:
    /// 프로젝트 내에서 중복되지 않도록 관리해야 한다.
    /// </summary>
    public int skillID;

    /// <summary>
    /// 스킬 아이콘 이미지.
    /// 
    /// 현재 전투 구조를 먼저 만드는 단계라 UI에 바로 안 쓸 수도 있지만,
    /// 나중에 스킬 버튼, 툴팁, 상세 정보 UI에 그대로 재사용할 수 있다.
    /// </summary>
    public Sprite icon;

    [Header("# Localization Key")]

    /// <summary>
    /// 스킬 이름 로컬라이징 키.
    /// 
    /// 예:
    /// "skill.fireball.name"
    /// 
    /// 현재는 실제 UI 연결을 아직 안 할 수 있지만,
    /// 나중에 다국어 지원 시 이 key를 이용해 이름을 가져오게 된다.
    /// </summary>
    public string nameKey;

    /// <summary>
    /// 스킬 설명 로컬라이징 키.
    /// 
    /// 예:
    /// "skill.fireball.desc"
    /// 
    /// 현재는 구조만 먼저 잡는 단계이므로 string key만 보관하고,
    /// 나중에 UI 포맷터 또는 로컬라이징 시스템에서 이 값을 사용하면 된다.
    /// </summary>
    public string descriptionKey;

    [Header("# Damage Info")]

    /// <summary>
    /// 스킬의 기본 데미지.
    /// 
    /// 이 값은 스킬 레벨이 0 또는 최소 상태일 때의 기준값 역할을 한다.
    /// 실제 최종 데미지는 GetDamage(level)에서 성장값과 함께 계산된다.
    /// </summary>
    public float baseDamage;

    /// <summary>
    /// 스킬 레벨이 오를 때마다 증가하는 데미지 성장값.
    /// 
    /// 현재 공식:
    /// finalDamage = baseDamage + (damageGrowth * level)
    /// 
    /// 예:
    /// baseDamage = 100
    /// damageGrowth = 20
    /// level = 3
    /// -> 160
    /// 
    /// 주의:
    /// 현재 공식은 level 1부터 성장값이 1회 반영된다.
    /// 만약 "레벨 1 = 기본값" 구조로 바꾸고 싶다면
    /// 나중에 (level - 1) 방식으로 수정할 수 있다.
    /// </summary>
    public float damageGrowth;

    [Header("# Cooldown Info")]

    /// <summary>
    /// 스킬의 기본 쿨타임.
    /// 
    /// 이 값은 스킬 쿨타임 계산의 기준값이다.
    /// 실제 최종 쿨타임은 GetCooldown(level)에서 성장값과 함께 계산된다.
    /// </summary>
    public float baseCooldown;

    /// <summary>
    /// 스킬 레벨에 따라 쿨타임이 얼마나 변하는지 나타내는 성장값.
    /// 
    /// 현재 공식:
    /// finalCooldown = baseCooldown + (cooldownGrowth * level)
    /// 
    /// 예:
    /// - cooldownGrowth가 음수면 레벨업할수록 쿨타임 감소
    /// - cooldownGrowth가 양수면 레벨업할수록 쿨타임 증가
    /// - cooldownGrowth가 0이면 고정 쿨타임
    /// 
    /// 따라서 이 값 하나로 다양한 성장 패턴을 만들 수 있다.
    /// </summary>
    public float cooldownGrowth;

    /// <summary>
    /// 쿨타임의 최소 제한값.
    /// 
    /// 레벨이 너무 높아져서 쿨타임이 0 이하가 되는 상황을 방지하기 위한 안전장치다.
    /// 
    /// 예:
    /// baseCooldown = 3
    /// cooldownGrowth = -0.5
    /// level이 높아지면 계산 결과가 0 이하가 될 수도 있는데,
    /// 그 경우에도 최소 minCooldown 이상은 유지하게 만든다.
    /// </summary>
    public float minCooldown = 0.1f;

    /// <summary>
    /// 전달받은 스킬 레벨을 기준으로 최종 데미지를 계산해 반환한다.
    /// 
    /// 현재 공식:
    /// baseDamage + (damageGrowth * level)
    /// 
    /// 이 함수는 "스킬 데이터 원본값 + 레벨"을 바탕으로
    /// 스킬 자체의 최종 데미지 수치를 계산하는 역할만 한다.
    /// 
    /// 주의:
    /// 여기서 계산된 값은 "스킬 자체의 데미지 값"이며,
    /// 나중에 캐릭터 공격력, 버프, 치명타, 방어력 등을 포함한
    /// 최종 피해 계산은 별도의 계산 단계에서 추가될 수 있다.
    /// </summary>
    /// <param name="level">현재 스킬 레벨</param>
    /// <returns>레벨이 반영된 스킬 데미지</returns>
    public float GetDamage(int level)
    {
        return baseDamage + (damageGrowth * level);
    }

    /// <summary>
    /// 전달받은 스킬 레벨을 기준으로 최종 쿨타임을 계산해 반환한다.
    /// 
    /// 현재 공식:
    /// baseCooldown + (cooldownGrowth * level)
    /// 
    /// 계산 후에는 Mathf.Max(minCooldown, cooldown)을 사용하여
    /// 최종 결과가 minCooldown보다 작아지지 않도록 보정한다.
    /// 
    /// 예:
    /// baseCooldown = 5
    /// cooldownGrowth = -0.5
    /// level = 20
    /// -> 계산상 음수가 될 수 있어도 최소 minCooldown 이상으로 유지
    /// </summary>
    /// <param name="level">현재 스킬 레벨</param>
    /// <returns>레벨이 반영된 최종 쿨타임</returns>
    public float GetCooldown(int level)
    {
        float cooldown = baseCooldown + (cooldownGrowth * level);
        return Mathf.Max(minCooldown, cooldown);
    }
}