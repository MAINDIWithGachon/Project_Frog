using System;

[Serializable]
public class EquipmentGachaResult
{
    public string equipmentId;
    public string displayName;
    public EquipmentRarity rarity;
    public int drawCost;
    public EquipmentDefinitionData definition;
}
