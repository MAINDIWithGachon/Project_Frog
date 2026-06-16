using System;

namespace NewMinGyeom.Equipment
{
    [Serializable]
    public class EquipmentStatBlock
    {
        public int attack;
        public int hp;
        public float hpRegen;
        public float critChance;
        public float critDamage;

        public static EquipmentStatBlock Zero => new();
    }
}
