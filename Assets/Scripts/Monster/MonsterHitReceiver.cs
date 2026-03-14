using UnityEngine;

public class MonsterHitReceiver : MonoBehaviour
{
    private MonsterHealth monsterHealth;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ReceiveHit(other.GetComponent<HitBoxModule>());
    }

    public void ReceiveHit(HitBoxModule hitBox)
    {
        if (hitBox == null)
            return;

        if (monsterHealth == null)
            monsterHealth = GetComponent<MonsterHealth>();

        if (monsterHealth == null)
            return;

        if (!hitBox.TryRegisterHit(monsterHealth))
            return;

        float finalDamage = hitBox.damage;

        bool isCritical = Random.value < hitBox.critChance;
        if (isCritical)
        {
            finalDamage *= (1f + hitBox.critDamage / 100f);
        }

        monsterHealth.TakeDamage(finalDamage);
    }
}
