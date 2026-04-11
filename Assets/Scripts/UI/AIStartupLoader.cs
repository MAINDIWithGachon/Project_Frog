using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;

public class AIStartupLoader : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private GameObject aiRoot;
    [SerializeField] private LLMAgent llmAgent;

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private Button downloadButton;
    [SerializeField] private Button quitButton;

    [Header("Progress UI")]
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    [Header("Optional")]
    [SerializeField] private RectTransform rect;

    private bool isStarting;

    private void Start()
    {
        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }

        if (downloadButton != null)
        {
            downloadButton.onClick.RemoveListener(OnClickDownload);
            downloadButton.onClick.AddListener(OnClickDownload);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnClickQuit);
            quitButton.onClick.AddListener(OnClickQuit);
        }

        if (progressBar != null)
        {
            progressBar.value = progressBar.minValue;
        }

        if (NeedsDownload())
        {
            if (aiRoot != null) aiRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(true);
            if (progressRoot != null) progressRoot.SetActive(false);
        }
        else
        {
            if (confirmRoot != null) confirmRoot.SetActive(false);
            _ = StartAIAsync();
        }
    }

    private bool NeedsDownload()
    {
#if UNITY_EDITOR
        return false;
#else
        if (llmAgent == null || llmAgent.llm == null || string.IsNullOrEmpty(llmAgent.llm.model))
        {
            return false;
        }

        string modelName = Path.GetFileName(llmAgent.llm.model);

        string persistentPath = Path.Combine(Application.persistentDataPath, modelName);
        if (File.Exists(persistentPath))
        {
            return false;
        }

        string streamingPath = Path.Combine(Application.streamingAssetsPath, modelName);
        if (File.Exists(streamingPath))
        {
            return false;
        }

        return true;
#endif
    }

    private async void OnClickDownload()
    {
        if (isStarting) return;
        await StartAIAsync();
    }

    private async Task StartAIAsync()
    {
        if (isStarting) return;
        isStarting = true;

        if (confirmRoot != null) confirmRoot.SetActive(false);
        if (progressRoot != null) progressRoot.SetActive(true);

        try
        {
            if (progressText != null)
            {
                progressText.text = "AI 모델 확인 중...";
            }

            if (aiRoot != null && !aiRoot.activeSelf)
            {
                aiRoot.SetActive(true);
            }

            bool ok = await LLM.WaitUntilModelSetup(UpdateProgress);
            if (!ok)
            {
                if (progressText != null)
                {
                    progressText.text = "모델 다운로드에 실패했습니다.";
                }
                return;
            }

            if (llmAgent != null && llmAgent.llm != null)
            {
                if (progressText != null)
                {
                    progressText.text = "AI 엔진 시작 중...";
                }

                await llmAgent.llm.WaitUntilReady();
            }

            if (progressText != null)
            {
                progressText.text = "대화 엔진 준비 중...";
            }

            await WaitForAgentReady();

            if (progressText != null)
            {
                progressText.text = "모델 준비 중...";
            }

            if (llmAgent != null)
            {
                await llmAgent.Warmup();
            }

            if (progressRoot != null)
            {
                progressRoot.SetActive(false);
            }

            GameStartupCoordinator.Instance?.ReportAiReady();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI startup failed: {e}");
            if (progressText != null)
            {
                progressText.text = "AI 초기화에 실패했습니다.";
            }
        }
        finally
        {
            isStarting = false;
        }
    }

    private async Task WaitForAgentReady()
    {
        if (llmAgent == null)
        {
            return;
        }

        while (llmAgent.llmAgent == null)
        {
            await Task.Yield();
        }
    }

    private void UpdateProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.value = Mathf.Lerp(progressBar.minValue, progressBar.maxValue, clamped);
        }

        if (progressText != null)
        {
            progressText.text = $"AI 모델 다운로드 중... {(int)(clamped * 100f)}%";
        }
    }

    private void OnClickQuit()
    {
        Application.Quit();
    }
}
