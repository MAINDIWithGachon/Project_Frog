using UnityEngine;

public class MasterDetailPanelController : MonoBehaviour
{
    // 메인 목록 패널
    [SerializeField] protected GameObject masterPanel;

    // 상세 정보 패널
    [SerializeField] protected GameObject detailPanel;

    // 상세창을 닫을 때 메인 패널을 다시 보여줄지 여부
    [SerializeField] protected bool restoreMasterOnClose = false;

    public virtual void OpenDetailPanel()
    {
        // 상세창을 열 때 메인 패널은 숨긴다.
        if (masterPanel != null)
            masterPanel.SetActive(false);

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    public virtual void CloseDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        // 옵션이 켜져 있을 때만 메인 패널을 복구한다.
        if (restoreMasterOnClose && masterPanel != null)
            masterPanel.SetActive(true);
    }

    public virtual void CloseAllPanels()
    {
        // 현재 UI 흐름을 완전히 닫을 때 사용한다.
        if (detailPanel != null)
            detailPanel.SetActive(false);

        if (masterPanel != null)
            masterPanel.SetActive(false);
    }
}
