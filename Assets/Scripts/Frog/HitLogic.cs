using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitLogic : MonoBehaviour
{
    private const int OverlapBufferSize = 16;

    [Header("Reference")]
    [SerializeField] private Health health;
    [SerializeField] private Movement movement;
    [SerializeField] private SpriteRenderer[] blinkRenderers;
    [SerializeField] private Collider2D[] hitCheckColliders;

    [Header("Hit Reaction")]
    [SerializeField] private string monsterTag = "Monster";
    [SerializeField] private float knockbackDistance = 0.35f;
    [SerializeField] private float knockbackDuration = 0.12f;
    [SerializeField] private float invincibleDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;

    private Coroutine hitReactionCoroutine;
    private bool isInvincible;
    private readonly Collider2D[] overlapResults = new Collider2D[OverlapBufferSize];

    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (movement == null)
            movement = GetComponent<Movement>();

        CacheHitCheckColliders();
        CacheBlinkRenderers();
    }

    private void OnDisable()
    {
        if (hitReactionCoroutine != null)
        {
            StopCoroutine(hitReactionCoroutine);
            hitReactionCoroutine = null;
        }

        isInvincible = false;
        SetBlinkVisible(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHandleMonsterHit(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleMonsterHit(other);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHandleMonsterHit(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHandleMonsterHit(other);
    }

    private void TryHandleMonsterHit(Component other)
    {
        if (!enabled || isInvincible || other == null)
            return;

        Monster_ATK monsterAtk = ResolveMonsterAttack(other);
        if (monsterAtk == null)
            return;

        if (!IsMonsterComponent(other))
            return;

        if (health != null)
            health.TakeDamage(monsterAtk.Damage);

        Vector3 hitSourcePosition = other.transform.position;

        if (hitReactionCoroutine != null)
            StopCoroutine(hitReactionCoroutine);

        hitReactionCoroutine = StartCoroutine(HitReactionRoutine(hitSourcePosition));
    }

    private Monster_ATK ResolveMonsterAttack(Component other)
    {
        Monster_ATK monsterAtk = other.GetComponent<Monster_ATK>();
        if (monsterAtk != null)
            return monsterAtk;

        monsterAtk = other.GetComponentInParent<Monster_ATK>();
        if (monsterAtk != null)
            return monsterAtk;

        return other.GetComponentInChildren<Monster_ATK>(true);
    }

    private bool IsMonsterComponent(Component other)
    {
        if (other.gameObject.CompareTag(monsterTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(monsterTag);
    }

    private IEnumerator HitReactionRoutine(Vector3 hitSourcePosition)
    {
        isInvincible = true;
        float knockbackDirection = transform.position.x >= hitSourcePosition.x ? 1f : -1f;
        yield return KnockbackRoutine(knockbackDirection);
        yield return BlinkRoutine(invincibleDuration);

        SetBlinkVisible(true);
        isInvincible = false;
        hitReactionCoroutine = null;

        // Physics stay callbacks can miss the exact invincibility-ending frame,
        // so re-check current overlaps once and immediately reapply damage if needed.
        TryHandleCurrentMonsterOverlap();
    }

    private void TryHandleCurrentMonsterOverlap()
    {
        CacheHitCheckColliders();

        if (hitCheckColliders == null || hitCheckColliders.Length == 0)
            return;

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true;
        contactFilter.NoFilter();

        for (int colliderIndex = 0; colliderIndex < hitCheckColliders.Length; colliderIndex++)
        {
            Collider2D selfCollider = hitCheckColliders[colliderIndex];
            if (selfCollider == null || !selfCollider.enabled)
                continue;

            int overlapCount = selfCollider.Overlap(contactFilter, overlapResults);
            for (int resultIndex = 0; resultIndex < overlapCount; resultIndex++)
            {
                Collider2D other = overlapResults[resultIndex];
                if (other == null || other.transform.root == transform.root)
                    continue;

                TryHandleMonsterHit(other);
                if (isInvincible)
                    return;
            }
        }
    }


    private IEnumerator KnockbackRoutine(float direction)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + new Vector3(direction * knockbackDistance, 0f, 0f);

        if (knockbackDuration <= 0f)
        {
            transform.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / knockbackDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        bool isVisible = false;

        while (elapsed < duration)
        {
            isVisible = !isVisible;
            SetBlinkVisible(isVisible);

            float waitTime = Mathf.Min(blinkInterval, duration - elapsed);
            elapsed += waitTime;
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void CacheBlinkRenderers()
    {
        if (blinkRenderers != null && blinkRenderers.Length > 0)
            return;

        List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        if (movement != null)
        {
            if (movement.spriteRenderer != null)
                renderers.Add(movement.spriteRenderer);

            if (movement.hat_SpriteRenderer != null && movement.hat_SpriteRenderer != movement.spriteRenderer)
                renderers.Add(movement.hat_SpriteRenderer);
        }

        if (renderers.Count == 0)
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < childRenderers.Length; index++)
            {
                if (childRenderers[index] == null)
                    continue;

                renderers.Add(childRenderers[index]);
            }
        }

        blinkRenderers = renderers.ToArray();
    }

    private void CacheHitCheckColliders()
    {
        if (hitCheckColliders != null && hitCheckColliders.Length > 0)
            return;

        hitCheckColliders = GetComponents<Collider2D>();

        if (hitCheckColliders == null || hitCheckColliders.Length == 0)
            hitCheckColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void SetBlinkVisible(bool isVisible)
    {
        if (blinkRenderers == null)
            return;

        for (int index = 0; index < blinkRenderers.Length; index++)
        {
            if (blinkRenderers[index] == null)
                continue;

            blinkRenderers[index].enabled = isVisible;
        }
    }
  
}
