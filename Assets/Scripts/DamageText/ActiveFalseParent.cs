using UnityEngine;

public class ActiveFalseParent : MonoBehaviour
{
    [SerializeField] private GameObject disableTarget;

    private void Awake()
    {
        if (disableTarget == null && transform.parent != null)
            disableTarget = transform.parent.gameObject;
    }

    public void ActiveFalse_Parent()
    {
        if (disableTarget == null)
        {
            Debug.LogWarning("[ActiveFalseParent] Disable target is missing.", this);
            return;
        }

        disableTarget.SetActive(false);
    }
}
