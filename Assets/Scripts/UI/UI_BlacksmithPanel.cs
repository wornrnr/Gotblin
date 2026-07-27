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
            if (selectedWeapon == null || !BlacksmithManager.Instance.ownedWeapons.Contains(selectedWeapon))
            {
                selectedWeapon = BlacksmithManager.Instance.equippedWeapon;
            }
            RefreshWeaponDetail(selectedWeapon);
            BuildWeaponGrid();
        }
        else
        {
            if (selectedGem == null || !BlacksmithManager.Instance.ownedGems.Contains(selectedGem))
            {
                if (BlacksmithManager.Instance.ownedGems.Count > 0)
                    selectedGem = BlacksmithManager.Instance.ownedGems[0];
                else
                    selectedGem = null;
            }
            RefreshGemDetail(selectedGem);
            BuildGemGrid();
        }
    }

    /// <summary>
    /// 선택 무기 상세 정보 및 3색 확률 인디케이터 (🟢, 🟡, 🔴) 갱신
    /// </summary>
    private void RefreshWeaponDetail(WeaponItemData weapon)
    {
        if (weapon == null)
        {
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
            bool isSelected = (wData != null && wData == selectedWeapon);
            slot.Setup(wData, isSelected, OnSlotSelected);
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
            bool isSelected = (gData != null && gData == selectedGem);
            slot.SetupGem(gData, isSelected, OnSlotSelected);
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
                selectedWeapon = slot.BoundWeapon;
                RefreshAllUI();
            }
        }
        else
        {
            if (slot.BoundGem != null)
            {
                selectedGem = slot.BoundGem;
                RefreshAllUI();
            }
        }
    }

    #region Tab & Action Handlers

    public void SwitchToWeaponTab()
    {
        isWeaponTab = true;
        if (tab1Highlight != null) tab1Highlight.gameObject.SetActive(true);
        if (tab2Highlight != null) tab2Highlight.gameObject.SetActive(false);
        RefreshAllUI();
    }

    public void SwitchToGemTab()
    {
        isWeaponTab = false;
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
            if (selectedWeapon == null) return;
            bool useProtection = useProtectionToggle != null && useProtectionToggle.isOn;
            WeaponEnhanceResult result = BlacksmithManager.Instance.EnhanceWeapon(selectedWeapon, useProtection);
            if (result == WeaponEnhanceResult.DestroyedFailure) selectedWeapon = null;
        }
        else
        {
            if (selectedGem == null) return;
            GemEnhanceResult result = BlacksmithManager.Instance.EnhanceGem(selectedGem);
            if (result == GemEnhanceResult.Destroyed) selectedGem = null;
        }

        RefreshAllUI();
    }

    private void OnClickSell()
    {
        if (BlacksmithManager.Instance == null) return;

        if (isWeaponTab)
        {
            if (selectedWeapon == null) return;
            Debug.Log($"[UI_BlacksmithPanel] 무기 판매: {selectedWeapon.weaponName}");
        }
        else
        {
            if (selectedGem == null) return;
            BlacksmithManager.Instance.SellGem(selectedGem);
            selectedGem = null;
        }

        RefreshAllUI();
    }

    #endregion
}
