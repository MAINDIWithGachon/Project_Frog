// Unity 기본 기능 사용
using UnityEngine;

// 뒤끝 API 사용
using BackEnd;

// 뒤끝 응답 JSON 데이터 처리용
using LitJson;

// 로그인한 유저 정보를 관리하는 클래스
public class SHBackndUserInfo : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SHBackndUserInfo Instance { get; private set; }

    // 현재 유저 닉네임
    public string Nickname { get; private set; }

    // 뒤끝 유저 고유값
    public string InDate { get; private set; }

    // 구글/애플 연동 계정 ID
    public string FederationId { get; private set; }

    // FederationId가 비어있으면 게스트 계정으로 판단
    public bool IsGuestAccount => string.IsNullOrEmpty(FederationId);

    // Start보다 먼저 실행됨
    private void Awake()
    {
        // 이미 인스턴스가 있으면 중복 방지
        if (Instance != null && Instance != this)
        {
            // 중복 오브젝트 삭제
            Destroy(gameObject);

            // 함수 종료
            return;
        }

        // 현재 오브젝트를 인스턴스로 등록
        Instance = this;
    }

    // 뒤끝 서버에서 현재 로그인한 유저 정보 불러오기
    public bool LoadData()
    {
        // 유저 정보 요청
        BackendReturnObject bro = Backend.BMember.GetUserInfo();

        // 요청 실패 처리
        if (!bro.IsSuccess())
        {
            Debug.LogError("유저 정보 불러오기 실패 : " + bro);
            return false;
        }

        // 응답 JSON에서 row 데이터 꺼내기
        JsonData row = bro.GetReturnValuetoJSON()["row"];

        // nickname 값 저장
        Nickname = GetString(row, "nickname");

        // inDate 값 저장
        InDate = GetString(row, "inDate");

        // federationId 값 저장
        FederationId = GetString(row, "federationId");

        // 확인용 로그
        Debug.Log($"유저 정보 로드 성공 / 닉네임: {Nickname}, inDate: {InDate}, 게스트 여부: {IsGuestAccount}");

        // 성공 반환
        return true;
    }

    // 닉네임 설정 함수
    public bool SetNickname(string nickname)
    {
        // 앞뒤 공백 제거
        nickname = nickname.Trim();

        // 닉네임 유효성 검사
        if (!CheckNicknameValidation(nickname))
            return false;

        // 뒤끝 서버에 닉네임 변경 요청
        BackendReturnObject bro = Backend.BMember.UpdateNickname(nickname);

        // 요청 실패 처리
        if (!bro.IsSuccess())
        {
            Debug.LogError("닉네임 설정 실패 : " + bro);
            return false;
        }

        // 서버 저장 성공 후 로컬 값도 변경
        Nickname = nickname;

        // 성공 로그
        Debug.Log("닉네임 설정 성공 : " + nickname);

        // 성공 반환
        return true;
    }

    // 닉네임 검사 함수
    private bool CheckNicknameValidation(string nickname)
    {
        // 비어있는 닉네임 검사
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("닉네임을 입력해야 합니다.");
            return false;
        }

        // 길이 검사
        if (nickname.Length > 20)
        {
            Debug.LogWarning("닉네임은 20자 이하로 입력해야 합니다.");
            return false;
        }

        // 공백 포함 검사
        if (nickname.Contains(" "))
        {
            Debug.LogWarning("닉네임에는 공백을 사용할 수 없습니다.");
            return false;
        }

        // 뒤끝 서버에 닉네임 중복 검사 요청
        BackendReturnObject bro = Backend.BMember.CheckNicknameDuplication(nickname);

        // 중복이거나 요청 실패일 경우
        if (!bro.IsSuccess())
        {
            Debug.LogWarning("이미 사용 중인 닉네임입니다 : " + bro);
            return false;
        }

        // 모든 검사 통과
        return true;
    }

    // JSON 데이터에서 문자열 안전하게 꺼내는 함수
    private string GetString(JsonData data, string key)
    {
        // 데이터 자체가 없으면 빈 문자열 반환
        if (data == null)
            return "";

        // key가 없으면 빈 문자열 반환
        if (!data.ContainsKey(key))
            return "";

        // 값이 null이면 빈 문자열 반환
        if (data[key] == null)
            return "";

        // 문자열로 변환해서 반환
        return data[key].ToString();
    }
}