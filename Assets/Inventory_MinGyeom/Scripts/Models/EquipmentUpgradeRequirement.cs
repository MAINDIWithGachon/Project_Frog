using System;

/// <summary>
/// 장비 강화 가능 여부와 요구 재화를 한 번에 전달하는 데이터입니다.
/// </summary>
[Serializable]
public class EquipmentUpgradeRequirement
{
    // 어떤 장비를 기준으로 계산한 결과인지 식별하는 ID입니다.
    public string equipmentId;

    // 현재 장비 레벨과 강화 성공 시 도달할 다음 레벨입니다.
    public int currentLevel;
    public int nextLevel;

    // 플레이어가 현재 보유 중인 동일 장비 개수입니다.
    public int ownedCount;

    // 강화에 소모되는 동일 장비 개수와 재화 요구량입니다.
    public int requiredDuplicateCount;
    public int requiredGold;
    public int requiredUpgradeStone;

    // 업그레이드 버튼 활성화 판단에 필요한 조건들입니다.
    public bool isAtMaxLevel;
    public bool hasEnoughDuplicates;
    public bool hasEnoughGold;
    public bool hasEnoughUpgradeStone;

    // UI에서는 이 값만 보면 최종 강화 가능 여부를 바로 판단할 수 있습니다.
    public bool CanUpgrade => !isAtMaxLevel &&
                              hasEnoughDuplicates &&
                              hasEnoughGold &&
                              hasEnoughUpgradeStone;
}
