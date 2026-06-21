using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class RecipeDetailPopup : MonoBehaviour
{
    [Serializable]
    private class RarityFrameBinding
    {
        public RecipeRarity rarity;
        public GameObject target;
    }

    [SerializeField] private bool autoWireChildReferences = true;

    [Header("UI")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image frameBackgroundImage;
    [SerializeField] private GameObject defaultFrame;
    [SerializeField] private RarityFrameBinding[] rarityFrames;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text passiveText;
    [SerializeField] private TMP_Text duplicateText;
    [SerializeField] private GameObject ownedProgressRoot;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private RectTransform progressFillRect;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject upgradeReadyObject;
    [SerializeField] private Image upgradeReadyImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject grayUpgradeButtonObject;
    [SerializeField] private RuntimeData runtimeData;
    public int upgradeGoldCost = 3000;
    [SerializeField] private GameObject upgradeGoldCostRoot;
    [SerializeField] private TMP_Text upgradeGoldCostText;
    [SerializeField] private TMP_Text upgradeGoldCostRedText;
    [SerializeField] private TMP_FontAsset fallbackFontAsset;
    [SerializeField] private bool autoApplyFallbackFont = true;
    [SerializeField] private Color progressNormalColor = new Color32(255, 128, 0, 255);
    [SerializeField] private Color progressReadyColor = new Color32(33, 224, 122, 255);
    [SerializeField] private Color affordableGoldColor = Color.white;
    [SerializeField] private Color insufficientGoldColor = new Color32(255, 76, 76, 255);

    [Header("Localization")]
    [SerializeField] private LocalizationEntry unknownRecipeText = new LocalizationEntry("UI", "ui.recipe.unknown");
    [SerializeField] private LocalizationEntry notOwnedText = new LocalizationEntry("UI", "ui.recipe.not_owned");
    [SerializeField] private LocalizationEntry lockedDescriptionText = new LocalizationEntry("UI", "ui.recipe.locked_description");
    [SerializeField] private LocalizationEntry passiveFormatText = new LocalizationEntry("UI", "ui.recipe.passive_format");
    [SerializeField] private LocalizationEntry duplicateProgressFormatText = new LocalizationEntry("UI", "ui.recipe.duplicate_progress_format");
    [SerializeField] private LocalizationEntry duplicateProgressMaxText = new LocalizationEntry("UI", "ui.recipe.duplicate_progress_max");
    [SerializeField] private LocalizationEntry upgradeButtonText = new LocalizationEntry("UI", "ui.upgrade");
    [SerializeField] private LocalizationEntry maxText = new LocalizationEntry("UI", "ui.common.max");

    [Header("Rarity Frame Colors")]
    [SerializeField] private Color commonFrameColor = new Color32(168, 101, 57, 255);
    [SerializeField] private Color magicFrameColor = new Color32(72, 205, 99, 255);
    [SerializeField] private Color rareFrameColor = new Color32(53, 160, 213, 255);
    [SerializeField] private Color epicFrameColor = new Color32(178, 78, 221, 255);
    [SerializeField] private Color legendaryFrameColor = new Color32(255, 211, 49, 255);

    private Image closeButtonRaycastImage;
    private RectTransform closeButtonRect;
    private RecipeDefinitionData currentDefinition;
    private RecipeOwnedState currentOwnedState;
    private RecipeInventoryState recipeState;

    private void Awake()
    {
        AutoWireChildReferences();
        ApplyFallbackFontIfNeeded();
        RegisterCloseButton();
        RegisterUpgradeButton();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnregisterCloseButton();
        UnregisterUpgradeButton();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (WasCloseAreaReleased())
        {
            Close();
        }
    }

    private void OnValidate()
    {
        AutoWireChildReferences();
    }

    public void Open(RecipeDefinitionData definition, RecipeOwnedState ownedState)
    {
        Open(definition, ownedState, recipeState);
    }

    public void Open(RecipeDefinitionData definition, RecipeOwnedState ownedState, RecipeInventoryState state)
    {
        AutoWireChildReferences();
        ApplyFallbackFontIfNeeded();
        RegisterCloseButton();
        RegisterUpgradeButton();

        if (definition == null)
        {
            Close();
            return;
        }

        definition.ApplyRarityFromRecipeId();
        ApplyRarityFrame(definition.rarity);

        currentDefinition = definition;
        recipeState = state != null ? state : recipeState;

        if (recipeState != null)
        {
            ownedState = recipeState.GetOwnedState(definition.recipeId);
        }

        currentOwnedState = ownedState;

        bool isOwned = ownedState != null;
        int currentLevel = isOwned ? Mathf.Max(1, ownedState.currentLevel) : 0;
        int maxLevel = Mathf.Max(1, definition.maxLevel);
        bool isMaxLevel = isOwned && currentLevel >= maxLevel;
        int currentShardCount = isOwned ? Mathf.Max(0, ownedState.ownedShardCount) : 0;
        int requiredShardCount = isMaxLevel ? 1 : definition.GetRequiredShardCountForLevel(Mathf.Max(1, currentLevel));
        bool hasEnoughShards = isOwned && !isMaxLevel && currentShardCount >= requiredShardCount;
        bool hasEnoughGold = HasEnoughUpgradeGold();
        bool canUpgrade = hasEnoughShards && hasEnoughGold;
        float progressRate = isOwned
            ? isMaxLevel ? 1f : Mathf.Clamp01((float)currentShardCount / requiredShardCount)
            : 0f;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = definition.uiIcon;
            itemIconImage.enabled = definition.uiIcon != null;
            itemIconImage.color = isOwned ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.8f);
        }

        SetText(rarityText, definition.rarity.ToString().ToUpperInvariant());
        SetText(itemNameText, isOwned
            ? definition.GetLocalizedName()
            : LocalizationLookup.GetLocalizedString(unknownRecipeText));
        SetText(levelText, isOwned
            ? $"Lv.{currentLevel}"
            : LocalizationLookup.GetLocalizedString(notOwnedText));
        SetText(descriptionText, isOwned
            ? definition.GetLocalizedDescription()
            : LocalizationLookup.GetLocalizedString(lockedDescriptionText));

        string passiveLabel = LocalizationLookup.GetLocalizedString(GetPassiveLabelEntry(definition.passiveType));
        SetText(passiveText, LocalizationLookup.GetLocalizedString(
            passiveFormatText,
            passiveLabel,
            FormatPercent(definition.passiveValue)));

        SetText(duplicateText, isMaxLevel
            ? LocalizationLookup.GetLocalizedString(duplicateProgressMaxText)
            : LocalizationLookup.GetLocalizedString(
                duplicateProgressFormatText,
                currentShardCount,
                requiredShardCount));

        if (ownedProgressRoot != null)
        {
            ownedProgressRoot.SetActive(isOwned);
        }

        if (progressText != null)
        {
            progressText.text = isOwned
                ? isMaxLevel ? LocalizationLookup.GetLocalizedString(maxText) : $"{currentShardCount} / {requiredShardCount}"
                : string.Empty;
        }

        UpdateProgressTexts(isOwned
            ? isMaxLevel ? LocalizationLookup.GetLocalizedString(maxText) : $"{currentShardCount} / {requiredShardCount}"
            : string.Empty);

        if (progressFillRect != null)
        {
            EnsureLeftPivot(progressFillRect);
            Vector3 scale = progressFillRect.localScale;
            scale.x = progressRate;
            progressFillRect.localScale = scale;
        }

        ApplyProgressColor(hasEnoughShards || isMaxLevel);
        ApplyUpgradeReadyVisual(hasEnoughShards || isMaxLevel);
        ApplyUpgradeGoldCostVisual(hasEnoughGold);
        ApplyUpgradeButtonVisual(canUpgrade);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        Debug.Log("[RecipeDetailPopup] Close button clicked.", this);
        gameObject.SetActive(false);
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (currentDefinition == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        RecipeOwnedState ownedState = recipeState != null
            ? recipeState.GetOwnedState(currentDefinition.recipeId)
            : currentOwnedState;
        Open(currentDefinition, ownedState, recipeState);
    }

    private void UpdateProgressTexts(string value)
    {
        if (ownedProgressRoot == null)
        {
            return;
        }

        TMP_Text[] texts = ownedProgressRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            text.text = value;
        }
    }

    private string FormatPercent(float value)
    {
        return value >= 0f ? $"+{value:0.##}%" : $"{value:0.##}%";
    }

    private void ApplyProgressColor(bool isReady)
    {
        Color color = isReady ? progressReadyColor : GetNormalProgressColor();

        if (progressFillImage != null)
        {
            progressFillImage.color = color;
        }

        if (progressFillRect == null)
        {
            return;
        }

        Image[] fillImages = progressFillRect.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < fillImages.Length; i++)
        {
            if (fillImages[i] != null)
            {
                fillImages[i].color = color;
            }
        }
    }

    private void ApplyUpgradeReadyVisual(bool isReady)
    {
        if (upgradeReadyObject != null)
        {
            upgradeReadyObject.SetActive(isReady);
        }

        if (upgradeReadyImage != null)
        {
            upgradeReadyImage.enabled = isReady;
            upgradeReadyImage.color = progressReadyColor;
        }
    }

    private void ApplyFrameColor(RecipeRarity rarity)
    {
        if (frameBackgroundImage != null)
        {
            frameBackgroundImage.color = GetFrameColor(rarity);
        }
    }

    private void ApplyRarityFrame(RecipeRarity? rarity)
    {
        bool matched = false;

        GameObject activeFrame = null;

        if (rarityFrames != null)
        {
            for (int i = 0; i < rarityFrames.Length; i++)
            {
                RarityFrameBinding binding = rarityFrames[i];
                if (binding == null || binding.target == null)
                {
                    continue;
                }

                bool isMatch = rarity.HasValue && binding.rarity == rarity.Value;
                binding.target.SetActive(isMatch);
                matched |= isMatch;
                activeFrame = isMatch ? binding.target : activeFrame;
            }
        }

        if (defaultFrame != null)
        {
            defaultFrame.SetActive(!rarity.HasValue || !matched);
            activeFrame = !rarity.HasValue || !matched ? defaultFrame : activeFrame;
        }

        if (rarity.HasValue)
        {
            ApplyFrameColor(rarity.Value);
            if (!matched)
            {
                RecipeRarityFramePalette.Apply(activeFrame, rarity.Value);
            }
        }
    }

    private Color GetFrameColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return RecipeRarityFramePalette.GetFillColor(rarity);
            case RecipeRarity.Rare:
                return RecipeRarityFramePalette.GetFillColor(rarity);
            case RecipeRarity.Epic:
                return RecipeRarityFramePalette.GetFillColor(rarity);
            case RecipeRarity.Legendary:
                return RecipeRarityFramePalette.GetFillColor(rarity);
            default:
                return RecipeRarityFramePalette.GetFillColor(rarity);
        }
    }

    private void EnsureLeftPivot(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        Vector2 pivot = target.pivot;
        if (!Mathf.Approximately(pivot.x, 0f))
        {
            pivot.x = 0f;
            target.pivot = pivot;
        }
    }

    private Color GetNormalProgressColor()
    {
        bool isSerializedWhite =
            progressNormalColor.r > 0.95f &&
            progressNormalColor.g > 0.95f &&
            progressNormalColor.b > 0.95f;

        bool isSerializedReadyGreen =
            progressNormalColor.r < 0.35f &&
            progressNormalColor.g > 0.75f &&
            progressNormalColor.b < 0.55f;

        return isSerializedWhite || isSerializedReadyGreen
            ? new Color32(255, 128, 0, 255)
            : progressNormalColor;
    }

    private LocalizationEntry GetPassiveLabelEntry(RecipePassiveType passiveType)
    {
        switch (passiveType)
        {
            case RecipePassiveType.AttackPercent:
                return new LocalizationEntry("SkillStat", "skill_stat.attack");
            case RecipePassiveType.MaxHpPercent:
                return new LocalizationEntry("SkillStat", "skill_stat.max_hp");
            case RecipePassiveType.HealPercent:
                return new LocalizationEntry("SkillStat", "skill_stat.heal");
            case RecipePassiveType.GoldGainPercent:
                return new LocalizationEntry("SkillStat", "skill_stat.gold_gain");
            case RecipePassiveType.SkillCooldownPercent:
                return new LocalizationEntry("SkillStat", "skill_stat.skill_cooldown");
            case RecipePassiveType.BossDamagePercent:
                return new LocalizationEntry("SkillStat", "skill_stat.boss_damage");
            default:
                return new LocalizationEntry("SkillStat", $"skill_stat.{passiveType.ToString().ToLowerInvariant()}");
        }
    }

    [ContextMenu("Auto Wire Child References")]
    private void AutoWireChildReferences()
    {
        if (!autoWireChildReferences)
        {
            return;
        }

        itemIconImage = FindImage("Popup/ItemFrame_01/Item") ?? itemIconImage;
        frameBackgroundImage = FindImage("Popup/ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brown/Bg")
            ?? FindImage("Popup/ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brow/Bg")
            ?? FindImage("Popup/ItemFrame_01/NormalArea/Bg")
            ?? FindImage("Popup/ItemFrame_01/Bg")
            ?? frameBackgroundImage;
        defaultFrame = FindChild("Popup/ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brown")?.gameObject
            ?? FindChild("Popup/ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brow")?.gameObject
            ?? defaultFrame;
        AutoWireRarityFrames("Popup/ItemFrame_01/NormalArea/");
        Transform rarityLabel = FindChild("Popup/Label_Tapered_01_Brown");
        rarityText = FindText("Popup/Label_Tapered_01_Brown/Text (TMP)")
            ?? FindText("Popup/Label_Tapered_01_Brown/Text")
            ?? FindFirstTextIn(rarityLabel)
            ?? rarityText;
        itemNameText = FindText("Popup/Text_ItemName") ?? itemNameText;
        levelText = FindText("Popup/Text_Level") ?? levelText;
        descriptionText = FindText("Popup/DescriptionScrollView/Viewport/Content/Text_Description (1)")
            ?? FindText("Popup/DescriptionScrollView/Viewport/Content/Text_Description")
            ?? FindText("Popup/Text_Description")
            ?? descriptionText;
        passiveText = FindText("Popup/DescriptionScrollView/Viewport/Content/Group_Buff (1)/Text_Buff")
            ?? FindText("Popup/DescriptionScrollView/Viewport/Content/Group_Buff/Text_Buff")
            ?? FindText("Popup/Group_Buff/Text_Buff")
            ?? passiveText;
        duplicateText = FindText("Popup/DescriptionScrollView/Viewport/Content/Group_Buff (1)/Text_Buff (1)")
            ?? FindText("Popup/DescriptionScrollView/Viewport/Content/Group_Buff/Text_Buff (1)")
            ?? FindText("Popup/Group_Buff/Text_Buff (1)")
            ?? duplicateText;
        Transform progressRoot = FindChild("Popup/Slider_Upgrade_01");
        ownedProgressRoot = progressRoot != null ? progressRoot.gameObject : ownedProgressRoot;
        progressFillRect = FindChild("Popup/Slider_Upgrade_01/Slider_02_Orange/Fill Area/Fill") as RectTransform
            ?? FindChild("Popup/Slider_Upgrade_01/Slider_02_Yellow/Fill Area/Fill") as RectTransform
            ?? progressFillRect;
        progressFillImage = FindImage("Popup/Slider_Upgrade_01/Slider_02_Orange/Fill Area/Fill")
            ?? FindImage("Popup/Slider_Upgrade_01/Slider_02_Yellow/Fill Area/Fill")
            ?? progressFillImage;
        progressText = FindText("Popup/Slider_Upgrade_01/Slider_02_Orange/Text (TMP)")
            ?? FindText("Popup/Slider_Upgrade_01/Slider_02_Yellow/Text (TMP)")
            ?? FindText("Popup/Slider_Upgrade_01/Text (TMP)")
            ?? progressText;
        Transform upgradeReadyTransform = FindChild("Popup/Slider_Upgrade_01/Upgrade");
        upgradeReadyObject = upgradeReadyTransform != null ? upgradeReadyTransform.gameObject : upgradeReadyObject;
        upgradeReadyImage = upgradeReadyTransform != null ? upgradeReadyTransform.GetComponent<Image>() : upgradeReadyImage;
        closeButton = FindButton("Popup/Button_Close_01")
            ?? FindButton("Button_Close_01")
            ?? closeButton;
        upgradeButton = FindButton("Popup/Button_02_Green")
            ?? FindButton("Button_02_Green")
            ?? upgradeButton;
        grayUpgradeButtonObject = FindChild("Popup/Button_02_Gray")?.gameObject
            ?? FindChild("Button_02_Gray")?.gameObject
            ?? grayUpgradeButtonObject;
        Transform popupRoot = FindChild("Popup");
        upgradeGoldCostRoot = FindChild("Popup/UpgradeGoldCost")?.gameObject
            ?? FindChild("Popup/UpgradeCost")?.gameObject
            ?? FindChild("Popup/GoldCost")?.gameObject
            ?? FindChild("ResourceBar_Coin")?.gameObject
            ?? FindChild("UpgradeGoldCost")?.gameObject
            ?? FindChild("UpgradeCost")?.gameObject
            ?? FindChild("GoldCost")?.gameObject
            ?? FindFirstChildByNameToken(transform, "ResourceBar_Coin")?.gameObject
            ?? FindFirstChildByNameToken(popupRoot, "Gold")?.gameObject
            ?? FindFirstChildByNameToken(popupRoot, "Cost")?.gameObject
            ?? FindFirstChildByNameToken(popupRoot, "Coin")?.gameObject
            ?? FindFirstChildByNameToken(popupRoot, "Price")?.gameObject
            ?? FindFirstChildByNameToken(transform, "Gold")?.gameObject
            ?? FindFirstChildByNameToken(transform, "Cost")?.gameObject
            ?? FindFirstChildByNameToken(transform, "Coin")?.gameObject
            ?? FindFirstChildByNameToken(transform, "Price")?.gameObject
            ?? upgradeGoldCostRoot;
        upgradeGoldCostText = FindText("Popup/UpgradeGoldCost/Text (TMP)")
            ?? FindText("Popup/UpgradeGoldCost/Text")
            ?? FindText("Popup/UpgradeCost/Text (TMP)")
            ?? FindText("Popup/UpgradeCost/Text")
            ?? FindText("Popup/GoldCost/Text (TMP)")
            ?? FindText("Popup/GoldCost/Text")
            ?? FindText("ResourceBar_Coin/Text (TMP)")
            ?? FindText("ResourceBar_Coin/Text")
            ?? FindFirstTextIn(upgradeGoldCostRoot != null ? upgradeGoldCostRoot.transform : null)
            ?? FindFirstTextByNameToken(popupRoot, "Gold")
            ?? FindFirstTextByNameToken(popupRoot, "Cost")
            ?? FindFirstTextByNameToken(popupRoot, "Coin")
            ?? FindFirstTextByNameToken(popupRoot, "Price")
            ?? FindFirstTextByNameToken(transform, "Gold")
            ?? FindFirstTextByNameToken(transform, "Cost")
            ?? FindFirstTextByNameToken(transform, "Coin")
            ?? FindFirstTextByNameToken(transform, "Price")
            ?? upgradeGoldCostText;
        upgradeGoldCostRedText = FindText("Popup/UpgradeGoldCost/Text_Red (TMP)")
            ?? FindText("Popup/UpgradeGoldCost/Text_Red")
            ?? FindText("Popup/UpgradeCost/Text_Red (TMP)")
            ?? FindText("Popup/UpgradeCost/Text_Red")
            ?? FindText("Popup/GoldCost/Text_Red (TMP)")
            ?? FindText("Popup/GoldCost/Text_Red")
            ?? FindText("ResourceBar_Coin/Text_Red (TMP)")
            ?? FindText("ResourceBar_Coin/Text_Red")
            ?? FindFirstTextByExactName(upgradeGoldCostRoot != null ? upgradeGoldCostRoot.transform : null, "Text_Red")
            ?? FindFirstTextByExactName(transform, "Text_Red")
            ?? upgradeGoldCostRedText;

        closeButtonRaycastImage = closeButton != null
            ? closeButton.GetComponent<Image>()
            : closeButtonRaycastImage;
        closeButtonRect = closeButton != null
            ? closeButton.GetComponent<RectTransform>()
            : closeButtonRect;
    }

    private Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    private void AutoWireRarityFrames(string rootPath)
    {
        rarityFrames = new[]
        {
            CreateFrameBinding(RecipeRarity.Magic, rootPath + "ItemFrame_01_Normal_Green"),
            CreateFrameBinding(RecipeRarity.Rare, rootPath + "ItemFrame_01_Normal_Blue"),
            CreateFrameBinding(RecipeRarity.Epic, rootPath + "ItemFrame_01_Normal_Plum"),
            CreateFrameBinding(RecipeRarity.Legendary, rootPath + "ItemFrame_01_Normal_Yellow"),
        };
    }

    private RarityFrameBinding CreateFrameBinding(RecipeRarity rarity, string path)
    {
        GameObject target = RecipeRarityFramePalette.FindOrCloneFrame(transform, path);
        return new RarityFrameBinding
        {
            rarity = rarity,
            target = target
        };
    }

    private TMP_Text FindText(string path)
    {
        Transform child = FindChild(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private TMP_Text FindFirstTextIn(Transform root)
    {
        return root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private Transform FindFirstChildByNameToken(Transform root, string token)
    {
        if (root == null || string.IsNullOrEmpty(token))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child;
            }
        }

        return null;
    }

    private TMP_Text FindFirstTextByNameToken(Transform root, string token)
    {
        Transform child = FindFirstChildByNameToken(root, token);
        return child != null ? child.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private TMP_Text FindFirstTextByExactName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName))
        {
            return null;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.name == exactName)
            {
                return text;
            }
        }

        return null;
    }

    private Image FindImage(string path)
    {
        Transform child = FindChild(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private Button FindButton(string path)
    {
        Transform child = FindChild(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void ApplyFallbackFontIfNeeded()
    {
        if (!autoApplyFallbackFont)
        {
            return;
        }

        if (fallbackFontAsset == null)
        {
            fallbackFontAsset = Resources.Load<TMP_FontAsset>("Fonts/ONE MOBILE POP SDF");
        }

        if (fallbackFontAsset == null)
        {
            return;
        }

        ApplyFont(itemNameText);
        ApplyFont(rarityText);
        ApplyFont(levelText);
        ApplyFont(descriptionText);
        ApplyFont(passiveText);
        ApplyFont(duplicateText);
        ApplyFont(progressText);
        ApplyFont(upgradeGoldCostText);
        ApplyFont(upgradeGoldCostRedText);

        if (grayUpgradeButtonObject != null)
        {
            TMP_Text[] texts = grayUpgradeButtonObject.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                ApplyFont(texts[i]);
            }
        }
    }

    private void ApplyFont(TMP_Text text)
    {
        if (text != null && text.font == null)
        {
            text.font = fallbackFontAsset;
        }
    }

    private void RegisterCloseButton()
    {
        if (closeButton == null)
        {
            return;
        }

        EnsureCloseButtonReceivesClicks();
        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void EnsureCloseButtonReceivesClicks()
    {
        if (closeButtonRaycastImage == null)
        {
            closeButtonRaycastImage = closeButton.GetComponent<Image>();
        }

        if (closeButtonRaycastImage == null)
        {
            closeButtonRaycastImage = closeButton.gameObject.AddComponent<Image>();
            closeButtonRaycastImage.color = new Color(1f, 1f, 1f, 0f);
        }

        closeButtonRaycastImage.raycastTarget = true;

        closeButton.targetGraphic = closeButtonRaycastImage;
    }

    private void RegisterUpgradeButton()
    {
        if (upgradeButton == null)
        {
            return;
        }

        upgradeButton.onClick.RemoveListener(UpgradeCurrentRecipe);
        upgradeButton.onClick.AddListener(UpgradeCurrentRecipe);
    }

    private void ApplyUpgradeGoldCostVisual(bool hasEnoughGold)
    {
        if (upgradeGoldCostRoot != null)
        {
            upgradeGoldCostRoot.SetActive(true);
        }

        if (upgradeGoldCostText != null)
        {
            ApplyUpgradeGoldCostText(upgradeGoldCostText, affordableGoldColor);
            upgradeGoldCostText.gameObject.SetActive(hasEnoughGold || upgradeGoldCostRedText == null);
        }

        if (upgradeGoldCostRedText != null)
        {
            ApplyUpgradeGoldCostText(upgradeGoldCostRedText, insufficientGoldColor);
            upgradeGoldCostRedText.gameObject.SetActive(!hasEnoughGold);
        }

        if (upgradeGoldCostText != null || upgradeGoldCostRedText != null)
        {
            return;
        }

        Color color = hasEnoughGold ? affordableGoldColor : insufficientGoldColor;

        bool applied = false;
        if (upgradeGoldCostRoot != null)
        {
            TMP_Text[] texts = upgradeGoldCostRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                ApplyUpgradeGoldCostText(text, color);
                applied = true;
            }
        }

        if (upgradeGoldCostText != null && !applied)
        {
            ApplyUpgradeGoldCostText(upgradeGoldCostText, color);
        }
    }

    private void ApplyUpgradeGoldCostText(TMP_Text text, Color color)
    {
        text.text = upgradeGoldCost.ToString();
        text.enableVertexGradient = false;
        text.color = color;
        text.ForceMeshUpdate();
    }

    private bool HasEnoughUpgradeGold()
    {
        RuntimeData sourceRuntimeData = GetRuntimeData();
        return sourceRuntimeData != null && sourceRuntimeData.GetGold() >= upgradeGoldCost;
    }

    private RuntimeData GetRuntimeData()
    {
        if (runtimeData == null)
        {
            runtimeData = FindFirstObjectByType<RuntimeData>();
        }

        return runtimeData;
    }

    private void ApplyUpgradeButtonVisual(bool canUpgrade)
    {
        if (upgradeButton != null)
        {
            upgradeButton.interactable = canUpgrade;
        }

        GameObject greenButtonObject = upgradeButton != null ? upgradeButton.gameObject : null;
        GameObject grayButtonObject = grayUpgradeButtonObject;

        if (greenButtonObject != null)
        {
            greenButtonObject.SetActive(canUpgrade);
            SetUpgradeButtonText(greenButtonObject);
        }

        if (grayButtonObject != null)
        {
            grayButtonObject.SetActive(!canUpgrade);
            SetUpgradeButtonText(grayButtonObject);
            DisableButtonInteraction(grayButtonObject);
        }
    }

    private void SetUpgradeButtonText(GameObject buttonObject)
    {
        TMP_Text[] texts = buttonObject.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            text.text = LocalizationLookup.GetLocalizedString(upgradeButtonText);
            ApplyFont(text);
        }
    }

    private void DisableButtonInteraction(GameObject buttonObject)
    {
        Button[] buttons = buttonObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }
    }

    private void UnregisterUpgradeButton()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeCurrentRecipe);
        }
    }

    public void UpgradeCurrentRecipe()
    {
        if (currentDefinition == null)
        {
            Debug.LogWarning("[RecipeDetailPopup] Upgrade ignored because current recipe is missing.", this);
            return;
        }

        if (recipeState == null)
        {
            recipeState = FindFirstObjectByType<RecipeInventoryState>();
        }

        if (recipeState == null)
        {
            Debug.LogWarning("[RecipeDetailPopup] RecipeInventoryState is not found.", this);
            return;
        }

        if (!recipeState.CanUpgradeRecipe(currentDefinition))
        {
            RecipeOwnedState ownedState = recipeState.GetOwnedState(currentDefinition.recipeId);
            int currentLevel = ownedState != null ? ownedState.currentLevel : 0;
            int shardCount = ownedState != null ? ownedState.ownedShardCount : 0;
            int requiredShardCount = currentDefinition.GetRequiredShardCountForLevel(Mathf.Max(1, currentLevel));
            Debug.Log($"[RecipeDetailPopup] Recipe upgrade is not available. id={currentDefinition.recipeId}, level={currentLevel}, shards={shardCount}, required={requiredShardCount}", this);
            return;
        }

        RuntimeData sourceRuntimeData = GetRuntimeData();
        if (sourceRuntimeData == null)
        {
            Debug.LogWarning("[RecipeDetailPopup] RuntimeData is not found.", this);
            return;
        }

        if (!sourceRuntimeData.SpendGold(upgradeGoldCost))
        {
            Debug.Log($"[RecipeDetailPopup] Not enough gold to upgrade recipe. id={currentDefinition.recipeId}, gold={sourceRuntimeData.GetGold()}, required={upgradeGoldCost}", this);
            Open(currentDefinition, recipeState.GetOwnedState(currentDefinition.recipeId), recipeState);
            return;
        }

        if (!recipeState.TryUpgradeRecipe(currentDefinition))
        {
            sourceRuntimeData.AddGold(upgradeGoldCost);
            RecipeOwnedState ownedState = recipeState.GetOwnedState(currentDefinition.recipeId);
            int currentLevel = ownedState != null ? ownedState.currentLevel : 0;
            int shardCount = ownedState != null ? ownedState.ownedShardCount : 0;
            int requiredShardCount = currentDefinition.GetRequiredShardCountForLevel(Mathf.Max(1, currentLevel));
            Debug.Log($"[RecipeDetailPopup] Recipe upgrade is not available. id={currentDefinition.recipeId}, level={currentLevel}, shards={shardCount}, required={requiredShardCount}", this);
            return;
        }

        currentOwnedState = recipeState.GetOwnedState(currentDefinition.recipeId);
        Debug.Log($"[RecipeDetailPopup] Recipe upgraded. id={currentDefinition.recipeId}, level={currentOwnedState.currentLevel}, shards={currentOwnedState.ownedShardCount}", this);
        Open(currentDefinition, currentOwnedState, recipeState);
    }

    private bool WasCloseAreaReleased()
    {
        if (closeButtonRect == null)
        {
            return false;
        }

        if (Input.GetMouseButtonUp(0) && IsScreenPointInCloseButton(Input.mousePosition))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended && IsScreenPointInCloseButton(touch.position))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsScreenPointInCloseButton(Vector2 screenPoint)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(closeButtonRect, screenPoint, eventCamera);
    }

    private void UnregisterCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }
}
