using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoticeUIController : MonoBehaviour
{
    private static NoticeUIController instance;
    public static NoticeUIController Instance => instance;

    [SerializeField] private GameObject _ui;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowNotice(string msg, Action confirmButtonAction = null)
    {
        _messageText.text = msg;

        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() =>
        {
            _ui.SetActive(false);
            confirmButtonAction?.Invoke();
        });

        _ui.SetActive(true);
    }
}