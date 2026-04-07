using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills the inventory list so owned equipment is packed from the front,
/// followed by one add slot, while remaining slots are cleared.
/// </summary>
public class EquipmentInventoryListBinder : MonoBehaviour
{
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private EquipmentCategory currentCategory = EquipmentCategory.Weapon;

    private EquipmentListItemView[] slots;
    private EquipmentPrototypeState subscribedState;

    private void Awake()
    {
        ResolveReferences();
        RefreshInventory();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToState();
        RefreshInventory();
    }

    private void OnDisable()
    {
        UnsubscribeFromState();
    }

    [ContextMenu("Refresh Inventory")]
    public void RefreshInventory()
    {
        ResolveReferences();

        if (equipmentState == null)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryListBinder)}] EquipmentPrototypeState was not found.", this);
            return;
        }

        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryListBinder)}] No {nameof(EquipmentListItemView)} slots were found.", this);
            return;
        }

        List<EquipmentDefinitionData> ownedItems = GetOwnedCategoryItems();
        int slotCount = slots.Length;
        int ownedItemCount = ownedItems.Count;
        int visibleSlotCount = Mathf.Min(slotCount, ownedItemCount + 1);

        for (int i = 0; i < slotCount; i++)
        {
            EquipmentListItemView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            if (i < ownedItemCount && ownedItems[i] != null)
            {
                EquipmentDefinitionData definition = ownedItems[i];
                EquipmentOwnedState ownedState = GetOwnedState(definition.equipmentId);

                if (ownedState == null)
                {
                    slot.Clear();
                    continue;
                }

                slot.SetItemById(
                    ownedState.equipmentId,
                    ownedState.currentLevel,
                    ownedState.ownedCount,
                    equipmentState,
                    equipmentState.EquipmentDatabase,
                    runtimeData);
            }
            else if (i == ownedItemCount && i < visibleSlotCount)
            {
                slot.SetAddSlot();
            }
            else
            {
                slot.Clear();
            }
        }
    }

    public void SetCategory(EquipmentCategory category)
    {
        currentCategory = category;
        RefreshInventory();
    }

    private void ResolveReferences()
    {
        if (equipmentState == null)
        {
            equipmentState = FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);
        }

        if (runtimeData == null)
        {
            runtimeData = FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        }

        if (contentRoot == null)
        {
            contentRoot = FindDescendantByName(transform, "Content");
        }

        if (contentRoot != null)
        {
            slots = contentRoot.GetComponentsInChildren<EquipmentListItemView>(true);
        }
    }

    private List<EquipmentDefinitionData> GetOwnedCategoryItems()
    {
        List<EquipmentDefinitionData> categoryItems = new();

        if (equipmentState == null || equipmentState.EquipmentDatabase == null)
        {
            return categoryItems;
        }

        IReadOnlyList<EquipmentDefinitionData> definitions = equipmentState.EquipmentDatabase.EquipmentDefinitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinitionData definition = definitions[i];
            if (definition == null || definition.category != currentCategory)
            {
                continue;
            }

            EquipmentOwnedState ownedState = GetOwnedState(definition.equipmentId);
            if (ownedState == null || ownedState.ownedCount <= 0)
            {
                continue;
            }

            categoryItems.Add(definition);
        }

        return categoryItems;
    }

    private EquipmentOwnedState GetOwnedState(string equipmentId)
    {
        return equipmentState == null ? null : equipmentState.GetOwnedState(equipmentId);
    }

    private void SubscribeToState()
    {
        if (equipmentState == subscribedState)
        {
            return;
        }

        UnsubscribeFromState();

        if (equipmentState != null)
        {
            equipmentState.StateChanged += RefreshInventory;
            subscribedState = equipmentState;
        }
    }

    private void UnsubscribeFromState()
    {
        if (subscribedState != null)
        {
            subscribedState.StateChanged -= RefreshInventory;
            subscribedState = null;
        }
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform found = FindDescendantByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
