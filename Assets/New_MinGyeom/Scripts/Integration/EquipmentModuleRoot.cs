using NewMinGyeom.Backend;
using NewMinGyeom.Equipment;
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
    [SerializeField] private MockEquipmentDatabaseLoader mockLoader;
    [SerializeField] private EquipmentIconResolver iconResolver;
    [SerializeField] private EquipmentSaveDirtyTracker saveDirtyTracker;

    [Header("Internal Roots")]
    [SerializeField] private GameObject equipmentWindowRoot;
    [SerializeField] private GameObject detailPopupRoot;

    [Header("Internal Components")]
    [SerializeField] private ModularEquipmentWindowController equipmentWindowController;
    [SerializeField] private ModularEquipmentDetailPanelController detailPanelController;

    [Header("Startup")]
    [SerializeField] private bool bindOnAwake = true;
    [SerializeField] private bool loadMockOnStart;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool hideEquipmentWindowOnStart = true;
    [SerializeField] private bool hideDetailPopupOnStart = true;

    public RuntimeData RuntimeData => runtimeData;
    public EquipmentRuntimeState EquipmentState => equipmentState;
    public EquipmentIconResolver IconResolver => iconResolver;
    public EquipmentSaveDirtyTracker SaveDirtyTracker => saveDirtyTracker;

    private void Awake()
    {
        if (bindOnAwake)
        {
            BindModule();
        }
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
        runtimeData = data;
        BindModule();
        RefreshModule();
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
    }

    public void Open()
    {
        BindModule();

        if (equipmentWindowController != null)
        {
            equipmentWindowController.Open();
            return;
        }

        if (equipmentWindowRoot != null)
        {
            equipmentWindowRoot.SetActive(true);
        }
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
            return;
        }

        if (equipmentWindowRoot != null)
        {
            equipmentWindowRoot.SetActive(false);
        }
    }

    private void ResolveInternalReferences()
    {
        equipmentWindowRoot ??= FindDirectChildGameObject(IsEquipmentWindowRootName);
        detailPopupRoot ??= FindDirectChildGameObject(name => name.Contains("Detail"));

        equipmentState ??= GetComponentInChildren<EquipmentRuntimeState>(true);
        mockLoader ??= GetComponentInChildren<MockEquipmentDatabaseLoader>(true);
        iconResolver ??= GetComponentInChildren<EquipmentIconResolver>(true);
        saveDirtyTracker ??= GetComponentInChildren<EquipmentSaveDirtyTracker>(true);
        equipmentWindowController ??= GetComponentInChildren<ModularEquipmentWindowController>(true);
        detailPanelController ??= GetComponentInChildren<ModularEquipmentDetailPanelController>(true);
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
    }

    private void ApplyStartupVisibility()
    {
        if (hideDetailPopupOnStart && detailPopupRoot != null)
        {
            detailPopupRoot.SetActive(false);
        }

        if (hideEquipmentWindowOnStart && equipmentWindowRoot != null)
        {
            equipmentWindowRoot.SetActive(false);
        }
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
