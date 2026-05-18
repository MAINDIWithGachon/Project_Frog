using System;
using NewMinGyeom.Equipment;
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

    private EquipmentSlotType slotType;
    private EquipmentDefinition currentDefinition;
    private int currentLevel;
    private Action<EquipmentSlotType, EquipmentDefinition, int> clicked;
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
        EquipmentSlotType targetSlotType,
        Action<EquipmentSlotType, EquipmentDefinition, int> onClicked)
    {
        slotType = targetSlotType;
        clicked = onClicked;
        CacheReferences();
        BindButton();
    }

    public void SetSlot(
        EquipmentSlotType targetSlotType,
        EquipmentDefinition definition,
        int level,
        bool showRedDot,
        EquipmentIconResolver iconResolver)
    {
        CacheReferences();

        slotType = targetSlotType;
        currentDefinition = definition;
        currentLevel = Mathf.Max(0, level);

        SetFrame(definition != null ? definition.grade : EquipmentGrade.Common);

        Sprite icon = definition != null ? iconResolver?.GetIcon(definition.iconKey) : null;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
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
        clicked?.Invoke(slotType, currentDefinition, currentLevel);
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

    private void SetFrame(EquipmentGrade grade)
    {
        SetActiveByNameContains(transform, "Normal_Rare", grade == EquipmentGrade.Rare);
        SetActiveByNameContains(transform, "Normal_Blue", grade == EquipmentGrade.Rare);
        SetActiveByNameContains(transform, "Normal_Common", grade == EquipmentGrade.Common);
        SetActiveByNameContains(transform, "Normal_Brown", grade == EquipmentGrade.Common);
        SetActiveByNameContains(transform, "Normal_Magic", grade == EquipmentGrade.Magic);
        SetActiveByNameContains(transform, "Normal_Green", grade == EquipmentGrade.Magic);
        SetActiveByNameContains(transform, "Normal_Epic", grade == EquipmentGrade.Epic);
        SetActiveByNameContains(transform, "Normal_Plum", grade == EquipmentGrade.Epic);
        SetActiveByNameContains(transform, "Normal_Legendary", grade == EquipmentGrade.Legendary);
        SetActiveByNameContains(transform, "Normal_Yellow", grade == EquipmentGrade.Legendary);
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
