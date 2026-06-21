using UnityEngine;
using UnityEngine.UI;

public static class RecipeRarityFramePalette
{
    public static void Apply(GameObject frameRoot, RecipeRarity rarity)
    {
        if (frameRoot == null)
        {
            return;
        }

        Color fill = GetFillColor(rarity);
        Color border = GetBorderColor(rarity);
        Color accent = GetAccentColor(rarity);
        Color highlight = GetHighlightColor(rarity);

        Image[] images = frameRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            string objectName = image.gameObject.name;
            if (objectName.Contains("Bg2"))
            {
                image.color = highlight;
            }
            else if (objectName.Contains("Bg"))
            {
                image.color = fill;
            }
            else if (objectName.Contains("HighLight") || objectName.Contains("Highlight") || objectName.Contains("Glow") || objectName.Contains("Focus"))
            {
                image.color = highlight;
            }
            else if (objectName.Contains("InnerBorder") || objectName.Contains("SpecialBorder"))
            {
                image.color = accent;
            }
            else if (objectName.Contains("Border"))
            {
                image.color = border;
            }
        }
    }

    public static GameObject FindOrCloneFrame(Transform owner, string relativePath)
    {
        if (owner == null || string.IsNullOrEmpty(relativePath))
        {
            return null;
        }

        Transform existing = owner.Find(relativePath);
        if (existing != null)
        {
            return existing.gameObject;
        }

        if (!Application.isPlaying)
        {
            return null;
        }

        int slashIndex = relativePath.LastIndexOf('/');
        string parentPath = slashIndex >= 0 ? relativePath.Substring(0, slashIndex) : string.Empty;
        string frameName = slashIndex >= 0 ? relativePath.Substring(slashIndex + 1) : relativePath;
        Transform parent = string.IsNullOrEmpty(parentPath) ? owner : owner.Find(parentPath);
        if (parent == null)
        {
            return null;
        }

        Transform template = FindSceneTemplate(owner, frameName);
        if (template == null)
        {
            return null;
        }

        GameObject clone = Object.Instantiate(template.gameObject, parent, false);
        clone.name = frameName;
        clone.SetActive(false);

        Transform defaultFrame = parent.Find("ItemFrame_01_Normal_Brown")
            ?? parent.Find("ItemFrame_01_Normal_Brow");
        CopyRectTransform(defaultFrame as RectTransform, clone.transform as RectTransform);

        return clone;
    }

    private static Transform FindSceneTemplate(Transform owner, string frameName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != frameName)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate == owner || candidate.IsChildOf(owner))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    public static Color GetFillColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return new Color32(82, 219, 105, 255);
            case RecipeRarity.Rare:
                return new Color32(61, 166, 213, 255);
            case RecipeRarity.Epic:
                return new Color32(201, 94, 236, 255);
            case RecipeRarity.Legendary:
                return new Color32(255, 215, 39, 255);
            default:
                return new Color32(178, 113, 66, 255);
        }
    }

    public static Color GetBorderColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return new Color32(28, 110, 46, 255);
            case RecipeRarity.Rare:
                return new Color32(31, 87, 132, 255);
            case RecipeRarity.Epic:
                return new Color32(105, 52, 143, 255);
            case RecipeRarity.Legendary:
                return new Color32(214, 133, 28, 255);
            default:
                return new Color32(110, 65, 39, 255);
        }
    }

    public static Color GetAccentColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return new Color32(57, 185, 80, 255);
            case RecipeRarity.Rare:
                return new Color32(45, 135, 188, 255);
            case RecipeRarity.Epic:
                return new Color32(165, 76, 211, 255);
            case RecipeRarity.Legendary:
                return new Color32(255, 171, 31, 255);
            default:
                return new Color32(154, 91, 53, 255);
        }
    }

    public static Color GetHighlightColor(RecipeRarity rarity)
    {
        switch (rarity)
        {
            case RecipeRarity.Magic:
                return new Color32(116, 245, 134, 255);
            case RecipeRarity.Rare:
                return new Color32(103, 204, 245, 255);
            case RecipeRarity.Epic:
                return new Color32(224, 136, 255, 255);
            case RecipeRarity.Legendary:
                return new Color32(255, 248, 94, 255);
            default:
                return new Color32(218, 156, 93, 255);
        }
    }
}
