using System;
using UnityEngine.Localization.Settings;

public static class LocalizationLookup
{
    public static string GetLocalizedString(LocalizationEntry entry)
    {
        if (!entry.IsValid)
        {
            return entry.Key;
        }

        return LocalizationSettings.StringDatabase.GetLocalizedString(entry.TableKey, entry.Key);
    }

    public static string GetLocalizedString(LocalizationEntry entry, params object[] arguments)
    {
        string value = GetLocalizedString(entry);

        if (arguments == null || arguments.Length == 0 || string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            return string.Format(value, arguments);
        }
        catch (FormatException)
        {
            return value;
        }
    }
}
