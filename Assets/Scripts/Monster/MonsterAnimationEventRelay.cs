using UnityEngine;

public class MonsterAnimationEventRelay : MonoBehaviour
{
    private MonsterHealth monsterHealth;

    private void Awake()
    {
        monsterHealth = GetComponentInParent<MonsterHealth>();
    }

    public void Die()
    {
        if (monsterHealth != null)
            monsterHealth.Die();
    }
}
