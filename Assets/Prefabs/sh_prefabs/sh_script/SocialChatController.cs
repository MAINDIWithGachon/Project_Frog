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
    [SerializeField] private float outgoingRightPadding = 16f;
    [SerializeField] private float outgoingLeftPadding = 110f;

    [Header("Message Prefab")]
    [SerializeField] private GameObject outgoingMessagePrefab;
    [SerializeField] private GameObject outgoingProfilePrefab;
    [SerializeField] private string myUserName = "Me";
    [SerializeField] private string timeFormat = "tt h:mm";

    private void Reset()
    {
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (sendButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
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
        instance.name = "OutgoingMessage";
        instance.transform.SetAsLastSibling();

        SetupOutgoingLayout(instance);
        var sanitizedMessage = SanitizeMessage(message);
        SetFirstText(instance.transform, sanitizedMessage, "MessageText", "Text (TMP)");
        SetFirstText(instance.transform, DateTime.Now.ToString(timeFormat), "TimeText", "Text_Time");
        SetFirstText(instance.transform, myUserName, "UserNameText", "Text_Name");
        SetActive(instance.transform, "UserNameText", false);
        SetActive(instance.transform, "Text_Name", false);
        SetActive(instance.transform, "ProfileArea", false);
        ApplyOutgoingProfile(instance.transform);

        RefreshLayout();
    }

    private void SetupOutgoingLayout(GameObject messageObject)
    {
        if (IsUserMessagePrefab(messageObject.transform))
        {
            SetupUserMessageLayout(messageObject);
            return;
        }

        var rootRect = messageObject.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.offsetMin = new Vector2(0f, rootRect.offsetMin.y);
            rootRect.offsetMax = new Vector2(0f, rootRect.offsetMax.y);
        }

        var rootLayoutElement = messageObject.GetComponent<LayoutElement>();
        if (rootLayoutElement != null)
        {
            rootLayoutElement.ignoreLayout = false;
            rootLayoutElement.flexibleWidth = -1f;
            rootLayoutElement.flexibleHeight = -1f;
            rootLayoutElement.preferredWidth = -1f;
            rootLayoutElement.minWidth = -1f;
        }

        var rootLayout = messageObject.GetComponent<HorizontalLayoutGroup>();
        if (rootLayout != null)
        {
            rootLayout.childAlignment = TextAnchor.UpperRight;
            rootLayout.reverseArrangement = true;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.padding.left = 450;
            rootLayout.padding.right = 0;
            rootLayout.padding.top = 5;
            rootLayout.padding.bottom = 5;
            rootLayout.spacing = 10f;
        }

        var messageArea = FindChild(messageObject.transform, "MessageArea");
        if (messageArea == null)
        {
            messageArea = FindChild(messageObject.transform, "MessageContainer");
        }
        if (messageArea != null)
        {
            var messageAreaRect = messageArea.GetComponent<RectTransform>();
            if (messageAreaRect != null)
            {
                messageAreaRect.anchorMin = new Vector2(0f, 1f);
                messageAreaRect.anchorMax = new Vector2(0f, 1f);
                messageAreaRect.pivot = new Vector2(0f, 1f);
                messageAreaRect.anchoredPosition = Vector2.zero;
            }

            var verticalLayout = messageArea.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.childAlignment = TextAnchor.UpperRight;
                verticalLayout.childControlWidth = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
            }
        }

        var bubbleRow = FindChild(messageObject.transform, "BubbleRow");
        if (bubbleRow == null)
        {
            bubbleRow = FindChild(messageObject.transform, "GameObject");
        }
        if (bubbleRow != null)
        {
            var bubbleLayout = bubbleRow.GetComponent<HorizontalLayoutGroup>();
            if (bubbleLayout != null)
            {
                bubbleLayout.childAlignment = TextAnchor.LowerRight;
                bubbleLayout.reverseArrangement = true;
                bubbleLayout.childForceExpandWidth = false;
                bubbleLayout.childForceExpandHeight = false;
            }
        }

        var profileImage = FindChild(messageObject.transform, "ProfileImage");
        if (profileImage != null)
        {
            var profileLayout = profileImage.GetComponent<LayoutElement>();
            if (profileLayout != null)
            {
                profileLayout.ignoreLayout = false;
                profileLayout.preferredWidth = 60f;
                profileLayout.preferredHeight = 60f;
            }
        }

        var messageText = FindFirstText(messageObject.transform, "MessageText", "Text (TMP)");
        if (messageText != null)
        {
            messageText.alignment = TextAlignmentOptions.TopLeft;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
        }

        var timeText = FindFirstText(messageObject.transform, "TimeText", "Text_Time");
        if (timeText != null)
        {
            timeText.alignment = TextAlignmentOptions.Right;
        }
    }

    private void SetupUserMessageLayout(GameObject messageObject)
    {
        var rootRect = messageObject.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.offsetMin = new Vector2(0f, rootRect.offsetMin.y);
            rootRect.offsetMax = new Vector2(0f, rootRect.offsetMax.y);
        }

        var rootLayout = messageObject.GetComponent<HorizontalLayoutGroup>();
        if (rootLayout != null)
        {
            rootLayout.childAlignment = TextAnchor.UpperRight;
            rootLayout.reverseArrangement = false;
            rootLayout.padding.left = 320;
            rootLayout.padding.right = 18;
            rootLayout.padding.top = 0;
            rootLayout.padding.bottom = 6;
            rootLayout.childControlWidth = false;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 0f;
        }

        var profileArea = FindChild(messageObject.transform, "ProfileArea");
        if (profileArea != null)
        {
            profileArea.gameObject.SetActive(false);
        }

        var messageContainer = FindChild(messageObject.transform, "MessageContainer");
        if (messageContainer != null)
        {
            var messageContainerRect = messageContainer.GetComponent<RectTransform>();
            if (messageContainerRect != null)
            {
                messageContainerRect.anchorMin = new Vector2(1f, 1f);
                messageContainerRect.anchorMax = new Vector2(1f, 1f);
                messageContainerRect.pivot = new Vector2(1f, 1f);
                messageContainerRect.anchoredPosition = Vector2.zero;
                messageContainerRect.sizeDelta = new Vector2(0f, messageContainerRect.sizeDelta.y);
            }

            var verticalLayout = messageContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.childAlignment = TextAnchor.UpperRight;
                verticalLayout.childControlWidth = false;
                verticalLayout.childControlHeight = true;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.spacing = 2f;
            }
        }

        var row = FindChild(messageObject.transform, "GameObject");
        if (row != null)
        {
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                rowLayout.childAlignment = TextAnchor.LowerRight;
                rowLayout.reverseArrangement = true;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = false;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
                rowLayout.spacing = 8f;
            }
        }

        var messageText = FindFirstText(messageObject.transform, "Text (TMP)", "MessageText");
        if (messageText != null)
        {
            messageText.alignment = TextAlignmentOptions.TopLeft;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Overflow;
        }

        var timeText = FindFirstText(messageObject.transform, "Text_Time", "TimeText");
        if (timeText != null)
        {
            timeText.alignment = TextAlignmentOptions.Left;
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

    private static void SetText(Transform root, string childName, string value)
    {
        var text = FindText(root, childName);
        if (text != null)
        {
            text.text = value;
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

    private static TMP_Text FindText(Transform root, string childName)
    {
        var target = FindChild(root, childName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text FindFirstText(Transform root, params string[] childNames)
    {
        foreach (var childName in childNames)
        {
            var text = FindText(root, childName);
            if (text != null)
            {
                return text;
            }
        }

        return null;
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
            if (child.name == "OutgoingMessage")
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static void SetActive(Transform root, string childName, bool active)
    {
        var target = FindChild(root, childName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static bool IsUserMessagePrefab(Transform root)
    {
        return FindChild(root, "ProfileArea") != null &&
               FindChild(root, "MessageContainer") != null &&
               FindChild(root, "Text_Time") != null;
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

    private void ApplyOutgoingProfile(Transform root)
    {
        var profileRoot = FindChild(root, "ProfileImage");
        if (profileRoot == null)
        {
            profileRoot = FindChild(root, "Profile");
        }

        if (profileRoot == null)
        {
            return;
        }

        foreach (Transform child in profileRoot)
        {
            child.gameObject.SetActive(false);
        }

        if (outgoingProfilePrefab == null)
        {
            return;
        }

        var instance = Instantiate(outgoingProfilePrefab, profileRoot, false);
        instance.name = "OutgoingProfile";

        var rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(60f, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(0.35f, 0.35f, 1f);
        }
    }
}
