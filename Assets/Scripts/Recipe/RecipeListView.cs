using System.Collections.Generic;
using UnityEngine;

public class RecipeListView : MonoBehaviour
{
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private RecipeInventoryState recipeState;
    [SerializeField] private RecipeDetailPopup detailPopup;
    [SerializeField] private bool autoCollectChildCells = true;
    [SerializeField] private List<RecipeItemCellView> itemCells = new();

    private void Awake()
    {
        ResolveMissingReferences();
        CollectChildCellsIfNeeded();
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
        CollectChildCellsIfNeeded();

        if (recipeState != null)
        {
            recipeState.StateChanged += Refresh;
        }

        RegisterCellClicks();
        Refresh();
    }

    private void OnDisable()
    {
        if (recipeState != null)
        {
            recipeState.StateChanged -= Refresh;
        }

        UnregisterCellClicks();
    }

    public void Refresh()
    {
        ResolveMissingReferences();
        CollectChildCellsIfNeeded();

        if (recipeDatabase == null)
        {
            ClearCells();
            return;
        }

        IReadOnlyList<RecipeDefinitionData> recipes = recipeDatabase.RecipeDefinitions;
        for (int i = 0; i < itemCells.Count; i++)
        {
            RecipeItemCellView cell = itemCells[i];
            if (cell == null)
            {
                continue;
            }

            if (i >= recipes.Count)
            {
                cell.Clear();
                continue;
            }

            RecipeDefinitionData definition = recipes[i];
            RecipeOwnedState ownedState = recipeState != null ? recipeState.GetOwnedState(definition.recipeId) : null;
            cell.Bind(definition, ownedState);
        }
    }

    private void ClearCells()
    {
        for (int i = 0; i < itemCells.Count; i++)
        {
            if (itemCells[i] != null)
            {
                itemCells[i].Clear();
            }
        }
    }

    [ContextMenu("Collect Child Cells")]
    private void CollectChildCells()
    {
        UnregisterCellClicks();
        itemCells.Clear();
        GetComponentsInChildren(true, itemCells);
        RegisterCellClicks();
    }

    private void CollectChildCellsIfNeeded()
    {
        if (!autoCollectChildCells || itemCells.Count > 0)
        {
            return;
        }

        CollectChildCells();
    }

    private void ResolveMissingReferences()
    {
        if (recipeState == null)
        {
            recipeState = FindFirstObjectByType<RecipeInventoryState>();
        }

        if (recipeDatabase == null && recipeState != null)
        {
            recipeDatabase = recipeState.RecipeDatabase;
        }

        if (detailPopup == null)
        {
            detailPopup = FindFirstObjectByType<RecipeDetailPopup>(FindObjectsInactive.Include);
        }
    }

    private void RegisterCellClicks()
    {
        for (int i = 0; i < itemCells.Count; i++)
        {
            if (itemCells[i] == null)
            {
                continue;
            }

            itemCells[i].Clicked -= OpenDetail;
            itemCells[i].Clicked += OpenDetail;
        }
    }

    private void UnregisterCellClicks()
    {
        for (int i = 0; i < itemCells.Count; i++)
        {
            if (itemCells[i] != null)
            {
                itemCells[i].Clicked -= OpenDetail;
            }
        }
    }

    private void OpenDetail(RecipeDefinitionData definition)
    {
        if (detailPopup == null)
        {
            ResolveMissingReferences();
        }

        if (detailPopup == null)
        {
            Debug.LogWarning("[RecipeListView] RecipeDetailPopup is not assigned.", this);
            return;
        }

        RecipeOwnedState ownedState = recipeState != null ? recipeState.GetOwnedState(definition.recipeId) : null;
        detailPopup.Open(definition, ownedState, recipeState);
    }
}
