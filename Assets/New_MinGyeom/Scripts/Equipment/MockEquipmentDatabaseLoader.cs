using System.Collections.Generic;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    public class MockEquipmentDatabaseLoader : MonoBehaviour
    {
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private bool autoLoadMockOnStart;
        [SerializeField] private bool equipFirstWeaponOnLoad = true;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (autoLoadMockOnStart)
            {
                LoadMock();
            }
        }

        [ContextMenu("Load Mock Equipment Database")]
        public void LoadMock()
        {
            ResolveReferences();
            if (equipmentState == null)
            {
                Debug.LogWarning("[MockEquipmentDatabaseLoader] EquipmentRuntimeState is missing.", this);
                return;
            }

            EquipmentRuntimeDatabase runtimeDatabase = new();
            runtimeDatabase.SetDefinitions(CreateMockDefinitions());

            equipmentState.SetDatabase(runtimeDatabase);
            equipmentState.SetOwnedEquipments(new[]
            {
                new EquipmentInstance("sword_001", 1, 4),
                new EquipmentInstance("hat_001", 1, 2),
                new EquipmentInstance("armor_001", 2, 5),
                new EquipmentInstance("ring_001", 1, 1),
                new EquipmentInstance("boots_001", 1, 3),
                new EquipmentInstance("necklace_001", 1, 1)
            });

            if (equipFirstWeaponOnLoad)
            {
                equipmentState.TryEquip("sword_001");
            }
        }

        private void ResolveReferences()
        {
            if (equipmentState == null)
            {
                equipmentState = GetComponent<EquipmentRuntimeState>();
            }

            if (equipmentState == null)
            {
                equipmentState = GetComponentInParent<EquipmentRuntimeState>();
            }
        }

        private static List<EquipmentDefinition> CreateMockDefinitions()
        {
            return new List<EquipmentDefinition>
            {
                CreateDefinition(
                    "sword_001",
                    "Training Sword",
                    EquipmentSlotType.Weapon,
                    EquipmentGrade.Common,
                    "sword_001",
                    "A simple sword for validating equipment flow.",
                    10,
                    attack: 12),
                CreateDefinition(
                    "hat_001",
                    "Apprentice Hat",
                    EquipmentSlotType.Hat,
                    EquipmentGrade.Magic,
                    "magic_hat",
                    "A light hat that sharpens focus.",
                    10,
                    hp: 30,
                    critChance: 0.02f),
                CreateDefinition(
                    "armor_001",
                    "Guard Armor",
                    EquipmentSlotType.Armor,
                    EquipmentGrade.Rare,
                    "armor_001",
                    "Reliable armor for front-line tests.",
                    10,
                    hp: 120),
                CreateDefinition(
                    "ring_001",
                    "Silver Ring",
                    EquipmentSlotType.Ring,
                    EquipmentGrade.Epic,
                    "ring_001",
                    "A ring with a small but noticeable edge.",
                    10,
                    attack: 4,
                    critDamage: 0.1f),
                CreateDefinition(
                    "boots_001",
                    "Traveler Boots",
                    EquipmentSlotType.Shoes,
                    EquipmentGrade.Common,
                    "boots_001",
                    "Boots used to confirm slot switching.",
                    10,
                    hpRegen: 0.8f),
                CreateDefinition(
                    "necklace_001",
                    "Blue Necklace",
                    EquipmentSlotType.Necklace,
                    EquipmentGrade.Legendary,
                    "necklace_001",
                    "A high-grade item for upgrade cost checks.",
                    10,
                    attack: 8,
                    hp: 60,
                    critChance: 0.04f,
                    critDamage: 0.2f)
            };
        }

        private static EquipmentDefinition CreateDefinition(
            string equipmentId,
            string displayName,
            EquipmentSlotType slotType,
            EquipmentGrade grade,
            string iconKey,
            string description,
            int maxLevel,
            int attack = 0,
            int hp = 0,
            float hpRegen = 0f,
            float critChance = 0f,
            float critDamage = 0f)
        {
            return new EquipmentDefinition
            {
                equipmentId = equipmentId,
                displayName = displayName,
                slotType = slotType,
                grade = grade,
                iconKey = iconKey,
                description = description,
                maxLevel = Mathf.Max(1, maxLevel),
                isGachaEnabled = true,
                stats = new EquipmentStatBlock
                {
                    attack = attack,
                    hp = hp,
                    hpRegen = hpRegen,
                    critChance = critChance,
                    critDamage = critDamage
                }
            };
        }
    }
}
