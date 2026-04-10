using System.Collections.Generic;

public class StageRuntimeContext
{
  
    public int stageIndex;
    public StageState currentState;

    public int normalKillCount;
    public float remainingBossTime;

    public List<string> activeEventIds = new();

    public float finalMonsterHpMultiplier = 1f;
    public float finalMonsterSpeedMultiplier = 1f;
    public float finalSpawnIntervalMultiplier = 1f;
    public float finalSpawnCountMultiplier = 1f;
}
