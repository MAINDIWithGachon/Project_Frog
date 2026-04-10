using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 장착 상태를 읽어 장착 슬롯 UI를 채웁니다.
/// 씬에 저장된 오래된 바인딩 값이 남아 있어도 현재 슬롯 순서 기준으로 다시 매핑합니다.
/// </summary>
[ExecuteAlways]
public class EquipmentEquippedSlotBinder : MonoBehaviour
{
    [Serializable]
    private class EquippedSlotBinding
    {
        public Transform slotRoot;
        public EquipmentCategory category;
    }

    private static readonly EquipmentCategory[] DefaultCategoryOrder =
    {
        EquipmentCategory.Weapon,
        EquipmentCategory.Hat,
        EquipmentCategory.Ring,
        EquipmentCategory.Armor,
        EquipmentCategory.Necklace,
        EquipmentCategory.Shoes
    };

    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private EquipmentCategoryTabController categoryTabController;
    [SerializeField] private EquipmentDetailPanelController detailPanelController;
    [SerializeField] private List<EquippedSlotBinding> slotBindings = new();

    [Header("Type Icons")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite hatTypeIcon;
    [SerializeField] private Sprite ringTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite necklaceTypeIcon;
    [SerializeField] private Sprite shoesTypeIcon;

    private EquipmentPrototypeState subscribedState;

    private void Awake()
    {
        ResolveState();
        SubscribeToState();
        RefreshEquippedSlots();
    }

    private void OnEnable()
    {
        ResolveState();
        SubscribeToState();
        RefreshEquippedSlots();
    }

    private void OnDisable()
    {
        UnsubscribeFromState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveState();
        RebuildSlotBindings();
        RefreshEquippedSlots();
    }
#endif

    [ContextMenu("Refresh Equipped Slots")]
    public void RefreshEquippedSlots()
    {
        ResolveState();
        RebuildSlotBindings();

        for (int i = 0; i < slotBindings.Count; i++)
        {
            EquippedSlotBinding binding = slotBindings[i];
            if (binding == null || binding.slotRoot == null)
            {
                continue;
            }

            RefreshSlot(binding.slotRoot, binding.category);
        }
    }

    [ContextMenu("Rebuild Slot Bindings")]
    private void RebuildSlotBindings()
    {
        int targetCount = Mathf.Min(transform.childCount, DefaultCategoryOrder.Length);

        while (slotBindings.Count < targetCount)
        {
            slotBindings.Add(new EquippedSlotBinding());
        }

        if (slotBindings.Count > targetCount)
        {
            slotBindings.RemoveRange(targetCount, slotBindings.Count - targetCount);
        }

        for (int i = 0; i < targetCount; i++)
        {
            EquippedSlotBinding binding = slotBindings[i];
            if (binding == null)
            {
                binding = new EquippedSlotBinding();
                slotBindings[i] = binding;
            }

            binding.slotRoot = transform.GetChild(i);
            binding.category = DefaultCategoryOrder[i];
        }
    }

    private void RefreshSlot(Transform slotRoot, EquipmentCategory category)
    {
        if (slotRoot == null)
        {
            return;
        }

        EnsureSlotClickHandler(slotRoot, category);

        string equippedItemId = equipmentState != null
            ? equipmentState.GetEquippedItemId(category)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(equippedItemId) ||
            equipmentState == null ||
            equipmentState.EquipmentDatabase == null ||
            !equipmentState.EquipmentDatabase.TryGetById(equippedItemId, out EquipmentDefinitionData definition) ||
            definition.category != category)
        {
            ApplyEmptySlot(slotRoot, category);
            return;
        }

        int currentLevel = equipmentState != null ? equipmentState.GetOwnedLevel(equippedItemId) : 1;
        ApplyFilledSlot(slotRoot, definition, currentLevel);
    }

    private void ApplyFilledSlot(Transform slotRoot, EquipmentDefinitionData definition, int level)
    {
        EquipmentEquippedSlotView slotView = GetOrAddSlotView(slotRoot);
        if (slotView == null)
        {
            return;
        }

        slotView.SetEquipped(definition, level);
        slotView.SetRedDotVisible(ShouldShowUpgradeReadyRedDot(definition));
    }

    private void ApplyEmptySlot(Transform slotRoot, EquipmentCategory category)
    {
        EquipmentEquippedSlotView slotView = GetOrAddSlotView(slotRoot);
        if (slotView == null)
        {
            return;
        }

        slotView.SetEmpty(category);
        slotView.SetRedDotVisible(ShouldShowEmptySlotOwnedItemRedDot(category));
    }

    private void ResolveState()
    {
        if (equipmentState == null)
        {
            equipmentState = FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);
        }

        if (runtimeData == null)
        {
            runtimeData = FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        }

        if (categoryTabController == null)
        {
            categoryTabController = GetComponentInParent<EquipmentCategoryTabController>(true);
            categoryTabController ??= FindFirstObjectByType<EquipmentCategoryTabController>(FindObjectsInactive.Include);
        }

        if (detailPanelController == null)
        {
            detailPanelController = GetComponentInParent<EquipmentDetailPanelController>(true);
            detailPanelController ??= FindFirstObjectByType<EquipmentDetailPanelController>(FindObjectsInactive.Include);
        }
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
            equipmentState.StateChanged += RefreshEquippedSlots;
            subscribedState = equipmentState;
        }
    }

    private void UnsubscribeFromState()
    {
        if (subscribedState != null)
        {
            subscribedState.StateChanged -= RefreshEquippedSlots;
            subscribedState = null;
        }
    }

    private EquipmentEquippedSlotView GetOrAddSlotView(Transform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        EquipmentEquippedSlotView slotView = slotRoot.GetComponent<EquipmentEquippedSlotView>();
        if (slotView == null)
        {
            slotView = slotRoot.gameObject.AddComponent<EquipmentEquippedSlotView>();
        }

        slotView.SetTypeIcons(
            weaponTypeIcon,
            hatTypeIcon,
            ringTypeIcon,
            armorTypeIcon,
            necklaceTypeIcon,
            shoesTypeIcon);

        return slotView;
    }

    private bool ShouldShowUpgradeReadyRedDot(EquipmentDefinitionData definition)
    {
        if (definition == null || equipmentState == null || runtimeData == null || string.IsNullOrWhiteSpace(definition.equipmentId))
        {
            return false;
        }

        EquipmentUpgradeRequirement requirement = equipmentState.GetUpgradeRequirement(
            definition.equipmentId,
            runtimeData.GetGold(),
            runtimeData.GetUpgradeStone());

        return requirement != null && requirement.CanUpgrade;
    }

    private bool ShouldShowEmptySlotOwnedItemRedDot(EquipmentCategory category)
    {
        if (equipmentState == null)
        {
            return false;
        }

        if (equipmentState.IsCategoryEquipped(category))
        {
            return false;
        }

        return equipmentState.HasOwnedItemInCategory(category);
    }

    private void EnsureSlotClickHandler(Transform slotRoot, EquipmentCategory category)
    {
        Button button = slotRoot.GetComponent<Button>();
        if (button == null)
        {
            button = slotRoot.GetComponentInChildren<Button>(true);
        }

        if (button == null)
        {
            return;
        }

        EquipmentEquippedSlotClickHandler clickHandler = button.GetComponent<EquipmentEquippedSlotClickHandler>();
        if (clickHandler == null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            clickHandler = button.gameObject.AddComponent<EquipmentEquippedSlotClickHandler>();
        }

        clickHandler.Configure(category, equipmentState, categoryTabController, detailPanelController, button);
    }
}

/// <summary>
/// 장착 슬롯 클릭 시 장착 중인 아이템 상세를 열거나, 비어 있으면 해당 카테고리 탭으로 이동합니다.
/// </summary>
