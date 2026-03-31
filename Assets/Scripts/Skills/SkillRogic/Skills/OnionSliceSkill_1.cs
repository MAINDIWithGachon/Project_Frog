using UnityEngine;

public class OnionSliceSkill_1 : MonoBehaviour, ISkillExecutable
{
    [SerializeField] private float hitActiveDuration = 0.08f;
    [SerializeField] private float hitStunDuration = 0.1f;

    public int SkillId => 1;

    public void Execute(SkillData skillData, SkillCastResult castResult)
    {
        float finalDamage = castResult.currentAttack * (castResult.damagePercent / 100f);

        GameObject slash = PoolingManager.instance.skillPrefabPooling.Get(SkillId);
        slash.transform.position = castResult.slashPivot.position;

        Vector3 scale = slash.transform.localScale;
        scale.x = castResult.facingDirection < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        slash.transform.localScale = scale;

        HitBoxModule hitBox = slash.GetComponentInChildren<HitBoxModule>(true);
        if (hitBox != null)
        {
            float critChance = castResult.currentCritChance / 100f;
            hitBox.Setup(finalDamage, critChance, castResult.currentCritDamage, slash, hitStunDuration, hitActiveDuration);
            Debug.Log("파이널데미지" + finalDamage);
        }
    }
}
