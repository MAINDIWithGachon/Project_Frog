using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    [Header("부모")]
    [SerializeField] private GameObject disableTarget;

    [Header("Animation")]
    [SerializeField] private Animator[] deathAnimators;
    [SerializeField] private string deathTriggerName = "Dead";

    [Header("Death Cleanup")]
    [SerializeField] private Monster_Movement monsterMovement;
    [SerializeField] private Collider2D[] collidersToDisable;

    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp;
    [SerializeField] private bool isDead;

    [Header("UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject hpBarRoot; // 보통 Canvas 또는 Slider 오브젝트

    public bool IsDead => isDead;

    private void Awake()
    {
        if (disableTarget == null)
        {
            if (transform.parent != null)
                disableTarget = transform.parent.gameObject;
            else
                disableTarget = gameObject;
        }

        if (deathAnimators == null || deathAnimators.Length == 0)
            deathAnimators = GetComponentsInChildren<Animator>(true);

        if (monsterMovement == null)
            monsterMovement = GetComponent<Monster_Movement>();

        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider2D>(true);

        currentHp = maxHp;
        isDead = currentHp <= 0f;
        RefreshHpBar();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHp -= amount;
        currentHp = Mathf.Max(currentHp, 0f);

        Debug.Log($"{gameObject.name} 피해 받음: {amount}, 남은 체력: {currentHp}");

        RefreshHpBar();

        if (currentHp <= 0f)
        {
            BeginDeath();
        }
    }

    private void RefreshHpBar()
    {
        float normalizedHp = currentHp / maxHp;

        if (hpSlider != null)
        {
            hpSlider.value = normalizedHp;
        }

        if (hpBarRoot != null)
        {
            hpBarRoot.SetActive(normalizedHp < 1f);
        }
    }

    private void BeginDeath()
    {
        if (isDead)
            return;

        isDead = true;
        currentHp = 0f;
        RefreshHpBar();
        StopDeathRelatedBehavior();
        TriggerDeathAnimation();
    }

    private void StopDeathRelatedBehavior()
    {
        if (monsterMovement != null)
            monsterMovement.StopForDeath();

        if (collidersToDisable == null || collidersToDisable.Length == 0)
            return;

        for (int index = 0; index < collidersToDisable.Length; index++)
        {
            if (collidersToDisable[index] == null)
                continue;

            collidersToDisable[index].enabled = false;
        }
    }

    private void TriggerDeathAnimation()
    {
        if (deathAnimators == null || deathAnimators.Length == 0)
        {
            Debug.LogWarning("[MonsterHealth] Death animation was requested, but no Animator was found.", this);
            Die();
            return;
        }

        foreach (Animator animator in deathAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }
    }

    public void Die()
    {
        if (disableTarget == null)
            return;

        Debug.Log($"{gameObject.name} 사망");
        disableTarget.SetActive(false);
    }
}
