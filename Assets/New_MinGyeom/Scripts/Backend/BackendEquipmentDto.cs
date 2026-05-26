using System;

namespace NewMinGyeom.Backend
{
    [Serializable]
    public class BackendEquipmentDto
    {
        public string equipmentId;
        public string displayName;
        public string slotType;
        public string grade;
        public string iconKey;
        public string description;
        public int maxLevel;
        public int attack;
        public int hp;
        public float hpRegen;
        public float critChance;
        public float critDamage;
        public bool isGachaEnabled = true;
    }
}
