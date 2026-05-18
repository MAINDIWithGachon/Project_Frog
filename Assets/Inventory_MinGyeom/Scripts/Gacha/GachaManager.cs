using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private EquipmentGachaService gachaService;

    [Header("Draw Buttons")]
    [SerializeField] private Button drawOneButton;
    [SerializeField] private Button drawTenButton;

    [Header("Result Popup Draw Buttons")]
    [SerializeField] private Button resultDrawOneButton;
    [SerializeField] private Button resultDrawTenButton;

    [Header("Result Popup")]
    [SerializeField] private GameObject gachaResultRoot;
    [SerializeField] private Transform resultContentRoot;
    [SerializeField] private GameObject resultItemFramePrefab;
    [SerializeField] private TMP_Text resultCountText;
    [SerializeField] private float resultRevealInterval = 0.06f;

    [Header("Failure Popup")]
    [SerializeField] private GameObject insufficientCurrencyRoot;

    public IReadOnlyList<EquipmentGachaResult> LastResults => lastResults;

    private readonly List<GameObject> spawnedResultItems = new();
    private readonly List<EquipmentGachaResult> lastResults = new();
    private Coroutine revealRoutine;

    private void Awake()
    {
        SetPopupActive(gachaResultRoot, false);
        SetPopupActive(insufficientCurrencyRoot, false);
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        StopRevealRoutine();
        UnregisterButtonListeners();
    }

    public void OnClickDrawOne()
    {
        Draw(1);
    }

    public void OnClickDrawTen()
    {
        Draw(10);
    }

    public void Draw(int drawCount)
    {
        SetPopupActive(insufficientCurrencyRoot, false);
        SetButtonsInteractable(false);

        if (gachaService == null)
        {
            Debug.LogWarning("[GachaManager] EquipmentGachaService is not assigned.", this);
            SetButtonsInteractable(true);
            return;
        }

        if (!gachaService.TryDraw(drawCount, out List<EquipmentGachaResult> results))
        {
            lastResults.Clear();
            SetPopupActive(insufficientCurrencyRoot, true);
            SetButtonsInteractable(true);
            return;
        }

        lastResults.Clear();
        lastResults.AddRange(results);
        LogDrawResults(results);
        ShowResults(results);
        SetButtonsInteractable(true);
    }

    public void CloseResultPopup()
    {
        SetPopupActive(gachaResultRoot, false);
    }

    public void CloseInsufficientCurrencyPopup()
    {
        SetPopupActive(insufficientCurrencyRoot, false);
    }

    private void ShowResults(IReadOnlyList<EquipmentGachaResult> results)
    {
        if (resultContentRoot == null)
        {
            Debug.LogWarning("[GachaManager] Result content root is not assigned.", this);
            return;
        }

        if (resultItemFramePrefab == null)
        {
            Debug.LogWarning("[GachaManager] Result item frame prefab is not assigned.", this);
            return;
        }

        StopRevealRoutine();
        ClearSpawnedResultItems();
        SetPopupActive(gachaResultRoot, true);

        if (resultCountText != null)
        {
            resultCountText.text = $"{results.Count} Draw";
        }

        for (int i = 0; i < results.Count; i++)
        {
            EquipmentGachaResult result = results[i];
            GameObject itemObject = Instantiate(resultItemFramePrefab, resultContentRoot);
            spawnedResultItems.Add(itemObject);
            BindResultItem(itemObject, result);
            itemObject.SetActive(false);
        }

        revealRoutine = StartCoroutine(RevealResultItemsSequentially());
    }

    private void BindResultItem(GameObject itemObject, EquipmentGachaResult result)
    {
        if (itemObject == null || result == null || result.definition == null)
        {
            return;
        }

        GachaResultItemView gachaResultItemView = itemObject.GetComponent<GachaResultItemView>();
        if (gachaResultItemView != null)
        {
            gachaResultItemView.Bind(result.equipmentId, gachaService.EquipmentDatabase);
            return;
        }

        EquipmentListItemView equipmentListItemView = itemObject.GetComponent<EquipmentListItemView>();
        if (equipmentListItemView != null)
        {
            equipmentListItemView.SetItemById(
                result.equipmentId,
                Mathf.Max(1, result.currentLevel),
                Mathf.Max(1, result.currentOwnedCount),
                gachaService.EquipmentState,
                gachaService.EquipmentDatabase,
                gachaService.RuntimeData);
            return;
        }

        itemObject.SendMessage("Bind", result, SendMessageOptions.DontRequireReceiver);
    }

    private void LogDrawResults(IReadOnlyList<EquipmentGachaResult> results)
    {
        if (results == null || results.Count == 0)
        {
            Debug.Log("[GachaManager] Draw succeeded but no results were returned.", this);
            return;
        }

        StringBuilder stringBuilder = new();
        for (int i = 0; i < results.Count; i++)
        {
            EquipmentGachaResult result = results[i];
            if (result == null)
            {
                continue;
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(result.equipmentId);
        }

        Debug.Log($"[GachaManager] Draw succeeded ({results.Count}) -> IDs: {stringBuilder}", this);
    }

    private void ClearSpawnedResultItems()
    {
        for (int i = 0; i < spawnedResultItems.Count; i++)
        {
            GameObject spawnedItem = spawnedResultItems[i];
            if (spawnedItem != null)
            {
                Destroy(spawnedItem);
            }
        }

        spawnedResultItems.Clear();
    }

    private IEnumerator RevealResultItemsSequentially()
    {
        for (int i = 0; i < spawnedResultItems.Count; i++)
        {
            GameObject itemObject = spawnedResultItems[i];
            if (itemObject == null)
            {
                continue;
            }

            itemObject.SetActive(true);

            GachaResultItemView itemView = itemObject.GetComponent<GachaResultItemView>();
            if (itemView != null && itemObject.activeInHierarchy)
            {
                itemView.PlayRevealAnimation();
            }

            if (i < spawnedResultItems.Count - 1)
            {
                yield return new WaitForSecondsRealtime(resultRevealInterval);
            }
        }

        revealRoutine = null;
    }

    private void StopRevealRoutine()
    {
        if (revealRoutine == null)
        {
            return;
        }

        StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    private void RegisterButtonListeners()
    {
        if (drawOneButton != null)
        {
            drawOneButton.onClick.RemoveListener(OnClickDrawOne);
            drawOneButton.onClick.AddListener(OnClickDrawOne);
        }

        if (drawTenButton != null)
        {
            drawTenButton.onClick.RemoveListener(OnClickDrawTen);
            drawTenButton.onClick.AddListener(OnClickDrawTen);
        }

        if (resultDrawOneButton != null)
        {
            resultDrawOneButton.onClick.RemoveListener(OnClickDrawOne);
            resultDrawOneButton.onClick.AddListener(OnClickDrawOne);
        }

        if (resultDrawTenButton != null)
        {
            resultDrawTenButton.onClick.RemoveListener(OnClickDrawTen);
            resultDrawTenButton.onClick.AddListener(OnClickDrawTen);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (drawOneButton != null)
        {
            drawOneButton.onClick.RemoveListener(OnClickDrawOne);
        }

        if (drawTenButton != null)
        {
            drawTenButton.onClick.RemoveListener(OnClickDrawTen);
        }

        if (resultDrawOneButton != null)
        {
            resultDrawOneButton.onClick.RemoveListener(OnClickDrawOne);
        }

        if (resultDrawTenButton != null)
        {
            resultDrawTenButton.onClick.RemoveListener(OnClickDrawTen);
        }
    }

    private void SetPopupActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        if (drawOneButton != null)
        {
            drawOneButton.interactable = isInteractable;
        }

        if (drawTenButton != null)
        {
            drawTenButton.interactable = isInteractable;
        }

        if (resultDrawOneButton != null)
        {
            resultDrawOneButton.interactable = isInteractable;
        }

        if (resultDrawTenButton != null)
        {
            resultDrawTenButton.interactable = isInteractable;
        }
    }
}
