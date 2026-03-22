using UnityEngine;
using UnityEngine.SceneManagement;

public class TestManager : MonoBehaviour
{
    public GameObject TestHamburger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnClickTestButton()
    {
        //메인 씬 우측 상단 테스트 버튼 누를시
        TestHamburger.gameObject.SetActive(true);//테스트 목록 활성화
    }
    public void OnClickDeathTestButton()
    {
        if (Health.PlayerInstance == null)
        {
            Debug.LogWarning("[TestManager] Player Health singleton is missing.");
            return;
        }

        Health.PlayerInstance.TakeDamage(999999f);
    }
    public void ReStart()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RestartCurrentScene();
            return;
        }

        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
