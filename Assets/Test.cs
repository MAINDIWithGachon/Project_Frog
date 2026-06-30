using System.Collections.Generic;
using UnityEngine;

public class MailTest : MonoBehaviour
{
    public void SendGoldMail()
    {
        List<RewardData> rewards =
            new List<RewardData>();

        rewards.Add(
            new RewardData
            {
                type = "Gold",
                id = "Gold",
                count = 1000
            });

        UserMailManager.Instance.CreateMail(
            "골드 지급",
            "골드 1000개 지급",
            "Reward",
            rewards);
    }

    public void SendJamMail()
    {
        List<RewardData> rewards =
            new List<RewardData>();

        rewards.Add(
            new RewardData
            {
                type = "Jam",
                id = "Jam",
                count = 10
            });

        UserMailManager.Instance.CreateMail(
            "잼 지급",
            "잼 10개 지급",
            "Reward",
            rewards);
    }

    public void SendEquipmentMail()
    {
        List<RewardData> rewards =
            new List<RewardData>();

        rewards.Add(
            new RewardData
            {
                type = "Equipment",
                id = "Equipment",
                count = 1
            });

        UserMailManager.Instance.CreateMail(
            "장비 지급",
            "장비 1개 지급",
            "Reward",
            rewards);
    }

    public void SendRecipeMail()
    {
        List<RewardData> rewards =
            new List<RewardData>();

        rewards.Add(
            new RewardData
            {
                type = "Recipe",
                id = "Recipe",
                count = 1
            });

        UserMailManager.Instance.CreateMail(
            "레시피 지급",
            "레시피 1개 지급",
            "Reward",
            rewards);
    }
}