using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 스킬 시전을 관리하는 최소 매니저.
///
/// 현재 단계에서 담당하는 역할:
/// 1. skillId로 SkillData를 찾는다.
/// 2. RuntimeData에서 해당 스킬의 현재 레벨을 가져온다.
/// 3. FinalStatData에서 현재 최종 공격력 / 치명타 스탯을 가져온다.
/// 4. SkillData를 이용해 공격력 계수 퍼센트와 쿨타임을 계산한다.
/// 5. 쿨타임 가능 여부를 검사한다.
/// 6. 시전 성공 시 이번 시전에 필요한 계산 결과를 SkillCastResult로 반환한다.
///
/// 아직 담당하지 않는 것:
/// - 실제 투사체 생성
/// - 실제 근접 판정 생성
/// - 실제 몬스터 타격 처리
/// - 스킬별 프리팹 실행
///
/// 즉, 이 클래스는 "지금 이 스킬을 사용할 수 있는가?" 와
/// "사용한다면 어떤 계산값으로 시작해야 하는가?" 까지 담당한다.
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private FinalStatData finalStatData;
    [SerializeField] private Transform casterSlashPivot;
    [SerializeField] private Movement casterMovement;

    [Header("# Skill Database")]
    [SerializeField] private SkillData[] skillDatabase;
    [SerializeField] private List<int> equippedSkillIds = new() { 0, 1, 6, -1 };

    /// <summary>
    /// 각 스킬의 다음 사용 가능 시각(Time.time 기준)을 저장한다.
    ///
    /// key   : skillId
    /// value : 다음 사용 가능 시간
    /// </summary>
    private readonly Dictionary<int, float> nextAvailableTimeBySkillId = new();
    private readonly Dictionary<int, ISkillExecutable> skillExecutablesById = new();
    private readonly Dictionary<int, SkillDetailData> uiSkillDetailsById = new();

    private Transform runtimeSkillExecutorRoot;

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

        InitializeEquippedSkills();
        RegisterSkillExecutables();
    }

    /// <summary>
    /// 특정 스킬 사용을 시도한다.
    ///
    /// 성공 시:
    /// - 쿨타임을 시작한다.
    /// - 이번 시전에 필요한 계산 결과를 result로 반환한다.
    ///
    /// 실패 시:
    /// - false를 반환한다.
    /// - result는 기본값이다.
    /// </summary>
    /// <param name="skillId">사용할 스킬의 고유 ID</param>
    /// <param name="result">이번 시전용 계산 결과</param>
    /// <returns>시전 성공 여부</returns>
    public bool TryCast(int skillId, out SkillCastResult result)
    {
        result = default;

        if (runtimeData == null || finalStatData == null)
        {
            Debug.LogError("[SkillManager] RuntimeData or FinalStatData reference is missing.");
            return false;
        }

        SkillData skillData = GetSkillData(skillId);
        if (skillData == null)
            return false;

        int skillLevel = runtimeData.GetSkillLevel(skillId);

        if (!CanCast(skillData, skillLevel))
            return false;

        result = BuildCastResult(skillData, skillLevel);

        if (!SpawnAndExecuteSkill(skillData, result))
        {
            result = default;
            return false;
        }

        StartCooldown(skillId, result.cooldown);
        return true;
    }

    private bool SpawnAndExecuteSkill(SkillData skillData, SkillCastResult result)
    {
        if (!TryGetSkillExecutable(skillData.id, out ISkillExecutable executable))
        {
            Debug.LogError($"[SkillManager] 실행 가능한 스킬을 찾지 못했습니다. skillId: {skillData.id}");
            return false;
        }
        executable.Execute(skillData, result);
        return true;
    }

    /// <summary>
    /// 특정 스킬이 현재 사용 가능한지 검사한다.
    ///
    /// 현재는 쿨타임만 검사한다.
    /// 나중에 필요하면 마나, 침묵, 상태이상, 타겟 유무 등을 추가할 수 있다.
    /// </summary>
    /// <param name="skillId">검사할 스킬의 고유 ID</param>
    /// <returns>사용 가능 여부</returns>
    public bool CanCast(int skillId)
    {
        if (runtimeData == null)
            return false;

        SkillData skillData = GetSkillData(skillId);
        if (skillData == null)
            return false;

        int skillLevel = runtimeData.GetSkillLevel(skillId);
        return CanCast(skillData, skillLevel);
    }

    /// <summary>
    /// 실제 시전 가능 여부 검사 내부 함수.
    /// </summary>
    private bool CanCast(SkillData skillData, int skillLevel)
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlayerDead)
            return false;

        float currentTime = Time.time;
        float nextAvailableTime = GetNextAvailableTime(skillData.id);

        // 아직 다음 사용 가능 시각이 안 왔으면 사용 불가
        if (currentTime < nextAvailableTime)
            return false;

        // 나중에 추가 가능:
        // - 마나 검사
        // - 침묵 상태 검사
        // - 행동 불가 상태 검사
        // - 타겟 존재 여부 검사

        return true;
    }

    /// <summary>
    /// 이번 시전에 필요한 계산 결과를 생성한다.
    ///
    /// 현재 포함 값:
    /// - 스킬 ID
    /// - 스킬 레벨
    /// - 공격력 계수 퍼센트
    /// - 쿨타임
    /// - 시전 시점의 현재 공격력
    /// - 시전 시점의 현재 치명타 확률
    /// - 시전 시점의 현재 치명타 공격력
    /// </summary>
    private SkillCastResult BuildCastResult(SkillData skillData, int skillLevel)
    {
        EnsureCasterReferences();

        float damagePercent = skillData.GetDamagePercent(skillLevel);
        float cooldown = skillData.GetCooldown(skillLevel);

        SkillCastResult result = new SkillCastResult
        {
            skillId = skillData.id,
            skillLevel = skillLevel,

            // 스킬 데이터 기준 계산값
            damagePercent = damagePercent,
            cooldown = cooldown,

            // 시전 시점의 플레이어 현재 최종 능력치
            currentAttack = finalStatData.attack,
            currentCritChance = finalStatData.critChance,
            currentCritDamage = finalStatData.critDamage,
            slashPivot = casterSlashPivot,
            facingDirection = casterMovement != null && casterMovement.direction < 0 ? -1 : 1
        };

        return result;
    }

    private void EnsureCasterReferences()
    {
        if (casterSlashPivot != null && casterMovement != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            if (casterMovement == null)
                casterMovement = playerObject.GetComponentInChildren<Movement>(true);

            if (casterSlashPivot == null)
                casterSlashPivot = FindChildTransformByName(playerObject.transform, "SlashPivot");
        }

        if (casterMovement == null)
            casterMovement = FindAnyObjectByType<Movement>();

        if (casterSlashPivot == null && casterMovement != null)
            casterSlashPivot = FindChildTransformByName(casterMovement.transform.root, "SlashPivot");
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }

    /// <summary>
    /// skillId에 해당하는 SkillData를 skillDatabase에서 찾아 반환한다.
    /// </summary>
    private SkillData GetSkillData(int skillId)
    {
        if (skillDatabase == null)
            return null;

        for (int i = 0; i < skillDatabase.Length; i++)
        {
            if (skillDatabase[i] != null && skillDatabase[i].id == skillId)
                return skillDatabase[i];
        }

        return null;
    }
    /// <summary>
    /// UI에서 특정 스킬의 SkillData를 참조할 때 사용하는 공개 메서드.
    /// 내부 검색은 기존 GetSkillData()를 그대로 사용한다.
    /// </summary>
    public SkillData GetSkillDataForUI(int skillId)
    {
        return GetSkillData(skillId);
    }

    public SkillDetailData GetSkillDetailDataForUI(int skillId)
    {
        SkillData skillData = GetSkillData(skillId);
        if (skillData == null)
        {
            if (uiSkillDetailsById.TryGetValue(skillId, out SkillDetailData registeredDetail))
            {
                return new SkillDetailData
                {
                    skillId = registeredDetail.skillId,
                    skillName = registeredDetail.skillName,
                    description = registeredDetail.description,
                    icon = registeredDetail.icon,
                    currentValue = registeredDetail.currentValue,
                    maxValue = registeredDetail.maxValue
                };
            }

            return null;
        }

        int skillLevel = GetSkillLevelForUI(skillId);

        return new SkillDetailData
        {
            skillId = skillData.id,
            skillName = string.IsNullOrWhiteSpace(skillData.nameKey) ? $"Skill {skillData.id}" : skillData.nameKey,
            description = string.IsNullOrWhiteSpace(skillData.descriptionKey) ? $"Skill {skillData.id}" : skillData.descriptionKey,
            icon = skillData.icon,
            currentValue = skillData.uiCurrentValue > 0 ? skillData.uiCurrentValue : skillLevel,
            maxValue = skillData.uiMaxValue > 0 ? skillData.uiMaxValue : Mathf.Max(1, skillLevel)
        };
    }

    public void RegisterSkillDetailDataForUI(SkillDetailData detailData)
    {
        if (detailData == null || detailData.skillId <= 0)
            return;

        uiSkillDetailsById[detailData.skillId] = new SkillDetailData
        {
            skillId = detailData.skillId,
            skillName = detailData.skillName,
            description = detailData.description,
            icon = detailData.icon,
            currentValue = detailData.currentValue,
            maxValue = detailData.maxValue
        };
    }

    public bool TryResolveSkillIdForUI(Sprite iconSprite, string skillName, out int skillId)
    {
        skillId = 0;

        if (skillDatabase == null)
            return false;

        for (int i = 0; i < skillDatabase.Length; i++)
        {
            SkillData candidate = skillDatabase[i];
            if (candidate == null)
                continue;

            if (iconSprite != null && candidate.icon == iconSprite)
            {
                skillId = candidate.id;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(skillName) && string.Equals(candidate.nameKey, skillName, StringComparison.OrdinalIgnoreCase))
            {
                skillId = candidate.id;
                return true;
            }
        }

        return false;
    }

    public int GetEquippedSlotCount()
    {
        return equippedSkillIds != null ? equippedSkillIds.Count : 0;
    }

    public int GetEquippedSkillIdAtSlot(int slotIndex)
    {
        if (equippedSkillIds == null || slotIndex < 0 || slotIndex >= equippedSkillIds.Count)
            return 0;

        return equippedSkillIds[slotIndex];
    }

    public bool IsSkillEquipped(int skillId)
    {
        return TryFindEquippedSlotIndex(skillId, out _);
    }

    public bool TryFindEquippedSlotIndex(int skillId, out int slotIndex)
    {
        slotIndex = -1;

        if (skillId < 0 || equippedSkillIds == null)
            return false;

        for (int i = 0; i < equippedSkillIds.Count; i++)
        {
            if (equippedSkillIds[i] != skillId)
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    public bool TryEquipSkill(int skillId, out int slotIndex)
    {
        slotIndex = -1;

        if (skillId < 0 || equippedSkillIds == null || equippedSkillIds.Count == 0)
            return false;

        if (TryFindEquippedSlotIndex(skillId, out slotIndex))
            return true;

        for (int i = 0; i < equippedSkillIds.Count; i++)
        {
            if (equippedSkillIds[i] != -1)
                continue;

            equippedSkillIds[i] = skillId;
            slotIndex = i;
            return true;
        }

        return false;
    }

    public bool TryUnequipSkill(int skillId, out int slotIndex)
    {
        if (!TryFindEquippedSlotIndex(skillId, out slotIndex))
            return false;

        equippedSkillIds[slotIndex] = -1;
        return true;
    }

    public bool TryUnequipSlot(int slotIndex)
    {
        if (equippedSkillIds == null || slotIndex < 0 || slotIndex >= equippedSkillIds.Count)
            return false;

        if (equippedSkillIds[slotIndex] < 0)
            return false;

        equippedSkillIds[slotIndex] = -1;
        return true;
    }

    /// <summary>
    /// UI에서 특정 스킬의 현재 레벨을 참조할 때 사용하는 공개 메서드.
    /// </summary>
    public int GetSkillLevelForUI(int skillId)
    {
        if (runtimeData == null)
            return 0;

        return runtimeData.GetSkillLevel(skillId);
    }

    /// <summary>
    /// 스킬 사용 후 다음 사용 가능 시간을 기록한다.
    /// </summary>
    private void StartCooldown(int skillId, float cooldown)
    {
        nextAvailableTimeBySkillId[skillId] = Time.time + cooldown;
    }

    /// <summary>
    /// 특정 스킬의 다음 사용 가능 시각을 반환한다.
    /// 아직 기록이 없으면 즉시 사용 가능 상태로 간주한다.
    /// </summary>
    private float GetNextAvailableTime(int skillId)
    {
        if (nextAvailableTimeBySkillId.TryGetValue(skillId, out float nextTime))
            return nextTime;

        return 0f;
    }

    /// <summary>
    /// 특정 스킬의 남은 쿨타임을 반환한다.
    /// UI에서 쿨타임 표시할 때 사용할 수 있다.
    /// </summary>
    public float GetRemainingCooldown(int skillId)
    {
        float remain = GetNextAvailableTime(skillId) - Time.time;
        return Mathf.Max(0f, remain);
    }

    /// <summary>
    /// 공격력 계수 퍼센트를 실제 피해량으로 바꾸는 간단한 유틸 함수.
    ///
    /// 예:
    /// currentAttack = 185
    /// damagePercent = 200
    /// -> finalDamage = 370
    /// </summary>
    public float CalculateFinalDamage(SkillCastResult castResult)
    {
        return castResult.currentAttack * (castResult.damagePercent / 100f);
    }

    private bool TryGetSkillExecutable(int skillId, out ISkillExecutable executable)
    {
        if (skillExecutablesById.Count == 0)
            RegisterSkillExecutables();

        return skillExecutablesById.TryGetValue(skillId, out executable);
    }

    private void RegisterSkillExecutables()
    {
        skillExecutablesById.Clear();

        RegisterSceneSkillExecutables();
        RegisterRuntimeSkillExecutables();
    }

    private void RegisterSceneSkillExecutables()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISkillExecutable executable)
                RegisterSkillExecutable(executable, "scene");
        }
    }

    private void RegisterRuntimeSkillExecutables()
    {
        Type executableInterfaceType = typeof(ISkillExecutable);
        Type monoBehaviourType = typeof(MonoBehaviour);
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Type[] types = GetLoadableTypes(assemblies[assemblyIndex]);

            for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
            {
                Type type = types[typeIndex];

                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                if (!monoBehaviourType.IsAssignableFrom(type))
                    continue;

                if (!executableInterfaceType.IsAssignableFrom(type))
                    continue;

                if (HasRegisteredExecutableType(type))
                    continue;

                MonoBehaviour runtimeComponent = GetOrCreateRuntimeSkillExecutorRoot().gameObject.AddComponent(type) as MonoBehaviour;
                if (runtimeComponent is ISkillExecutable executable)
                    RegisterSkillExecutable(executable, "runtime");
            }
        }
    }

    private void RegisterSkillExecutable(ISkillExecutable executable, string source)
    {
        if (executable == null)
            return;

        if (skillExecutablesById.TryGetValue(executable.SkillId, out ISkillExecutable registeredExecutable))
        {
            if (ReferenceEquals(registeredExecutable, executable))
                return;

            Debug.LogWarning(
                $"[SkillManager] 중복된 스킬 실행체를 무시했습니다. skillId: {executable.SkillId}, " +
                $"existing: {registeredExecutable.GetType().Name}, ignored: {executable.GetType().Name}, source: {source}");
            return;
        }

        skillExecutablesById.Add(executable.SkillId, executable);
    }

    private bool HasRegisteredExecutableType(Type targetType)
    {
        foreach (ISkillExecutable executable in skillExecutablesById.Values)
        {
            if (executable != null && executable.GetType() == targetType)
                return true;
        }

        return false;
    }

    private Transform GetOrCreateRuntimeSkillExecutorRoot()
    {
        if (runtimeSkillExecutorRoot != null)
            return runtimeSkillExecutorRoot;

        Transform foundRoot = transform.Find("RuntimeSkillExecutables");
        if (foundRoot != null)
        {
            runtimeSkillExecutorRoot = foundRoot;
            return runtimeSkillExecutorRoot;
        }

        GameObject rootObject = new GameObject("RuntimeSkillExecutables");
        rootObject.transform.SetParent(transform, false);
        runtimeSkillExecutorRoot = rootObject.transform;

        return runtimeSkillExecutorRoot;
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types;
        }
    }

    private void InitializeEquippedSkills()
    {
        if (equippedSkillIds == null)
        {
            equippedSkillIds = new List<int> { 0, 1, 6, -1 };
            return;
        }

        if (equippedSkillIds.Count == 0)
            equippedSkillIds.AddRange(new[] { 0, 1, 6, -1 });
    }
}

/// <summary>
/// 스킬 1회 시전에 필요한 계산 결과 묶음.
///
/// 이 값은 "이번 시전" 에만 유효한 런타임 데이터다.
/// 나중에 실제 스킬 실행체(OnionSliceSkill, ProjectileSkill 등)에 넘겨줄 수 있다.
/// </summary>
[System.Serializable]
public struct SkillCastResult
{
    /// <summary>
    /// 이번에 시전한 스킬의 고유 ID
    /// </summary>
    public int skillId;

    /// <summary>
    /// 이번 시전 시점의 스킬 레벨
    /// </summary>
    public int skillLevel;

    /// <summary>
    /// 공격력 계수 퍼센트.
    ///
    /// 예:
    /// - 100 = 공격력의 100%
    /// - 200 = 공격력의 200%
    /// - 350 = 공격력의 350%
    /// </summary>
    public float damagePercent;

    /// <summary>
    /// 이번 스킬 레벨 기준 최종 쿨타임
    /// </summary>
    public float cooldown;

    /// <summary>
    /// 시전 시점의 플레이어 최종 공격력
    /// </summary>
    public float currentAttack;

    /// <summary>
    /// 시전 시점의 플레이어 최종 치명타 확률
    ///
    /// 예:
    /// 80 = 80%
    /// </summary>
    public float currentCritChance;

    /// <summary>
    /// 시전 시점의 플레이어 최종 치명타 공격력
    /// </summary>
    public float currentCritDamage;


    /// <summary>
    /// 플레이어의 현재 위치에서 스킬 이펙트가 생성될 위치를 지정하는 벡터.
    /// </summary>
    public Transform slashPivot;

    /// <summary>
    /// 시전 시점의 플레이어 바라보는 방향.
    /// 1은 오른쪽, -1은 왼쪽이다.
    /// </summary>
    public int facingDirection;
}
