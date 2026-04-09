using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("# Reference")]
    [SerializeField] private Button targetButton;
    public Animator Pushanim;

    [Header("# Repeat")]
    [SerializeField] private float initialDelay = 0.4f;
    [SerializeField] private float repeatInterval = 0.1f;

    private Coroutine repeatCoroutine;
    private bool isPointerDown;
    private bool didRepeat;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
    }

    private void OnDisable()
    {
        StopRepeating();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsButtonAvailable())
            return;
        Pushanim.SetTrigger("Push");


        StopRepeating();

        isPointerDown = true;
        didRepeat = false;
        repeatCoroutine = StartCoroutine(RepeatClickRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bool wasRepeated = didRepeat;
        StopRepeating();

        if (wasRepeated)
            StartCoroutine(SuppressReleaseClickRoutine());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopRepeating();
    }

    private IEnumerator RepeatClickRoutine()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (isPointerDown)
        {
            if (!IsButtonAvailable())
            {
                StopRepeating();
                yield break;
            }

            didRepeat = true;
            targetButton.onClick.Invoke();

            yield return new WaitForSeconds(Mathf.Max(0.01f, repeatInterval));
        }
    }

    private IEnumerator SuppressReleaseClickRoutine()
    {
        bool wasInteractable = targetButton != null && targetButton.interactable;

        if (targetButton != null)
            targetButton.interactable = false;

        yield return null;

        if (targetButton != null)
            targetButton.interactable = wasInteractable;
    }

    private bool IsButtonAvailable()
    {
        return targetButton != null && targetButton.isActiveAndEnabled && targetButton.interactable;
    }

    private void StopRepeating()
    {
        isPointerDown = false;
        didRepeat = false;

        if (repeatCoroutine == null)
            return;

        StopCoroutine(repeatCoroutine);
        repeatCoroutine = null;
    }
}
