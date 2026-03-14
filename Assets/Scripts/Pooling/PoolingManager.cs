using UnityEngine;

/// <summary>
/// 전역에서 접근 가능한 풀링 매니저 싱글톤.
/// 
/// 사용 예:
/// PoolingManager.instance.skillPrefabPooling.Get(0);
/// </summary>
public class PoolingManager : MonoBehaviour
{
    public static PoolingManager instance;

    [Header("# Pool References")]
    public SkillPrefabPooling skillPrefabPooling;

    private void Awake()
    {
        // 이미 다른 인스턴스가 존재하면 중복 오브젝트이므로 제거
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 씬 전환 후에도 유지하고 싶으면 사용
        // DontDestroyOnLoad(gameObject);
    }
}