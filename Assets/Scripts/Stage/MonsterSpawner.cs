using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Transform spawnParent;

    [Header("Monster Prefabs")]
    [SerializeField] private GameObject[] normalMonsterPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private int baseSpawnCount = 1;
    [SerializeField] private int maxAliveMonsters = 10;
    [SerializeField] private Vector3 baseSpawnPosition = new(10f, -0.7f, 0f);
    [SerializeField] private float[] spawnYOptions = { -0.8f, -0.8f, -0.8f };

    private readonly List<GameObject> aliveMonsters = new();
    private float spawnTimer;

    private void Awake()
    {
        if (stageManager == null)
            stageManager = StageManager.Instance;
    }

    private void Update()
    {
        if (!CanSpawn())
            return;

        spawnTimer += Time.deltaTime;

        float spawnInterval = GetCurrentSpawnInterval();
        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;
        SpawnWave();
    }

    private bool CanSpawn()
    {
        if (stageManager == null || stageManager.runtime == null)
            return false;

        if (stageManager.runtime.currentState != StageState.Normal)
            return false;

        CleanupDeadMonsters();
        return aliveMonsters.Count < maxAliveMonsters;
    }

    private float GetCurrentSpawnInterval()
    {
        float multiplier = 1f;

        if (stageManager != null && stageManager.runtime != null)
            multiplier = stageManager.runtime.finalSpawnIntervalMultiplier;

        return Mathf.Max(0.1f, baseSpawnInterval * multiplier);
    }

    private int GetCurrentSpawnCount()
    {
        float multiplier = 1f;

        if (stageManager != null && stageManager.runtime != null)
            multiplier = stageManager.runtime.finalSpawnCountMultiplier;

        int finalSpawnCount = Mathf.RoundToInt(baseSpawnCount * multiplier);
        return Mathf.Max(1, finalSpawnCount);
    }

    private void SpawnWave()
    {
        if (normalMonsterPrefabs == null || normalMonsterPrefabs.Length == 0)
        {
            Debug.LogWarning("[MonsterSpawner] No normal monster prefabs assigned.", this);
            return;
        }

        int spawnCount = GetCurrentSpawnCount();

        for (int index = 0; index < spawnCount; index++)
        {
            if (aliveMonsters.Count >= maxAliveMonsters)
                break;

            GameObject monsterPrefab = GetRandomMonsterPrefab();
            if (monsterPrefab == null)
                continue;

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject spawnedMonster = Instantiate(
                monsterPrefab,
                spawnPosition,
                Quaternion.identity,
                spawnParent);
                
            aliveMonsters.Add(spawnedMonster);
            BindPlayerTarget(spawnedMonster);
        }
    }

    private GameObject GetRandomMonsterPrefab()
    {
        int randomIndex = Random.Range(0, normalMonsterPrefabs.Length);
        return normalMonsterPrefabs[randomIndex];
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 finalSpawnPosition = baseSpawnPosition;

        if (spawnYOptions == null || spawnYOptions.Length == 0)
            return finalSpawnPosition;

        int randomIndex = Random.Range(0, spawnYOptions.Length);
        finalSpawnPosition.y = spawnYOptions[randomIndex];
        return finalSpawnPosition;
    }

    private void BindPlayerTarget(GameObject spawnedMonster)
    {
        if (spawnedMonster == null)
            return;

        Monster_Movement monsterMovement = spawnedMonster.GetComponentInChildren<Monster_Movement>(true);
        if (monsterMovement == null || monsterMovement.player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogWarning("[MonsterSpawner] Player tagged object not found.");
            return;
        }

        Transform playerCenterPivot = FindChildTransformByName(playerObject.transform, "CenterPivot");
        if (playerCenterPivot == null)
        {
            Debug.LogWarning("[MonsterSpawner] Player CenterPivot not found.");
            return;
        }

        monsterMovement.Initialize(playerCenterPivot);
    }


    private void CleanupDeadMonsters()
    {
        for (int index = aliveMonsters.Count - 1; index >= 0; index--)
        {
            GameObject monster = aliveMonsters[index];
            if (monster != null && monster.activeInHierarchy)
                continue;

            aliveMonsters.RemoveAt(index);
        }
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int index = 0; index < children.Length; index++)
        {
            if (children[index].name == childName)
                return children[index];
        }

        return null;
    }
}
