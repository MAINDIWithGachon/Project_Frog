using UnityEngine;
using UnityEngine.Serialization;

public class CanChuck : MonoBehaviour, ISkillExecutable
{
    [Header("# Skill")]
    [SerializeField] private int skillId = 6;
    [SerializeField] private float testDamagePercent = 100f;

    [Header("# Projectile")]
    [SerializeField] private Collider2D hitBox;
    [FormerlySerializedAs("speed")]
    [Min(0f)]
    [SerializeField] private float throwSpeed = 8f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float spawnForwardOffset = 0.35f;

    private float damage;
    private float critChance;
    private float critDamage;
    private float currentLifeTime;
    private int moveDirection = 1;
    private bool isProjectileActive;
    private bool hasHit;
    private HitBoxModule runtimeHitBox;

    public int SkillId => skillId;

    public void TestCast()
    {
        FinalStatData finalStatData = FindAnyObjectByType<FinalStatData>();
        Movement movement = FindAnyObjectByType<Movement>();
        Transform slashPivot = FindPlayerChild("SlashPivot");

        SkillCastResult testCastResult = new SkillCastResult
        {
            skillId = skillId,
            skillLevel = 1,
            damagePercent = testDamagePercent,
            cooldown = 0f,
            currentAttack = finalStatData != null ? finalStatData.attack : 10f,
            currentCritChance = finalStatData != null ? finalStatData.critChance : 0f,
            currentCritDamage = finalStatData != null ? finalStatData.critDamage : 0f,
            slashPivot = slashPivot != null ? slashPivot : transform,
            facingDirection = movement != null && movement.direction < 0 ? -1 : 1
        };

        Execute(null, testCastResult);
    }

    private void Awake()
    {
        if (hitBox == null)
            hitBox = GetComponentInChildren<Collider2D>(true);

        runtimeHitBox = GetComponent<HitBoxModule>();
        if (runtimeHitBox == null)
            runtimeHitBox = gameObject.AddComponent<HitBoxModule>();

        runtimeHitBox.enabled = false;
    }

    private void OnEnable()
    {
        currentLifeTime = lifeTime;
        hasHit = false;

        if (hitBox != null)
            hitBox.enabled = true;

        if (runtimeHitBox == null)
            runtimeHitBox = GetComponent<HitBoxModule>();

        if (runtimeHitBox != null)
            runtimeHitBox.enabled = false;
    }

    private void Update()
    {
        if (!isProjectileActive)
            return;

        transform.position += Vector3.right * (moveDirection * throwSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, -moveDirection * rotationSpeed * Time.deltaTime);

        if (lifeTime <= 0f)
            return;

        currentLifeTime -= Time.deltaTime;
        if (currentLifeTime <= 0f)
            gameObject.SetActive(false);
    }

    public void Execute(SkillData skillData, SkillCastResult castResult)
    {
        if (PoolingManager.instance == null || PoolingManager.instance.skillPrefabPooling == null)
        {
            Debug.LogError("[CanChuck] SkillPrefabPooling reference is missing.");
            return;
        }

        GameObject tunaCan = PoolingManager.instance.skillPrefabPooling.Get(SkillId);
        if (tunaCan == null)
            return;

        CanChuck projectile = tunaCan.GetComponent<CanChuck>();
        if (projectile == null)
            projectile = tunaCan.GetComponentInChildren<CanChuck>(true);

        if (projectile == null)
        {
            Debug.LogError("[CanChuck] CanChuck component was not found on the pooled tuna can prefab.", tunaCan);
            tunaCan.SetActive(false);
            return;
        }

        Vector3 spawnPosition = castResult.slashPivot != null
            ? castResult.slashPivot.position
            : transform.position;
        spawnPosition.x += castResult.facingDirection * spawnForwardOffset;

        tunaCan.transform.position = spawnPosition;
        tunaCan.transform.rotation = Quaternion.identity;

        projectile.InitializeProjectile(castResult);
    }

    private void InitializeProjectile(SkillCastResult castResult)
    {
        moveDirection = castResult.facingDirection < 0 ? -1 : 1;
        damage = castResult.currentAttack * (castResult.damagePercent / 100f);
        critChance = castResult.currentCritChance / 100f;
        critDamage = castResult.currentCritDamage;
        currentLifeTime = lifeTime;
        hasHit = false;
        isProjectileActive = true;

        if (hitBox != null)
            hitBox.enabled = true;

        if (runtimeHitBox == null)
            runtimeHitBox = GetComponent<HitBoxModule>();

        if (runtimeHitBox != null)
        {
            runtimeHitBox.Setup(
                damage,
                critChance,
                critDamage,
                gameObject,
                knockbackDirectionX: moveDirection,
                keepActiveUntilDisabled: true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!isProjectileActive || hasHit || other == null || !other.CompareTag("Monster"))
            return;

        MonsterHitReceiver hitReceiver = other.GetComponent<MonsterHitReceiver>();
        if (hitReceiver == null)
            hitReceiver = other.GetComponentInParent<MonsterHitReceiver>();
        if (hitReceiver == null)
            hitReceiver = other.GetComponentInChildren<MonsterHitReceiver>(true);
        if (hitReceiver == null || runtimeHitBox == null)
            return;

        hasHit = true;
        isProjectileActive = false;

        hitReceiver.ReceiveHit(runtimeHitBox);

        if (hitBox != null)
            hitBox.enabled = false;

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        isProjectileActive = false;
        hasHit = false;
    }

    private static Transform FindPlayerChild(string childName)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return null;

        Transform[] children = playerObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
