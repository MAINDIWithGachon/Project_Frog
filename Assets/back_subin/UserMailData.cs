using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class UserMailData
{
    public string inDate;

    public string title;
    public string content;

    public string mailType;

    public bool isReceived;

    public List<RewardData> rewards =
        new List<RewardData>();
}