using System;
using NewMinGyeom.Equipment;
using NewMinGyeom.Gacha;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultItemEffectController : MonoBehaviour
{
    [Serializable]
    private class RarityEffectSetting
    {
        public EquipmentRarity rarity;
        public bool enableEffectLight;
        public bool enableGlow;
        public Color effectLightColor = Color.white;
        public Color glowColor = Color.white;
    }

    [Header("Effect References")]
    [SerializeField] private GameObject effectLightObject;
    [SerializeField] private Image effectLightImage;
    [SerializeField] private GameObject glowObject;
    [SerializeField] private Image glowImage;

    [Header("Per Rarity Settings")]
    [SerializeField] private RarityEffectSetting[] raritySettings =
    {
        new RarityEffectSetting
        {
            rarity = EquipmentRarity.Common,
            enableEffectLight = false,
            enableGlow = false,
            effectLightColor = Color.white,
            glowColor = Color.white
        },
        new RarityEffectSetting
        {
            rarity = EquipmentRarity.Magic,
            enableEffectLight = false,
            enableGlow = false,
            effectLightColor = Color.white,
            glowColor = Color.white
        },
        new RarityEffectSetting
        {
            rarity = EquipmentRarity.Rare,
            enableEffectLight = true,
            enableGlow = true,
            effectLightColor = new Color(0.35f, 1f, 0.7f, 1f),
            glowColor = new Color(0.35f, 1f, 0.7f, 0.9f)
        },
        new RarityEffectSetting
        {
            rarity = EquipmentRarity.Epic,
            enableEffectLight = true,
            enableGlow = true,
            effectLightColor = new Color(0.8f, 0.4f, 1f, 1f),
            glowColor = new Color(0.8f, 0.4f, 1f, 0.95f)
        },
        new RarityEffectSetting
        {
            rarity = EquipmentRarity.Legendary,
            enableEffectLight = true,
            enableGlow = true,
            effectLightColor = new Color(1f, 0.82f, 0.28f, 1f),
            glowColor = new Color(1f, 0.82f, 0.28f, 1f)
        }
    };

    [Header("Future Camera Hook")]
    [SerializeField] private bool useCameraShake;
   // [SerializeField] private float cameraShakeIntensity = 0f;
   // [SerializeField] private float cameraShakeDuration = 0f;

    private void Reset()
    {
        if (effectLightObject == null)
        {
            Transform child = transform.Find("Group/SampleEffect");
            if (child != null)
            {
                effectLightObject = child.gameObject;
                effectLightImage = child.GetComponent<Image>();
            }
        }

        if (glowObject == null)
        {
            Transform child = transform.Find("Group/Glow");
            if (child != null)
            {
                glowObject = child.gameObject;
                glowImage = child.GetComponent<Image>();
            }
        }
    }

    public void Apply(EquipmentGachaResult result)
    {
        if (result == null)
        {
            Clear();
            return;
        }

        Apply(result.equipmentId, result.rarity);
    }

    public void Apply(RuntimeEquipmentGachaResult result)
    {
        if (result == null)
        {
            Clear();
            return;
        }

        Apply(result.equipmentId, ToLegacyRarity(result.grade));
    }

    public void Apply(string equipmentId, EquipmentGrade grade)
    {
        Apply(equipmentId, ToLegacyRarity(grade));
    }

    public void Apply(string equipmentId, EquipmentRarity rarity)
    {
        RarityEffectSetting setting = GetSetting(rarity);
        if (setting == null)
        {
            Clear();
            return;
        }

        SetEffectState(effectLightObject, setting.enableEffectLight);
        SetEffectState(glowObject, setting.enableGlow);

        if (effectLightImage != null)
        {
            effectLightImage.color = setting.effectLightColor;
        }

        if (glowImage != null)
        {
            glowImage.color = setting.glowColor;
        }

        ApplyEquipmentOverrides(equipmentId, rarity);
        QueueFutureCameraEffects(rarity);
    }

    public void Clear()
    {
        SetEffectState(effectLightObject, false);
        SetEffectState(glowObject, false);
    }

    private RarityEffectSetting GetSetting(EquipmentRarity rarity)
    {
        if (raritySettings == null)
        {
            return null;
        }

        for (int i = 0; i < raritySettings.Length; i++)
        {
            RarityEffectSetting setting = raritySettings[i];
            if (setting != null && setting.rarity == rarity)
            {
                return setting;
            }
        }

        return null;
    }

    private void ApplyEquipmentOverrides(string equipmentId, EquipmentRarity rarity)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return;
        }

        // Add per-equipment effect overrides here when specific IDs need custom visuals.
        // Example:
        // if (equipmentId == "4301") { ... }
    }

    private void QueueFutureCameraEffects(EquipmentRarity rarity)
    {
        if (!useCameraShake)
        {
            return;
        }

        // Future extension point:
        // Trigger camera shake here based on rarity / intensity / duration.
        // Example:
        // cameraShakeController.Play(cameraShakeIntensity, cameraShakeDuration);
    }

    private static void SetEffectState(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private static EquipmentRarity ToLegacyRarity(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Common => EquipmentRarity.Common,
            EquipmentGrade.Magic => EquipmentRarity.Magic,
            EquipmentGrade.Rare => EquipmentRarity.Rare,
            EquipmentGrade.Epic => EquipmentRarity.Epic,
            EquipmentGrade.Legendary => EquipmentRarity.Legendary,
            _ => EquipmentRarity.Common
        };
    }
}
