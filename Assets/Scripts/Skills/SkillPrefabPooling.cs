using System.Collections.Generic;
using UnityEngine;

public class SkillPrefabPooling : MonoBehaviour
{
    public GameObject[] prefabs;
    private List<GameObject>[] pools;

    private void Awake()
    {

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[SkillPrefabPooling] Prefabs array is empty.");
            return;
        }

        pools = new List<GameObject>[prefabs.Length];

        for (int index = 0; index < pools.Length; index++)
        {
            pools[index] = new List<GameObject>();
        }
    }

    public GameObject Get(int index)
    {
        if (prefabs == null || pools == null)
        {
            Debug.LogError("[SkillPrefabPooling] Pool is not initialized.");
            return null;
        }

        if (index < 0 || index >= prefabs.Length)
        {
            Debug.LogError($"[SkillPrefabPooling] Invalid prefab index: {index}");
            return null;
        }

        GameObject select = null;

        // 선택한 풀에서 비활성화된 오브젝트 재사용
        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                break;
            }
        }

        // 비활성 오브젝트가 없으면 새로 생성
        if (select == null)
        {
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select);
        }

        select.SetActive(true);
        return select;
    }
}