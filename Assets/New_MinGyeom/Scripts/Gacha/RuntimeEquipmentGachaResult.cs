using System;
using NewMinGyeom.Equipment;

namespace NewMinGyeom.Gacha
{
    [Serializable]
    public class RuntimeEquipmentGachaResult
    {
        public int drawIndex;
        public string equipmentId;
        public string displayName;
        public EquipmentGrade grade;
        public int drawCost;
        public int currentLevel;
        public int currentOwnedCount;
        public EquipmentDefinition definition;
    }
}
