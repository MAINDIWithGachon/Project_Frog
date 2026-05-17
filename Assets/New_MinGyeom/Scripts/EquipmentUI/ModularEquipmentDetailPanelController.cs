using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Module-local copy of the old equipment detail controller.
/// It keeps the existing detail popup UI, but exposes definition-based entry points.
/// </summary>
public class ModularEquipmentDetailPanelController : MonoBehaviour
{
    [Header("Injected References")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private RuntimeData runtimeData;

    [Header("Auto Bound UI")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text equipButtonText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject upgradeButtonGreenBackground;
    [SerializeField] private GameObject upgradeButtonGrayBackground;
    [SerializeField] private GameObject upgradeSliderRoot;
    [SerializeField] private Slider upgradeProgressSlider;
    [SerializeField] private TMP_Text upgradeProgressText;
    [SerializeField] private TMP_Text insufficientGoldTargetText;
    [SerializeField] private GameObject upgradeReadyObject;
    [SerializeField] private GameObject upgradeLockObject;
    [SerializeField] private Button[] closeButtons;
    [SerializeField] private Graphic[] closeClickAreas;

    [Header("Detail UI")]
    [SerializeField] private Image detailIconImage;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailRarityText;
    [SerializeField] private TMP_Text detailLevelText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailStatText;
    [SerializeField] private Transform detailStatListRoot;
    [SerializeField] private Transform rarityLabelRoot;
    [SerializeField] private Transform detailSlotRoot;

    [Header("Style")]
    [SerializeField] private Color insufficientCurrencyColor = Color.red;

    private EquipmentDefinitionData currentDefinition;
    private int currentLevel;
    private EquipmentPrototypeState subscribedState;
    private bool buttonsBound;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        SubscribeToState();
        RefreshCurrentDetail();
    }

    private void OnDisable()
    {
        UnsubscribeFromState();
    }

    public void Configure(GameObject panel, EquipmentPrototypeState state, RuntimeData data)
    {
        detailPanel = panel;
        equipmentState = state;
        runtimeData = data;

        ResolveReferences();
        BindButtons();
        SubscribeToState();
        RefreshCurrentDetail();
    }

    public void OpenDetail(EquipmentDefinitionData definition, int level)
    {
        if (definition == null)
        {
            return;
        }

        currentDefinition = definition;
        currentLevel = Mathf.Max(1, level);
        RefreshCurrentDetail();

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }
    }

    public void OpenDetail(EquipmentCategory category)
    {
        if (equipmentState == null)
        {
            return;
        }

        EquipmentDefinitionData definition = equipmentState.GetEquippedDefinition(category);
        if (definition == null)
        {
            return;
        }

        OpenDetail(definition, equipmentState.GetOwnedLevel(definition.equipmentId));
    }

    public void Close()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    public void RefreshCurrentDetail()
    {
        ResolveReferences();

        if (currentDefinition == null)
        {
            RefreshButtons();
            RefreshUpgradeProgress();
            return;
        }

        int level = GetOwnedLevel(currentDefinition.equipmentId);
        currentLevel = Mathf.Max(1, level);

        if (detailIconImage != null)
        {
            detailIconImage.sprite = currentDefinition.uiIcon;
            detailIconImage.enabled = currentDefinition.uiIcon != null;
        }

        if (detailNameText != null)
        {
            detailNameText.text = currentDefinition.displayName;
        }

        if (detailRarityText != null)
        {
            detailRarityText.text = GetRarityLabel(currentDefinition.rarity);
        }

        if (detailLevelText != null)
        {
            detailLevelText.text = $"Lv.{currentLevel}";
        }

        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = currentDefinition.description;
        }

        ApplyStats(currentDefinition);
        ApplyRarityLabel(currentDefinition.rarity);
        ApplyDetailSlot(currentDefinition);
        RefreshButtons();
        RefreshUpgradeProgress();
    }

    private void ResolveReferences()
    {
        Transform root = detailPanel != null ? detailPanel.transform : transform;

        detailNameText ??= FindTextByName(root, "Text_ItemName");
        detailLevelText ??= FindTextByName(root, "Text_Level");
        detailDescriptionText ??= FindTextByName(root, "Text_Description");
        detailStatText ??= FindTextByName(root, "Text_GearStats");
        detailStatText ??= FindTextByName(root, "Text_Buff");
        detailRarityText ??= FindTextByName(root, "Text_Rarity");
        detailStatListRoot ??= FindDescendantByName(root, "Group_Buff");
        rarityLabelRoot ??= FindDescendantByName(root, "Rarity_Label");
        detailSlotRoot ??= FindDescendantByName(root, "Slot");

        equipButton ??= FindButtonByName(root, "EquipButton");
        upgradeButton ??= FindButtonByName(root, "UpgradeButton");
        upgradeButton ??= FindButtonByLabel(root, "Upgrade");

        upgradeButtonGreenBackground ??= FindDescendantByName(root, "Button_02_Green")?.gameObject;
        upgradeButtonGrayBackground ??= FindDescendantByName(root, "Button_02_White")?.gameObject;
        upgradeSliderRoot ??= FindDescendantByName(root, "Slider_Upgrade_01")?.gameObject;
        upgradeSliderRoot ??= FindDescendantByName(root, "UpgradeSlider")?.gameObject;

        detailIconImage ??= FindImageByPath(root, "Slot/ItemFrame_01/Item/Icon");
        detailIconImage ??= FindImageByPath(root, "Slot/ItemFrame_01/Item");
        detailIconImage ??= FindImageByName(root, "Icon");

        equipButtonText ??= equipButton != null ? equipButton.GetComponentInChildren<TMP_Text>(true) : null;
        upgradeProgressSlider ??= upgradeSliderRoot != null ? FindBestUpgradeSlider(upgradeSliderRoot.transform) : null;
        upgradeProgressText ??= upgradeSliderRoot != null ? FindBestUpgradeProgressText(upgradeSliderRoot.transform) : null;
        insufficientGoldTargetText ??= FindTextByName(root, "Text_Amount");
        upgradeReadyObject ??= FindDescendantByName(upgradeSliderRoot != null ? upgradeSliderRoot.transform : root, "Upgrade")?.gameObject;
        upgradeLockObject ??= FindDescendantByName(upgradeSliderRoot != null ? upgradeSliderRoot.transform : root, "Lock")?.gameObject;

        if (closeButtons == null || closeButtons.Length == 0)
        {
            closeButtons = FindCloseButtons(root);
        }
    }

    private void BindButtons()
    {
        if (buttonsBound)
        {
            return;
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(HandleEquipClicked);
            equipButton.onClick.AddListener(HandleEquipClicked);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
            upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }

        if (closeButtons != null)
        {
            for (int i = 0; i < closeButtons.Length; i++)
            {
                Button closeButton = closeButtons[i];
                if (closeButton == null)
                {
                    continue;
                }

                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        if (closeClickAreas != null)
        {
            for (int i = 0; i < closeClickAreas.Length; i++)
            {
                Graphic area = closeClickAreas[i];
                if (area == null)
                {
                    continue;
                }

                Button button = area.GetComponent<Button>();
                if (button == null)
                {
                    button = area.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                }

                button.onClick.RemoveListener(Close);
                button.onClick.AddListener(Close);
            }
        }

        buttonsBound = true;
    }

    private void HandleEquipClicked()
    {
        if (equipmentState == null || currentDefinition == null)
        {
            return;
        }

        equipmentState.ToggleEquip(currentDefinition);
        RefreshCurrentDetail();
    }

    private void HandleUpgradeClicked()
    {
        if (equipmentState == null || runtimeData == null || currentDefinition == null)
        {
            return;
        }

        if (equipmentState.TryUpgrade(currentDefinition.equipmentId, runtimeData, out _))
        {
            RefreshCurrentDetail();
        }
    }

    private void RefreshButtons()
    {
        bool hasOwnedCurrent = equipmentState != null &&
                               currentDefinition != null &&
                               equipmentState.HasOwnedItem(currentDefinition);

        if (equipButton != null)
        {
            equipButton.interactable = hasOwnedCurrent;
        }

        if (equipButtonText != null)
        {
            equipButtonText.text = hasOwnedCurrent && equipmentState.IsEquippedInSlot(currentDefinition)
                ? "Unequip"
                : "Equip";
        }

        EquipmentUpgradeRequirement requirement = GetCurrentUpgradeRequirement();
        bool canUpgrade = requirement != null && requirement.CanUpgrade;

        if (upgradeButton != null)
        {
            upgradeButton.interactable = canUpgrade;
        }

        if (upgradeButtonGreenBackground != null)
        {
            upgradeButtonGreenBackground.SetActive(true);
        }

        if (upgradeButtonGrayBackground != null)
        {
            upgradeButtonGrayBackground.SetActive(requirement != null && !requirement.CanUpgrade);
        }
    }

    private void RefreshUpgradeProgress()
    {
        EquipmentUpgradeRequirement requirement = GetCurrentUpgradeRequirement();

        if (upgradeSliderRoot != null)
        {
            upgradeSliderRoot.SetActive(currentDefinition != null);
        }

        if (requirement == null)
        {
            if (upgradeProgressText != null)
            {
                upgradeProgressText.text = string.Empty;
            }

            if (upgradeProgressSlider != null)
            {
                upgradeProgressSlider.value = 0f;
            }

            return;
        }

        int ownedCount = Mathf.Max(0, requirement.ownedCount);
        int requiredCount = Mathf.Max(1, requirement.requiredDuplicateCount);
        bool duplicateReady = ownedCount >= requiredCount;

        if (upgradeProgressText != null)
        {
            upgradeProgressText.text = $"{ownedCount}/{requiredCount}";
        }

        if (upgradeProgressSlider != null)
        {
            upgradeProgressSlider.value = Mathf.Clamp01((float)ownedCount / requiredCount);
        }

        if (insufficientGoldTargetText != null)
        {
            insufficientGoldTargetText.text = requirement.requiredGold.ToString();
            insufficientGoldTargetText.color = requirement.hasEnoughGold ? Color.white : insufficientCurrencyColor;
        }

        if (upgradeReadyObject != null)
        {
            upgradeReadyObject.SetActive(duplicateReady);
        }

        if (upgradeLockObject != null)
        {
            upgradeLockObject.SetActive(!duplicateReady);
        }
    }

    private EquipmentUpgradeRequirement GetCurrentUpgradeRequirement()
    {
        if (equipmentState == null || currentDefinition == null)
        {
            return null;
        }

        return equipmentState.GetUpgradeRequirement(
            currentDefinition.equipmentId,
            runtimeData != null ? runtimeData.GetGold() : int.MaxValue,
            runtimeData != null ? runtimeData.GetUpgradeStone() : int.MaxValue);
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

    private void ApplyStats(EquipmentDefinitionData definition)
    {
        List<string> lines = BuildStatLines(definition);

        if (detailStatText != null)
        {
            detailStatText.text = string.Join("\n", lines);
        }

        if (detailStatListRoot == null)
        {
            return;
        }

        TMP_Text[] texts = detailStatListRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null)
            {
                text.text = i < lines.Count ? lines[i] : string.Empty;
            }
        }
    }

    private void ApplyRarityLabel(EquipmentRarity rarity)
    {
        if (rarityLabelRoot == null)
        {
            return;
        }

        SetActiveByName(rarityLabelRoot, "Rarity_Label_Common", rarity == EquipmentRarity.Common);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Magic", rarity == EquipmentRarity.Magic);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Rare", rarity == EquipmentRarity.Rare);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Epic", rarity == EquipmentRarity.Epic);
        SetActiveByName(rarityLabelRoot, "Rarity_Label_Legendary", rarity == EquipmentRarity.Legendary);
    }

    private void ApplyDetailSlot(EquipmentDefinitionData definition)
    {
        if (detailSlotRoot == null || definition == null)
        {
            return;
        }

        SetFrame(detailSlotRoot, definition.rarity);

        Image icon = FindImageByPath(detailSlotRoot, "ItemFrame_01/Item/Icon");
        icon ??= FindImageByPath(detailSlotRoot, "ItemFrame_01/Item");
        icon ??= FindImageByName(detailSlotRoot, "Icon");

        if (icon != null)
        {
            icon.sprite = definition.uiIcon;
            icon.enabled = definition.uiIcon != null;
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
            equipmentState.StateChanged += RefreshCurrentDetail;
            subscribedState = equipmentState;
        }
    }

    private void UnsubscribeFromState()
    {
        if (subscribedState == null)
        {
            return;
        }

        subscribedState.StateChanged -= RefreshCurrentDetail;
        subscribedState = null;
    }

    private static List<string> BuildStatLines(EquipmentDefinitionData definition)
    {
        List<string> lines = new();
        if (definition == null)
        {
            return lines;
        }

        if (definition.attack != 0) lines.Add($"ATK +{definition.attack}");
        if (definition.hp != 0) lines.Add($"HP +{definition.hp}");
        if (!Mathf.Approximately(definition.healPerSec, 0f)) lines.Add($"HPS +{definition.healPerSec}");
        if (!Mathf.Approximately(definition.critChance, 0f)) lines.Add($"Crit +{definition.critChance}%");
        if (!Mathf.Approximately(definition.critDamage, 0f)) lines.Add($"Crit DMG +{definition.critDamage}%");
        return lines;
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

    private static void SetFrame(Transform root, EquipmentRarity rarity)
    {
        SetActiveByNameContains(root, "Normal_Rare", rarity == EquipmentRarity.Rare);
        SetActiveByNameContains(root, "Normal_Blue", rarity == EquipmentRarity.Rare);
        SetActiveByNameContains(root, "Normal_Common", rarity == EquipmentRarity.Common);
        SetActiveByNameContains(root, "Normal_Brown", rarity == EquipmentRarity.Common);
        SetActiveByNameContains(root, "Normal_Magic", rarity == EquipmentRarity.Magic);
        SetActiveByNameContains(root, "Normal_Green", rarity == EquipmentRarity.Magic);
        SetActiveByNameContains(root, "Normal_Epic", rarity == EquipmentRarity.Epic);
        SetActiveByNameContains(root, "Normal_Plum", rarity == EquipmentRarity.Epic);
        SetActiveByNameContains(root, "Normal_Legendary", rarity == EquipmentRarity.Legendary);
        SetActiveByNameContains(root, "Normal_Yellow", rarity == EquipmentRarity.Legendary);
    }

    private static Button[] FindCloseButtons(Transform root)
    {
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

    private static Slider FindBestUpgradeSlider(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        return sliders.Length > 0 ? sliders[0] : null;
    }

    private static TMP_Text FindBestUpgradeProgressText(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text text = FindTextByName(root, "Text_Count");
        text ??= FindTextByName(root, "Text_Amount");
        if (text != null)
        {
            return text;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private static Button FindButtonByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Button FindButtonByLabel(Transform root, string targetLabel)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null && label.text.Equals(targetLabel, System.StringComparison.OrdinalIgnoreCase))
            {
                return button;
            }
        }

        return null;
    }

    private static TMP_Text FindTextByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Image FindImageByName(Transform root, string targetName)
    {
        Transform target = FindDescendantByName(root, targetName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Image FindImageByPath(Transform root, string relativePath)
    {
        Transform target = root != null ? root.Find(relativePath) : null;
        return target != null ? target.GetComponent<Image>() : null;
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

    private static void SetActiveByName(Transform root, string targetName, bool active)
    {
        Transform target = FindDescendantByName(root, targetName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static void SetActiveByNameContains(Transform root, string namePart, bool active)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name.Contains(namePart))
            {
                child.gameObject.SetActive(active);
            }
        }
    }
}
