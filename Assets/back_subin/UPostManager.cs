using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;

public class UPostManager
{
    private List<UPostMailData> mailList =
        new List<UPostMailData>();

    public List<UPostMailData> MailList =>
        mailList;

    private static UPostManager instance;

    public static UPostManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UPostManager();
            }

            return instance;
        }
    }

    public void GetPostListTest()
    {
        mailList.Clear();

        int limit = 100;
        PostType postType = PostType.Admin;

        BackendReturnObject bro =
            Backend.UPost.GetPostList(
                postType,
                limit);

        if (!bro.IsSuccess())
        {
            Debug.LogError(bro.ToString());
            return;
        }

        LitJson.JsonData postListJson =
            bro.GetReturnValuetoJSON()["postList"];

        for (int i = 0; i < postListJson.Count; i++)
        {
            UPostMailData postItem =
                new UPostMailData();

            postItem.inDate =
                postListJson[i]["inDate"].ToString();

            postItem.title =
                postListJson[i]["title"].ToString();

            postItem.postType = postType;

            if (postType == PostType.Admin ||
                postType == PostType.Rank)
            {
                postItem.content =
                    postListJson[i]["content"].ToString();

                postItem.expirationDate =
                    DateTime.Parse(
                        postListJson[i]["expirationDate"].ToString());

                postItem.reservationDate =
                    DateTime.Parse(
                        postListJson[i]["reservationDate"].ToString());

                postItem.sentDate =
                    DateTime.Parse(
                        postListJson[i]["sentDate"].ToString());

                if (postListJson[i].ContainsKey("nickname"))
                {
                    postItem.nickname =
                        postListJson[i]["nickname"].ToString();
                }

                if (postListJson[i].ContainsKey("author"))
                {
                    postItem.author =
                        postListJson[i]["author"].ToString();
                }

                if (postListJson[i].ContainsKey("rankType"))
                {
                    postItem.rankType =
                        postListJson[i]["rankType"].ToString();
                }
            }

            if (postListJson[i]["items"].Count > 0)
            {
                for (int itemNum = 0;
                    itemNum < postListJson[i]["items"].Count;
                    itemNum++)
                {
                    UPostChartItem item =
                        new UPostChartItem();

                    item.itemCount =
                        int.Parse(
                            postListJson[i]["items"][itemNum]["itemCount"].ToString());

                    if (postListJson[i]["items"][itemNum]["item"]
                        .ContainsKey("chartFileName"))
                    {
                        item.chartFileName =
                            postListJson[i]["items"][itemNum]["item"]["chartFileName"].ToString();
                    }

                    item.itemID =
                        postListJson[i]["items"][itemNum]["item"]["itemID"].ToString();

                    item.itemName =
                        postListJson[i]["items"][itemNum]["item"]["itemName"].ToString();

                    if (postListJson[i]["items"][itemNum]["item"]
                        .ContainsKey("hpPower"))
                    {
                        item.hpPower =
                            postListJson[i]["items"][itemNum]["item"]["hpPower"].ToString();
                    }

                    postItem.items.Add(item);
                }
            }

            mailList.Add(postItem);

            Debug.Log(
                $"제목 : {postItem.title}\n" +
                $"내용 : {postItem.content}\n" +
                $"보상 개수 : {postItem.items.Count}");
        }

        Debug.Log(
            $"관리자 우편 {mailList.Count}개 로드 완료");
    }
}