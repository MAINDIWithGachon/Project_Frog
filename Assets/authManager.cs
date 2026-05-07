using UnityEngine;

public class authManager : MonoBehaviour
{
    public static bool isLoggedIn = false;

    void Start()
    {
        FakeLogin();
    }

    void FakeLogin()
    {
        isLoggedIn = true;

        Debug.Log("임시 로그인 완료");
        // NoticeManager 실행
        noticeManager.Instance.LoadNotices();
    }
}