using UnityEngine;
using System;

public class MailUIManager : MonoBehaviour
{
    [Header("Popup")]
    public GameObject mailboxPopup;

    [Header("Mail List")]
    public Transform content;
    public GameObject emptyObject;
    public GameObject mailItemPrefab;

    public GameObject scrollRectObject;

    public MailDetailUI mailDetailUI;

    // 아이콘 관리
    [Header("Reward Icon")]
    public Sprite goldIcon;
    public Sprite gemIcon;
    public Sprite equipmentIcon;
    public Sprite recipeIcon;

    public void OpenMailbox()
    {
        Debug.Log("OpenMailbox 호출");
        Debug.Log("popup = " + mailboxPopup.name);

        mailboxPopup.SetActive(true);

        mailDetailUI.mailUIManager = this;

        RefreshMail();
    }

    public void CloseMailbox()
    {
        mailboxPopup.SetActive(false);
    }

    public void RefreshMail()
    {
        Debug.Log("RefreshMail 시작");

        UserMailManager.Instance.LoadMail();
        UPostManager.Instance.GetPostListTest();

        Debug.Log("메일 개수 : " +
        UserMailManager.Instance.MailList.Count);

        int totalCount = UserMailManager.Instance.MailList.Count +UPostManager.Instance.MailList.Count;

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (totalCount == 0)
        {
            emptyObject.SetActive(true);
            scrollRectObject.SetActive(false);
            return;
        }

        emptyObject.SetActive(false);
        scrollRectObject.SetActive(true);

        foreach (UserMailData mail in UserMailManager.Instance.MailList)
        {
            Debug.Log("생성할 우편 : " + mail.title);

            GameObject obj =
                Instantiate(mailItemPrefab, content);

            MailItemUI item =
                obj.GetComponent<MailItemUI>();
            
            item.mailUIManager = this;

            item.mailDetailUI = mailDetailUI;

            string rewardText = "";

            if (mail.rewards != null &&
                mail.rewards.Count > 0)
            {
                RewardData reward =
                    mail.rewards[0];

                rewardText =
                    reward.id +
                    " x" +
                    reward.count;
            }

            item.SetData(
                mail,
                rewardText,
                GetRemainTime(mail.expireDate));
        }

        foreach (UPostMailData mail in UPostManager.Instance.MailList)
        {
            GameObject obj = Instantiate(mailItemPrefab, content);

            MailItemUI item = obj.GetComponent<MailItemUI>();

            item.mailUIManager = this;

            item.mailDetailUI = mailDetailUI;


            string rewardText = "";

            if (mail.items != null && mail.items.Count > 0)
            {
                rewardText = mail.items[0].itemName +" x" + mail.items[0].itemCount;
            }

            item.SetData( mail, rewardText, mail.expirationDate.ToString("yyyy-MM-dd"));

        }
    }  

    public void RemoveMail(GameObject mailObject)
    {
        Destroy(mailObject);
        Invoke(nameof(CheckEmpty), 0f);
    }

    public void CheckEmpty()
    {
        // 메일 삭제용 check empty
        bool isEmpty = content.childCount <= 0;

        emptyObject.SetActive(isEmpty);
        scrollRectObject.SetActive(!isEmpty);

    }

    public string GetRemainTime(DateTime expire)
    {
        TimeSpan remain = expire - DateTime.UtcNow;

        if (remain.TotalSeconds <= 0)
            return "만료";

        if (remain.TotalDays >= 1)
            return $"{(int)remain.TotalDays}일 남음";

        if (remain.TotalHours >= 1)
            return $"{(int)remain.TotalHours}시간 남음";

        if (remain.TotalMinutes >= 1)
            return $"{(int)remain.TotalMinutes}분 남음";

        return $"{(int)remain.TotalSeconds}초 남음";
    }
    
    public Sprite GetRewardIcon(string rewardType)
    {
        switch (rewardType)
        {
            case "Gold":
                return goldIcon;

            case "Gem":
                return gemIcon;

            case "Equipment":
                return equipmentIcon;

            case "Recipe":
                return recipeIcon;
        }

        return null;
    }
}