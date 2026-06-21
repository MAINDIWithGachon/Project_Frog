using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private LocalizationEntry text;

    private void Awake()
    {
        AutoWireReferences();
    }

    private void OnEnable()
    {
        AutoWireReferences();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    public void Refresh()
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = LocalizationLookup.GetLocalizedString(text);
    }

    private void OnLocaleChanged(Locale locale)
    {
        Refresh();
    }

    private void AutoWireReferences()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }
}
