using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RewardListWrapper
{
    public List<RewardData> rewards;

    public RewardListWrapper(
        List<RewardData> rewards)
    {
        this.rewards = rewards;
    }
}
