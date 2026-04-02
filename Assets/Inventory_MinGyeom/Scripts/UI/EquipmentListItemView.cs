using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 목록에서 장비 아이템 1칸의 표시를 담당합니다.
/// 아이템 아이콘, 레벨, 등급 프레임, 타입 배지, 장착 체크 상태를 여기서 갱신합니다.
/// </summary>
public class EquipmentListItemView : MonoBehaviour
{
    [Header("Type Icons")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite hatTypeIcon;
    [SerializeField] private Sprite ringTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite necklaceTypeIcon;
    [SerializeField] private Sprite shoesTypeIcon;

    [Header("Debug")]
    [SerializeField] private string currentEquipmentId;
    [SerializeField] private int currentLevel;
    [SerializeField] private int currentOwnedCount;

    [Header("Bound UI References")]
    [SerializeField] private GameObject normalBlueFrame;
    [SerializeField] private GameObject normalBrownFrame;
    [SerializeField] private GameObject normalGreenFrame;
    [SerializeField] private GameObject normalPlumFrame;
    [SerializeField] private GameObject normalYellowFrame;
    [SerializeField] private GameObject normalRedFrame;
    [SerializeField] private GameObject itemFrameRoot;
    [SerializeField] private GameObject add1Root;
    [SerializeField] private GameObject add2Root;
    [SerializeField] private GameObject checkRoot;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private GameObject typeAreaRoot;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image typeFrameImage;
    [SerializeField] private Image typeBgImage;
    [SerializeField] private Image typeIconImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button button;

    [Header("Runtime UI State")]
    [SerializeField] private string activeRarityFrameName;
    [SerializeField] private Sprite currentItemIconSprite;
    [SerializeField] private bool isAdd1Active;
    [SerializeField] private bool isAdd2Active;
    [SerializeField] private bool isCheckActive;
    [SerializeField] private bool isLockActive;
    [SerializeField] private bool isItemFrameActive;
    [SerializeField] private bool isTypeAreaActive;
    [SerializeField] private bool isLevelTextActive;
    [SerializeField] private string levelDisplayText;
    [SerializeField] private Color currentTypeFrameColor = Color.white;
    [SerializeField] private Color currentTypeBgColor = Color.white;
    [SerializeField] private Sprite currentTypeIconSprite;

    [Header("Output Bindings")]
    [SerializeField] private TMP_Text equipmentNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text itemLevelText;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private TMP_Text equippedStateText;
    [SerializeField] private GameObject equippedStateOnObject;
    [SerializeField] private GameObject equippedStateOffObject;

    public string CurrentEquipmentId => currentEquipmentId;
    public int CurrentLevel => currentLevel;
    public int CurrentOwnedCount => currentOwnedCount;

    private void Awake()
    {
        // 씬에서 처음 만들어질 때 UI 자식 참조를 미리 캐시합니다.
        CacheReferences();
    }

    private void OnEnable()
    {
        // 비활성/활성 전환 이후에도 참조가 유지되도록 다시 확인합니다.
        CacheReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif

    public void SetItemById(
        string equipmentId,
        int level,
        int ownedCount = 1,
        EquipmentPrototypeState state = null,
        EquipmentDatabase database = null)
    {
        CacheReferences();

        // 현재 슬롯이 어떤 장비를 어떤 레벨로 보여주는지 기록해 둡니다.
        currentEquipmentId = equipmentId;
        currentLevel = level;
        currentOwnedCount = Mathf.Max(0, ownedCount);
        SetSlotVisualActive(true);

        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            Clear();
            return;
        }

        EquipmentPrototypeState resolvedState = state != null
            ? state
            : FindFirstObjectByType<EquipmentPrototypeState>(FindObjectsInactive.Include);

        EquipmentDatabase resolvedDatabase = database != null
            ? database
            : resolvedState != null ? resolvedState.EquipmentDatabase : null;

        // 외부에서 state/database를 넘기지 않았으면 씬에서 찾아서 표시를 이어갑니다.
        if (resolvedDatabase == null)
        {
            Debug.LogWarning($"[{nameof(EquipmentListItemView)}] EquipmentDatabase is not assigned for '{name}'.", this);
            Clear();
            return;
        }

        if (!resolvedDatabase.TryGetById(equipmentId, out EquipmentDefinitionData definition))
        {
            Debug.LogWarning($"[{nameof(EquipmentListItemView)}] Could not find item id '{equipmentId}'.", this);
            Clear();
            return;
        }

        // 같은 카테고리 슬롯에 실제로 장착된 장비인지 확인해서 체크 표시를 결정합니다.
        bool isEquipped = resolvedState != null && resolvedState.IsEquippedInSlot(definition);
        ApplyOwnedDefinition(definition, Mathf.Max(1, level), isEquipped);
        ApplyBoundOutput(definition, Mathf.Max(1, level), currentOwnedCount, isEquipped);
    }

    public void SetUnownedDefinition(EquipmentDefinitionData definition)
    {
        CacheReferences();

        if (definition == null)
        {
            Clear();
            return;
        }

        // 미보유 아이템은 상세 데이터만 보여주고, 현재 보유 장비 정보는 비워 둡니다.
        currentEquipmentId = string.Empty;
        currentLevel = 0;
        currentOwnedCount = 0;
        SetSlotVisualActive(true);

        SetRarityFrame(definition.rarity);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        if (typeAreaRoot != null)
        {
            typeAreaRoot.SetActive(false);
        }

        if (checkRoot != null)
        {
            checkRoot.SetActive(false);
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(false);
        }

        SetFrameActive(add1Root, false);
        SetFrameActive(add2Root, true);

        SetButtonInteractable(false);
        ApplyBoundOutput(definition, 0, 0, false, false);
        RefreshRuntimeUiState();
    }

    public void SetAddSlot()
    {
        CacheReferences();

        currentEquipmentId = string.Empty;
        currentLevel = 0;
        currentOwnedCount = 0;
        SetSlotVisualActive(true);

        SetRarityFrame(EquipmentRarity.Common);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        if (typeAreaRoot != null)
        {
            typeAreaRoot.SetActive(false);
        }

        if (checkRoot != null)
        {
            checkRoot.SetActive(false);
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(false);
        }

        SetFrameActive(add1Root, false);
        SetFrameActive(add2Root, true);

        SetButtonInteractable(false);
        ClearBoundOutput();
        RefreshRuntimeUiState();
    }

    public void Clear()
    {
        CacheReferences();

        // 빈 슬롯 상태로 되돌립니다.
        currentEquipmentId = string.Empty;
        currentLevel = 0;
        currentOwnedCount = 0;
        SetSlotVisualActive(false);

        SetRarityFrame(EquipmentRarity.Common);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.gameObject.SetActive(false);
        }

        if (typeAreaRoot != null)
        {
            typeAreaRoot.SetActive(false);
        }

        if (checkRoot != null)
        {
            checkRoot.SetActive(false);
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(false);
        }

        if (add1Root != null)
        {
            add1Root.SetActive(false);
        }

        if (add2Root != null)
        {
            add2Root.SetActive(true);
        }

        SetButtonInteractable(false);
        ClearBoundOutput();
        RefreshRuntimeUiState();
    }

    private void ApplyOwnedDefinition(EquipmentDefinitionData definition, int level, bool isEquipped)
    {
        // 보유 중인 장비를 슬롯에 그릴 때 사용하는 공통 표시 로직입니다.
        SetRarityFrame(definition.rarity);
        ApplyTypeArea(definition.rarity, definition.category);

        if (itemIconImage != null)
        {
            itemIconImage.sprite = definition.uiIcon;
            itemIconImage.enabled = definition.uiIcon != null;
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(true);
            levelText.text = $"Lv.{level}";
        }

        if (typeAreaRoot != null)
        {
            typeAreaRoot.SetActive(true);
        }

        if (checkRoot != null)
        {
            checkRoot.SetActive(isEquipped);
        }

        if (lockRoot != null)
        {
            lockRoot.SetActive(false);
        }

        if (add1Root != null)
        {
            add1Root.SetActive(false);
        }

        if (add2Root != null)
        {
            add2Root.SetActive(false);
        }

        SetButtonInteractable(true);
        RefreshRuntimeUiState();
    }

    private void ApplyTypeArea(EquipmentRarity rarity, EquipmentCategory category)
    {
        // 타입 영역은 등급 색상과 카테고리 아이콘을 함께 맞춰서 표시합니다.
        if (typeFrameImage != null)
        {
            typeFrameImage.color = GetTypeFrameColor(rarity);
        }

        if (typeBgImage != null)
        {
            typeBgImage.color = GetTypeFillColor(rarity);
        }

        if (typeIconImage != null)
        {
            typeIconImage.sprite = GetCategoryIcon(category);
            typeIconImage.enabled = typeIconImage.sprite != null;
        }
    }

    private void SetRarityFrame(EquipmentRarity rarity)
    {
        // 등급별로 준비된 프레임 중 하나만 켭니다.
        SetFrameActive(normalBlueFrame, rarity == EquipmentRarity.Rare);
        SetFrameActive(normalBrownFrame, rarity == EquipmentRarity.Common);
        SetFrameActive(normalGreenFrame, rarity == EquipmentRarity.Magic);
        SetFrameActive(normalPlumFrame, rarity == EquipmentRarity.Epic);
        SetFrameActive(normalYellowFrame, rarity == EquipmentRarity.Legendary);
        SetFrameActive(normalRedFrame, false);
    }

    private void CacheReferences()
    {
        // 자주 접근하는 자식 오브젝트를 한 번 찾아두고 이후 재사용합니다.
        itemFrameRoot ??= FindByPath("ItemFrame_01");
        normalBlueFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Blue");
        normalBrownFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Brown");
        normalGreenFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Green");
        normalPlumFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Plum");
        normalYellowFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Yellow");
        normalRedFrame ??= FindByPath("ItemFrame_01/NormalArea/ItemFrame_01_Normal_Red");

        add1Root ??= FindByPath("ItemFrame_01/Add_1");
        add2Root ??= FindByPath("ItemFrame_01/Add_2");
        checkRoot ??= FindByPath("Check");
        lockRoot ??= FindByPath("ItemFrame_01/Lock");
        typeAreaRoot ??= FindByPath("TypeArea");

        levelText ??= FindComponentByPath<TMP_Text>("Text_Level");
        ownedCountText ??= FindComponentByPath<TMP_Text>("Text_Count");
        ownedCountText ??= FindComponentByPath<TMP_Text>("Text_OwnedCount");
        ownedCountText ??= FindComponentByPath<TMP_Text>("Text_Amount");
        itemIconImage ??= FindComponentByPath<Image>("ItemFrame_01/Item/Icon");
        itemIconImage ??= FindComponentByPath<Image>("ItemFrame_01/Item");
        typeBgImage ??= FindComponentByPath<Image>("TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Bg");
        typeIconImage ??= FindComponentByPath<Image>("TypeArea/BasicFrame_Diamond_H48_NoBorder_BasePrefab/Icon");
        button ??= GetComponent<Button>();

        // 타입 프레임은 프리팹 구조상 첫 자식 Image를 그대로 쓰고 있습니다.
        if (typeFrameImage == null && typeAreaRoot != null && typeAreaRoot.transform.childCount > 0)
        {
            typeFrameImage = typeAreaRoot.transform.GetChild(0).GetComponent<Image>();
        }

        RefreshRuntimeUiState();
    }

    private GameObject FindByPath(string relativePath)
    {
        Transform found = transform.Find(relativePath);
        return found != null ? found.gameObject : null;
    }

    private T FindComponentByPath<T>(string relativePath) where T : Component
    {
        Transform found = transform.Find(relativePath);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static void SetFrameActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void SetSlotVisualActive(bool isActive)
    {
        SetFrameActive(itemFrameRoot, isActive);

        if (!isActive)
        {
            SetFrameActive(typeAreaRoot, false);

            if (levelText != null)
            {
                levelText.gameObject.SetActive(false);
            }
        }
    }

    private void SetButtonInteractable(bool isInteractable)
    {
        if (button != null)
        {
            button.interactable = isInteractable;
        }
    }

    private void ApplyBoundOutput(EquipmentDefinitionData definition, int level, int ownedCount, bool isEquipped, bool showLevel = true)
    {
        if (equipmentNameText != null)
        {
            equipmentNameText.text = definition != null ? definition.displayName : string.Empty;
        }

        if (rarityText != null)
        {
            rarityText.text = definition != null ? definition.rarity.ToString() : string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition != null ? definition.description : string.Empty;
        }

        if (itemLevelText != null)
        {
            itemLevelText.text = definition != null && showLevel ? $"Lv.{Mathf.Max(1, level)}" : string.Empty;
            itemLevelText.gameObject.SetActive(definition != null && showLevel);
        }

        if (ownedCountText != null)
        {
            bool shouldShowOwnedCount = definition != null && ownedCount > 0;
            ownedCountText.text = shouldShowOwnedCount ? $"x{ownedCount}" : string.Empty;
            ownedCountText.gameObject.SetActive(shouldShowOwnedCount);
        }

        if (equippedStateText != null)
        {
            equippedStateText.text = definition == null ? string.Empty : isEquipped ? "장착중" : "미장착";
        }

        if (equippedStateOnObject != null)
        {
            equippedStateOnObject.SetActive(definition != null && isEquipped);
        }

        if (equippedStateOffObject != null)
        {
            equippedStateOffObject.SetActive(definition != null && !isEquipped);
        }
    }

    private void ClearBoundOutput()
    {
        ApplyBoundOutput(null, 0, 0, false, false);
    }

    private void RefreshRuntimeUiState()
    {
        activeRarityFrameName = GetActiveFrameName();
        currentItemIconSprite = itemIconImage != null ? itemIconImage.sprite : null;

        isAdd1Active = add1Root != null && add1Root.activeSelf;
        isAdd2Active = add2Root != null && add2Root.activeSelf;
        isCheckActive = checkRoot != null && checkRoot.activeSelf;
        isLockActive = lockRoot != null && lockRoot.activeSelf;
        isItemFrameActive = itemFrameRoot != null && itemFrameRoot.activeSelf;
        isTypeAreaActive = typeAreaRoot != null && typeAreaRoot.activeSelf;
        isLevelTextActive = levelText != null && levelText.gameObject.activeSelf;
        levelDisplayText = levelText != null ? levelText.text : string.Empty;

        currentTypeFrameColor = typeFrameImage != null ? typeFrameImage.color : Color.white;
        currentTypeBgColor = typeBgImage != null ? typeBgImage.color : Color.white;
        currentTypeIconSprite = typeIconImage != null ? typeIconImage.sprite : null;
    }

    private string GetActiveFrameName()
    {
        if (normalBlueFrame != null && normalBlueFrame.activeSelf)
        {
            return normalBlueFrame.name;
        }

        if (normalBrownFrame != null && normalBrownFrame.activeSelf)
        {
            return normalBrownFrame.name;
        }

        if (normalGreenFrame != null && normalGreenFrame.activeSelf)
        {
            return normalGreenFrame.name;
        }

        if (normalPlumFrame != null && normalPlumFrame.activeSelf)
        {
            return normalPlumFrame.name;
        }

        if (normalYellowFrame != null && normalYellowFrame.activeSelf)
        {
            return normalYellowFrame.name;
        }

        if (normalRedFrame != null && normalRedFrame.activeSelf)
        {
            return normalRedFrame.name;
        }

        return string.Empty;
    }

    private Sprite GetCategoryIcon(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Weapon => weaponTypeIcon,
            EquipmentCategory.Hat => hatTypeIcon,
            EquipmentCategory.Ring => ringTypeIcon,
            EquipmentCategory.Armor => armorTypeIcon,
            EquipmentCategory.Necklace => necklaceTypeIcon,
            EquipmentCategory.Shoes => shoesTypeIcon,
            _ => weaponTypeIcon
        };
    }

    private static Color GetTypeFrameColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => new Color32(181, 126, 79, 255),
            EquipmentRarity.Magic => new Color32(74, 151, 84, 255),
            EquipmentRarity.Rare => new Color32(52, 103, 185, 255),
            EquipmentRarity.Epic => new Color32(151, 86, 187, 255),
            EquipmentRarity.Legendary => new Color32(214, 149, 44, 255),
            _ => Color.white
        };
    }

    private static Color GetTypeFillColor(EquipmentRarity rarity)
    {
        return rarity switch
        {
            EquipmentRarity.Common => new Color32(241, 206, 146, 255),
            EquipmentRarity.Magic => new Color32(138, 219, 138, 255),
            EquipmentRarity.Rare => new Color32(99, 191, 255, 255),
            EquipmentRarity.Epic => new Color32(206, 144, 255, 255),
            EquipmentRarity.Legendary => new Color32(255, 221, 105, 255),
            _ => Color.white
        };
    }
}
