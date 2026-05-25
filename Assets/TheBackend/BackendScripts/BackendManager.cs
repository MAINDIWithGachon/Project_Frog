// Unity 기본 기능 사용
using UnityEngine;

// 뒤끝 SDK 사용
using BackEnd;

// 뒤끝 초기화를 담당하는 클래스
public class BackendManager : MonoBehaviour
{
    // 다른 스크립트에서 뒤끝 초기화 여부를 확인할 수 있게 만든 변수
    // static이라 BackendManager.IsInitialized로 바로 접근 가능
    public static bool IsInitialized { get; private set; }

    // Unity에서 씬 시작 후 자동 실행
    private void Start()
    {
        // 뒤끝 SDK 초기화 요청
        BackendReturnObject bro = Backend.Initialize();

        // 초기화 성공 여부 확인
        if (bro.IsSuccess())
        {
            // 초기화 성공 상태 저장
            IsInitialized = true;

            // 콘솔에 성공 로그 출력
            Debug.Log("뒤끝 초기화 성공 : " + bro);
        }
        else
        {
            // 초기화 실패 상태 저장
            IsInitialized = false;

            // 콘솔에 실패 로그 출력
            Debug.LogError("뒤끝 초기화 실패 : " + bro);
        }
    }
}