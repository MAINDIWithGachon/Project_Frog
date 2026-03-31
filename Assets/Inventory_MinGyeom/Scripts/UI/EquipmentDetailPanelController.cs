using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 리스트 아이템 클릭으로 상세 패널을 열고, 지정된 UI 요소로 닫을 수 있게 관리합니다.
/// </summary>
public class EquipmentDetailPanelController : MonoBehaviour
{
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Transform listRoot;
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private Button[] closeButtons;
    [SerializeField] private Graphic[] closeClickAreas;
    [SerializeField] private bool hideDetailOnStart = true;
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject upgradeSliderRoot;
    [SerializeField] private Slider upgradeProgressSlider;
    [SerializeField] private TMP_Text upgradeProgressText;
    [SerializeField] private GameObject upgradeReadyObject;
    [SerializeField] private GameObject upgradeLockObject;

    [Header("Detail UI")]
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailRarityText;
    [SerializeField] private TMP_Text detailLevelText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailStatText;
    [SerializeField] private Transform rarityLabelRoot;
    [SerializeField] private Transform detailSlotRoot;

    [Header("Type Icons")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite hatTypeIcon;
    [SerializeField] private Sprite ringTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite necklaceTypeIcon;
    [SerializeField] private Sprite shoesTypeIcon;

    private EquipmentDefinitionData currentDefinition;
    private int currentLevel;
    private EquipmentPrototypeState subscribedState;

    private void Awake()
    {
        if (!EnsurePrimaryController())
        {
            enabled = false;
        }
    }

    private void Start()
    {
        ResolveReferences();

        if (detailPanel == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] detailPanel is not assigned.", this);
            return;
        }

        if (listRoot == null)
        {
            listRoot = transform;
        }

        if (hideDetailOnStart)
        {
            detailPanel.SetActive(false);
        }

        BindButtons();
        BindCloseTargets();
        BindEquipButton();
        BindUpgradeButton();
        SubscribeToState();
    }

    private bool EnsurePrimaryController()
    {
        EquipmentDetailPanelController[] controllers =
            FindObjectsByType<EquipmentDetailPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        EquipmentDetailPanelController primary = this;
        for (int i = 0; i < controllers.Length; i++)
        {
            EquipmentDetailPanelController candidate = controllers[i];
            if (candidate == null || candidate == this || candidate.detailPanel != detailPanel)
            {
                continue;
            }

            if (GetBindingScore(candidate) > GetBindingScore(primary))
            {
                primary = candidate;
            }
        }

        if (primary != this)
        {
            primary.AbsorbBindingsFrom(this);
            return false;
        }

        for (int i = 0; i < controllers.Length; i++)
        {
            EquipmentDetailPanelController duplicate = controllers[i];
            if (duplicate == null || duplicate == this || duplicate.detailPanel != detailPanel)
            {
                continue;
            }

            AbsorbBindingsFrom(duplicate);
            duplicate.enabled = false;
        }

        return true;
    }

    private void AbsorbBindingsFrom(EquipmentDetailPanelController other)
    {
        if (other == null)
        {
            return;
        }

        detailPanel ??= other.detailPanel;
        listRoot ??= other.listRoot;
        equipmentState ??= other.equipmentState;
        runtimeData ??= other.runtimeData;
        equipButton ??= other.equipButton;
        equipButtonText ??= other.equipButtonText;
        upgradeButton ??= other.upgradeButton;
        upgradeSliderRoot ??= other.upgradeSliderRoot;
        upgradeProgressSlider ??= other.upgradeProgressSlider;
        upgradeProgressText ??= other.upgradeProgressText;
        upgradeReadyObject ??= other.upgradeReadyObject;
        upgradeLockObject ??= other.upgradeLockObject;
        detailIconImage ??= other.detailIconImage;
        detailNameText ??= other.detailNameText;
        detailRarityText ??= other.detailRarityText;
        detailLevelText ??= other.detailLevelText;
        detailDescriptionText ??= other.detailDescriptionText;
        detailStatText ??= other.detailStatText;
        rarityLabelRoot ??= other.rarityLabelRoot;
        detailSlotRoot ??= other.detailSlotRoot;
        weaponTypeIcon ??= other.weaponTypeIcon;
        hatTypeIcon ??= other.hatTypeIcon;
        ringTypeIcon ??= other.ringTypeIcon;
        armorTypeIcon ??= other.armorTypeIcon;
        necklaceTypeIcon ??= other.necklaceTypeIcon;
        shoesTypeIcon ??= other.shoesTypeIcon;

        if ((closeButtons == null || closeButtons.Length == 0) && other.closeButtons is { Length: > 0 })
        {
            closeButtons = other.closeButtons;
        }

        if ((closeClickAreas == null || closeClickAreas.Length == 0) && other.closeClickAreas is { Length: > 0 })
        {
            closeClickAreas = other.closeClickAreas;
        }
    }

    private int GetBindingScore(EquipmentDetailPanelController controller)
    {
        if (controller == null)
        {
            return -1;
        }

        int score = 0;
        score += controller.detailPanel != null ? 4 : 0;
        score += controller.listRoot != null ? 2 : 0;
        score += controller.equipmentState != null ? 2 : 0;
        score += controller.runtimeData != null ? 2 : 0;
        score += controller.equipButton != null ? 1 : 0;
        score += controller.equipButtonText != null ? 1 : 0;
        score += controller.upgradeButton != null ? 3 : 0;
        score += controller.upgradeSliderRoot != null ? 3 : 0;
        score += controller.upgradeProgressSlider != null ? 3 : 0;
        score += controller.upgradeProgressText != null ? 3 : 0;
        score += controller.upgradeReadyObject != null ? 1 : 0;
        score += controller.upgradeLockObject != null ? 1 : 0;
        score += controller.detailIconImage != null ? 1 : 0;
        score += controller.detailNameText != null ? 1 : 0;
        score += controller.detailRarityText != null ? 1 : 0;
        score += controller.detailLevelText != null ? 1 : 0;
        score += controller.detailDescriptionText != null ? 1 : 0;
        score += controller.detailStatText != null ? 1 : 0;
        score += controller.rarityLabelRoot != null ? 1 : 0;
        score += controller.detailSlotRoot != null ? 1 : 0;
        score += controller.closeButtons != null ? controller.closeButtons.Length : 0;
        score += controller.closeClickAreas != null ? controller.closeClickAreas.Length : 0;
        score += controller.weaponTypeIcon != null ? 1 : 0;
        score += controller.hatTypeIcon != null ? 1 : 0;
        score += controller.ringTypeIcon != null ? 1 : 0;
        score += controller.armorTypeIcon != null ? 1 : 0;
        score += controller.necklaceTypeIcon != null ? 1 : 0;
        score += controller.shoesTypeIcon != null ? 1 : 0;
        return score;
    }

    private void OnEnable()
    {
        if (!enabled)
        {
            return;
        }

        ResolveReferences();
        SubscribeToState();
        RefreshEquipButton();
        RefreshUpgradeButton();
        RefreshUpgradeProgress();
    }

    private void OnDisable()
    {
        UnsubscribeFromState();
    }

    [ContextMenu("Bind Buttons")]
    public void BindButtons()
    {
        ResolveReferences();

        if (detailPanel == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] detailPanel is not assigned.", this);
            return;
        }

        if (listRoot == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] listRoot is not assigned.", this);
            return;
        }

        Button[] buttons = listRoot.GetComponentsInChildren<Button>(true);
        int bindCount = 0;

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            // 인벤토리 리스트 아이템 버튼만 상세 패널을 열 수 있도록 제한합니다.
            if (!ShouldBindEquipmentButton(button))
            {
                continue;
            }

            EquipmentListItemView itemView = button.GetComponentInParent<EquipmentListItemView>(true);
            if (itemView == null)
            {
                continue;
            }

            EquipmentDetailPanelItemButton binder = button.GetComponent<EquipmentDetailPanelItemButton>();
            if (binder == null)
            {
                binder = button.gameObject.AddComponent<EquipmentDetailPanelItemButton>();
            }

            binder.Configure(this, itemView, button);
            bindCount++;
        }

        Debug.Log($"[EquipmentDetailPanelController] Bound {bindCount} item buttons.", this);
    }

    [ContextMenu("Bind Close Targets")]
    public void BindCloseTargets()
    {
        int bindCount = 0;

        if (closeButtons != null)
        {
            foreach (Button closeButton in closeButtons)
            {
                if (closeButton == null)
                {
                    continue;
                }

                closeButton.onClick.RemoveListener(CloseDetailPanel);
                closeButton.onClick.AddListener(CloseDetailPanel);
                bindCount++;
            }
        }

        if (closeClickAreas != null)
        {
            foreach (Graphic closeArea in closeClickAreas)
            {
                if (closeArea == null)
                {
                    continue;
                }

                // 닫기 영역이 이미지/그래픽만 있는 경우, 클릭 처리를 위해 Button을 자동 추가합니다.
                Button closeAreaButton = closeArea.GetComponent<Button>();
                if (closeAreaButton == null)
                {
                    closeAreaButton = closeArea.gameObject.AddComponent<Button>();
                    closeAreaButton.transition = Selectable.Transition.None;
                }

                if (closeAreaButton.targetGraphic == null)
                {
                    closeAreaButton.targetGraphic = closeArea;
                }

                closeAreaButton.onClick.RemoveListener(CloseDetailPanel);
                closeAreaButton.onClick.AddListener(CloseDetailPanel);
                bindCount++;
            }
        }

        Debug.Log($"[EquipmentDetailPanelController] Bound {bindCount} close targets.", this);
    }

    public void OpenDetailPanel()
    {
        ResolveReferences();

        if (detailPanel == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] detailPanel is not assigned.", this);
            return;
        }

        detailPanel.SetActive(true);
        Debug.Log("[EquipmentDetailPanelController] Opened Character_Hero_Item_Detail.", this);
    }

    public void OpenDetailPanel(EquipmentListItemView itemView)
    {
        ResolveReferences();

        if (itemView == null)
        {
            return;
        }

        if (equipmentState == null || equipmentState.EquipmentDatabase == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] Equipment state or database is not assigned.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(itemView.CurrentEquipmentId))
        {
            return;
        }

        if (!equipmentState.EquipmentDatabase.TryGetById(itemView.CurrentEquipmentId, out EquipmentDefinitionData definition))
        {
            Debug.LogWarning($"[EquipmentDetailPanelController] Could not find item id '{itemView.CurrentEquipmentId}'.", this);
            return;
        }

        BindDetail(definition, itemView.CurrentLevel);
        OpenDetailPanel();
    }

    public void OpenDetailPanel(EquipmentCategory category)
    {
        ResolveReferences();

        if (equipmentState == null)
        {
            return;
        }

        EquipmentDefinitionData definition = equipmentState.GetEquippedDefinition(category);
        if (definition == null)
        {
            return;
        }

        BindDetail(definition, GetOwnedLevel(definition.equipmentId));
        OpenDetailPanel();
    }

    public void CloseDetailPanel()
    {
        if (detailPanel == null)
        {
            Debug.LogWarning("[EquipmentDetailPanelController] detailPanel is not assigned.", this);
            return;
        }

        detailPanel.SetActive(false);
        Debug.Log("[EquipmentDetailPanelController] Closed Character_Hero_Item_Detail.", this);
    }

    private void BindDetail(EquipmentDefinitionData definition, int level)
    {
        if (definition == null)
        {
            ClearDetail();
            return;
        }

        currentDefinition = definition;
        currentLevel = Mathf.Max(1, level);

        if (detailIconImage != null)
        {
            detailIconImage.sprite = definition.icon;
            detailIconImage.enabled = definition.icon != null;
        }

        if (detailNameText != null)
        {
            detailNameText.text = definition.displayName;
        }

        if (detailRarityText != null)
        {
            detailRarityText.text = GetRarityLabel(definition.rarity);
        }

        if (detailLevelText != null)
        {
            detailLevelText.text = $"Lv.{Mathf.Max(1, level)}";
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = definition.description;
        }

        if (detailStatText != null)
        {
            detailStatText.text = BuildStatText(definition);
        }

        ApplyRarityLabel(definition.rarity);
        ApplyDetailSlot(definition);
        RefreshUpgradeButton();
        RefreshUpgradeProgress();
    }

    private void ClearDetail()
    {
        if (detailIconImage != null)
        {
            detailIconImage.sprite = null;
            detailIconImage.enabled = false;
        }

        if (detailNameText != null)
        {
            detailNameText.text = string.Empty;
        }

        if (detailRarityText != null)
        {
            detailRarityText.text = string.Empty;
        }

        if (detailLevelText != null)
        {
            detailLevelText.text = string.Empty;
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = string.Empty;
        }

        if (detailStatText != null)
        {
            detailStatText.text = string.Empty;
        }

        ApplyRarityLabel(EquipmentRarity.Common, false);

        if (detailSlotRoot != null)
        {
            SetDetailSlotFrame(detailSlotRoot, EquipmentRarity.Common);
            SetSlotTypeAreaActive(false);
        }

        currentDefinition = null;
        currentLevel = 0;
        RefreshEquipButton();
        RefreshUpgradeButton();
        RefreshUpgradeProgress();
    }

    private void ResolveReferences()
    {
        if (listRoot == null)
        {
            listRoot = transform;
        }

        if (equipmentState == null)
        {
            equipmentState = FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);
        }

        if (runtimeData == null)
        {
            runtimeData = FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        }

        Transform detailRoot = detailPanel != null ? detailPanel.transform : null;
        if (detailRoot == null)
        {
            return;
        }

        detailNameText ??= FindTextByName(detailRoot, "Text_ItemName");
        detailLevelText ??= FindTextByName(detailRoot, "Text_Level");
        detailStatText ??= FindTextByName(detailRoot, "Text_GearStats");
        detailRarityText ??= FindTextByName(detailRoot, "Text_Rarity");
        detailDescriptionText ??= FindTextByName(detailRoot, "Text_Description");
        detailDescriptionText ??= FindTextByName(detailRoot, "Text_Desc");
        rarityLabelRoot ??= FindDescendantByName(detailRoot, "Rarity_Label");
        detailSlotRoot ??= FindDescendantByName(detailRoot, "Slot");
        equipButton ??= FindButtonByName(detailRoot, "EquipButton");
        upgradeButton ??= FindButtonByLabel(detailRoot, "Upgrade");
        upgradeSliderRoot ??= FindDescendantByName(detailRoot, "Slider_Upgrade_01")?.gameObject;
        upgradeSliderRoot ??= FindDescendantByName(detailRoot, "Slider_Upgrade")?.gameObject;
        upgradeSliderRoot ??= FindDescendantByName(detailRoot, "Slider")?.gameObject;
        detailIconImage ??= FindImageByPath(detailRoot, "Slot/ItemFrame_01/Item/Icon");
        detailIconImage ??= FindImageByPath(detailRoot, "Slot/ItemFrame_01/Item");
        detailIconImage ??= FindImageByPath(detailRoot, "ItemFrame_01/Item/Icon");
        detailIconImage ??= FindImageByPath(detailRoot, "ItemFrame_01/Item");
        detailIconImage ??= FindImageByName(detailRoot, "Icon");
        equipButtonText ??= equipButton != null ? equipButton.GetComponentInChildren<TMP_Text>(true) : null;
        upgradeProgressSlider ??= upgradeSliderRoot != null ? FindBestUpgradeSlider(upgradeSliderRoot.transform) : null;
        upgradeProgressText ??= upgradeSliderRoot != null ? FindBestUpgradeProgressText(upgradeSliderRoot.transform) : null;
        upgradeReadyObject ??= upgradeSliderRoot != null ? FindDescendantByName(upgradeSliderRoot.transform, "Upgrade")?.gameObject : null;
        upgradeLockObject ??= upgradeSliderRoot != null ? FindDescendantByName(upgradeSliderRoot.transform, "Lock")?.gameObject : null;
    }

    private static string GetRarityLabel(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => "Common",
            EquipmentRarity.Magic => "Magic",
            EquipmentRarity.Rare => "Rare",
            EquipmentRarity.Epic => "Epic",
            EquipmentRarity.Legendary => "Legendary",
            _ => rarity.ToString()
        };
    }

    private static string BuildStatText(EquipmentDefinitionData definition)
    {
        List<string> lines = new();

        if (definition.attack != 0)
        {
            lines.Add($"ATK +{definition.attack}");
        }

        if (definition.hp != 0)
        {
            lines.Add($"HP +{definition.hp}");
        }

        if (!Mathf.Approximately(definition.critChance, 0f))
        {
            lines.Add($"Crit +{definition.critChance}%");
        }

        if (lines.Count == 0)
        {
            return "No stats";
        }

        return string.Join("\n", lines);
    }

    private void ApplyRarityLabel(EquipmentRarity rarity, bool showSelected = true)
    {
        if (rarityLabelRoot == null)
        {
            return;
        }

        SetActiveByName(rarityLabelRoot, "Rarity_Label_Common", showSelected && rarity == EquipmentRarity.Common);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Magic", showSelected && rarity == EquipmentRarity.Magic);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Rare", showSelected && rarity == EquipmentRarity.Rare);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Epic", showSelected && rarity == EquipmentRarity.Epic);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Legendary", showSelected && rarity == EquipmentRarity.Legendary);
    }

    private void ApplyDetailSlot(EquipmentDefinitionData definition)
    {
        if (detailSlotRoot == null || definition == null)
        {
            return;
        }

        SetDetailSlotFrame(detailSlotRoot, definition.rarity);
        SetSlotTypeAreaActive(true);

        Image slotItemIcon = FindImageByPath(detailSlotRoot, "ItemFrame_01/Item/Icon");
        slotItemIcon ??= FindImageByPath(detailSlotRoot, "ItemFrame_01/Item");
        slotItemIcon ??= FindImageByName(detailSlotRoot, "Icon");
        if (slotItemIcon != null)
        {
            slotItemIcon.sprite = definition.icon;
            slotItemIcon.enabled = definition.icon != null;
        }

        Image typeFrame = FindImageByPath(detailSlotRoot, "BasicFrame_Diamond_H48_NoBorder_BasePrefab");
        typeFrame ??= FindImageByPath(detailSlotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab");
        typeFrame ??= FindImageByName(detailSlotRoot, "BasicFrame_Diamond_H48_NoBorder_BasePrefab");
        if (typeFrame != null)
        {
            typeFrame.color = GetTypeFrameColor(definition.rarity);
        }

        Image typeBg = FindImageByPath(detailSlotRoot, "BasicFrame_Diamond_H48_NoBorder_BasePrefab/Bg");
        typeBg ??= FindImageByPath(detailSlotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Bg");
        if (typeBg != null)
        {
            typeBg.color = GetTypeFillColor(definition.rarity);
        }

        Image typeIcon = FindImageByPath(detailSlotRoot, "BasicFrame_Diamond_H48_NoBorder_BasePrefab/Icon");
        typeIcon ??= FindImageByPath(detailSlotRoot, "TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Icon");
        if (typeIcon != null)
        {
            typeIcon.sprite = GetCategoryIcon(definition.category);
            typeIcon.enabled = typeIcon.sprite != null;
        }

        RefreshEquipButton();
    }

    private static void SetDetailSlotFrame(Transform slotRoot, EquipmentRarity rarity)
    {
        SetFrameVariantActive(slotRoot, "Blue", "Rare", rarity == EquipmentRarity.Rare);
        SetFrameVariantActive(slotRoot, "Brown", "Common", rarity == EquipmentRarity.Common);
        SetFrameVariantActive(slotRoot, "Green", "Magic", rarity == EquipmentRarity.Magic);
        SetFrameVariantActive(slotRoot, "Plum", "Epic", rarity == EquipmentRarity.Epic);
        SetFrameVariantActive(slotRoot, "Yellow", "Legendary", rarity == EquipmentRarity.Legendary);
        SetFrameVariantActive(slotRoot, "Red", "Red", false);
    }

    private void SetSlotTypeAreaActive(bool isActive)
    {
        if (detailSlotRoot == null)
        {
            return;
        }

        SetActiveByPath(detailSlotRoot, "BasicFrame_Diamond_H48_NoBorder_BasePrefab", isActive);
        SetActiveByPath(detailSlotRoot, "TypeArea", isActive);
        SetActiveByPath(detailSlotRoot, "ItemFrame_01/Add_1", false);
        SetActiveByPath(detailSlotRoot, "ItemFrame_01/Add_2", false);
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

    private static TMP_Text FindTextByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButtonByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Image FindImageByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Image FindImageByPath(Transform root, string relativePath)
    {
        Transform target = root.Find(relativePath);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static void SetActiveByName(Transform root, string targetName, bool isActive)
    {
        Transform target = FindDescendantByName(root, targetName);
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }

    private static void SetActiveByPath(Transform root, string relativePath, bool isActive)
    {
        Transform target = root.Find(relativePath);
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }

    private static void SetFrameVariantActive(Transform slotRoot, string legacySuffix, string currentSuffix, bool isActive)
    {
        SetActiveByPath(slotRoot, $"ItemFrame_01/NormalArea/ItemFrame_01_Normal_{legacySuffix}", isActive);

        if (!string.Equals(legacySuffix, currentSuffix))
        {
            SetActiveByPath(slotRoot, $"ItemFrame_01/NormalArea/ItemFrame_01_Normal_{currentSuffix}", isActive);
        }
    }

    private static Slider FindBestUpgradeSlider(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform namedSliderRoot = FindDescendantByName(root, "Slider_02_Yellow");
        if (namedSliderRoot != null && namedSliderRoot.TryGetComponent(out Slider namedSlider))
        {
            return namedSlider;
        }

        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        if (sliders.Length == 0)
        {
            return null;
        }

        foreach (Slider slider in sliders)
        {
            if (slider != null && slider.gameObject.name.Contains("Slider"))
            {
                return slider;
            }
        }

        return sliders[0];
    }

    private static TMP_Text FindBestUpgradeProgressText(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text namedText = FindTextByName(root, "Text_Count");
        namedText ??= FindTextByName(root, "Text_Amount");
        namedText ??= FindTextByName(root, "Text_OwnedCount");
        namedText ??= FindTextByName(root, "Text (TMP)");
        if (namedText != null)
        {
            return namedText;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            if (text.text.Contains("/"))
            {
                return text;
            }
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private static bool ShouldBindEquipmentButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        return button.GetComponentInParent<EquipmentListItemView>(true) != null;
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

    private void BindEquipButton()
    {
        if (equipButton == null)
        {
            return;
        }

        equipButton.onClick.RemoveListener(HandleEquipButtonClick);
        equipButton.onClick.AddListener(HandleEquipButtonClick);
        RefreshEquipButton();
    }

    private void BindUpgradeButton()
    {
        if (upgradeButton == null)
        {
            return;
        }

        upgradeButton.onClick.RemoveListener(HandleUpgradeButtonClick);
        upgradeButton.onClick.AddListener(HandleUpgradeButtonClick);
        RefreshUpgradeButton();
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
            equipmentState.StateChanged += RefreshEquipButton;
            equipmentState.StateChanged += RefreshUpgradeButton;
            equipmentState.StateChanged += RefreshUpgradeProgress;
            subscribedState = equipmentState;
        }
    }

    private void UnsubscribeFromState()
    {
        if (subscribedState == null)
        {
            return;
        }

        subscribedState.StateChanged -= RefreshEquipButton;
        subscribedState.StateChanged -= RefreshUpgradeButton;
        subscribedState.StateChanged -= RefreshUpgradeProgress;
        subscribedState = null;
    }

    private void HandleEquipButtonClick()
    {
        ResolveReferences();

        if (equipmentState == null || currentDefinition == null)
        {
            return;
        }

        equipmentState.ToggleEquip(currentDefinition);

        EquipmentDefinitionData equippedDefinition = equipmentState.GetEquippedDefinition(currentDefinition.category);
        if (equippedDefinition == null)
        {
            CloseDetailPanel();
            return;
        }

        BindDetail(equippedDefinition, GetOwnedLevel(equippedDefinition.equipmentId));
    }

    private void HandleUpgradeButtonClick()
    {
        ResolveReferences();

        if (equipmentState == null || currentDefinition == null || runtimeData == null)
        {
            return;
        }

        if (!equipmentState.TryUpgrade(currentDefinition.equipmentId, runtimeData, out _))
        {
            return;
        }

        BindDetail(currentDefinition, GetOwnedLevel(currentDefinition.equipmentId));
    }

    private void RefreshEquipButton()
    {
        if (equipButton == null)
        {
            return;
        }

        bool canInteract = equipmentState != null &&
                           currentDefinition != null &&
                           equipmentState.HasOwnedItem(currentDefinition);

        equipButton.interactable = canInteract;

        if (equipButtonText == null)
        {
            return;
        }

        if (!canInteract)
        {
            equipButtonText.text = "Equip";
            return;
        }

        equipButtonText.text = equipmentState.IsEquippedInSlot(currentDefinition)
            ? "Unequip"
            : "Equip";
    }

    private void RefreshUpgradeButton()
    {
        if (upgradeButton == null)
        {
            return;
        }

        bool canInteract = equipmentState != null &&
                           currentDefinition != null &&
                           runtimeData != null &&
                           equipmentState.CanUpgrade(
                               currentDefinition.equipmentId,
                               runtimeData.GetGold(),
                               runtimeData.GetUpgradeStone());

        upgradeButton.interactable = canInteract;
    }

    private void RefreshUpgradeProgress()
    {
        if (upgradeSliderRoot == null || upgradeProgressText == null || upgradeProgressSlider == null)
        {
            ResolveReferences();
        }

        if (upgradeSliderRoot == null)
        {
            return;
        }

        bool hasDefinition = currentDefinition != null;
        upgradeSliderRoot.SetActive(true);

        if (!hasDefinition)
        {
            if (upgradeProgressText != null)
            {
                upgradeProgressText.gameObject.SetActive(true);
                upgradeProgressText.text = string.Empty;
            }

            if (upgradeProgressSlider != null)
            {
                upgradeProgressSlider.value = 0f;
            }

            if (upgradeReadyObject != null)
            {
                upgradeReadyObject.SetActive(false);
            }

            if (upgradeLockObject != null)
            {
                upgradeLockObject.SetActive(false);
            }

            return;
        }

        EquipmentUpgradeRequirement requirement = equipmentState != null
            ? equipmentState.GetUpgradeRequirement(currentDefinition.equipmentId)
            : new EquipmentUpgradeRequirement
            {
                equipmentId = currentDefinition.equipmentId,
                ownedCount = 0,
                requiredDuplicateCount = Mathf.Max(1, currentDefinition.requiredItemCountForNextLevel)
            };

        int ownedCount = Mathf.Max(0, requirement.ownedCount);
        int requiredCount = Mathf.Max(1, requirement.requiredDuplicateCount);
        float progress = requiredCount > 0 ? (float)ownedCount / requiredCount : 0f;
        bool isReadyToUpgrade = progress >= 1f;

        if (upgradeProgressText != null)
        {
            upgradeProgressText.gameObject.SetActive(true);
            upgradeProgressText.text = $"{ownedCount}/{requiredCount}";
        }

        if (upgradeProgressSlider != null)
        {
            upgradeProgressSlider.value = Mathf.Clamp01(progress);
        }

        if (upgradeReadyObject != null)
        {
            upgradeReadyObject.SetActive(isReadyToUpgrade);
        }

        if (upgradeLockObject != null)
        {
            upgradeLockObject.SetActive(!isReadyToUpgrade);
        }
    }

    private int GetOwnedLevel(string equipmentId)
    {
        if (equipmentState == null || string.IsNullOrWhiteSpace(equipmentId))
        {
            return Mathf.Max(1, currentLevel);
        }

        int ownedLevel = equipmentState.GetOwnedLevel(equipmentId);
        return ownedLevel > 0 ? ownedLevel : Mathf.Max(1, currentLevel);
    }

    private static Button FindButtonByLabel(Transform root, string targetLabel)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetLabel))
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && string.Equals(label.text, targetLabel, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }
}

public class EquipmentDetailPanelItemButton : MonoBehaviour
{
    [SerializeField] private EquipmentDetailPanelController controller;
    [SerializeField] private EquipmentListItemView itemView;
    [SerializeField] private Button targetButton;

    private bool isBound;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    public void Configure(EquipmentDetailPanelController detailController, EquipmentListItemView view, Button button)
    {
        controller = detailController;
        itemView = view;
        targetButton = button;
        BindButton(true);
    }

    private void HandleClick()
    {
        controller?.OpenDetailPanel(itemView);
    }

    private void BindButton(bool forceRebind = false)
    {
        targetButton ??= GetComponent<Button>();

        if (targetButton == null)
        {
            return;
        }

        if (forceRebind || isBound)
        {
            targetButton.onClick.RemoveListener(HandleClick);
            isBound = false;
        }

        if (isBound)
        {
            return;
        }

        targetButton.onClick.AddListener(HandleClick);
        isBound = true;
    }
}
