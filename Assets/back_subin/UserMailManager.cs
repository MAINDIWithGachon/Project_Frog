using System.Collections.Generic;
using UnityEngine;
using BackEnd;

public class UserMailManager
{

    private List<UserMailData> mailList = new List<UserMailData>();
    public List<UserMailData> MailList => mailList;
    private static UserMailManager instance;

    public static UserMailManager Instance
    {
         get
        {
            if(instance == null)
            {
                instance = new UserMailManager();
            }

            return instance;
        }
    }


    public void CreateMail(
        string title,
        string content,
        string mailType,
        List<RewardData> rewards)
    {
         Param param = new Param();

        param.Add("title", title);
        param.Add("content", content);
        param.Add("mailType", mailType);
        param.Add("isReceived", false);

         // 임시
        string rewardJson =
            JsonUtility.ToJson(
                new RewardListWrapper(rewards));
        
        Debug.Log("rewardJson = " + rewardJson);

        param.Add("rewardJson", rewardJson);

        var bro =
            Backend.GameData.Insert(
                "UserMail",
                param);

        if(bro.IsSuccess())
        {
            Debug.Log("우편 생성 성공");
        }
        else
        {
            Debug.LogError(
                "우편 생성 실패 : " + bro);
        }
    }

    public void LoadMail()
    {
        mailList.Clear();

        Where where = new Where();

        var bro = Backend.GameData.GetMyData(
        "UserMail",
        where);

        if (!bro.IsSuccess())
        {
            Debug.LogError("우편 조회 실패 : " + bro);
            return;
        }

        LitJson.JsonData rows = bro.FlattenRows();

        foreach (LitJson.JsonData row in rows)
        {
            UserMailData mail =
                new UserMailData();

            mail.inDate =
                row["inDate"].ToString();

            mail.title =
                row["title"].ToString();

            mail.content =
                row["content"].ToString();

            mail.mailType =
                row["mailType"].ToString();

            mail.isReceived =
                bool.Parse(row["isReceived"].ToString());

            if (mail.isReceived)
            {
                continue;
            }

            string rewardJson =
                row["rewardJson"].ToString();

            RewardListWrapper wrapper =
                JsonUtility.FromJson<RewardListWrapper>(rewardJson);

            if(wrapper != null && wrapper.rewards != null)
            {
                mail.rewards = wrapper.rewards;
            }

            mailList.Add(mail);
        }

        Debug.Log( $"우편 {mailList.Count}개 로드 완료");
        foreach(var mail in mailList)
        {
            Debug.Log(
                $"제목 : {mail.title}\n" +
                $"종류 : {mail.mailType}\n" +
                $"수령 : {mail.isReceived}");
        }
    }

    public bool ReceiveMail(UserMailData mail)
    {
        Param param = new Param();
        param.Add("isReceived", true);

        var bro = Backend.GameData.UpdateV2(
            "UserMail",
            mail.inDate,
            Backend.UserInDate,
            param);

        if (bro.IsSuccess())
        {
            mail.isReceived = true;

            string rewardInfo = "";

            foreach (var reward in mail.rewards)
            {
                 rewardInfo += $"{reward.id} x{reward.count}\n";
            }

            Debug.Log(
                $"우편 수령 완료\n" +
                $"제목 : {mail.title}\n" +
                $"내용 : {mail.content}\n" +
                $"보상 :\n{rewardInfo}");

            return true;
        }

        Debug.LogError(
            "우편 수령 실패 : " + bro);

        return false;
    }
}
