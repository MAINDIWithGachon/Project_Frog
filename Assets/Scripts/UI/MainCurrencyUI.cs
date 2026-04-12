using TMPro;
using UnityEngine;

public class MainCurrencyUI : MonoBehaviour
{
    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;

    [Header("# UI")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemText;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (runtimeData != null)
            runtimeData.OnDataChanged += RefreshCurrencyUI;

        RefreshCurrencyUI();
    }

    private void OnDisable()
    {
        if (runtimeData != null)
            runtimeData.OnDataChanged -= RefreshCurrencyUI;
    }

    public void RefreshCurrencyUI()
    {
        if (runtimeData == null)
            return;

        if (goldText != null)
            goldText.text = runtimeData.GetGold().ToString();

        if (gemText != null)
            gemText.text = runtimeData.GetGem().ToString();
    }

    private void ResolveReferences()
    {
        if (runtimeData == null)
            runtimeData = FindAnyObjectByType<RuntimeData>();
    }
}
