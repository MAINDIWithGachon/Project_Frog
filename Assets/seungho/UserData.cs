// 이 namespace는 저장 데이터 관련 클래스를 묶기 위한 공간입니다.
namespace Project.Gameplay.SaveData
{
    // UserData는 유저의 기본 게임 데이터를 저장하는 클래스입니다.
    public class UserData
    {
        // 뒤끝 서버에서 이 데이터 행을 구분하는 고유 ID입니다.
        public string inDate;

        // 유저가 현재 보유한 골드입니다.
        public long Gold = 0;

        // 유저가 현재 보유한 보석입니다.
        public long Gems = 0;

        // 유저가 지금까지 획득한 총 골드입니다.
        // 랭킹이나 업적에 사용할 수 있습니다.
        public long TotalGoldEarned = 0;

        // 현재 사용 중인 프로필 이미지 ID입니다.
        // 실제 이미지를 저장하는 것이 아니라 이미지 ID만 저장합니다.
        public string UsedProfileImageID = "profile_frog_basic";

        // 현재 선택한 캐릭터 ID입니다.
        public string SelectedCharacterID = "frog_basic";

        // 현재 장착 중인 장비 정보를 JSON 문자열로 저장합니다.
        // 예: {"weapon":"weapon_001","armor":"armor_001"}
        public string EquippedItemJson = "{}";

        // 현재 장착 중인 스킬 정보를 JSON 문자열로 저장합니다.
        // 예: ["skill_001","skill_002"]
        public string EquippedSkillJson = "[]";

        // 마지막으로 플레이한 스테이지 ID입니다.
        public string LastPlayedStageID = "stage_001";
    }
}