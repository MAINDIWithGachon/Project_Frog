using System;
using UnityEngine;

/// <summary>
/// 런타임에서 사용하는 플레이어 데이터의 단일 진실 원본(Single Source of Truth) 역할.
/// 
/// 핵심 개념:
/// 1. mockJson은 "초기 서버 응답을 흉내 낸 문자열 데이터"이다.
/// 2. 실제 인게임 로직은 JSON 문자열을 직접 수정하지 않고, root 객체만 사용한다.
/// 3. 게임 도중에는 root를 계속 수정한다.
/// 4. 저장이 필요한 시점에만 root를 다시 JSON으로 직렬화하여 서버로 보낸다.
/// 
/// 즉,
/// - 로드 시점: JSON -> 객체(root)
/// - 플레이 중: 객체(root) 수정
/// - 저장 시점: 객체(root) -> JSON
/// 
/// 이 구조를 사용하면 인게임에서는 객체 기반으로 편하게 작업할 수 있고,
/// 저장 시점에만 JSON 변환을 하면 되므로 관리가 단순해진다.
/// </summary>
public class RuntimeData : MonoBehaviour
{
    public event Action OnDataChanged;

    /// <summary>
    /// 테스트용 가짜 서버 데이터(JSON).
    /// 
    /// 실제 서버가 아직 없거나, 서버 연결 전에 인게임 로직을 먼저 테스트할 때 사용한다.
    /// 인스펙터에서 여러 줄로 보기 편하도록 TextArea를 사용했다.
    /// 
    /// 현재 저장하는 데이터:
    /// - Currency : 재화 정보
    /// - skillLevels : 스킬 ID별 레벨
    /// - statLevels : 5대 스탯 강화 레벨
    /// 
    /// 주의:
    /// 여기 들어있는 값은 "초기 로드용 값"이다.
    /// 게임 중에 값이 바뀌더라도 이 문자열이 자동으로 바뀌지는 않는다.
    /// 실제 플레이 중 최신 값은 아래 root 객체가 가진다.
    /// </summary>
    [TextArea(10, 60)]
    private string mockJson = @"
    {
        ""Currency"": {
            ""Gold"": 10000,
            ""UpgradeStone"": 100
        },
        ""skillLevels"": [
            { ""skillId"": 0, ""level"": 1 },
            { ""skillId"": 1, ""level"": 3 },
            { ""skillId"": 2, ""level"": 5 }
        ],
        ""statLevels"": {
            ""attackLevel"": 10,
            ""hpLevel"": 8,
            ""hpRegenLevel"": 3,
            ""critChanceLevel"": 6,
            ""critDamageLevel"": 4
        }
    }";

    /// <summary>
    /// 인게임에서 실제로 사용하는 진짜 데이터 객체.
    /// 
    /// 이 클래스의 가장 중요한 필드다.
    /// 게임 내 모든 로직은 가능하면 이 root를 기준으로 데이터를 읽고 수정해야 한다.
    /// 
    /// 예:
    /// - 스킬 레벨 조회
    /// - 골드 사용
    /// - 스탯 레벨 증가
    /// - 저장 시 JSON 추출
    /// 
    /// root가 곧 현재 세션의 최신 데이터라고 생각하면 된다.
    /// </summary>
    private RootData root;

    /// <summary>
    /// Unity 생명주기 시작 지점.
    /// 
    /// 게임 오브젝트가 활성화될 때 root가 아직 없으면 mockJson을 파싱해서 root를 생성한다.
    /// 즉, 최초 1회 초기화를 담당한다.
    /// </summary>
    private void Awake()
    {
        LoadFromJsonIfNeeded();
    }

    /// <summary>
    /// root가 아직 생성되지 않았을 때만 JSON으로부터 데이터를 로드한다.
    /// 
    /// 왜 "IfNeeded" 인가?
    /// - 이미 root가 만들어진 상태라면 다시 JSON을 파싱할 필요가 없기 때문이다.
    /// - 인게임 중에는 root가 최신 데이터이며, JSON 문자열은 초기값일 뿐이다.
    /// 
    /// 처리 흐름:
    /// 1. root가 이미 있으면 즉시 종료
    /// 2. mockJson을 RootData로 파싱 시도
    /// 3. 실패 시 빈 RootData 생성
    /// 4. null 방지용 보정(EnsureValid) 실행
    /// </summary>
    private void LoadFromJsonIfNeeded()
    {
        // 이미 root가 존재하면 다시 로드할 필요가 없다.
        if (root != null) return;

        try
        {
            // JSON 문자열을 RootData 객체로 변환
            root = JsonUtility.FromJson<RootData>(mockJson);
        }
        catch (Exception e)
        {
            // JSON 형식이 잘못되었거나 예외가 발생한 경우를 대비
            Debug.LogError($"[RuntimeData] JSON parse failed: {e.Message}");

            // 파싱 실패 시에도 게임이 완전히 멈추지 않도록 빈 객체 생성
            root = new RootData();
        }

        // 파싱 후 누락된 필드(null)가 있을 수 있으므로 안전하게 보정
        EnsureValid();
    }

    /// <summary>
    /// root 내부의 null 필드를 안전한 기본값으로 보정한다.
    /// 
    /// 이유:
    /// JSON에 특정 필드가 빠져 있으면 해당 필드는 null일 수 있다.
    /// 예를 들어 skillLevels가 JSON에 없으면 root.skillLevels는 null이 된다.
    /// 
    /// null 상태를 방치하면 나중에
    /// - root.skillLevels.Length
    /// - root.Currency.Gold
    /// 같은 접근에서 NullReferenceException이 발생할 수 있다.
    /// 
    /// 따라서 이 메서드에서 최소한의 기본값을 채워 넣는다.
    /// </summary>
    private void EnsureValid()
    {
        // root 자체가 null이면 비어 있는 데이터 객체 생성
        if (root == null) root = new RootData();

        // Currency가 없으면 기본 재화 객체 생성
        if (root.Currency == null) root.Currency = new CurrencyData();

        // skillLevels가 없으면 빈 배열로 초기화
        // 배열 길이 0인 상태는 "데이터는 없지만 null은 아님" 이라서 처리하기 쉽다.
        if (root.skillLevels == null) root.skillLevels = Array.Empty<SkillLevelData>();

        // statLevels가 없으면 기본 스탯 레벨 객체 생성
        if (root.statLevels == null) root.statLevels = new StatLevelData();
    }

    // =========================
    // 조회
    // =========================

    /// <summary>
    /// 특정 스킬 ID의 현재 레벨을 반환한다.
    /// 
    /// 사용 예:
    /// - 스킬 버튼 UI에서 현재 레벨 표시
    /// - 스킬 데미지 계산 전 레벨 참조
    /// - 강화 가능 여부 판단
    /// 
    /// 동작 방식:
    /// - root.skillLevels 배열을 순회하면서
    /// - 전달받은 skillId와 일치하는 항목을 찾고
    /// - 해당 level을 반환한다.
    /// 
    /// 못 찾은 경우:
    /// - 현재는 0을 반환한다.
    /// - 즉, "등록되지 않은 스킬"의 기본 레벨은 0으로 간주한다.
    /// 
    /// 주의:
    /// 스킬이 많아지면 지금처럼 배열 선형 탐색보다는
    /// Dictionary<int, SkillLevelData> 캐시를 고려할 수 있다.
    /// 하지만 현재 규모에서는 단순 배열 순회도 충분하다.
    /// </summary>
    /// <param name="skillId">조회할 스킬의 고유 ID</param>
    /// <returns>해당 스킬의 현재 레벨. 없으면 0</returns>
    public int GetSkillLevel(int skillId)
    {
        // 혹시 아직 초기화가 안 된 상태라면 먼저 로드
        LoadFromJsonIfNeeded();

        // 등록된 스킬 레벨 데이터 배열을 끝까지 순회
        for (int i = 0; i < root.skillLevels.Length; i++)
        {
            // 현재 검사 중인 스킬 ID가 요청한 ID와 같다면
            if (root.skillLevels[i].skillId == skillId)
                return root.skillLevels[i].level;
        }

        // 해당 skillId를 찾지 못한 경우 기본값 0 반환
        return 0;
    }

    /// <summary>
    /// 현재 런타임 데이터의 root 객체 자체를 반환한다.
    /// 
    /// 장점:
    /// - 디버깅 시 전체 데이터를 한 번에 보기 좋다.
    /// - 외부 시스템에서 여러 필드를 한 번에 참조할 수 있다.
    /// 
    /// 주의:
    /// 이 메서드로 root를 직접 꺼내서 외부에서 마음대로 수정하면
    /// RuntimeData를 둔 의미가 약해질 수 있다.
    /// 
    /// 권장:
    /// - 읽기(조회) 목적일 때 사용
    /// - 수정은 가능하면 SetSkillLevel, SpendGold 같은 메서드로 수행
    /// </summary>
    /// <returns>현재 세션의 전체 데이터 root 객체</returns>
    public RootData GetRoot()
    {
        LoadFromJsonIfNeeded();
        return root;
    }

    // =========================
    // 수정
    // =========================

    /// <summary>
    /// 특정 스킬 ID의 레벨을 지정한 값으로 직접 설정한다.
    /// 
    /// 사용 예:
    /// - 서버 동기화 결과 반영
    /// - 디버그/치트 기능
    /// - 레벨업 처리 후 수동 지정
    /// 
    /// 현재 동작 방식:
    /// - 기존 배열에서 skillId를 찾는다.
    /// - 찾으면 그 항목의 level을 newLevel로 교체한다.
    /// - 찾지 못하면 경고 로그를 출력한다.
    /// 
    /// 주의:
    /// 현재는 "없는 스킬 ID를 새로 추가" 하지는 않는다.
    /// 즉, 반드시 mockJson 또는 초기 데이터에 해당 skillId가 들어 있어야 한다.
    /// 
    /// 나중에 필요하면:
    /// - 없는 스킬 ID를 자동 추가하는 기능
    /// - 최소/최대 레벨 제한
    /// 등을 붙일 수 있다.
    /// </summary>
    /// <param name="skillId">수정할 스킬의 고유 ID</param>
    /// <param name="newLevel">설정할 새 레벨 값</param>
    public void SetSkillLevel(int skillId, int newLevel)
    {
        LoadFromJsonIfNeeded();

        // skillId가 일치하는 항목을 찾아 레벨 교체
        for (int i = 0; i < root.skillLevels.Length; i++)
        {
            if (root.skillLevels[i].skillId == skillId)
            {
                root.skillLevels[i].level = newLevel;
                RaiseDataChanged();
                return;
            }
        }

        // 현재 구조에서는 없는 skillId는 추가하지 않고 경고만 출력
        Debug.LogWarning($"[RuntimeData] skillId {skillId} not found.");
    }

    /// <summary>
    /// 골드를 지정한 양만큼 증가시킨다.
    /// 
    /// 사용 예:
    /// - 전투 보상 지급
    /// - 퀘스트 보상
    /// - 광고 보상
    /// - GM/디버그 지급
    /// 
    /// 현재는 음수 방어를 하지 않는다.
    /// 따라서 호출하는 쪽에서 amount가 양수인지 보장하는 것이 좋다.
    /// 
    /// 나중에 필요하면:
    /// - amount가 0 이하일 때 무시
    /// - 변경 이벤트 발행
    /// - 저장 dirty 플래그 처리
    /// 등을 추가할 수 있다.
    /// </summary>
    /// <param name="amount">추가할 골드 양</param>
    public void AddGold(int amount)
    {
        LoadFromJsonIfNeeded();
        root.Currency.Gold += amount;
        RaiseDataChanged();
    }

    public int GetGold()
    {
        // 장비 강화/상점 구매처럼 현재 보유 골드를 바로 확인해야 할 때 사용합니다.
        LoadFromJsonIfNeeded();
        return root.Currency.Gold;
    }

    public int GetUpgradeStone()
    {
        // 장비 강화 UI에서 강화석 보유량을 확인할 때 사용합니다.
        LoadFromJsonIfNeeded();
        return root.Currency.UpgradeStone;
    }

    public void AddUpgradeStone(int amount)
    {
        // 보상 지급이나 디버그 지급처럼 강화석을 증가시킬 때 사용합니다.
        LoadFromJsonIfNeeded();
        root.Currency.UpgradeStone += amount;
    }

    /// <summary>
    /// 골드를 지정한 양만큼 차감하려고 시도한다.
    /// 
    /// 사용 예:
    /// - 스킬 강화 비용 지불
    /// - 장비 강화 비용 지불
    /// - 상점 구매
    /// 
    /// 처리 방식:
    /// 1. 현재 골드가 충분한지 검사
    /// 2. 부족하면 false 반환
    /// 3. 충분하면 차감 후 true 반환
    /// 
    /// 이 메서드를 bool 반환으로 만든 이유:
    /// - 호출한 쪽이 "성공/실패"를 쉽게 판단할 수 있다.
    /// - 별도로 현재 골드를 먼저 검사하지 않아도 된다.
    /// 
    /// 예:
    /// if (runtimeData.SpendGold(cost))
    /// {
    ///     // 구매 성공
    /// }
    /// else
    /// {
    ///     // 골드 부족
    /// }
    /// </summary>
    /// <param name="amount">차감할 골드 양</param>
    /// <returns>차감 성공 시 true, 골드 부족 시 false</returns>
    public bool SpendGold(int amount)
    {
        LoadFromJsonIfNeeded();

        // 현재 골드가 부족하면 실패
        if (root.Currency.Gold < amount)
            return false;

        // 충분하면 차감 후 성공 반환
        root.Currency.Gold -= amount;
        RaiseDataChanged();
        return true;
    }

    public bool SpendUpgradeStone(int amount)
    {
        // 장비 강화에 필요한 강화석을 차감합니다.
        // 보유량이 부족하면 아무것도 차감하지 않고 false를 반환합니다.
        LoadFromJsonIfNeeded();

        if (root.Currency.UpgradeStone < amount)
            return false;

        root.Currency.UpgradeStone -= amount;
        return true;
    }

    /// <summary>
    /// 외부에서 root를 직접 수정한 뒤 구독자들에게 변경 사실을 알려준다.
    /// 
    /// 장기적으로는 모든 수정 경로를 RuntimeData 메서드로 통일하는 편이 더 좋지만,
    /// 현재 단계에서는 이 브리지 메서드만으로도 UI/계산 갱신 흐름을 안정적으로 연결할 수 있다.
    /// </summary>
    public void NotifyDataChanged()
    {
        LoadFromJsonIfNeeded();
        EnsureValid();
        RaiseDataChanged();
    }

    private void RaiseDataChanged()
    {
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 현재 root 데이터를 JSON 문자열로 직렬화해서 반환한다.
    /// 
    /// 이 메서드는 저장/전송 시점에 사용한다.
    /// 
    /// 예:
    /// - 서버에 전체 데이터 저장 요청을 보낼 때
    /// - 디버그 로그로 현재 데이터 상태를 확인할 때
    /// - 로컬 파일 저장 시
    /// 
    /// pretty = true:
    /// - 사람이 읽기 좋은 예쁜 JSON
    /// - 디버깅에 유리
    /// 
    /// pretty = false:
    /// - 줄바꿈 없는 압축 JSON
    /// - 네트워크 전송량 측면에서 약간 유리
    /// </summary>
    /// <param name="pretty">true면 보기 좋은 형식, false면 압축 형식</param>
    /// <returns>현재 root 데이터를 JSON으로 변환한 문자열</returns>
    public string ExportJson(bool pretty = true)
    {
        LoadFromJsonIfNeeded();
        return JsonUtility.ToJson(root, pretty);
    }

    // =========================
    // 데이터 클래스
    // =========================

    /// <summary>
    /// 서버와 주고받는 최상위 데이터 루트.
    /// 
    /// 이 클래스 하나가 전체 플레이어 저장 데이터의 뿌리라고 보면 된다.
    /// 현재 포함된 데이터:
    /// - Currency : 재화
    /// - skillLevels : 스킬별 레벨
    /// - statLevels : 5대 스탯 강화 레벨
    /// 
    /// 나중에 확장 가능:
    /// - 장비 정보
    /// - 인벤토리
    /// - 업적
    /// - 스테이지 진행도
    /// - 설정값
    /// </summary>
    [Serializable]
    public class RootData
    {
        /// <summary>
        /// 플레이어 재화 정보.
        /// 현재는 Gold, UpgradeStone 두 종류를 사용한다.
        /// </summary>
        public CurrencyData Currency;

        /// <summary>
        /// 스킬 ID별 레벨 목록.
        /// 
        /// 예:
        /// skillId = 0, level = 3
        /// skillId = 1, level = 5
        /// 
        /// 주의:
        /// 배열이므로 스킬 추가/삭제가 많아지면
        /// 나중에는 List 또는 Dictionary 캐시를 고려할 수 있다.
        /// </summary>
        public SkillLevelData[] skillLevels;

        /// <summary>
        /// 5대 기본 스탯 강화 레벨 정보.
        /// 
        /// 여기에는 "최종 공격력 수치"가 아니라
        /// "UI에서 몇 번 강화했는지에 해당하는 레벨"만 저장한다.
        /// 실제 공격력, 체력 등의 최종 수치는
        /// 별도 계산식(기본값 + 레벨 보정값)을 통해 런타임에서 계산하는 것이 바람직하다.
        /// </summary>
        public StatLevelData statLevels;
    }

    /// <summary>
    /// 재화 데이터.
    /// 
    /// Gold:
    /// - 일반적인 소모성 재화
    /// 
    /// UpgradeStone:
    /// - 강화 재화
    /// 
    /// 나중에 필요하면
    /// - Diamond
    /// - Ticket
    /// - Energy
    /// 등을 추가할 수 있다.
    /// </summary>
    [Serializable]
    public class CurrencyData
    {
        public int Gold;
        public int UpgradeStone;
    }

    /// <summary>
    /// 스킬 1개의 레벨 정보를 담는 데이터.
    /// 
    /// skillId:
    /// - 어떤 스킬인지 구분하는 고유 ID
    /// 
    /// level:
    /// - 해당 스킬의 현재 레벨
    /// </summary>
    [Serializable]
    public class SkillLevelData
    {
        public int skillId;
        public int level;
    }

    /// <summary>
    /// 5대 스탯의 "강화 레벨" 저장용 데이터.
    /// 
    /// 매우 중요:
    /// 여기에는 실제 전투 최종 수치를 저장하지 않는다.
    /// 예를 들어 attackLevel = 10 이라는 것은
    /// "공격력 강화 버튼을 10번 올린 상태"를 의미한다.
    /// 
    /// 실제 공격력 계산 예시:
    /// finalAttack = baseAttack + attackLevel * attackPerLevel
    /// 
    /// 이런 식의 계산은 별도의 StatCalculator에서 처리하는 것이 좋다.
    /// </summary>
    [Serializable]
    public class StatLevelData
    {
        /// <summary>
        /// 공격력 강화 레벨
        /// </summary>
        public int attackLevel;

        /// <summary>
        /// 체력 강화 레벨
        /// </summary>
        public int hpLevel;

        /// <summary>
        /// 초당 체력 회복 강화 레벨
        /// </summary>
        public int hpRegenLevel;

        /// <summary>
        /// 치명타 확률 강화 레벨
        /// </summary>
        public int critChanceLevel;

        /// <summary>
        /// 치명타 공격력 강화 레벨
        /// </summary>
        public int critDamageLevel;
    }
}
