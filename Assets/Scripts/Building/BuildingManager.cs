using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다중 건설 슬롯 관리 및 모바일 환경에서의 오프라인 시간 경과 계산을 처리하는 건물 매니저 싱글톤 클래스입니다.
/// </summary>
[DisallowMultipleComponent]
public class BuildingManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static BuildingManager Instance { get; private set; }

    [Header("건물 기획 템플릿")]
    [Tooltip("에디터에서 설정한 건물의 기본 스펙 리스트(BuildingData ScriptableObject)입니다.")]
    [SerializeField] private List<BuildingData> allBuildingTemplates = new List<BuildingData>();

    [Header("유저 실시간 건물 상태")]
    [Tooltip("현재 인게임 유저의 실시간 건물 건설 및 레벨 정보입니다.")]
    [SerializeField] private List<BuildingInstance> activeBuildings = new List<BuildingInstance>();

    [Header("건설 슬롯 설정")]
    [Tooltip("현재 해금된 최대 건설 가능 슬롯 수입니다 (최소 1, 최대 3).")]
    [Range(1, 3)]
    [SerializeField] private int maxUnlockedSlots = 1;

    private const string OfflineTimestampKey = "BuildingOfflineTimestamp";

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 유저 데이터 세션 초기화
        InitializeUserBuildings();
    }

    private void Start()
    {
        // 씬 시작 시 오프라인 경과 시간 일괄 차감 적용
        ProcessOfflineConstruction();
    }

    private void Update()
    {
        // 매 프레임 건설 중인 모든 건물의 시간을 갱신합니다.
        // 리스트 순회 중 요소 추가/삭제 등으로 인한 InvalidOperationException을 예방하기 위해 인덱스(for) 루프로 순회합니다.
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            BuildingInstance instance = activeBuildings[i];

            if (instance != null && instance.isConstructing)
            {
                instance.remainingTime -= Time.deltaTime;

                if (instance.remainingTime <= 0f)
                {
                    CompleteConstruction(instance);
                }
            }
        }
    }

    /// <summary>
    /// 게임 일시 정지(포커스 해제) 시 또는 게임 종료 시 현재 시각 타임스탬프를 안전하게 디스크에 직렬화 저장합니다.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 백그라운드로 전환될 때 타임스탬프 기록
            SaveTimestamp();
        }
        else
        {
            // 백그라운드에서 다시 돌아올 때 오프라인 경과 시간을 읽어 적용
            ProcessOfflineConstruction();
        }
    }

    private void OnApplicationQuit()
    {
        // 게임 완전 종료 시 타임스탬프 기록
        SaveTimestamp();
    }

    /// <summary>
    /// 현재 해금된 최대 슬롯 개수를 설정합니다. (1 ~ 3개 사이로 자동 제어)
    /// </summary>
    public void SetMaxUnlockedSlots(int count)
    {
        maxUnlockedSlots = Mathf.Clamp(count, 1, 3);
        Debug.Log($"[BuildingManager] 건설 슬롯이 해금되었습니다! (현재 사용 가능한 슬롯: {maxUnlockedSlots}개)");
    }

    /// <summary>
    /// 현재 동시에 건설 또는 업그레이드 중인 활성 건물 개수를 구합니다.
    /// </summary>
    public int GetCurrentConstructingCount()
    {
        int count = 0;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i].isConstructing)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 특정 건물 ID에 대한 건설/업그레이드를 요청합니다.
    /// </summary>
    public void StartConstruction(string buildingID)
    {
        // 1. 기획 스펙이 존재하는지 선검증
        BuildingData template = FindTemplate(buildingID);
        if (template == null)
        {
            Debug.LogError($"[BuildingManager] ID가 '{buildingID}'인 건물 기획 템플릿을 찾을 수 없습니다!");
            return;
        }

        // 2. 유저의 실시간 건물 상태 인스턴스 검색
        BuildingInstance instance = FindInstance(buildingID);
        if (instance == null)
        {
            // 미등록 건물의 경우 0레벨 가동 인스턴스를 실시간 추가하여 유연하게 대응
            instance = new BuildingInstance(buildingID);
            activeBuildings.Add(instance);
        }

        // 3. 건설 조건 검증
        if (instance.isConstructing)
        {
            Debug.LogWarning($"[BuildingManager] {template.BuildingName}은(는) 이미 건설/업그레이드가 진행 중입니다! (남은 시간: {instance.remainingTime:F1}초)");
            return;
        }

        if (instance.currentLevel >= template.MaxLevel)
        {
            Debug.LogWarning($"[BuildingManager] {template.BuildingName}은(는) 이미 최대 레벨({template.MaxLevel})에 도달했습니다!");
            return;
        }

        if (GetCurrentConstructingCount() >= maxUnlockedSlots)
        {
            Debug.LogWarning($"<color=#FF4F4F><b>[BuildingManager] 건설 실패!</b></color> 가용 건설 슬롯이 부족합니다. (현재 슬롯: {maxUnlockedSlots}개 전체 가동 중)");
            return;
        }

        // 4. 레벨별 테이블 데이터 기반 비용 및 시간 획득
        // 현재 레벨 인덱스를 안전하게 계산하여 바운더리 체크
        int currentLevelIndex = Mathf.Clamp(instance.currentLevel, 0, template.MaxLevel);
        BuildingLevelEnv levelEnv = template.levelSettings[currentLevelIndex];

        int requiredGold = levelEnv.upgradeCost;
        float buildTime = levelEnv.buildDuration;

        // 5. CurrencyManager를 통한 전역 재화 소모 연동
        if (CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.ConsumeGold(requiredGold))
            {
                Debug.LogWarning($"[BuildingManager] 골드가 부족하여 건설을 시작할 수 없습니다. (필요 골드: {requiredGold:N0} G)");
                UI_ToastPopup.Show("Notice_No_Currency");
                return;
            }
        }
        else
        {
            Debug.LogError("[BuildingManager] CurrencyManager 인스턴스를 찾을 수 없습니다! 개발용 디버깅을 위해 재화 무료 패스로 무가 과금 시작 처리합니다.");
        }

        // 6. 건설 활성화 적용
        instance.isConstructing = true;
        instance.remainingTime = buildTime;

        Debug.Log($"<color=#1BE468><b>[BuildingManager] 건설 및 업그레이드 시작!</b></color>\n" +
                  $"- 건물 이름: <color=yellow>{template.BuildingName}</color> (Lv. {instance.currentLevel} ➡️ Lv. {instance.currentLevel + 1})\n" +
                  $"- 소모 골드: <color=orange>{requiredGold:N0} Gold</color> / 건설 소요 시간: {buildTime:F1}초\n" +
                  $"- 현재 가동 건설 슬롯: {GetCurrentConstructingCount()} / {maxUnlockedSlots}");
    }

    /// <summary>
    /// 최초 시작 시 기획 템플릿(allBuildingTemplates)에 정의된 모든 건물들을 0레벨 기본 인스턴스로 자동 등록합니다.
    /// </summary>
    private void InitializeUserBuildings()
    {
        if (activeBuildings == null)
        {
            activeBuildings = new List<BuildingInstance>();
        }

        // [대장간 기획 템플릿 런타임 획득 보장]
        if (FindTemplate("Blacksmith") == null)
        {
            BuildingData bsData = null;
#if UNITY_EDITOR
            bsData = UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/BuildingData/BlacksmithData.asset");
#endif
            if (bsData == null)
            {
                bsData = Resources.Load<BuildingData>("BuildingData/BlacksmithData");
            }

            if (bsData != null)
            {
                allBuildingTemplates.Add(bsData);
            }
        }

        for (int i = 0; i < allBuildingTemplates.Count; i++)
        {
            BuildingData data = allBuildingTemplates[i];
            if (data != null && FindInstance(data.BuildingID) == null)
            {
                activeBuildings.Add(new BuildingInstance(data.BuildingID));
            }
        }
    }

    /// <summary>
    /// 오프라인 상태에서 흘러간 시간(elapsedSeconds)을 구하고, 건설이 완료된 모든 건물의 시간 차감 및 완공 처리를 일괄 수행합니다.
    /// </summary>
    private void ProcessOfflineConstruction()
    {
        if (!PlayerPrefs.HasKey(OfflineTimestampKey))
        {
            // 이전 타임스탬프 기록이 없는 경우 현재 시각을 새로 기록하고 패스
            SaveTimestamp();
            return;
        }

        string savedTimeStr = PlayerPrefs.GetString(OfflineTimestampKey);
        if (DateTime.TryParse(savedTimeStr, out DateTime savedTimeUtc))
        {
            DateTime currentUtc = DateTime.UtcNow;
            double elapsedSecondsDouble = (currentUtc - savedTimeUtc).TotalSeconds;
            float elapsedSeconds = (float)Math.Max(0, elapsedSecondsDouble);

            if (elapsedSeconds > 0.5f)
            {
                Debug.Log($"<color=#3BB2FF><b>[BuildingManager] 오프라인 시간 경과 계산 완료</b></color>\n" +
                          $"- 오프라인 시간: <color=white>{elapsedSeconds:F1}초</color> 경과가 건설 스케줄에 적용됩니다.");

                // 건설 중인 모든 슬롯의 시간을 오프라인 흐른 시간만큼 차감
                // InvalidOperationException 예방을 위해 for 루프 구조 사용
                for (int i = 0; i < activeBuildings.Count; i++)
                {
                    BuildingInstance instance = activeBuildings[i];
                    if (instance != null && instance.isConstructing)
                    {
                        instance.remainingTime -= elapsedSeconds;

                        if (instance.remainingTime <= 0f)
                        {
                            // 오프라인 중에 완공된 건물 처리
                            CompleteConstruction(instance);
                        }
                        else
                        {
                            // 건설 중이지만 시간이 덜 흐른 경우
                            BuildingData template = FindTemplate(instance.buildingID);
                            string bName = template != null ? template.BuildingName : instance.buildingID;
                            Debug.Log($"[BuildingManager] 오프라인 차감 중: {bName} (남은 시간: {instance.remainingTime:F1}초)");
                        }
                    }
                }
            }
        }

        // 중복 차감을 차단하기 위해 타임스탬프를 최신 현재 UTC 시각으로 즉시 갱신
        SaveTimestamp();
    }

    /// <summary>
    /// 건물의 건설 및 업그레이드를 마무리하고 레벨을 올립니다.
    /// </summary>
    private void CompleteConstruction(BuildingInstance instance)
    {
        instance.isConstructing = false;
        instance.remainingTime = 0f;
        instance.currentLevel++;

        BuildingData template = FindTemplate(instance.buildingID);
        string bName = template != null ? template.BuildingName : instance.buildingID;

        Debug.Log($"<color=cyan><b>[BuildingManager] ★ 건설 완료! ★</b></color>\n" +
                  $"- <color=yellow><b>{bName}</b></color> 건물 완공! (현재 레벨: <color=white><b>Lv. {instance.currentLevel}</b></color>)\n" +
                  $"- 가용 가능해진 슬롯: {GetCurrentConstructingCount()} / {maxUnlockedSlots}");
    }

    /// <summary>
    /// 현재의 UTC 시간 문자열을 PlayerPrefs 디스크 영역에 안전하게 저장합니다.
    /// </summary>
    private void SaveTimestamp()
    {
        PlayerPrefs.SetString(OfflineTimestampKey, DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }

    // -----------------------------------------------------------------------------------
    // 에셋 검색을 위한 헬퍼 내부 조회 함수
    // -----------------------------------------------------------------------------------
    private BuildingData FindTemplate(string id)
    {
        for (int i = 0; i < allBuildingTemplates.Count; i++)
        {
            if (allBuildingTemplates[i] != null && allBuildingTemplates[i].BuildingID == id)
            {
                return allBuildingTemplates[i];
            }
        }
        return null;
    }

    private BuildingInstance FindInstance(string id)
    {
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i] != null && activeBuildings[i].buildingID == id)
            {
                return activeBuildings[i];
            }
        }
        return null;
    }

    // -----------------------------------------------------------------------------------
    // 외부 조회를 위한 퍼블릭 API
    // -----------------------------------------------------------------------------------
    /// <summary>
    /// 특정 건물 ID에 해당하는 기획 템플릿 스펙 데이터를 조회합니다.
    /// </summary>
    public BuildingData GetBuildingData(string id)
    {
        return FindTemplate(id);
    }

    /// <summary>
    /// 특정 건물 ID에 해당하는 실시간 유저 인스턴스 상태를 조회합니다.
    /// </summary>
    public BuildingInstance GetBuildingInstance(string id)
    {
        return FindInstance(id);
    }

    /// <summary>
    /// 전체 실시간 건물 인스턴스 리스트를 반환합니다.
    /// </summary>
    public List<BuildingInstance> GetActiveBuildings()
    {
        return activeBuildings;
    }

    // -----------------------------------------------------------------------------------
    // 에디터 인스펙터 테스트용 ContextMenu 디버그 기능군
    // -----------------------------------------------------------------------------------
    [ContextMenu("Slots/Unlock 2 Slots")]
    private void DebugUnlock2Slots()
    {
        SetMaxUnlockedSlots(2);
    }

    [ContextMenu("Slots/Unlock 3 Slots")]
    private void DebugUnlock3Slots()
    {
        SetMaxUnlockedSlots(3);
    }

    [ContextMenu("Test/Build TownHall")]
    private void DebugBuildTownHall()
    {
        StartConstruction("TownHall");
    }

    [ContextMenu("Test/Build Barracks")]
    private void DebugBuildBarracks()
    {
        StartConstruction("Barracks");
    }

    /// <summary>
    /// 강제로 오프라인 시간 경과 1시간(3600초)을 속여서 테스트하는 시뮬레이션 메서드입니다.
    /// </summary>
    [ContextMenu("Test/Simulate Offline 1 Hour")]
    private void DebugSimulateOffline1Hour()
    {
        Debug.Log("<color=orange><b>[BuildingManager] 강제 오프라인 1시간(3600초) 흐름 시뮬레이션을 시작합니다.</b></color>");

        // 타임스탬프를 1시간 전으로 조작
        DateTime fakePastTimeUtc = DateTime.UtcNow.AddHours(-1);
        PlayerPrefs.SetString(OfflineTimestampKey, fakePastTimeUtc.ToString());
        PlayerPrefs.Save();

        // 즉시 차감 시스템 가동
        ProcessOfflineConstruction();
    }
}
