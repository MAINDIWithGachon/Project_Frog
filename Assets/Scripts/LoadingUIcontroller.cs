using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingUIController : MonoBehaviour
{
    private static LoadingUIController instance;
    public static LoadingUIController Instance => instance;

    [SerializeField] private GameObject _ui;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _loadingInfoDescription;

    private YieldInstruction _awaitLoadingUIClose = new WaitForSeconds(1f);

    private int currentLoadingCount;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Loading(string loadingInfo = "")
    {
        if (currentLoadingCount == 0)
            _ui.SetActive(true);

        if (!string.IsNullOrEmpty(loadingInfo))
            _loadingInfoDescription.SetText(loadingInfo);

        currentLoadingCount++;

        _progressBar.maxValue = currentLoadingCount;
    }

    public void FinishLoading()
    {
        currentLoadingCount--;

        _progressBar.value++;

        if (currentLoadingCount <= 0)
            StartCoroutine(CloseLoadingAwait());
    }

    private IEnumerator CloseLoadingAwait()
    {
        yield return _awaitLoadingUIClose;

        if (currentLoadingCount <= 0)
        {
            _ui.SetActive(false);

            currentLoadingCount = 0;
            _progressBar.value = 0;
        }
    }
}