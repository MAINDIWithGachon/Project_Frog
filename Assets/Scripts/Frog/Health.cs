using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private FinalStatData finalStatData;

    [Header("Runtime HP")]
    [SerializeField] private float currentHp;
    [SerializeField] private bool isDead;
    [Header("HP Slider")]
    public Slider hpSlider;

    public float CurrentHp => currentHp;
    public float MaxHp => finalStatData != null ? finalStatData.maxHp : 0f;
    public bool IsDead => isDead;

    private void Awake()
    {
        if (finalStatData == null)
            finalStatData = GetComponentInParent<FinalStatData>();
    }

    private void Start()
    {
        InitializeHp();
    }

    private void Update()
    {
        if (isDead || finalStatData == null)
            return;

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        if (finalStatData.hpRegenPerSecond > 0f)
        {
            Heal(finalStatData.hpRegenPerSecond * Time.deltaTime);
        }
    }

    private void InitializeHp()
    {
        if (finalStatData == null)
        {
            Debug.LogError("[Health] FinalStatData reference is missing.");
            return;
        }

        currentHp = finalStatData.maxHp;
        isDead = currentHp <= 0f;
        RefreshHpBar();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHp = Mathf.Max(currentHp - amount, 0f);
        RefreshHpBar();

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f || finalStatData == null)
            return;

        currentHp = Mathf.Min(currentHp + amount, finalStatData.maxHp);
        RefreshHpBar();
    }

    public void FullHeal()
    {
        if (finalStatData == null)
            return;

        currentHp = finalStatData.maxHp;
        isDead = false;
        RefreshHpBar();
    }

    public void ApplyStatChanged(float previousMaxHp)
    {
        if (finalStatData == null)
            return;

        float newMaxHp = finalStatData.maxHp;

        if (previousMaxHp <= 0f)
        {
            currentHp = newMaxHp;
            isDead = currentHp <= 0f;
            RefreshHpBar();
            return;
        }

        float hpRatio = currentHp / previousMaxHp;
        currentHp = Mathf.Clamp(newMaxHp * hpRatio, 0f, newMaxHp);
        isDead = currentHp <= 0f;
        RefreshHpBar();
    }

    private void Die()
    {
        isDead = true;
        currentHp = 0f;
        RefreshHpBar();

        GameStateManager.Instance?.OnPlayerDead();
    }

    private void RefreshHpBar()
    {
        if (hpSlider == null || finalStatData == null)
            return;

        hpSlider.maxValue = finalStatData.maxHp;
        hpSlider.value = currentHp;
    }
}
