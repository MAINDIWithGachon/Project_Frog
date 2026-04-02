using UnityEngine;

/// <summary>
/// Mirrors RuntimeData currency values in the inspector and lets designers tweak them live during play mode.
/// </summary>
public class RuntimeCurrencyInspector : MonoBehaviour
{
    [SerializeField] private RuntimeData runtimeData;
    [SerializeField] private bool refreshFromRuntimeEachFrame = true;

    [Header("Live Currency View")]
    [SerializeField] private int gold;
    [SerializeField] private int upgradeStone;

    private int lastAppliedGold = int.MinValue;
    private int lastAppliedUpgradeStone = int.MinValue;

    private void Awake()
    {
        ResolveReferences();
        RefreshFromRuntime();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshFromRuntime();
    }

    private void Update()
    {
        if (!Application.isPlaying || !refreshFromRuntimeEachFrame)
        {
            return;
        }

        ResolveReferences();
        if (runtimeData == null)
        {
            return;
        }

        int runtimeGold = runtimeData.GetGold();
        int runtimeUpgradeStone = runtimeData.GetUpgradeStone();
        if (runtimeGold == lastAppliedGold && runtimeUpgradeStone == lastAppliedUpgradeStone)
        {
            return;
        }

        gold = runtimeGold;
        upgradeStone = runtimeUpgradeStone;
        lastAppliedGold = runtimeGold;
        lastAppliedUpgradeStone = runtimeUpgradeStone;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        ApplyInspectorValuesToRuntime();
    }

    [ContextMenu("Refresh From Runtime")]
    public void RefreshFromRuntime()
    {
        ResolveReferences();
        if (runtimeData == null)
        {
            return;
        }

        gold = runtimeData.GetGold();
        upgradeStone = runtimeData.GetUpgradeStone();
        lastAppliedGold = gold;
        lastAppliedUpgradeStone = upgradeStone;
    }

    [ContextMenu("Apply Inspector Values")]
    public void ApplyInspectorValuesToRuntime()
    {
        ResolveReferences();
        if (runtimeData == null)
        {
            return;
        }

        RuntimeData.RootData root = runtimeData.GetRoot();
        if (root == null || root.Currency == null)
        {
            return;
        }

        root.Currency.Gold = Mathf.Max(0, gold);
        root.Currency.UpgradeStone = Mathf.Max(0, upgradeStone);

        gold = root.Currency.Gold;
        upgradeStone = root.Currency.UpgradeStone;
        lastAppliedGold = gold;
        lastAppliedUpgradeStone = upgradeStone;
    }

    private void ResolveReferences()
    {
        runtimeData ??= FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
    }
}
