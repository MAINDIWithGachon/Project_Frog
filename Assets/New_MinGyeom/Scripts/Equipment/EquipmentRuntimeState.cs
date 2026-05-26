using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    public class EquipmentRuntimeState : MonoBehaviour
    {
        private static readonly EquipmentSlotType[] FixedSlotTypes =
            (EquipmentSlotType[])Enum.GetValues(typeof(EquipmentSlotType));

        [SerializeField] private EquipmentRuntimeDatabase database = new();
        [SerializeField] private List<EquipmentInstance> ownedEquipments = new();
        [SerializeField] private List<EquippedSlotState> equippedSlots = new();
        [SerializeField] private List<EquipmentUpgradeRule> upgradeRules = new();

        public event Action StateChanged;

        public EquipmentRuntimeDatabase Database => database;
        public EquipmentDatabaseLoadState LoadState => database != null ? database.LoadState : EquipmentDatabaseLoadState.Empty;
        public IReadOnlyList<EquipmentInstance> OwnedEquipments => ownedEquipments;
        public IReadOnlyList<EquippedSlotState> EquippedSlots => equippedSlots;
        public IReadOnlyList<EquipmentUpgradeRule> UpgradeRules => upgradeRules;

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeOwnedEquipments();
            NormalizeEquippedSlots();
            NormalizeUpgradeRules();
        }
#endif

        private void Awake()
        {
            NormalizeOwnedEquipments();
            NormalizeEquippedSlots();
            NormalizeUpgradeRules();
        }

        public void SetDatabase(EquipmentRuntimeDatabase nextDatabase)
        {
            database = nextDatabase ?? new EquipmentRuntimeDatabase();
            NotifyStateChanged();
        }

        public void SetUpgradeRules(IEnumerable<EquipmentUpgradeRule> rules)
        {
            upgradeRules.Clear();

            if (rules != null)
            {
                foreach (EquipmentUpgradeRule rule in rules)
                {
                    if (rule == null)
                    {
                        continue;
                    }

                    upgradeRules.Add(new EquipmentUpgradeRule
                    {
                        grade = rule.grade,
                        currentLevel = Mathf.Max(1, rule.currentLevel),
                        requiredDuplicateCount = Mathf.Max(0, rule.requiredDuplicateCount),
                        requiredGold = Mathf.Max(0, rule.requiredGold),
                        requiredUpgradeStone = Mathf.Max(0, rule.requiredUpgradeStone)
                    });
                }
            }

            SortUpgradeRules();
            NotifyStateChanged();
        }

        public void SetOwnedEquipments(IEnumerable<EquipmentInstance> items)
        {
            ownedEquipments.Clear();

            if (items != null)
            {
                foreach (EquipmentInstance item in items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.equipmentId))
                    {
                        continue;
                    }

                    item.level = Mathf.Max(1, item.level);
                    item.ownedCount = Mathf.Max(0, item.ownedCount);
                    ownedEquipments.Add(item);
                }
            }

            NotifyStateChanged();
        }

        public bool TryAddOwnedEquipment(string equipmentId, int amount = 1, int initialLevel = 1)
        {
            if (string.IsNullOrWhiteSpace(equipmentId) || amount <= 0)
            {
                return false;
            }

            EquipmentInstance ownedState = GetOwnedState(equipmentId);
            if (ownedState == null)
            {
                ownedState = new EquipmentInstance(equipmentId, initialLevel, amount);
                ownedEquipments.Add(ownedState);
            }
            else
            {
                ownedState.level = Mathf.Max(1, ownedState.level);
                ownedState.ownedCount = Mathf.Max(0, ownedState.ownedCount) + amount;
            }

            NotifyStateChanged();
            return true;
        }

        public bool TryConsumeOwnedEquipmentCopies(string equipmentId, int amount)
        {
            EquipmentInstance ownedState = GetOwnedState(equipmentId);
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

        public EquipmentInstance GetOwnedState(string equipmentId)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
            {
                return null;
            }

            for (int i = 0; i < ownedEquipments.Count; i++)
            {
                EquipmentInstance ownedState = ownedEquipments[i];
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
            EquipmentInstance ownedState = GetOwnedState(equipmentId);
            return ownedState != null ? Mathf.Max(1, ownedState.level) : 0;
        }

        public int GetOwnedCount(string equipmentId)
        {
            EquipmentInstance ownedState = GetOwnedState(equipmentId);
            return ownedState != null ? Mathf.Max(0, ownedState.ownedCount) : 0;
        }

        public List<EquipmentDefinition> GetOwnedBySlotType(EquipmentSlotType slotType)
        {
            List<EquipmentDefinition> results = new();
            if (database == null || !database.IsReady)
            {
                return results;
            }

            for (int i = 0; i < ownedEquipments.Count; i++)
            {
                EquipmentInstance ownedState = ownedEquipments[i];
                if (ownedState == null || Mathf.Max(0, ownedState.ownedCount) <= 0)
                {
                    continue;
                }

                if (!database.TryGetDefinition(ownedState.equipmentId, out EquipmentDefinition definition))
                {
                    continue;
                }

                if (definition.slotType == slotType)
                {
                    results.Add(definition);
                }
            }

            return results;
        }

        public bool TryEquip(string equipmentId)
        {
            if (!IsOwned(equipmentId))
            {
                Debug.LogWarning($"[EquipmentRuntimeState] Cannot equip '{equipmentId}' because it is not owned.", this);
                return false;
            }

            if (database == null || !database.TryGetDefinition(equipmentId, out EquipmentDefinition definition))
            {
                Debug.LogWarning($"[EquipmentRuntimeState] Cannot equip '{equipmentId}' because it is missing from the database.", this);
                return false;
            }

            SetEquippedItemId(definition.slotType, equipmentId);
            return true;
        }

        public bool TryEquip(EquipmentDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (!HasOwnedItem(definition))
            {
                Debug.LogWarning($"[EquipmentRuntimeState] Cannot equip '{definition.displayName}' because it is not owned.", this);
                return false;
            }

            SetEquippedItemId(definition.slotType, definition.equipmentId);
            return true;
        }

        public bool ToggleEquip(EquipmentDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (IsEquippedInSlot(definition))
            {
                Unequip(definition.slotType);
                return true;
            }

            return TryEquip(definition);
        }

        public void Unequip(EquipmentSlotType slotType)
        {
            SetEquippedItemId(slotType, string.Empty);
        }

        public string GetEquippedItemId(EquipmentSlotType slotType)
        {
            EquippedSlotState slot = GetOrCreateSlot(slotType);
            return slot.equipmentId;
        }

        public EquipmentDefinition GetEquippedDefinition(EquipmentSlotType slotType)
        {
            string equipmentId = GetEquippedItemId(slotType);
            if (string.IsNullOrWhiteSpace(equipmentId) || database == null)
            {
                return null;
            }

            return database.GetDefinition(equipmentId);
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
                if (slot != null && slot.equipmentId == equipmentId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsEquippedInSlot(EquipmentDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return GetEquippedItemId(definition.slotType) == definition.equipmentId;
        }

        public bool HasOwnedItem(EquipmentDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return GetOwnedCount(definition.equipmentId) > 0;
        }

        public bool HasOwnedItemInSlotType(EquipmentSlotType slotType)
        {
            return GetOwnedBySlotType(slotType).Count > 0;
        }

        public bool IsOwned(string equipmentId)
        {
            return GetOwnedCount(equipmentId) > 0;
        }

        public EquipmentUpgradeRequirement GetUpgradeRequirement(
            string equipmentId,
            int availableGold = int.MaxValue,
            int availableUpgradeStone = int.MaxValue)
        {
            EquipmentUpgradeRequirement requirement = new()
            {
                equipmentId = equipmentId
            };

            EquipmentInstance ownedState = GetOwnedState(equipmentId);
            if (ownedState == null || database == null)
            {
                requirement.isAtMaxLevel = true;
                return requirement;
            }

            if (!database.TryGetDefinition(equipmentId, out EquipmentDefinition definition) || definition == null)
            {
                requirement.isAtMaxLevel = true;
                return requirement;
            }

            int currentLevel = Mathf.Max(1, ownedState.level);
            int ownedCount = Mathf.Max(0, ownedState.ownedCount);
            bool isAtMaxLevel = currentLevel >= Mathf.Max(1, definition.maxLevel);
            int requiredDuplicateCount = 0;
            int requiredGold = 0;
            int requiredUpgradeStone = 0;

            if (!isAtMaxLevel)
            {
                if (TryGetUpgradeRule(definition.grade, currentLevel, out EquipmentUpgradeRule rule))
                {
                    requiredDuplicateCount = rule.requiredDuplicateCount;
                    requiredGold = rule.requiredGold;
                    requiredUpgradeStone = rule.requiredUpgradeStone;
                }
                else
                {
                    requiredDuplicateCount = GetRequiredDuplicateCount(currentLevel);
                    requiredGold = GetRequiredGold(definition.grade, currentLevel);
                    requiredUpgradeStone = GetRequiredUpgradeStone(definition.grade, currentLevel);
                }
            }

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

        public bool TryUpgrade(string equipmentId, RuntimeData runtimeData, out EquipmentUpgradeRequirement requirement)
        {
            int availableGold = runtimeData != null ? runtimeData.GetGold() : 0;
            int availableUpgradeStone = runtimeData != null ? runtimeData.GetUpgradeStone() : 0;
            requirement = GetUpgradeRequirement(equipmentId, availableGold, availableUpgradeStone);

            if (!requirement.CanUpgrade || runtimeData == null)
            {
                return false;
            }

            EquipmentInstance ownedState = GetOwnedState(equipmentId);
            if (ownedState == null)
            {
                return false;
            }

            if (!runtimeData.SpendGold(requirement.requiredGold))
            {
                return false;
            }

            if (!runtimeData.SpendUpgradeStone(requirement.requiredUpgradeStone))
            {
                runtimeData.AddGold(requirement.requiredGold);
                return false;
            }

            ownedState.ownedCount = Mathf.Max(1, ownedState.ownedCount - requirement.requiredDuplicateCount);
            ownedState.level = requirement.nextLevel;
            NotifyStateChanged();

            requirement = GetUpgradeRequirement(equipmentId, runtimeData.GetGold(), runtimeData.GetUpgradeStone());
            return true;
        }

        public void SetEquippedSlot(EquipmentSlotType slotType, string equipmentId)
        {
            SetEquippedItemId(slotType, equipmentId);
        }

        public void ClearEquippedSlot(EquipmentSlotType slotType)
        {
            Unequip(slotType);
        }

        private void SetEquippedItemId(EquipmentSlotType slotType, string equipmentId)
        {
            EquippedSlotState slot = GetOrCreateSlot(slotType);
            slot.equipmentId = equipmentId;
            NotifyStateChanged();
        }

        private EquippedSlotState GetOrCreateSlot(EquipmentSlotType slotType)
        {
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedSlotState slot = equippedSlots[i];
                if (slot != null && slot.slotType == slotType)
                {
                    return slot;
                }
            }

            EquippedSlotState createdSlot = new() { slotType = slotType };
            equippedSlots.Add(createdSlot);
            return createdSlot;
        }

        private void NormalizeOwnedEquipments()
        {
            if (ownedEquipments == null)
            {
                ownedEquipments = new List<EquipmentInstance>();
            }

            for (int i = 0; i < ownedEquipments.Count; i++)
            {
                EquipmentInstance ownedState = ownedEquipments[i];
                if (ownedState == null)
                {
                    continue;
                }

                ownedState.level = Mathf.Max(1, ownedState.level);
                ownedState.ownedCount = Mathf.Max(0, ownedState.ownedCount);
            }
        }

        private void NormalizeEquippedSlots()
        {
            if (equippedSlots == null)
            {
                equippedSlots = new List<EquippedSlotState>();
            }

            Dictionary<EquipmentSlotType, string> equippedItemIdsBySlotType = new();
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedSlotState slot = equippedSlots[i];
                if (slot == null)
                {
                    continue;
                }

                equippedItemIdsBySlotType[slot.slotType] = slot.equipmentId;
            }

            List<EquippedSlotState> normalizedSlots = new(FixedSlotTypes.Length);
            for (int i = 0; i < FixedSlotTypes.Length; i++)
            {
                EquipmentSlotType slotType = FixedSlotTypes[i];
                normalizedSlots.Add(new EquippedSlotState
                {
                    slotType = slotType,
                    equipmentId = equippedItemIdsBySlotType.TryGetValue(slotType, out string equipmentId)
                        ? equipmentId
                        : string.Empty
                });
            }

            equippedSlots = normalizedSlots;
        }

        private void NormalizeUpgradeRules()
        {
            if (upgradeRules == null)
            {
                upgradeRules = new List<EquipmentUpgradeRule>();
            }

            for (int i = 0; i < upgradeRules.Count; i++)
            {
                EquipmentUpgradeRule rule = upgradeRules[i];
                if (rule == null)
                {
                    continue;
                }

                rule.currentLevel = Mathf.Max(1, rule.currentLevel);
                rule.requiredDuplicateCount = Mathf.Max(0, rule.requiredDuplicateCount);
                rule.requiredGold = Mathf.Max(0, rule.requiredGold);
                rule.requiredUpgradeStone = Mathf.Max(0, rule.requiredUpgradeStone);
            }

            SortUpgradeRules();
        }

        private bool TryGetUpgradeRule(EquipmentGrade grade, int currentLevel, out EquipmentUpgradeRule rule)
        {
            int normalizedLevel = Mathf.Max(1, currentLevel);
            for (int i = 0; i < upgradeRules.Count; i++)
            {
                EquipmentUpgradeRule candidate = upgradeRules[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.grade == grade && candidate.currentLevel == normalizedLevel)
                {
                    rule = candidate;
                    return true;
                }
            }

            rule = null;
            return false;
        }

        private void SortUpgradeRules()
        {
            upgradeRules.Sort((left, right) =>
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

                int gradeCompare = left.grade.CompareTo(right.grade);
                return gradeCompare != 0 ? gradeCompare : left.currentLevel.CompareTo(right.currentLevel);
            });
        }

        private static int GetRequiredDuplicateCount(int currentLevel)
        {
            return Mathf.Max(1, currentLevel + 1);
        }

        private static int GetRequiredGold(EquipmentGrade grade, int currentLevel)
        {
            return GetGoldMultiplier(grade) * Mathf.Max(1, currentLevel) * 100;
        }

        private static int GetRequiredUpgradeStone(EquipmentGrade grade, int currentLevel)
        {
            return GetUpgradeStoneMultiplier(grade) * Mathf.Max(1, currentLevel) * 5;
        }

        private static int GetGoldMultiplier(EquipmentGrade grade)
        {
            return grade switch
            {
                EquipmentGrade.Common => 1,
                EquipmentGrade.Magic => 2,
                EquipmentGrade.Rare => 4,
                EquipmentGrade.Epic => 7,
                EquipmentGrade.Legendary => 11,
                _ => 1
            };
        }

        private static int GetUpgradeStoneMultiplier(EquipmentGrade grade)
        {
            return grade switch
            {
                EquipmentGrade.Common => 1,
                EquipmentGrade.Magic => 2,
                EquipmentGrade.Rare => 3,
                EquipmentGrade.Epic => 5,
                EquipmentGrade.Legendary => 8,
                _ => 1
            };
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }

    [Serializable]
    public class EquippedSlotState
    {
        public EquipmentSlotType slotType;
        public string equipmentId;
    }
}
