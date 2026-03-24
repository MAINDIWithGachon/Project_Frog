using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Playing,
    Paused,
    PlayerDead
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsPaused => CurrentState == GameState.Paused;
    public bool IsPlayerDead => CurrentState == GameState.PlayerDead;
    public Image blackImage;//게임 정지시 화면 어둡게
    public GameObject retryButton;//다시하기 버튼

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        ApplyStateSettings();
    }

    public void PauseGame()
    {
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        SetState(GameState.Playing);
    }

    public void OnPlayerDead()
    {
        SetState(GameState.PlayerDead);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void ApplyStateSettings()
    {
        switch (CurrentState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                SetOverlayActive(false);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                SetOverlayActive(true);
                break;

            case GameState.PlayerDead:
                Time.timeScale = 1f;
                break;
        }
    }

    private void SetOverlayActive(bool isActive)
    {
        if (blackImage != null)
        {
            blackImage.gameObject.SetActive(isActive);
        }

        if (retryButton != null)
        {
            retryButton.SetActive(isActive);
        }
    }
}
