using UnityEngine;

public class Fire_Rogic : MonoBehaviour, ISkillExecutable
{
    [SerializeField] private float leftSpawnOffsetMultiplier = 0.1f;
    [SerializeField] private float hitActiveDuration = 0.08f;
    [SerializeField] private float hitStunDuration = 0.1f;
    [SerializeField] private float knockbackDistance = 1f;
    public int SkillId => 1;

    public void Execute(SkillData skillData, SkillCastResult castResult)
    {
        float finalDamage = castResult.currentAttack * (castResult.damagePercent / 100f);
        GameObject Fire = PoolingManager.instance.skillPrefabPooling.Get(SkillId);

        Vector3 scale = Fire.transform.localScale;
        scale.x = castResult.facingDirection < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        Fire.transform.localScale = scale;

        Vector3 spawnPosition = castResult.slashPivot.position;
        if (castResult.facingDirection < 0)
        {
            float leftSpawnOffset = Mathf.Abs(scale.x) * leftSpawnOffsetMultiplier;
            spawnPosition.x -= leftSpawnOffset;
        }

        Fire.transform.position = spawnPosition;

        HitBoxModule hitBox = Fire.GetComponentInChildren<HitBoxModule>(true);
        if (hitBox != null)
        {
            float critChance = castResult.currentCritChance / 100f;
            hitBox.Setup(
                finalDamage,
                critChance,
                castResult.currentCritDamage,
                Fire,
                hitStunDuration,
                hitActiveDuration,
                knockbackDistance,
                castResult.facingDirection,
                true);
            Debug.Log("파이널데미지" + finalDamage);
        }

        Bullet_Fire bulletFire = Fire.GetComponent<Bullet_Fire>();
        if (bulletFire != null)
            bulletFire.Initialize(castResult.facingDirection);
    }
}
