using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 부락 월드 맵에 실물 배치되어 있는 각 건물 오브젝트들의 비주얼(레벨, 텍스트, 스프라이트) 및
/// 건설 남은 시간 게이지를 실시간 갱신하고 클릭 이벤트를 처리하는 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
public class UI_WorldBuildingObject : MonoBehaviour
{
    [Header("건물 핵심 매핑 설정")]
    [Tooltip("이 오브젝트가 연계할 건물의 고유 식별자 ID입니다. (예: TownHall, Barracks)")]
    public string buildingID;

    [Tooltip("건물 클릭을 감지할 버튼 컴포넌트입니다.")]
    [SerializeField] private Button buildingButton;

    [Tooltip("건물의 외형 스프라이트 이미지를 실시간으로 교체할 버튼의 Image 컴포넌트입니다.")]
    [SerializeField] private Image buildingButtonImage;

    [Tooltip("건물 명칭과 레벨을 실시간 표기할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI infoText;

    [Tooltip("업그레이드 버튼 내부에 들어갈 비용 또는 상태(MAX)를 표시할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI costText;

    [Header("Construction UI (건설 게이지)")]
    [Tooltip("건설 진행도 슬라이더와 텍스트를 담고 있는 부모 그룹 오브젝트입니다.")]
    [SerializeField] private GameObject progressGroup;

    [Tooltip("건설 진행도를 보여줄 슬라이더 컴포넌트입니다.")]
    [SerializeField] private Slider buildSlider;

    [Tooltip("남은 건설 시간(00:00)을 표기할 TextMeshProUGUI입니다.")]
    [SerializeField] private TextMeshProUGUI timerText;

    private BuildingData cachedData;
    private BuildingInstance cachedInstance;

    private void Start()
    {
        // 1. 버튼 클릭 리스너 등록
        if (buildingButton != null)
        {
            buildingButton.onClick.RemoveAllListeners();
            buildingButton.onClick.AddListener(OnBuildingClicked);
        }

        // 2. 초기 기획 스펙 및 인스턴스 캐싱 시도
        InitializeData();
    }

    private void Update()
    {
        // 데이터가 아직 초기화되지 않았다면 매 프레임 재시도 (매니저 초기화 시점 차이 방어)
        if (cachedData == null || cachedInstance == null)
        {
            InitializeData();
            return;
        }

        // 3. 건물 레벨 정보 및 비주얼 이미지 실시간 갱신
        UpdateInfoText();

        // 4. 건설 진행 상태 실시간 동기화
        UpdateConstructionProgress();
    }

    /// <summary>
    /// BuildingManager로부터 건물의 기획 정보와 실시간 데이터 인스턴스를 가져옵니다.
    /// </summary>
    private void InitializeData()
    {
        if (BuildingManager.Instance == null || string.IsNullOrEmpty(buildingID)) return;

        cachedData = BuildingManager.Instance.GetBuildingData(buildingID);
        cachedInstance = BuildingManager.Instance.GetBuildingInstance(buildingID);
    }

    /// <summary>
    /// 레벨에 따른 텍스트 출력 형태와 외형 스프라이트를 테이블 데이터 기반으로 실시간 갱신합니다.
    /// </summary>
    private void UpdateInfoText()
    {
        if (cachedData == null || cachedInstance == null) return;

        string buildingName = cachedData.BuildingName;
        int level = cachedInstance.currentLevel;
        int maxLevel = cachedData.MaxLevel;

        // [코딩 제약 조건] 인덱스 참조 시 바운더리 오버 크래시 안전 클램핑 방어 처리
        int currentLevelIndex = Mathf.Clamp(level, 0, maxLevel);

        if (cachedData.levelSettings != null && cachedData.levelSettings.Count > currentLevelIndex)
        {
            BuildingLevelEnv currentEnv = cachedData.levelSettings[currentLevelIndex];

            // 1. 레벨별 가변 스프라이트 이미지 즉각 교체 적용
            if (buildingButtonImage != null && currentEnv.visualSprite != null)
            {
                buildingButtonImage.sprite = currentEnv.visualSprite;
            }

            // 2. 이름 및 레벨 텍스트 표기 분기
            if (infoText != null)
            {
                if (level >= maxLevel)
                {
                    infoText.text = $"{buildingName} (Lv.{level} - MAX)";
                }
                else
                {
                    infoText.text = level <= 0 ? $"{buildingName} (미건설)" : $"{buildingName} (Lv.{level})";
                }
            }

            // 3. 최고 레벨 여부에 따른 비용 텍스트 및 클릭 잠금 처리 (대장간 건물은 완공 후에도 팝업 진입을 위해 버튼 클릭 허용)
            if (level >= maxLevel)
            {
                if (costText != null) costText.text = "MAX";
                if (buildingButton != null && !cachedInstance.isConstructing)
                {
                    // 대장간(Blacksmith) 건물은 완공(MAX) 후에도 클릭하여 팝업 창을 열어야 하므로 interactable 유지
                    buildingButton.interactable = (buildingID == "Blacksmith") || false;
                }
            }
            else
            {
                if (costText != null)
                {
                    costText.text = $"{currentEnv.upgradeCost:N0} G";
                }

                // 건설 중이 아닐 때만 유저 드래그 방어 체크와 비례해 버튼 오픈
                if (buildingButton != null)
                {
                    buildingButton.interactable = !cachedInstance.isConstructing;
                }
            }
        }
    }

    /// <summary>
    /// 건설 중 상태를 감지하여 게이지 슬라이더와 포맷팅 타이머를 실시간 업데이트합니다.
    /// </summary>
    private void UpdateConstructionProgress()
    {
        if (cachedInstance == null || cachedData == null) return;

        if (cachedInstance.isConstructing)
        {
            // 게이지 연출 활성화
            if (progressGroup != null && !progressGroup.activeSelf)
            {
                progressGroup.SetActive(true);
            }

            // [코딩 제약 조건] 레벨 인덱스 바운더리 체크
            int currentLevelIndex = Mathf.Clamp(cachedInstance.currentLevel, 0, cachedData.MaxLevel);
            
            if (cachedData.levelSettings != null && cachedData.levelSettings.Count > currentLevelIndex)
            {
                float totalBuildTime = cachedData.levelSettings[currentLevelIndex].buildDuration;
                float elapsed = totalBuildTime - cachedInstance.remainingTime;

                // 슬라이더 반영
                if (buildSlider != null)
                {
                    buildSlider.value = Mathf.Clamp01(elapsed / totalBuildTime);
                }

                // 타이머 분:초 포맷팅 표기 (MM:SS)
                if (timerText != null)
                {
                    float timeToDisplay = Mathf.Max(0f, cachedInstance.remainingTime);
                    int minutes = Mathf.FloorToInt(timeToDisplay / 60f);
                    int seconds = Mathf.FloorToInt(timeToDisplay % 60f);
                    timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                }
            }
        }
        else
        {
            // 건설이 진행 중이지 않을 때는 비활성화
            if (progressGroup != null && progressGroup.activeSelf)
            {
                progressGroup.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 건물 클릭 이벤트를 감지하여 업그레이드를 시작합니다.
    /// 드래그 중 발생하는 오클릭 씹힘 방지 안전장치를 내장하고 있습니다.
    /// </summary>
    private void OnBuildingClicked()
    {
        if (BuildingManager.Instance == null || string.IsNullOrEmpty(buildingID)) return;

        // [코딩 제약 조건] 드래그 중인 상태라면 오클릭 방어 처리로 드래그와 빌드 클릭 분리
        if (UI_FieldDragController.Instance != null && UI_FieldDragController.Instance.IsDragging)
        {
            Debug.Log($"[UI_WorldBuildingObject] {buildingID} 드래그 상태가 감지되어 클릭 업그레이드 요청을 안전하게 차단합니다.");
            return;
        }

        Debug.Log($"[UI_WorldBuildingObject] {buildingID} 클릭됨. 업그레이드/건설 시작을 요청합니다.");
        
        // [전역 다중 팝업 아키텍처 연동]: 완공(Lv >= 1)된 건물을 클릭했을 때 대응하는 팝업 UI(PopupManager)를 자동으로 오픈합니다.
        if (cachedInstance != null && cachedInstance.currentLevel >= 1 && !cachedInstance.isConstructing)
        {
            if (PopupManager.Instance != null && PopupManager.Instance.HasPopup(buildingID))
            {
                bool opened = PopupManager.Instance.OpenPopup(buildingID);
                if (opened)
                {
                    Debug.Log($"<color=green>[UI_WorldBuildingObject] 건물 '{buildingID}'의 팝업 UI를 PopupManager를 통해 엽니다!</color>");
                    return;
                }
            }

            // 폴백: PopupManager 미등록 시 direct 탐색
            UI_BasePopup targetPopup = null;
            UI_BasePopup[] popups = Object.FindObjectsByType<UI_BasePopup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pop in popups)
            {
                if (pop != null && (pop.popupID == buildingID || (buildingID == "TownHall" && pop.popupID == "HQ")))
                {
                    targetPopup = pop;
                    break;
                }
            }

            if (targetPopup != null)
            {
                targetPopup.OpenPopup();
                Debug.Log($"<color=green>[UI_WorldBuildingObject] 건물 '{buildingID}'의 팝업 UI를 직접 활성화하였습니다.</color>");
                return;
            }
        }

        // 건설 매니저 시작 신호 전달
        BuildingManager.Instance.StartConstruction(buildingID);
    }
}
