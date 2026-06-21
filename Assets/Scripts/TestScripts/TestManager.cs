using UnityEngine;
using UnityEngine.SceneManagement;

public class TestManager : MonoBehaviour
{
    public GameObject TestHamburger;
    public GameObject TestDataMenu;

    public void OnClickTestButton()
    {
        if (TestHamburger == null)
        {
            Debug.LogWarning("[TestManager] TestHamburger reference is missing.");
            return;
        }

        TestHamburger.SetActive(true);
    }

    public void OnClickTestBackndMenuButton()
    {
        if (TestDataMenu == null)
        {
            Debug.LogWarning("[TestManager] TestDataMenu reference is missing.");
            return;
        }

        TestDataMenu.SetActive(true);
    }

    public void OnClickSaveRuntimeDataTest()
    {
        if (!BackndRuntimeDataTestActions.SaveCurrentRuntimeDataJson(out string message))
        {
            Debug.LogError("[RuntimeData Save Test] " + message);
            return;
        }

        Debug.Log("[RuntimeData Save Test] " + message);
    }

    public void OnClickLoadRuntimeDataTest()
    {
        if (!BackndRuntimeDataTestActions.LoadRuntimeDataJson(out string message))
        {
            Debug.LogError("[RuntimeData Load Test] " + message);
            return;
        }

        Debug.Log("[RuntimeData Load Test] " + message);
    }

    public void OnClickLoadAndApplyRuntimeDataTest()
    {
        if (!BackndRuntimeDataTestActions.LoadAndApplyRuntimeDataJson(out string message))
        {
            Debug.LogError("[RuntimeData Load Test] " + message);
            return;
        }

        Debug.Log("[RuntimeData Load Test] " + message);
    }

    public void OnClickAddRuntimeDataTestGold()
    {
        if (!BackndRuntimeDataTestActions.AddTestGold(out string message))
        {
            Debug.LogError("[RuntimeData Add Test Gold] " + message);
            return;
        }

        Debug.Log("[RuntimeData Add Test Gold] " + message);
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
