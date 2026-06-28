using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class UserMailData
{
    public string inDate;

    public string title;
    public string content;

    public string mailType;

    public bool isReceived;

    public DateTime expireDate;

    public List<RewardData> rewards =
        new List<RewardData>();
}