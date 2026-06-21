using System.Collections.Generic;
using UnityEngine;

public class RecipeGachaService : MonoBehaviour
{
    [SerializeField] private RecipeGachaSettings settings;
    [SerializeField] private RecipeInventoryState recipeState;
    [SerializeField] private RuntimeData runtimeData;

    private static readonly RecipeRarity[] RarityOrder =
    {
        RecipeRarity.Common,
        RecipeRarity.Magic,
        RecipeRarity.Rare,
        RecipeRarity.Epic,
        RecipeRarity.Legendary
    };

    public RecipeGachaSettings Settings => settings;
    public RecipeInventoryState RecipeState => recipeState;
    public RuntimeData RuntimeData => runtimeData;
    public RecipeDatabase RecipeDatabase => recipeState != null ? recipeState.RecipeDatabase : null;

    public int GetTotalCost(int drawCount)
    {
        if (settings == null || drawCount <= 0)
        {
            return 0;
        }

        return settings.GetCost(drawCount);
    }

    public bool CanDraw(int drawCount)
    {
        if (drawCount != 1 && drawCount != 10)
        {
            Debug.LogWarning("[RecipeGachaService] Draw count must be 1 or 10.", this);
            return false;
        }

        if (settings == null)
        {
            Debug.LogWarning("[RecipeGachaService] Settings are not assigned.", this);
            return false;
        }

        if (recipeState == null)
        {
            Debug.LogWarning("[RecipeGachaService] RecipeInventoryState is not assigned.", this);
            return false;
        }

        if (runtimeData == null)
        {
            Debug.LogWarning("[RecipeGachaService] RuntimeData is not assigned.", this);
            return false;
        }

        RecipeDatabase database = RecipeDatabase;
        if (database == null)
        {
            Debug.LogWarning("[RecipeGachaService] RecipeDatabase is not assigned.", this);
            return false;
        }

        if (!settings.IsValidRate())
        {
            Debug.LogWarning($"[RecipeGachaService] Invalid rarity total rate: {settings.TotalRate}.", this);
            return false;
        }

        for (int i = 0; i < RarityOrder.Length; i++)
        {
            RecipeRarity rarity = RarityOrder[i];
            if (settings.GetRate(rarity) <= 0f)
            {
                continue;
            }

            if (database.GetGachaCandidates(rarity).Count == 0)
            {
                Debug.LogWarning($"[RecipeGachaService] Rarity '{rarity}' has a rate but no enabled recipes.", this);
                return false;
            }
        }

        return runtimeData.GetGem() >= GetTotalCost(drawCount);
    }

    public bool TryDraw(int drawCount, out List<RecipeGachaResult> results)
    {
        results = new List<RecipeGachaResult>();

        if (!CanDraw(drawCount))
        {
            return false;
        }

        int totalCost = GetTotalCost(drawCount);
        if (!runtimeData.SpendGem(totalCost))
        {
            Debug.LogWarning("[RecipeGachaService] Not enough gem to draw.", this);
            return false;
        }

        for (int i = 0; i < drawCount; i++)
        {
            if (!TryDrawSingle(i, out RecipeGachaResult result))
            {
                runtimeData.AddGem(totalCost);
                results.Clear();
                Debug.LogWarning("[RecipeGachaService] Failed during draw. Refunded draw cost.", this);
                return false;
            }

            results.Add(result);
        }

        return true;
    }

    private bool TryDrawSingle(int drawIndex, out RecipeGachaResult result)
    {
        result = null;

        RecipeRarity drawnRarity = RollRarity();
        List<RecipeDefinitionData> candidates = RecipeDatabase.GetGachaCandidates(drawnRarity);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[RecipeGachaService] No gacha candidates found for rarity '{drawnRarity}'.", this);
            return false;
        }

        RecipeDefinitionData selectedDefinition = candidates[Random.Range(0, candidates.Count)];
        if (!recipeState.TryAddOwnedRecipe(selectedDefinition, out bool isNewRecipe, out bool leveledUp))
        {
            Debug.LogWarning("[RecipeGachaService] Failed to grant drawn recipe.", this);
            return false;
        }

        result = new RecipeGachaResult
        {
            drawIndex = drawIndex,
            recipeId = selectedDefinition.recipeId,
            displayName = selectedDefinition.GetLocalizedName(),
            rarity = selectedDefinition.rarity,
            drawCost = GetTotalCost(1),
            currentLevel = recipeState.GetOwnedLevel(selectedDefinition.recipeId),
            currentShardCount = recipeState.GetOwnedShardCount(selectedDefinition.recipeId),
            isNewRecipe = isNewRecipe,
            leveledUp = leveledUp,
            definition = selectedDefinition
        };

        return true;
    }

    private RecipeRarity RollRarity()
    {
        float roll = Random.Range(0f, settings.TotalRate);
        float cumulative = 0f;

        for (int i = 0; i < RarityOrder.Length; i++)
        {
            RecipeRarity rarity = RarityOrder[i];
            cumulative += settings.GetRate(rarity);

            if (roll <= cumulative)
            {
                return rarity;
            }
        }

        return RecipeRarity.Legendary;
    }
}
