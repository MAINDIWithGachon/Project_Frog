using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Serializable]
    private class RarityFrameBinding
    {
        public EquipmentRarity rarity;
        public GameObject target;
    }

    [Header("Core References")]
    [SerializeField] private EquipmentGachaService gachaService;

    [Header("Result Popup")]
    [SerializeField] private GameObject gachaResultRoot;
    [SerializeField] private Image resultItemIconImage;
    [SerializeField] private TMP_Text resultItemNameText;
    [SerializeField] private TMP_Text resultRarityText;
    [SerializeField] private GameObject resultDefaultFrame;
    [SerializeField] private RarityFrameBinding[] rarityFrames;

    [Header("Failure Popup")]
    [SerializeField] private GameObject insufficientCurrencyRoot;

    [Header("Optional Buttons")]
    [SerializeField] private Button drawButton;

    public EquipmentGachaResult LastResult { get; private set; }

    private void Awake()
    {
        SetPopupActive(gachaResultRoot, false);
        SetPopupActive(insufficientCurrencyRoot, false);
    }

    public void OnClickDraw()
    {
        SetPopupActive(insufficientCurrencyRoot, false);

        if (drawButton != null)
        {
            drawButton.interactable = false;
        }

        if (gachaService == null)
        {
            Debug.LogWarning("[GachaManager] EquipmentGachaService is not assigned.", this);
            RestoreDrawButton();
            return;
        }

        if (!gachaService.TryDraw(out EquipmentGachaResult result))
        {
            LastResult = null;
            SetPopupActive(insufficientCurrencyRoot, true);
            RestoreDrawButton();
            return;
        }

        LastResult = result;
        ShowResult(result);
        RestoreDrawButton();
    }

    public void CloseResultPopup()
    {
        SetPopupActive(gachaResultRoot, false);
    }

    public void CloseInsufficientCurrencyPopup()
    {
        SetPopupActive(insufficientCurrencyRoot, false);
    }

    private void ShowResult(EquipmentGachaResult result)
    {
        if (result == null || result.definition == null)
        {
            Debug.LogWarning("[GachaManager] Cannot show result because the gacha result is empty.", this);
            SetPopupActive(gachaResultRoot, false);
            return;
        }

        EquipmentDefinitionData definition = result.definition;

        if (resultItemIconImage != null)
        {
            resultItemIconImage.sprite = definition.uiIcon;
            resultItemIconImage.enabled = definition.uiIcon != null;
        }

        if (resultItemNameText != null)
        {
            resultItemNameText.text = definition.displayName;
        }

        if (resultRarityText != null)
        {
            resultRarityText.text = definition.rarity.ToString();
        }

        ApplyRarityFrame(definition.rarity);
        SetPopupActive(gachaResultRoot, true);
    }

    private void ApplyRarityFrame(EquipmentRarity rarity)
    {
        SetPopupActive(resultDefaultFrame, true);

        if (rarityFrames == null)
        {
            return;
        }

        for (int i = 0; i < rarityFrames.Length; i++)
        {
            RarityFrameBinding binding = rarityFrames[i];
            if (binding == null || binding.target == null)
            {
                continue;
            }

            bool isMatch = binding.rarity == rarity;
            binding.target.SetActive(isMatch);

            if (isMatch)
            {
                SetPopupActive(resultDefaultFrame, false);
            }
        }
    }

    private void SetPopupActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void RestoreDrawButton()
    {
        if (drawButton != null)
        {
            drawButton.interactable = true;
        }
    }
}
