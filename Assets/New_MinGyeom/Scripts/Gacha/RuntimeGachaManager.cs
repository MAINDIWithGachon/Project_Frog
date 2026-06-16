using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NewMinGyeom.Gacha
{
    public class RuntimeGachaManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private RuntimeEquipmentGachaService gachaService;
        [SerializeField] private EquipmentIconResolver iconResolver;

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

        public IReadOnlyList<RuntimeEquipmentGachaResult> LastResults => lastResults;

        private readonly List<GameObject> spawnedResultItems = new();
        private readonly List<RuntimeEquipmentGachaResult> lastResults = new();
        private Coroutine revealRoutine;

        public void Configure(
            RuntimeEquipmentGachaService service,
            EquipmentIconResolver resolver,
            Button oneButton,
            Button tenButton,
            Button popupOneButton,
            Button popupTenButton,
            GameObject resultRoot,
            Transform contentRoot,
            GameObject itemFramePrefab,
            TMP_Text countText,
            float revealInterval,
            GameObject failureRoot)
        {
            UnregisterButtonListeners();

            gachaService = service != null ? service : gachaService;
            iconResolver = resolver != null ? resolver : iconResolver;
            drawOneButton = oneButton;
            drawTenButton = tenButton;
            resultDrawOneButton = popupOneButton;
            resultDrawTenButton = popupTenButton;
            gachaResultRoot = resultRoot;
            resultContentRoot = contentRoot;
            resultItemFramePrefab = itemFramePrefab;
            resultCountText = countText;
            resultRevealInterval = revealInterval;
            insufficientCurrencyRoot = failureRoot;

            ResolveReferences();
            RegisterButtonListeners();
        }

        private void Awake()
        {
            ResolveReferences();
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
            ResolveReferences();
            SetPopupActive(insufficientCurrencyRoot, false);
            SetButtonsInteractable(false);

            if (gachaService == null)
            {
                Debug.LogWarning("[RuntimeGachaManager] RuntimeEquipmentGachaService is not assigned.", this);
                SetButtonsInteractable(true);
                return;
            }

            if (!gachaService.TryDraw(drawCount, out List<RuntimeEquipmentGachaResult> results))
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

        private void ShowResults(IReadOnlyList<RuntimeEquipmentGachaResult> results)
        {
            if (resultContentRoot == null)
            {
                Debug.LogWarning("[RuntimeGachaManager] Result content root is not assigned.", this);
                return;
            }

            if (resultItemFramePrefab == null)
            {
                Debug.LogWarning("[RuntimeGachaManager] Result item frame prefab is not assigned.", this);
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
                RuntimeEquipmentGachaResult result = results[i];
                GameObject itemObject = Instantiate(resultItemFramePrefab, resultContentRoot);
                spawnedResultItems.Add(itemObject);
                BindResultItem(itemObject, result);
                itemObject.SetActive(false);
            }

            revealRoutine = StartCoroutine(RevealResultItemsSequentially());
        }

        private void BindResultItem(GameObject itemObject, RuntimeEquipmentGachaResult result)
        {
            if (itemObject == null || result == null || result.definition == null)
            {
                return;
            }

            GachaResultItemView itemView = itemObject.GetComponent<GachaResultItemView>();
            if (itemView != null)
            {
                itemView.Bind(result, iconResolver);
                return;
            }

            itemObject.SendMessage("Bind", result, SendMessageOptions.DontRequireReceiver);
        }

        private void LogDrawResults(IReadOnlyList<RuntimeEquipmentGachaResult> results)
        {
            if (results == null || results.Count == 0)
            {
                Debug.Log("[RuntimeGachaManager] Draw succeeded but no results were returned.", this);
                return;
            }

            StringBuilder stringBuilder = new();
            for (int i = 0; i < results.Count; i++)
            {
                RuntimeEquipmentGachaResult result = results[i];
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

            Debug.Log($"[RuntimeGachaManager] Draw succeeded ({results.Count}) -> IDs: {stringBuilder}", this);
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

        private void ResolveReferences()
        {
            GachaModuleRoot gachaModuleRoot = GetComponentInParent<GachaModuleRoot>();

            if (gachaService == null)
            {
                gachaService = GetComponent<RuntimeEquipmentGachaService>();
            }

            if (gachaService == null)
            {
                gachaService = GetComponentInParent<RuntimeEquipmentGachaService>();
            }

            if (gachaService == null && gachaModuleRoot != null)
            {
                gachaService = gachaModuleRoot.GachaService;
            }

            if (iconResolver == null)
            {
                iconResolver = GetComponentInParent<EquipmentModuleRoot>()?.IconResolver;
            }

            if (iconResolver == null && gachaModuleRoot != null)
            {
                iconResolver = gachaModuleRoot.IconResolver;
            }
        }

        private static void SetPopupActive(GameObject target, bool isActive)
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
}
