using UnityEngine;
using UnityEngine.EventSystems;

public class TabPressVisualSwitcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameObject normalRoot;
    [SerializeField] private GameObject pressedRoot;

    private bool isPressed;

    private void Awake()
    {
        CacheReferences();
        SetPressed(false);
    }

    private void OnEnable()
    {
        CacheReferences();
        SetPressed(false);
    }

    private void OnDisable()
    {
        SetPressed(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void CacheReferences()
    {
        normalRoot ??= FindDescendantByName(transform, "Normal_01")?.gameObject;
        pressedRoot ??= FindDescendantByName(transform, "Normal_02")?.gameObject;
    }

    private void SetPressed(bool pressed)
    {
        isPressed = pressed;
        SetActive(normalRoot, !isPressed);
        SetActive(pressedRoot, isPressed);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform found = FindDescendantByName(child, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
