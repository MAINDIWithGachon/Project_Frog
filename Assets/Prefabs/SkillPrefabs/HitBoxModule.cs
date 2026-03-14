using System.Collections.Generic;
using UnityEngine;

public class HitBoxModule : MonoBehaviour
{
    public float damage;
    public float critChance;
    public float critDamage;
    public GameObject owner;

    private readonly HashSet<int> hitTargetIds = new();

    public void Setup(float damage, float critChance, float critDamage, GameObject owner)
    {
        this.damage = damage;
        this.critChance = critChance;
        this.critDamage = critDamage;
        this.owner = owner;
        hitTargetIds.Clear();
    }

    public bool TryRegisterHit(Component target)
    {
        if (target == null)
            return false;

        int targetId = target.transform.root.gameObject.GetInstanceID();
        return hitTargetIds.Add(targetId);
    }
}
