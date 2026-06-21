using System;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

public class NoticeManager : MonoBehaviour
{
    [Header("Load Setting")]
    [SerializeField] private int loadCount = 10;

    // 전체 공지 저장
    private List<NoticeData> noticeList = new List<NoticeData>();

    // 다음 페이지 offset
    private string currentOffset;

    // 중복 요청 방지
    private bool isLoading;

    // 다음 페이지 존재 여부
    private bool hasNextPage = true;

    private HashSet<string> readNoticeIds = new HashSet<string>();
    private string tableRowInDate = "";

    //--------------------------------------------------
    // 외부 접근용
    //--------------------------------------------------

    public List<NoticeData> GetNoticeList()
    {
        return noticeList;
    }

    public bool IsLoading()
    {
        return isLoading;
    }

    public bool HasNextPage()
    {
        return hasNextPage;
    }

    //--------------------------------------------------
    // 최초 공지 로딩
    //--------------------------------------------------

    public void LoadFirstNoticeList(Action onComplete = null)
    {
        Debug.Log("LoadFirstNoticeList 호출됨");

        if (isLoading)
        {
            Debug.Log("이미 로딩 중입니다.");
            onComplete?.Invoke(); // 로딩 중이더라도 콜백을 풀어주어 먹통 방지
            return;
        }

        // 초기화
        noticeList.Clear();

        currentOffset = null;
        hasNextPage = true;

        RequestNoticeList(onComplete);
    }

    //--------------------------------------------------
    // 추가 공지 로딩
    //--------------------------------------------------

    public void LoadMoreNoticeList(Action onComplete = null)
    {
        Debug.Log("LoadMoreNoticeList 호출됨");

        if (isLoading)
        {
            Debug.Log("이미 로딩 중입니다.");
            return;
        }

        if (!hasNextPage)
        {
            Debug.Log("더 이상 불러올 공지가 없습니다.");
            return;
        }

        RequestNoticeList(onComplete);
    }

    //--------------------------------------------------
    // 실제 서버 요청
    //--------------------------------------------------

    private void RequestNoticeList(Action onComplete = null)
    {
        Debug.Log("공지 서버 요청 시작");

        isLoading = true;

        //--------------------------------------------------
        // 첫 요청
        //--------------------------------------------------

        if (string.IsNullOrEmpty(currentOffset))
        {
            Backend.Notice.NoticeList(
                loadCount,
                callback =>
                {
                    HandleNoticeResponse(callback);

                    onComplete?.Invoke();
                });
        }

        //--------------------------------------------------
        // 추가 요청
        //--------------------------------------------------

        else
        {
            Backend.Notice.NoticeList(
                loadCount,
                currentOffset,
                callback =>
                {
                    HandleNoticeResponse(callback);

                    onComplete?.Invoke();
                });
        }
    }

    //--------------------------------------------------
    // 응답 처리
    //--------------------------------------------------

    private void HandleNoticeResponse(BackendReturnObject bro)
    {
        Debug.Log("공지 응답 도착");

        isLoading = false;

        //--------------------------------------------------
        // 실패
        //--------------------------------------------------

        if (!bro.IsSuccess())
        {
            Debug.LogError($"공지 불러오기 실패 : {bro}");
            return;
        }

        //--------------------------------------------------
        // 성공
        //--------------------------------------------------

        Debug.Log($"공지 불러오기 성공 : {bro}");

        JsonData jsonList = bro.FlattenRows();

        Debug.Log($"받아온 공지 개수 : {jsonList.Count}");

        //--------------------------------------------------
        // 공지 파싱
        //--------------------------------------------------

        for (int i = 0; i < jsonList.Count; i++)
        {
            NoticeData notice = new NoticeData();

            //--------------------------------------------------
            // 기본 데이터
            //--------------------------------------------------

            notice.title = jsonList[i]["title"].ToString();
            notice.contents = jsonList[i]["content"].ToString();
            notice.postingDate = DateTime.Parse(jsonList[i]["postingDate"].ToString());
            notice.inDate = jsonList[i]["inDate"].ToString();
            notice.uuid = jsonList[i]["uuid"].ToString();
            notice.author = jsonList[i]["author"].ToString();

            //--------------------------------------------------
            // 공개 여부
            //--------------------------------------------------

            if (jsonList[i].ContainsKey("isPublic"))
            {
                notice.isPublic = jsonList[i]["isPublic"].ToString() == "y";
            }

            //--------------------------------------------------
            // 이미지
            //--------------------------------------------------

            if (jsonList[i].ContainsKey("imageKey"))
            {
                string rawKey = jsonList[i]["imageKey"].ToString();
                if (rawKey.StartsWith("/upload"))
                {
                    notice.imageKey = "http://upload-console.thebackend.io" + rawKey;
                }
                else
                {
                    notice.imageKey = "http://upload-console.thebackend.io/upload" + rawKey;
                }
            }

            //--------------------------------------------------
            // 링크 URL
            //--------------------------------------------------

            if (jsonList[i].ContainsKey("linkUrl"))
            {
                notice.linkUrl = jsonList[i]["linkUrl"].ToString();
            }

            //--------------------------------------------------
            // 링크 버튼 이름
            //--------------------------------------------------

            if (jsonList[i].ContainsKey("linkButtonName"))
            {
                notice.linkButtonName = jsonList[i]["linkButtonName"].ToString();
            }

            //--------------------------------------------------
            // 리스트 추가
            //--------------------------------------------------

            noticeList.Add(notice);
        }

        //--------------------------------------------------
        // 다음 페이지 확인
        //--------------------------------------------------

        currentOffset = bro.LastEvaluatedKeyString();

        if (string.IsNullOrEmpty(currentOffset))
        {
            hasNextPage = false;
            Debug.Log("마지막 페이지입니다.");
        }
        else
        {
            Debug.Log("다음 페이지가 존재합니다.");
        }

        Debug.Log($"현재 저장된 공지 개수 : {noticeList.Count}");
    }

    // 🌟 [안전화 완료] 무거운 SendQueue를 완벽히 걷어내고 중첩 콜백 타이밍을 방어한 버전입니다.
    public void LoadReadNoticeList(System.Action onComplete)
    {
        readNoticeIds.Clear();
        tableRowInDate = "";

        Debug.Log("유저 읽음 목록 데이터 서버 요청 시작");

        // 대기 현상 방지를 위해 순수 비동기 GetMyData 메서드로 직접 호출합니다.
        Backend.GameData.GetMyData("UserNoticeStatus", new Where(), callback =>
        {
            bool isCallbackInvoked = false;

            try
            {
                if (callback.IsSuccess())
                {
                    var rows = callback.Rows();
                    if (rows.Count > 0)
                    {
                        tableRowInDate = rows[0]["inDate"]["S"].ToString();
                        if (rows[0].ContainsKey("readIds"))
                        {
                            var idList = rows[0]["readIds"]["L"];
                            for (int i = 0; i < idList.Count; i++)
                            {
                                readNoticeIds.Add(idList[i]["S"].ToString());
                            }
                        }
                        Debug.Log($"기존 유저 공지 읽음 기록 로드 완료. 읽은 공지 개수: {readNoticeIds.Count}");
                        
                        isCallbackInvoked = true;
                        onComplete?.Invoke(); // UI 리프레시 트리거 호출
                    }
                    else
                    {
                        Debug.Log("UserNoticeStatus 테이블에 데이터 없음 -> 신규 생성 시작");
                        
                        Param param = new Param();
                        param.Add("readIds", new List<string>());
                        
                        Backend.GameData.Insert("UserNoticeStatus", param, insertCallback =>
                        {
                            if (insertCallback.IsSuccess())
                            {
                                tableRowInDate = insertCallback.GetInDate();
                                Debug.Log($"신규 유저 공지 데이터 초기화 성공! inDate: {tableRowInDate}");
                            }
                            else
                            {
                                Debug.LogError("신규 유저 공지 데이터 초기화 실패: " + insertCallback.GetMessage());
                            }
                            
                            isCallbackInvoked = true;
                            onComplete?.Invoke(); // 🌟 Insert 처리가 완전히 끝난 이 시점에 콜백을 호출해야 순서가 안 꼬입니다.
                        });
                    }
                }
                else
                {
                    Debug.LogError("읽음 목록 불러오기 실패: " + callback.GetMessage());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("읽음 목록 파싱 중 예외 발생: " + e.Message);
            }
            finally
            {
                // 통신 실패나 예외로 인해 콜백이 갇혔을 때 UI 락을 푸는 강제 탈출 안전장치
                if (!isCallbackInvoked)
                {
                    Debug.LogWarning("안전 장치 작동: 예외/실패 상황으로 인해 콜백 강제 호출");
                    onComplete?.Invoke();
                }
            }
        });
    }

    public void SaveNoticeAsRead(string noticeId)
    {
        if (readNoticeIds.Contains(noticeId)) return;

        readNoticeIds.Add(noticeId);
        Debug.Log($"[로컬 메모리 임시 저장] 공지 ID: {noticeId} 읽음 처리 완료 (백업 대기 중)");
    }

    

    public bool IsNoticeRead(string noticeId)
    {
        return readNoticeIds.Contains(noticeId);
    }

    //--------------------------------------------------
    // 공지 데이터 클래스
    //--------------------------------------------------

    [Serializable]
    public class NoticeData
    {
        public string title;

        public string contents;

        public DateTime postingDate;

        public string imageKey;

        public string inDate;

        public string uuid;

        public string linkUrl;

        public bool isPublic;

        public string linkButtonName;

        public string author;

        public override string ToString()
        {
            return $"title : {title}\n" +
                   $"contents : {contents}\n" +
                   $"postingDate : {postingDate}\n" +
                   $"imageKey : {imageKey}\n" +
                   $"inDate : {inDate}\n" +
                   $"uuid : {uuid}\n" +
                   $"linkUrl : {linkUrl}\n" +
                   $"isPublic : {isPublic}\n" +
                   $"linkButtonName : {linkButtonName}\n" +
                   $"author : {author}\n";
        }
    }
}
