using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;

public class AIStartupLoader : MonoBehaviour
{
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private LLMAgent llmAgent;

    async void Start()
    {
        loadingRoot.SetActive(true);
        bool ok = await LLM.WaitUntilModelSetup(UpdateProgress);
        if (!ok)
        {
            progressText.text = "모델 다운로드에 실패했습니다.";
            return;
        }

        progressText.text = "모델 준비 중...";
        await llmAgent.Warmup();

        loadingRoot.SetActive(false);
    }

    void UpdateProgress(float progress)
    {
        if (progressBar != null) progressBar.value = progress;
        if (progressText != null) progressText.text = $"AI 모델 다운로드 중... {(int)(progress * 100)}%";
    }
}
