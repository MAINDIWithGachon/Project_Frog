using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class NoticeItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private Button clickButton;

    private int myIndex; // 리스트에서 내 아이템의 순서 번호
    private Action<int> onClickCallback; // 클릭했을 때 부모 UI 매니저에게 알릴 콜백

    // 🌟 NoticeUIManager에서 호출하는 5개짜리 인자 버전의 Setup 함수입니다.
    public void Setup(int index, string id, string title, DateTime date, Action<int> onClick)
    {
        myIndex = index;
        titleText.text = title;
        dateText.text = date.ToString("yyyy-MM-dd");
        onClickCallback = onClick;

        // 버튼 이벤트가 중복 등록되지 않도록 초기화 후 다시 연결합니다.
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // 클릭 시 등록된 UI 매니저의 상세보기 함수를 실행시킵니다.
        onClickCallback?.Invoke(myIndex);
    }
}