using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LocaleToggleButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Text legacyLabelText;
    [SerializeField] private string koreanLocaleCode = "ko";
    [SerializeField] private string englishLocaleCode = "en";

    private void Awake()
    {
        AutoWireReferences();
    }

    private void OnEnable()
    {
        AutoWireReferences();

        if (button != null)
        {
            button.onClick.RemoveListener(ToggleLocale);
            button.onClick.AddListener(ToggleLocale);
        }

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        StartCoroutine(RefreshAfterLocalizationReady());
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleLocale);
        }

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    public void ToggleLocale()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        string nextCode = selectedLocale != null && selectedLocale.Identifier.Code == koreanLocaleCode
            ? englishLocaleCode
            : koreanLocaleCode;

        SetLocale(nextCode);
    }

    public void SetLocale(string localeCode)
    {
        StartCoroutine(SetLocaleRoutine(localeCode));
    }

    private IEnumerator SetLocaleRoutine(string localeCode)
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale == null)
        {
            Debug.LogWarning($"[LocaleToggleButton] Locale not found: {localeCode}", this);
            yield break;
        }

        LocalizationSettings.SelectedLocale = locale;
        RefreshLabel();
    }

    private IEnumerator RefreshAfterLocalizationReady()
    {
        yield return LocalizationSettings.InitializationOperation;
        RefreshLabel();
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        bool isKorean = selectedLocale != null && selectedLocale.Identifier.Code == koreanLocaleCode;
        string label = isKorean ? "EN" : "KO";

        if (labelText != null)
        {
            labelText.text = label;
        }

        if (legacyLabelText != null)
        {
            legacyLabelText.text = label;
        }
    }

    private void AutoWireReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (labelText == null)
        {
            labelText = GetComponentInChildren<TMP_Text>(true);
        }

        if (legacyLabelText == null)
        {
            legacyLabelText = GetComponentInChildren<Text>(true);
        }
    }
}
