using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrowthStatManager : MonoBehaviour
{
    [Header("# Reference")]
    [SerializeField] private RuntimeData runtimeData;

    [Header("# Temporary EXP")]
    [SerializeField] private float expPerMonsterKill = 100f;

    public int userLevel;
    public float nowExp;
    public float maxExp;
    public int growthPoint;

    [Header("# Growth Stat Per Level")]
    [SerializeField] private float attackPerLevel = 3f;
    [SerializeField] private float hpPerLevel = 15f;
    [SerializeField] private float critDamagePerLevel = 5f;

    [Header("# UI")]
    public TMP_Text UserLevel_Text;
    public TMP_Text userExp_Text;
    public TMP_Text growthPoint_Text;
    public Slider expSlider;

    public int GrowthLevel => GrowthData.growthLevel;
    public int GrowthPoint => GrowthData.growthPoint;
    public float NowExp => GrowthData.nowExp;
    public float MaxExp => Mathf.Max(1f, GrowthLevel * 300f);
    public int AttackLevel => GrowthData.attackLevel;
    public int HpLevel => GrowthData.hpLevel;
    public int CritDamageLevel => GrowthData.critDamageLevel;

    public float AttackBonus => AttackLevel * attackPerLevel;
    public float HpBonus => HpLevel * hpPerLevel;
    public float CritDamageBonus => CritDamageLevel * critDamagePerLevel;


    public GrowthUpgradeCard[] cards;

    private RuntimeData.GrowthData GrowthData
    {
        get
        {
            ResolveReferences();
            return runtimeData.GetRoot().growth;
        }
    }

    private void Awake()
    {
        ResolveReferences();

        if (cards == null || cards.Length == 0)
            cards = GetComponentsInChildren<GrowthUpgradeCard>(true);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (runtimeData != null)
            runtimeData.OnDataChanged += RefreshUI;

        MonsterHealth.OnNormalMonsterKilled += HandleNormalMonsterKilled;

        InitCards();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (runtimeData != null)
            runtimeData.OnDataChanged -= RefreshUI;

        MonsterHealth.OnNormalMonsterKilled -= HandleNormalMonsterKilled;
    }

    public void AddGrowthLevel(int amount = 1)
    {
        if (amount <= 0)
            return;

        RuntimeData.GrowthData growth = GrowthData;
        growth.growthLevel += amount;
        growth.growthPoint += amount;
        growth.nowExp = 0f;
        runtimeData.NotifyDataChanged();
    }

    public void AddGrowthExp(float amount)
    {
        if (amount <= 0f)
            return;

        RuntimeData.GrowthData growth = GrowthData;
        float maxExpForCurrentLevel = GetMaxExp(growth.growthLevel);
        growth.nowExp = Mathf.Min(growth.nowExp + amount, maxExpForCurrentLevel);

        runtimeData.NotifyDataChanged();
    }

    public bool TryLevelUp()
    {
        RuntimeData.GrowthData growth = GrowthData;
        float maxExpForCurrentLevel = GetMaxExp(growth.growthLevel);

        if (growth.nowExp < maxExpForCurrentLevel)
            return false;

        growth.nowExp = 0f;
        growth.growthLevel++;
        growth.growthPoint++;
        runtimeData.NotifyDataChanged();
        return true;
    }

    public void LevelUp()
    {
        TryLevelUp();
    }

    public bool TryUpgradeAttack()
    {
        return TrySpendGrowthPoint(growth => growth.attackLevel++);
    }

    public bool TryUpgradeHp()
    {
        return TrySpendGrowthPoint(growth => growth.hpLevel++);
    }

    public bool TryUpgradeCritDamage()
    {
        return TrySpendGrowthPoint(growth => growth.critDamageLevel++);
    }

    public bool TryUpgradeStat(GrowthUpgradeCard.GrowthStatType statType)
    {
        switch (statType)
        {
            case GrowthUpgradeCard.GrowthStatType.Attack:
                return TryUpgradeAttack();
            case GrowthUpgradeCard.GrowthStatType.Hp:
                return TryUpgradeHp();
            case GrowthUpgradeCard.GrowthStatType.CritDamage:
                return TryUpgradeCritDamage();
            default:
                return false;
        }
    }

    public int GetGrowthStatLevel(GrowthUpgradeCard.GrowthStatType statType)
    {
        switch (statType)
        {
            case GrowthUpgradeCard.GrowthStatType.Attack:
                return AttackLevel;
            case GrowthUpgradeCard.GrowthStatType.Hp:
                return HpLevel;
            case GrowthUpgradeCard.GrowthStatType.CritDamage:
                return CritDamageLevel;
            default:
                return 0;
        }
    }

    public float GetGrowthStatBonus(GrowthUpgradeCard.GrowthStatType statType)
    {
        switch (statType)
        {
            case GrowthUpgradeCard.GrowthStatType.Attack:
                return AttackBonus;
            case GrowthUpgradeCard.GrowthStatType.Hp:
                return HpBonus;
            case GrowthUpgradeCard.GrowthStatType.CritDamage:
                return CritDamageBonus;
            default:
                return 0f;
        }
    }

    private bool TrySpendGrowthPoint(System.Action<RuntimeData.GrowthData> applyUpgrade)
    {
        RuntimeData.GrowthData growth = GrowthData;

        if (growth.growthPoint <= 0)
            return false;

        growth.growthPoint--;
        applyUpgrade?.Invoke(growth);
        runtimeData.NotifyDataChanged();
        return true;
    }

    private void HandleNormalMonsterKilled()
    {
        AddGrowthExp(expPerMonsterKill);
    }

    private void InitCards()
    {
        if (cards == null || cards.Length == 0)
            return;

        for (int index = 0; index < cards.Length; index++)
        {
            GrowthUpgradeCard card = cards[index];
            if (card == null)
                continue;

            card.Init(this, GetCardStatType(index));
        }
    }

    private void RefreshUI()
    {
        RuntimeData.GrowthData growth = GrowthData;

        userLevel = growth.growthLevel;
        nowExp = growth.nowExp;
        maxExp = MaxExp;
        growthPoint = growth.growthPoint;

        if (UserLevel_Text != null)
            UserLevel_Text.text = $"Lv. {userLevel}";

        if (userExp_Text != null)
            userExp_Text.text = $"{nowExp:0} / {maxExp:0}";

        if (growthPoint_Text != null)
            growthPoint_Text.text = "Stat Point : " + growthPoint.ToString();

        if (expSlider != null)
        {
            expSlider.maxValue = maxExp;
            expSlider.value = Mathf.Clamp(nowExp, 0f, maxExp);
        }

        RefreshCards();
    }

    private void RefreshCards()
    {
        if (cards == null || cards.Length == 0)
            return;

        for (int index = 0; index < cards.Length; index++)
        {
            if (cards[index] != null)
                cards[index].Refresh();
        }
    }

    private void ResolveReferences()
    {
        if (runtimeData == null)
            runtimeData = GetComponent<RuntimeData>();

        if (runtimeData == null)
            runtimeData = FindAnyObjectByType<RuntimeData>();
    }

    private static float GetMaxExp(int growthLevel)
    {
        return Mathf.Max(1f, growthLevel * 300f);
    }

    private static GrowthUpgradeCard.GrowthStatType GetCardStatType(int index)
    {
        switch (index)
        {
            case 0:
                return GrowthUpgradeCard.GrowthStatType.Attack;
            case 1:
                return GrowthUpgradeCard.GrowthStatType.Hp;
            case 2:
                return GrowthUpgradeCard.GrowthStatType.CritDamage;
            default:
                return GrowthUpgradeCard.GrowthStatType.Attack;
        }
    }
}
