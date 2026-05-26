using NewMinGyeom.Equipment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NewMinGyeom.Gacha
{
    public class GachaModuleRoot : MonoBehaviour
    {
        [Header("External Input")]
        [SerializeField] private RuntimeData runtimeData;

        [Header("Runtime Equipment")]
        [SerializeField] private EquipmentRuntimeState equipmentState;
        [SerializeField] private EquipmentIconResolver iconResolver;
        [SerializeField] private EquipmentGachaSettings settings;

        [Header("Internal Components")]
        [SerializeField] private RuntimeEquipmentGachaService gachaService;
        [SerializeField] private RuntimeGachaManager gachaManager;

        [Header("Draw Buttons")]
        [SerializeField] private Button drawOneButton;
        [SerializeField] private Button drawTenButton;

        [Header("Result Popup")]
        [SerializeField] private GameObject resultPopupRoot;
        [SerializeField] private Transform resultContentRoot;
        [SerializeField] private GameObject resultItemFramePrefab;
        [SerializeField] private TMP_Text resultCountText;
        [SerializeField] private Button resultDrawOneButton;
        [SerializeField] private Button resultDrawTenButton;
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private Button resultDimmedButton;
        [SerializeField] private float resultRevealInterval = 0.06f;

        [Header("Failure Popup")]
        [SerializeField] private GameObject insufficientCurrencyRoot;

        [Header("Startup")]
        [SerializeField] private bool bindOnAwake = true;
        [SerializeField] private bool hideResultPopupOnStart = true;
        [SerializeField] private bool hideInsufficientCurrencyOnStart = true;

        public RuntimeEquipmentGachaService GachaService => gachaService;
        public RuntimeGachaManager GachaManager => gachaManager;
        public RuntimeData RuntimeData => runtimeData;
        public EquipmentRuntimeState EquipmentState => equipmentState;
        public EquipmentIconResolver IconResolver => iconResolver;
        public EquipmentGachaSettings Settings => settings;

        private bool missingDependencyWarningLogged;

        private void Awake()
        {
            if (bindOnAwake)
            {
                BindModule();
            }
        }

        private void Start()
        {
            ApplyStartupVisibility();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        public void Initialize(RuntimeData data)
        {
            runtimeData = data;
            BindModule();
        }

        [ContextMenu("Bind Module")]
        public void BindModule()
        {
            ResolveReferences();
            EnsureInternalComponents();
            InjectReferences();
            RegisterButtonListeners();
        }

        public void DrawOne()
        {
            Draw(1);
        }

        public void DrawTen()
        {
            Draw(10);
        }

        public void Draw(int drawCount)
        {
            BindModule();

            if (!HasRequiredExternalReferences())
            {
                LogMissingExternalReferences();
                return;
            }

            if (gachaManager == null)
            {
                Debug.LogWarning("[GachaModuleRoot] RuntimeGachaManager is not assigned.", this);
                return;
            }

            gachaManager.Draw(drawCount);
        }

        public void CloseResultPopup()
        {
            gachaManager?.CloseResultPopup();
        }

        public void CloseInsufficientCurrencyPopup()
        {
            gachaManager?.CloseInsufficientCurrencyPopup();
        }

        private void ResolveReferences()
        {
            EquipmentModuleRoot equipmentModuleRoot = GetComponentInParent<EquipmentModuleRoot>();

            gachaService ??= GetComponent<RuntimeEquipmentGachaService>();
            gachaService ??= GetComponentInChildren<RuntimeEquipmentGachaService>(true);
            gachaManager ??= GetComponent<RuntimeGachaManager>();
            gachaManager ??= GetComponentInChildren<RuntimeGachaManager>(true);

            if (runtimeData == null)
            {
                runtimeData = equipmentModuleRoot != null ? equipmentModuleRoot.RuntimeData : GetComponentInParent<RuntimeData>();
            }

            if (equipmentState == null)
            {
                equipmentState = equipmentModuleRoot != null
                    ? equipmentModuleRoot.EquipmentState
                    : GetComponentInParent<EquipmentRuntimeState>();
            }

            if (iconResolver == null)
            {
                iconResolver = equipmentModuleRoot != null
                    ? equipmentModuleRoot.IconResolver
                    : GetComponentInParent<EquipmentIconResolver>();
            }

            if (settings == null && gachaService != null)
            {
                settings = gachaService.Settings;
            }

            if (resultPopupRoot == null)
            {
                resultPopupRoot = FindDescendantByName(transform, "Gacha_Summon_Result")?.gameObject;
            }

            Transform resultRoot = resultPopupRoot != null ? resultPopupRoot.transform : transform;
            drawOneButton ??= FindButtonByAnyName(transform, "DrawOne", "Draw_One", "Button_DrawOne", "Gacha_One");
            drawTenButton ??= FindButtonByAnyName(transform, "DrawTen", "Draw_Ten", "Button_DrawTen", "Gacha_Ten", "GachaTen");
            resultContentRoot ??= FindDescendantByName(resultRoot, "Content");
            resultDrawOneButton ??= FindButtonByName(resultRoot, "DrawAgain_One");
            resultDrawTenButton ??= FindButtonByName(resultRoot, "DrawAgain_Ten");
            resultCloseButton ??= FindButtonByName(resultRoot, "Text_TouchContionue");
            resultDimmedButton ??= FindButtonByName(resultRoot, "Dimmed");
        }

        private void EnsureInternalComponents()
        {
            if (gachaService == null)
            {
                gachaService = gameObject.AddComponent<RuntimeEquipmentGachaService>();
            }

            if (gachaManager == null)
            {
                gachaManager = gameObject.AddComponent<RuntimeGachaManager>();
            }
        }

        private void InjectReferences()
        {
            if (gachaService != null)
            {
                gachaService.Configure(settings, equipmentState, runtimeData);
            }

            if (gachaManager != null)
            {
                gachaManager.Configure(
                    gachaService,
                    iconResolver,
                    drawOneButton,
                    drawTenButton,
                    resultDrawOneButton,
                    resultDrawTenButton,
                    resultPopupRoot,
                    resultContentRoot,
                    resultItemFramePrefab,
                    resultCountText,
                    resultRevealInterval,
                    insufficientCurrencyRoot);
            }
        }

        private void RegisterButtonListeners()
        {
            RegisterButton(resultCloseButton, CloseResultPopup);
            RegisterButton(resultDimmedButton, CloseResultPopup);
        }

        private bool HasRequiredExternalReferences()
        {
            return runtimeData != null &&
                   equipmentState != null &&
                   settings != null;
        }

        private void LogMissingExternalReferences()
        {
            if (missingDependencyWarningLogged)
            {
                return;
            }

            missingDependencyWarningLogged = true;
            Debug.LogWarning(
                "[GachaModuleRoot] Missing required external references. Assign RuntimeData, EquipmentRuntimeState, and EquipmentGachaSettings on GachaModuleRoot, or place this module under an EquipmentModuleRoot that provides RuntimeData and EquipmentState.",
                this);
        }

        private void ApplyStartupVisibility()
        {
            if (hideResultPopupOnStart && resultPopupRoot != null)
            {
                resultPopupRoot.SetActive(false);
            }

            if (hideInsufficientCurrencyOnStart && insufficientCurrencyRoot != null)
            {
                insufficientCurrencyRoot.SetActive(false);
            }
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static Button FindButtonByName(Transform root, string targetName)
        {
            Transform found = FindDescendantByName(root, targetName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Button FindButtonByAnyName(Transform root, params string[] targetNames)
        {
            if (targetNames == null)
            {
                return null;
            }

            for (int i = 0; i < targetNames.Length; i++)
            {
                Button button = FindButtonByName(root, targetNames[i]);
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        private static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == targetName)
                {
                    return child;
                }

                Transform found = FindDescendantByName(child, targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

    }
}
