using System;
using System.Globalization;
using System.Text;
using BackEnd;
using LitJson;
using UnityEngine;

/// <summary>
/// RuntimeData JSON과 뒤끝 DB 테이블 사이의 변환만 담당하는 저장소 클래스입니다.
///
/// 중요한 원칙:
/// 1. 이 클래스는 인게임 RuntimeData 값을 직접 수정하지 않습니다.
/// 2. 저장할 때는 RuntimeData.Instance.GetRuntimeDataJson()으로 받은 JSON 문자열만 입력으로 받습니다.
/// 3. 로드할 때는 뒤끝 여러 테이블을 읽어서 클라이언트가 쓰는 RuntimeData JSON 형태로 다시 조립해 반환합니다.
/// 4. 클라이언트 쪽 게임 로직은 뒤끝 테이블 구조를 알 필요가 없습니다.
///
/// 즉, 이 클래스는 "JSON 한 덩어리"와 "뒤끝 테이블 여러 개" 사이의 통역 계층입니다.
/// </summary>
public class BackndRuntimeDataRepository : MonoBehaviour
{
    public static BackndRuntimeDataRepository Instance { get; private set; }

    // 테이블 구조가 바뀌거나 컬럼이 추가될 때 올리는 버전입니다.
    // 예: v2에서 Energy 재화가 추가되면 CurrentSchemaVersion을 2로 올리고,
    // 로드 시 구버전 row에 기본 Energy 값을 채워주는 마이그레이션 코드를 추가하면 됩니다.
    private const int CurrentSchemaVersion = 1;

    // 뒤끝 콘솔에 생성해야 하는 테이블 이름입니다.
    // 테이블명을 바꾸면 기존 서버 데이터와 연결이 끊길 수 있으니 신중하게 변경해야 합니다.
    private const string PlayerCurrencyTable = "PlayerCurrency";
    private const string PlayerSkillLevelsTable = "PlayerSkillLevels";
    private const string PlayerStatLevelsTable = "PlayerStatLevels";
    private const string PlayerGrowthTable = "PlayerGrowth";

    // skillLevels는 스킬별 row로 쪼개지 않고 배열 전체를 JSON 문자열 한 컬럼에 저장합니다.
    // 스킬 필드가 늘어나거나 순서가 바뀌어도 DB 컬럼 변경을 줄일 수 있어서 확장성이 좋습니다.
    private const string SkillLevelsJsonColumn = "skillLevelsJson";

    // 신규 유저이거나 PlayerSkillLevels row가 누락된 경우 사용할 기본값입니다.
    // 클라이언트 기본 스킬 배열이 확정되면 "[]" 대신 그 배열 JSON을 넣어도 됩니다.
    private static readonly string DefaultSkillLevelsJson = "[]";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// RuntimeData.Instance.GetRuntimeDataJson() 결과를 받아 뒤끝 테이블별 row로 나누어 저장합니다.
    ///
    /// 저장 흐름:
    /// 1. JSON 문자열 파싱
    /// 2. Currency / skillLevels / statLevels / growth 영역 추출
    /// 3. 저장 전 최소 검증
    /// 4. PlayerCurrency 저장
    /// 5. PlayerSkillLevels 저장(skillLevelsJson 컬럼)
    /// 6. PlayerStatLevels 저장
    /// 7. PlayerGrowth 저장
    ///
    /// 주의:
    /// 현재 뒤끝 GameData API 호출은 테이블별로 순차 실행됩니다.
    /// 하나라도 실패하면 실패 결과를 반환하므로 클라이언트는 dirty 상태를 유지하고 재시도하는 방식이 안전합니다.
    /// </summary>
    public RuntimeDataBackendResult SaveRuntimeDataJson(string runtimeDataJson)
    {
        if (!TryParseRuntimeData(runtimeDataJson, out RuntimeDataTableData data, out RuntimeDataBackendResult error))
            return error;

        long savedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        RuntimeDataBackendResult result = UpsertPlayerCurrency(data.currency, savedAt);
        if (!result.isSuccess) return result;

        result = UpsertPlayerSkillLevels(data.skillLevelsJson, savedAt);
        if (!result.isSuccess) return result;

        result = UpsertPlayerStatLevels(data.statLevels, savedAt);
        if (!result.isSuccess) return result;

        result = UpsertPlayerGrowth(data.growth, savedAt);
        if (!result.isSuccess) return result;

        return RuntimeDataBackendResult.Success("RuntimeData save succeeded.", runtimeDataJson);
    }

    /// <summary>
    /// 뒤끝 테이블들을 조회해서 클라이언트 RuntimeData JSON 계약 형태로 다시 조립합니다.
    ///
    /// 로드 흐름:
    /// 1. PlayerCurrency 조회, 없으면 기본 row 생성
    /// 2. PlayerSkillLevels 조회, 없으면 기본 row 생성
    /// 3. PlayerStatLevels 조회, 없으면 기본 row 생성
    /// 4. PlayerGrowth 조회, 없으면 기본 row 생성
    /// 5. 네 영역을 합쳐 RuntimeData JSON 문자열 생성
    ///
    /// 이 함수는 RuntimeData에 Import까지 하지 않습니다.
    /// 호출한 쪽에서 result.runtimeDataJson을 받아 RuntimeData 적용 함수에 넘기는 구조를 권장합니다.
    /// </summary>
    public RuntimeDataBackendResult LoadRuntimeDataJson()
    {
        RuntimeDataBackendResult result = LoadOrCreatePlayerCurrency(out CurrencySection currency);
        if (!result.isSuccess) return result;

        result = LoadOrCreatePlayerSkillLevels(out string skillLevelsJson);
        if (!result.isSuccess) return result;

        result = LoadOrCreatePlayerStatLevels(out StatLevelsSection statLevels);
        if (!result.isSuccess) return result;

        result = LoadOrCreatePlayerGrowth(out GrowthSection growth);
        if (!result.isSuccess) return result;

        string runtimeDataJson = BuildRuntimeDataJson(currency, skillLevelsJson, statLevels, growth);
        return RuntimeDataBackendResult.Success("RuntimeData load succeeded.", runtimeDataJson);
    }

    public void SaveCurrentRuntimeData_Button()
    {
        if (!TryGetCurrentRuntimeDataJson(out string runtimeDataJson))
        {
            Debug.LogError("[BackndRuntimeDataRepository] RuntimeData JSON provider was not found.");
            return;
        }

        RuntimeDataBackendResult result = SaveRuntimeDataJson(runtimeDataJson);
        Debug.Log(result.ToJson());
    }

    public void LoadRuntimeDataJson_Button()
    {
        RuntimeDataBackendResult result = LoadRuntimeDataJson();
        Debug.Log(result.ToJson());
    }

    private static bool TryGetCurrentRuntimeDataJson(out string runtimeDataJson)
    {
        runtimeDataJson = "";

        // 현재 브랜치와 develop 브랜치의 RuntimeData API 차이를 견디기 위한 디버그용 연결 코드입니다.
        // develop에는 GetRuntimeDataJson()이 있고, 구버전에는 ExportJson(bool)이 있어서 리플렉션으로 둘 다 지원합니다.
        // 실제 게임 코드에서는 RuntimeData.Instance.GetRuntimeDataJson()을 직접 호출해 SaveRuntimeDataJson에 넘기는 편이 더 명확합니다.
        RuntimeData runtimeData = FindFirstObjectByType<RuntimeData>(FindObjectsInactive.Include);
        if (runtimeData == null)
            return false;

        Type runtimeDataType = runtimeData.GetType();
        System.Reflection.MethodInfo getRuntimeDataJson = runtimeDataType.GetMethod("GetRuntimeDataJson");
        if (getRuntimeDataJson != null)
        {
            runtimeDataJson = getRuntimeDataJson.Invoke(runtimeData, null) as string;
            return !string.IsNullOrEmpty(runtimeDataJson);
        }

        System.Reflection.MethodInfo exportJson = runtimeDataType.GetMethod("ExportJson", new[] { typeof(bool) });
        if (exportJson != null)
        {
            runtimeDataJson = exportJson.Invoke(runtimeData, new object[] { false }) as string;
            return !string.IsNullOrEmpty(runtimeDataJson);
        }

        return false;
    }

    private RuntimeDataBackendResult UpsertPlayerCurrency(CurrencySection currency, long savedAt)
    {
        // Currency JSON 영역을 PlayerCurrency 테이블 컬럼으로 펼쳐 저장합니다.
        // 새 재화가 추가되면 여기에 param.Add("Energy", currency.Energy)를 추가하고
        // CurrencySection, TryReadCurrency, BuildRuntimeDataJson도 함께 확장하면 됩니다.
        Param param = CreateBaseParam(savedAt);
        param.Add("Gold", currency.Gold);
        param.Add("UpgradeStone", currency.UpgradeStone);
        param.Add("Gem", currency.Gem);

        return UpsertSingleRow(PlayerCurrencyTable, param, "SAVE_CURRENCY_FAILED");
    }

    private RuntimeDataBackendResult UpsertPlayerSkillLevels(string skillLevelsJson, long savedAt)
    {
        // skillLevels 배열은 컬럼 여러 개로 펼치지 않고 JSON 문자열 그대로 저장합니다.
        // 예: [{"skillId":0,"level":1},{"skillId":1,"level":3}]
        Param param = CreateBaseParam(savedAt);
        param.Add(SkillLevelsJsonColumn, skillLevelsJson);

        return UpsertSingleRow(PlayerSkillLevelsTable, param, "SAVE_SKILL_LEVELS_FAILED");
    }

    private RuntimeDataBackendResult UpsertPlayerStatLevels(StatLevelsSection statLevels, long savedAt)
    {
        // statLevels.attackLevel과 growth.attackLevel은 이름이 같지만 의미가 다릅니다.
        // 테이블을 PlayerStatLevels와 PlayerGrowth로 분리해 저장하므로 컬럼명 충돌을 피할 수 있습니다.
        Param param = CreateBaseParam(savedAt);
        param.Add("attackLevel", statLevels.attackLevel);
        param.Add("hpLevel", statLevels.hpLevel);
        param.Add("hpRegenLevel", statLevels.hpRegenLevel);
        param.Add("critChanceLevel", statLevels.critChanceLevel);
        param.Add("critDamageLevel", statLevels.critDamageLevel);

        return UpsertSingleRow(PlayerStatLevelsTable, param, "SAVE_STAT_LEVELS_FAILED");
    }

    private RuntimeDataBackendResult UpsertPlayerGrowth(GrowthSection growth, long savedAt)
    {
        // growth 영역은 성장 레벨/경험치/성장 포인트/성장 스탯 레벨만 저장합니다.
        // 최종 공격력 같은 계산 결과는 저장하지 않고 런타임 계산에 맡기는 편이 안전합니다.
        Param param = CreateBaseParam(savedAt);
        param.Add("growthLevel", growth.growthLevel);
        param.Add("nowExp", growth.nowExp);
        param.Add("growthPoint", growth.growthPoint);
        param.Add("attackLevel", growth.attackLevel);
        param.Add("hpLevel", growth.hpLevel);
        param.Add("critDamageLevel", growth.critDamageLevel);

        return UpsertSingleRow(PlayerGrowthTable, param, "SAVE_GROWTH_FAILED");
    }

    private RuntimeDataBackendResult UpsertSingleRow(string tableName, Param param, string errorCode)
    {
        // 각 테이블은 유저당 row 1개를 기준으로 처리합니다.
        // row가 없으면 Insert, 있으면 inDate를 사용해 UpdateV2를 수행합니다.
        RuntimeDataBackendResult rowResult = TryGetSingleRow(tableName, out JsonData row);
        if (!rowResult.isSuccess)
            return RuntimeDataBackendResult.Fail(errorCode, $"{tableName} row check failed: {rowResult.message}");

        BackendReturnObject bro;
        if (row == null)
        {
            bro = Backend.GameData.Insert(tableName, param);
        }
        else
        {
            string inDate = GetStringFromBackendField(row, "inDate");
            bro = Backend.GameData.UpdateV2(tableName, inDate, Backend.UserInDate, param);
        }

        if (!bro.IsSuccess())
            return RuntimeDataBackendResult.Fail(errorCode, $"{tableName} upsert failed: {bro}");

        return RuntimeDataBackendResult.Success($"{tableName} upsert succeeded.");
    }

    private RuntimeDataBackendResult LoadOrCreatePlayerCurrency(out CurrencySection currency)
    {
        // Currency row가 누락된 경우 신규 유저 또는 부분 저장 실패 복구 상황으로 보고 기본 row를 생성합니다.
        currency = CreateDefaultCurrency();

        RuntimeDataBackendResult rowResult = TryGetSingleRow(PlayerCurrencyTable, out JsonData row);
        if (!rowResult.isSuccess) return rowResult;

        if (row == null)
        {
            Debug.LogWarning("[BackndRuntimeDataRepository] PlayerCurrency row missing. Creating default row.");
            return UpsertPlayerCurrency(currency, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        currency.Gold = GetIntFromBackendField(row, "Gold", currency.Gold);
        currency.UpgradeStone = GetIntFromBackendField(row, "UpgradeStone", currency.UpgradeStone);
        currency.Gem = GetIntFromBackendField(row, "Gem", currency.Gem);
        return RuntimeDataBackendResult.Success("PlayerCurrency loaded.");
    }

    private RuntimeDataBackendResult LoadOrCreatePlayerSkillLevels(out string skillLevelsJson)
    {
        // skillLevelsJson이 비어 있거나 row가 없으면 기본 배열로 복구합니다.
        // 단, row는 있는데 JSON 파싱이 불가능하면 데이터 손상 가능성이 있으므로 실패를 반환합니다.
        skillLevelsJson = DefaultSkillLevelsJson;

        RuntimeDataBackendResult rowResult = TryGetSingleRow(PlayerSkillLevelsTable, out JsonData row);
        if (!rowResult.isSuccess) return rowResult;

        if (row == null)
        {
            Debug.LogWarning("[BackndRuntimeDataRepository] PlayerSkillLevels row missing. Creating default row.");
            return UpsertPlayerSkillLevels(skillLevelsJson, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        string storedJson = GetStringFromBackendField(row, SkillLevelsJsonColumn);
        if (string.IsNullOrEmpty(storedJson))
            storedJson = DefaultSkillLevelsJson;

        if (!ValidateSkillLevelsJson(storedJson, out string errorMessage))
            return RuntimeDataBackendResult.Fail("LOAD_SKILL_LEVELS_INVALID", errorMessage);

        skillLevelsJson = storedJson;
        return RuntimeDataBackendResult.Success("PlayerSkillLevels loaded.");
    }

    private RuntimeDataBackendResult LoadOrCreatePlayerStatLevels(out StatLevelsSection statLevels)
    {
        // StatLevels row가 누락된 경우 기본값 0 레벨들로 row를 생성합니다.
        statLevels = CreateDefaultStatLevels();

        RuntimeDataBackendResult rowResult = TryGetSingleRow(PlayerStatLevelsTable, out JsonData row);
        if (!rowResult.isSuccess) return rowResult;

        if (row == null)
        {
            Debug.LogWarning("[BackndRuntimeDataRepository] PlayerStatLevels row missing. Creating default row.");
            return UpsertPlayerStatLevels(statLevels, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        statLevels.attackLevel = GetIntFromBackendField(row, "attackLevel", statLevels.attackLevel);
        statLevels.hpLevel = GetIntFromBackendField(row, "hpLevel", statLevels.hpLevel);
        statLevels.hpRegenLevel = GetIntFromBackendField(row, "hpRegenLevel", statLevels.hpRegenLevel);
        statLevels.critChanceLevel = GetIntFromBackendField(row, "critChanceLevel", statLevels.critChanceLevel);
        statLevels.critDamageLevel = GetIntFromBackendField(row, "critDamageLevel", statLevels.critDamageLevel);
        return RuntimeDataBackendResult.Success("PlayerStatLevels loaded.");
    }

    private RuntimeDataBackendResult LoadOrCreatePlayerGrowth(out GrowthSection growth)
    {
        // Growth 기본값은 growthLevel 1, 성장 스탯 레벨 1을 사용합니다.
        // 최종 기본값 정책이 바뀌면 CreateDefaultGrowth()만 우선 확인하면 됩니다.
        growth = CreateDefaultGrowth();

        RuntimeDataBackendResult rowResult = TryGetSingleRow(PlayerGrowthTable, out JsonData row);
        if (!rowResult.isSuccess) return rowResult;

        if (row == null)
        {
            Debug.LogWarning("[BackndRuntimeDataRepository] PlayerGrowth row missing. Creating default row.");
            return UpsertPlayerGrowth(growth, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        growth.growthLevel = GetIntFromBackendField(row, "growthLevel", growth.growthLevel);
        growth.nowExp = GetFloatFromBackendField(row, "nowExp", growth.nowExp);
        growth.growthPoint = GetIntFromBackendField(row, "growthPoint", growth.growthPoint);
        growth.attackLevel = GetIntFromBackendField(row, "attackLevel", growth.attackLevel);
        growth.hpLevel = GetIntFromBackendField(row, "hpLevel", growth.hpLevel);
        growth.critDamageLevel = GetIntFromBackendField(row, "critDamageLevel", growth.critDamageLevel);
        return RuntimeDataBackendResult.Success("PlayerGrowth loaded.");
    }

    private RuntimeDataBackendResult TryGetSingleRow(string tableName, out JsonData row)
    {
        // 현재 로그인한 유저의 해당 테이블 row를 조회합니다.
        // 여러 row가 발견되더라도 현재 정책은 첫 번째 row만 사용합니다.
        // 운영 전에 테이블별 유저당 row 1개만 생성되도록 관리하는 것이 좋습니다.
        row = null;

        BackendReturnObject bro = Backend.GameData.GetMyData(tableName, new Where());
        if (!bro.IsSuccess())
            return RuntimeDataBackendResult.Fail("LOAD_TABLE_FAILED", $"{tableName} load failed: {bro}");

        JsonData rows = bro.GetReturnValuetoJSON()["rows"];
        if (rows == null || rows.Count <= 0)
            return RuntimeDataBackendResult.Success($"{tableName} row not found.");

        row = rows[0];
        return RuntimeDataBackendResult.Success($"{tableName} row found.");
    }

    private static Param CreateBaseParam(long savedAt)
    {
        // 모든 테이블에 공통으로 들어가는 메타 컬럼입니다.
        // schemaVersion은 마이그레이션 판단에 사용하고, lastSaveUnixTime은 중복 저장/디버깅에 사용할 수 있습니다.
        Param param = new Param();
        param.Add("schemaVersion", CurrentSchemaVersion);
        param.Add("lastSaveUnixTime", savedAt);
        return param;
    }

    private static bool TryParseRuntimeData(
        string runtimeDataJson,
        out RuntimeDataTableData data,
        out RuntimeDataBackendResult error)
    {
        // 저장 요청으로 들어온 RuntimeData JSON을 테이블별 임시 구조체로 분리합니다.
        // 여기서 검증에 실패하면 뒤끝에는 아무것도 저장하지 않습니다.
        data = null;
        error = null;

        if (string.IsNullOrWhiteSpace(runtimeDataJson))
        {
            error = RuntimeDataBackendResult.Fail("INVALID_JSON", "RuntimeData JSON is empty.");
            return false;
        }

        JsonData root;
        try
        {
            root = JsonMapper.ToObject(runtimeDataJson);
        }
        catch (Exception e)
        {
            error = RuntimeDataBackendResult.Fail("INVALID_JSON", $"RuntimeData JSON parse failed: {e.Message}");
            return false;
        }

        if (root == null || !root.IsObject)
        {
            error = RuntimeDataBackendResult.Fail("INVALID_JSON", "RuntimeData JSON root must be an object.");
            return false;
        }

        if (!TryReadCurrency(root, out CurrencySection currency, out error)) return false;
        if (!TryReadSkillLevels(root, out string skillLevelsJson, out error)) return false;
        if (!TryReadStatLevels(root, out StatLevelsSection statLevels, out error)) return false;
        if (!TryReadGrowth(root, out GrowthSection growth, out error)) return false;

        data = new RuntimeDataTableData
        {
            currency = currency,
            skillLevelsJson = skillLevelsJson,
            statLevels = statLevels,
            growth = growth
        };

        return true;
    }

    private static bool TryReadCurrency(JsonData root, out CurrencySection currency, out RuntimeDataBackendResult error)
    {
        // JSON의 Currency 영역을 읽고 음수 재화가 없는지 검증합니다.
        currency = CreateDefaultCurrency();
        error = null;

        if (!TryGetObject(root, "Currency", out JsonData json))
        {
            error = RuntimeDataBackendResult.Fail("MISSING_CURRENCY", "Currency object is missing.");
            return false;
        }

        currency.Gold = ReadInt(json, "Gold");
        currency.UpgradeStone = ReadInt(json, "UpgradeStone");
        currency.Gem = ReadInt(json, "Gem");

        if (currency.Gold < 0 || currency.UpgradeStone < 0 || currency.Gem < 0)
        {
            error = RuntimeDataBackendResult.Fail("VALIDATION_FAILED", "Currency values must be greater than or equal to 0.");
            return false;
        }

        return true;
    }

    private static bool TryReadSkillLevels(JsonData root, out string skillLevelsJson, out RuntimeDataBackendResult error)
    {
        // JSON의 skillLevels 배열을 읽어 배열 그대로 JSON 문자열로 다시 저장합니다.
        // 이 방식이면 스킬 데이터가 나중에 { skillId, level, exp }처럼 확장되어도 저장 컬럼은 그대로 둘 수 있습니다.
        skillLevelsJson = DefaultSkillLevelsJson;
        error = null;

        if (!root.ContainsKey("skillLevels") || root["skillLevels"] == null || !root["skillLevels"].IsArray)
        {
            error = RuntimeDataBackendResult.Fail("MISSING_SKILL_LEVELS", "skillLevels array is missing.");
            return false;
        }

        skillLevelsJson = JsonMapper.ToJson(root["skillLevels"]);
        if (!ValidateSkillLevelsJson(skillLevelsJson, out string errorMessage))
        {
            error = RuntimeDataBackendResult.Fail("VALIDATION_FAILED", errorMessage);
            return false;
        }

        return true;
    }

    private static bool TryReadStatLevels(JsonData root, out StatLevelsSection statLevels, out RuntimeDataBackendResult error)
    {
        // JSON의 statLevels 영역을 읽습니다.
        // growth에도 attackLevel/hpLevel이 있으므로 절대 root에서 납작하게 읽지 말고 statLevels 내부에서만 읽습니다.
        statLevels = CreateDefaultStatLevels();
        error = null;

        if (!TryGetObject(root, "statLevels", out JsonData json))
        {
            error = RuntimeDataBackendResult.Fail("MISSING_STAT_LEVELS", "statLevels object is missing.");
            return false;
        }

        statLevels.attackLevel = ReadInt(json, "attackLevel");
        statLevels.hpLevel = ReadInt(json, "hpLevel");
        statLevels.hpRegenLevel = ReadInt(json, "hpRegenLevel");
        statLevels.critChanceLevel = ReadInt(json, "critChanceLevel");
        statLevels.critDamageLevel = ReadInt(json, "critDamageLevel");

        if (statLevels.attackLevel < 0 || statLevels.hpLevel < 0 || statLevels.hpRegenLevel < 0 ||
            statLevels.critChanceLevel < 0 || statLevels.critDamageLevel < 0)
        {
            error = RuntimeDataBackendResult.Fail("VALIDATION_FAILED", "statLevels values must be greater than or equal to 0.");
            return false;
        }

        return true;
    }

    private static bool TryReadGrowth(JsonData root, out GrowthSection growth, out RuntimeDataBackendResult error)
    {
        // JSON의 growth 영역을 읽습니다.
        // growthLevel은 최소 1이어야 하고, 경험치/포인트/성장 스탯 레벨은 음수가 될 수 없습니다.
        growth = CreateDefaultGrowth();
        error = null;

        if (!TryGetObject(root, "growth", out JsonData json))
        {
            // 현재 작업 브랜치의 RuntimeData에는 아직 growth가 없을 수 있습니다.
            // develop 최신 RuntimeData에는 growth가 들어가므로, 최종 계약에서는 growth를 저장합니다.
            // 다만 matest 검증 단계에서 구버전 JSON도 저장 흐름을 테스트할 수 있도록
            // 누락된 growth는 기본값으로 보정해서 PlayerGrowth 테이블에 저장합니다.
            Debug.LogWarning("[BackndRuntimeDataRepository] growth object is missing. Default growth will be saved for compatibility.");
            return true;
        }

        growth.growthLevel = ReadInt(json, "growthLevel");
        growth.nowExp = ReadFloat(json, "nowExp");
        growth.growthPoint = ReadInt(json, "growthPoint");
        growth.attackLevel = ReadInt(json, "attackLevel");
        growth.hpLevel = ReadInt(json, "hpLevel");
        growth.critDamageLevel = ReadInt(json, "critDamageLevel");

        if (growth.growthLevel < 1 || growth.nowExp < 0f || growth.growthPoint < 0 ||
            growth.attackLevel < 0 || growth.hpLevel < 0 || growth.critDamageLevel < 0)
        {
            error = RuntimeDataBackendResult.Fail("VALIDATION_FAILED", "growth values are outside the allowed range.");
            return false;
        }

        return true;
    }

    private static bool ValidateSkillLevelsJson(string skillLevelsJson, out string errorMessage)
    {
        // PlayerSkillLevels.skillLevelsJson 컬럼에 저장하기 전/로드 후 모두 사용하는 검증 함수입니다.
        // 지금은 skillId와 level만 검사하지만, 스킬 데이터 필드가 늘어나면 여기 검증도 같이 확장하면 됩니다.
        errorMessage = "";

        try
        {
            JsonData skillLevels = JsonMapper.ToObject(skillLevelsJson);
            if (!skillLevels.IsArray)
            {
                errorMessage = "skillLevels must be a JSON array.";
                return false;
            }

            for (int i = 0; i < skillLevels.Count; i++)
            {
                JsonData skill = skillLevels[i];
                int skillId = ReadInt(skill, "skillId");
                int level = ReadInt(skill, "level");
                if (skillId < 0 || level < 0)
                {
                    errorMessage = "skillId and skill level must be greater than or equal to 0.";
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            errorMessage = $"skillLevels JSON parse failed: {e.Message}";
            return false;
        }

        return true;
    }

    private static bool TryGetObject(JsonData root, string key, out JsonData value)
    {
        // 최상위 JSON에서 특정 객체 섹션을 안전하게 꺼냅니다.
        // Currency, statLevels, growth처럼 객체여야 하는 영역에 사용합니다.
        value = null;
        if (root == null || !root.IsObject || !root.ContainsKey(key) || root[key] == null || !root[key].IsObject)
            return false;

        value = root[key];
        return true;
    }

    private static int ReadInt(JsonData json, string key)
    {
        // LitJson 값이 숫자 타입이든 문자열 타입이든 ToString 후 int로 변환합니다.
        // 필드가 없으면 0을 반환하므로, 필수 필드 누락 정책이 필요하면 호출부에서 별도 검증을 추가해야 합니다.
        if (json == null || !json.IsObject || !json.ContainsKey(key) || json[key] == null)
            return 0;

        int.TryParse(json[key].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
        return value;
    }

    private static float ReadFloat(JsonData json, string key)
    {
        // nowExp처럼 소수점이 가능한 값을 읽습니다.
        // CultureInfo.InvariantCulture를 사용해 지역 설정에 따른 소수점 파싱 문제를 피합니다.
        if (json == null || !json.IsObject || !json.ContainsKey(key) || json[key] == null)
            return 0f;

        float.TryParse(json[key].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
        return value;
    }

    private static string GetStringFromBackendField(JsonData row, string key)
    {
        // 뒤끝 응답은 컬럼 값이 {"S":"값"} 또는 {"N":"123"} 같은 래핑 형태로 올 수 있어 둘 다 처리합니다.
        // 기존 코드와 호환되도록 일반 문자열 형태도 함께 지원합니다.
        if (row == null || !row.ContainsKey(key) || row[key] == null)
            return "";

        JsonData field = row[key];
        if (field.IsObject)
        {
            if (field.ContainsKey("S")) return field["S"].ToString();
            if (field.ContainsKey("N")) return field["N"].ToString();
            if (field.ContainsKey("BOOL")) return field["BOOL"].ToString();
        }

        return field.ToString();
    }

    private static int GetIntFromBackendField(JsonData row, string key, int defaultValue)
    {
        string value = GetStringFromBackendField(row, key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;

        return defaultValue;
    }

    private static float GetFloatFromBackendField(JsonData row, string key, float defaultValue)
    {
        string value = GetStringFromBackendField(row, key);
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;

        return defaultValue;
    }

    private static string BuildRuntimeDataJson(
        CurrencySection currency,
        string skillLevelsJson,
        StatLevelsSection statLevels,
        GrowthSection growth)
    {
        // 로드한 테이블별 값을 클라이언트 계약 JSON 형태로 다시 조립합니다.
        // 반환 구조는 저장 요청 때 받은 RuntimeData JSON과 같은 이름/중첩 구조를 유지해야 합니다.
        // 새 섹션이 추가되면 여기에 최상위 JSON 필드를 추가하고, 저장 파싱/테이블 저장 함수도 함께 확장합니다.
        StringBuilder builder = new StringBuilder(512);
        builder.Append('{');
        builder.Append("\"Currency\":{");
        builder.Append("\"Gold\":").Append(currency.Gold).Append(',');
        builder.Append("\"UpgradeStone\":").Append(currency.UpgradeStone).Append(',');
        builder.Append("\"Gem\":").Append(currency.Gem).Append("},");
        builder.Append("\"skillLevels\":").Append(string.IsNullOrEmpty(skillLevelsJson) ? DefaultSkillLevelsJson : skillLevelsJson).Append(',');
        builder.Append("\"statLevels\":{");
        builder.Append("\"attackLevel\":").Append(statLevels.attackLevel).Append(',');
        builder.Append("\"hpLevel\":").Append(statLevels.hpLevel).Append(',');
        builder.Append("\"hpRegenLevel\":").Append(statLevels.hpRegenLevel).Append(',');
        builder.Append("\"critChanceLevel\":").Append(statLevels.critChanceLevel).Append(',');
        builder.Append("\"critDamageLevel\":").Append(statLevels.critDamageLevel).Append("},");
        builder.Append("\"growth\":{");
        builder.Append("\"growthLevel\":").Append(growth.growthLevel).Append(',');
        builder.Append("\"nowExp\":").Append(growth.nowExp.ToString(CultureInfo.InvariantCulture)).Append(',');
        builder.Append("\"growthPoint\":").Append(growth.growthPoint).Append(',');
        builder.Append("\"attackLevel\":").Append(growth.attackLevel).Append(',');
        builder.Append("\"hpLevel\":").Append(growth.hpLevel).Append(',');
        builder.Append("\"critDamageLevel\":").Append(growth.critDamageLevel).Append("}");
        builder.Append('}');
        return builder.ToString();
    }

    private static CurrencySection CreateDefaultCurrency()
    {
        // 신규 유저 또는 Currency row 누락 복구 시 사용하는 기본 재화값입니다.
        // 최종 기본값은 기획/클라이언트와 합의 후 여기에서 조정합니다.
        return new CurrencySection();
    }

    private static StatLevelsSection CreateDefaultStatLevels()
    {
        // 신규 유저 또는 StatLevels row 누락 복구 시 사용하는 기본 스탯 강화 레벨입니다.
        // 현재는 모든 강화 레벨 0으로 시작합니다.
        return new StatLevelsSection();
    }

    private static GrowthSection CreateDefaultGrowth()
    {
        // 신규 유저 또는 Growth row 누락 복구 시 사용하는 기본 성장 데이터입니다.
        // growthLevel과 성장 스탯 레벨은 1부터 시작하는 정책으로 둡니다.
        return new GrowthSection
        {
            growthLevel = 1,
            attackLevel = 1,
            hpLevel = 1,
            critDamageLevel = 1
        };
    }

    private class RuntimeDataTableData
    {
        // RuntimeData JSON을 테이블별로 분리한 임시 컨테이너입니다.
        // 뒤끝에 저장하기 전 단계에서만 사용합니다.
        public CurrencySection currency;
        public string skillLevelsJson;
        public StatLevelsSection statLevels;
        public GrowthSection growth;
    }

    private struct CurrencySection
    {
        // JSON의 Currency 영역과 PlayerCurrency 테이블 컬럼에 대응합니다.
        public int Gold;
        public int UpgradeStone;
        public int Gem;
    }

    private struct StatLevelsSection
    {
        // JSON의 statLevels 영역과 PlayerStatLevels 테이블 컬럼에 대응합니다.
        public int attackLevel;
        public int hpLevel;
        public int hpRegenLevel;
        public int critChanceLevel;
        public int critDamageLevel;
    }

    private struct GrowthSection
    {
        // JSON의 growth 영역과 PlayerGrowth 테이블 컬럼에 대응합니다.
        // statLevels와 같은 이름의 attackLevel/hpLevel이 있으므로 반드시 GrowthSection 안에서만 다룹니다.
        public int growthLevel;
        public float nowExp;
        public int growthPoint;
        public int attackLevel;
        public int hpLevel;
        public int critDamageLevel;
    }

    [Serializable]
    public class RuntimeDataBackendResult
    {
        // 클라이언트/디버그 UI에서 성공 여부와 실패 사유를 확인하기 위한 공통 응답 객체입니다.
        // 단순 bool만 반환하면 어느 테이블에서 실패했는지 알기 어려워서 errorCode/message를 함께 둡니다.
        public bool isSuccess;
        public string errorCode;
        public string message;
        public int schemaVersion;
        public string runtimeDataJson;

        public static RuntimeDataBackendResult Success(string message, string runtimeDataJson = "")
        {
            return new RuntimeDataBackendResult
            {
                isSuccess = true,
                errorCode = "",
                message = message,
                schemaVersion = CurrentSchemaVersion,
                runtimeDataJson = runtimeDataJson
            };
        }

        public static RuntimeDataBackendResult Fail(string errorCode, string message)
        {
            return new RuntimeDataBackendResult
            {
                isSuccess = false,
                errorCode = errorCode,
                message = message,
                schemaVersion = CurrentSchemaVersion,
                runtimeDataJson = ""
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}
