using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultItemView : MonoBehaviour
{
    [Serializable]
    private class RarityFrameBinding
    {
        public EquipmentRarity rarity;
        public GameObject target;
    }

    [Header("Data")]
    [SerializeField] private EquipmentDatabase equipmentDatabase;

    [Header("UI")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private GameObject defaultFrame;
    [SerializeField] private RarityFrameBinding[] rarityFrames;
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private float revealDuration = 0.12f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private float overshootScale = 1.05f;

    [Header("Debug")]
    [SerializeField] private string currentEquipmentId;
    [SerializeField] private EquipmentRarity currentRarity;

    private CanvasGroup canvasGroup;
    private Coroutine revealRoutine;
    private Vector3 initialScale = Vector3.one;
    private GachaResultItemEffectController effectController;

    private void Awake()
    {
        if (animatedRoot == null)
        {
            animatedRoot = transform as RectTransform;
        }

        if (animatedRoot != null)
        {
            initialScale = animatedRoot.localScale;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        effectController = GetComponent<GachaResultItemEffectController>();
    }

    public void Bind(EquipmentGachaResult result)
    {
        EnsureInitialized();

        if (result == null)
        {
            Clear();
            return;
        }

        EquipmentDatabase sourceDatabase = equipmentDatabase;
        if (result.definition != null)
        {
            ApplyDefinition(result.equipmentId, result.definition);
            return;
        }

        Bind(result.equipmentId, sourceDatabase);
    }

    public void Bind(string equipmentId, EquipmentDatabase sourceDatabase = null)
    {
        EnsureInitialized();

        EquipmentDatabase resolvedDatabase = sourceDatabase != null ? sourceDatabase : equipmentDatabase;
        if (resolvedDatabase == null)
        {
            Debug.LogWarning("[GachaResultItemView] EquipmentDatabase is not assigned.", this);
            Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(equipmentId) || !resolvedDatabase.TryGetById(equipmentId, out EquipmentDefinitionData definition))
        {
            Debug.LogWarning($"[GachaResultItemView] Could not find equipment id '{equipmentId}'.", this);
            Clear();
            return;
        }

        ApplyDefinition(equipmentId, definition);
    }

    public void Clear()
    {
        EnsureInitialized();
        StopRevealAnimation();
        currentEquipmentId = string.Empty;
        currentRarity = EquipmentRarity.Common;

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

        ApplyRarityFrame(null);
        SetRevealVisuals(1f, 1f);

        if (effectController != null)
        {
            effectController.Clear();
        }
    }

    public void PlayRevealAnimation()
    {
        EnsureInitialized();
        StopRevealAnimation();
        revealRoutine = StartCoroutine(PlayRevealAnimationRoutine());
    }

    private void ApplyDefinition(string equipmentId, EquipmentDefinitionData definition)
    {
        if (definition == null)
        {
            Clear();
            return;
        }

        currentEquipmentId = equipmentId;
        currentRarity = definition.rarity;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = definition.uiIcon;
            itemIconImage.enabled = definition.uiIcon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = definition.displayName;
        }

        if (rarityText != null)
        {
            rarityText.text = definition.rarity.ToString();
        }

        ApplyRarityFrame(definition.rarity);

        if (effectController != null)
        {
            effectController.Apply(equipmentId, definition.rarity);
        }
    }

    private void ApplyRarityFrame(EquipmentRarity? rarity)
    {
        if (defaultFrame != null)
        {
            defaultFrame.SetActive(!rarity.HasValue);
        }

        if (rarityFrames == null)
        {
            return;
        }

        bool matched = false;
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
        }

        if (defaultFrame != null)
        {
            defaultFrame.SetActive(!matched);
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
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = initialScale * scaleMultiplier;
        }
    }

    private void EnsureInitialized()
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

        if (effectController == null)
        {
            effectController = GetComponent<GachaResultItemEffectController>();
        }
    }
}
