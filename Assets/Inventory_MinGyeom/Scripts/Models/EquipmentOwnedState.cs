using System;

/// <summary>
/// 플레이어가 현재 보유한 장비와 그 장비의 현재 레벨 상태를 저장하는 런타임 데이터입니다.
/// </summary>
[Serializable]
public class EquipmentOwnedState
{
    public string equipmentId;
    public int currentLevel = 1;
    public int ownedCount = 1;
}
