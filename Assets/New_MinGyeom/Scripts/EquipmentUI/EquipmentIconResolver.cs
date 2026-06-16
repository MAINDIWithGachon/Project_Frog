using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NewMinGyeom.Equipment;
using UnityEngine;
using UnityEngine.U2D;

public class EquipmentIconResolver : MonoBehaviour
{
    [Header("Item Icons")]
    [SerializeField] private SpriteAtlas iconAtlas;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private bool logMissingIcons = true;

    [Header("Equipment Type Icons")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite hatTypeIcon;
    [SerializeField] private Sprite ringTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite necklaceTypeIcon;
    [SerializeField] private Sprite shoesTypeIcon;

    private readonly HashSet<string> loggedMissingKeys = new();

    public Sprite GetIcon(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey) || iconAtlas == null)
        {
            return fallbackIcon;
        }

        string normalizedKey = NormalizeIconKey(iconKey);
        Sprite sprite = iconAtlas.GetSprite(normalizedKey);
        if (sprite != null)
        {
            return sprite;
        }

        if (logMissingIcons && loggedMissingKeys.Add(iconKey))
        {
            Debug.LogWarning(
                $"[EquipmentIconResolver] Could not find icon '{iconKey}' as '{normalizedKey}' in atlas '{iconAtlas.name}'.",
                this);
        }

        return fallbackIcon;
    }

    public Sprite GetTypeIcon(EquipmentSlotType slotType)
    {
        return slotType switch
        {
            EquipmentSlotType.Weapon => weaponTypeIcon,
            EquipmentSlotType.Hat => hatTypeIcon,
            EquipmentSlotType.Ring => ringTypeIcon,
            EquipmentSlotType.Armor => armorTypeIcon,
            EquipmentSlotType.Necklace => necklaceTypeIcon,
            EquipmentSlotType.Shoes => shoesTypeIcon,
            _ => weaponTypeIcon
        };
    }

    public static Color GetTypeFrameColor(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Common => new Color32(181, 126, 79, 255),
            EquipmentGrade.Magic => new Color32(74, 151, 84, 255),
            EquipmentGrade.Rare => new Color32(52, 103, 185, 255),
            EquipmentGrade.Epic => new Color32(151, 86, 187, 255),
            EquipmentGrade.Legendary => new Color32(214, 149, 44, 255),
            _ => Color.white
        };
    }

    public static Color GetTypeFillColor(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Common => new Color32(241, 206, 146, 255),
            EquipmentGrade.Magic => new Color32(138, 219, 138, 255),
            EquipmentGrade.Rare => new Color32(99, 191, 255, 255),
            EquipmentGrade.Epic => new Color32(206, 144, 255, 255),
            EquipmentGrade.Legendary => new Color32(255, 221, 105, 255),
            _ => Color.white
        };
    }

    private static string NormalizeIconKey(string iconKey)
    {
        string key = iconKey.Trim().Replace('\\', '/');
        key = Path.GetFileNameWithoutExtension(key);

        if (decimal.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) &&
            parsed == decimal.Truncate(parsed))
        {
            key = decimal.ToInt64(parsed).ToString(CultureInfo.InvariantCulture);
        }

        return key;
    }
}
