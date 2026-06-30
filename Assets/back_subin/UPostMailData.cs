using System;
using System.Collections.Generic;
using BackEnd;

[Serializable]
public class UPostChartItem
{
    public string chartFileName;
    public string itemID;
    public string itemName;
    public string hpPower;
    public int itemCount;

    public override string ToString()
    {
        return
        "item : \n" +
        $"| chartFileName : {chartFileName}\n" +
        $"| itemID : {itemID}\n" +
        $"| itemName : {itemName}\n" +
        $"| itemCount : {itemCount}\n" +
        $"| hpPower : {hpPower}\n";
    }
}

[Serializable]
public class UPostMailData
{
    public PostType postType;

    public string title;
    public string content;

    public DateTime expirationDate;
    public DateTime reservationDate;
    public DateTime sentDate;

    public string nickname;
    public string inDate;

    public string author;
    public string rankType;

    public List<UPostChartItem> items =
        new List<UPostChartItem>();

    public override string ToString()
    {
        string totalString =
            $"title : {title}\n" +
            $"inDate : {inDate}\n";

        if (postType == PostType.Admin ||
            postType == PostType.Rank)
        {
            totalString +=
                $"content : {content}\n" +
                $"expirationDate : {expirationDate}\n" +
                $"reservationDate : {reservationDate}\n" +
                $"sentDate : {sentDate}\n" +
                $"nickname : {nickname}\n";
        }

        return totalString;
    }
}