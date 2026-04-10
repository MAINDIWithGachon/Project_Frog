using UnityEngine;

public class FrogAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private Health health;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();
    }

    public void OnDeathAnimationFinished()
    {
        health?.OnDeathAnimationFinished();
    }
}