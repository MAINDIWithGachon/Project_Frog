using UnityEngine;

public class SpawnTest_Button : MonoBehaviour
{
    [Header("# Spawn")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Vector3 spawnPosition = new(10f, -0.7f, 0f);
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float[] spawnYOptions = { -0.8f, -0.8f, -0.8f };

    public void OnClickSpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[SpawnTest_Button] monsterPrefab reference is missing.");
            return;
        }

        Vector3 finalSpawnPosition = GetSpawnPosition();
        GameObject spawnedMonster = Instantiate(monsterPrefab, finalSpawnPosition, Quaternion.identity, spawnParent);
        BindPlayerTarget(spawnedMonster);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 finalSpawnPosition = spawnPosition;

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

        Monster_Movement monsterMovement = spawnedMonster.GetComponent<Monster_Movement>();
        if (monsterMovement == null || monsterMovement.player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogWarning("[SpawnTest_Button] Player tagged object not found.");
            return;
        }

        Transform playerCenterPivot = FindChildTransformByName(playerObject.transform, "CenterPivot");
        if (playerCenterPivot == null)
        {
            Debug.LogWarning("[SpawnTest_Button] Player CenterPivot not found.");
            return;
        }

        monsterMovement.Initialize(playerCenterPivot);
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
