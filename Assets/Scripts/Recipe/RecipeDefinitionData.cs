using System;
using UnityEngine;

[Serializable]
public class RecipeDefinitionData
{
    [Header("Identity")]
    public string recipeId;
    [HideInInspector] public string displayName;
    public RecipeRarity rarity;
    public bool isGachaEnabled = true;

    [Header("Localization")]
    [SerializeField] private LocalizedTextData text;

    [Header("UI")]
    public Sprite uiIcon;
    [HideInInspector, TextArea] public string description;

    [Header("Passive")]
    public RecipePassiveType passiveType;
    public float passiveValue;

    [Header("Duplicate Progress")]
    [Min(1)] public int maxLevel = 20;
    [Min(1)] public int baseRequiredShardCount = 5;
    [Min(1)] public int shardCountOnDuplicate = 1;

    public LocalizedTextData Text => text;
    public LocalizationEntry NameEntry => text.Title;
    public LocalizationEntry DescriptionEntry => text.Description;

    public string GetLocalizedName()
    {
        return LocalizationLookup.GetLocalizedString(NameEntry);
    }

    public string GetLocalizedDescription()
    {
        return LocalizationLookup.GetLocalizedString(DescriptionEntry);
    }

    public int GetRequiredShardCountForLevel(int level)
    {
        return Mathf.Max(1, level) * Mathf.Max(1, baseRequiredShardCount);
    }

    public void ApplyRarityFromRecipeId()
    {
        if (TryGetRarityFromRecipeId(recipeId, out RecipeRarity inferredRarity))
        {
            rarity = inferredRarity;
        }
    }

    public static bool TryGetRarityFromRecipeId(string recipeId, out RecipeRarity rarity)
    {
        rarity = RecipeRarity.Common;

        if (string.IsNullOrWhiteSpace(recipeId) || !int.TryParse(recipeId, out int numericId))
        {
            return false;
        }

        int rarityCode = numericId / 100 % 10;
        switch (rarityCode)
        {
            case 2:
                rarity = RecipeRarity.Magic;
                return true;
            case 3:
                rarity = RecipeRarity.Rare;
                return true;
            case 4:
                rarity = RecipeRarity.Epic;
                return true;
            case 5:
                rarity = RecipeRarity.Legendary;
                return true;
            case 1:
                rarity = RecipeRarity.Common;
                return true;
            default:
                return false;
        }
    }
}
