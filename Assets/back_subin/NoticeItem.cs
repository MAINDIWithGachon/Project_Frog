using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NoticeItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private Button clickButton;
    [SerializeField] private GameObject newBadgeObject;

    private int myIndex; // NoticeManager의 List에서 내가 몇 번째 데이터인지 저장
    private string noticeId;
    private Action<int> onClickCallback; // 클릭 시 부모(매니저)에게 인덱스를 던질 콜백

    // 🌟 [완벽 해결] NoticeUIManager가 던져주는 6개의 인자를 모두 받도록 매개변수를 교정했습니다!
    public void Setup(int index, string id, string title, DateTime date, bool isRead, Action<int> onClick)
    {
        myIndex = index;
        noticeId = id; // 👈 괄호에서 받아온 진짜 id를 매핑합니다.
        titleText.text = title;
        dateText.text = date.ToString("yyyy-MM-dd");
        onClickCallback = onClick;

        // 🌟 [완벽 해결] 괄호에서 받아온 읽음 여부(isRead) 상태에 따라 배지를 켜고 끕니다.
        if (isRead == false)
        {
            if (newBadgeObject != null) newBadgeObject.SetActive(true);
        }
        else
        {
            if (newBadgeObject != null) newBadgeObject.SetActive(false);
        }

        // 버튼 이벤트 바인딩 (기존 리스너 청소 후 등록)
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        //최신 함수인 FindFirstObjectByType으로 교체하여 경고를 지웁니다!
        NoticeManager noticeManager = FindFirstObjectByType<NoticeManager>();
        
        if (noticeManager != null)
        {
            noticeManager.SaveNoticeAsRead(noticeId);
        }

        if (newBadgeObject != null) newBadgeObject.SetActive(false);
        onClickCallback?.Invoke(myIndex);
    }
}