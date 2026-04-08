using TMPro;
using UnityEngine;

public class StatContent : MonoBehaviour
{
    public enum StatType
    {
        ATK,
        HP,
        HpRegen,
        CriChance,
        CriDamage
    }
    public StatType statType;//인스펙터 지정
    public TMP_Text text_Lv;
    public TMP_Text text_Value;
    public TMP_Text text_Price;
}
