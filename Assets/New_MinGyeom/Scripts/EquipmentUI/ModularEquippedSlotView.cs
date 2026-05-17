using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModularEquippedSlotView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject redDotRoot;
    [SerializeField] private GameObject addRoot;
    [SerializeField] private GameObject typeAreaRoot;

    private EquipmentCategory category;
    private EquipmentDefinitionData currentDefinition;
    private int currentLevel;
    private Action<EquipmentCategory, EquipmentDefinitionData, int> clicked;
    private bool bound;

    private void Awake()
    {
        CacheReferences();
        BindButton();
    }

    private void OnEnable()
    {
        CacheReferences();
        BindButton();
    }

    public void Configure(
        EquipmentCategory slotCategory,
        Action<EquipmentCategory, EquipmentDefinitionData, int> onClicked)
    {
        category = slotCategory;
        clicked = onClicked;
        CacheReferences();
        BindButton();
    }

    public void SetSlot(
        EquipmentCategory slotCategory,
        EquipmentDefinitionData definition,
        int level,
        bool showRedDot)
    {
        CacheReferences();

        category = slotCategory;
        currentDefinition = definition;
        currentLevel = Mathf.Max(0, level);

        SetFrame(definition != null ? definition.rarity : EquipmentRarity.Common);

        if (iconImage != null)
        {
            iconImage.sprite = definition != null ? definition.uiIcon : null;
            iconImage.enabled = definition != null && definition.uiIcon != null;
        }

        if (levelText != null)
        {
            bool hasLevel = definition != null && currentLevel > 0;
            levelText.gameObject.SetActive(hasLevel);
            levelText.text = hasLevel ? $"Lv.{currentLevel}" : string.Empty;
        }

        SetActive(addRoot, definition == null);
        SetActive(typeAreaRoot, definition != null);
        SetActive(redDotRoot, showRedDot);
    }

    private void HandleClick()
    {
        clicked?.Invoke(category, currentDefinition, currentLevel);
    }

    private void BindButton()
    {
        if (button == null || bound)
        {
            return;
        }

        button.onClick.AddListener(HandleClick);
        bound = true;
    }

    private void CacheReferences()
    {
        button ??= GetComponent<Button>();
        button ??= GetComponentInChildren<Button>(true);
        iconImage ??= FindImageByName(transform, "Icon");
        levelText ??= FindTextByName(transform, "Text_Level");
        redDotRoot ??= FindDescendantByNameContains(transform, "Alert_Dot")?.gameObject;
        addRoot ??= FindDescendantByName(transform, "Add_2")?.gameObject;
        addRoot ??= FindDescendantByName(transform, "Add_1")?.gameObject;
        typeAreaRoot ??= FindDescendantByName(transform, "TypeArea")?.gameObject;
    }

    private void SetFrame(EquipmentRarity rarity)
    {
        SetActiveByNameContains(transform, "Normal_Rare", rarity == EquipmentRarity.Rare);
        SetActiveByNameContains(transform, "Normal_Blue", rarity == EquipmentRarity.Rare);
        SetActiveByNameContains(transform, "Normal_Common", rarity == EquipmentRarity.Common);
        SetActiveByNameContains(transform, "Normal_Brown", rarity == EquipmentRarity.Common);
        SetActiveByNameContains(transform, "Normal_Magic", rarity == EquipmentRarity.Magic);
        SetActiveByNameContains(transform, "Normal_Green", rarity == EquipmentRarity.Magic);
        SetActiveByNameContains(transform, "Normal_Epic", rarity == EquipmentRarity.Epic);
        SetActiveByNameContains(transform, "Normal_Plum", rarity == EquipmentRarity.Epic);
        SetActiveByNameContains(transform, "Normal_Legendary", rarity == EquipmentRarity.Legendary);
        SetActiveByNameContains(transform, "Normal_Yellow", rarity == EquipmentRarity.Legendary);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
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
