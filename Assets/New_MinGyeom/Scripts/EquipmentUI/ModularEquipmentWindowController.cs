using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModularEquipmentWindowController : MonoBehaviour
{
    private static readonly EquipmentCategory[] CategoryOrder =
    {
        EquipmentCategory.Weapon,
        EquipmentCategory.Hat,
        EquipmentCategory.Ring,
        EquipmentCategory.Armor,
        EquipmentCategory.Necklace,
        EquipmentCategory.Shoes
    };

    [Header("Injected References")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private ModularEquipmentDetailPanelController detailPanelController;

    [Header("Auto Bound UI")]
    [SerializeField] private Transform listContentRoot;
    [SerializeField] private Transform tabRoot;
    [SerializeField] private Transform equippedSlotsRoot;
    [SerializeField] private Button[] closeButtons;

    [Header("State")]
    [SerializeField] private EquipmentCategory currentCategory = EquipmentCategory.Weapon;

    private readonly List<ModularEquipmentListItemView> listItemViews = new();
    private readonly List<ModularEquippedSlotView> equippedSlotViews = new();
    private readonly List<GameObject> tabFocusObjects = new();
    private EquipmentPrototypeState subscribedState;
    private bool tabsBound;
    private bool closeButtonsBound;

    private void Awake()
    {
        ResolveReferences();
        BindUi();
        SubscribeToState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindUi();
        SubscribeToState();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromState();
    }

    public void Configure(
        GameObject root,
        EquipmentPrototypeState state,
        RuntimeData data,
        ModularEquipmentDetailPanelController detailController)
    {
        windowRoot = root;
        equipmentState = state;
        runtimeData = data;
        detailPanelController = detailController;

        ResolveReferences();
        BindUi();
        SubscribeToState();
        Refresh();
    }

    public void Open()
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(true);
        }

        Refresh();
    }

    public void Close()
    {
        detailPanelController?.Close();

        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
        }
    }

    public void SelectCategory(EquipmentCategory category)
    {
        currentCategory = category;
        RefreshList();
        RefreshTabFocus();
    }

    public void Refresh()
    {
        ResolveReferences();
        RefreshList();
        RefreshEquippedSlots();
        RefreshTabFocus();
    }

    private void ResolveReferences()
    {
        if (windowRoot == null)
        {
            windowRoot = gameObject;
        }

        Transform root = windowRoot != null ? windowRoot.transform : transform;

        listContentRoot ??= FindDescendantByName(root, "Content");
        tabRoot ??= FindDescendantByName(root, "Tab_02_BoxMenu_Icon");
        equippedSlotsRoot ??= FindDescendantByNameContains(root, "Current_Equipment");

        if (equipmentState == null)
        {
            equipmentState = GetComponentInParent<EquipmentModuleRoot>()?.EquipmentState;
        }

        if (runtimeData == null)
        {
            runtimeData = GetComponentInParent<EquipmentModuleRoot>()?.RuntimeData;
        }

        if (detailPanelController == null)
        {
            detailPanelController = GetComponentInParent<EquipmentModuleRoot>()?.GetComponentInChildren<ModularEquipmentDetailPanelController>(true);
        }
    }

    private void BindUi()
    {
        BindListItems();
        BindEquippedSlots();
        BindTabs();
        BindCloseButtons();
    }

    private void BindListItems()
    {
        listItemViews.Clear();

        Transform searchRoot = listContentRoot != null ? listContentRoot : (windowRoot != null ? windowRoot.transform : transform);
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || !IsListItemRoot(child.name))
            {
                continue;
            }

            ModularEquipmentListItemView itemView = child.GetComponent<ModularEquipmentListItemView>();
            if (itemView == null)
            {
                itemView = child.gameObject.AddComponent<ModularEquipmentListItemView>();
            }

            itemView.Configure(HandleItemClicked);
            listItemViews.Add(itemView);
        }
    }

    private void BindEquippedSlots()
    {
        equippedSlotViews.Clear();

        Transform searchRoot = equippedSlotsRoot != null ? equippedSlotsRoot : (windowRoot != null ? windowRoot.transform : transform);
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || !child.name.Contains("EquipedSlotListItem"))
            {
                continue;
            }

            ModularEquippedSlotView slotView = child.GetComponent<ModularEquippedSlotView>();
            if (slotView == null)
            {
                slotView = child.gameObject.AddComponent<ModularEquippedSlotView>();
            }

            EquipmentCategory category = equippedSlotViews.Count < CategoryOrder.Length
                ? CategoryOrder[equippedSlotViews.Count]
                : EquipmentCategory.Weapon;

            slotView.Configure(category, HandleEquippedSlotClicked);
            equippedSlotViews.Add(slotView);
        }
    }

    private void BindTabs()
    {
        if (tabsBound || tabRoot == null)
        {
            return;
        }

        Button[] buttons = tabRoot.GetComponentsInChildren<Button>(true);
        int count = Mathf.Min(buttons.Length, CategoryOrder.Length);
        tabFocusObjects.Clear();

        for (int i = 0; i < count; i++)
        {
            Button button = buttons[i];
            EquipmentCategory category = CategoryOrder[i];
            GameObject focus = FindDescendantByName(button.transform, "Focus")?.gameObject;
            tabFocusObjects.Add(focus);

            button.onClick.AddListener(() => SelectCategory(category));
        }

        tabsBound = true;
    }

    private void BindCloseButtons()
    {
        if (closeButtonsBound)
        {
            return;
        }

        if (closeButtons == null || closeButtons.Length == 0)
        {
            closeButtons = FindCloseButtons();
        }

        for (int i = 0; i < closeButtons.Length; i++)
        {
            Button button = closeButtons[i];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(Close);
            button.onClick.AddListener(Close);
        }

        closeButtonsBound = true;
    }

    private void RefreshList()
    {
        if (equipmentState == null || equipmentState.EquipmentDatabase == null)
        {
            ClearList();
            return;
        }

        IReadOnlyList<EquipmentDefinitionData> definitions = equipmentState.EquipmentDatabase.EquipmentDefinitions;
        List<EquipmentDefinitionData> ownedDefinitions = new();

        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinitionData definition = definitions[i];
            if (definition == null || definition.category != currentCategory)
            {
                continue;
            }

            EquipmentOwnedState ownedState = equipmentState.GetOwnedState(definition.equipmentId);
            if (ownedState == null || ownedState.ownedCount <= 0)
            {
                continue;
            }

            ownedDefinitions.Add(definition);
        }

        for (int i = 0; i < listItemViews.Count; i++)
        {
            ModularEquipmentListItemView view = listItemViews[i];
            if (view == null)
            {
                continue;
            }

            if (i < ownedDefinitions.Count)
            {
                EquipmentDefinitionData definition = ownedDefinitions[i];
                EquipmentOwnedState ownedState = equipmentState.GetOwnedState(definition.equipmentId);
                bool isEquipped = equipmentState.IsEquippedInSlot(definition);

                view.SetItem(
                    definition,
                    ownedState != null ? ownedState.currentLevel : 1,
                    ownedState != null ? ownedState.ownedCount : 1,
                    isEquipped,
                    equipmentState.GetUpgradeRequirement(
                        definition.equipmentId,
                        runtimeData != null ? runtimeData.GetGold() : int.MaxValue,
                        runtimeData != null ? runtimeData.GetUpgradeStone() : int.MaxValue).CanUpgrade);
            }
            else if (i == ownedDefinitions.Count)
            {
                view.SetAddSlot();
            }
            else
            {
                view.Clear();
            }
        }
    }

    private void RefreshEquippedSlots()
    {
        for (int i = 0; i < equippedSlotViews.Count; i++)
        {
            ModularEquippedSlotView view = equippedSlotViews[i];
            if (view == null)
            {
                continue;
            }

            EquipmentCategory category = i < CategoryOrder.Length ? CategoryOrder[i] : EquipmentCategory.Weapon;
            EquipmentDefinitionData definition = equipmentState != null ? equipmentState.GetEquippedDefinition(category) : null;
            int level = definition != null ? equipmentState.GetOwnedLevel(definition.equipmentId) : 0;
            bool showRedDot = definition == null
                ? equipmentState != null && equipmentState.HasOwnedItemInCategory(category)
                : equipmentState.GetUpgradeRequirement(
                    definition.equipmentId,
                    runtimeData != null ? runtimeData.GetGold() : int.MaxValue,
                    runtimeData != null ? runtimeData.GetUpgradeStone() : int.MaxValue).CanUpgrade;

            view.SetSlot(category, definition, level, showRedDot);
        }
    }

    private void RefreshTabFocus()
    {
        for (int i = 0; i < tabFocusObjects.Count; i++)
        {
            GameObject focus = tabFocusObjects[i];
            if (focus == null)
            {
                continue;
            }

            focus.SetActive(i < CategoryOrder.Length && CategoryOrder[i] == currentCategory);
        }
    }

    private void ClearList()
    {
        for (int i = 0; i < listItemViews.Count; i++)
        {
            listItemViews[i]?.Clear();
        }
    }

    private void HandleItemClicked(EquipmentDefinitionData definition, int level)
    {
        if (definition == null)
        {
            return;
        }

        detailPanelController?.OpenDetail(definition, level);
    }

    private void HandleEquippedSlotClicked(EquipmentCategory category, EquipmentDefinitionData definition, int level)
    {
        if (definition != null)
        {
            detailPanelController?.OpenDetail(definition, level);
            return;
        }

        SelectCategory(category);
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
            equipmentState.StateChanged += Refresh;
            subscribedState = equipmentState;
        }
    }

    private void UnsubscribeFromState()
    {
        if (subscribedState == null)
        {
            return;
        }

        subscribedState.StateChanged -= Refresh;
        subscribedState = null;
    }

    private Button[] FindCloseButtons()
    {
        Transform root = windowRoot != null ? windowRoot.transform : transform;
        List<Button> results = new();
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string lowerName = button.name.ToLowerInvariant();
            if (lowerName.Contains("close") || lowerName.Contains("exit") || lowerName.Contains("back"))
            {
                results.Add(button);
            }
        }

        return results.ToArray();
    }

    private static bool IsListItemRoot(string objectName)
    {
        return objectName.Contains("ListItem_EquipMent") ||
               objectName.Contains("EquipmentInventoryListItem");
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
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

    private static Transform FindDescendantByNameContains(Transform root, string namePart)
    {
        if (root == null || string.IsNullOrEmpty(namePart))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.Contains(namePart))
            {
                return child;
            }

            Transform found = FindDescendantByNameContains(child, namePart);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
