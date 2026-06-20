using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class NoticeUIManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private NoticeManager noticeManager; 

    [Header("Popups")]
    [SerializeField] private GameObject noticeListPopup;   
    [SerializeField] private GameObject noticeDetailPopup; 

    [Header("List View Settings")]
    [SerializeField] private Transform listContentParent;  
    [SerializeField] private GameObject noticeItemPrefab;  

    [Header("Detail UI Elements (Text)")]
    [SerializeField] private TMP_Text detailTitleText;
    [SerializeField] private TMP_Text detailContentText;
    [SerializeField] private TMP_Text detailDateText;
    [SerializeField] private TMP_Text detailAuthorText;

    [Header("Detail UI Elements (Media & Link)")]
    [SerializeField] private RawImage detailRawImage;      
    [SerializeField] private Button detailLinkButton;      

    private List<GameObject> spawnedItems = new List<GameObject>();
    private string currentLinkUrl = ""; 

    private void Start()
    {
        if (noticeListPopup != null) noticeListPopup.SetActive(false);
        if (noticeDetailPopup != null) noticeDetailPopup.SetActive(false);
    }

    public void OnClickNoticeButton()
    {
        Debug.Log("공지사항 버튼 클릭됨! 서버에 요청을 보냅니다.");

        if (noticeListPopup != null)
        {
            noticeListPopup.SetActive(true);
        }

        noticeManager.LoadFirstNoticeList(() =>
        {
            RefreshNoticeList();
            Debug.Log("서버 데이터 로드 완료 및 UI 갱신 성공!");
        });
    }

    private void RefreshNoticeList()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();

        List<NoticeManager.NoticeData> noticeList = noticeManager.GetNoticeList();

        for (int i = 0; i < noticeList.Count; i++)
        {
            GameObject itemObj = Instantiate(noticeItemPrefab, listContentParent);
            spawnedItems.Add(itemObj);

            itemObj.transform.localScale = Vector3.one;

            NoticeItem itemScript = itemObj.GetComponent<NoticeItem>();
            if (itemScript != null)
            {  
                itemScript.Setup(i, noticeList[i].uuid, noticeList[i].title, noticeList[i].postingDate, ShowDetailNotice);
            }
        }

        // 🌟 [코드 교정] 목록 생성 직후 레이아웃을 강제로 즉시 재계산합니다. (스크롤 마비 방지)
        Canvas.ForceUpdateCanvases();
        if (listContentParent != null)
        {
            var layoutGroup = listContentParent.GetComponent<VerticalLayoutGroup>();
            var sizeFitter = listContentParent.GetComponent<ContentSizeFitter>();
            if (layoutGroup != null) layoutGroup.enabled = false; layoutGroup.enabled = true;
            if (sizeFitter != null) sizeFitter.enabled = false; sizeFitter.enabled = true;
        }
    }

    private void ShowDetailNotice(int index)
    {
        List<NoticeManager.NoticeData> noticeList = noticeManager.GetNoticeList();
        if (index < 0 || index >= noticeList.Count) return;

        NoticeManager.NoticeData selectedNotice = noticeList[index];

        detailTitleText.text = selectedNotice.title;
        detailContentText.text = selectedNotice.contents;
        detailDateText.text = selectedNotice.postingDate.ToString("yyyy-MM-dd HH:mm");
        
        if (detailAuthorText != null)
            detailAuthorText.text = "작성자: " + selectedNotice.author;

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

        noticeDetailPopup.SetActive(true);

        // 🌟 [코드 교정] 상세 팝업창도 켜진 직후 레이아웃을 강제로 즉시 재계산합니다. (본문 스크롤 마비 방지)
        Canvas.ForceUpdateCanvases();
        Transform detailContent = detailContentText.transform.parent; // Scroll View의 진짜 Content 자동 추적
        if (detailContent != null)
        {
            var layoutGroup = detailContent.GetComponent<VerticalLayoutGroup>();
            var sizeFitter = detailContent.GetComponent<ContentSizeFitter>();
            if (layoutGroup != null) layoutGroup.enabled = false; layoutGroup.enabled = true;
            if (sizeFitter != null) sizeFitter.enabled = false; sizeFitter.enabled = true;
        }
    }

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
                    
                    // 🌟 이미지가 다운로드 완료되어 실제 화면에 꽂힌 순간 레이아웃을 한 번 더 갱신합니다.
                    Canvas.ForceUpdateCanvases();
                    Transform detailContent = detailContentText.transform.parent;
                    if (detailContent != null)
                    {
                        var sizeFitter = detailContent.GetComponent<ContentSizeFitter>();
                        if (sizeFitter != null) { sizeFitter.enabled = false; sizeFitter.enabled = true; }
                    }
                }
            }
            else
            {
                Debug.LogError("공지 이미지 로드 실패: " + request.error);
                if (detailRawImage != null) detailRawImage.gameObject.SetActive(false);
            }
        }
    }

    private void OnClickedLinkButton()
    {
        if (!string.IsNullOrEmpty(currentLinkUrl))
        {
            Application.OpenURL(currentLinkUrl);
        }
    }

    public void CloseNoticeDetailPopup()
    {
        noticeDetailPopup.SetActive(false);
        Debug.Log("Content Height : " + ((RectTransform)detailContentText.transform.parent).rect.height);
    }

    public void CloseNoticeListPopup()
    {
        noticeListPopup.SetActive(false);
    }
}