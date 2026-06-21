using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecipeItemCellView : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    private class RarityFrameBinding
    {
        public RecipeRarity rarity;
        public GameObject target;
    }

    [SerializeField] private bool autoWireChildReferences = true;

    [Header("Images")]
    [SerializeField] private Image frameBackgroundImage;
    [SerializeField] private GameObject defaultFrame;
    [SerializeField] private RarityFrameBinding[] rarityFrames;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject disableOverlay;
    [SerializeField] private GameObject lockObject;

    [Header("Progress")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private RectTransform progressFillRect;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject upgradeReadyObject;
    [SerializeField] private Image upgradeReadyImage;
    [SerializeField] private Color progressNormalColor = new Color32(255, 128, 0, 255);
    [SerializeField] private Color progressReadyColor = new Color32(33, 224, 122, 255);

    [Header("Rarity Frame Colors")]
    [SerializeField] private Color commonFrameColor = new Color32(168, 101, 57, 255);
    [SerializeField] private Color magicFrameColor = new Color32(255, 211, 49, 255);
    [SerializeField] private Color rareFrameColor = new Color32(53, 160, 213, 255);
    [SerializeField] private Color epicFrameColor = new Color32(178, 78, 221, 255);
    [SerializeField] private Color legendaryFrameColor = new Color32(240, 75, 92, 255);

    public event Action<RecipeDefinitionData> Clicked;

    private RecipeDefinitionData boundDefinition;

    private void Awake()
    {
        AutoWireChildReferencesIfNeeded();
    }

    private void OnValidate()
    {
        AutoWireChildReferencesIfNeeded();
    }

    public void Bind(RecipeDefinitionData definition, RecipeOwnedState ownedState)
    {
        AutoWireChildReferencesIfNeeded();
        boundDefinition = definition;

        if (definition == null)
        {
            Clear();
            return;
        }

        bool isOwned = ownedState != null;
        int currentLevel = isOwned ? Mathf.Max(1, ownedState.currentLevel) : 0;
        int currentShardCount = isOwned ? Mathf.Max(0, ownedState.ownedShardCount) : 0;
        int maxLevel = Mathf.Max(1, definition.maxLevel);
        bool isMaxLevel = isOwned && currentLevel >= maxLevel;
        int requiredShardCount = isMaxLevel ? 1 : definition.GetRequiredShardCountForLevel(currentLevel);
        bool canUpgrade = isOwned && !isMaxLevel && currentShardCount >= requiredShardCount;
        float progressRate = isMaxLevel ? 1f : Mathf.Clamp01((float)currentShardCount / requiredShardCount);

        if (iconImage != null)
        {
            iconImage.sprite = definition.uiIcon;
            iconImage.enabled = definition.uiIcon != null;
            iconImage.color = isOwned ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.75f);
        }

        if (disableOverlay != null)
        {
            disableOverlay.SetActive(!isOwned);
        }

        if (lockObject != null)
        {
            lockObject.SetActive(!isOwned);
        }

        if (levelText != null)
        {
            levelText.text = isOwned ? $"Lv.{currentLevel}" : string.Empty;
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = progressRate;
        }

        if (progressFillImage != null && progressFillImage.type == Image.Type.Filled)
        {
            progressFillImage.fillAmount = progressRate;
        }

        if (progressFillRect != null)
        {
            Vector3 scale = progressFillRect.localScale;
            scale.x = progressRate;
            progressFillRect.localScale = scale;
        }

        ApplyProgressColor(canUpgrade || isMaxLevel);

        if (upgradeReadyObject != null)
        {
            upgradeReadyObject.SetActive(canUpgrade || isMaxLevel);
        }

        if (upgradeReadyImage != null)
        {
            upgradeReadyImage.enabled = canUpgrade || isMaxLevel;
            upgradeReadyImage.color = progressReadyColor;
        }

        if (progressText != null)
        {
            progressText.text = isOwned
                ? isMaxLevel ? "MAX" : $"{currentShardCount} / {requiredShardCount}"
                : string.Empty;
        }

        UpdateProgressTexts(isOwned
            ? isMaxLevel ? "MAX" : $"{currentShardCount} / {requiredShardCount}"
            : string.Empty);

        ApplyRarityFrame(definition.rarity);
    }

    public void Clear()
    {
        boundDefinition = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
        }

        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }

        if (progressFillImage != null && progressFillImage.type == Image.Type.Filled)
        {
            progressFillImage.fillAmount = 0f;
        }

        if (progressFillRect != null)
        {
            Vector3 scale = progressFillRect.localScale;
            scale.x = 0f;
            progressFillRect.localScale = scale;
        }

        if (progressText != null)
        {
            progressText.text = string.Empty;
        }

        UpdateProgressTexts(string.Empty);

        if (upgradeReadyObject != null)
        {
            upgradeReadyObject.SetActive(false);
        }

        if (upgradeReadyImage != null)
        {
            upgradeReadyImage.enabled = false;
        }

        if (disableOverlay != null)
        {
            disableOverlay.SetActive(true);
        }

        if (lockObject != null)
        {
            lockObject.SetActive(true);
        }

        ApplyRarityFrame(null);
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

    private void ApplyFrameColor(RecipeRarity rarity)
    {
        if (frameBackgroundImage == null)
        {
            return;
        }

        switch (rarity)
        {
            case RecipeRarity.Magic:
                frameBackgroundImage.color = RecipeRarityFramePalette.GetFillColor(rarity);
                break;
            case RecipeRarity.Rare:
                frameBackgroundImage.color = RecipeRarityFramePalette.GetFillColor(rarity);
                break;
            case RecipeRarity.Epic:
                frameBackgroundImage.color = RecipeRarityFramePalette.GetFillColor(rarity);
                break;
            case RecipeRarity.Legendary:
                frameBackgroundImage.color = RecipeRarityFramePalette.GetFillColor(rarity);
                break;
            default:
                frameBackgroundImage.color = RecipeRarityFramePalette.GetFillColor(rarity);
                break;
        }
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

    private Color GetNormalProgressColor()
    {
        bool isSerializedWhite =
            progressNormalColor.r > 0.95f &&
            progressNormalColor.g > 0.95f &&
            progressNormalColor.b > 0.95f;

        return isSerializedWhite
            ? new Color32(255, 128, 0, 255)
            : progressNormalColor;
    }

    [ContextMenu("Auto Wire Child References")]
    private void AutoWireChildReferencesIfNeeded()
    {
        if (!autoWireChildReferences)
        {
            return;
        }

        frameBackgroundImage = FindImage("NormalArea/ItemFrame_01_Normal_Brown/Bg")
            ?? FindImage("NormalArea/ItemFrame_01_Normal_Brow/Bg")
            ?? FindImage("NormalArea/Bg")
            ?? frameBackgroundImage;
        defaultFrame = FindChild("NormalArea/ItemFrame_01_Normal_Brown")?.gameObject
            ?? FindChild("NormalArea/ItemFrame_01_Normal_Brow")?.gameObject
            ?? defaultFrame;
        AutoWireRarityFrames("NormalArea/");
        iconImage = FindImage("Item") ?? iconImage;
        disableOverlay = FindRelativeChild("Disable")?.gameObject ?? disableOverlay;
        Transform lockTransform = FindRelativeChild("Slider_Upgrade_01/Slider_02_Orange/Lock")
            ?? FindRelativeChild("Slider_Upgrade_01/Slider_02_Orange/Lock/Icon_Lock")
            ?? FindRelativeChild("Slider_Upgrade_01/Lock")
            ?? FindRelativeChild("Slider_Upgrade_01/Lock/Icon_Lock")
            ?? FindRelativeChild("Lock")
            ?? FindRelativeChild("Lock/Icon_Lock");
        lockObject = lockTransform != null ? lockTransform.gameObject : lockObject;
        Transform upgradeTransform = FindRelativeChild("Slider_Upgrade_01/Upgrade");
        upgradeReadyObject = upgradeTransform != null ? upgradeTransform.gameObject : upgradeReadyObject;
        upgradeReadyImage = upgradeTransform != null ? upgradeTransform.GetComponent<Image>() : upgradeReadyImage;
        levelText = FindRelativeText("Text_Level") ?? levelText;
        progressFillRect = FindRelativeChild("Slider_Upgrade_01/Slider_02_Orange/Fill Area/Fill") as RectTransform
            ?? progressFillRect;
        progressFillImage = FindRelativeImage("Slider_Upgrade_01/Slider_02_Orange/Fill Area/Fill")
            ?? progressFillImage;
        progressText = FindRelativeText("Slider_Upgrade_01/Slider_02_Orange/Text (TMP)")
            ?? FindRelativeText("Slider_Upgrade_01/Text (TMP)")
            ?? progressText;
    }

    private Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    private Transform FindRelativeChild(string path)
    {
        Transform child = transform.Find(path);
        if (child != null)
        {
            return child;
        }

        return transform.parent != null ? transform.parent.Find(path) : null;
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

    private Image FindImage(string path)
    {
        Transform child = FindChild(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private TMP_Text FindText(string path)
    {
        Transform child = FindChild(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void UpdateProgressTexts(string value)
    {
        Transform progressRoot = FindRelativeChild("Slider_Upgrade_01");
        if (progressRoot == null)
        {
            return;
        }

        TMP_Text[] texts = progressRoot.GetComponentsInChildren<TMP_Text>(true);
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

    private Image FindRelativeImage(string path)
    {
        Transform child = FindRelativeChild(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private TMP_Text FindRelativeText(string path)
    {
        Transform child = FindRelativeChild(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundDefinition != null)
        {
            Clicked?.Invoke(boundDefinition);
        }
    }
}
