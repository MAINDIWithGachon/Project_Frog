// Unity 로그 출력 사용
using UnityEngine;

// 뒤끝 로그인 API 사용
using BackEnd;

// 로그인 기능 전용 클래스
// 오브젝트에 붙이지 않고 BackndLogin.GuestLogin()처럼 사용
public static class BackndLogin
{
    public static string LastErrorMessage { get; private set; }
    // 게스트 로그인 실행 함수
    // 성공하면 true, 실패하면 false 반환
    public static bool GuestLogin()
    {
        // 뒤끝 서버에 게스트 로그인 요청
        BackendReturnObject bro = Backend.BMember.GuestLogin();

        // 요청 성공 여부 확인
        if (bro.IsSuccess())
        {
            //에러 메시지 초기화
            LastErrorMessage = "";
            // 성공 로그 출력
            Debug.Log("게스트 로그인 성공 : " + bro);

            // 성공 반환
            return true;
        }

        LastErrorMessage =
            "StatusCode: " + bro.GetStatusCode() + "\n" +
            "ErrorCode: " + bro.GetErrorCode() + "\n" +
            "Message: " + bro.GetMessage();
        // 실패 로그 출력
        Debug.LogError("게스트 로그인 실패 : " + LastErrorMessage);

        // 실패 반환
        return false;
    }
}