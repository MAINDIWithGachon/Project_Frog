using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private int targetFrameRate = 120;
    public GameObject DevelopingUI;

    private void Awake()
    {
        ApplyFrameRateSettings();
    }

    private void ApplyFrameRateSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
    public void ShowDevelopingUI()
    {
        DevelopingUI.SetActive(true);

        Animator anim = DevelopingUI.GetComponent<Animator>();

        anim.Rebind();
        anim.Update(0f);
        anim.Play("Open", 0, 0f);
    }
}
