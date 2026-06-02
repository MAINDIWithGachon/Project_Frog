using UnityEngine;
using BackEnd;

public class BackendLoginTest : MonoBehaviour
{
    private void Start()
    {
        //--------------------------------------------------
        // 뒤끝 초기화 상태 확인
        //--------------------------------------------------

        if (BackendManager.EnsureInitialized())
        {
            GuestLogin();
        }
        else
        {
            Debug.LogError("초기화 실패로 게스트 로그인을 중단합니다.");
        }
    }

    //--------------------------------------------------
    // 게스트 로그인 테스트
    //--------------------------------------------------

    private void GuestLogin()
    {
        Backend.BMember.GuestLogin(callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("게스트 로그인 성공");
            }
            else
            {
                Debug.LogError($"게스트 로그인 실패 : {callback}");
            }
        });
    }
}
