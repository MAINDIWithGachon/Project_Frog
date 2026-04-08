using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카테고리 탭 버튼을 인벤토리 리스트와 연결하고, 선택된 탭의 포커스 표시를 관리합니다.
/// </summary>
public class EquipmentCategoryTabController : MonoBehaviour
{
    [SerializeField] private EquipmentInventoryListBinder inventoryListBinder;
    [SerializeField] private Transform tabRoot;
    [SerializeField] private EquipmentCategory defaultCategory = EquipmentCategory.Weapon;

    private readonly List<GameObject> focusObjects = new();
    private bool isBound;

    // 현재 프리팹에서는 버튼 순서 자체가 카테고리 순서이므로 이 배열 기준으로 매칭합니다.
    private static readonly EquipmentCategory[] CategoryOrder =
    {
        EquipmentCategory.Weapon,
        EquipmentCategory.Hat,
        EquipmentCategory.Ring,
        EquipmentCategory.Armor,
        EquipmentCategory.Necklace,
        EquipmentCategory.Shoes
    };

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        SelectCategory(defaultCategory);
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        SelectCategory(defaultCategory);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    public void SelectCategory(EquipmentCategory category)
    {
        // 탭을 선택하면 먼저 리스트를 갱신하고, 그 다음 해당 탭의 포커스 표시를 켭니다.
        if (inventoryListBinder != null)
        {
            inventoryListBinder.SetCategory(category);
        }

        for (int i = 0; i < focusObjects.Count; i++)
        {
            GameObject focusObject = focusObjects[i];
            if (focusObject == null)
            {
                continue;
            }

            bool isSelected = i < CategoryOrder.Length && CategoryOrder[i] == category;
            focusObject.SetActive(isSelected);
        }
    }

    private void ResolveReferences()
    {
        // 프리팹 배치 시 수동 연결을 줄이기 위해 자주 쓰는 참조는 자동으로 찾아옵니다.
        if (inventoryListBinder == null)
        {
            inventoryListBinder = GetComponent<EquipmentInventoryListBinder>();
        }

        if (tabRoot == null)
        {
            tabRoot = FindDescendantByName(transform, "Tab_02_BoxMenu_Icon");
        }
    }

    private void BindButtons()
    {
        if (isBound || tabRoot == null)
        {
            return;
        }

        Button[] buttons = tabRoot.GetComponentsInChildren<Button>(true);
        int count = Mathf.Min(buttons.Length, CategoryOrder.Length);

        focusObjects.Clear();

        for (int i = 0; i < count; i++)
        {
            Button button = buttons[i];
            EquipmentCategory category = CategoryOrder[i];
            GameObject focusObject = FindDescendantByName(button.transform, "Focus")?.gameObject;

            // 각 버튼 클릭 시 자신에게 대응되는 카테고리를 기억하도록 로컬 변수를 캡처합니다.
            focusObjects.Add(focusObject);
            button.onClick.AddListener(() => SelectCategory(category));
        }

        isBound = true;
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        // 전체 경로를 하드코딩하지 않기 위해 이름 기준으로 재귀 탐색합니다.
        if (root == null)
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
