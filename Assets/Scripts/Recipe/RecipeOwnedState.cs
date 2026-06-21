using System;

[Serializable]
public class RecipeOwnedState
{
    public string recipeId;
    public int currentLevel = 1;
    public int ownedShardCount;
}
