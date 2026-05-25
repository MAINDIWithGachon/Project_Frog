using UnityEngine;
using BackEnd;

public class BackendLoginTest : MonoBehaviour
{
    private void Start()
    {
        //--------------------------------------------------
        // 뒤끝 초기화
        //--------------------------------------------------

        var bro = Backend.Initialize();

        if (bro.IsSuccess())
        {
            Debug.Log("뒤끝 초기화 성공");

            GuestLogin();
        }
        else
        {
            Debug.LogError($"초기화 실패 : {bro}");
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