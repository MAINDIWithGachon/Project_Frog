using UnityEngine;

[System.Serializable]
public class SkillDetailData
{
    // 스킬을 구분하기 위한 고유 ID
    public int skillId;

    // 상세창에 표시할 스킬 이름
    public string skillName;

    // 상세창에 표시할 스킬 설명
    public string description;

    // 상세창 및 장착 슬롯에 사용할 스킬 아이콘
    public Sprite icon;

    // 상세창 하단 수치의 현재값
    public int currentValue;

    // 상세창 하단 수치의 최대값
    public int maxValue;
}
