using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RecipeDatabase",
    menuName = "Project Frog/Recipe/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeDefinitionData> recipeDefinitions = new();

    public IReadOnlyList<RecipeDefinitionData> RecipeDefinitions => recipeDefinitions;

    private void OnEnable()
    {
        ApplyRaritiesFromRecipeIds();
    }

    private void OnValidate()
    {
        ApplyRaritiesFromRecipeIds();
    }

    public bool TryGetById(string recipeId, out RecipeDefinitionData definition)
    {
        ApplyRaritiesFromRecipeIds();

        definition = null;

        if (string.IsNullOrWhiteSpace(recipeId))
        {
            return false;
        }

        for (int i = 0; i < recipeDefinitions.Count; i++)
        {
            RecipeDefinitionData candidate = recipeDefinitions[i];
            if (candidate == null || candidate.recipeId != recipeId)
            {
                continue;
            }

            definition = candidate;
            return true;
        }

        return false;
    }

    public List<RecipeDefinitionData> GetGachaCandidates(RecipeRarity rarity)
    {
        ApplyRaritiesFromRecipeIds();

        List<RecipeDefinitionData> candidates = new();

        for (int i = 0; i < recipeDefinitions.Count; i++)
        {
            RecipeDefinitionData definition = recipeDefinitions[i];
            if (definition == null || !definition.isGachaEnabled || definition.rarity != rarity)
            {
                continue;
            }

            candidates.Add(definition);
        }

        return candidates;
    }

    private void ApplyRaritiesFromRecipeIds()
    {
        for (int i = 0; i < recipeDefinitions.Count; i++)
        {
            recipeDefinitions[i]?.ApplyRarityFromRecipeId();
        }
    }
}
