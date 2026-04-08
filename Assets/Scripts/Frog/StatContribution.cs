using UnityEngine;

[System.Serializable]
public struct StatContribution
{
    public float baseValue;
    public float statValue;
    public float equipmentValue;
    public float recipeValue;
    public float buffValue;

    public float FinalValue => baseValue + statValue + equipmentValue + recipeValue + buffValue;
    public float CombatPowerValue => baseValue + statValue + equipmentValue + recipeValue;

    public StatContribution(float baseValue, float statValue, float equipmentValue, float recipeValue, float buffValue)
    {
        this.baseValue = baseValue;
        this.statValue = statValue;
        this.equipmentValue = equipmentValue;
        this.recipeValue = recipeValue;
        this.buffValue = buffValue;
    }
}
