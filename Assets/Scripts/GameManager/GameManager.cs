using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private int targetFrameRate = 120;

    private void Awake()
    {
        ApplyFrameRateSettings();
    }

    private void ApplyFrameRateSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
