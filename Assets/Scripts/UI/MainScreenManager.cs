using UnityEngine;

/// <summary>
/// 메인 로비 화면의 탭 메뉴에 대응하여 두 개의 코어 패널(그래프 게임 ↔ 부락 건설)을 스위칭하고
/// 진입 시 최신 상태 동기화를 트리거하는 메인 화면 UI 전역 매니저입니다.
/// </summary>
[DisallowMultipleComponent]
public class MainScreenManager : MonoBehaviour
{
    // UI 전역 매니저 싱글톤
    public static MainScreenManager Instance { get; private set; }

    [Header("화면 패널 UI 오브젝트")]
    [Tooltip("코어 1: 그래프 게임 화면의 부모 GameObject 패널입니다.")]
    [SerializeField] private GameObject graphGamePanel;

    [Tooltip("코어 2: 부락 건설 및 관리 화면의 부모 GameObject 패널입니다.")]
    [SerializeField] private GameObject townBuildingPanel;

    [Tooltip("코어 3: 자동 전투 시스템 화면의 부모 GameObject 패널입니다.")]
    [SerializeField] private GameObject combatPanel;

    /// <summary>
    /// 현재 전투 시스템 탭이 활성화되어 있는지 여부를 반환합니다.
    /// (백그라운드 최적화 및 팝업 텍스트 스킵 등에 사용)
    /// </summary>
    public bool IsCombatPanelActive { get; private set; }

    private void Awake()
    {
        // 싱글톤 이니셜라이즈
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 패널의 상태를 CanvasGroup을 통해 제어합니다.
    /// GameObject를 끄지 않아 백그라운드 코루틴 및 Update 로직이 계속 동작할 수 있습니다.
    /// </summary>
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel == null) return;
        
        // 유니티 인스펙터나 이전에 SetActive(false)로 꺼져있었다면 캔버스그룹이 동작하지 않으므로 강제로 켜줍니다.
        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }

        cg.alpha = isActive ? 1f : 0f;
        cg.interactable = isActive;
        cg.blocksRaycasts = isActive;
    }

    /// <summary>
    /// 그래프 게임 패널을 켜고 부락 건설 및 전투 패널을 비활성화합니다.
    /// </summary>
    public void SwitchToGraphGame()
    {
        IsCombatPanelActive = false;
        SetPanelActive(townBuildingPanel, false);
        SetPanelActive(combatPanel, false);
        SetPanelActive(graphGamePanel, true);

        Debug.Log("[MainScreenManager] [코어 1: 그래프 게임] 패널 활성화 완료.");
    }

    /// <summary>
    /// 부락 건설 패널을 활성화하고 하위 슬롯들의 UI 상태를 즉시 최신 정보로 갱신합니다.
    /// </summary>
    public void SwitchToTownBuilding()
    {
        IsCombatPanelActive = false;
        SetPanelActive(graphGamePanel, false);
        SetPanelActive(combatPanel, false);
        
        if (townBuildingPanel != null)
        {
            SetPanelActive(townBuildingPanel, true);

            // [대장간 건물 오브젝트 자동 셋업]
            var bsSetup = townBuildingPanel.GetComponent<TownBuildingBlacksmithSetup>();
            if (bsSetup == null)
            {
                bsSetup = townBuildingPanel.AddComponent<TownBuildingBlacksmithSetup>();
            }
            bsSetup.EnsureBlacksmithBuilding();

            // [핵심 요구사항] 진입 시 하위에 등록된 모든 건설 슬롯의 UI 갱신 일괄 호출
            UI_BuildingSlot[] slots = townBuildingPanel.GetComponentsInChildren<UI_BuildingSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].RefreshUI();
                }
            }
        }

        Debug.Log("[MainScreenManager] [코어 2: 부락 건설] 패널 활성화 및 UI 데이터 동기화 완료.");
    }

    /// <summary>
    /// 코어 3: 전투 시스템 패널을 켜고 그래프 게임 및 부락 건설 패널을 비활성화합니다.
    /// </summary>
    public void SwitchToCombatSystem()
    {
        IsCombatPanelActive = true;
        SetPanelActive(graphGamePanel, false);
        SetPanelActive(townBuildingPanel, false);
        SetPanelActive(combatPanel, true);

        Debug.Log("[MainScreenManager] [코어 3: 전투 시스템] 패널 활성화 완료.");
    }
}
