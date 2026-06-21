using UnityEngine;
using UnityEngine.UI;

public class RecipeGachaResultPopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Buttons")]
    [SerializeField] private Button dimmedButton;
    [SerializeField] private Button redrawOneButton;
    [SerializeField] private Button redrawTenButton;
    [SerializeField] private Button closeButton;

    [Header("Dependencies")]
    [SerializeField] private RecipeGachaManager gachaManager;

    [Header("Close Behavior")]
    [SerializeField] private bool closeOnPointerRelease = true;

    private void Awake()
    {
        RegisterButtonListeners();
    }

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    private void Update()
    {
        if (!closeOnPointerRelease || !IsPopupActive())
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            CloseFromScreenPoint(Input.mousePosition);
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended)
            {
                CloseFromScreenPoint(touch.position);
                return;
            }
        }
    }

    public void Open()
    {
        SetPopupActive(true);
    }

    public void Close()
    {
        SetPopupActive(false);
    }

    public void RedrawOne()
    {
        Redraw(1);
    }

    public void RedrawTen()
    {
        Redraw(10);
    }

    private void Redraw(int drawCount)
    {
        Close();

        if (gachaManager == null)
        {
            Debug.LogWarning("[RecipeGachaResultPopup] RecipeGachaManager is not assigned.", this);
            return;
        }

        gachaManager.Draw(drawCount);
    }

    private void RegisterButtonListeners()
    {
        if (dimmedButton != null)
        {
            dimmedButton.onClick.RemoveListener(Close);
            dimmedButton.onClick.AddListener(Close);
        }

        if (redrawOneButton != null)
        {
            redrawOneButton.onClick.RemoveListener(RedrawOne);
            redrawOneButton.onClick.AddListener(RedrawOne);
        }

        if (redrawTenButton != null)
        {
            redrawTenButton.onClick.RemoveListener(RedrawTen);
            redrawTenButton.onClick.AddListener(RedrawTen);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (dimmedButton != null)
        {
            dimmedButton.onClick.RemoveListener(Close);
        }

        if (redrawOneButton != null)
        {
            redrawOneButton.onClick.RemoveListener(RedrawOne);
        }

        if (redrawTenButton != null)
        {
            redrawTenButton.onClick.RemoveListener(RedrawTen);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    private void SetPopupActive(bool isActive)
    {
        GameObject target = popupRoot != null ? popupRoot : gameObject;
        target.SetActive(isActive);
    }

    private void CloseFromScreenPoint(Vector2 screenPoint)
    {
        if (IsPointOverButton(redrawOneButton, screenPoint) || IsPointOverButton(redrawTenButton, screenPoint))
        {
            return;
        }

        Close();
    }

    private bool IsPopupActive()
    {
        GameObject target = popupRoot != null ? popupRoot : gameObject;
        return target != null && target.activeInHierarchy;
    }

    private bool IsPointOverButton(Button button, Vector2 screenPoint)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
        {
            return false;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return false;
        }

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
    }
}
