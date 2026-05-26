using System;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentDefinition
    {
        [Header("Identity")]
        public string equipmentId;
        public string displayName;
        public EquipmentGrade grade;
        public EquipmentSlotType slotType;

        [Header("UI")]
        public string iconKey;
        [TextArea] public string description;

        [Header("Progression")]
        [Min(1)] public int maxLevel = 10;
        public bool isGachaEnabled = true;

        [Header("Stats")]
        public EquipmentStatBlock stats = new();
    }
}
