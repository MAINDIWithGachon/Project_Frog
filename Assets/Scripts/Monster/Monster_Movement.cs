using UnityEngine;

public class Monster_Movement : MonoBehaviour
{
    private const string AttackParameterName = "Attack";

    private enum RandomMoveType
    {
        MoveLeft,
        MoveRight,
        Idle
    }

    [Header("추적 대상")]
    public Transform player;
    public Transform centerPivot;
    public Transform visualRoot;
    public Transform movementRoot;

    [Header("이동 설정")]
    public float speed = 2.0f;
    public float stoppingDistance = 0.1f;
    public float facingUpdateInterval = 0.1f;
    public float defaultHitStunDuration = 0.1f;
    public Animator animator;

    [Header("랜덤 이동 패턴")]
    [SerializeField] private float moveLeftDuration = 2f;
    [SerializeField] private float moveRightDuration = 1f;
    [SerializeField] private float idleDuration = 1f;

    [Header("이동 범위 제한")]
    [SerializeField] private Vector3 leftLimitPosition = new Vector3(-10f, 0f, 0f);
    [SerializeField] private Vector3 rightLimitPosition = new Vector3(15f, 0f, 0f);

    [Header("방향")]
    public bool faceRightWhenScalePositive = true;

    private float baseVisualScaleX = 1f;
    private Vector3 baseVisualLocalScale = Vector3.one;
    private Quaternion baseVisualLocalRotation = Quaternion.identity;
    private float cachedTargetX;
    private float facingUpdateTimer;
    private float hitStunTimer;
    private bool isAttacking;
    private bool isDead;
    private int playerContactCount;
    private float baseSpeed;
    private float movePatternTimer;
    private RandomMoveType currentMoveType;
    private float leftLimit;
    private float rightLimit;
    private bool hasStartedRandomPattern;

    private void Awake()
    {
        baseSpeed = speed;
        ApplyStageDifficulty();

        EnsureCombatComponents();

        if (movementRoot == null)
        {
            movementRoot = transform.root;
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        float visualScaleX = Mathf.Abs(visualRoot.localScale.x);
        baseVisualScaleX = visualScaleX > 0.001f ? visualScaleX : 1f;
        baseVisualLocalScale = visualRoot.localScale;
        baseVisualLocalRotation = visualRoot.localRotation;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void ApplyStageDifficulty()
    {
        StageRuntimeContext runtime = StageManager.Instance != null ? StageManager.Instance.runtime : null;
        if (runtime == null)
        {
            speed = baseSpeed;
            return;
        }

        speed = baseSpeed * runtime.finalMonsterSpeedMultiplier;
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
        UpdateMovementLimits();
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
    }
    private void CheckWallCollision()
    {
        if (movementRoot == null)
            return;

        float clampedX = Mathf.Clamp(movementRoot.position.x, leftLimit, rightLimit);
        movementRoot.position = new Vector3(clampedX, movementRoot.position.y, movementRoot.position.z);
    }

    public void Initialize(Transform playerTarget)
    {
        player = playerTarget;
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (hitStunTimer > 0f)
        {
            hitStunTimer = Mathf.Max(0f, hitStunTimer - Time.deltaTime);
        }

        UpdateMovementLimits();
        UpdateRandomMovement();
        CheckWallCollision();
    }

    private void UpdateRandomMovement()
    {
        if (isAttacking || hitStunTimer > 0f)
        {
            return;
        }

        if (movePatternTimer <= 0f)
        {
            SelectNextRandomMove(forceSelection: false);
        }

        movePatternTimer = Mathf.Max(0f, movePatternTimer - Time.deltaTime);

        switch (currentMoveType)
        {
            case RandomMoveType.MoveLeft:
                MoveHorizontally(-1f);
                break;

            case RandomMoveType.MoveRight:
                MoveHorizontally(1f);
                break;
        }
    }

    private void SelectNextRandomMove(bool forceSelection)
    {
        if (!forceSelection && movePatternTimer > 0f)
            return;

        if (!hasStartedRandomPattern)
        {
            currentMoveType = RandomMoveType.MoveLeft;
            hasStartedRandomPattern = true;
        }
        else
        {
            currentMoveType = (RandomMoveType)Random.Range(0, 3);
        }

        switch (currentMoveType)
        {
            case RandomMoveType.MoveLeft:
                movePatternTimer = moveLeftDuration;
                UpdateFacing(-1f);
                break;

            case RandomMoveType.MoveRight:
                movePatternTimer = moveRightDuration;
                UpdateFacing(1f);
                break;

            default:
                movePatternTimer = idleDuration;
                break;
        }
    }

    private void MoveHorizontally(float directionX)
    {
        if (movementRoot == null || Mathf.Approximately(directionX, 0f))
            return;

        movementRoot.position += new Vector3(directionX * speed * Time.deltaTime, 0f, 0f);
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

    public void ApplyHitStun(float duration)
    {
        if (isDead)
            return;

        float finalDuration = duration > 0f ? duration : defaultHitStunDuration;
        hitStunTimer = Mathf.Max(hitStunTimer, finalDuration);
    }

    public void ApplyKnockback(float distance, float directionX)
    {
        if (isDead || Mathf.Approximately(distance, 0f) || Mathf.Approximately(directionX, 0f))
            return;

        movementRoot.position += new Vector3(distance * Mathf.Sign(directionX), 0f, 0f);
    }

    public void StopForDeath()
    {
        isDead = true;
        isAttacking = false;
        playerContactCount = 0;
        hitStunTimer = 0f;
        SetAttack(false);
    }

    private void ResolvePlayerTargetIfNeeded()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        Transform[] children = playerObject.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < children.Length; index++)
        {
            if (children[index].name == "CenterPivot")
            {
                player = children[index];
                return;
            }
        }
    }

    private void UpdateMovementLimits()
    {
        if (movementRoot == null)
            return;

        float monsterHalfWidth = GetMonsterHalfWidth();
        leftLimit = leftLimitPosition.x + monsterHalfWidth;
        rightLimit = rightLimitPosition.x - monsterHalfWidth;

        if (leftLimit > rightLimit)
        {
            float middleX = (leftLimit + rightLimit) * 0.5f;
            leftLimit = middleX;
            rightLimit = middleX;
        }
    }

    private float GetMonsterHalfWidth()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0)
            return 0f;

        Bounds combinedBounds = colliders[0].bounds;

        for (int index = 1; index < colliders.Length; index++)
        {
            if (colliders[index] == null)
                continue;

            combinedBounds.Encapsulate(colliders[index].bounds);
        }

        return combinedBounds.extents.x;
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
        if (isDead)
            return;

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
        if (isDead)
            return;

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

        if (currentMoveType == RandomMoveType.MoveLeft)
        {
            UpdateFacing(-1f);
        }
        else if (currentMoveType == RandomMoveType.MoveRight)
        {
            UpdateFacing(1f);
        }
    }

    private bool IsPlayerComponent(Component other)
    {
        if (other == null || player == null)
        {
            return false;
        }

        return other.transform.root == player.root;
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
