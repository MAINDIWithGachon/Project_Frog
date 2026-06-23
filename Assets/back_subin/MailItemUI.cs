using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailItemUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text rewardText;
    public TMP_Text timerText;

    public Image iconImage;

    public Button receiveButton;

    public GameObject stamp;

    private UserMailData currentMail;

    public Sprite goldIcon;

    public Sprite gemIcon;

    public Sprite equipmentIcon;

    public Sprite recipeIcon;

    public void SetData(UserMailData mail, string reward,string expire)
    {
        currentMail = mail;

        titleText.text = mail.title;
        rewardText.text = reward;
        timerText.text = expire;
        if(mail.rewards != null &&mail.rewards.Count > 0)
        {
            SetRewardIcon(
            mail.rewards[0].type);
        }

        stamp.SetActive(mail.isReceived);

        receiveButton.gameObject.SetActive(!mail.isReceived);

        receiveButton.onClick.RemoveAllListeners();
        receiveButton.onClick.AddListener(OnClickReceive);
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

        stamp.SetActive(true);
    }
    private void SetRewardIcon(string rewardType)
    {
        switch (rewardType)
        {
            case "Gold":
                iconImage.sprite = goldIcon;
                break;

            case "Gem":
                iconImage.sprite = gemIcon;
                break;

            case "Equipment":
                iconImage.sprite = equipmentIcon;
                break;

            case "Recipe":
                iconImage.sprite = recipeIcon;
                break;
        }
    }
}