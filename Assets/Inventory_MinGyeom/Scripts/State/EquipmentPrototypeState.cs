using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장비 인벤토리의 보유 상태와 장착 상태를 관리하는 런타임 상태 클래스입니다.
/// </summary>
public class EquipmentPrototypeState : MonoBehaviour
{
    private static readonly EquipmentCategory[] FixedSlotCategories =
        (EquipmentCategory[])Enum.GetValues(typeof(EquipmentCategory));

    /// <summary>
    /// 카테고리별 현재 장착 중인 장비 ID를 저장합니다.
    /// </summary>
    [Serializable]
    private class EquippedSlotState
    {
        [SerializeField, HideInInspector] private EquipmentCategory category;
        [SerializeField] private string equippedItemId;

        public EquipmentCategory Category => category;

        public string EquippedItemId
        {
            get => equippedItemId;
            set => equippedItemId = value;
        }

        public void SetCategory(EquipmentCategory value)
        {
            category = value;
        }
    }

    // 장비 정의 데이터와 플레이어의 장비 보유/장착 상태를 함께 관리합니다.
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private EquipmentUpgradeRuleDatabase upgradeRuleDatabase;
    [SerializeField] private List<EquipmentOwnedState> ownedEquipment = new();
    [SerializeField] private List<EquippedSlotState> equippedSlots = new();

    // UI나 바인더에서 상태 변화를 구독할 수 있도록 이벤트를 제공합니다.
    public event Action StateChanged;

    public EquipmentDatabase EquipmentDatabase => equipmentDatabase;
    public EquipmentUpgradeRuleDatabase UpgradeRuleDatabase => upgradeRuleDatabase;
    public IReadOnlyList<EquipmentOwnedState> OwnedEquipment => ownedEquipment;

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeOwnedEquipment();
        NormalizeEquippedSlots();
        NotifyStateChanged();
    }
#endif

    private void Awake()
    {
        // 인스펙터에 잘못된 값이 들어가도 최소 레벨과 수량은 유지되도록 보정합니다.
        NormalizeOwnedEquipment();
        NormalizeEquippedSlots();
    }

    public List<EquipmentDefinitionData> GetOwnedDefinitionsByCategory(EquipmentCategory category)
    {
        List<EquipmentDefinitionData> results = new();

        if (equipmentDatabase == null)
        {
            Debug.LogWarning("[EquipmentPrototypeState] EquipmentDatabase is not assigned.", this);
            return results;
        }

        for (int i = 0; i < ownedEquipment.Count; i++)
        {
            EquipmentOwnedState ownedState = ownedEquipment[i];
            if (ownedState == null || string.IsNullOrWhiteSpace(ownedState.equipmentId))
            {
                continue;
            }

            if (!equipmentDatabase.TryGetById(ownedState.equipmentId, out EquipmentDefinitionData definition))
            {
                continue;
            }

            if (definition.category != category)
            {
                continue;
            }

            results.Add(definition);
        }

        return results;
    }

    public bool TryEquip(string equipmentId)
    {
        // 보유 중인 장비이면서 데이터베이스에 등록된 장비만 장착할 수 있습니다.
        if (!IsOwned(equipmentId))
        {
            Debug.LogWarning($"[EquipmentPrototypeState] Cannot equip '{equipmentId}' because it is not owned.", this);
            return false;
        }

        if (equipmentDatabase == null || !equipmentDatabase.TryGetById(equipmentId, out EquipmentDefinitionData definition))
        {
            Debug.LogWarning($"[EquipmentPrototypeState] Cannot equip '{equipmentId}' because it is missing from the database.", this);
            return false;
        }

        SetEquippedItemId(definition.category, equipmentId);
        return true;
    }

    public bool TryEquip(EquipmentDefinitionData definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (!HasOwnedItem(definition))
        {
            Debug.LogWarning($"[EquipmentPrototypeState] Cannot equip '{definition.displayName}' because it is not owned.", this);
            return false;
        }

        SetEquippedItemId(definition.category, definition.equipmentId);
        return true;
    }

    public bool ToggleEquip(EquipmentDefinitionData definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (IsEquippedInSlot(definition))
        {
            Unequip(definition.category);
            return true;
        }

        return TryEquip(definition);
    }

    public void Unequip(EquipmentCategory category)
    {
        SetEquippedItemId(category, string.Empty);
    }

    public bool IsOwned(string equipmentId)
    {
        return GetOwnedCount(equipmentId) > 0;
    }

    public bool IsEquipped(string equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return false;
        }

        for (int i = 0; i < equippedSlots.Count; i++)
        {
            EquippedSlotState slot = equippedSlots[i];
            if (slot == null || slot.EquippedItemId != equipmentId)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public bool IsCategoryEquipped(EquipmentCategory category)
    {
        return !string.IsNullOrWhiteSpace(GetEquippedItemId(category));
    }

    public bool IsEquippedInSlot(EquipmentDefinitionData definition)
    {
        if (definition == null)
        {
            return false;
        }

        string equippedItemId = GetEquippedItemId(definition.category);
        if (string.IsNullOrWhiteSpace(equippedItemId))
        {
            return false;
        }

        return equippedItemId == definition.equipmentId;
    }

    public string GetEquippedItemId(EquipmentCategory category)
    {
        EquippedSlotState slot = GetOrCreateSlot(category);
        return slot.EquippedItemId;
    }

    public EquipmentDefinitionData GetEquippedDefinition(EquipmentCategory category)
    {
        string equippedItemId = GetEquippedItemId(category);
        if (string.IsNullOrWhiteSpace(equippedItemId) || equipmentDatabase == null)
        {
            return null;
        }

        equipmentDatabase.TryGetById(equippedItemId, out EquipmentDefinitionData definition);
        return definition;
    }

    public bool HasOwnedItemInCategory(EquipmentCategory category)
    {
        if (equipmentDatabase == null)
        {
            return false;
        }

        for (int i = 0; i < ownedEquipment.Count; i++)
        {
            EquipmentOwnedState ownedState = ownedEquipment[i];
            if (ownedState == null || string.IsNullOrWhiteSpace(ownedState.equipmentId))
            {
                continue;
            }

            if (Mathf.Max(0, ownedState.ownedCount) <= 0)
            {
                continue;
            }

            if (!equipmentDatabase.TryGetById(ownedState.equipmentId, out EquipmentDefinitionData definition))
            {
                continue;
            }

            if (definition.category == category)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasOwnedItem(EquipmentDefinitionData definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (equipmentDatabase == null)
        {
            return false;
        }

        for (int i = 0; i < ownedEquipment.Count; i++)
        {
            EquipmentOwnedState ownedState = ownedEquipment[i];
            if (ownedState == null || string.IsNullOrWhiteSpace(ownedState.equipmentId))
            {
                continue;
            }

            if (Mathf.Max(0, ownedState.ownedCount) <= 0)
            {
                continue;
            }

            if (!equipmentDatabase.TryGetById(ownedState.equipmentId, out EquipmentDefinitionData ownedDefinition))
            {
                continue;
            }

            if (Mathf.Max(0, ownedState.ownedCount) <= 0)
            {
                continue;
            }

            if (ownedDefinition.category != definition.category)
            {
                continue;
            }

            if (ownedDefinition.equipmentId == definition.equipmentId)
            {
                return true;
            }
        }

        return false;
    }

    public EquipmentOwnedState GetOwnedState(string equipmentId)
    {
        // 플레이어가 보유한 특정 장비의 런타임 상태를 ID로 조회합니다.
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return null;
        }

        for (int i = 0; i < ownedEquipment.Count; i++)
        {
            EquipmentOwnedState ownedState = ownedEquipment[i];
            if (ownedState == null || ownedState.equipmentId != equipmentId)
            {
                continue;
            }

            return ownedState;
        }

        return null;
    }

    public int GetOwnedLevel(string equipmentId)
    {
        // 장비 상세 UI 등에서 현재 강화 레벨을 표시할 때 사용합니다.
        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        return ownedState != null ? Mathf.Max(1, ownedState.currentLevel) : 0;
    }

    public int GetOwnedCount(string equipmentId)
    {
        // 동일 장비 보유 개수를 조회해 강화 가능 여부를 판단할 때 사용합니다.
        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        return ownedState != null ? Mathf.Max(0, ownedState.ownedCount) : 0;
    }

    public bool TryAddOwnedEquipment(string equipmentId, int amount = 1, int initialLevel = 1)
    {
        // 신규 장비 획득 또는 중복 장비 지급 시 보유 목록을 갱신합니다.
        if (string.IsNullOrWhiteSpace(equipmentId) || amount <= 0)
        {
            return false;
        }

        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        if (ownedState == null)
        {
            ownedState = new EquipmentOwnedState
            {
                equipmentId = equipmentId,
                currentLevel = Mathf.Max(1, initialLevel),
                ownedCount = amount
            };

            ownedEquipment.Add(ownedState);
        }
        else
        {
            ownedState.currentLevel = Mathf.Max(1, ownedState.currentLevel);
            ownedState.ownedCount = Mathf.Max(0, ownedState.ownedCount) + amount;
        }

        NotifyStateChanged();
        return true;
    }

    public bool TryConsumeOwnedEquipmentCopies(string equipmentId, int amount)
    {
        // 강화나 합성처럼 동일 장비를 재료로 소모할 때 사용합니다.
        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        if (ownedState == null || amount <= 0)
        {
            return false;
        }

        int currentCount = Mathf.Max(0, ownedState.ownedCount);
        if (currentCount < amount)
        {
            return false;
        }

        ownedState.ownedCount = currentCount - amount;
        NotifyStateChanged();
        return true;
    }

    public EquipmentUpgradeRequirement GetUpgradeRequirement(
        string equipmentId,
        int availableGold = int.MaxValue,
        int availableUpgradeStone = int.MaxValue)
    {
        // 현재 레벨, 보유 복사본 수, 재화 수량을 종합해서 강화 조건을 계산합니다.
        EquipmentUpgradeRequirement requirement = new()
        {
            equipmentId = equipmentId
        };

        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        if (ownedState == null || equipmentDatabase == null)
        {
            requirement.isAtMaxLevel = true;
            return requirement;
        }

        if (!equipmentDatabase.TryGetById(equipmentId, out EquipmentDefinitionData definition) || definition == null)
        {
            requirement.isAtMaxLevel = true;
            return requirement;
        }

        int currentLevel = Mathf.Max(1, ownedState.currentLevel);
        int ownedCount = Mathf.Max(0, ownedState.ownedCount);
        bool isAtMaxLevel = currentLevel >= Mathf.Max(1, definition.maxLevel);
        int requiredDuplicateCount = isAtMaxLevel ? 0 : GetRequiredDuplicateCount(definition, currentLevel);
        int requiredGold = isAtMaxLevel ? 0 : GetRequiredGold(definition, currentLevel);
        int requiredUpgradeStone = isAtMaxLevel ? 0 : GetRequiredUpgradeStone(definition, currentLevel);

        requirement.currentLevel = currentLevel;
        requirement.nextLevel = isAtMaxLevel ? currentLevel : currentLevel + 1;
        requirement.ownedCount = ownedCount;
        requirement.requiredDuplicateCount = requiredDuplicateCount;
        requirement.requiredGold = requiredGold;
        requirement.requiredUpgradeStone = requiredUpgradeStone;
        requirement.isAtMaxLevel = isAtMaxLevel;
        requirement.hasEnoughDuplicates = ownedCount >= requiredDuplicateCount;
        requirement.hasEnoughGold = availableGold >= requiredGold;
        requirement.hasEnoughUpgradeStone = availableUpgradeStone >= requiredUpgradeStone;
        return requirement;
    }

    public bool CanUpgrade(string equipmentId, int availableGold, int availableUpgradeStone)
    {
        // 외부에서는 최종 강화 가능 여부만 간단히 확인할 수 있습니다.
        return GetUpgradeRequirement(equipmentId, availableGold, availableUpgradeStone).CanUpgrade;
    }

    public bool TryUpgrade(string equipmentId, RuntimeData runtimeData, out EquipmentUpgradeRequirement requirement)
    {
        // 강화 처리 순서:
        // 1. 요구 조건 계산
        // 2. 골드/강화석 차감
        // 3. 동일 장비 개수 차감
        // 4. 장비 레벨 증가
        int availableGold = runtimeData != null ? runtimeData.GetGold() : 0;
        int availableUpgradeStone = runtimeData != null ? runtimeData.GetUpgradeStone() : 0;
        requirement = GetUpgradeRequirement(equipmentId, availableGold, availableUpgradeStone);

        if (!requirement.CanUpgrade || runtimeData == null)
        {
            return false;
        }

        EquipmentOwnedState ownedState = GetOwnedState(equipmentId);
        if (ownedState == null)
        {
            return false;
        }

        bool spentGold = runtimeData.SpendGold(requirement.requiredGold);
        if (!spentGold)
        {
            return false;
        }

        if (!runtimeData.SpendUpgradeStone(requirement.requiredUpgradeStone))
        {
            runtimeData.AddGold(requirement.requiredGold);
            return false;
        }

        ownedState.ownedCount = Mathf.Max(1, ownedState.ownedCount - requirement.requiredDuplicateCount);
        ownedState.currentLevel = requirement.nextLevel;
        NotifyStateChanged();
        requirement = GetUpgradeRequirement(equipmentId, runtimeData.GetGold(), runtimeData.GetUpgradeStone());
        return true;
    }

    private void SetEquippedItemId(EquipmentCategory category, string equipmentId)
    {
        EquippedSlotState slot = GetOrCreateSlot(category);
        slot.EquippedItemId = equipmentId;
        NotifyStateChanged();
    }

    private EquippedSlotState GetOrCreateSlot(EquipmentCategory category)
    {
        // 아직 슬롯 데이터가 없으면 해당 카테고리용 슬롯을 새로 만듭니다.
        for (int i = 0; i < equippedSlots.Count; i++)
        {
            EquippedSlotState slot = equippedSlots[i];
            if (slot == null || slot.Category != category)
            {
                continue;
            }

            return slot;
        }

        EquippedSlotState createdSlot = new EquippedSlotState
        {
            EquippedItemId = string.Empty
        };
        createdSlot.SetCategory(category);

        equippedSlots.Add(createdSlot);
        return createdSlot;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private void NormalizeOwnedEquipment()
    {
        // 인스펙터에 잘못된 값이 들어와도 최소 레벨과 수량을 보정합니다.
        for (int i = 0; i < ownedEquipment.Count; i++)
        {
            EquipmentOwnedState ownedState = ownedEquipment[i];
            if (ownedState == null)
            {
                continue;
            }

            ownedState.currentLevel = Mathf.Max(1, ownedState.currentLevel);
            ownedState.ownedCount = Mathf.Max(1, ownedState.ownedCount);
        }
    }

    private void NormalizeEquippedSlots()
    {
        if (equippedSlots == null)
        {
            equippedSlots = new List<EquippedSlotState>();
        }

        Dictionary<EquipmentCategory, string> equippedItemIdsByCategory = new();

        for (int i = 0; i < equippedSlots.Count; i++)
        {
            EquippedSlotState slot = equippedSlots[i];
            if (slot == null)
            {
                continue;
            }

            equippedItemIdsByCategory[slot.Category] = slot.EquippedItemId;
        }

        List<EquippedSlotState> normalizedSlots = new(FixedSlotCategories.Length);
        for (int i = 0; i < FixedSlotCategories.Length; i++)
        {
            EquipmentCategory category = FixedSlotCategories[i];
            EquippedSlotState slot = new EquippedSlotState
            {
                EquippedItemId = equippedItemIdsByCategory.TryGetValue(category, out string equippedItemId)
                    ? equippedItemId
                    : string.Empty
            };

            slot.SetCategory(category);
            normalizedSlots.Add(slot);
        }

        equippedSlots = normalizedSlots;
    }

    private int GetRequiredDuplicateCount(EquipmentDefinitionData definition, int currentLevel)
    {
        if (definition == null)
        {
            return 10;
        }

        if (upgradeRuleDatabase != null &&
            upgradeRuleDatabase.TryGetRule(definition.rarity, currentLevel, out EquipmentUpgradeRuleDatabase.RuleEntry rule))
        {
            return Mathf.Max(0, rule.requiredDuplicateCount);
        }

        return Mathf.Max(1, definition.requiredItemCountForNextLevel);
    }

    private int GetRequiredGold(EquipmentDefinitionData definition, int currentLevel)
    {
        if (definition == null)
        {
            return 100 * Mathf.Max(1, currentLevel);
        }

        if (upgradeRuleDatabase != null &&
            upgradeRuleDatabase.TryGetRule(definition.rarity, currentLevel, out EquipmentUpgradeRuleDatabase.RuleEntry rule))
        {
            return Mathf.Max(0, rule.requiredGold);
        }

        int rarityMultiplier = definition.rarity switch
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

    private int GetRequiredUpgradeStone(EquipmentDefinitionData definition, int currentLevel)
    {
        if (definition == null)
        {
            return 5 * Mathf.Max(1, currentLevel);
        }

        if (upgradeRuleDatabase != null &&
            upgradeRuleDatabase.TryGetRule(definition.rarity, currentLevel, out EquipmentUpgradeRuleDatabase.RuleEntry rule))
        {
            return Mathf.Max(0, rule.requiredUpgradeStone);
        }

        int rarityMultiplier = definition.rarity switch
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
