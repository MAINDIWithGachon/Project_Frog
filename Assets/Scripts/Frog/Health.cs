using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public static Health PlayerInstance { get; private set; }

    [Header("Reference")]
    [SerializeField] private FinalStatData finalStatData;
    [SerializeField] private Movement movement;
    [SerializeField] private Animator[] deathAnimators;

    [Header("Animation")]
    [SerializeField] private string deathTriggerName = "Dead";

    [Header("Runtime HP")]
    [SerializeField] private float currentHp;
    [SerializeField] private bool isDead;
    [SerializeField] private bool hasHandledDeathAnimationFinish;
    [Header("HP Slider")]
    public Slider hpSlider;

    public float CurrentHp => currentHp;
    public float MaxHp => finalStatData != null ? finalStatData.maxHp : 0f;
    public bool IsDead => isDead;

    private void Awake()
    {
        if (PlayerInstance != null && PlayerInstance != this)
        {
            Debug.LogWarning("[Health] Multiple player Health instances detected. Keeping the first registered instance.");
        }
        else
        {
            PlayerInstance = this;
        }

        if (finalStatData == null)
            finalStatData = GetComponent<FinalStatData>();

        if (finalStatData == null)
            finalStatData = GetComponentInParent<FinalStatData>();

        if (movement == null)
            movement = GetComponent<Movement>();

        if (movement == null)
            movement = GetComponentInChildren<Movement>(true);

        if (movement == null)
            movement = GetComponentInParent<Movement>();

        if (deathAnimators == null || deathAnimators.Length == 0)
            deathAnimators = GetComponentsInChildren<Animator>(true);
    }

    private void OnDestroy()
    {
        if (PlayerInstance == this)
            PlayerInstance = null;
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
        hasHandledDeathAnimationFinish = false;
        RefreshHpBar();
    }

    public void ResetForRespawn()
    {
        if (finalStatData == null)
        {
            Debug.LogError("[Health] FinalStatData reference is missing.");
            return;
        }

        currentHp = finalStatData.maxHp;
        isDead = false;
        hasHandledDeathAnimationFinish = false;
        RefreshHpBar();

        if (movement != null)
            movement.ResetForRespawn();

        ResetDeathAnimation();
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
        hasHandledDeathAnimationFinish = false;
        RefreshHpBar();

        StopDeathRelatedMotion();
        TriggerDeathAnimation();
        LogDeathEvent();
    }

    private void StopDeathRelatedMotion()
    {
        if (movement != null)
            movement.StopMovement();

        StageManager.Instance?.StopStage();
    }

    private void TriggerDeathAnimation()
    {
        if (deathAnimators == null || deathAnimators.Length == 0)
        {
            Debug.LogWarning("[Health] Death animation was requested, but no Animator was found.");
            return;
        }

        int triggeredCount = 0;

        foreach (Animator animator in deathAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
            triggeredCount++;
        }

        Debug.Log($"[Health] Death animation triggered on {triggeredCount} animator(s) with trigger '{deathTriggerName}'.");
    }

    private void LogDeathEvent()
    {
        Debug.Log("[Health] Frog HP reached 0. Death event fired.");
        Debug.Log("[Health] Skipping gameplay-side death result. Animation only mode is active.");
        Debug.Log("[Health] GameStateManager.OnPlayerDead() was not executed.");
    }

    private void ResetDeathAnimation()
    {
        if (deathAnimators == null || deathAnimators.Length == 0)
            return;

        foreach (Animator animator in deathAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(deathTriggerName);
            animator.Rebind();
            animator.Play("Run", 0, 0f);
            animator.Update(0f);
        }
    }

    private void RefreshHpBar()
    {
        if (hpSlider == null || finalStatData == null)
            return;

        hpSlider.maxValue = finalStatData.maxHp;
        hpSlider.value = currentHp;
    }
    public void OnDeathAnimationFinished()
    {
        if (hasHandledDeathAnimationFinish)
            return;

        hasHandledDeathAnimationFinish = true;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.FailStage();
            if (StageManager.Instance.gameReult_Lose != null)
                StageManager.Instance.gameReult_Lose.gameObject.SetActive(true);

            StageManager.Instance.RestartCurrentStage();
        }

        GameStateManager.Instance?.SetState(GameState.Playing);
    }
}
