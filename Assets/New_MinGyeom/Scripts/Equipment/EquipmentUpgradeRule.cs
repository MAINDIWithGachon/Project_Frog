using System;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentUpgradeRule
    {
        public EquipmentGrade grade;
        [Min(1)] public int currentLevel = 1;
        [Min(0)] public int requiredDuplicateCount;
        [Min(0)] public int requiredGold;
        [Min(0)] public int requiredUpgradeStone;
    }
}
