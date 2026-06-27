// Unity 기본 기능 사용
using UnityEngine;

// Button 같은 Unity UI 사용
using UnityEngine.UI;

// TextMeshPro 텍스트와 입력창 사용
using TMPro;

// GuestLogin 오브젝트에 붙일 화면 제어 스크립트
public class GuestLoginController : MonoBehaviour
{
    [Header("Guest Login UI")]

    // 게스트 로그인 버튼
    [SerializeField] private Button guestLoginButton;

    [Header("Nickname UI")]

    // 닉네임 입력 패널
    [SerializeField] private GameObject nicknamePanel;

    // 닉네임 입력칸
    [SerializeField] private TMP_InputField nicknameInputField;

    // 닉네임 확인 버튼
    [SerializeField] private Button nicknameConfirmButton;

    [Header("Profile UI")]

    // 상단 TestUser 텍스트 연결
    [SerializeField] private TextMeshProUGUI userNameText;

    [Header("Optional Status Text")]

    // 상태 출력 텍스트
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Editor Test Option")]

    // true면 에디터에서도 실제 뒤끝 게스트 로그인 허용
    // false면 에디터에서는 서버 로그인 없이 UI 테스트만 진행
    [SerializeField] private bool allowEditorBackendLogin = false;

    // 중복 로그인 클릭 방지용 변수
    private bool isLoggingIn = false;

    // Unity에서 시작 시 자동 실행
    private void Start()
    {
        // 필수 UI 연결 확인
        if (!ValidateReferences())
            return;

        // 게스트 로그인 버튼 클릭 이벤트 등록
        guestLoginButton.onClick.AddListener(OnClickGuestLogin);

        // 닉네임 확인 버튼 클릭 이벤트 등록
        nicknameConfirmButton.onClick.AddListener(OnClickNicknameConfirm);

        // 시작 시 닉네임 패널 숨기기
        nicknamePanel.SetActive(false);
    }

    // 게스트 로그인 버튼 클릭 시 실행
    private void OnClickGuestLogin()
    {
        // 이미 로그인 중이면 중복 요청 방지
        if (isLoggingIn)
        {
            SetStatus("이미 로그인 처리 중입니다.");
            return;
        }

        // 에디터에서 실제 로그인을 허용하지 않는 경우
#if UNITY_EDITOR
        if (!allowEditorBackendLogin)
        {
            SetStatus("에디터 테스트 모드: 실제 로그인 없이 닉네임 UI만 테스트합니다.");

            // 서버 로그인 없이 닉네임 입력창만 열기
            nicknamePanel.SetActive(true);

            return;
        }
#endif

        // 뒤끝 초기화 여부 확인
        if (!BackendManager.IsInitialized)
        {
            SetStatus("뒤끝 초기화가 아직 완료되지 않았습니다.");
            return;
        }

        // BackndUserInfo 오브젝트 존재 확인
        if (SHBackndUserInfo.Instance == null)
        {
            SetStatus("SHBackndUserInfo 오브젝트가 없습니다.");
            return;
        }

        // 로그인 진행 상태로 변경
        isLoggingIn = true;

        // 중복 클릭 방지를 위해 버튼 비활성화
        guestLoginButton.interactable = false;

        // 상태 출력
        SetStatus("게스트 로그인 중...");

        // 게스트 로그인 실행
        bool loginSuccess = BackndLogin.GuestLogin();

        // 로그인 진행 상태 해제
        isLoggingIn = false;

        // 버튼 다시 활성화
        guestLoginButton.interactable = true;

        // 로그인 실패 처리
        if (!loginSuccess)
        {
            SetStatus("게스트 로그인 실패\n" + BackndLogin.LastErrorMessage);
            return;
        }

        // 로그인 성공 후 유저 정보 불러오기
        bool loadSuccess = SHBackndUserInfo.Instance.LoadData();

        // 유저 정보 로드 실패 처리
        if (!loadSuccess)
        {
            SetStatus("유저 정보 불러오기 실패");
            return;
        }
        // 로그인 성공 후 버튼 숨기기
        guestLoginButton.gameObject.SetActive(false);

        // 유저 정보 로드 후 처리
        AfterUserInfoLoaded();
    }

    // 유저 정보 로드 후 실행
    private void AfterUserInfoLoaded()
    {
        // 현재 닉네임 가져오기
        string nickname = SHBackndUserInfo.Instance.Nickname;

        // 닉네임이 없으면
        if (string.IsNullOrEmpty(nickname))
        {
            SetStatus("닉네임을 설정해주세요.");

            // 닉네임 입력창 열기
            nicknamePanel.SetActive(true);

            return;
        }

        // 닉네임이 있으면 상단 이름 텍스트 변경
        userNameText.text = nickname;

        // 닉네임 입력창 닫기
        nicknamePanel.SetActive(false);

        // 상태 출력
        SetStatus("게스트 로그인 완료");

        // 새 RuntimeData 4테이블(PlayerCurrency, PlayerSkillLevels, PlayerStatLevels, PlayerGrowth)을 불러오거나 생성합니다.
        if (!BackndRuntimeDataTestActions.LoadAndApplyRuntimeDataJson(out string runtimeDataMessage))
        {
            SetStatus("게임 데이터 불러오기 실패\n" + runtimeDataMessage);
            return;
        }

        Debug.Log("[GuestLoginController] RuntimeData load result: " + runtimeDataMessage);

        // 로그인 완료 후 상태 텍스트 숨기기
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        // TODO: 나중에 여기에 데이터 로드 연결
        // 캐릭터 데이터 불러오기
        // 장비 데이터 불러오기
        // 재화 데이터 불러오기
        // 인벤토리 데이터 불러오기
        // 게임 오브젝트 초기화
    }

    // 닉네임 확인 버튼 클릭 시 실행
    private void OnClickNicknameConfirm()
    {
        // 입력창에서 닉네임 가져오고 앞뒤 공백 제거
        string nickname = nicknameInputField.text.Trim();

        // 빈 닉네임 검사
        if (string.IsNullOrEmpty(nickname))
        {
            SetStatus("닉네임을 입력해주세요.");
            return;
        }

        // 에디터 테스트 모드에서는 서버 저장 없이 UI만 변경
#if UNITY_EDITOR
        if (!allowEditorBackendLogin)
        {
            userNameText.text = nickname;

            nicknamePanel.SetActive(false);

            SetStatus("에디터 테스트: 닉네임 UI 적용 완료");

            return;
        }
#endif

        // BackndUserInfo 존재 확인
        if (SHBackndUserInfo.Instance == null)
        {
            SetStatus("SHBackndUserInfo 오브젝트가 없습니다.");
            return;
        }

        // 실제 서버에 닉네임 저장
        bool success = SHBackndUserInfo.Instance.SetNickname(nickname);

        // 닉네임 설정 실패 처리
        if (!success)
        {
            SetStatus("닉네임 설정 실패");
            return;
        }

        // 성공 시 상단 이름 변경
        userNameText.text = nickname;

        // 닉네임 입력창 닫기
        nicknamePanel.SetActive(false);

        // 상태 출력
        SetStatus("닉네임 설정 완료");
    }

    // Inspector 연결 누락 체크
    private bool ValidateReferences()
    {
        if (guestLoginButton == null)
        {
            Debug.LogError("GuestLoginButton이 연결되지 않았습니다.");
            return false;
        }

        if (nicknamePanel == null)
        {
            Debug.LogError("NicknamePanel이 연결되지 않았습니다.");
            return false;
        }

        if (nicknameInputField == null)
        {
            Debug.LogError("NicknameInputField가 연결되지 않았습니다.");
            return false;
        }

        if (nicknameConfirmButton == null)
        {
            Debug.LogError("NicknameConfirmButton이 연결되지 않았습니다.");
            return false;
        }

        if (userNameText == null)
        {
            Debug.LogError("UserNameText가 연결되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 상태 메시지 출력 함수
    private void SetStatus(string message)
    {
        // 콘솔 로그 출력
        Debug.Log(message);

        // StatusText가 연결되어 있다면
        if (statusText != null)
        {
            // 텍스트 출력
            statusText.text = message;

            // 상태창 활성화
            statusText.gameObject.SetActive(true);
        }   
    }
}
