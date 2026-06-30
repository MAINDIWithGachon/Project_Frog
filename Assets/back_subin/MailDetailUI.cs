using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MailDetailUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text contentText;

    public Image rewardIcon;

    public Button receiveButton;
    public Button closeButton;

    public MailUIManager mailUIManager;

    public TMP_Text remainTimeText;

    private UserMailData currentMail;

    private void Awake()
    {
        Debug.Log("Awake");

        closeButton.onClick.AddListener(ClosePopup);
        receiveButton.onClick.AddListener(OnClickReceive);

    }

    public void SetData(UserMailData mail)
    {
        Debug.Log("SetData 호출");

        currentMail = mail;

        titleText.text = mail.title;
        contentText.text = mail.content;
        remainTimeText.text = mailUIManager.GetRemainTime(mail.expireDate);

        if (mail.rewards != null && mail.rewards.Count > 0)
        {
            rewardIcon.gameObject.SetActive(true);
            rewardIcon.sprite =
                mailUIManager.GetRewardIcon(mail.rewards[0].type);
        }
        else
        {
            rewardIcon.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void SetData(UPostMailData mail)
    {
        titleText.text = mail.title;
        contentText.text = mail.content;
        remainTimeText.text = mailUIManager.GetRemainTime(mail.expirationDate);

        if (mail.items != null && mail.items.Count > 0)
        {
            rewardIcon.gameObject.SetActive(true);
            rewardIcon.sprite = 
                mailUIManager.GetRewardIcon(mail.items[0].itemName);
        }
        else
        {
            rewardIcon.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    private void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    private void OnClickReceive()
    {
        Debug.Log("세부 팝업 받기 버튼");
    }
}