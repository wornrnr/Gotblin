using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 소셜 그래프 미니게임의 화면 상태(시작 전 / 진행 중 / 성공 / 패배)에 맞추어
/// 텍스트, 버튼, 배경 컬러 등을 통합 스위칭 및 연동하는 UI 메인 컨트롤러 클래스입니다.
/// 가비지 컬렉션(GC Alloc)을 원천 차단하고 기존 Localization 및 GameManager와 유기적으로 소통합니다.
/// </summary>
[DisallowMultipleComponent]
public class GraphGameViewController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("연동할 GraphGameManager를 할당합니다.")]
    [SerializeField] private GraphGameManager gameManager;

    [Header("Top Panel")]
    [Tooltip("레벨 및 유저 정보 텍스트 컴포넌트입니다.")]
    [SerializeField] private TextMeshProUGUI playerInfoText;

    [Tooltip("보유 골드 재화 상태를 표시할 텍스트 컴포넌트 배열입니다 (여러 레이아웃 대응 가능).")]
    [SerializeField] private TextMeshProUGUI[] currencyTexts;

    [Header("Center Info Banner")]
    [Tooltip("중앙 배너의 메인 한국어/다국어 안내 문구 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI bannerMainText;

    [Tooltip("중앙 배너의 서브 실시간 골드 획득 정보 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI bannerSubText;

    [Tooltip("상태별 변화 피드백을 극대화하기 위한 배너의 전체 배경 이미지 컴포넌트입니다.")]
    [SerializeField] private Image bannerBackground;

    [Header("Control Area")]
    [Tooltip("남은 도전자 고블린 수량을 표기하는 텍스트입니다.")]
    [SerializeField] private TextMeshProUGUI attemptsText;

    [Tooltip("시작, 그만, 재시작 기능을 1버튼으로 처리하는 컨텍스트 통합 버튼입니다.")]
    [SerializeField] private Button contextActionButton;

    [Tooltip("통합 버튼에 들어갈 한국어/다국어 텍스트 컴포넌트입니다.")]
    [SerializeField] private TextMeshProUGUI contextButtonText;

    [Header("Bottom Navigation")]
    [Tooltip("게임 진행 도중 하단 메뉴 터치를 원천적으로 감금/허용할 CanvasGroup 컴포넌트입니다.")]
    [SerializeField] private CanvasGroup bottomNavigationCanvasGroup;

    private GraphGameState lastState = GraphGameState.Ready;

    private void Start()
    {
        // 1. 매니저 레퍼런스 자동 바인딩 시도 (유니티 6 권장)
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GraphGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[GraphGameViewController] GraphGameManager 인스턴스를 씬에서 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        }

        // 2. 통합 액션 버튼 클릭 이벤트 연결
        if (contextActionButton != null)
        {
            contextActionButton.onClick.AddListener(OnActionButtonClicked);
        }

        // 3. 전역 재화 및 다국어 언어 변경 이벤트 바인딩
        CurrencyManager.OnGoldChanged += UpdateCurrencyUI;
        LocalizationManager.OnLanguageChanged += RefreshAllTexts;

        // 4. 초기 UI 동기화
        if (CurrencyManager.Instance != null)
        {
            UpdateCurrencyUI(CurrencyManager.Instance.Gold);
        }

        if (playerInfoText != null)
        {
            playerInfoText.SetText("Lv.1 <color=yellow>고블린 도둑</color>");
        }

        UpdateAttemptsUI();

        // 5. BannerSubTxt 진동 및 파편 Juice 연출 컨트롤러 자동 바인딩
        if (bannerSubText != null && bannerSubText.GetComponent<BannerSubTxtJuiceController>() == null)
        {
            bannerSubText.gameObject.AddComponent<BannerSubTxtJuiceController>();
        }

        RefreshUI(gameManager.CurrentState);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 강제 해제로 씬 메모리 릭 예방
        CurrencyManager.OnGoldChanged -= UpdateCurrencyUI;
        LocalizationManager.OnLanguageChanged -= RefreshAllTexts;
    }

    private void Update()
    {
        GraphGameState currentState = gameManager.CurrentState;

        // 실시간 상태 전이 관찰 및 UI 드로잉 일괄 스위칭
        if (currentState != lastState)
        {
            RefreshUI(currentState);
            lastState = currentState;
        }

        // Running(게임 작동 중)일 때만 실시간 골드 획득량을 가비지 없이 화면 갱신
        if (currentState == GraphGameState.Running)
        {
            int currentGold = Mathf.RoundToInt(gameManager.BaseReward * gameManager.CurrentMultiplier);
            
            // TMPro 비할당 문자열 포맷팅으로 가비지 발생 차단
            bannerSubText.SetText("실시간 획득 골드: <color=yellow><b>{0:N0} G</b></color>", currentGold);
        }
    }

    /// <summary>
    /// 게임의 4가지 고유 상태 레이아웃에 맞춰 UI 스타일 및 터치 조작 범위를 일괄 통제합니다.
    /// </summary>
    public void RefreshUI(GraphGameState state)
    {
        if (contextActionButton == null || bannerMainText == null || bannerSubText == null) return;

        // 모든 상태 전이 시점에 시도 횟수(남은 고블린 수) UI를 실시간 동기화 갱신합니다.
        UpdateAttemptsUI();

        switch (state)
        {
            case GraphGameState.Ready:
                // 1) 시작 전 화면
                if (bottomNavigationCanvasGroup != null)
                {
                    bottomNavigationCanvasGroup.interactable = true;
                    bottomNavigationCanvasGroup.blocksRaycasts = true;
                }

                bannerMainText.gameObject.SetActive(true);
                bannerMainText.text = LocalizationManager.Instance.GetLocalizedString("GraphGame_Desc");
                bannerMainText.color = Color.white;

                bannerSubText.gameObject.SetActive(false); // 준비 상태 시 보상 숨김

                if (bannerBackground != null)
                {
                    bannerBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // 기본 묵직한 어두운색
                }

                contextButtonText.text = LocalizationManager.Instance.GetLocalizedString("ui_start_btn");
                
                // [버튼 활성화 제약] 남은 고블린 시도 횟수가 없으면 시작 버튼 비활성화 차단
                contextActionButton.interactable = (gameManager.RemainingAttempts > 0);
                break;

            case GraphGameState.Running:
                // 2) 게임 진행 중 화면
                if (bottomNavigationCanvasGroup != null)
                {
                    bottomNavigationCanvasGroup.interactable = false; // 하단 탭 조작 원천 봉쇄
                    bottomNavigationCanvasGroup.blocksRaycasts = false;
                }

                bannerMainText.gameObject.SetActive(false); // 설명문 페이드아웃
                bannerSubText.gameObject.SetActive(true);

                if (bannerBackground != null)
                {
                    bannerBackground.color = new Color(0.1f, 0.4f, 0.8f, 0.95f); // 몰입도 높은 블루 계열
                }

                contextButtonText.text = LocalizationManager.Instance.GetLocalizedString("ui_cashout_btn");
                contextActionButton.interactable = true;
                break;

            case GraphGameState.Success:
                // 3) 보물 탈취 성공 화면
                if (bottomNavigationCanvasGroup != null)
                {
                    bottomNavigationCanvasGroup.interactable = true;
                    bottomNavigationCanvasGroup.blocksRaycasts = true;
                }

                bannerMainText.gameObject.SetActive(true);
                bannerMainText.text = LocalizationManager.Instance.GetLocalizedString("GraphGame_Win_Desc");
                bannerMainText.color = Color.green;

                bannerSubText.gameObject.SetActive(true);
                int finalGold = Mathf.RoundToInt(gameManager.BaseReward * gameManager.CurrentMultiplier);
                bannerSubText.SetText("탈출 성공 보상: <color=yellow><b>+{0:N0} G</b></color>", finalGold);

                if (bannerBackground != null)
                {
                    bannerBackground.color = new Color(0.1f, 0.6f, 0.2f, 0.95f); // 성공을 알리는 그린
                }

                contextButtonText.text = LocalizationManager.Instance.GetLocalizedString("ui_next_btn");
                contextActionButton.interactable = true;
                break;

            case GraphGameState.Busted:
                // 4) 발각당한 패배 화면
                if (bottomNavigationCanvasGroup != null)
                {
                    bottomNavigationCanvasGroup.interactable = true;
                    bottomNavigationCanvasGroup.blocksRaycasts = true;
                }

                bannerMainText.gameObject.SetActive(true);
                bannerMainText.text = LocalizationManager.Instance.GetLocalizedString("GraphGame_Lose_Desc");
                bannerMainText.color = new Color(1f, 0.2f, 0.2f); // 경고성 선명한 빨간색

                bannerSubText.gameObject.SetActive(true);
                bannerSubText.text = "<color=#FF5555><b>수비 대장에게 발각당해 재화를 잃었습니다!</b></color>";

                if (bannerBackground != null)
                {
                    bannerBackground.color = new Color(0.6f, 0.05f, 0.05f, 0.95f); // 자극적인 딥 레드
                }

                contextButtonText.text = LocalizationManager.Instance.GetLocalizedString("ui_next_btn");
                contextActionButton.interactable = true;
                break;
        }
    }

    /// <summary>
    /// 남은 고블린 마리수(Attempts) 텍스트를 기획 사양에 연동해 실시간 가비지 프리 갱신합니다.
    /// (핵심 매니저인 GraphGameManager의 데이터를 직접 읽어와 표시합니다.)
    /// </summary>
    private void UpdateAttemptsUI()
    {
        if (attemptsText == null) return;

        string label = LocalizationManager.Instance.GetLocalizedString("GraphGame_Count");
        // TMPro.SetText의 float 제한 오버로드로 인한 형식 불일치 컴파일 오류를 해결하기 위해 string.Format 사용
        attemptsText.text = string.Format("{0}: <color=yellow><b>{1}</b></color> 마리", label, gameManager.RemainingAttempts);
    }

    /// <summary>
    /// CurrencyManager로부터의 골드 변화 이벤트를 포착해 실시간 보유 골드 정보를 갱신합니다.
    /// </summary>
    private void UpdateCurrencyUI(int gold)
    {
        if (currencyTexts == null) return;

        for (int i = 0; i < currencyTexts.Length; i++)
        {
            if (currencyTexts[i] != null)
            {
                currencyTexts[i].SetText("{0:N0} G", gold);
            }
        }
    }

    /// <summary>
    /// 기획자가 런타임 언어 테이블(KO/EN/JA)을 스위칭했을 때 UI의 모든 정적/동적 문구를 일괄 재갱신합니다.
    /// </summary>
    private void RefreshAllTexts()
    {
        UpdateAttemptsUI();
        RefreshUI(gameManager.CurrentState);
    }

    /// <summary>
    /// 1버튼 통합 액션 버튼 클릭 시 작동하는 분기 핸들러입니다.
    /// </summary>
    private void OnActionButtonClicked()
    {
        switch (gameManager.CurrentState)
        {
            case GraphGameState.Ready:
                if (gameManager.RemainingAttempts <= 0)
                {
                    Debug.LogWarning("[GraphGameViewController] 참가할 수 있는 고블린이 없습니다! 재도전하려면 충전이 필요합니다.");
                    return;
                }
                gameManager.StartRound();
                break;

            case GraphGameState.Running:
                gameManager.CashOut();
                break;

            case GraphGameState.Success:
            case GraphGameState.Busted:
                // [기획자 테스트 편의 꿀기능] 목숨이 0마리가 되었을 때 다음을 누르면 편한 루프 테스트를 위해 3개로 리필해 줍니다!
                if (gameManager.RemainingAttempts <= 0)
                {
                    gameManager.RefillAttempts(3);
                    Debug.Log("<color=orange><b>[GraphGameViewController] 기획자 테스트 편의를 위해 고블린 3마리를 자동 충전 완료했습니다!</b></color>");
                }
                gameManager.ResetGame();
                break;
        }
    }

    // -----------------------------------------------------------------------------------
    // 에디터 인스펙터 테스트용 ContextMenu 디버그 기능군
    // -----------------------------------------------------------------------------------
    [ContextMenu("Debug/Add Goblin attempts")]
    private void DebugAddGoblin()
    {
        gameManager.RefillAttempts(1);
        UpdateAttemptsUI();
    }

    [ContextMenu("Debug/Clear Goblin attempts")]
    private void DebugClearGoblin()
    {
        // 0마리로 테스트하기 위해 강제 차감 조작 (매니저의 값을 인스펙터에서 강제 제어 가능)
        // remainingAttempts 필드는 매니저의 SerializeField 이므로 직접 인스펙터에서 수정하거나 
        // 헬퍼로 강제 0으로 만들어 테스트를 모의할 수 있습니다.
        if (gameManager != null)
        {
            // 리셋용으로 강제 리필을 0 이하로 설정하기 위해 매니저를 통해 차감하도록 지원하거나
            // 직접 매니저 인스펙터에서 Attempts를 0으로 조절하여 테스트할 수 있음을 가이드합니다.
            Debug.Log("[GraphGameViewController] 남은 고블린 수 조절은 GraphGameManager 인스펙터의 Remaining Attempts 값을 0으로 설정해 주세요!");
        }
    }
}
