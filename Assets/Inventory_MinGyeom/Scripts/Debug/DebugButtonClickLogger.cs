using UnityEngine;

/// <summary>
/// 버튼 클릭 이벤트가 제대로 연결되었는지 인스펙터에서 쉽게 확인할 수 있도록 로그를 남기는 보조 컴포넌트입니다.
/// </summary>
public class DebugButtonClickLogger : MonoBehaviour
{
    // 버튼마다 다른 문구를 넣어두면 어떤 UI에서 호출됐는지 로그만 보고도 구분할 수 있습니다.
    [SerializeField] private string logMessage = "Button clicked.";

    public void LogClick()
    {
        Debug.Log($"[DebugButtonClickLogger] {logMessage}", this);
    }
}
