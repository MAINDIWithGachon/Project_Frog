using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("설정")]
    public float speed = 2.0f;
    private const float LeftMoveSpeedMultiplier = 1.5f;

    private float originalSpeed;
    private float leftLimit;
    private float rightLimit;
    public int direction = 1; // 1: 오른쪽, -1: 왼쪽
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer hat_SpriteRenderer;

    void Awake()
    {
        originalSpeed = speed;
    }

    void Start()
    {
        UpdateScreenLimits();
        UpdateFlip();
        // 시작 위치를 왼쪽 끝(-5였던 곳) 근처로 설정
        // transform.position = new Vector3(leftLimit + 0.1f, transform.position.y, transform.position.z);
    }

    void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        Move();
        CheckWallCollision(); // ClampPosition 대신 더 명확한 충돌 체크
    }

    private void UpdateScreenLimits()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float halfScreenWidth = cam.orthographicSize * cam.aspect;

        float screenLeft = cam.transform.position.x - halfScreenWidth;
        float leftOffset = -2f;

        leftLimit = screenLeft + leftOffset;
        rightLimit = cam.transform.position.x; // 정확히 화면 중앙
    }

    private void Move()
    {
        float moveSpeed = speed;
        if (direction == -1)
        {
            moveSpeed *= LeftMoveSpeedMultiplier;
        }

        float nextX = transform.position.x + direction * moveSpeed * Time.deltaTime;

        // 이동하려는 위치가 범위를 넘지 않을 때만 이동
        if (nextX >= leftLimit && nextX <= rightLimit)
        {
            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        }
    }

    public void ChangeDirection()
    {
        if(GameStateManager.Instance.CurrentState == GameState.PlayerDead)
        return;
        
        direction *= -1;
        UpdateFlip();
    }

    public void StopMovement()
    {
        speed = 0f;
        GameStateManager.Instance?.SetState(GameState.PlayerDead);
    }

    public void ResumeMovement()
    {
        speed = originalSpeed;
    }

    private void CheckWallCollision()
    {
        // 실제 좌표 제한 (떨림 방지)
        float clampedX = Mathf.Clamp(transform.position.x, leftLimit, rightLimit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void UpdateFlip()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction == -1;
        }

        if (hat_SpriteRenderer != null)
            hat_SpriteRenderer.flipX = direction == -1;
    }
}
