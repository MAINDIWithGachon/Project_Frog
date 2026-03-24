using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public ParallaxBackground[] parallaxBackgrounds;

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
