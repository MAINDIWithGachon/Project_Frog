using System;
using NewMinGyeom.Equipment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModularEquipmentListItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text equippedStateText;
    [SerializeField] private GameObject checkRoot;
    [SerializeField] private GameObject addRoot;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private GameObject redDotRoot;
    [SerializeField] private GameObject typeAreaRoot;

    private EquipmentDefinition currentDefinition;
    private int currentLevel;
    private Action<EquipmentDefinition, int> clicked;
    private bool bound;

    public EquipmentDefinition CurrentDefinition => currentDefinition;
    public int CurrentLevel => currentLevel;

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

    public void Configure(Action<EquipmentDefinition, int> onClicked)
    {
        clicked = onClicked;
        CacheReferences();
        BindButton();
    }

    public void SetItem(
        EquipmentDefinition definition,
        int level,
        int ownedCount,
        bool isEquipped,
        bool canUpgrade,
        EquipmentIconResolver iconResolver)
    {
        CacheReferences();

        currentDefinition = definition;
        currentLevel = Mathf.Max(1, level);

        gameObject.SetActive(true);
        SetFrame(definition != null ? definition.grade : EquipmentGrade.Common);

        Sprite icon = definition != null ? iconResolver?.GetIcon(definition.iconKey) : null;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = $"Lv.{currentLevel}";
        }

        if (ownedCountText != null)
        {
            ownedCountText.text = ownedCount > 1 ? ownedCount.ToString() : string.Empty;
        }

        if (nameText != null)
        {
            nameText.text = definition != null ? definition.displayName : string.Empty;
        }

        if (equippedStateText != null)
        {
            equippedStateText.text = isEquipped ? "Equipped" : string.Empty;
        }

        SetActive(checkRoot, isEquipped);
        SetActive(addRoot, false);
        SetActive(lockRoot, false);
        SetActive(redDotRoot, canUpgrade);
        SetActive(typeAreaRoot, definition != null);

        if (button != null)
        {
            button.interactable = definition != null;
        }
    }

    public void SetAddSlot()
    {
        CacheReferences();

        currentDefinition = null;
        currentLevel = 0;

        gameObject.SetActive(true);
        SetFrame(EquipmentGrade.Common);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        if (ownedCountText != null)
        {
            ownedCountText.text = string.Empty;
        }

        if (nameText != null)
        {
            nameText.text = string.Empty;
        }

        if (equippedStateText != null)
        {
            equippedStateText.text = string.Empty;
        }

        SetActive(checkRoot, false);
        SetActive(addRoot, true);
        SetActive(lockRoot, false);
        SetActive(redDotRoot, false);
        SetActive(typeAreaRoot, false);

        if (button != null)
        {
            button.interactable = false;
        }
    }

    public void Clear()
    {
        currentDefinition = null;
        currentLevel = 0;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (currentDefinition == null)
        {
            return;
        }

        clicked?.Invoke(currentDefinition, currentLevel);
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
        nameText ??= FindTextByName(transform, "Text_ItemName");
        ownedCountText ??= FindTextByName(transform, "Text_Count");
        equippedStateText ??= FindTextByName(transform, "Text_Equipped");

        checkRoot ??= FindDescendantByName(transform, "Check")?.gameObject;
        addRoot ??= FindDescendantByName(transform, "Add_2")?.gameObject;
        addRoot ??= FindDescendantByName(transform, "Add_1")?.gameObject;
        lockRoot ??= FindDescendantByName(transform, "Lock")?.gameObject;
        redDotRoot ??= FindDescendantByNameContains(transform, "Alert_Dot")?.gameObject;
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
