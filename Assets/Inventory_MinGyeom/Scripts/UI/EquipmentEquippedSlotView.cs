using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one equipped-slot cell in the fixed six-slot equipment area.
/// This view is separate from inventory list item rendering.
/// </summary>
public class EquipmentEquippedSlotView : MonoBehaviour
{
    [Header("Type Icons")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite hatTypeIcon;
    [SerializeField] private Sprite ringTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite necklaceTypeIcon;
    [SerializeField] private Sprite shoesTypeIcon;

    [Header("Bound UI References")]
    [SerializeField] private GameObject itemFrameRoot;
    [SerializeField] private GameObject normalBlueFrame;
    [SerializeField] private GameObject normalBrownFrame;
    [SerializeField] private GameObject normalGreenFrame;
    [SerializeField] private GameObject normalPlumFrame;
    [SerializeField] private GameObject normalYellowFrame;
    [SerializeField] private GameObject add1Root;
    [SerializeField] private GameObject add2Root;
    [SerializeField] private GameObject redDotRoot;
    [SerializeField] private GameObject typeAreaRoot;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image typeFrameImage;
    [SerializeField] private Image typeBgImage;
    [SerializeField] private Image typeIconImage;
    [SerializeField] private TMP_Text levelText;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif

    public void SetTypeIcons(
        Sprite weapon,
        Sprite hat,
        Sprite ring,
        Sprite armor,
        Sprite necklace,
        Sprite shoes)
    {
        weaponTypeIcon = weapon;
        hatTypeIcon = hat;
        ringTypeIcon = ring;
        armorTypeIcon = armor;
        necklaceTypeIcon = necklace;
        shoesTypeIcon = shoes;
    }

    public void SetEquipped(EquipmentDefinitionData definition, int level)
    {
        CacheReferences();

        if (definition == null)
        {
            SetEmpty(EquipmentCategory.Weapon);
            return;
        }

        SetFrameActive(itemFrameRoot, true);
        SetRarityFrame(definition.rarity);
        ApplyTypeArea(definition.rarity, definition.category);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = definition.uiIcon;
            itemIconImage.enabled = definition.uiIcon != null;
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = $"Lv.{Mathf.Max(1, level)}";
        }

        SetFrameActive(typeAreaRoot, true);
        SetFrameActive(add1Root, false);
        SetFrameActive(add2Root, false);
        SetFrameActive(redDotRoot, false);
    }

    public void SetEmpty(EquipmentCategory category)
    {
        CacheReferences();

        SetFrameActive(itemFrameRoot, true);
        SetRarityFrame(EquipmentRarity.Common);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        ApplyTypeArea(EquipmentRarity.Common, category);
        SetFrameActive(typeAreaRoot, false);
        SetFrameActive(add1Root, false);
        SetFrameActive(add2Root, true);
        SetFrameActive(redDotRoot, false);
    }

    public void SetRedDotVisible(bool isVisible)
    {
        SetFrameActive(redDotRoot, isVisible);
    }

    private void ApplyTypeArea(EquipmentRarity rarity, EquipmentCategory category)
    {
        if (typeFrameImage != null)
        {
            typeFrameImage.color = GetTypeFrameColor(rarity);
        }

        if (typeBgImage != null)
        {
            typeBgImage.color = GetTypeFillColor(rarity);
        }

        if (typeIconImage != null)
        {
            typeIconImage.sprite = GetCategoryIcon(category);
            typeIconImage.enabled = typeIconImage.sprite != null;
        }
    }

    private void SetRarityFrame(EquipmentRarity rarity)
    {
        SetFrameActive(normalBlueFrame, rarity == EquipmentRarity.Rare);
        SetFrameActive(normalBrownFrame, rarity == EquipmentRarity.Common);
        SetFrameActive(normalGreenFrame, rarity == EquipmentRarity.Magic);
        SetFrameActive(normalPlumFrame, rarity == EquipmentRarity.Epic);
        SetFrameActive(normalYellowFrame, rarity == EquipmentRarity.Legendary);
    }

    private void CacheReferences()
    {
        itemFrameRoot ??= FindByPath("ItemFrame_01");
        normalBlueFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Blue");
        normalBrownFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brown");
        normalGreenFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Green");
        normalPlumFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Plum");
        normalYellowFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Yellow");
        add1Root ??= FindByPath("ItemFrame_01/Add_1");
        add2Root ??= FindByPath("ItemFrame_01/Add_2");
        typeAreaRoot ??= FindByPath("TypeArea");
        levelText ??= FindComponentByPath<TMP_Text>("Text_Level");
        itemIconImage ??= FindComponentByPath<Image>("ItemFrame_01/Item/Icon");
        itemIconImage ??= FindComponentByPath<Image>("ItemFrame_01/Item");
        typeBgImage ??= FindComponentByPath<Image>("TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Bg");
        typeIconImage ??= FindComponentByPath<Image>("TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Icon");

        if (typeFrameImage == null && typeAreaRoot != null && typeAreaRoot.transform.childCount > 0)
        {
            typeFrameImage = typeAreaRoot.transform.GetChild(0).GetComponent<Image>();
        }
    }

    private GameObject FindByPath(string relativePath)
    {
        Transform found = transform.Find(relativePath);
        return found != null ? found.gameObject : null;
    }

    private T FindComponentByPath<T>(string relativePath) where T : Component
    {
        Transform found = transform.Find(relativePath);
        return found != null ? found.GetComponent<T>() : null;
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

    private static void SetFrameActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
