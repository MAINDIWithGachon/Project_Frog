using System;
using System.Collections.Generic;
using UnityEngine;

public class RecipeInventoryState : MonoBehaviour
{
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private List<RecipeOwnedState> ownedRecipes = new();

    public event Action StateChanged;

    public RecipeDatabase RecipeDatabase => recipeDatabase;
    public IReadOnlyList<RecipeOwnedState> OwnedRecipes => ownedRecipes;

    private void Awake()
    {
        NormalizeOwnedRecipes();
    }

    public bool IsOwned(string recipeId)
    {
        return GetOwnedState(recipeId) != null;
    }

    public RecipeOwnedState GetOwnedState(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return null;
        }

        for (int i = 0; i < ownedRecipes.Count; i++)
        {
            RecipeOwnedState ownedState = ownedRecipes[i];
            if (ownedState == null || ownedState.recipeId != recipeId)
            {
                continue;
            }

            return ownedState;
        }

        return null;
    }

    public int GetOwnedLevel(string recipeId)
    {
        RecipeOwnedState ownedState = GetOwnedState(recipeId);
        return ownedState != null ? Mathf.Max(1, ownedState.currentLevel) : 0;
    }

    public int GetOwnedShardCount(string recipeId)
    {
        RecipeOwnedState ownedState = GetOwnedState(recipeId);
        return ownedState != null ? Mathf.Max(0, ownedState.ownedShardCount) : 0;
    }

    public bool TryAddOwnedRecipe(RecipeDefinitionData definition, out bool isNewRecipe, out bool leveledUp)
    {
        isNewRecipe = false;
        leveledUp = false;

        if (definition == null || string.IsNullOrWhiteSpace(definition.recipeId))
        {
            return false;
        }

        RecipeOwnedState ownedState = GetOwnedState(definition.recipeId);
        if (ownedState == null)
        {
            ownedState = new RecipeOwnedState
            {
                recipeId = definition.recipeId,
                currentLevel = 1,
                ownedShardCount = 1
            };

            ownedRecipes.Add(ownedState);
            isNewRecipe = true;
        }
        else
        {
            ownedState.ownedShardCount += Mathf.Max(1, definition.shardCountOnDuplicate);
        }

        NotifyStateChanged();
        return true;
    }

    public bool CanUpgradeRecipe(RecipeDefinitionData definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.recipeId))
        {
            return false;
        }

        RecipeOwnedState ownedState = GetOwnedState(definition.recipeId);
        if (ownedState == null)
        {
            return false;
        }

        int maxLevel = Mathf.Max(1, definition.maxLevel);
        int currentLevel = Mathf.Max(1, ownedState.currentLevel);
        if (currentLevel >= maxLevel)
        {
            return false;
        }

        int requiredShardCount = definition.GetRequiredShardCountForLevel(currentLevel);
        return ownedState.ownedShardCount >= requiredShardCount;
    }

    public bool TryUpgradeRecipe(RecipeDefinitionData definition)
    {
        if (!CanUpgradeRecipe(definition))
        {
            return false;
        }

        RecipeOwnedState ownedState = GetOwnedState(definition.recipeId);
        int requiredShardCount = definition.GetRequiredShardCountForLevel(ownedState.currentLevel);

        ownedState.ownedShardCount -= requiredShardCount;
        ownedState.currentLevel++;

        NotifyStateChanged();
        return true;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private void NormalizeOwnedRecipes()
    {
        for (int i = 0; i < ownedRecipes.Count; i++)
        {
            RecipeOwnedState ownedState = ownedRecipes[i];
            if (ownedState == null)
            {
                continue;
            }

            ownedState.currentLevel = Mathf.Max(1, ownedState.currentLevel);
            ownedState.ownedShardCount = Mathf.Max(0, ownedState.ownedShardCount);
        }
    }
}
