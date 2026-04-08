using UnityEngine;

public class SkillDetailUIOpener : MasterDetailPanelController
{
    // 스킬 상세창 오브젝트 참조
    [SerializeField] private GameObject skillDetailPanel;

    private void Awake()
    {
        // 상속받은 detailPanel 필드와 인스펙터 값을 맞춰준다.
        if (detailPanel == null)
            detailPanel = skillDetailPanel;
    }

    private void OnValidate()
    {
        // 에디터에서 값이 바뀔 때도 동일하게 동기화한다.
        if (skillDetailPanel != null)
            detailPanel = skillDetailPanel;
    }

    public void OpenSkillDetail()
    {
        // 스킬 상세창 열기
        OpenDetailPanel();
    }

    public void CloseSkillDetail()
    {
        // 현재 스킬 UI 흐름 전체 닫기
        CloseAllPanels();
    }
}
