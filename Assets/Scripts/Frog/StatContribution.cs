using UnityEngine;

[System.Serializable]
public struct StatContribution
{
    public float baseValue;
    public float statValue;
    public float growthValue;
    public float equipmentValue;
    public float recipeValue;
    public float buffValue;

    public float FinalValue => baseValue + statValue + growthValue + equipmentValue + recipeValue + buffValue;
    public float CombatPowerValue => baseValue + statValue + growthValue + equipmentValue + recipeValue;

    public StatContribution(
        float baseValue,
        float statValue,
        float growthValue,
        float equipmentValue,
        float recipeValue,
        float buffValue)
    {
        this.baseValue = baseValue;
        this.statValue = statValue;
        this.growthValue = growthValue;
        this.equipmentValue = equipmentValue;
        this.recipeValue = recipeValue;
        this.buffValue = buffValue;
    }
}
