// Unity 기본 기능 사용
using UnityEngine;

// Unity UI Button 사용
using UnityEngine.UI;

// TextMeshPro 텍스트 사용
using TMPro;

// 강화 UI를 제어하는 클래스입니다.
public class StatUpgradeUIController : MonoBehaviour
{
    // 공격력 강화 버튼입니다.
    [SerializeField] private Button attackUpgradeButton;

    // 체력 강화 버튼입니다.
    [SerializeField] private Button hpUpgradeButton;

    // 초당 체력 회복 강화 버튼입니다.
    [SerializeField] private Button hpRegenUpgradeButton;

    // 치명타 확률 강화 버튼입니다.
    [SerializeField] private Button critRateUpgradeButton;

    // 치명타 공격력 강화 버튼입니다.
    [SerializeField] private Button critDamageUpgradeButton;

    // 공격력 강화 레벨 표시 텍스트입니다.
    [SerializeField] private TextMeshProUGUI attackLevelText;

    // 체력 강화 레벨 표시 텍스트입니다.
    [SerializeField] private TextMeshProUGUI hpLevelText;

    // 초당 체력 회복 강화 레벨 표시 텍스트입니다.
    [SerializeField] private TextMeshProUGUI hpRegenLevelText;

    // 치명타 확률 강화 레벨 표시 텍스트입니다.
    [SerializeField] private TextMeshProUGUI critRateLevelText;

    // 치명타 공격력 강화 레벨 표시 텍스트입니다.
    [SerializeField] private TextMeshProUGUI critDamageLevelText;

    // Unity 시작 시 자동 실행됩니다.
    private void Start()
    {
        // 공격력 버튼 클릭 시 공격력 강화 함수 실행
        attackUpgradeButton.onClick.AddListener(() => OnClickUpgrade(BackndGameDataManager.STAT_ATTACK));

        // 체력 버튼 클릭 시 체력 강화 함수 실행
        hpUpgradeButton.onClick.AddListener(() => OnClickUpgrade(BackndGameDataManager.STAT_HP));

        // 초당 회복 버튼 클릭 시 회복 강화 함수 실행
        hpRegenUpgradeButton.onClick.AddListener(() => OnClickUpgrade(BackndGameDataManager.STAT_HP_REGEN));

        // 치명타 확률 버튼 클릭 시 치명타 확률 강화 함수 실행
        critRateUpgradeButton.onClick.AddListener(() => OnClickUpgrade(BackndGameDataManager.STAT_CRIT_RATE));

        // 치명타 공격력 버튼 클릭 시 치명타 공격력 강화 함수 실행
        critDamageUpgradeButton.onClick.AddListener(() => OnClickUpgrade(BackndGameDataManager.STAT_CRIT_DAMAGE));

        // 처음 시작할 때 UI를 한 번 갱신합니다.
        RefreshUI();
    }

    // 강화 버튼 클릭 시 실행되는 함수입니다.
    private void OnClickUpgrade(string statId)
    {
        // 게임 데이터 매니저가 없으면
        if (BackndGameDataManager.Instance == null)
        {
            // 에러 로그 출력
            Debug.LogError("BackndGameDataManager가 없습니다.");

            // 함수 종료
            return;
        }

        // 해당 스탯 강화 요청
        bool success = BackndGameDataManager.Instance.UpgradeStat(statId);

        // 강화 실패 시
        if (!success)
        {
            // 에러 로그 출력
            Debug.LogError("스탯 강화 실패 : " + statId);

            // 함수 종료
            return;
        }

        // 강화 성공 후 UI 갱신
        RefreshUI();
    }

    // 강화 레벨 텍스트를 갱신하는 함수입니다.
    public void RefreshUI()
    {
        // 매니저가 없으면 함수 종료
        if (BackndGameDataManager.Instance == null)
            return;

        // 강화 데이터가 없으면 함수 종료
        if (BackndGameDataManager.Instance.CurrentStatUpgradeData == null)
            return;

        // 공격력 강화 레벨 표시
        attackLevelText.text = "공격력 Lv." + BackndGameDataManager.Instance.CurrentStatUpgradeData.GetLevel(BackndGameDataManager.STAT_ATTACK);

        // 체력 강화 레벨 표시
        hpLevelText.text = "체력 Lv." + BackndGameDataManager.Instance.CurrentStatUpgradeData.GetLevel(BackndGameDataManager.STAT_HP);

        // 초당 회복 강화 레벨 표시
        hpRegenLevelText.text = "회복 Lv." + BackndGameDataManager.Instance.CurrentStatUpgradeData.GetLevel(BackndGameDataManager.STAT_HP_REGEN);

        // 치명타 확률 강화 레벨 표시
        critRateLevelText.text = "치확 Lv." + BackndGameDataManager.Instance.CurrentStatUpgradeData.GetLevel(BackndGameDataManager.STAT_CRIT_RATE);

        // 치명타 공격력 강화 레벨 표시
        critDamageLevelText.text = "치피 Lv." + BackndGameDataManager.Instance.CurrentStatUpgradeData.GetLevel(BackndGameDataManager.STAT_CRIT_DAMAGE);
    }
}