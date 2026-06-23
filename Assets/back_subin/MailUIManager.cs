using UnityEngine;

public class MailUIManager : MonoBehaviour
{
    [Header("Popup")]
    public GameObject mailboxPopup;

    [Header("Mail List")]
    public Transform content;
    public GameObject emptyObject;
    public GameObject mailItemPrefab;

    public GameObject scrollRectObject;

    public void OpenMailbox()
    {
        Debug.Log("OpenMailbox 호출");
        Debug.Log("popup = " + mailboxPopup.name);

        mailboxPopup.SetActive(true);

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

        Debug.Log("메일 개수 : " +
        UserMailManager.Instance.MailList.Count);

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        if (UserMailManager.Instance.MailList.Count == 0)
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
                "만료일 미구현");
        }
    }  
}