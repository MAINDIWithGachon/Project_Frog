using UnityEngine;

public class SkillItemButton : MonoBehaviour
{
    // 이 버튼이 대표하는 스킬 ID
    [SerializeField] private int skillId;

    // 스킬 ID 기준으로 데이터 조회를 담당하는 매니저
    [SerializeField] private SkillManager skillManager;

    // 버튼에 직접 넣어두는 상세 정보.
    // SkillManager 조회가 안 될 때 보조 데이터로 사용한다.
    public SkillDetailData skillData;

    // 하단 스킬 버튼 클릭 시 열릴 상세창 UI
    public SkillDetailPanelUI detailPanelUI;

    private void Awake()
    {
        // 인스펙터 연결이 비어 있으면 씬에서 자동으로 찾아온다.
        if (detailPanelUI == null)
            detailPanelUI = FindFirstObjectByType<SkillDetailPanelUI>(FindObjectsInactive.Include);

        // 상세창 UI 컴포넌트가 없으면 Character_Skill_Detail에 최소 컴포넌트를 붙여서 사용한다.
        if (detailPanelUI == null)
            detailPanelUI = EnsureDetailPanelUI();

        if (skillManager == null)
            skillManager = FindFirstObjectByType<SkillManager>(FindObjectsInactive.Include);

        // 버튼에 ID가 비어 있으면 현재 아이콘/이름 기준으로 한 번 자동 추정한다.
        TryAutoResolveSkillId();
    }

    public void OnClickSkill()
    {
        if (detailPanelUI == null)
        {
            Debug.LogWarning("[SkillItemButton] SkillDetailPanelUI reference is missing.");
            return;
        }

        // 우선 SkillManager에서 ID 기반 상세 정보를 받아오고,
        // 없으면 버튼에 직접 저장된 skillData를 사용한다.
        SkillDetailData detailData = GetDetailData();
        if (detailData == null)
        {
            Debug.LogWarning($"[SkillItemButton] Skill data is missing on {name}.");
            return;
        }

        // 이후 다른 UI에서도 같은 상세 정보를 재사용할 수 있도록 매니저에 등록한다.
        if (skillManager != null)
            skillManager.RegisterSkillDetailDataForUI(detailData);

        // 선택한 스킬 정보를 상세창에 전달한다.
        detailPanelUI.Open(detailData);
    }

    private SkillDetailData GetDetailData()
    {
        // 가장 먼저 SkillManager의 ID 기반 데이터 조회를 시도한다.
        if (skillManager != null && skillId > 0)
        {
            SkillDetailData managerData = skillManager.GetSkillDetailDataForUI(skillId);
            if (managerData != null)
                return managerData;
        }

        // 매니저 데이터가 없으면 버튼에 직접 넣은 데이터를 사용한다.
        if (skillData != null && skillId > 0)
        {
            skillData.skillId = skillId;
            if (skillManager != null)
                skillManager.RegisterSkillDetailDataForUI(skillData);
        }

        return skillData;
    }

    private void TryAutoResolveSkillId()
    {
        if (skillId > 0 || skillManager == null || skillData == null)
            return;

        // 스프라이트나 이름이 일치하는 스킬이 있으면 해당 ID를 버튼에 세팅한다.
        if (skillManager.TryResolveSkillIdForUI(skillData.icon, skillData.skillName, out int resolvedSkillId))
            skillId = resolvedSkillId;
    }

    private SkillDetailPanelUI EnsureDetailPanelUI()
    {
        // Character_Skill_Detail 오브젝트를 찾아 상세창 UI 스크립트를 보장한다.
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != "Character_Skill_Detail")
                continue;

            SkillDetailPanelUI panelUI = transforms[i].GetComponent<SkillDetailPanelUI>();
            if (panelUI != null)
                return panelUI;

            // 스크립트가 없으면 최소 기능 버전을 붙여서 사용한다.
            panelUI = transforms[i].gameObject.AddComponent<SkillDetailPanelUI>();
            panelUI.panel = transforms[i].gameObject;
            return panelUI;
        }

        return null;
    }
}
