using System.Collections.Generic;
using UnityEngine;

public class Monster_Movement : MonoBehaviour
{
    private const string AttackParameterName = "Attack";

    [Header("추적 대상")]
    public Transform player;
    public Transform centerPivot;
    public Transform visualRoot;

    [Header("이동 설정")]
    public float speed = 2.0f;
    public float stoppingDistance = 0.1f;
    public float facingUpdateInterval = 0.1f;
    public Animator animator;

    [Header("방향")]
    public bool faceRightWhenScalePositive = true;

    private float baseVisualScaleX = 1f;
    private float cachedTargetX;
    private float facingUpdateTimer;
    private bool isAttacking;
    private int playerContactCount;
    private readonly HashSet<Collider2D> blockingHitBoxes = new();

    private void Awake()
    {
        EnsureCombatComponents();

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        float visualScaleX = Mathf.Abs(visualRoot.localScale.x);
        baseVisualScaleX = visualScaleX > 0.001f ? visualScaleX : 1f;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void EnsureCombatComponents()
    {
        if (GetComponent<MonsterHealth>() == null)
            gameObject.AddComponent<MonsterHealth>();

        if (GetComponent<MonsterHitReceiver>() == null)
            gameObject.AddComponent<MonsterHitReceiver>();
    }

    private void Start()
    {
        ResolvePlayerTargetIfNeeded();

        if (player == null)
        {
            Debug.LogWarning("Monster_Movement: player에 플레이어의 CenterPivot을 연결하세요.", this);
        }

        if (centerPivot == null)
        {
            Debug.LogWarning("Monster_Movement: centerPivot에 몬스터의 CenterPivot을 연결하세요.", this);
        }

        if (animator == null)
        {
            Debug.LogWarning("Monster_Movement: animator가 연결되지 않았습니다.", this);
        }

        RefreshTrackingState();
    }

    public void Initialize(Transform playerTarget)
    {
        player = playerTarget;
        RefreshTrackingState();
    }

    private void Update()
    {
        if (player == null || centerPivot == null)
        {
            return;
        }

        CleanupBlockingHitBoxes();

        // 플레이어의 월드 좌표를 일정 주기마다 읽어서 방향만 갱신한다.
        facingUpdateTimer += Time.deltaTime;

        if (facingUpdateTimer >= facingUpdateInterval)
        {
            facingUpdateTimer = 0f;
            RefreshTrackingState();
        }

        MoveToPlayer();
    }

    private void MoveToPlayer()
    {
        if (isAttacking || blockingHitBoxes.Count > 0)
        {
            return;
        }

        float remainingDistanceX = cachedTargetX - centerPivot.position.x;

        if (Mathf.Abs(remainingDistanceX) <= stoppingDistance)
        {
            return;
        }

        float deltaX = Mathf.Sign(remainingDistanceX) * speed * Time.deltaTime;

        if (Mathf.Abs(deltaX) > Mathf.Abs(remainingDistanceX))
        {
            deltaX = remainingDistanceX;
        }

        transform.position += new Vector3(deltaX, 0f, 0f);
    }

    private void RefreshTrackingState()
    {
        if (player == null || centerPivot == null)
        {
            isAttacking = false;
            playerContactCount = 0;
            SetAttack(false);
            return;
        }

        // 자식 피벗이라도 position은 월드 좌표이므로 기준점으로 그대로 사용한다.
        float monsterX = centerPivot.position.x;
        cachedTargetX = player.position.x;
        float distanceX = cachedTargetX - monsterX;

        UpdateFacing(distanceX);
    }

    private void UpdateFacing(float distanceX)
    {
        if (Mathf.Abs(distanceX) < 0.01f)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        float directionSign = distanceX > 0f ? -1f : 1f;

        if (!faceRightWhenScalePositive)
        {
            directionSign *= -1f;
        }

        scale.x = baseVisualScaleX * directionSign;
        visualRoot.localScale = scale;
    }

    private void SetAttack(bool isAttacking)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(AttackParameterName, isAttacking);
    }

    public void ApplyAttackHit()
    {
        if (!isAttacking)
        {
            return;
        }

        Debug.Log("몬스터 공격 적중: 플레이어 데미지 처리 예정");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerContactEnter(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        HandlePlayerContactExit(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerContactEnter(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandlePlayerContactExit(other);
    }

    private void HandlePlayerContactEnter(Component other)
    {
        if (TryRegisterBlockingHitBox(other))
        {
            return;
        }

        if (!IsPlayerComponent(other))
        {
            return;
        }

        playerContactCount++;
        isAttacking = true;
        SetAttack(true);
    }

    private void HandlePlayerContactExit(Component other)
    {
        if (TryUnregisterBlockingHitBox(other))
        {
            return;
        }

        if (!IsPlayerComponent(other))
        {
            return;
        }

        playerContactCount = Mathf.Max(0, playerContactCount - 1);

        if (playerContactCount > 0)
        {
            return;
        }

        isAttacking = false;
        SetAttack(false);
        RefreshTrackingState();
    }

    private bool IsPlayerComponent(Component other)
    {
        if (other == null || player == null)
        {
            return false;
        }

        return other.transform.root == player.root;
    }

    private bool TryRegisterBlockingHitBox(Component other)
    {
        if (other is not Collider2D collider)
            return false;

        if (collider.GetComponent<HitBoxModule>() == null)
            return false;

        blockingHitBoxes.Add(collider);
        return true;
    }

    private bool TryUnregisterBlockingHitBox(Component other)
    {
        if (other is not Collider2D collider)
            return false;

        if (collider.GetComponent<HitBoxModule>() == null)
            return false;

        blockingHitBoxes.Remove(collider);
        return true;
    }

    private void CleanupBlockingHitBoxes()
    {
        blockingHitBoxes.RemoveWhere(collider =>
            collider == null ||
            !collider.enabled ||
            !collider.gameObject.activeInHierarchy);
    }

    private void ResolvePlayerTargetIfNeeded()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = FindChildTransformByName(playerObject.transform, "CenterPivot");
    }

    private static Transform FindChildTransformByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
