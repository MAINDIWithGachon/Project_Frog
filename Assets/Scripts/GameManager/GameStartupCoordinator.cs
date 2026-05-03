using System;
using UnityEngine;

[Flags]
public enum StartupReadyFlags
{
    None = 0,
    Scene = 1 << 0,
    Player = 1 << 1,
    UI = 1 << 2,
    All = Scene | Player | UI,
}

public class GameStartupCoordinator : MonoBehaviour
{
    public static GameStartupCoordinator Instance { get; private set; }

    [Header("References")]
    [SerializeField] private StageManager stageManager;

    [Header("Startup Flow")]
    [SerializeField] private StartupReadyFlags requiredFlags =
        StartupReadyFlags.All;
    [SerializeField] private StartupReadyFlags autoReadyFlags =
        StartupReadyFlags.Scene |
        StartupReadyFlags.Player |
        StartupReadyFlags.UI;

    [Header("Debug")]
    [SerializeField] private StartupReadyFlags currentReadyFlags = StartupReadyFlags.None;
    [SerializeField] private bool stageStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (stageManager == null)
        {
            stageManager = StageManager.Instance;
        }
    }

    private void Start()
    {
        ReportReady(autoReadyFlags);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ReportReady(StartupReadyFlags flags)
    {
        if (flags == StartupReadyFlags.None)
        {
            return;
        }

        currentReadyFlags |= flags;
        TryStartStage();
    }

    public void ReportSceneReady()
    {
        ReportReady(StartupReadyFlags.Scene);
    }

    public void ReportPlayerReady()
    {
        ReportReady(StartupReadyFlags.Player);
    }

    public void ReportUiReady()
    {
        ReportReady(StartupReadyFlags.UI);
    }

    private void TryStartStage()
    {
        StartupReadyFlags validFlags = StartupReadyFlags.All;
        StartupReadyFlags maskedRequiredFlags = requiredFlags & validFlags;
        StartupReadyFlags maskedReadyFlags = currentReadyFlags & validFlags;

        if (stageStarted)
        {
            return;
        }

        if ((maskedReadyFlags & maskedRequiredFlags) != maskedRequiredFlags)
        {
            return;
        }

        if (stageManager == null)
        {
            stageManager = StageManager.Instance;
        }

        if (stageManager == null)
        {
            Debug.LogWarning("[GameStartupCoordinator] StageManager is missing. Cannot start stage yet.");
            return;
        }

        stageStarted = true;
        stageManager.StartStage();
    }
}
