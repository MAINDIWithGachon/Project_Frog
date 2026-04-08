using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentGachaSettings",
    menuName = "Project Frog/Gacha/Equipment Gacha Settings")]
public class EquipmentGachaSettings : ScriptableObject
{
    [Header("Cost")]
    [Min(0)] public int drawCost = 100;

    [Header("Rarity Rates")]
    [Range(0f, 100f)] public float commonRate = 40f;
    [Range(0f, 100f)] public float magicRate = 30f;
    [Range(0f, 100f)] public float rareRate = 15f;
    [Range(0f, 100f)] public float epicRate = 10f;
    [Range(0f, 100f)] public float legendaryRate = 5f;

    public float TotalRate => commonRate + magicRate + rareRate + epicRate + legendaryRate;

    public bool IsValidRate()
    {
        return Mathf.Approximately(TotalRate, 100f);
    }

    public float GetRate(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => commonRate,
            EquipmentRarity.Magic => magicRate,
            EquipmentRarity.Rare => rareRate,
            EquipmentRarity.Epic => epicRate,
            EquipmentRarity.Legendary => legendaryRate,
            _ => 0f
        };
    }
}
