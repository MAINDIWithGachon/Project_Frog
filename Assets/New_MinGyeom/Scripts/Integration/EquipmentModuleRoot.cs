using NewMinGyeom.Backend;
using NewMinGyeom.Equipment;
using TMPro;
using UnityEngine;

/// <summary>
/// Single entry point for the modular equipment UI.
/// External scenes should only need to assign RuntimeData here.
/// </summary>
public class EquipmentModuleRoot : MonoBehaviour
{
    [Header("External Input")]
    [SerializeField] private RuntimeData runtimeData;

    [Header("Runtime Equipment")]
    [SerializeField] private EquipmentRuntimeState equipmentState;
    [SerializeField] private EquipmentStatProvider statProvider;
    [SerializeField] private MockEquipmentDatabaseLoader mockLoader;
    [SerializeField] private EquipmentIconResolver iconResolver;
    [SerializeField] private EquipmentSaveDirtyTracker saveDirtyTracker;

    [Header("Internal Roots")]
    [SerializeField] private GameObject equipmentWindowRoot;
    [SerializeField] private GameObject resourceBarRoot;
    [SerializeField] private GameObject detailPopupRoot;

    [Header("Internal Components")]
    [SerializeField] private ModularEquipmentWindowController equipmentWindowController;
    [SerializeField] private ModularEquipmentDetailPanelController detailPanelController;

    [Header("Currency UI")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemText;

    [Header("Combat Power UI")]
    [SerializeField] private TMP_Text combatPowerText;

    [Header("Startup")]
    [SerializeField] private bool bindOnAwake = true;
    [SerializeField] private bool loadMockOnStart;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool hideEquipmentWindowOnStart = true;
    [SerializeField] private bool hideDetailPopupOnStart = true;

    public RuntimeData RuntimeData => runtimeData;
    public EquipmentRuntimeState EquipmentState => equipmentState;
    public EquipmentStatProvider StatProvider => statProvider;
    public EquipmentIconResolver IconResolver => iconResolver;
    public EquipmentSaveDirtyTracker SaveDirtyTracker => saveDirtyTracker;
    private RuntimeData subscribedRuntimeData;
    private CombatPowerData combatPowerData;
    private PlayerStatController playerStatController;

    private void Awake()
    {
        if (bindOnAwake)
        {
            BindModule();
        }
    }

    private void OnEnable()
    {
        SubscribeCurrency();
        SubscribeCombatPower();
        RefreshCurrencyUI();
        RefreshCombatPowerUI();
    }

    private void OnDisable()
    {
        UnsubscribeCurrency();
        UnsubscribeCombatPower();
    }

    private void Start()
    {
        if (loadMockOnStart && mockLoader != null && equipmentState.LoadState != EquipmentDatabaseLoadState.Ready)
        {
            mockLoader.LoadMock();
            saveDirtyTracker?.ClearDirty();
        }

        if (refreshOnStart)
        {
            RefreshModule();
        }

        ApplyStartupVisibility();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveInternalReferences();
    }
#endif

    public void Initialize(RuntimeData data)
    {
        UnsubscribeCurrency();
        runtimeData = data;
        BindModule();
        RefreshModule();
        SubscribeCurrency();
        SubscribeCombatPower();
        RefreshCurrencyUI();
        RefreshCombatPowerUI();
    }

    [ContextMenu("Bind Module")]
    public void BindModule()
    {
        ResolveInternalReferences();
        EnsureInternalComponents();
        InjectReferences();
    }

    [ContextMenu("Load Mock Equipment")]
    public void LoadMockEquipment()
    {
        BindModule();
        mockLoader?.LoadMock();
        saveDirtyTracker?.ClearDirty();
        RefreshModule();
    }

    [ContextMenu("Refresh Module")]
    public void RefreshModule()
    {
        if (equipmentWindowController != null)
        {
            equipmentWindowController.Refresh();
        }

        if (detailPanelController != null)
        {
            detailPanelController.RefreshCurrentDetail();
        }

        RefreshCurrencyUI();
        RefreshCombatPowerUI();
    }

    public void Open()
    {
        BindModule();

        if (equipmentWindowController != null)
        {
            equipmentWindowController.Open();
            SetEquipmentWindowVisible(true);
            return;
        }

        SetEquipmentWindowVisible(true);
    }

    public void Close()
    {
        if (detailPanelController != null)
        {
            detailPanelController.Close();
        }

        if (equipmentWindowController != null)
        {
            equipmentWindowController.Close();
            SetEquipmentWindowVisible(false);
            return;
        }

        SetEquipmentWindowVisible(false);
    }

    private void ResolveInternalReferences()
    {
        equipmentWindowRoot ??= FindDirectChildGameObject(IsEquipmentWindowRootName);
        resourceBarRoot ??= FindDirectChildGameObject(name => name.Contains("ResourceBar"));
        detailPopupRoot ??= FindDirectChildGameObject(name => name.Contains("Detail"));

        equipmentState ??= GetComponentInChildren<EquipmentRuntimeState>(true);
        statProvider ??= GetComponentInChildren<EquipmentStatProvider>(true);
        mockLoader ??= GetComponentInChildren<MockEquipmentDatabaseLoader>(true);
        iconResolver ??= GetComponentInChildren<EquipmentIconResolver>(true);
        saveDirtyTracker ??= GetComponentInChildren<EquipmentSaveDirtyTracker>(true);
        equipmentWindowController ??= GetComponentInChildren<ModularEquipmentWindowController>(true);
        detailPanelController ??= GetComponentInChildren<ModularEquipmentDetailPanelController>(true);
        ResolveCombatPowerReferences();
        ResolveCurrencyTexts();
    }

    private void EnsureInternalComponents()
    {
        GameObject stateRoot = FindDirectChildGameObject(name => name == "EquipmentState");
        GameObject managerRoot = FindDirectChildGameObject(name => name == "EquipmentManager");
        GameObject detailControllerRoot = FindDirectChildGameObject(name => name == "EquipmentDetailPanelController");

        if (equipmentState == null)
        {
            GameObject target = stateRoot != null ? stateRoot : gameObject;
            equipmentState = target.GetComponent<EquipmentRuntimeState>();
            if (equipmentState == null)
            {
                equipmentState = target.AddComponent<EquipmentRuntimeState>();
            }
        }

        if (mockLoader == null && stateRoot != null)
        {
            mockLoader = stateRoot.GetComponent<MockEquipmentDatabaseLoader>();
            if (mockLoader == null)
            {
                mockLoader = stateRoot.AddComponent<MockEquipmentDatabaseLoader>();
            }
        }

        if (statProvider == null)
        {
            statProvider = GetComponent<EquipmentStatProvider>();
            if (statProvider == null)
            {
                statProvider = gameObject.AddComponent<EquipmentStatProvider>();
            }
        }

        if (saveDirtyTracker == null && stateRoot != null)
        {
            saveDirtyTracker = stateRoot.GetComponent<EquipmentSaveDirtyTracker>();
            if (saveDirtyTracker == null)
            {
                saveDirtyTracker = stateRoot.AddComponent<EquipmentSaveDirtyTracker>();
            }
        }

        if (iconResolver == null)
        {
            iconResolver = GetComponent<EquipmentIconResolver>();
            if (iconResolver == null)
            {
                iconResolver = gameObject.AddComponent<EquipmentIconResolver>();
            }
        }

        if (equipmentWindowController == null)
        {
            GameObject target = managerRoot != null ? managerRoot : equipmentWindowRoot;
            if (target != null)
            {
                equipmentWindowController = target.GetComponent<ModularEquipmentWindowController>();
                if (equipmentWindowController == null)
                {
                    equipmentWindowController = target.AddComponent<ModularEquipmentWindowController>();
                }
            }
        }

        if (detailPanelController == null)
        {
            GameObject target = detailControllerRoot != null ? detailControllerRoot : detailPopupRoot;
            if (target != null)
            {
                detailPanelController = target.GetComponent<ModularEquipmentDetailPanelController>();
                if (detailPanelController == null)
                {
                    detailPanelController = target.AddComponent<ModularEquipmentDetailPanelController>();
                }
            }
        }
    }

    private void InjectReferences()
    {
        statProvider?.Configure(equipmentState);

        if (detailPanelController != null)
        {
            detailPanelController.Configure(detailPopupRoot, equipmentState, runtimeData, iconResolver);
        }

        if (equipmentWindowController != null)
        {
            equipmentWindowController.Configure(
                equipmentWindowRoot,
                equipmentState,
                runtimeData,
                detailPanelController,
                iconResolver);
        }

        SubscribeCurrency();
        SubscribeCombatPower();
        RefreshCurrencyUI();
        RefreshCombatPowerUI();
    }

    private void ApplyStartupVisibility()
    {
        if (hideDetailPopupOnStart && detailPopupRoot != null)
        {
            detailPopupRoot.SetActive(false);
        }

        if (hideEquipmentWindowOnStart)
        {
            SetEquipmentWindowVisible(false);
        }
    }

    private void SetEquipmentWindowVisible(bool visible)
    {
        if (equipmentWindowRoot != null)
        {
            equipmentWindowRoot.SetActive(visible);
        }

        if (resourceBarRoot != null)
        {
            resourceBarRoot.SetActive(visible);
        }
    }

    private void RefreshCurrencyUI()
    {
        if (runtimeData == null)
        {
            return;
        }

        if (goldText != null)
        {
            goldText.text = runtimeData.GetGold().ToString();
        }

        if (gemText != null)
        {
            gemText.text = runtimeData.GetGem().ToString();
        }
    }

    private void RefreshCombatPowerUI()
    {
        if (combatPowerText == null)
        {
            return;
        }

        ResolveCombatPowerReferences();

        if (combatPowerData == null)
        {
            return;
        }

        combatPowerText.text = StatUI_CombatPower.FormatCombatPower(combatPowerData.currentCombatPower);
    }

    private void ResolveCombatPowerReferences()
    {
        if (combatPowerData == null)
        {
            combatPowerData = FindAnyObjectByType<CombatPowerData>();
        }

        if (playerStatController == null)
        {
            playerStatController = FindAnyObjectByType<PlayerStatController>();
        }
    }

    private void SubscribeCombatPower()
    {
        ResolveCombatPowerReferences();

        if (playerStatController != null)
        {
            playerStatController.OnStatsRecalculated -= RefreshCombatPowerUI;
            playerStatController.OnStatsRecalculated += RefreshCombatPowerUI;
        }
    }

    private void UnsubscribeCombatPower()
    {
        if (playerStatController != null)
        {
            playerStatController.OnStatsRecalculated -= RefreshCombatPowerUI;
        }
    }

    private void SubscribeCurrency()
    {
        if (runtimeData == null || subscribedRuntimeData == runtimeData)
        {
            return;
        }

        UnsubscribeCurrency();
        runtimeData.OnDataChanged += RefreshCurrencyUI;
        subscribedRuntimeData = runtimeData;
    }

    private void UnsubscribeCurrency()
    {
        if (subscribedRuntimeData == null)
        {
            return;
        }

        subscribedRuntimeData.OnDataChanged -= RefreshCurrencyUI;
        subscribedRuntimeData = null;
    }

    private void ResolveCurrencyTexts()
    {
        if (resourceBarRoot == null || goldText != null && gemText != null)
        {
            return;
        }

        TMP_Text[] texts = resourceBarRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            if (goldText == null && HasAncestorName(text.transform, "ResourceBar_Coin", "Gold"))
            {
                goldText = text;
                continue;
            }

            if (gemText == null && HasAncestorName(text.transform, "ResourceBar_Gem", "Gem"))
            {
                gemText = text;
            }
        }
    }

    private static bool HasAncestorName(Transform target, string exactName, string fallbackName)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            string objectName = current.name;
            if (objectName == exactName)
            {
                return true;
            }

            if (objectName.Contains("GemStone"))
            {
                return false;
            }

            if (objectName.Contains(fallbackName))
            {
                return true;
            }
        }

        return false;
    }

    private GameObject FindDirectChildGameObject(System.Predicate<string> namePredicate)
    {
        if (namePredicate == null)
        {
            return null;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && namePredicate(child.name))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static bool IsEquipmentWindowRootName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        return objectName.Contains("Equipment") &&
               !objectName.Contains("Detail") &&
               objectName != "EquipmentModule" &&
               objectName != "EquipmentState" &&
               objectName != "EquipmentManager" &&
               objectName != "EquipmentDetailPanelController";
    }
}
