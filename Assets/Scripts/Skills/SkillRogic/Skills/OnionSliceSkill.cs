using UnityEngine;

public class OnionSliceSkill : MonoBehaviour, ISkillExecutable
{
    [SerializeField] private float leftSpawnOffsetMultiplier = 0.1f;
    [SerializeField] private float hitActiveDuration = 0.08f;
    [SerializeField] private float hitStunDuration = 0.1f;

    public int SkillId => 0;

    public void Execute(SkillData skillData, SkillCastResult castResult)
    {
        float finalDamage = castResult.currentAttack * (castResult.damagePercent / 100f);

        GameObject slash = PoolingManager.instance.skillPrefabPooling.Get(SkillId);

        Vector3 scale = slash.transform.localScale;
        scale.x = castResult.facingDirection < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        slash.transform.localScale = scale;

        Vector3 spawnPosition = castResult.slashPivot.position;
        if (castResult.facingDirection < 0)
        {
            float leftSpawnOffset = Mathf.Abs(scale.x) * leftSpawnOffsetMultiplier;
            spawnPosition.x -= leftSpawnOffset;
        }

        slash.transform.position = spawnPosition;

        HitBoxModule hitBox = slash.GetComponentInChildren<HitBoxModule>(true);
        if (hitBox != null)
        {
            float critChance = castResult.currentCritChance / 100f;
            hitBox.Setup(finalDamage, critChance, castResult.currentCritDamage, slash, hitStunDuration, hitActiveDuration);
            Debug.Log("파이널데미지" + finalDamage);
        }
    }
}
