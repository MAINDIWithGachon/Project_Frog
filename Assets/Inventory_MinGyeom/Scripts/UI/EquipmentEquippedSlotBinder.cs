using System;
using System.Collections.Generic;
using TMPro;
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
            ApplySlotViewState(slotRoot, string.Empty, 0, 0);
            ApplyEmptySlot(slotRoot);
            return;
        }

        int currentLevel = equipmentState != null ? equipmentState.GetOwnedLevel(equippedItemId) : 1;
        int ownedCount = equipmentState != null ? equipmentState.GetOwnedCount(equippedItemId) : 0;
        ApplySlotViewState(slotRoot, equippedItemId, currentLevel, ownedCount);
        ApplyFilledSlot(slotRoot, definition, currentLevel);
    }

    private void ApplySlotViewState(Transform slotRoot, string equipmentId, int level, int ownedCount)
    {
        EquipmentListItemView slotView = slotRoot.GetComponentInChildren<EquipmentListItemView>(true);
        if (slotView == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            slotView.Clear();
            return;
        }

        slotView.SetItemById(
            equipmentId,
            Mathf.Max(1, level),
            ownedCount,
            equipmentState,
            equipmentState != null ? equipmentState.EquipmentDatabase : null);
    }

    private void ApplyFilledSlot(Transform slotRoot, EquipmentDefinitionData definition, int level)
    {
        SetRarityFrame(slotRoot, definition.rarity);

        Image itemIcon = FindImage(slotRoot, "ItemFrame_01/Item/Icon") ?? FindImage(slotRoot, "ItemFrame_01/Item");
        if (itemIcon != null)
        {
            itemIcon.sprite = definition.icon;
            itemIcon.enabled = definition.icon != null;
        }

        TMP_Text levelText = FindText(slotRoot, "Text_Level");
        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = $"Lv.{Mathf.Max(1, level)}";
        }

        GameObject typeArea = FindObject(slotRoot, "TypeArea");
        if (typeArea != null)
        {
            typeArea.SetActive(true);
        }

        Image typeFrame = FindImage(slotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab");
        if (typeFrame != null)
        {
            typeFrame.color = GetTypeFrameColor(definition.rarity);
        }

        Image typeBg = FindImage(slotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Bg");
        if (typeBg != null)
        {
            typeBg.color = GetTypeFillColor(definition.rarity);
        }

        Image typeIcon = FindImage(slotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Icon");
        if (typeIcon != null)
        {
            typeIcon.sprite = GetCategoryIcon(definition.category);
            typeIcon.enabled = typeIcon.sprite != null;
        }

        SetActive(slotRoot, "ItemFrame_01/Add_2", false);
        SetActive(slotRoot, "ItemFrame_01/Add_1", false);
        SetActive(slotRoot, "Check", false);
    }

    private void ApplyEmptySlot(Transform slotRoot)
    {
        SetRarityFrame(slotRoot, EquipmentRarity.Common);

        Image itemIcon = FindImage(slotRoot, "ItemFrame_01/Item/Icon") ?? FindImage(slotRoot, "ItemFrame_01/Item");
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        TMP_Text levelText = FindText(slotRoot, "Text_Level");
        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        SetActive(slotRoot, "TypeArea", false);
        SetActive(slotRoot, "ItemFrame_01/Add_1", false);
        SetActive(slotRoot, "ItemFrame_01/Add_2", true);
        SetActive(slotRoot, "Check", false);
    }

    private void SetRarityFrame(Transform slotRoot, EquipmentRarity rarity)
    {
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Blue", rarity == EquipmentRarity.Rare);
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brown", rarity == EquipmentRarity.Common);
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Green", rarity == EquipmentRarity.Magic);
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Plum", rarity == EquipmentRarity.Epic);
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Yellow", rarity == EquipmentRarity.Legendary);
        SetActive(slotRoot, "ItemFrame_01/NormalArea/ItemFrame_01_Normal_Red", false);
    }

    private void ResolveState()
    {
        if (equipmentState == null)
        {
            equipmentState = FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);
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

    private Sprite GetCategoryIcon(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Weapon => weaponTypeIcon,
            EquipmentCategory.Hat => hatTypeIcon,
            EquipmentCategory.Ring => ringTypeIcon,
            EquipmentCategory.Armor => armorTypeIcon,
            EquipmentCategory.Necklace => necklaceTypeIcon,
            EquipmentCategory.Shoes => shoesTypeIcon,
            _ => weaponTypeIcon
        };
    }

    private static Color GetTypeFrameColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => new Color32(181, 126, 79, 255),
            EquipmentRarity.Magic => new Color32(74, 151, 84, 255),
            EquipmentRarity.Rare => new Color32(52, 103, 185, 255),
            EquipmentRarity.Epic => new Color32(151, 86, 187, 255),
            EquipmentRarity.Legendary => new Color32(214, 149, 44, 255),
            _ => Color.white
        };
    }

    private static Color GetTypeFillColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => new Color32(241, 206, 146, 255),
            EquipmentRarity.Magic => new Color32(138, 219, 138, 255),
            EquipmentRarity.Rare => new Color32(99, 191, 255, 255),
            EquipmentRarity.Epic => new Color32(206, 144, 255, 255),
            EquipmentRarity.Legendary => new Color32(255, 221, 105, 255),
            _ => Color.white
        };
    }

    private static void SetActive(Transform root, string relativePath, bool isActive)
    {
        GameObject target = FindObject(root, relativePath);
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private static GameObject FindObject(Transform root, string relativePath)
    {
        Transform found = root.Find(relativePath);
        return found != null ? found.gameObject : null;
    }

    private static Image FindImage(Transform root, string relativePath)
    {
        Transform found = root.Find(relativePath);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private static TMP_Text FindText(Transform root, string relativePath)
    {
        Transform found = root.Find(relativePath);
        return found != null ? found.GetComponent<TMP_Text>() : null;
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
