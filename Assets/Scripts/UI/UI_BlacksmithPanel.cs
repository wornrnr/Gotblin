using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코어2 대장간 건물 클릭 시 노출되는 대장간 콘텐츠 메인 UI 팝업 컨트롤러입니다.
/// 대장간 해금 조건 검증, 무기 탭/보석 탭 전환, 무기 장착 및 파괴 방지 강화, 보석 강화 및 판매 기능을 제공합니다.
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

    [Header("무기 탭 UI 요소")]
    [SerializeField] private Image equippedWeaponIcon;
    [SerializeField] private TextMeshProUGUI equippedWeaponNameText;
    [SerializeField] private TextMeshProUGUI equippedWeaponOptionsText;

    [SerializeField] private TextMeshProUGUI ironIngotCountText;
    [SerializeField] private TextMeshProUGUI protectionItemCountText;

    [SerializeField] private Toggle useProtectionToggle;
    [SerializeField] private TextMeshProUGUI selectedWeaponInfoText;
    [SerializeField] private Button equipBtn;
    [SerializeField] private Button enhanceWeaponBtn;

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
        if (equipBtn != null) equipBtn.onClick.AddListener(OnClickEquipWeapon);
        if (enhanceWeaponBtn != null) enhanceWeaponBtn.onClick.AddListener(OnClickEnhanceWeapon);
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
        if (equipBtn != null) equipBtn.onClick.RemoveListener(OnClickEquipWeapon);
        if (enhanceWeaponBtn != null) enhanceWeaponBtn.onClick.RemoveListener(OnClickEnhanceWeapon);
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
            lockNoticeText.text = isUnlocked ? string.Empty : "대장간 건물을 먼저 건설해야 합니﻿다!\n(코어2 대장간 건물 Lv 1 이상 필요)";
        }

        if (BlacksmithManager.Instance == null) return;

        // 재화 수량 갱신
        if (ironIngotCountText != null)
        {
            ironIngotCountText.text = $"철 주괴: {BlacksmithManager.Instance.ironIngotCount}개";
        }
        if (protectionItemCountText != null)
        {
            protectionItemCountText.text = $"파괴 방지권: {BlacksmithManager.Instance.protectionItemCount}개";
        }

        // 장착 중인 무기 정보 갱신
        var eqWeapon = BlacksmithManager.Instance.equippedWeapon;
        if (eqWeapon != null)
        {
            if (equippedWeaponIcon != null)
            {
                equippedWeaponIcon.sprite = eqWeapon.iconSprite != null ? eqWeapon.iconSprite : eqWeapon.visualSprite;
                equippedWeaponIcon.gameObject.SetActive(true);
            }
            if (equippedWeaponNameText != null)
            {
                equippedWeaponNameText.text = $"{eqWeapon.weaponName} (Lv.{eqWeapon.grade})";
            }
            if (equippedWeaponOptionsText != null)
            {
                equippedWeaponOptionsText.text = BuildWeaponOptionsString(eqWeapon);
            }
        }
        else
        {
            if (equippedWeaponIcon != null) equippedWeaponIcon.gameObject.SetActive(false);
            if (equippedWeaponNameText != null) equippedWeaponNameText.text = "장착된 무기 없음";
            if (equippedWeaponOptionsText != null) equippedWeaponOptionsText.text = "-";
        }

        // 선택된 무기 정보 갱신
        RefreshSelectedWeaponInfo();
        RefreshSelectedGemInfo();
    }

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

    public void SelectWeapon(WeaponItemData weapon)
    {
        selectedWeapon = weapon;
        RefreshSelectedWeaponInfo();
    }

    public void SelectGem(GemItemData gem)
    {
        selectedGem = gem;
        RefreshSelectedGemInfo();
    }

    private void RefreshSelectedWeaponInfo()
    {
        if (selectedWeaponInfoText == null) return;

        if (selectedWeapon != null)
        {
            float failRatio = Mathf.Max(0f, 1f - (selectedWeapon.upgradeSuccessRate + selectedWeapon.upgradeKeepRate));
            selectedWeaponInfoText.text = $"[선택 무기]: {selectedWeapon.weaponName} (Lv.{selectedWeapon.grade})\n" +
                                          $"{BuildWeaponOptionsString(selectedWeapon)}\n" +
                                          $"강화 필요 철 주괴: {selectedWeapon.requiredIronIngot}개\n" +
                                          $"확률 - 성공: {selectedWeapon.upgradeSuccessRate * 100:F0}% / 유지: {selectedWeapon.upgradeKeepRate * 100:F0}% / 파괴: {failRatio * 100:F0}%";
        }
        else
        {
            selectedWeaponInfoText.text = "선택된 무기가 없습니다.";
        }
    }

    private void RefreshSelectedGemInfo()
    {
        if (selectedGemInfoText == null) return;

        if (selectedGem != null)
        {
            float failRatio = Mathf.Max(0f, 1f - (selectedGem.upgradeSuccessRate + selectedGem.upgradeKeepRate));
            selectedGemInfoText.text = $"[선택 보석]: {selectedGem.gemName} (Lv.{selectedGem.level})\n" +
                                       $"판매가: {selectedGem.sellPrice:N0} Gold\n" +
                                       $"강화 성공률: {selectedGem.upgradeSuccessRate * 100:F0}% / 유지: {selectedGem.upgradeKeepRate * 100:F0}% / 파괴: {failRatio * 100:F0}%";
        }
        else
        {
            selectedGemInfoText.text = "선택된 보석이 없습니다.";
        }
    }

    private string BuildWeaponOptionsString(WeaponItemData weapon)
    {
        if (weapon == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.Append($"기본 옵션: 공격력 +{weapon.baseATK}");

        if (weapon.additionalOptions != null && weapon.additionalOptions.Count > 0)
        {
            sb.Append(" | 추가 옵션: ");
            for (int i = 0; i < weapon.additionalOptions.Count; i++)
            {
                var opt = weapon.additionalOptions[i];
                switch (opt.optionType)
                {
                    case WeaponOptionType.ATKPercent:
                        sb.Append($"공격력 +{opt.value * 100:F0}% ");
                        break;
                    case WeaponOptionType.TargetCount:
                        sb.Append($"타겟수 +{opt.value:F0} ");
                        break;
                    case WeaponOptionType.AttackSpeed:
                        sb.Append($"공속 +{opt.value:F2} ");
                        break;
                    case WeaponOptionType.HPPercent:
                        sb.Append($"생명력 +{opt.value * 100:F0}% ");
                        break;
                    case WeaponOptionType.MoveSpeed:
                        sb.Append($"이속 +{opt.value:F1} ");
                        break;
                }
            }
        }
        return sb.ToString();
    }

    private void OnClickEquipWeapon()
    {
        if (selectedWeapon != null && BlacksmithManager.Instance != null)
        {
            BlacksmithManager.Instance.EquipWeapon(selectedWeapon);
            Debug.Log($"<color=cyan>[UI_BlacksmithPanel] '{selectedWeapon.weaponName}' 무기를 히어로 고블린에게 장착하였습니다!</color>");
        }
    }

    private void OnClickEnhanceWeapon()
    {
        if (selectedWeapon != null && BlacksmithManager.Instance != null)
        {
            bool useProtection = useProtectionToggle != null && useProtectionToggle.isOn;
            var res = BlacksmithManager.Instance.EnhanceWeapon(selectedWeapon, useProtection);

            Debug.Log($"<color=yellow>[UI_BlacksmithPanel] 무기 강화 결과: {res}</color>");
        }
    }

    private void OnClickEnhanceGem()
    {
        if (selectedGem != null && BlacksmithManager.Instance != null)
        {
            var res = BlacksmithManager.Instance.EnhanceGem(selectedGem);
            Debug.Log($"<color=yellow>[UI_BlacksmithPanel] 보석 강화 결과: {res}</color>");
        }
    }

    private void OnClickSellGem()
    {
        if (selectedGem != null && BlacksmithManager.Instance != null)
        {
            bool sold = BlacksmithManager.Instance.SellGem(selectedGem);
            if (sold)
            {
                Debug.Log($"<color=green>[UI_BlacksmithPanel] 보석 '{selectedGem.gemName}' 판매 완료!</color>");
                selectedGem = null;
            }
        }
    }
}
