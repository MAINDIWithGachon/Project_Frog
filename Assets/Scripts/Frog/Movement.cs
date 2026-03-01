using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("설정")]
    public float speed = 2.0f; 
    
    private float leftLimit; 
    private float rightLimit;
    private int direction = 1; 
    public SpriteRenderer spriteRenderer;

    void Start()
    {
        UpdateScreenLimits();
        // 시작 위치를 왼쪽 끝(-5였던 곳) 근처로 설정
       // transform.position = new Vector3(leftLimit + 0.1f, transform.position.y, transform.position.z);
    }

    void Update()
    {
        Move();
        CheckWallCollision(); // ClampPosition 대신 더 명확한 충돌 체크
    }

    private void UpdateScreenLimits()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float halfScreenWidth = cam.orthographicSize * cam.aspect;

        float screenLeft = cam.transform.position.x - halfScreenWidth;
        float screenRight = cam.transform.position.x + halfScreenWidth;
        
        float a = -2f;
        leftLimit = screenLeft + a;
        rightLimit = cam.transform.position.x; // 정확히 화면 중앙
    }


        private void Move()
    {
        float nextX = transform.position.x + direction * speed * Time.deltaTime;

        // 이동하려는 위치가 범위를 넘지 않을 때만 이동
        if (nextX >= leftLimit && nextX <= rightLimit)
        {
            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        }
    }

    public void ChangeDirection()
    {
        direction *= -1;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction == -1;
        }
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
    }
}