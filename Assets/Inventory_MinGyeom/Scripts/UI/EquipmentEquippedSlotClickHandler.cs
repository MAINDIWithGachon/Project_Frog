using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장착 슬롯 클릭 시 장착 중인 아이템 상세를 열거나, 비어 있으면 해당 카테고리 탭으로 이동합니다.
/// </summary>
public class EquipmentEquippedSlotClickHandler : MonoBehaviour
{
    [SerializeField] private EquipmentCategory category;
    [SerializeField] private EquipmentPrototypeState equipmentState;
    [SerializeField] private EquipmentCategoryTabController categoryTabController;
    [SerializeField] private EquipmentDetailPanelController detailPanelController;
    [SerializeField] private Button targetButton;

    private bool isBound;

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButton();
    }

    public void Configure(
        EquipmentCategory slotCategory,
        EquipmentPrototypeState state,
        EquipmentCategoryTabController tabController,
        EquipmentDetailPanelController panelController,
        Button button)
    {
        category = slotCategory;
        equipmentState = state;
        categoryTabController = tabController;
        detailPanelController = panelController;
        targetButton = button;

        BindButton(true);
    }

    private void HandleClick()
    {
        ResolveReferences();

        if (equipmentState != null && equipmentState.GetEquippedDefinition(category) != null)
        {
            detailPanelController?.OpenDetailPanel(category);
            return;
        }

        if (equipmentState != null && equipmentState.HasOwnedItemInCategory(category))
        {
            categoryTabController?.SelectCategory(category);
        }
    }

    private void ResolveReferences()
    {
        equipmentState ??= GetComponentInParent<EquipmentPrototypeState>(true);
        equipmentState ??= FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);

        categoryTabController ??= GetComponentInParent<EquipmentCategoryTabController>(true);
        categoryTabController ??= FindFirstObjectByType<EquipmentCategoryTabController>(FindObjectsInactive.Include);

        detailPanelController ??= GetComponentInParent<EquipmentDetailPanelController>(true);
        detailPanelController ??= FindFirstObjectByType<EquipmentDetailPanelController>(FindObjectsInactive.Include);

        targetButton ??= GetComponent<Button>();
    }

    private void BindButton(bool forceRebind = false)
    {
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
