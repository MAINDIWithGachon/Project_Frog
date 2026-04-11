using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnimplementedButtonNoticeManager : MonoBehaviour
{
    [Header("# Buttons")]
    [SerializeField] private List<Button> targetButtons = new List<Button>();

    [Header("# Notice")]
    [SerializeField] private GameObject noticeObject;
    [SerializeField] private float visibleDuration = 1f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void ShowNotice()
    {
        if (noticeObject == null)
        {
            Debug.LogWarning("[UnimplementedButtonNoticeManager] Notice object is missing.", this);
            return;
        }

        noticeObject.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideNoticeAfterDelay());
    }

    private IEnumerator HideNoticeAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        if (noticeObject != null)
            noticeObject.SetActive(false);

        hideCoroutine = null;
    }

    private void BindButtons()
    {
        for (int i = 0; i < targetButtons.Count; i++)
        {
            Button button = targetButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveListener(ShowNotice);
            button.onClick.AddListener(ShowNotice);
        }
    }

    private void UnbindButtons()
    {
        for (int i = 0; i < targetButtons.Count; i++)
        {
            Button button = targetButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveListener(ShowNotice);
        }
    }
}
