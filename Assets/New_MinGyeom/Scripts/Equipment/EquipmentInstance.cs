using System;
using UnityEngine;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentInstance
    {
        public string equipmentId;
        [Min(1)] public int level = 1;
        [Min(0)] public int ownedCount = 1;

        public EquipmentInstance()
        {
        }

        public EquipmentInstance(string equipmentId, int level = 1, int ownedCount = 1)
        {
            this.equipmentId = equipmentId;
            this.level = Mathf.Max(1, level);
            this.ownedCount = Mathf.Max(0, ownedCount);
        }
    }
}
