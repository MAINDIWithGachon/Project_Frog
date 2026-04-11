using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controls the main equipment popup and the item detail popup using inspector-assigned references.
/// </summary>
public class EquipmentPopupManager : MonoBehaviour
{
    [Serializable]
    private class ItemButtonBinding
    {
        public Button button;
        public EquipmentListItemView itemView;
    }

    [Serializable]
    private class EquippedSlotBinding
    {
        public Button button;
        public EquipmentCategory category;
    }

    [Header("Popup Roots")]
    [SerializeField] private GameObject mainPopup;
    [SerializeField] private GameObject detailPopup;
    [SerializeField] private EquipmentDetailPanelController detailPanelController;

    [Header("Initial State")]
    [SerializeField] private bool hideMainPopupOnStart = true;
    [SerializeField] private bool hideDetailPopupOnStart = true;
    [SerializeField] private bool closeDetailWhenMainOpens = true;
    [SerializeField] private bool keepMainOpenWhenDetailOpens = true;

    [Header("Main Popup Buttons")]
    [SerializeField] private Button[] mainOpenButtons;
    [SerializeField] private Button[] mainCloseButtons;
    [SerializeField] private Graphic[] mainCloseAreas;

    [Header("Detail Popup Buttons")]
    [SerializeField] private Button[] detailCloseButtons;
    [SerializeField] private Graphic[] detailCloseAreas;

    [Header("Detail Open Bindings")]
    [SerializeField] private ItemButtonBinding[] inventoryItemButtons;
    [SerializeField] private EquippedSlotBinding[] equippedSlotButtons;

    [Header("Combat Power UI")]
    [SerializeField] private TMP_Text combatPowerText;
    [SerializeField] private CombatPowerData combatPowerData;
    [SerializeField] private PlayerStatController playerStatController;

    private void Awake()
    {
        ApplyInitialState();
    }

    private void OnEnable()
    {
        ResolveCombatPowerReferences();

        if (playerStatController != null)
            playerStatController.OnStatsRecalculated += RefreshCombatPowerUI;

        RefreshCombatPowerUI();
    }

    private void OnDisable()
    {
        if (playerStatController != null)
            playerStatController.OnStatsRecalculated -= RefreshCombatPowerUI;
    }

    private void Start()
    {
        ResolveReferences();
        BindAll();
        RefreshCombatPowerUI();
    }

    [ContextMenu("Bind All")]
    public void BindAll()
    {
        ResolveReferences();

        BindButtons(mainOpenButtons, OpenMainPopup);
        BindButtons(mainCloseButtons, CloseMainPopup);
        BindGraphics(mainCloseAreas, CloseMainPopup);

        BindButtons(detailCloseButtons, CloseDetailPopup);
        BindGraphics(detailCloseAreas, CloseDetailPopup);

        BindInventoryItemButtons();
        BindEquippedSlotButtons();
    }

    public void OpenMainPopup()
    {
        ResolveReferences();

        if (closeDetailWhenMainOpens)
        {
            CloseDetailPopup();
        }

        if (mainPopup != null)
        {
            mainPopup.SetActive(true);
        }

        RefreshCombatPowerUI();
    }

    public void CloseMainPopup()
    {
        CloseDetailPopup();

        if (mainPopup != null)
        {
            mainPopup.SetActive(false);
        }
    }

    public void ToggleMainPopup()
    {
        if (mainPopup == null)
        {
            return;
        }

        if (mainPopup.activeSelf)
        {
            CloseMainPopup();
            return;
        }

        OpenMainPopup();
    }

    public void OpenDetailPopup()
    {
        ResolveReferences();

        if (keepMainOpenWhenDetailOpens && mainPopup != null)
        {
            mainPopup.SetActive(true);
        }

        if (detailPanelController != null)
        {
            detailPanelController.OpenDetailPanel();
            return;
        }

        if (detailPopup != null)
        {
            detailPopup.SetActive(true);
        }
    }

    public void OpenDetailForItem(EquipmentListItemView itemView)
    {
        ResolveReferences();

        if (itemView == null)
        {
            return;
        }

        if (keepMainOpenWhenDetailOpens && mainPopup != null)
        {
            mainPopup.SetActive(true);
        }

        if (detailPanelController != null)
        {
            detailPanelController.OpenDetailPanel(itemView);
            return;
        }

        if (detailPopup != null)
        {
            detailPopup.SetActive(true);
        }
    }

    public void OpenDetailForCategory(EquipmentCategory category)
    {
        ResolveReferences();

        if (keepMainOpenWhenDetailOpens && mainPopup != null)
        {
            mainPopup.SetActive(true);
        }

        if (detailPanelController != null)
        {
            detailPanelController.OpenDetailPanel(category);
            return;
        }

        if (detailPopup != null)
        {
            detailPopup.SetActive(true);
        }
    }

    public void CloseDetailPopup()
    {
        ResolveReferences();

        if (detailPanelController != null)
        {
            detailPanelController.CloseDetailPanel();
            return;
        }

        if (detailPopup != null)
        {
            detailPopup.SetActive(false);
        }
    }

    public void CloseAllPopups()
    {
        CloseDetailPopup();

        if (mainPopup != null)
        {
            mainPopup.SetActive(false);
        }
    }

    public void RefreshCombatPowerUI()
    {
        ResolveCombatPowerReferences();

        if (combatPowerText == null || combatPowerData == null)
        {
            return;
        }

        combatPowerText.text = StatUI_CombatPower.FormatCombatPower(combatPowerData.currentCombatPower);
    }

    private void ApplyInitialState()
    {
        if (hideMainPopupOnStart && mainPopup != null)
        {
            mainPopup.SetActive(false);
        }

        if (hideDetailPopupOnStart && detailPopup != null)
        {
            detailPopup.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        detailPanelController ??= GetComponentInChildren<EquipmentDetailPanelController>(true);
        detailPanelController ??= GetComponentInParent<EquipmentDetailPanelController>(true);
        detailPanelController ??= FindFirstObjectByType<EquipmentDetailPanelController>(FindObjectsInactive.Include);

        ResolveCombatPowerReferences();
    }

    private void ResolveCombatPowerReferences()
    {
        if (combatPowerText == null && mainPopup != null)
        {
            Transform totalStat = mainPopup.transform.Find("Bottom/Totalstat");
            if (totalStat != null)
            {
                combatPowerText = totalStat.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (combatPowerData == null)
            combatPowerData = FindAnyObjectByType<CombatPowerData>();

        if (playerStatController == null)
            playerStatController = FindAnyObjectByType<PlayerStatController>();
    }

    private void BindInventoryItemButtons()
    {
        if (inventoryItemButtons == null)
        {
            return;
        }

        for (int i = 0; i < inventoryItemButtons.Length; i++)
        {
            ItemButtonBinding binding = inventoryItemButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            EquipmentPopupManagerItemButtonRelay relay =
                binding.button.GetComponent<EquipmentPopupManagerItemButtonRelay>();
            if (relay == null)
            {
                relay = binding.button.gameObject.AddComponent<EquipmentPopupManagerItemButtonRelay>();
            }

            relay.Configure(this, binding.itemView, binding.button);
        }
    }

    private void BindEquippedSlotButtons()
    {
        if (equippedSlotButtons == null)
        {
            return;
        }

        for (int i = 0; i < equippedSlotButtons.Length; i++)
        {
            EquippedSlotBinding binding = equippedSlotButtons[i];
            if (binding == null || binding.button == null)
            {
                continue;
            }

            EquipmentPopupManagerSlotButtonRelay relay =
                binding.button.GetComponent<EquipmentPopupManagerSlotButtonRelay>();
            if (relay == null)
            {
                relay = binding.button.gameObject.AddComponent<EquipmentPopupManagerSlotButtonRelay>();
            }

            relay.Configure(this, binding.category, binding.button);
        }
    }

    private static void BindButtons(Button[] buttons, UnityAction action)
    {
        if (buttons == null || action == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

    private static void BindGraphics(Graphic[] graphics, UnityAction action)
    {
        if (graphics == null || action == null)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            Button button = graphic.GetComponent<Button>();
            if (button == null)
            {
                button = graphic.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }

            if (button.targetGraphic == null)
            {
                button.targetGraphic = graphic;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}

public class EquipmentPopupManagerItemButtonRelay : MonoBehaviour
{
    [SerializeField] private EquipmentPopupManager manager;
    [SerializeField] private EquipmentListItemView itemView;
    [SerializeField] private Button targetButton;

    private bool isBound;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    public void Configure(EquipmentPopupManager popupManager, EquipmentListItemView view, Button button)
    {
        manager = popupManager;
        itemView = view;
        targetButton = button;
        BindButton(true);
    }

    private void HandleClick()
    {
        manager?.OpenDetailForItem(itemView);
    }

    private void BindButton(bool forceRebind = false)
    {
        targetButton ??= GetComponent<Button>();

        if (targetButton == null)
        {
            return;
        }

        if (forceRebind || isBound)
        {
            targetButton.onClick.RemoveListener(HandleClick);
            isBound = false;
        }

        if (isBound)
        {
            return;
        }

        targetButton.onClick.AddListener(HandleClick);
        isBound = true;
    }
}

public class EquipmentPopupManagerSlotButtonRelay : MonoBehaviour
{
    [SerializeField] private EquipmentPopupManager manager;
    [SerializeField] private EquipmentCategory category;
    [SerializeField] private Button targetButton;

    private bool isBound;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    public void Configure(EquipmentPopupManager popupManager, EquipmentCategory slotCategory, Button button)
    {
        manager = popupManager;
        category = slotCategory;
        targetButton = button;
        BindButton(true);
    }

    private void HandleClick()
    {
        manager?.OpenDetailForCategory(category);
    }

    private void BindButton(bool forceRebind = false)
    {
        targetButton ??= GetComponent<Button>();

        if (targetButton == null)
        {
            return;
        }

        if (forceRebind || isBound)
        {
            targetButton.onClick.RemoveListener(HandleClick);
            isBound = false;
        }

        if (isBound)
        {
            return;
        }

        targetButton.onClick.AddListener(HandleClick);
        isBound = true;
    }
}
