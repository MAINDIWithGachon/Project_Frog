using UnityEngine;

public class Bullet_Fire : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 2f;

    private float currentLifeTime;
    private int moveDirection = 1;

    private void OnEnable()
    {
        currentLifeTime = lifeTime;
    }

    private void Update()
    {
        transform.position += Vector3.right * (moveDirection * speed * Time.deltaTime);

        if (lifeTime <= 0f)
            return;

        currentLifeTime -= Time.deltaTime;
        if (currentLifeTime <= 0f)
            gameObject.SetActive(false);
    }

    public void Initialize(int direction)
    {
        moveDirection = direction < 0 ? -1 : 1;
        currentLifeTime = lifeTime;
    }
}
