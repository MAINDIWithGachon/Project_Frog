// List를 사용하기 위한 namespace입니다.
using System.Collections.Generic;

// 저장 데이터 관련 namespace입니다.
namespace Project.Gameplay.SaveData
{
    // JSON 저장이 가능하게 만드는 속성입니다.
    [System.Serializable]
    public class UserStatUpgradeEntry
    {
        // 강화 스탯 ID입니다.
        // 예: attack, hp, hp_regen, crit_rate, crit_damage
        public string StatID;

        // 해당 스탯의 현재 강화 레벨입니다.
        public int Level;
    }

    // 여러 강화 스탯 데이터를 하나로 묶는 클래스입니다.
    [System.Serializable]
    public class UserStatUpgradeSaveData
    {
        // 유저가 가진 강화 스탯 목록입니다.
        public List<UserStatUpgradeEntry> Stats = new List<UserStatUpgradeEntry>();

        // 특정 스탯의 레벨을 가져오는 함수입니다.
        public int GetLevel(string statId)
        {
            // Stats 리스트를 처음부터 끝까지 반복합니다.
            for (int i = 0; i < Stats.Count; i++)
            {
                // 현재 항목의 StatID가 찾는 statId와 같다면
                if (Stats[i].StatID == statId)
                {
                    // 해당 스탯의 레벨을 반환합니다.
                    return Stats[i].Level;
                }
            }

            // 찾는 스탯이 없으면 기본값 0을 반환합니다.
            return 0;
        }

        // 특정 스탯의 레벨을 설정하는 함수입니다.
        public void SetLevel(string statId, int level)
        {
            // Stats 리스트를 처음부터 끝까지 반복합니다.
            for (int i = 0; i < Stats.Count; i++)
            {
                // 현재 항목의 StatID가 찾는 statId와 같다면
                if (Stats[i].StatID == statId)
                {
                    // 해당 스탯의 레벨을 새 값으로 변경합니다.
                    Stats[i].Level = level;

                    // 변경했으므로 함수를 종료합니다.
                    return;
                }
            }

            // 기존에 없는 스탯이면 새 항목을 생성합니다.
            UserStatUpgradeEntry newEntry = new UserStatUpgradeEntry();

            // 새 항목의 스탯 ID를 설정합니다.
            newEntry.StatID = statId;

            // 새 항목의 레벨을 설정합니다.
            newEntry.Level = level;

            // 새 항목을 리스트에 추가합니다.
            Stats.Add(newEntry);
        }
    }
}