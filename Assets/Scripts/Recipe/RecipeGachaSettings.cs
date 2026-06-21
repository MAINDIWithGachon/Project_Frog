using UnityEngine;

[CreateAssetMenu(
    fileName = "RecipeGachaSettings",
    menuName = "Project Frog/Gacha/Recipe Gacha Settings")]
public class RecipeGachaSettings : ScriptableObject
{
    [Header("Cost")]
    [Min(0)] public int drawOneCost = 100;
    [Min(0)] public int drawTenCost = 950;

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

    public int GetCost(int drawCount)
    {
        return drawCount >= 10 ? drawTenCost : drawOneCost;
    }

    public float GetRate(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Common:
                return commonRate;
            case RecipeRarity.Magic:
                return magicRate;
            case RecipeRarity.Rare:
                return rareRate;
            case RecipeRarity.Epic:
                return epicRate;
            case RecipeRarity.Legendary:
                return legendaryRate;
            default:
                return 0f;
        }
    }
}
