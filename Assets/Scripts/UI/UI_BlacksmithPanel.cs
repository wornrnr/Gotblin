using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stitch 대장간 UI (최종 수정본) 시안 기반의 대장간 콘텐츠 메인 UI 컨트롤러입니다.
/// 대장간 해금 조건 검증, 3열 강화 확률(성공/실패/파괴) 표시, 스탯 패널, 강화석/골드 소모 재료 뷰어,
/// 불꽃 이펙트(UI_BlacksmithEmberFX) 및 무기/보석 탭 전환 및 강화를 지원합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BlacksmithPanel : MonoBehaviour
{
    [Header("해금 및 가림막 UI")]
    [Tooltip("대장간 건물 미건설 시 조작을 막는 해금 경고 가림막 오버레이 패널입니다.")]
    [SerializeField] private GameObject lockedOverlayPanel;

    [Tooltip("해금 경고 메시지 텍스트 컴포넌트입니다.")]
    [SerializeField] private TextMeshProUGUI lockNoticeText;

    [Header("탭 뷰 패널 참조")]
    [SerializeField] private GameObject weaponTabPanel;
    [SerializeField] private GameObject gemTabPanel;
    [SerializeField] private Button weaponTabBtn;
    [SerializeField] private Button gemTabBtn;

    [Header("Stitch Hero Forge Area (무기 슬롯 및 확률)")]
    [SerializeField] private Image equippedWeaponIcon;
    [SerializeField] private TextMeshProUGUI equippedWeaponNameText;
    [SerializeField] private TextMeshProUGUI weaponGradeLevelText; //예: +7

    [Header("Stitch 3-Column Rate Layout (확률 표시)")]
    [Tooltip("강화 성공 확률 텍스트 (예: 24%)")]
    [SerializeField] private TextMeshProUGUI successRateText;

    [Tooltip("강화 실패(보존) 확률 텍스트 (예: 66%)")]
    [SerializeField] private TextMeshProUGUI keepRateText;

    [Tooltip("강화 파괴 확률 텍스트 (예: 10%)")]
    [SerializeField] private TextMeshProUGUI destroyRateText;

    [Header("Stitch Stats Panel (스탯 단일 행)")]
    [SerializeField] private TextMeshProUGUI baseATKStatText;
    [SerializeField] private TextMeshProUGUI fireOptionStatText;
    [SerializeField] private TextMeshProUGUI durabilityStatText;

    [Header("Stitch Materials Required (강화 필요 재료)")]
    [Tooltip("강화석 소지 수량 / 필요 수량 텍스트 (예: 12 / 5)")]
    [SerializeField] private TextMeshProUGUI materialCountText;

    [Tooltip("골드 소지 수량 / 필요 수량 텍스트 (예: 24,500 / 5,000)")]
    [SerializeField] private TextMeshProUGUI goldCountText;

    [Header("Stitch 파티클 및 버튼")]
    [SerializeField] private UI_BlacksmithEmberFX emberFX;
    [SerializeField] private Toggle useProtectionToggle;
    [SerializeField] private Button enhanceWeaponBtn;
    [SerializeField] private Button sellWeaponBtn;

    [Header("보석 탭 UI 요소")]
    [SerializeField] private TextMeshProUGUI selectedGemInfoText;
    [SerializeField] private Button enhanceGemBtn;
    [SerializeField] private Button sellGemBtn;

    // 현재 선택된 항목
    private WeaponItemData selectedWeapon;
    private GemItemData selectedGem;

    private void OnEnable()
    {
        BlacksmithManager.OnInventoryUpdated += RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged += RefreshAllUI;

        if (weaponTabBtn != null) weaponTabBtn.onClick.AddListener(SwitchToWeaponTab);
        if (gemTabBtn != null) gemTabBtn.onClick.AddListener(SwitchToGemTab);
        if (enhanceWeaponBtn != null) enhanceWeaponBtn.onClick.AddListener(OnClickEnhanceWeapon);
        if (sellWeaponBtn != null) sellWeaponBtn.onClick.AddListener(OnClickSellWeapon);
        if (enhanceGemBtn != null) enhanceGemBtn.onClick.AddListener(OnClickEnhanceGem);
        if (sellGemBtn != null) sellGemBtn.onClick.AddListener(OnClickSellGem);

        SwitchToWeaponTab();
        RefreshAllUI();
    }

    private void OnDisable()
    {
        BlacksmithManager.OnInventoryUpdated -= RefreshAllUI;
        BlacksmithManager.OnEquippedWeaponChanged -= RefreshAllUI;

        if (weaponTabBtn != null) weaponTabBtn.onClick.RemoveListener(SwitchToWeaponTab);
        if (gemTabBtn != null) gemTabBtn.onClick.RemoveListener(SwitchToGemTab);
        if (enhanceWeaponBtn != null) enhanceWeaponBtn.onClick.RemoveListener(OnClickEnhanceWeapon);
        if (sellWeaponBtn != null) sellWeaponBtn.onClick.RemoveListener(OnClickSellWeapon);
        if (enhanceGemBtn != null) enhanceGemBtn.onClick.RemoveListener(OnClickEnhanceGem);
        if (sellGemBtn != null) sellGemBtn.onClick.RemoveListener(OnClickSellGem);
    }

    /// <summary>
    /// 대장간 해금 유무 체크 및 인벤토리/재화 정보 일괄 UI 갱신
    /// </summary>
    public void RefreshAllUI()
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

        // 1. 선택 무기 또는 장착 무기 할당
        if (selectedWeapon == null || !BlacksmithManager.Instance.ownedWeapons.Contains(selectedWeapon))
        {
            selectedWeapon = BlacksmithManager.Instance.equippedWeapon;
        }

        // 2. 무기 정보 및 3열 확률 레이아웃 갱신
        RefreshWeaponInfo(selectedWeapon);

        // 3. 재화 수량 (강화석 & 골드) 갱신
        RefreshMaterialAndCurrency(selectedWeapon);
    }

    /// <summary>
    /// 무기 정보, 레벨, 스탯 및 3열 확률 (성공/실패/파괴) UI 갱신
    /// </summary>
    private void RefreshWeaponInfo(WeaponItemData weapon)
    {
        if (weapon == null)
        {
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "선택된 무기 없음";
            if (weaponGradeLevelText != null) weaponGradeLevelText.text = "+0";
            if (successRateText != null) successRateText.text = "0%";
            if (keepRateText != null) keepRateText.text = "0%";
            if (destroyRateText != null) destroyRateText.text = "0%";
            if (baseATKStatText != null) baseATKStatText.text = "공격력: 0";
            if (fireOptionStatText != null) fireOptionStatText.text = "화염: 0";
            if (durabilityStatText != null) durabilityStatText.text = "내구: 0/100";
            return;
        }

        // 비주얼 & 이름
        if (equippedWeaponIcon != null && weapon.iconSprite != null)
        {
            equippedWeaponIcon.sprite = weapon.iconSprite;
            equippedWeaponIcon.gameObject.SetActive(true);
        }
        if (equippedWeaponNameText != null)
        {
            equippedWeaponNameText.text = weapon.weaponName;
        }
        if (weaponGradeLevelText != null)
        {
            weaponGradeLevelText.text = $"+{weapon.grade}";
        }

        // Stitch 3-Column Rates
        float successRatio = Mathf.Clamp01(weapon.upgradeSuccessRate);
        float keepRatio = Mathf.Clamp01(weapon.upgradeKeepRate);
        float destroyRatio = Mathf.Clamp01(1.0f - (successRatio + keepRatio));

        if (successRateText != null) successRateText.text = $"{Mathf.RoundToInt(successRatio * 100f)}%";
        if (keepRateText != null) keepRateText.text = $"{Mathf.RoundToInt(keepRatio * 100f)}%";
        if (destroyRateText != null) destroyRateText.text = $"{Mathf.RoundToInt(destroyRatio * 100f)}%";

        // Stats Panel
        if (baseATKStatText != null)
        {
            int bonus = weapon.grade * 5;
            baseATKStatText.text = $"공격력: {weapon.baseATK} <color=#e9c349>(+{bonus})</color>";
        }
        if (fireOptionStatText != null)
        {
            fireOptionStatText.text = $"화염: {weapon.grade * 15}";
        }
        if (durabilityStatText != null)
        {
            durabilityStatText.text = "내구: 85/100";
        }
    }

    /// <summary>
    /// 필요 재료(강화석) 및 골드 수량 뷰어 갱신
    /// </summary>
    private void RefreshMaterialAndCurrency(WeaponItemData weapon)
    {
        int currentIngots = BlacksmithManager.Instance != null ? BlacksmithManager.Instance.ironIngotCount : 0;
        int requiredIngots = weapon != null ? weapon.requiredIronIngot : 5;

        if (materialCountText != null)
        {
            materialCountText.text = $"{currentIngots} / {requiredIngots}";
        }

        int currentGold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 24500;
        int requiredGold = 5000;

        if (goldCountText != null)
        {
            goldCountText.text = $"{currentGold:N0} / {requiredGold:N0}";
        }
    }

    /// <summary>
    /// 인벤토리 항목 클릭 시 선택된 무기를 변경
    /// </summary>
    public void SelectWeapon(WeaponItemData weapon)
    {
        selectedWeapon = weapon;
        RefreshWeaponInfo(selectedWeapon);
        RefreshMaterialAndCurrency(selectedWeapon);
    }

    #region Tab & Button Event Callbacks

    public void SwitchToWeaponTab()
    {
        if (weaponTabPanel != null) weaponTabPanel.SetActive(true);
        if (gemTabPanel != null) gemTabPanel.SetActive(false);
    }

    public void SwitchToGemTab()
    {
        if (weaponTabPanel != null) weaponTabPanel.SetActive(false);
        if (gemTabPanel != null) gemTabPanel.SetActive(true);
    }

    private void OnClickEnhanceWeapon()
    {
        if (selectedWeapon == null || BlacksmithManager.Instance == null) return;

        bool useProtection = useProtectionToggle != null && useProtectionToggle.isOn;

        // 불꽃 FX 및 안빌 바운스 효과 트리거
        if (emberFX != null)
        {
            emberFX.TriggerEnhanceSparkFX();
        }

        WeaponEnhanceResult result = BlacksmithManager.Instance.EnhanceWeapon(selectedWeapon, useProtection);

        switch (result)
        {
            case WeaponEnhanceResult.Success:
                Debug.Log("[UI_BlacksmithPanel] 🎉 무기 강화 성공!");
                break;
            case WeaponEnhanceResult.Keep:
                Debug.Log("[UI_BlacksmithPanel] 🛡️ 무기 강화 단계 유지");
                break;
            case WeaponEnhanceResult.ProtectedFailure:
                Debug.Log("[UI_BlacksmithPanel] 🔰 파괴 방지권 작동 - 무기 파괴 방지됨");
                break;
            case WeaponEnhanceResult.DestroyedFailure:
                Debug.LogWarning("[UI_BlacksmithPanel] 💥 무기 파괴 소멸!");
                selectedWeapon = null;
                break;
        }

        RefreshAllUI();
    }

    private void OnClickSellWeapon()
    {
        if (selectedWeapon == null || BlacksmithManager.Instance == null) return;
        Debug.Log($"[UI_BlacksmithPanel] 무기 판매 실행: {selectedWeapon.weaponName}");
    }

    private void OnClickEnhanceGem()
    {
        if (selectedGem == null || BlacksmithManager.Instance == null) return;
        BlacksmithManager.Instance.EnhanceGem(selectedGem);
        RefreshAllUI();
    }

    private void OnClickSellGem()
    {
        if (selectedGem == null || BlacksmithManager.Instance == null) return;
        BlacksmithManager.Instance.SellGem(selectedGem);
        selectedGem = null;
        RefreshAllUI();
    }

    #endregion
}
