using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Single entry point for the modular equipment UI.
/// External scenes should only need to assign RuntimeData here.
/// </summary>
public class EquipmentModuleRoot : MonoBehaviour
{
    [Header("External Input")]
    [SerializeField] private RuntimeData runtimeData;

    [Header("Local Test Data")]
    [SerializeField] private EquipmentDatabase defaultEquipmentDatabase;
    [SerializeField] private EquipmentUpgradeRuleDatabase defaultUpgradeRuleDatabase;

    [Header("Internal Roots")]
    [SerializeField] private GameObject equipmentWindowRoot;
    [SerializeField] private GameObject detailPopupRoot;

    [Header("Internal Components")]
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private ModularEquipmentWindowController equipmentWindowController;
    [SerializeField] private ModularEquipmentDetailPanelController detailPanelController;

    [Header("Startup")]
    [SerializeField] private bool bindOnAwake = true;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private bool hideEquipmentWindowOnStart = true;
    [SerializeField] private bool hideDetailPopupOnStart = true;

    public RuntimeData RuntimeData => runtimeData;
    public EquipmentPrototypeState EquipmentState => equipmentState;

    private void Awake()
    {
        if (bindOnAwake)
        {
            BindModule();
        }
    }

    private void Start()
    {
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

        equipmentState ??= GetComponentInChildren<EquipmentPrototypeState>(true);
        equipmentWindowController ??= GetComponentInChildren<ModularEquipmentWindowController>(true);
        detailPanelController ??= GetComponentInChildren<ModularEquipmentDetailPanelController>(true);
    }

    private void EnsureInternalComponents()
    {
        GameObject stateRoot = FindDirectChildGameObject(name => name == "EquipmentState");
        GameObject managerRoot = FindDirectChildGameObject(name => name == "EquipmentManager");
        GameObject detailControllerRoot = FindDirectChildGameObject(name => name == "EquipmentDetailPanelController");

        if (equipmentState == null && stateRoot != null)
        {
            equipmentState = stateRoot.GetComponent<EquipmentPrototypeState>();
            if (equipmentState == null)
            {
                equipmentState = stateRoot.AddComponent<EquipmentPrototypeState>();
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
        if (equipmentState != null)
        {
            SetPrivateField(equipmentState, "equipmentDatabase", defaultEquipmentDatabase);
            SetPrivateField(equipmentState, "upgradeRuleDatabase", defaultUpgradeRuleDatabase);
        }

        if (detailPanelController != null)
        {
            detailPanelController.Configure(detailPopupRoot, equipmentState, runtimeData);
        }

        if (equipmentWindowController != null)
        {
            equipmentWindowController.Configure(
                equipmentWindowRoot,
                equipmentState,
                runtimeData,
                detailPanelController);
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

    private GameObject FindDirectChildGameObject(Predicate<string> namePredicate)
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

    private static bool IsEquipmentWindowRootName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.Contains("Equipment") &&
               !name.Contains("Detail") &&
               name != "EquipmentModule" &&
               name != "EquipmentState" &&
               name != "EquipmentManager" &&
               name != "EquipmentDetailPanelController";
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
        {
            return;
        }

        Type type = target.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            type = type.BaseType;
        }
    }
}
