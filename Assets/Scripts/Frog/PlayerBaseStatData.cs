using System;
using UnityEngine;

/// <summary>
/// 캐릭터가 원래 가지고 있는 "기본 능력치" 데이터.
/// 
/// 이 값들은 저장 데이터가 아니라 캐릭터의 베이스 성능을 의미한다.
/// 예:
/// - 기본 공격력 5
/// - 기본 체력 100
/// - 기본 초당 체력 회복 1
/// - 기본 치명타 확률 5
/// - 기본 치명타 공격력 50
/// 
/// PlayerStatController는 이 기본값을 시작점으로 삼아,
/// 스탯 강화 / 장비 / 레시피 / 버프 값을 더해서 FinalStatData를 만든다.
/// 
/// 향후 캐릭터의 BaseStatData는 캐릭터 성장에 따라 변할 수 있다.
/// </summary>
public class PlayerBaseStatData : MonoBehaviour
{
    [Header("# Base Stat")]
    public float baseAttack;
    public float baseMaxHp;
    public float baseHpRegenPerSecond;
    public float baseCritChance;
    public float baseCritDamage;
}