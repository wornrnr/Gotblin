using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 부락 건설 화면 내 개별 건물 슬롯의 정보와 건설 상태 게이지, 오프라인 시간 경과 결과 등을
/// 실시간 및 활성화(OnEnable) 시점에 맞추어 매끄럽게 표현하는 개별 건물 UI 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_BuildingSlot : MonoBehaviour
{
    [Header("UI 구성 컴포넌트")]
    [Tooltip("건물 이름 및 현재 레벨을 출력할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI buildingNameText;

    [Tooltip("건설 및 레벨업을 진행시킬 업그레이드 버튼 컴포넌트입니다.")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("버튼 내부에 업그레이드에 필요한 골드 비용을 표기할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("남은 건설 시간을 시각적 진행도로 표현할 슬라이더 UI 컴포넌트입니다.")]
    [SerializeField] private Slider buildProgressSlider;

    [Tooltip("남은 시간(분:초)을 디지털 시계(00:00)로 표시할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("연동 건물 식별 ID")]
    [Tooltip("이 슬롯이 연계 및 추적할 건물의 고유 식별자 ID입니다. (예: TownHall, Barracks)")]
    [SerializeField] private string targetBuildingID;

    private BuildingData cachedData;
    private BuildingInstance cachedInstance;

    private void Awake()
    {
        // 1. 업그레이드 버튼 클릭 리스너 설정
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
    }

    private void Start()
    {
        // 2. 씬에 배치되었을 때 미리 기입된 ID가 존재하면 초기 셋업
        if (!string.IsNullOrEmpty(targetBuildingID))
        {
            SetupSlot(targetBuildingID);
        }
    }

    /// <summary>
    /// 슬롯에 추적 대상 건물 ID를 입력하고 초기 데이터 셋업을 완료합니다.
    /// </summary>
    public void SetupSlot(string buildingID)
    {
        targetBuildingID = buildingID;
        
        if (BuildingManager.Instance != null)
        {
            cachedData = BuildingManager.Instance.GetBuildingData(buildingID);
            cachedInstance = BuildingManager.Instance.GetBuildingInstance(buildingID);
        }

        RefreshUI();
    }

    /// <summary>
    /// UI가 활성화될 때(OnEnable) 오프라인 시간이 반영된 최신 데이터를 즉시 반영하도록 리프레시를 작동합니다.
    /// </summary>
    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (cachedData == null || cachedInstance == null) return;

        // 3. 실시간 건설 진행 상황의 UI 동기화 분기 처리
        if (cachedInstance.isConstructing)
        {
            // 진행 정보 컴포넌트 노출
            if (buildProgressSlider != null && !buildProgressSlider.gameObject.activeSelf)
                buildProgressSlider.gameObject.SetActive(true);
            
            if (timerText != null && !timerText.gameObject.activeSelf)
                timerText.gameObject.SetActive(true);

            // 테이블 기반 실시간 진행 시간 계산
            int currentLevelIndex = Mathf.Clamp(cachedInstance.currentLevel, 0, cachedData.MaxLevel);
            float totalBuildTime = 0f;
            if (cachedData.levelSettings != null && cachedData.levelSettings.Count > currentLevelIndex)
            {
                totalBuildTime = cachedData.levelSettings[currentLevelIndex].buildDuration;
            }
            float elapsed = totalBuildTime - cachedInstance.remainingTime;

            // 슬라이더 진행 게이지 동기화
            if (buildProgressSlider != null)
            {
                buildProgressSlider.value = Mathf.Clamp01(elapsed / totalBuildTime);
            }

            // 남은 시간 00:00 문자열 포맷 변환
            if (timerText != null)
            {
                float timeToDisplay = Mathf.Max(0f, cachedInstance.remainingTime);
                int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
                int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            // 건설 중인 상태일 때는 업그레이드 버튼 비활성화
            if (upgradeButton != null && upgradeButton.interactable)
            {
                upgradeButton.interactable = false;
            }
        }
        else
        {
            // 건설이 진행 중이지 않을 때 진행률 컴포넌트 비활성화
            if (buildProgressSlider != null && buildProgressSlider.gameObject.activeSelf)
                buildProgressSlider.gameObject.SetActive(false);

            if (timerText != null && timerText.gameObject.activeSelf)
                timerText.gameObject.SetActive(false);

            // 완공 상태 리프레시
            RefreshUI();
        }
    }

    /// <summary>
    /// 레벨, 소모 비용, 버튼 활성성 상태 등 건물의 텍스트 요소를 최신화합니다.
    /// </summary>
    public void RefreshUI()
    {
        if (BuildingManager.Instance == null) return;

        // 런타임 캐시 복원
        if (cachedData == null) cachedData = BuildingManager.Instance.GetBuildingData(targetBuildingID);
        cachedInstance = BuildingManager.Instance.GetBuildingInstance(targetBuildingID);

        if (cachedData == null || cachedInstance == null) return;

        // 1. 이름 및 현재 레벨 갱신
        if (buildingNameText != null)
        {
            if (cachedInstance.currentLevel >= cachedData.MaxLevel)
            {
                buildingNameText.text = $"{cachedData.BuildingName} (Lv. {cachedInstance.currentLevel} / MAX)";
            }
            else
            {
                buildingNameText.text = $"{cachedData.BuildingName} (Lv. {cachedInstance.currentLevel})";
            }
        }

        // 2. 레벨 상태에 따라 업그레이드 버튼 잠금 및 골드 비용 표기 분기
        if (cachedInstance.currentLevel >= cachedData.MaxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (upgradeButton != null && upgradeButton.interactable) upgradeButton.interactable = false;
        }
        else
        {
            int currentLevelIndex = Mathf.Clamp(cachedInstance.currentLevel, 0, cachedData.MaxLevel);
            int requiredGold = 0;
            if (cachedData.levelSettings != null && cachedData.levelSettings.Count > currentLevelIndex)
            {
                requiredGold = cachedData.levelSettings[currentLevelIndex].upgradeCost;
            }

            if (costText != null)
            {
                costText.text = $"{requiredGold:N0} G";
            }

            if (upgradeButton != null)
            {
                // 건설 중이 아닐 때만 유저가 클릭할 수 있게 개방
                upgradeButton.interactable = !cachedInstance.isConstructing;
            }
        }
    }

    /// <summary>
    /// 업그레이드 클릭 시 BuildingManager에 건설 개시를 요청합니다.
    /// </summary>
    private void OnUpgradeButtonClicked()
    {
        if (string.IsNullOrEmpty(targetBuildingID) || BuildingManager.Instance == null) return;

        BuildingManager.Instance.StartConstruction(targetBuildingID);
        RefreshUI();
    }
}
