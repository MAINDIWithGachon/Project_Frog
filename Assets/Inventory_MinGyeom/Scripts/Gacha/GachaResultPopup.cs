using UnityEngine;
using UnityEngine.UI;

public class GachaResultPopup : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Buttons")]
    [SerializeField] private Button dimmedButton;
    [SerializeField] private Button redrawOneButton;
    [SerializeField] private Button redrawTenButton;
    [SerializeField] private Button closeButton;

    [Header("Dependencies")]
    [SerializeField] private GachaManager gachaManager;

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
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
            if (gachaManager != null)
            {
                redrawOneButton.onClick.AddListener(RedrawOne);
            }
        }

        if (redrawTenButton != null)
        {
            redrawTenButton.onClick.RemoveListener(RedrawTen);
            if (gachaManager != null)
            {
                redrawTenButton.onClick.AddListener(RedrawTen);
            }
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
}
