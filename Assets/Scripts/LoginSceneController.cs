using UnityEngine;
using UnityEngine.UI;
using BackEnd;

public class LoginSceneController : MonoBehaviour
{
    [SerializeField] private Button guestLoginButton;

    private void Start()
    {
        guestLoginButton.onClick.AddListener(OnClickGuestLogin);
    }

    private void OnClickGuestLogin()
    {
        LoadingUIController.Instance.Loading("게스트 로그인 중...");

        BackendReturnObject bro = Backend.BMember.GuestLogin();

        LoadingUIController.Instance.FinishLoading();

        if (bro.IsSuccess())
        {
            NoticeUIController.Instance.ShowNotice(
                "게스트 로그인 성공!",
                null
            );

            Debug.Log("게스트 로그인 성공");
        }
        else
        {
            NoticeUIController.Instance.ShowNotice(
                bro.GetMessage(),
                null
            );

            Debug.LogError(bro.GetMessage());
        }
    }
}