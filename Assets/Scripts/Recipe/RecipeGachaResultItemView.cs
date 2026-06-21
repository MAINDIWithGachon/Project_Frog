using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class RecipeGachaResultItemView : MonoBehaviour
{
    [Serializable]
    private class RarityFrameBinding
    {
        public RecipeRarity rarity;
        public GameObject target;
    }

    [SerializeField] private bool autoWireChildReferences = true;

    [Header("Data")]
    [SerializeField] private RecipeDatabase recipeDatabase;

    [Header("UI")]
    [SerializeField] private Image frameBackgroundImage;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private GameObject defaultFrame;
    [SerializeField] private RarityFrameBinding[] rarityFrames;
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private float revealDuration = 0.12f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private float overshootScale = 1.05f;

    [Header("Rarity Effects")]
    [SerializeField] private GameObject effectLightObject;
    [SerializeField] private Image effectLightImage;
    [SerializeField] private GameObject glowObject;
    [SerializeField] private Image glowImage;
    [SerializeField] private Color magicEffectColor = new Color(0.35f, 1f, 0.7f, 1f);
    [SerializeField] private Color rareEffectColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color epicEffectColor = new Color(0.8f, 0.4f, 1f, 1f);
    [SerializeField] private Color legendaryEffectColor = new Color(1f, 0.82f, 0.28f, 1f);

    [Header("Rarity Frame Colors")]
    [SerializeField] private Color commonFrameColor = new Color32(168, 101, 57, 255);
    [SerializeField] private Color magicFrameColor = new Color32(72, 205, 99, 255);
    [SerializeField] private Color rareFrameColor = new Color32(53, 160, 213, 255);
    [SerializeField] private Color epicFrameColor = new Color32(178, 78, 221, 255);
    [SerializeField] private Color legendaryFrameColor = new Color32(255, 211, 49, 255);

    private RecipeDefinitionData currentDefinition;
    private RecipeGachaResult currentResult;
    private CanvasGroup canvasGroup;
    private Coroutine revealRoutine;
    private Vector3 initialScale = Vector3.one;

    private void Awake()
    {
        AutoWireChildReferences();
        EnsureRevealReferences();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        StopRevealAnimation();
        SetRevealVisuals(1f, 1f);
    }

    private void OnValidate()
    {
        AutoWireChildReferences();
    }

    public void Bind(RecipeGachaResult result)
    {
        EnsureChildReferences();

        if (result == null)
        {
            Clear();
            return;
        }

        if (result.definition != null)
        {
            ApplyDefinition(result.definition, result);
            return;
        }

        RecipeDatabase sourceDatabase = recipeDatabase;
        if (sourceDatabase == null || !sourceDatabase.TryGetById(result.recipeId, out RecipeDefinitionData definition))
        {
            Clear();
            return;
        }

        ApplyDefinition(definition, result);
    }

    public void Bind(string recipeId, RecipeDatabase sourceDatabase = null)
    {
        EnsureChildReferences();

        RecipeDatabase resolvedDatabase = sourceDatabase != null ? sourceDatabase : recipeDatabase;
        if (resolvedDatabase == null || !resolvedDatabase.TryGetById(recipeId, out RecipeDefinitionData definition))
        {
            Clear();
            return;
        }

        ApplyDefinition(definition, null);
    }

    public void Clear()
    {
        ApplyRarityFrame(null);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = string.Empty;
        }

        if (rarityText != null)
        {
            rarityText.text = string.Empty;
        }

        if (stateText != null)
        {
            stateText.text = string.Empty;
        }
    }

    public void PlayRevealAnimation()
    {
        EnsureRevealReferences();
        StopRevealAnimation();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            SetRevealVisuals(1f, 1f);
            return;
        }

        revealRoutine = StartCoroutine(PlayRevealAnimationRoutine());
    }

    private void ApplyDefinition(RecipeDefinitionData definition, RecipeGachaResult result)
    {
        EnsureChildReferences();

        if (definition == null)
        {
            Clear();
            return;
        }

        currentDefinition = definition;
        currentResult = result;

        definition.ApplyRarityFromRecipeId();
        ApplyRarityFrame(definition.rarity);
        ApplyRarityEffect(definition.rarity);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = definition.uiIcon;
            itemIconImage.enabled = definition.uiIcon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = definition.GetLocalizedName();
        }

        if (rarityText != null)
        {
            rarityText.text = definition.rarity.ToString();
        }

        if (stateText != null && result != null)
        {
            if (result.isNewRecipe)
            {
                stateText.text = "NEW";
            }
            else if (result.leveledUp)
            {
                stateText.text = $"Lv.{result.currentLevel} UP";
            }
            else
            {
                stateText.text = $"+{definition.shardCountOnDuplicate}";
            }
        }
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (currentDefinition == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        ApplyDefinition(currentDefinition, currentResult);
    }

    private void EnsureChildReferences()
    {
        AutoWireChildReferences();
        EnsureRevealReferences();
    }

    [ContextMenu("Auto Wire Child References")]
    private void AutoWireChildReferences()
    {
        if (!autoWireChildReferences)
        {
            return;
        }

        frameBackgroundImage = FindImage("Group/NormalArea/ItemFrame_01_Normal_Brown/Bg")
            ?? FindImage("Group/NormalArea/ItemFrame_01_Normal_Brow/Bg")
            ?? FindImage("Group/NormalArea/Bg")
            ?? FindImage("NormalArea/ItemFrame_01_Normal_Brown/Bg")
            ?? FindImage("NormalArea/ItemFrame_01_Normal_Brow/Bg")
            ?? FindImage("NormalArea/Bg")
            ?? FindImage("Bg")
            ?? frameBackgroundImage;
        defaultFrame = FindChild("Group/NormalArea/ItemFrame_01_Normal_Brown")?.gameObject
            ?? FindChild("Group/NormalArea/ItemFrame_01_Normal_Brow")?.gameObject
            ?? FindChild("Group/NormalArea")?.gameObject
            ?? FindChild("NormalArea/ItemFrame_01_Normal_Brown")?.gameObject
            ?? FindChild("NormalArea/ItemFrame_01_Normal_Brow")?.gameObject
            ?? FindChild("NormalArea")?.gameObject
            ?? defaultFrame;
        Transform groupNormalArea = FindChild("Group/NormalArea");
        if (groupNormalArea != null)
        {
            rarityFrames = Array.Empty<RarityFrameBinding>();
        }
        else
        {
            AutoWireRarityFrames("NormalArea/");
        }

        itemIconImage = FindImage("Item") ?? FindImage("Group/Item") ?? itemIconImage;
        effectLightObject = FindChild("EffectLight")?.gameObject
            ?? FindChild("Group/EffectLight")?.gameObject
            ?? FindDescendantByName("EffectLight")?.gameObject
            ?? effectLightObject;
        effectLightImage = effectLightObject != null ? effectLightObject.GetComponent<Image>() : effectLightImage;
        glowObject = FindChild("Glow")?.gameObject
            ?? FindChild("Group/Glow")?.gameObject
            ?? FindDescendantByName("Glow")?.gameObject
            ?? glowObject;
        glowImage = glowObject != null ? glowObject.GetComponent<Image>() : glowImage;
    }

    private Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    private Transform FindDescendantByName(string childName)
    {
        if (string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private Image FindImage(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Image>() : null;
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
            bool activeFrameIsInsideDefault = activeFrame != null && activeFrame.transform.IsChildOf(defaultFrame.transform);
            bool shouldShowDefaultFrame = !rarity.HasValue || !matched || activeFrameIsInsideDefault;
            defaultFrame.SetActive(shouldShowDefaultFrame);
            activeFrame = !rarity.HasValue || !matched ? defaultFrame : activeFrame;
        }

        if (rarity.HasValue)
        {
            ApplyFrameColor(rarity.Value);
            RecipeRarityFramePalette.Apply(activeFrame, rarity.Value);
        }
    }

    private void ApplyFrameColor(RecipeRarity rarity)
    {
        if (frameBackgroundImage != null)
        {
            frameBackgroundImage.color = GetFrameColor(rarity);
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

    private void ApplyRarityEffect(RecipeRarity? rarity)
    {
        bool shouldShowEffect = rarity.HasValue && rarity.Value != RecipeRarity.Common;
        SetEffectState(effectLightObject, shouldShowEffect);
        SetEffectState(glowObject, shouldShowEffect);

        if (!shouldShowEffect || !rarity.HasValue)
        {
            return;
        }

        Color color = GetEffectColor(rarity.Value);
        if (effectLightImage != null)
        {
            effectLightImage.color = color;
            effectLightImage.raycastTarget = false;
        }

        if (glowImage != null)
        {
            Color glowColor = color;
            glowColor.a = Mathf.Min(glowColor.a, 0.95f);
            glowImage.color = glowColor;
            glowImage.raycastTarget = false;
        }
    }

    private Color GetEffectColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return magicEffectColor;
            case RecipeRarity.Rare:
                return rareEffectColor;
            case RecipeRarity.Epic:
                return epicEffectColor;
            case RecipeRarity.Legendary:
                return legendaryEffectColor;
            default:
                return Color.white;
        }
    }

    private IEnumerator PlayRevealAnimationRoutine()
    {
        float elapsed = 0f;
        SetRevealVisuals(0f, startScale);

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / revealDuration);
            float scale = normalized < 0.65f
                ? Mathf.Lerp(startScale, overshootScale, normalized / 0.65f)
                : Mathf.Lerp(overshootScale, 1f, (normalized - 0.65f) / 0.35f);

            SetRevealVisuals(normalized, scale);
            yield return null;
        }

        SetRevealVisuals(1f, 1f);
        revealRoutine = null;
    }

    private void StopRevealAnimation()
    {
        if (revealRoutine == null)
        {
            return;
        }

        StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    private void SetRevealVisuals(float alpha, float scaleMultiplier)
    {
        EnsureRevealReferences();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = initialScale * scaleMultiplier;
        }
    }

    private void EnsureRevealReferences()
    {
        if (animatedRoot == null)
        {
            animatedRoot = transform as RectTransform;
        }

        if (animatedRoot != null && initialScale == Vector3.one)
        {
            initialScale = animatedRoot.localScale;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetEffectState(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}











