using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 테스트용 장비 인벤토리 씬에서 아이템 상세 팝업과 닫기 버튼을 자동으로 연결합니다.
/// </summary>
public static class EquipmentInventoryTestSceneAutoBinder
{
    private const bool AutoBindEnabled = false;
    private const string TargetSceneName = "EquipmentInventory_TestScene";
    private const string MainPopupObjectName = "Popup_Box_03_BasePrefab";
    private const string DetailPopupObjectName = "Character_Hero_Item_Detail";
    private const string MainPopupOpenButtonName = "EquipmentsOpen";
    private const string CloseButtonName = "Button_Close_01";
    private const string DimmedObjectName = "Dimmed";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        if (!AutoBindEnabled)
        {
            return;
        }

      //  SceneManager.sceneLoaded -= OnSceneLoaded;
      //  SceneManager.sceneLoaded += OnSceneLoaded;

       // TryBind(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBind(scene);
    }

    private static void TryBind(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TargetSceneName)
        {
            return;
        }

        GameObject mainPopup = FindObjectByName(MainPopupObjectName);
        GameObject detailPopup = FindObjectByName(DetailPopupObjectName);

        if (mainPopup == null)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryTestSceneAutoBinder)}] Could not find '{MainPopupObjectName}'.");
        }

        if (detailPopup == null)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryTestSceneAutoBinder)}] Could not find '{DetailPopupObjectName}'.");
        }
        else
        {
            detailPopup.SetActive(false);
        }

        BindOpenButton(mainPopup, detailPopup);
        BindInventoryButtons(detailPopup);
        BindCloseTargets(mainPopup, CloseMainPopup);
        BindCloseTargets(detailPopup, CloseDetailPopup);

        void CloseMainPopup()
        {
            if (detailPopup != null)
            {
                detailPopup.SetActive(false);
            }

            if (mainPopup != null)
            {
                mainPopup.SetActive(false);
            }
        }

        void CloseDetailPopup()
        {
            if (detailPopup != null)
            {
                detailPopup.SetActive(false);
            }
        }
    }

    private static void BindOpenButton(GameObject mainPopup, GameObject detailPopup)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int boundCount = 0;

        foreach (Button button in buttons)
        {
            if (button == null || button.name != MainPopupOpenButtonName)
            {
                continue;
            }

            button.onClick.RemoveListener(OpenMainPopup);
            button.onClick.AddListener(OpenMainPopup);
            boundCount++;
        }

        if (boundCount == 0)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryTestSceneAutoBinder)}] Could not find any '{MainPopupOpenButtonName}' buttons.");
        }

        void OpenMainPopup()
        {
            if (detailPopup != null)
            {
                detailPopup.SetActive(false);
            }

            if (mainPopup != null)
            {
                mainPopup.SetActive(true);
            }
        }
    }

    private static void BindInventoryButtons(GameObject detailPopup)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int boundCount = 0;

        foreach (Button button in buttons)
        {
            if (!ShouldBindEquipmentButton(button))
            {
                continue;
            }

            button.onClick.RemoveListener(OpenDetailPopup);
            button.onClick.AddListener(OpenDetailPopup);
            boundCount++;
        }

        if (boundCount == 0)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryTestSceneAutoBinder)}] Could not find any equipment item buttons.");
        }

        void OpenDetailPopup()
        {
            if (detailPopup != null)
            {
                detailPopup.SetActive(true);
            }
        }
    }

    private static bool ShouldBindEquipmentButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        if (button.GetComponentInParent<EquipmentListItemView>(true) != null)
        {
            return true;
        }

        return button.GetComponentInParent<EquipmentEquippedSlotBinder>(true) != null;
    }

    private static void BindCloseTargets(GameObject popupRoot, UnityAction closeAction)
    {
        if (popupRoot == null || closeAction == null)
        {
            return;
        }

        int bindCount = 0;
        bindCount += BindButtonTarget(FindDescendantByName(popupRoot.transform, CloseButtonName), closeAction);
        bindCount += BindGraphicTarget(FindDescendantByName(popupRoot.transform, DimmedObjectName), closeAction);

        if (bindCount == 0)
        {
            Debug.LogWarning($"[{nameof(EquipmentInventoryTestSceneAutoBinder)}] Could not find close targets under '{popupRoot.name}'.");
        }
    }

    private static int BindButtonTarget(Transform target, UnityAction closeAction)
    {
        if (target == null)
        {
            return 0;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            return 0;
        }

        button.onClick.RemoveListener(closeAction);
        button.onClick.AddListener(closeAction);
        return 1;
    }

    private static int BindGraphicTarget(Transform target, UnityAction closeAction)
    {
        if (target == null)
        {
            return 0;
        }

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic == null)
        {
            return 0;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = graphic;
        }

        button.onClick.RemoveListener(closeAction);
        button.onClick.AddListener(closeAction);
        return 1;
    }

    private static GameObject FindObjectByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform transform in transforms)
        {
            if (transform != null && transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
