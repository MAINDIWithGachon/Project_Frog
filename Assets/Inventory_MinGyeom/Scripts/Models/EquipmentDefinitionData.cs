using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 장비 데이터베이스 안에 저장되는 개별 장비의 고정 정보 묶음입니다.
/// </summary>
[Serializable]
public class EquipmentDefinitionData
{
    // 아이템을 식별하고 타입을 판단할 때 사용하는 기본 정보입니다.
    [Header("Identity")]
    public string equipmentId;
    public string displayName;
    public EquipmentCategory category;
    public EquipmentRarity rarity;

    // 인벤토리 목록과 장착 슬롯 UI에서 보여줄 표시용 데이터입니다.
    [Header("UI")]
    [FormerlySerializedAs("icon")] public Sprite uiIcon;
    [TextArea] public string description;

    // 강화/성장 관련 표시에서 사용할 기본 성장 설정입니다.
    [Header("Progression")]
    [Min(1)] public int maxLevel = 10;
    [Min(1)] public int requiredItemCountForNextLevel = 10;

    // 현재 프로토타입에서 사용하는 예시 능력치 값입니다.
    [Header("Prototype Stats")]
    public int attack;
    public int hp;
    public float healPerSec;
    public float critChance;
    public float critDamage;
}
