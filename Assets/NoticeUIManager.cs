using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class NoticeUIManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private NoticeManager noticeManager; // 백엔드 연결 매니저

    [Header("Popups")]
    [SerializeField] private GameObject noticeListPopup;   // 리스트 팝업창
    [SerializeField] private GameObject noticeDetailPopup; // 상세 보기 팝업창

    [Header("List View Settings")]
    [SerializeField] private Transform listContentParent;  // 스크롤뷰의 Content 부모
    [SerializeField] private GameObject noticeItemPrefab;  // 공지 아이템 프리팹

    [Header("Detail UI Elements (Text)")]
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailContentText;
    [SerializeField] private TMP_Text detailDateText;
    [SerializeField] private TMP_Text detailAuthorText;

    [Header("Detail UI Elements (Media & Link)")]
    [SerializeField] private RawImage detailRawImage;      // 사진 표시용 UI
    [SerializeField] private Button detailLinkButton;      // 링크 이동 버튼

    private List<GameObject> spawnedItems = new List<GameObject>();
    private string currentLinkUrl = ""; // 현재 열린 공지의 링크 주소 임시 저장

    private void Start()
    {
        // 게임 시작 시 두 팝업창은 기본적으로 꺼둡니다.
        if (noticeListPopup != null) noticeListPopup.SetActive(false);
        if (noticeDetailPopup != null) noticeDetailPopup.SetActive(false);
    }

    // [로비의 공지사항 버튼 클릭 시 호출]
    public void OnClickNoticeButton()
    {
        Debug.Log("공지사항 버튼 클릭됨! 서버에 요청을 보냅니다.");

        if (noticeListPopup != null)
        {
            noticeListPopup.SetActive(true);
        }

        // 1. 첫 번째 공지 리스트 데이터 로드 시도
        noticeManager.LoadFirstNoticeList(() =>
        {
            // 2. 이어서 유저의 공지 읽음 기록 테이블 데이터 로드
            noticeManager.LoadReadNoticeList(() =>
            {
                // 3. 두 서버 응답이 모두 완료되면 UI 리스트를 갱신
                RefreshNoticeList();
                Debug.Log("서버 데이터 로드 완료 및 UI 갱신 성공!");
            });
        });
    }

    // [공지사항 리스트 UI 갱신]
    private void RefreshNoticeList()
    {
        // 기존에 생성되어 있던 리스트 아이템들 청소
        foreach (var item in spawnedItems)
        {
            Destroy(item);
        }
        spawnedItems.Clear();

        // 🌟 [교정] 확실하게 파악된 NoticeData 타입으로 리스트를 가져옵니다.
        List<NoticeManager.NoticeData> noticeList = noticeManager.GetNoticeList();

        for (int i = 0; i < noticeList.Count; i++)
        {
            GameObject itemObj = Instantiate(noticeItemPrefab, listContentParent);
            spawnedItems.Add(itemObj);

            // UI 스케일이 0으로 압축되는 유니티 자체 버그 방지
            itemObj.transform.localScale = Vector3.one;

            NoticeItem itemScript = itemObj.GetComponent<NoticeItem>();
            if (itemScript != null)
            {
                // 공지의 고유 ID 추출
                string realNoticeId = noticeList[i].uuid;
                
                // 읽은 적이 있는 공지인지 체크
                bool isAlreadyRead = noticeManager.IsNoticeRead(realNoticeId);

                // 최신 NoticeItem 스펙(6개 인자)에 맞춰 빈틈없이 토스합니다.
                itemScript.Setup(i, realNoticeId, noticeList[i].title, noticeList[i].postingDate, isAlreadyRead, ShowDetailNotice);
            }
        }
    }

    // [특정 공지사항 클릭 시 상세 팝업 열기]
    private void ShowDetailNotice(int index)
    {
        // 🌟 [교정] 여기도 NoticeData 타입으로 일치시켰습니다.
        List<NoticeManager.NoticeData> noticeList = noticeManager.GetNoticeList();
        if (index < 0 || index >= noticeList.Count) return;

        NoticeManager.NoticeData selectedNotice = noticeList[index];

        // 1. 기본 텍스트 데이터 반영
        detailTitleText.text = selectedNotice.title;
        detailContentText.text = selectedNotice.contents;
        detailDateText.text = selectedNotice.postingDate.ToString("yyyy-MM-dd HH:mm");
        
        if (detailAuthorText != null)
            detailAuthorText.text = "작성자: " + selectedNotice.author;

        // 2. 이미지 처리
        if (detailRawImage != null)
        {
            if (!string.IsNullOrEmpty(selectedNotice.imageKey))
            {
                detailRawImage.gameObject.SetActive(true);
                StartCoroutine(DownloadImage(selectedNotice.imageKey));
            }
            else
            {
                detailRawImage.gameObject.SetActive(false);
            }
        }

        // 3. 링크 버튼 처리
        if (detailLinkButton != null)
        {
            if (!string.IsNullOrEmpty(selectedNotice.linkUrl))
            {
                detailLinkButton.gameObject.SetActive(true);
                currentLinkUrl = selectedNotice.linkUrl;

                TMP_Text buttonText = detailLinkButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null && !string.IsNullOrEmpty(selectedNotice.linkButtonName))
                {
                    buttonText.text = selectedNotice.linkButtonName;
                }

                detailLinkButton.onClick.RemoveAllListeners();
                detailLinkButton.onClick.AddListener(OnClickedLinkButton);
            }
            else
            {
                detailLinkButton.gameObject.SetActive(false);
            }
        }

        // =================================================================
        // 🌟 [실시간 NEW 배지 끄기 및 로컬 저장 핵심 로직 추가]
        // =================================================================
        
        // 1) 로컬 메모리(HashSet)에 읽음 상태 등록 (추후 일괄 백업 대기)
        string realNoticeId = selectedNotice.uuid;
        noticeManager.SaveNoticeAsRead(realNoticeId);

        // 2) 내가 방금 클릭한 리스트 아이템 오브젝트 가져오기
        if (index < spawnedItems.Count && spawnedItems[index] != null)
        {
            // 해당 아이템 자식 중 "Badge" (또는 세팅하신 NEW 배지 오브젝트 이름)를 검색
            Transform badgeTransform = spawnedItems[index].transform.Find("NewBadge");
            if (badgeTransform != null)
            {
                // 실시간으로 눈앞에서 배지만 슥 비활성화!
                badgeTransform.gameObject.SetActive(false);
                Debug.Log($"[UI] 인덱스 {index}번 공지의 NEW 배지를 실시간으로 껐습니다.");
            }
            else
            {
                Debug.LogWarning("NoticeItem 프리팹 내부에서 'Badge'라는 이름의 오브젝트를 찾을 수 없습니다. 인스펙터 이름을 확인하세요.");
            }
        }

        // =================================================================

        // 상세 팝업창 활성화
        noticeDetailPopup.SetActive(true);
    }
    // 웹 URL로부터 이미지를 받아오는 코루틴 함수
    private IEnumerator DownloadImage(string url)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                if (detailRawImage != null)
                {
                    detailRawImage.texture = texture;
                }
            }
            else
            {
                Debug.LogError("공지 이미지 로드 실패: " + request.error);
                if (detailRawImage != null) detailRawImage.gameObject.SetActive(false);
            }
        }
    }

    // 링크 버튼 클릭 시 기기의 웹 브라우저를 열어주는 함수
    private void OnClickedLinkButton()
    {
        if (!string.IsNullOrEmpty(currentLinkUrl))
        {
            Application.OpenURL(currentLinkUrl);
        }
    }

    // [상세 팝업 닫기 버튼용]
    public void CloseNoticeDetailPopup()
    {
        noticeDetailPopup.SetActive(false);
    }

    // [리스트 팝업 닫기 버튼용]
    public void CloseNoticeListPopup()
    {

        if (noticeManager != null)
        {
            noticeManager.SaveNoticeAsReadToServer();
        }
        
        noticeListPopup.SetActive(false);
    }
}