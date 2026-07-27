using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 부락 건설 코어2의 고블린 본부(TownHall / HQ) 건물에 대응하는 메인 UI 팝업 컨트롤러입니다.
/// 본부 레벨, 생산 골드 부스트, 총 인구 수, 업그레이드 비용 정보를 표기하고 조작을 지원합니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_HQPanel : UI_BasePopup
{
    [Header("고블린 본부 뷰 요소")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI goldProductionBoostText;
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Button hqUpgradeButton;

    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(popupID)) popupID = "HQ";

        if (hqUpgradeButton != null)
        {
            hqUpgradeButton.onClick.RemoveAllListeners();
            hqUpgradeButton.onClick.AddListener(OnClickUpgradeHQ);
        }
    }

    private void OnEnable()
    {
        RefreshAllUI();
    }

    public override void RefreshAllUI()
    {
        if (BuildingManager.Instance == null) return;

        var hqData = BuildingManager.Instance.GetBuildingData("HQ");
        if (hqData == null) hqData = BuildingManager.Instance.GetBuildingData("TownHall");

        var hqInst = BuildingManager.Instance.GetBuildingInstance("HQ");
        if (hqInst == null) hqInst = BuildingManager.Instance.GetBuildingInstance("TownHall");

        int currentLevel = hqInst != null ? hqInst.currentLevel : 0;
        int maxLevel = hqData != null ? hqData.MaxLevel : 10;

        if (levelText != null)
        {
            levelText.text = $"🏰 고블린 본부 (Lv.{currentLevel} / {maxLevel})";
        }

        if (goldProductionBoostText != null)
        {
            float boostPercent = currentLevel * 15f; // 레벨당 15% 생산량 증가
            goldProductionBoostText.text = $"전체 골드 생산량: +{boostPercent:F0}%";
        }

        if (populationText != null)
        {
            int maxPop = 10 + (currentLevel * 5);
            populationText.text = $"수용 가능한 고블린: {maxPop}명";
        }

        if (upgradeCostText != null && hqData != null && hqData.levelSettings != null && hqData.levelSettings.Count > currentLevel)
        {
            if (currentLevel >= maxLevel)
            {
                upgradeCostText.text = "MAX";
                if (hqUpgradeButton != null) hqUpgradeButton.interactable = false;
            }
            else
            {
                int cost = hqData.levelSettings[currentLevel].upgradeCost;
                upgradeCostText.text = $"{cost:N0} G";
                if (hqUpgradeButton != null) hqUpgradeButton.interactable = true;
            }
        }
    }

    private void OnClickUpgradeHQ()
    {
        if (BuildingManager.Instance == null) return;

        string idToUpgrade = "HQ";
        if (BuildingManager.Instance.GetBuildingData("HQ") == null) idToUpgrade = "TownHall";

        BuildingManager.Instance.StartConstruction(idToUpgrade);
        RefreshAllUI();
    }
}
