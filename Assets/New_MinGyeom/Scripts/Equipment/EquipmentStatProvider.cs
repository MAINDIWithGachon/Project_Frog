using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public struct EquipmentStatSnapshot
    {
        public int equippedCount;
        public float attack;
        public float hp;
        public float hpRegenPerSecond;
        public float critChance;
        public float critDamage;

        public static EquipmentStatSnapshot Zero => default;
    }

    public class EquipmentStatProvider : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool multiplyStatsByLevel = true;

        public event Action OnEquipmentStatsChanged;

        public EquipmentRuntimeState EquipmentState => equipmentState;
        public EquipmentStatSnapshot CurrentStats { get; private set; }

        private EquipmentRuntimeState subscribedState;

        private void Awake()
        {
            ResolveReferences();
            RecalculateStats();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToState();
            RecalculateStats();
        }

        private void OnDisable()
        {
            UnsubscribeFromState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            RecalculateStats();
        }
#endif

        public void Configure(EquipmentRuntimeState state)
        {
            if (equipmentState == state)
            {
                RecalculateStats();
                return;
            }

            UnsubscribeFromState();
            equipmentState = state;
            SubscribeToState();
            RecalculateAndNotify();
        }

        public void Refresh()
        {
            RecalculateAndNotify();
        }

        private void ResolveReferences()
        {
            if (equipmentState != null)
            {
                return;
            }

            equipmentState = GetComponent<EquipmentRuntimeState>();
            equipmentState ??= GetComponentInChildren<EquipmentRuntimeState>(true);
            equipmentState ??= GetComponentInParent<EquipmentRuntimeState>(true);
            equipmentState ??= FindAnyObjectByType<EquipmentRuntimeState>();
        }

        private void SubscribeToState()
        {
            if (subscribedState == equipmentState)
            {
                return;
            }

            UnsubscribeFromState();

            if (equipmentState == null)
            {
                return;
            }

            equipmentState.StateChanged += HandleEquipmentStateChanged;
            subscribedState = equipmentState;
        }

        private void UnsubscribeFromState()
        {
            if (subscribedState == null)
            {
                return;
            }

            subscribedState.StateChanged -= HandleEquipmentStateChanged;
            subscribedState = null;
        }

        private void HandleEquipmentStateChanged()
        {
            RecalculateAndNotify();
        }

        private void RecalculateAndNotify()
        {
            RecalculateStats();
            OnEquipmentStatsChanged?.Invoke();
        }

        private void RecalculateStats()
        {
            EquipmentStatSnapshot total = EquipmentStatSnapshot.Zero;

            if (equipmentState == null || equipmentState.Database == null)
            {
                CurrentStats = total;
                return;
            }

            IReadOnlyList<EquippedSlotState> equippedSlots = equipmentState.EquippedSlots;
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedSlotState slot = equippedSlots[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.equipmentId))
                {
                    continue;
                }

                EquipmentDefinition definition = equipmentState.Database.GetDefinition(slot.equipmentId);
                if (definition == null || definition.stats == null)
                {
                    continue;
                }

                int level = multiplyStatsByLevel
                    ? Mathf.Max(1, equipmentState.GetOwnedLevel(definition.equipmentId))
                    : 1;

                total.equippedCount++;
                total.attack += definition.stats.attack * level;
                total.hp += definition.stats.hp * level;
                total.hpRegenPerSecond += definition.stats.hpRegen * level;
                total.critChance += NormalizePercent(definition.stats.critChance) * level;
                total.critDamage += NormalizePercent(definition.stats.critDamage) * level;
            }

            CurrentStats = total;
        }

        private static float NormalizePercent(float value)
        {
            return Mathf.Abs(value) <= 1f ? value * 100f : value;
        }
    }
}
