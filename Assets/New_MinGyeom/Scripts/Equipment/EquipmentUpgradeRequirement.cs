using System;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentUpgradeRequirement
    {
        public string equipmentId;
        public int currentLevel;
        public int nextLevel;
        public int ownedCount;
        public int requiredDuplicateCount;
        public int requiredGold;
        public int requiredUpgradeStone;
        public bool isAtMaxLevel;
        public bool hasEnoughDuplicates;
        public bool hasEnoughGold;
        public bool hasEnoughUpgradeStone;

        public bool CanUpgrade => !isAtMaxLevel &&
                                  hasEnoughDuplicates &&
                                  hasEnoughGold &&
                                  hasEnoughUpgradeStone;
    }
}
