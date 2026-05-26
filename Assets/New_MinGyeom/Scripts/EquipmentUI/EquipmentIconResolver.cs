using System;
using UnityEngine;

public class EquipmentIconResolver : MonoBehaviour
{
    [Serializable]
    public class IconBinding
    {
        public string iconKey;
        public Sprite sprite;
    }

    [SerializeField] private IconBinding[] icons = Array.Empty<IconBinding>();
    [SerializeField] private Sprite fallbackIcon;

    public Sprite GetIcon(string iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return fallbackIcon;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            IconBinding binding = icons[i];
            if (binding == null || binding.iconKey != iconKey)
            {
                continue;
            }

            return binding.sprite != null ? binding.sprite : fallbackIcon;
        }

        return fallbackIcon;
    }
}
