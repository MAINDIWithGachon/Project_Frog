using UnityEngine;

public class StageManager : MonoBehaviour
{
    private const int RequiredNormalKillCount = 100;

    public static StageManager Instance { get; private set; }

    [Header("Stage Start Settings")]
    [SerializeField] private int testStartStageIndex = 1;

    [Header("Stage Difficulty Offsets")]
    [SerializeField] private float monsterHpOffsetPerStage = 0.1f;
    [SerializeField] private float monsterSpeedOffsetPerStage = 0.02f;
    [SerializeField] private float spawnIntervalOffsetPerStage = 0.03f;
    [SerializeField] private float minSpawnIntervalMultiplier = 0.4f;
    [SerializeField] private int spawnCountOffsetStageInterval = 5;

    [Header("Runtime UI Debug")]
    [SerializeField] private int currentStageIndex;
    [SerializeField] private StageState currentStageState;
    [SerializeField] private int currentNormalKillCount;
    [SerializeField] private float currentRemainingBossTime;

    public ParallaxBackground[] parallaxBackgrounds;
    public StageRuntimeContext runtime;

    [Header("UI")]
    public SpriteRenderer StageFog;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[StageManager] Multiple instances detected. Keeping the first registered instance.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    [ContextMenu("Start Stage")]
    public void StartStage()
    {
        runtime = new StageRuntimeContext();

        runtime.stageIndex = Mathf.Max(1, testStartStageIndex);
        runtime.currentState = StageState.Ready;
        runtime.normalKillCount = 0;
        runtime.remainingBossTime = 0f;

        ResetRuntimeMultipliers();
        ApplyStageDifficulty();
        SyncRuntimeToInspector();

        EnterNormalMode();
    }

    public void EnterNormalMode()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[StageManager] Runtime is not initialized. Call StartStage() first.");
            return;
        }
        StageFog.gameObject.SetActive(true);
        runtime.currentState = StageState.Normal;
        SyncRuntimeToInspector();
        Debug.Log($"[StageManager] Stage {runtime.stageIndex} entered Normal mode.");
    }

    public void EnterBossMode()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[StageManager] Runtime is not initialized. Call StartStage() first.");
            return;
        }

        if (runtime.currentState == StageState.Boss)
            return;

        runtime.currentState = StageState.Boss;
        runtime.remainingBossTime = 0f;
        SyncRuntimeToInspector();
        Debug.Log($"[StageManager] Stage {runtime.stageIndex} entered Boss mode.");
    }

    public void ClearStage()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[StageManager] Runtime is not initialized. Call StartStage() first.");
            return;
        }

        runtime.currentState = StageState.Clear;
        SyncRuntimeToInspector();
        Debug.Log($"[StageManager] Stage {runtime.stageIndex} cleared.");
        AdvanceToNextStage();
    }

    public void FailStage()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[StageManager] Runtime is not initialized. Call StartStage() first.");
            return;
        }

        runtime.currentState = StageState.Fail;
        SyncRuntimeToInspector();
        Debug.Log($"[StageManager] Stage {runtime.stageIndex} failed.");
    }

    public void RegisterNormalMonsterKill()
    {
        if (runtime == null)
        {
            Debug.LogWarning("[StageManager] Runtime is not initialized. Call StartStage() first.");
            return;
        }

        if (runtime.currentState != StageState.Normal)
            return;

        runtime.normalKillCount++;
        SyncRuntimeToInspector();
        Debug.Log($"[StageManager] Normal monster kill registered: {runtime.normalKillCount}/{RequiredNormalKillCount}");

        if (runtime.normalKillCount >= RequiredNormalKillCount)
        {
            ClearStage();
        }
    }

    private void AdvanceToNextStage()
    {
        if (runtime == null)
            return;

        runtime.stageIndex++;
        runtime.normalKillCount = 0;
        runtime.remainingBossTime = 0f;
        runtime.activeEventIds.Clear();

        ResetRuntimeMultipliers();
        ApplyStageDifficulty();
        SyncRuntimeToInspector();

        Debug.Log($"[StageManager] Moving to stage {runtime.stageIndex}.");
        EnterNormalMode();
    }

    private void SyncRuntimeToInspector()
    {
        if (runtime == null)
        {
            currentStageIndex = 0;
            currentStageState = StageState.Ready;
            currentNormalKillCount = 0;
            currentRemainingBossTime = 0f;
            return;
        }

        currentStageIndex = runtime.stageIndex;
        currentStageState = runtime.currentState;
        currentNormalKillCount = runtime.normalKillCount;
        currentRemainingBossTime = runtime.remainingBossTime;
    }

    private void ResetRuntimeMultipliers()
    {
        if (runtime == null)
            return;

        runtime.finalMonsterHpMultiplier = 1f;
        runtime.finalMonsterSpeedMultiplier = 1f;
        runtime.finalSpawnIntervalMultiplier = 1f;
        runtime.finalSpawnCountMultiplier = 1f;
    }

    private void ApplyStageDifficulty()
    {
        if (runtime == null)
            return;

        int stageStep = Mathf.Max(0, runtime.stageIndex - 1);

        runtime.finalMonsterHpMultiplier += stageStep * monsterHpOffsetPerStage;
        runtime.finalMonsterSpeedMultiplier += stageStep * monsterSpeedOffsetPerStage;
        runtime.finalSpawnIntervalMultiplier = Mathf.Max(
            minSpawnIntervalMultiplier,
            runtime.finalSpawnIntervalMultiplier - (stageStep * spawnIntervalOffsetPerStage));

        if (spawnCountOffsetStageInterval > 0)
        {
            runtime.finalSpawnCountMultiplier += Mathf.FloorToInt((float)stageStep / spawnCountOffsetStageInterval);
        }

        Debug.Log(
            $"[StageManager] Applied difficulty for stage {runtime.stageIndex} | " +
            $"HP x{runtime.finalMonsterHpMultiplier:F2}, " +
            $"Speed x{runtime.finalMonsterSpeedMultiplier:F2}, " +
            $"SpawnInterval x{runtime.finalSpawnIntervalMultiplier:F2}, " +
            $"SpawnCount x{runtime.finalSpawnCountMultiplier:F2}");
    }


    public void StopStage()
    {
        //스테이지의 이동을 0으로 함. 사망시 사용
        if (parallaxBackgrounds == null || parallaxBackgrounds.Length == 0)
        {
            Debug.LogWarning("[StageManager] No parallax backgrounds assigned.");
            return;
        }

        for (int index = 0; index < parallaxBackgrounds.Length; index++)
        {
            if (parallaxBackgrounds[index] == null)
                continue;

            parallaxBackgrounds[index].speedMultiplier = 0f;
        }
    }
}
