using UnityEngine;

public class ChatPopupClose : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }
}