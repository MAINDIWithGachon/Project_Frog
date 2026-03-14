using UnityEngine;

public class SkillEffectAutoDisable : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private GameObject disableTarget;

    private bool isPlaying;

    private void OnEnable()
    {
        isPlaying = true;

        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        if (disableTarget == null)
            disableTarget = ResolveDisableTarget();

        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        bool anyAlive = false;

        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;

            if (ps.IsAlive(true))
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            isPlaying = false;
            if (disableTarget != null)
                disableTarget.SetActive(false);
        }
    }

    private GameObject ResolveDisableTarget()
    {
        Transform current = transform;
        SkillPrefabPooling pool = GetComponentInParent<SkillPrefabPooling>();

        if (pool != null)
        {
            while (current.parent != null && current.parent != pool.transform)
            {
                current = current.parent;
            }

            return current.gameObject;
        }

        while (current.parent != null)
        {
            current = current.parent;
        }

        return current.gameObject;
    }
}
