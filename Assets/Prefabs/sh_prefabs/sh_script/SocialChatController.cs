using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SocialChatController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Outgoing Message")]
    [SerializeField] private GameObject outgoingMessagePrefab;
    [SerializeField] private string outgoingObjectName = "User_chat";
    [SerializeField] private string myUserName = "";
    [SerializeField] private string timeFormat = "HH:mm";
    [SerializeField] private bool showMyUserName;

    [Header("AI Message")]
    [SerializeField] private GameObject aiMessagePrefab;
    [SerializeField] private string aiObjectName = "AI_answer";
    [SerializeField] private string aiUserName = "";
    [SerializeField] private bool showAiUserName = true;

    private void Reset()
    {
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (sendButton == null)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name.IndexOf("send", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sendButton = button;
                    break;
                }
            }
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        if (contentRoot == null && scrollRect != null)
        {
            contentRoot = scrollRect.content;
        }
    }

    private void Awake()
    {
        if (contentRoot == null && scrollRect != null)
        {
            contentRoot = scrollRect.content;
        }

        CleanupGeneratedMessages();

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendCurrentMessage);
        }

        if (inputField != null)
        {
            inputField.onSubmit.AddListener(HandleSubmit);
        }
    }

    private void OnDestroy()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendCurrentMessage);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(HandleSubmit);
        }
    }

    private void HandleSubmit(string _)
    {
        SendCurrentMessage();
    }

    public void SendCurrentMessage()
    {
        if (inputField == null || outgoingMessagePrefab == null || contentRoot == null)
        {
            Debug.LogWarning("SocialChatController references are missing.");
            return;
        }

        var message = inputField.text;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        CreateOutgoingMessage(message.Trim());

        inputField.text = string.Empty;
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void CreateOutgoingMessage(string message)
    {
        var instance = Instantiate(outgoingMessagePrefab, contentRoot);
        instance.name = outgoingObjectName;
        instance.transform.SetAsLastSibling();

        ConfigureMessage(instance.transform, message, myUserName, showMyUserName, TextAlignmentOptions.Right);
        RefreshLayout();
    }

    public void AddAIMessage(string message)
    {
        if (aiMessagePrefab == null || contentRoot == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var instance = Instantiate(aiMessagePrefab, contentRoot);
        instance.name = aiObjectName;
        instance.transform.SetAsLastSibling();

        ConfigureMessage(instance.transform, message.Trim(), aiUserName, showAiUserName, TextAlignmentOptions.Left);
        RefreshLayout();
    }

    private void ConfigureMessage(
        Transform root,
        string message,
        string overrideUserName,
        bool shouldShowUserName,
        TextAlignmentOptions nameAlignment)
    {
        var sanitizedMessage = SanitizeMessage(message);

        var messageText = FindMessageText(root);
        if (messageText != null)
        {
            messageText.text = sanitizedMessage;
            messageText.alignment = TextAlignmentOptions.TopLeft;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
        }

        var timeText = FindTimeText(root);
        if (timeText != null)
        {
            timeText.text = DateTime.Now.ToString(timeFormat);
            timeText.alignment = TextAlignmentOptions.Right;
        }

        var nameText = FindNameText(root);
        if (nameText != null)
        {
            if (!string.IsNullOrWhiteSpace(overrideUserName))
            {
                nameText.text = overrideUserName;
            }

            nameText.gameObject.SetActive(shouldShowUserName);
            nameText.alignment = nameAlignment;
        }
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }
    }

    private void CleanupGeneratedMessages()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (var i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            if (child.name == outgoingObjectName || child.name == aiObjectName || child.name == "OutgoingMessage")
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static void SetFirstText(Transform root, string value, params string[] childNames)
    {
        var text = FindFirstText(root, childNames);
        if (text != null)
        {
            text.text = value;
        }
    }

    private static TMP_Text FindFirstText(Transform root, params string[] childNames)
    {
        foreach (var childName in childNames)
        {
            var target = FindChild(root, childName);
            if (target == null)
            {
                continue;
            }

            var text = target.GetComponent<TMP_Text>();
            if (text != null)
            {
                return text;
            }
        }

        return null;
    }

    private static TMP_Text FindMessageText(Transform root)
    {
        var direct = FindFirstText(
            root,
            "MessageText",
            "Text (TMP)",
            "MessageText_User",
            "UserMessageText",
            "ChatMessageText",
            "BodyText");

        if (direct != null)
        {
            return direct;
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            var lowerName = text.name.ToLowerInvariant();
            if (lowerName.Contains("message") || lowerName.Contains("body"))
            {
                return text;
            }
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            var lowerName = text.name.ToLowerInvariant();
            if (!lowerName.Contains("time") && !lowerName.Contains("name"))
            {
                return text;
            }
        }

        return null;
    }

    private static TMP_Text FindTimeText(Transform root)
    {
        var direct = FindFirstText(
            root,
            "Text_Time",
            "TimeText",
            "MessageTimeText",
            "ChatTimeText",
            "TimeLabel");

        if (direct != null)
        {
            return direct;
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name.ToLowerInvariant().Contains("time"))
            {
                return text;
            }
        }

        return null;
    }

    private static TMP_Text FindNameText(Transform root)
    {
        var direct = FindFirstText(
            root,
            "Text_Name",
            "UserNameText",
            "SenderNameText",
            "NameText",
            "ChatNameText");

        if (direct != null)
        {
            return direct;
        }

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name.ToLowerInvariant().Contains("name"))
            {
                return text;
            }
        }

        return null;
    }

    private static void SetActive(Transform root, string childName, bool active)
    {
        var target = FindChild(root, childName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static string SanitizeMessage(string message)
    {
        return message
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'')
            .Replace('\u201c', '"')
            .Replace('\u201d', '"');
    }
}
