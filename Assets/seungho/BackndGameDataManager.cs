// Unity 기본 기능을 사용하기 위한 namespace입니다.
using UnityEngine;

// 뒤끝 SDK 기능을 사용하기 위한 namespace입니다.
using BackEnd;

// 뒤끝 응답 JSON을 읽기 위한 namespace입니다.
using LitJson;

// 저장 데이터 클래스들을 사용하기 위한 namespace입니다.
using Project.Gameplay.SaveData;

// 뒤끝 게임 데이터 저장과 불러오기를 담당하는 클래스입니다.
public class BackndGameDataManager : MonoBehaviour
{
    // 싱글톤 인스턴스입니다.
    // 다른 스크립트에서 BackndGameDataManager.Instance로 접근할 수 있습니다.
    public static BackndGameDataManager Instance { get; private set; }

    // USER_DATA 테이블 이름입니다.
    private const string USER_DATA_TABLE = "USER_DATA";

    // USER_STAT_UPGRADE 테이블 이름입니다.
    private const string USER_STAT_UPGRADE_TABLE = "USER_STAT_UPGRADE";

    // 공격력 강화 스탯 ID입니다.
    public const string STAT_ATTACK = "attack";

    // 체력 강화 스탯 ID입니다.
    public const string STAT_HP = "hp";

    // 초당 체력 회복 강화 스탯 ID입니다.
    public const string STAT_HP_REGEN = "hp_regen";

    // 치명타 확률 강화 스탯 ID입니다.
    public const string STAT_CRIT_RATE = "crit_rate";

    // 치명타 공격력 강화 스탯 ID입니다.
    public const string STAT_CRIT_DAMAGE = "crit_damage";

    // 현재 로그인한 유저의 기본 게임 데이터입니다.
    public UserData CurrentUserData { get; private set; }

    // 현재 로그인한 유저의 강화 스탯 데이터입니다.
    public UserStatUpgradeSaveData CurrentStatUpgradeData { get; private set; }

    // Awake는 Start보다 먼저 실행됩니다.
    private void Awake()
    {
        // 이미 인스턴스가 존재하고, 그 인스턴스가 자기 자신이 아니라면
        if (Instance != null && Instance != this)
        {
            // 중복 오브젝트를 삭제합니다.
            Destroy(gameObject);

            // 함수 실행을 종료합니다.
            return;
        }

        // 현재 오브젝트를 싱글톤 인스턴스로 등록합니다.
        Instance = this;
    }

    // 로그인 후 호출할 함수입니다.
    // 유저 기본 데이터와 강화 데이터를 모두 불러오거나 생성합니다.
    public bool LoadOrCreateAllData()
    {
        // USER_DATA를 불러오거나 생성합니다.
        bool userDataSuccess = LoadOrCreateUserData();

        // USER_DATA 처리에 실패하면 false를 반환합니다.
        if (!userDataSuccess)
        {
            return false;
        }

        // USER_STAT_UPGRADE를 불러오거나 생성합니다.
        bool statDataSuccess = LoadOrCreateStatUpgradeData();

        // USER_STAT_UPGRADE 처리 결과를 반환합니다.
        return statDataSuccess;
    }

    // USER_DATA 테이블에서 현재 유저의 데이터를 불러오거나 없으면 생성합니다.
    private bool LoadOrCreateUserData()
    {
        // 현재 로그인한 유저의 USER_DATA를 조회합니다.
        BackendReturnObject bro = Backend.GameData.GetMyData(USER_DATA_TABLE, new Where());

        // 서버 요청이 실패했다면
        if (!bro.IsSuccess())
        {
            // 에러 로그를 출력합니다.
            Debug.LogError("USER_DATA 불러오기 실패 : " + bro);

            // 실패 반환
            return false;
        }

        // 응답 JSON에서 rows 배열을 가져옵니다.
        JsonData rows = bro.GetReturnValuetoJSON()["rows"];

        // rows 개수가 0이면 아직 데이터가 없다는 뜻입니다.
        if (rows.Count <= 0)
        {
            // 기본 USER_DATA를 생성합니다.
            return CreateDefaultUserData();
        }

        // 첫 번째 데이터를 가져옵니다.
        JsonData row = rows[0];

        // 새 UserData 객체를 생성합니다.
        CurrentUserData = new UserData();

        // 뒤끝 데이터 행 ID를 저장합니다.
        CurrentUserData.inDate = GetStringFromBackendField(row, "inDate");

        // Gold 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.Gold = GetLongFromBackendField(row, "Gold");

        // Gems 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.Gems = GetLongFromBackendField(row, "Gems");

        // TotalGoldEarned 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.TotalGoldEarned = GetLongFromBackendField(row, "TotalGoldEarned");

        // UsedProfileImageID 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.UsedProfileImageID = GetStringFromBackendField(row, "UsedProfileImageID");

        // SelectedCharacterID 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.SelectedCharacterID = GetStringFromBackendField(row, "SelectedCharacterID");

        // EquippedItemJson 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.EquippedItemJson = GetStringFromBackendField(row, "EquippedItemJson");

        // EquippedSkillJson 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.EquippedSkillJson = GetStringFromBackendField(row, "EquippedSkillJson");

        // LastPlayedStageID 값을 서버 데이터에서 읽어 저장합니다.
        CurrentUserData.LastPlayedStageID = GetStringFromBackendField(row, "LastPlayedStageID");

        // 성공 로그를 출력합니다.
        Debug.Log("USER_DATA 불러오기 성공");

        // 성공 반환
        return true;
    }

    // USER_DATA가 없을 때 기본 데이터를 생성합니다.
    private bool CreateDefaultUserData()
    {
        // 기본 UserData 객체를 생성합니다.
        CurrentUserData = new UserData();

        // 뒤끝에 보낼 데이터 묶음 객체를 생성합니다.
        Param param = new Param();

        // 기본 Gold 값을 추가합니다.
        param.Add("Gold", CurrentUserData.Gold);

        // 기본 Gems 값을 추가합니다.
        param.Add("Gems", CurrentUserData.Gems);

        // 기본 TotalGoldEarned 값을 추가합니다.
        param.Add("TotalGoldEarned", CurrentUserData.TotalGoldEarned);

        // 기본 프로필 이미지 ID를 추가합니다.
        param.Add("UsedProfileImageID", CurrentUserData.UsedProfileImageID);

        // 기본 선택 캐릭터 ID를 추가합니다.
        param.Add("SelectedCharacterID", CurrentUserData.SelectedCharacterID);

        // 기본 장착 장비 JSON을 추가합니다.
        param.Add("EquippedItemJson", CurrentUserData.EquippedItemJson);

        // 기본 장착 스킬 JSON을 추가합니다.
        param.Add("EquippedSkillJson", CurrentUserData.EquippedSkillJson);

        // 기본 마지막 스테이지 ID를 추가합니다.
        param.Add("LastPlayedStageID", CurrentUserData.LastPlayedStageID);

        // USER_DATA 테이블에 새 데이터를 삽입합니다.
        BackendReturnObject bro = Backend.GameData.Insert(USER_DATA_TABLE, param);

        // 삽입 요청이 실패했다면
        if (!bro.IsSuccess())
        {
            // 에러 로그를 출력합니다.
            Debug.LogError("기본 USER_DATA 생성 실패 : " + bro);

            // 실패 반환
            return false;
        }

        // 생성된 데이터의 inDate를 저장합니다.
        CurrentUserData.inDate = bro.GetInDate();

        // 성공 로그를 출력합니다.
        Debug.Log("기본 USER_DATA 생성 성공");

        // 성공 반환
        return true;
    }

    // USER_STAT_UPGRADE 테이블에서 강화 데이터를 불러오거나 없으면 생성합니다.
    private bool LoadOrCreateStatUpgradeData()
    {
        // 현재 로그인한 유저의 강화 데이터를 조회합니다.
        BackendReturnObject bro = Backend.GameData.GetMyData(USER_STAT_UPGRADE_TABLE, new Where());

        // 요청이 실패했다면
        if (!bro.IsSuccess())
        {
            // 에러 로그 출력
            Debug.LogError("USER_STAT_UPGRADE 불러오기 실패 : " + bro);

            // 실패 반환
            return false;
        }

        // rows 배열을 가져옵니다.
        JsonData rows = bro.GetReturnValuetoJSON()["rows"];

        // 데이터가 없다면
        if (rows.Count <= 0)
        {
            // 기본 강화 데이터를 생성합니다.
            return CreateDefaultStatUpgradeData();
        }

        // 첫 번째 행을 가져옵니다.
        JsonData row = rows[0];

        // 강화 데이터 행의 inDate를 가져옵니다.
        string inDate = GetStringFromBackendField(row, "inDate");

        // 저장되어 있는 JSON 문자열을 가져옵니다.
        string json = GetStringFromBackendField(row, "StatUpgradeJson");

        // JSON 문자열이 비어 있다면
        if (string.IsNullOrEmpty(json))
        {
            // 기본 강화 데이터를 생성합니다.
            CurrentStatUpgradeData = CreateDefaultStatUpgradeObject();
        }
        else
        {
            // JSON 문자열을 강화 데이터 객체로 변환합니다.
            CurrentStatUpgradeData = JsonUtility.FromJson<UserStatUpgradeSaveData>(json);
        }

        // 현재 강화 데이터의 서버 행 ID를 따로 저장하기 위해 UserData 쪽과 별도로 보관합니다.
        PlayerPrefs.SetString("STAT_UPGRADE_INDATE", inDate);

        // 성공 로그를 출력합니다.
        Debug.Log("USER_STAT_UPGRADE 불러오기 성공");

        // 성공 반환
        return true;
    }

    // USER_STAT_UPGRADE가 없을 때 기본 강화 데이터를 생성합니다.
    private bool CreateDefaultStatUpgradeData()
    {
        // 기본 강화 데이터 객체를 생성합니다.
        CurrentStatUpgradeData = CreateDefaultStatUpgradeObject();

        // 강화 데이터 객체를 JSON 문자열로 변환합니다.
        string json = JsonUtility.ToJson(CurrentStatUpgradeData);

        // 뒤끝에 보낼 Param 객체를 생성합니다.
        Param param = new Param();

        // StatUpgradeJson 컬럼에 JSON 문자열을 넣습니다.
        param.Add("StatUpgradeJson", json);

        // USER_STAT_UPGRADE 테이블에 삽입합니다.
        BackendReturnObject bro = Backend.GameData.Insert(USER_STAT_UPGRADE_TABLE, param);

        // 삽입 요청이 실패했다면
        if (!bro.IsSuccess())
        {
            // 에러 로그를 출력합니다.
            Debug.LogError("기본 USER_STAT_UPGRADE 생성 실패 : " + bro);

            // 실패 반환
            return false;
        }

        // 생성된 강화 데이터 행의 inDate를 저장합니다.
        PlayerPrefs.SetString("STAT_UPGRADE_INDATE", bro.GetInDate());

        // 성공 로그를 출력합니다.
        Debug.Log("기본 USER_STAT_UPGRADE 생성 성공");

        // 성공 반환
        return true;
    }

    // 기본 강화 데이터 객체를 생성하는 함수입니다.
    private UserStatUpgradeSaveData CreateDefaultStatUpgradeObject()
    {
        // 새 강화 데이터 객체를 생성합니다.
        UserStatUpgradeSaveData data = new UserStatUpgradeSaveData();

        // 공격력 강화 레벨 0 추가
        data.SetLevel(STAT_ATTACK, 0);

        // 체력 강화 레벨 0 추가
        data.SetLevel(STAT_HP, 0);

        // 초당 체력 회복 강화 레벨 0 추가
        data.SetLevel(STAT_HP_REGEN, 0);

        // 치명타 확률 강화 레벨 0 추가
        data.SetLevel(STAT_CRIT_RATE, 0);

        // 치명타 공격력 강화 레벨 0 추가
        data.SetLevel(STAT_CRIT_DAMAGE, 0);

        // 생성한 기본 데이터를 반환합니다.
        return data;
    }

    // 특정 스탯을 1레벨 강화하는 함수입니다.
    public bool UpgradeStat(string statId)
    {
        // 강화 데이터가 아직 없다면
        if (CurrentStatUpgradeData == null)
        {
            // 에러 로그 출력
            Debug.LogError("강화 데이터가 아직 로드되지 않았습니다.");

            // 실패 반환
            return false;
        }

        // 현재 스탯 레벨을 가져옵니다.
        int currentLevel = CurrentStatUpgradeData.GetLevel(statId);

        // 현재 레벨에 1을 더한 값을 저장합니다.
        CurrentStatUpgradeData.SetLevel(statId, currentLevel + 1);

        // 변경된 강화 데이터를 서버에 저장합니다.
        bool saveSuccess = SaveStatUpgradeData();

        // 저장 성공 여부 반환
        return saveSuccess;
    }

    // 강화 데이터를 서버에 저장하는 함수입니다.
    public bool SaveStatUpgradeData()
    {
        // 강화 데이터가 없다면
        if (CurrentStatUpgradeData == null)
        {
            // 에러 로그 출력
            Debug.LogError("저장할 강화 데이터가 없습니다.");

            // 실패 반환
            return false;
        }

        // 저장해둔 강화 데이터 행 ID를 가져옵니다.
        string statInDate = PlayerPrefs.GetString("STAT_UPGRADE_INDATE", "");

        // inDate가 비어있다면
        if (string.IsNullOrEmpty(statInDate))
        {
            // 에러 로그 출력
            Debug.LogError("USER_STAT_UPGRADE inDate가 없습니다.");

            // 실패 반환
            return false;
        }

        // 강화 데이터 객체를 JSON 문자열로 변환합니다.
        string json = JsonUtility.ToJson(CurrentStatUpgradeData);

        // 뒤끝에 보낼 Param 객체를 생성합니다.
        Param param = new Param();

        // StatUpgradeJson 컬럼에 JSON 문자열을 넣습니다.
        param.Add("StatUpgradeJson", json);

        // USER_STAT_UPGRADE 데이터를 수정합니다.
        BackendReturnObject bro = Backend.GameData.UpdateV2(USER_STAT_UPGRADE_TABLE, statInDate, Backend.UserInDate, param);

        // 수정 요청이 실패했다면
        if (!bro.IsSuccess())
        {
            // 에러 로그 출력
            Debug.LogError("USER_STAT_UPGRADE 저장 실패 : " + bro);

            // 실패 반환
            return false;
        }

        // 성공 로그 출력
        Debug.Log("USER_STAT_UPGRADE 저장 성공");

        // 성공 반환
        return true;
    }

    // 서버 응답 row에서 문자열 값을 안전하게 가져오는 함수입니다.
    private string GetStringFromBackendField(JsonData row, string key)
    {
        // row가 null이면 빈 문자열 반환
        if (row == null)
            return "";

        // row에 key가 없으면 빈 문자열 반환
        if (!row.ContainsKey(key))
            return "";

        // 해당 key의 값이 null이면 빈 문자열 반환
        if (row[key] == null)
            return "";

        // 뒤끝 데이터가 {"S":"값"} 형태라면
        if (row[key].IsObject && row[key].ContainsKey("S"))
            return row[key]["S"].ToString();

        // 뒤끝 데이터가 그냥 문자열이면
        return row[key].ToString();
    }

    // 서버 응답 row에서 long 숫자 값을 안전하게 가져오는 함수입니다.
    private long GetLongFromBackendField(JsonData row, string key)
    {
        // 문자열로 값을 가져옵니다.
        string value = GetStringFromBackendField(row, key);

        // 문자열이 비어있으면 0 반환
        if (string.IsNullOrEmpty(value))
            return 0;

        // long으로 변환을 시도합니다.
        long.TryParse(value, out long result);

        // 변환 결과를 반환합니다.
        return result;
    }
}