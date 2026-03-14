using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp;

    [Header("UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private GameObject hpBarRoot; // 보통 Canvas 또는 Slider 오브젝트

    private void Awake()
    {
        currentHp = maxHp;
        RefreshHpBar();
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Max(currentHp, 0f);

        Debug.Log($"{gameObject.name} 피해 받음: {amount}, 남은 체력: {currentHp}");

        RefreshHpBar();

        if (currentHp <= 0f)
        {
            Die();
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

    private void Die()
    {
        Debug.Log($"{gameObject.name} 사망");
        gameObject.SetActive(false);
    }
}