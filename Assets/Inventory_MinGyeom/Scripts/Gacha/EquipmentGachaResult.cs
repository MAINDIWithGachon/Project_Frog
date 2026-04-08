using System;

[Serializable]
public class EquipmentGachaResult
{
    public int drawIndex;
    public string equipmentId;
    public string displayName;
    public EquipmentRarity rarity;
    public int drawCost;
    public int currentLevel;
    public int currentOwnedCount;
    public EquipmentDefinitionData definition;
}
