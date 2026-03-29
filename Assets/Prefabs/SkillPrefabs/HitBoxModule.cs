using System.Collections.Generic;
using UnityEngine;

public class HitBoxModule : MonoBehaviour
{
    [SerializeField] private float defaultHitActiveDuration = 0.08f;
    [SerializeField] private float defaultHitStunDuration = 0.1f;

    public float damage;
    public float critChance;
    public float critDamage;
    public GameObject owner;
    public float hitActiveDuration { get; private set; }
    public float hitStunDuration { get; private set; }

    private readonly HashSet<int> hitTargetIds = new();
    private Collider2D[] hitColliders;
    private float hitActiveTimer;
    private bool isHitBoxActive;

    private void Awake()
    {
        hitColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Update()
    {
        if (!isHitBoxActive)
            return;

        hitActiveTimer -= Time.deltaTime;
        if (hitActiveTimer > 0f)
            return;

        isHitBoxActive = false;
        SetHitCollidersEnabled(false);
    }

    private void OnDisable()
    {
        isHitBoxActive = false;
        hitActiveTimer = 0f;
        SetHitCollidersEnabled(false);
    }

    public void Setup(
        float damage,
        float critChance,
        float critDamage,
        GameObject owner,
        float hitStunDuration = -1f,
        float hitActiveDuration = -1f)
    {
        this.damage = damage;
        this.critChance = critChance;
        this.critDamage = critDamage;
        this.owner = owner;
        this.hitActiveDuration = hitActiveDuration >= 0f ? hitActiveDuration : defaultHitActiveDuration;
        this.hitStunDuration = hitStunDuration >= 0f ? hitStunDuration : defaultHitStunDuration;
        hitTargetIds.Clear();

        hitActiveTimer = this.hitActiveDuration;
        isHitBoxActive = true;
        SetHitCollidersEnabled(true);
    }

    public bool TryRegisterHit(Component target)
    {
        if (target == null)
            return false;

        int targetId = target.transform.root.gameObject.GetInstanceID();
        return hitTargetIds.Add(targetId);
    }

    private void SetHitCollidersEnabled(bool enabled)
    {
        if (hitColliders == null || hitColliders.Length == 0)
            hitColliders = GetComponentsInChildren<Collider2D>(true);

        for (int index = 0; index < hitColliders.Length; index++)
        {
            if (hitColliders[index] == null)
                continue;

            hitColliders[index].enabled = enabled;
        }
    }
}
