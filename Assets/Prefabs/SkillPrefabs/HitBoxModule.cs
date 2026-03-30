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
    public float knockbackDistance { get; private set; }
    public float knockbackDirectionX { get; private set; }

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

        DisableHitBox();
    }

    private void OnDisable()
    {
        DisableHitBox();
    }

    public void Setup(
        float damage,
        float critChance,
        float critDamage,
        GameObject owner,
        float hitStunDuration = -1f,
        float hitActiveDuration = -1f,
        float knockbackDistance = 0f,
        float knockbackDirectionX = 0f)
    {
        this.damage = damage;
        this.critChance = critChance;
        this.critDamage = critDamage;
        this.owner = owner;
        this.hitActiveDuration = hitActiveDuration >= 0f ? hitActiveDuration : defaultHitActiveDuration;
        this.hitStunDuration = hitStunDuration >= 0f ? hitStunDuration : defaultHitStunDuration;
        this.knockbackDistance = Mathf.Max(0f, knockbackDistance);
        this.knockbackDirectionX = Mathf.Sign(knockbackDirectionX);
        hitTargetIds.Clear();

        hitActiveTimer = this.hitActiveDuration;
        isHitBoxActive = true;
        SetHitCollidersEnabled(true);
    }

    public bool TryRegisterHit(Component target)
    {
        if (target == null)
            return false;

        return hitTargetIds.Add(target.GetInstanceID());
    }

    private void SetHitCollidersEnabled(bool enabled)
    {
        if (hitColliders == null || hitColliders.Length == 0)
            hitColliders = GetComponentsInChildren<Collider2D>(true);

        for (int index = 0; index < hitColliders.Length; index++)
        {
            Collider2D hitCollider = hitColliders[index];
            if (hitCollider == null)
                continue;

            hitCollider.enabled = enabled;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleHit(other);
    }

    private void TryHandleHit(Collider2D other)
    {
        if (!CanHit(other))
            return;

        // 충돌 감지와 실제 데미지 적용 책임을 분리한다.
        MonsterHitReceiver hitReceiver = other.GetComponent<MonsterHitReceiver>();
        hitReceiver.ReceiveHit(this);
    }

    private bool CanHit(Collider2D other)
    {
        if (!isHitBoxActive || other == null)
            return false;

        // 현재 프로젝트에서는 몬스터 태그가 붙은 충돌체만 타격 대상으로 본다.
        if (!other.CompareTag("Monster"))
            return false;

        return true;
    }

    private void DisableHitBox()
    {
        isHitBoxActive = false;
        hitActiveTimer = 0f;
        SetHitCollidersEnabled(false);
    }
}
