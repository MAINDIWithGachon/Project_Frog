using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeGachaManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private RecipeGachaService gachaService;

    [Header("Draw Buttons")]
    [SerializeField] private Button drawOneButton;
    [SerializeField] private Button drawTenButton;

    [Header("Result Popup")]
    [SerializeField] private GameObject gachaResultRoot;
    [SerializeField] private Transform resultContentRoot;
    [SerializeField] private GameObject resultItemFramePrefab;
    [SerializeField] private TMP_Text resultCountText;
    [SerializeField] private float resultRevealInterval = 0.06f;
    [SerializeField] private UIConfettiBurst resultConfetti;
    [SerializeField] private bool closeResultOnPointerRelease = true;

    [Header("Result Particle Prefab Effect")]
    [SerializeField] private ParticleSystem resultParticle;
    [SerializeField] private Camera resultParticleCamera;
    [SerializeField] private bool positionResultParticleByViewport = true;
    [SerializeField] private Vector2 resultParticleViewportPosition = new Vector2(0.5f, 0.68f);
    [SerializeField] private float resultParticleCameraDistance = 10f;
    [SerializeField] private Vector3 resultParticleScale = Vector3.one * 4f;
    [SerializeField] private string resultParticleSortingLayer = "UI";
    [SerializeField] private int resultParticleSortingOrder = 50;

    [Header("Failure Popup")]
    [SerializeField] private GameObject insufficientCurrencyRoot;

    [Header("Recipe List Refresh")]
    [SerializeField] private RecipeListView[] recipeListViews;

    public IReadOnlyList<RecipeGachaResult> LastResults => lastResults;

    private readonly List<GameObject> spawnedResultItems = new List<GameObject>();
    private readonly List<RecipeGachaResult> lastResults = new List<RecipeGachaResult>();
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

    private void Update()
    {
        if (!closeResultOnPointerRelease || gachaResultRoot == null || !gachaResultRoot.activeInHierarchy)
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            CloseResultPopupFromScreenPoint(Input.mousePosition);
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended)
            {
                CloseResultPopupFromScreenPoint(touch.position);
                return;
            }
        }
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
            Debug.LogWarning("[RecipeGachaManager] RecipeGachaService is not assigned.", this);
            SetButtonsInteractable(true);
            return;
        }

        if (!gachaService.TryDraw(drawCount, out List<RecipeGachaResult> results))
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
        RefreshRecipeLists();
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

    private void ShowResults(IReadOnlyList<RecipeGachaResult> results)
    {
        if (resultContentRoot == null)
        {
            Debug.LogWarning("[RecipeGachaManager] Result content root is not assigned.", this);
            return;
        }

        if (resultItemFramePrefab == null)
        {
            Debug.LogWarning("[RecipeGachaManager] Result item frame prefab is not assigned.", this);
            return;
        }

        ClearSpawnedResultItems();
        StopRevealRoutine();

        if (resultCountText != null)
        {
            resultCountText.text = $"{results.Count} Draw";
        }

        for (int i = 0; i < results.Count; i++)
        {
            RecipeGachaResult result = results[i];
            GameObject itemObject = Instantiate(resultItemFramePrefab, resultContentRoot);
            spawnedResultItems.Add(itemObject);
            BindResultItem(itemObject, result);
            itemObject.SetActive(false);
        }

        SetPopupActive(gachaResultRoot, true);
        PlayResultEffects();

        revealRoutine = StartCoroutine(RevealResultItemsSequentially());
    }

    private void PlayResultEffects()
    {
        if (resultConfetti != null)
        {
            resultConfetti.Play();
        }

        PlayResultParticle();
    }

    private void PlayResultParticle()
    {
        if (resultParticle == null)
        {
            return;
        }

        if (positionResultParticleByViewport)
        {
            Camera targetCamera = resultParticleCamera != null ? resultParticleCamera : Camera.main;
            if (targetCamera != null)
            {
                Vector3 viewportPosition = new Vector3(
                    resultParticleViewportPosition.x,
                    resultParticleViewportPosition.y,
                    resultParticleCameraDistance);
                resultParticle.transform.position = targetCamera.ViewportToWorldPoint(viewportPosition);
            }
        }

        resultParticle.transform.localScale = resultParticleScale;
        ApplyResultParticleSorting(resultParticle.transform);
        resultParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        resultParticle.Play(true);
    }

    private void ApplyResultParticleSorting(Transform root)
    {
        if (root == null)
        {
            return;
        }

        ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            ParticleSystemRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.sortingLayerName = resultParticleSortingLayer;
            renderer.sortingOrder = resultParticleSortingOrder;
        }
    }
    private void BindResultItem(GameObject itemObject, RecipeGachaResult result)
    {
        if (itemObject == null || result == null || result.definition == null)
        {
            return;
        }

        RecipeGachaResultItemView recipeResultItemView = itemObject.GetComponent<RecipeGachaResultItemView>();
        if (recipeResultItemView != null)
        {
            recipeResultItemView.Bind(result);
            return;
        }

        itemObject.SendMessage("Bind", result, SendMessageOptions.DontRequireReceiver);
    }

    private void LogDrawResults(IReadOnlyList<RecipeGachaResult> results)
    {
        if (results == null || results.Count == 0)
        {
            Debug.Log("[RecipeGachaManager] Draw succeeded but no results were returned.", this);
            return;
        }

        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < results.Count; i++)
        {
            RecipeGachaResult result = results[i];
            if (result == null)
            {
                continue;
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(result.recipeId);
        }

        Debug.Log($"[RecipeGachaManager] Draw succeeded ({results.Count}) -> IDs: {stringBuilder}", this);
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

            RecipeGachaResultItemView itemView = itemObject.GetComponent<RecipeGachaResultItemView>();
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
    }

    private void CloseResultPopupFromScreenPoint(Vector2 screenPoint)
    {
        if (IsPointOverButton(drawOneButton, screenPoint) || IsPointOverButton(drawTenButton, screenPoint))
        {
            return;
        }

        CloseResultPopup();
    }

    private bool IsPointOverButton(Button button, Vector2 screenPoint)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
        {
            return false;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return false;
        }

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
    }

    private void RefreshRecipeLists()
    {
        if (recipeListViews == null || recipeListViews.Length == 0)
        {
            recipeListViews = FindObjectsByType<RecipeListView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        for (int i = 0; i < recipeListViews.Length; i++)
        {
            if (recipeListViews[i] != null)
            {
                recipeListViews[i].Refresh();
            }
        }
    }
}




