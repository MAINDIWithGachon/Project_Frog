using UnityEngine;

public class CombatPowerData : MonoBehaviour
{
    [Header("# Combat Power")]
    public int currentCombatPower;
    public float statCombatPower;
    public float skillCombatPower;

    [Header("# Stat Breakdown")]
    public StatContribution attack;
    public StatContribution maxHp;
    public StatContribution hpRegenPerSecond;
    public StatContribution critChance;
    public StatContribution critDamage;

    public void SetData(
        StatContribution attack,
        StatContribution maxHp,
        StatContribution hpRegenPerSecond,
        StatContribution critChance,
        StatContribution critDamage,
        float statCombatPower,
        float skillCombatPower)
    {
        this.attack = attack;
        this.maxHp = maxHp;
        this.hpRegenPerSecond = hpRegenPerSecond;
        this.critChance = critChance;
        this.critDamage = critDamage;
        this.statCombatPower = statCombatPower;
        this.skillCombatPower = skillCombatPower;
        currentCombatPower = Mathf.RoundToInt(statCombatPower + skillCombatPower);
    }
}
