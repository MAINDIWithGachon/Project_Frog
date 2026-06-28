using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BackEnd;

public class MailItemUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text rewardText;
    public TMP_Text timerText;

    public Image iconImage;

    public Button receiveButton;

    private UserMailData currentMail;

    private UPostMailData currentUPostMail;

    public MailUIManager mailUIManager;

    public MailDetailUI mailDetailUI;

    public Button openDetailButton;

    public void SetData(UserMailData mail, string reward,string expire)
    {
        currentMail = mail;

        titleText.text = mail.title;
        rewardText.text = reward;
        timerText.text = expire;
        if(mail.rewards != null &&mail.rewards.Count > 0)
        {
            iconImage.sprite = mailUIManager.GetRewardIcon(mail.rewards[0].type);
        }

        receiveButton.gameObject.SetActive(!mail.isReceived);

        receiveButton.onClick.RemoveAllListeners();
        receiveButton.onClick.AddListener(OnClickReceive);

        openDetailButton.onClick.RemoveAllListeners();
        openDetailButton.onClick.AddListener(OnClickMail);
        Debug.Log("버튼 연결 완료");
    } 

    public void SetData( UPostMailData mail,string reward,string expire)
    {
        currentUPostMail = mail;

        titleText.text = mail.title;
        rewardText.text = reward;
        timerText.text = expire;

        if (mail.items != null && mail.items.Count > 0)
        {
            iconImage.sprite = mailUIManager.GetRewardIcon(mail.items[0].itemName);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        receiveButton.gameObject.SetActive(true);

        receiveButton.onClick.RemoveAllListeners();
        receiveButton.onClick.AddListener(OnClickReceiveUPost);

        openDetailButton.onClick.RemoveAllListeners();
        openDetailButton.onClick.AddListener(OnClickUPostMail);
    }

    private void OnClickReceive()
    {
        bool success =
            UserMailManager.Instance.ReceiveMail(
                currentMail);

        if (!success)
        {
            return;
        }

        FindFirstObjectByType<MailUIManager>().RemoveMail(gameObject);
    }

    private void OnClickReceiveUPost()
    {
        BackendReturnObject bro =
            Backend.UPost.ReceivePostItem(
                currentUPostMail.postType,
                currentUPostMail.inDate);

        if (!bro.IsSuccess())
        {
            Debug.LogError(
                "우편 수령 실패 : " + bro);

            return;
        }
    
        Debug.Log(
            "우편 수령 성공\n" +
            "제목 : " + currentUPostMail.title);

        FindFirstObjectByType<MailUIManager>()
            .RefreshMail();
    }

    public void OnClickMail()
    {
       Debug.Log("메일 클릭");

        mailDetailUI.gameObject.SetActive(true);

        mailDetailUI.SetData(currentMail);
    }

    public void OnClickUPostMail()
    {
        Debug.Log("UPost 메일 클릭");
        mailDetailUI.gameObject.SetActive(true);
        mailDetailUI.SetData(currentUPostMail);
    }
}