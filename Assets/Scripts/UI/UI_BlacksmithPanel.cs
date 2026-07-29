using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Figma 대장간 UI (UI_Blacksmith) 와이어프레임 시안 기반의 개편된 대장간 메인 UI 컨트롤러입니다.
/// 상단 Split 2컬럼 패널(비주얼 & 선택 항목/3색 확률 인디케이터), 중앙 5x3 인벤토리 그리드,
/// 하단 Sell & Forge 액션 버튼(비용 뱃지 포함) 및 최하단 탭바를 지원합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithPanel : UI_BasePopup
{
    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(popupID)) popupID = "Blacksmith";
    }

    [Header("해금 및 가림막 UI")]
    [SerializeField] private GameObject lockedOverlayPanel;
    [SerializeField] private TextMeshProUGUI lockNoticeText;

    [Header("Figma Header & Title")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Upper Split Section (선택 항목 정보 및 3색 확률)")]
    [SerializeField] private RectTransform leftVisualPanel;
    [SerializeField] private Image equippedWeaponIcon;
    [SerializeField] private TextMeshProUGUI equippedWeaponNameText;
    [SerializeField] private TextMeshProUGUI weaponGradeLevelText; // 예: +7

    [Header("3-Color Rate Indicators (🟢, 🟡, 🔴)")]
    [Tooltip("성공 확률 텍스트 (🟢 초록)")]
    [SerializeField] private TextMeshProUGUI successRateText;

    [Tooltip("유지 확률 텍스트 (🟡 노랑)")]
    [SerializeField] private TextMeshProUGUI keepRateText;

    [Tooltip("파괴 확률 텍스트 (🔴 빨강)")]
    [SerializeField] private TextMeshProUGUI destroyRateText;

    [Header("Middle Inventory Grid Section (5x3 Grid)")]
    [SerializeField] private Transform inventoryGridContainer;
    [SerializeField] private UI_BlacksmithSlot slotPrefab;

    [Header("Lower Action Buttons (Sell & Forge)")]
    [SerializeField] private Button sellBtn;
    [SerializeField] private TextMeshProUGUI sellGoldText;
    [SerializeField] private Button forgeBtn;
    [SerializeField] private TextMeshProUGUI forgeGoldText;

    [Header("Bottom Navigation Tabs (Tab 1: Weapon / Tab 2: Gem)")]
    [SerializeField] private Button tab1Btn; // 무기 탭
    [SerializeField] private Button tab2Btn; // 보석 탭
    [SerializeField] private Image tab1Highlight;
    [SerializeField] private Image tab2Highlight;

    [Header("기타 파티클 및 옵션")]
    [SerializeField] private UI_BlacksmithEmberFX emberFX;
    [SerializeField] private Toggle useProtectionToggle;

    // 내부 관리 변수
    private WeaponItemData selectedWeapon;
    private GemItemData selectedGem;
    private int selectedWeaponIndex = 0;
    private int selectedGemIndex = 0;
    private bool isWeaponTab = true;

    private readonly List<UI_BlacksmithSlot> activeSlots = new List<UI_BlacksmithSlot>();

    private void OnEnable()
    {
        BlacksmithManager.OnInventoryUpdated += RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged += RefreshAllUI;

        if (tab1Btn != null) tab1Btn.onClick.AddListener(SwitchToWeaponTab);
        if (tab2Btn != null) tab2Btn.onClick.AddListener(SwitchToGemTab);
        if (forgeBtn != null) forgeBtn.onClick.AddListener(OnClickForge);
        if (sellBtn != null) sellBtn.onClick.AddListener(OnClickSell);

        // UI가 새로 켜지면 아무 슬롯도 선택하지 않은 상태
        selectedWeapon = null;
        selectedGem = null;
        selectedWeaponIndex = -1;
        selectedGemIndex = -1;

        SwitchToWeaponTab();
        RefreshAllUI();
    }

    private void OnDisable()
    {
        BlacksmithManager.OnInventoryUpdated -= RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged -= RefreshAllUI;

        if (tab1Btn != null) tab1Btn.onClick.RemoveListener(SwitchToWeaponTab);
        if (tab2Btn != null) tab2Btn.onClick.RemoveListener(SwitchToGemTab);
        if (forgeBtn != null) forgeBtn.onClick.RemoveListener(OnClickForge);
        if (sellBtn != null) sellBtn.onClick.RemoveListener(OnClickSell);
    }

    /// <summary>
    /// 대장간 해금 검증 및 전체 UI 갱신
    /// </summary>
    public override void RefreshAllUI()
    {
        bool isUnlocked = BlacksmithManager.Instance != null && BlacksmithManager.Instance.IsBlacksmithUnlocked();

        if (lockedOverlayPanel != null)
        {
            lockedOverlayPanel.SetActive(!isUnlocked);
        }

        if (lockNoticeText != null)
        {
            lockNoticeText.text = isUnlocked ? string.Empty : "대장간 건물을 먼저 건설해야 합니다!\n(코어2 대장간 건물 Lv 1 이상 필요)";
        }

        if (BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            BlacksmithManager.Instance.SortWeapons();
            var weapons = BlacksmithManager.Instance.ownedWeapons;
            if (weapons.Count == 0 || selectedWeaponIndex < 0 || selectedWeaponIndex >= weapons.Count)
            {
                selectedWeaponIndex = -1;
                selectedWeapon = null;
            }
            else
            {
                selectedWeapon = weapons[selectedWeaponIndex];
            }
            RefreshWeaponDetail(selectedWeapon);
            BuildWeaponGrid();
        }
        else
        {
            BlacksmithManager.Instance.SortGems();
            var gems = BlacksmithManager.Instance.ownedGems;
            if (gems.Count == 0 || selectedGemIndex < 0 || selectedGemIndex >= gems.Count)
            {
                selectedGemIndex = -1;
                selectedGem = null;
            }
            else
            {
                selectedGem = gems[selectedGemIndex];
            }
            RefreshGemDetail(selectedGem);
            BuildGemGrid();
        }

        bool hasSelection = isWeaponTab ? (selectedWeapon != null) : (selectedGem != null);
        if (forgeBtn != null) forgeBtn.interactable = hasSelection;
        if (sellBtn != null) sellBtn.interactable = hasSelection;
    }

    /// <summary>
    /// 선택 무기 상세 정보 및 3색 확률 인디케이터 (🟢, 🟡, 🔴) 갱신
    /// </summary>
    private void RefreshWeaponDetail(WeaponItemData weapon)
    {
        if (weapon == null)
        {
            if (equippedWeaponIcon != null)
            {
                equippedWeaponIcon.sprite = null;
                equippedWeaponIcon.gameObject.SetActive(false);
            }
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "선택된 무기 없음";
            if (weaponGradeLevelText != null) weaponGradeLevelText.text = "+0";
            if (successRateText != null) successRateText.text = "0%";
            if (keepRateText != null) keepRateText.text = "0%";
            if (destroyRateText != null) destroyRateText.text = "0%";
            if (forgeGoldText != null) forgeGoldText.text = "0";
            if (sellGoldText != null) sellGoldText.text = "0";
            return;
        }

        if (equippedWeaponIcon != null)
        {
            equippedWeaponIcon.sprite = weapon.iconSprite;
            equippedWeaponIcon.gameObject.SetActive(weapon.iconSprite != null);
        }
        if (equippedWeaponNameText != null)
        {
            equippedWeaponNameText.text = weapon.weaponName;
        }
        if (weaponGradeLevelText != null)
        {
            weaponGradeLevelText.text = $"+{weapon.grade}";
        }

        // 3-Color Rates (🟢 초록=성공, 🟡 노랑=유지, 🔴 빨강=파괴)
        float successRatio = weapon.upgradeSuccessRate;
        float keepRatio = weapon.upgradeKeepRate;
        float destroyRatio = weapon.upgradeDestroyRate;

        if (successRateText != null) successRateText.text = $"{Mathf.RoundToInt(successRatio * 100f)}%";
        if (keepRateText != null) keepRateText.text = $"{Mathf.RoundToInt(keepRatio * 100f)}%";
        if (destroyRateText != null) destroyRateText.text = $"{Mathf.RoundToInt(destroyRatio * 100f)}%";

        // 버튼 비용 뱃지 갱신
        if (forgeGoldText != null) forgeGoldText.text = "5,000";
        if (sellGoldText != null) sellGoldText.text = "2,500";
    }

    /// <summary>
    /// 선택 보석 상세 정보 갱신
    /// </summary>
    private void RefreshGemDetail(GemItemData gem)
    {
        if (gem == null)
        {
            if (equippedWeaponIcon != null)
            {
                equippedWeaponIcon.sprite = null;
                equippedWeaponIcon.gameObject.SetActive(false);
            }
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "선택된 보석 없음";
            if (weaponGradeLevelText != null) weaponGradeLevelText.text = "Lv.0";
            if (successRateText != null) successRateText.text = "0%";
            if (keepRateText != null) keepRateText.text = "0%";
            if (destroyRateText != null) destroyRateText.text = "0%";
            if (forgeGoldText != null) forgeGoldText.text = "0";
            if (sellGoldText != null) sellGoldText.text = "0";
            return;
        }

        if (equippedWeaponIcon != null)
        {
            equippedWeaponIcon.sprite = gem.iconSprite;
            equippedWeaponIcon.gameObject.SetActive(gem.iconSprite != null);
        }
        if (equippedWeaponNameText != null)
        {
            equippedWeaponNameText.text = gem.gemName;
        }
        if (weaponGradeLevelText != null)
        {
            weaponGradeLevelText.text = $"Lv.{gem.level}";
        }

        float successRatio = gem.upgradeSuccessRate;
        float keepRatio = gem.upgradeKeepRate;
        float destroyRatio = gem.upgradeDestroyRate;

        if (successRateText != null) successRateText.text = $"{Mathf.RoundToInt(successRatio * 100f)}%";
        if (keepRateText != null) keepRateText.text = $"{Mathf.RoundToInt(keepRatio * 100f)}%";
        if (destroyRateText != null) destroyRateText.text = $"{Mathf.RoundToInt(destroyRatio * 100f)}%";

        if (forgeGoldText != null) forgeGoldText.text = "3,000";
        if (sellGoldText != null) sellGoldText.text = $"{gem.sellPrice:N0}";
    }

    /// <summary>
    /// 5x3 그리드 무기 인벤토리 슬롯 생성 및 갱신
    /// </summary>
    private void BuildWeaponGrid()
    {
        ClearGridSlots();
        if (inventoryGridContainer == null || slotPrefab == null || BlacksmithManager.Instance == null) return;

        var weapons = BlacksmithManager.Instance.ownedWeapons;
        int totalSlots = Mathf.Max(15, weapons.Count); // 최소 15개 슬롯 (5x3)

        for (int i = 0; i < totalSlots; i++)
        {
            WeaponItemData wData = i < weapons.Count ? weapons[i] : null;
            UI_BlacksmithSlot slot = Instantiate(slotPrefab, inventoryGridContainer);
            bool isSelected = (wData != null && i == selectedWeaponIndex);
            slot.Setup(wData, i, isSelected, OnSlotSelected);
            activeSlots.Add(slot);
        }
    }

    /// <summary>
    /// 5x3 그리드 보석 인벤토리 슬롯 생성 및 갱신
    /// </summary>
    private void BuildGemGrid()
    {
        ClearGridSlots();
        if (inventoryGridContainer == null || slotPrefab == null || BlacksmithManager.Instance == null) return;

        var gems = BlacksmithManager.Instance.ownedGems;
        int totalSlots = Mathf.Max(15, gems.Count);

        for (int i = 0; i < totalSlots; i++)
        {
            GemItemData gData = i < gems.Count ? gems[i] : null;
            UI_BlacksmithSlot slot = Instantiate(slotPrefab, inventoryGridContainer);
            bool isSelected = (gData != null && i == selectedGemIndex);
            slot.SetupGem(gData, i, isSelected, OnSlotSelected);
            activeSlots.Add(slot);
        }
    }

    private void ClearGridSlots()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        activeSlots.Clear();
    }

    private void OnSlotSelected(UI_BlacksmithSlot slot)
    {
        if (isWeaponTab)
        {
            if (slot.BoundWeapon != null)
            {
                selectedWeaponIndex = slot.SlotIndex;
                RefreshAllUI();
            }
        }
        else
        {
            if (slot.BoundGem != null)
            {
                selectedGemIndex = slot.SlotIndex;
                RefreshAllUI();
            }
        }
    }

    #region Tab & Action Handlers

    public void SwitchToWeaponTab()
    {
        isWeaponTab = true;
        selectedWeapon = null;
        selectedWeaponIndex = -1;
        if (tab1Highlight != null) tab1Highlight.gameObject.SetActive(true);
        if (tab2Highlight != null) tab2Highlight.gameObject.SetActive(false);
        RefreshAllUI();
    }

    public void SwitchToGemTab()
    {
        isWeaponTab = false;
        selectedGem = null;
        selectedGemIndex = -1;
        if (tab1Highlight != null) tab1Highlight.gameObject.SetActive(false);
        if (tab2Highlight != null) tab2Highlight.gameObject.SetActive(true);
        RefreshAllUI();
    }

    private void OnClickForge()
    {
        if (BlacksmithManager.Instance == null) return;

        if (emberFX != null)
        {
            emberFX.TriggerEnhanceSparkFX();
        }

        if (isWeaponTab)
        {
            if (selectedWeapon == null || selectedWeaponIndex < 0) return;
            if (selectedWeapon.nextGradeWeapon == null)
            {
                UI_ToastPopup.Show("Notice_Max_Upgrade");
                return;
            }

            WeaponItemData nextWeapon = selectedWeapon.nextGradeWeapon;
            bool useProtection = useProtectionToggle != null && useProtectionToggle.isOn;
            WeaponEnhanceResult result = BlacksmithManager.Instance.EnhanceWeaponAtIndex(selectedWeaponIndex, useProtection);

            if (result == WeaponEnhanceResult.Success)
            {
                // 성공 시 강화되어 변경된 무기를 정렬된 리스트에서 찾아 계속 선택 상태 유지
                int newIndex = BlacksmithManager.Instance.ownedWeapons.IndexOf(nextWeapon);
                selectedWeaponIndex = newIndex;
                selectedWeapon = (newIndex >= 0) ? nextWeapon : null;
            }
            else if (result == WeaponEnhanceResult.DestroyedFailure)
            {
                // 파괴 시 선택 해제 (아무 슬롯도 선택하지 않음)
                selectedWeapon = null;
                selectedWeaponIndex = -1;
            }
        }
        else
        {
            if (selectedGem == null || selectedGemIndex < 0) return;
            if (selectedGem.nextLevelGem == null)
            {
                UI_ToastPopup.Show("Notice_Max_Upgrade");
                return;
            }

            GemItemData nextGem = selectedGem.nextLevelGem;
            GemEnhanceResult result = BlacksmithManager.Instance.EnhanceGemAtIndex(selectedGemIndex);

            if (result == GemEnhanceResult.Success)
            {
                // 성공 시 강화되어 변경된 보석을 정렬된 리스트에서 찾아 계속 선택 상태 유지
                int newIndex = BlacksmithManager.Instance.ownedGems.IndexOf(nextGem);
                selectedGemIndex = newIndex;
                selectedGem = (newIndex >= 0) ? nextGem : null;
            }
            else if (result == GemEnhanceResult.Destroyed)
            {
                // 파괴 시 선택 해제 (아무 슬롯도 선택하지 않음)
                selectedGem = null;
                selectedGemIndex = -1;
            }
        }

        RefreshAllUI();
    }

    private void OnClickSell()
    {
        if (BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            if (selectedWeapon == null || selectedWeaponIndex < 0) return;
            Debug.Log($"[UI_BlacksmithPanel] 무기 판매: {selectedWeapon.weaponName}");
            selectedWeapon = null;
            selectedWeaponIndex = -1;
        }
        else
        {
            if (selectedGem == null || selectedGemIndex < 0) return;
            BlacksmithManager.Instance.SellGemAtIndex(selectedGemIndex);
            selectedGem = null;
            selectedGemIndex = -1;
        }

        RefreshAllUI();
    }

    #endregion
}
