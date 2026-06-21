using System;

[Serializable]
public class RecipeGachaResult
{
    public int drawIndex;
    public string recipeId;
    public string displayName;
    public RecipeRarity rarity;
    public int drawCost;
    public int currentLevel;
    public int currentShardCount;
    public bool isNewRecipe;
    public bool leveledUp;
    public RecipeDefinitionData definition;
}
