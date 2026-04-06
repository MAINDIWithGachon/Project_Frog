using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillDetailPanelUI : MonoBehaviour
{
    // 상세창 루트 오브젝트
    public GameObject panel;

    // 상세창 아이콘 영역
    public Image iconImage;

    // 상세창 이름 텍스트
    public TMP_Text nameText;

    // 상세창 설명 텍스트
    public TMP_Text descriptionText;

    // 상세창 수치 텍스트
    public TMP_Text valueText;

    // 실제로 스킬 아이콘을 갈아끼울 이미지 타겟
    private Image runtimeIconTarget;

    // 현재 상세창에 표시 중인 스킬 데이터
    private SkillDetailData currentData;

    // Equip 버튼
    private Button equipButton;

    // 닫기 버튼
    private Button closeButton;

    // Equip / Unequip 텍스트 변경용 참조
    private TMP_Text equipButtonText;

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        // 인스펙터 연결이 비어 있어도 자동으로 자식 UI를 찾아서 연결한다.
        AutoBindIfMissing();
        BindButtons();
    }

    public void Open(SkillDetailData data)
    {
        if (data == null)
            return;

        // 어떤 스킬을 보고 있는지 저장해두고 Equip/Unequip 시 재사용한다.
        currentData = data;
        AutoBindIfMissing();

        // 상세창 안의 실제 아이콘 이미지에 선택한 스킬 스프라이트를 넣는다.
        Image targetImage = ResolveIconTarget();
        if (targetImage != null)
        {
            targetImage.enabled = data.icon != null;
            targetImage.sprite = data.icon;
            targetImage.overrideSprite = data.icon;
            targetImage.color = Color.white;
            targetImage.preserveAspect = true;
            targetImage.type = Image.Type.Simple;
        }

        if (nameText != null)
            nameText.text = data.skillName;

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (valueText != null)
            valueText.text = data.currentValue + " / " + data.maxValue;

        // 현재 스킬이 이미 장착된 상태면 버튼 문구를 Unequip으로 바꾼다.
        RefreshEquipButtonLabel();

        if (panel != null)
            panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void EquipCurrentSkill()
    {
        if (currentData == null || currentData.icon == null)
            return;

        // 실제 장착 슬롯 3칸이 들어 있는 그룹을 찾는다.
        Transform slotGroup = FindSlotGroupRoot();
        if (slotGroup == null)
            return;

        // 현재 하단에 보이는 스킬 버튼들의 아이콘 목록을 이용해
        // 슬롯이 비어 있는지 / 이미 스킬이 들어있는지 판정한다.
        SkillItemButton[] skillButtons = FindObjectsByType<SkillItemButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 이미 장착된 스킬이라면 다시 눌렀을 때 해제한다.
        int equippedSlotIndex = FindEquippedSlotIndex(slotGroup);
        if (equippedSlotIndex >= 0)
        {
            ClearSlot(slotGroup.GetChild(equippedSlotIndex));
            RefreshEquipButtonLabel();
            Close();
            return;
        }

        for (int i = 0; i < slotGroup.childCount; i++)
        {
            Transform slot = slotGroup.GetChild(i);
            if (slot == null)
                continue;

            // 잠금 슬롯은 장착 대상에서 제외한다.
            Transform lockRoot = slot.Find("Lock");
            if (lockRoot != null && lockRoot.gameObject.activeSelf)
                continue;

            Transform normalRoot = slot.Find("Normal");
            Transform emptyRoot = slot.Find("Empty");
            Transform disabledRoot = slot.Find("Disabled");
            Image slotIcon = FindSlotIcon(slot);

            bool isEmpty = IsEquippableEmptySlot(slotIcon, skillButtons)
                || (emptyRoot != null && emptyRoot.gameObject.activeSelf)
                || (normalRoot != null && !normalRoot.gameObject.activeSelf);

            if (!isEmpty)
                continue;

            // 빈 슬롯을 찾으면 장착 상태로 전환한다.
            if (normalRoot != null)
                normalRoot.gameObject.SetActive(true);

            if (emptyRoot != null)
                emptyRoot.gameObject.SetActive(false);

            if (disabledRoot != null)
                disabledRoot.gameObject.SetActive(false);

            if (slotIcon != null)
            {
                // 왼쪽부터 찾은 첫 빈 슬롯에 현재 스킬 아이콘을 넣는다.
                slotIcon.enabled = true;
                slotIcon.sprite = currentData.icon;
                slotIcon.overrideSprite = currentData.icon;
                slotIcon.color = Color.white;
                slotIcon.preserveAspect = false;
                slotIcon.type = Image.Type.Simple;
            }

            RefreshEquipButtonLabel();
            Close();
            return;
        }
    }

    private bool IsEquippableEmptySlot(Image slotIcon, SkillItemButton[] skillButtons)
    {
        // 아이콘 자체가 없으면 빈 슬롯으로 본다.
        if (slotIcon == null || slotIcon.sprite == null)
            return true;

        // 슬롯 아이콘이 하단 스킬 5개 중 하나와 일치하면
        // 이미 장착된 슬롯으로 판단한다.
        for (int i = 0; i < skillButtons.Length; i++)
        {
            SkillDetailData buttonData = skillButtons[i].skillData;
            if (buttonData == null || buttonData.icon == null)
                continue;

            if (buttonData.icon == slotIcon.sprite || buttonData.icon.name == slotIcon.sprite.name)
                return false;
        }

        return true;
    }

    private Transform FindSlotGroupRoot()
    {
        Transform current = transform;

        // 같은 팝업 안의 Character_Skill > Middle1 > Group_SkillSlot를 우선 찾는다.
        while (current != null)
        {
            Transform popupRoot = current.Find("Border/Character_Skill/Middle1/Group_SkillSlot");
            if (popupRoot != null)
                return popupRoot;

            Transform border = current.Find("Border");
            if (border != null)
            {
                Transform nested = border.Find("Character_Skill/Middle1/Group_SkillSlot");
                if (nested != null)
                    return nested;
            }

            current = current.parent;
        }

        Transform characterSkill = FindCharacterSkillRoot();
        if (characterSkill == null)
            return null;

        // 최후 fallback
        return characterSkill.Find("Middle1/Group_SkillSlot");
    }

    private void AutoBindIfMissing()
    {
        // 상세창 아이콘/텍스트를 이름 기준으로 자동 연결한다.
        if (iconImage == null)
            iconImage = FindImageByName("Skill") ?? FindImageByName("Icon") ?? FindImageByName("Bg(Mask)") ?? FindImageByName("SkillFrame_01");

        if (nameText == null)
            nameText = FindTextByName("Text_ItemName");

        if (descriptionText == null)
            descriptionText = FindTextByName("Text_Description");

        if (valueText == null)
            valueText = FindTextByName("Text (TMP)");
    }

    private void BindButtons()
    {
        // 텍스트가 Equip인 버튼을 찾아 EquipCurrentSkill에 연결한다.
        if (equipButton == null)
            equipButton = FindButtonByText("Equip");

        if (equipButton != null)
        {
            equipButton.onClick.RemoveListener(EquipCurrentSkill);
            equipButton.onClick.AddListener(EquipCurrentSkill);
            equipButtonText = equipButton.GetComponentInChildren<TMP_Text>(true);
        }

        // X 버튼을 찾아 닫기 함수에 연결한다.
        if (closeButton == null)
            closeButton = FindButtonByName("Button_Close_01");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private Image ResolveIconTarget()
    {
        if (runtimeIconTarget != null)
            return runtimeIconTarget;

        if (iconImage == null)
            return null;

        // 프레임/배경 말고 실제 스킬 아이콘으로 쓰는 이미지를 찾는다.
        Image[] images = iconImage.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (!IsSkillIconCandidate(images[i]))
                continue;

            runtimeIconTarget = images[i];
            return runtimeIconTarget;
        }

        runtimeIconTarget = iconImage;
        return runtimeIconTarget;
    }

    private bool IsSkillIconCandidate(Image candidate)
    {
        if (candidate == null)
            return false;

        // 이름과 스프라이트 이름을 기준으로
        // 프레임/배경/잠금 이미지는 제외하고 실제 아이콘만 고른다.
        string objectName = candidate.gameObject.name.ToLowerInvariant();
        if (objectName.Contains("skill"))
            return true;

        if (objectName.Contains("border") || objectName.Contains("frame") || objectName.Contains("lock") || objectName.Contains("empty"))
            return false;

        if (candidate.sprite == null)
            return true;

        string spriteName = candidate.sprite.name.ToLowerInvariant();
        if (spriteName.Contains("frame") || spriteName.Contains("white_bg") || spriteName.Contains("border") || spriteName.Contains("lock"))
            return false;

        return objectName.Contains("icon") || objectName.Contains("mask");
    }

    private Image FindImageByName(string targetName)
    {
        // 자식 Image 중 이름이 일치하는 첫 대상을 찾는다.
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == targetName)
                return images[i];
        }

        return null;
    }

    private TMP_Text FindTextByName(string targetName)
    {
        // 자식 TMP_Text 중 이름이 일치하는 첫 대상을 찾는다.
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == targetName)
                return texts[i];
        }

        return null;
    }

    private Button FindButtonByText(string targetText)
    {
        // 버튼 자식 텍스트를 읽어 원하는 버튼을 찾는다.
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            TMP_Text text = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                continue;

            if (text.text.Trim() == targetText)
                return buttons[i];
        }

        return null;
    }

    private Button FindButtonByName(string targetName)
    {
        // 버튼 오브젝트 이름으로 찾는다.
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == targetName)
                return buttons[i];
        }

        return null;
    }

    private Transform FindCharacterSkillRoot()
    {
        Transform current = transform;

        // 현재 상세창과 같은 부모 아래 있는 Character_Skill를 우선 찾는다.
        while (current != null)
        {
            Transform parent = current.parent;
            if (parent != null)
            {
                Transform sibling = parent.Find("Character_Skill");
                if (sibling != null)
                    return sibling;
            }

            current = current.parent;
        }

        // 씬 전체에서 같은 이름을 가진 오브젝트를 마지막으로 찾는다.
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == "Character_Skill")
                return allTransforms[i];
        }

        return null;
    }

    private Image FindSlotIcon(Transform slot)
    {
        if (slot == null)
            return null;

        // 실제로 보이는 슬롯 아이콘 이미지 경로를 우선순위대로 찾는다.
        string[] candidatePaths =
        {
            "Normal/Bg(Mask)/Skill",
            "Normal/Skill",
            "Normal/Bg(Mask)/Icon",
            "Normal/Icon"
        };

        for (int i = 0; i < candidatePaths.Length; i++)
        {
            Transform target = slot.Find(candidatePaths[i]);
            if (target == null)
                continue;

            Image image = target.GetComponent<Image>();
            if (image != null)
                return image;
        }

        // 경로로 못 찾으면 이름이 Icon / Skill인 이미지로 fallback 한다.
        Image[] images = slot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            string objectName = images[i].name.ToLowerInvariant();
            if (objectName == "icon" || objectName == "skill")
                return images[i];
        }

        return null;
    }

    private int FindEquippedSlotIndex(Transform slotGroup)
    {
        // 현재 상세창에 떠 있는 스킬 아이콘과 같은 슬롯이 있으면
        // 이미 장착된 상태로 간주한다.
        for (int i = 0; i < slotGroup.childCount; i++)
        {
            Transform slot = slotGroup.GetChild(i);
            Image slotIcon = FindSlotIcon(slot);
            if (slotIcon == null || slotIcon.sprite == null || currentData == null || currentData.icon == null)
                continue;

            if (slotIcon.sprite == currentData.icon || slotIcon.sprite.name == currentData.icon.name)
                return i;
        }

        return -1;
    }

    private void ClearSlot(Transform slot)
    {
        if (slot == null)
            return;

        // 해제 시에는 Normal을 끄고 Empty를 켜서
        // 빈 슬롯의 번개 아이콘 상태로 복구한다.
        Transform normalRoot = slot.Find("Normal");
        Transform emptyRoot = slot.Find("Empty");
        Transform disabledRoot = slot.Find("Disabled");
        Image slotIcon = FindSlotIcon(slot);

        if (normalRoot != null)
            normalRoot.gameObject.SetActive(false);

        if (emptyRoot != null)
            emptyRoot.gameObject.SetActive(true);

        if (disabledRoot != null)
            disabledRoot.gameObject.SetActive(false);

        if (slotIcon != null)
        {
            slotIcon.sprite = null;
            slotIcon.overrideSprite = null;
        }
    }

    private void RefreshEquipButtonLabel()
    {
        if (equipButtonText == null)
            return;

        // 현재 스킬이 위 슬롯 중 하나에 있으면 Unequip, 아니면 Equip
        Transform slotGroup = FindSlotGroupRoot();
        bool isEquipped = slotGroup != null && FindEquippedSlotIndex(slotGroup) >= 0;
        equipButtonText.text = isEquipped ? "Unequip" : "Equip";
    }
}
