using UnityEngine;

public class Monster_ATK : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float damage = 1f;

    public float Damage => damage;
}
