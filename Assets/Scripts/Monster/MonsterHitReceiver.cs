using UnityEngine;

public class MonsterHitReceiver : MonoBehaviour
{
    [Header("Damage Text")]
    [SerializeField] private int damageTextPoolIndex = 0;
    [SerializeField] private float damageTextYOffset = 0.3f;

    private MonsterHealth monsterHealth;
    private Monster_Movement monsterMovement;
    private Collider2D cachedCollider;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        monsterMovement = GetComponent<Monster_Movement>();
        cachedCollider = GetComponent<Collider2D>();
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

        if (monsterHealth == null || monsterHealth.IsDead)
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
        if (monsterMovement != null)
            monsterMovement.ApplyHitStun(hitBox.hitStunDuration);

        SpawnDamageText(finalDamage, isCritical);
    }

    private void SpawnDamageText(float finalDamage, bool isCritical)
    {
        if (PoolingManager.instance == null || PoolingManager.instance.damageTextPooling == null)
            return;

        GameObject damageTextObject = PoolingManager.instance.damageTextPooling.Get(damageTextPoolIndex);
        if (damageTextObject == null)
            return;

        damageTextObject.transform.position = GetDamageTextSpawnPosition();

        DamageText damageText = damageTextObject.GetComponent<DamageText>();
        if (damageText == null)
            damageText = damageTextObject.GetComponentInChildren<DamageText>(true);

        if (damageText == null)
        {
            Debug.LogWarning($"[{nameof(MonsterHitReceiver)}] DamageText component was not found on pooled object.", damageTextObject);
            return;
        }

        damageText.Init(finalDamage, isCritical);
    }

    private Vector3 GetDamageTextSpawnPosition()
    {
        Bounds bounds = GetMonsterBounds();
        Vector3 spawnPosition = bounds.center;
        spawnPosition.y = bounds.max.y + damageTextYOffset;
        spawnPosition.z = transform.position.z;
        return spawnPosition;
    }

    private Bounds GetMonsterBounds()
    {
        if (cachedCollider != null)
            return cachedCollider.bounds;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds combinedBounds = colliders[0].bounds;
        for (int index = 1; index < colliders.Length; index++)
        {
            combinedBounds.Encapsulate(colliders[index].bounds);
        }

        return combinedBounds;
    }
}
