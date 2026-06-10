using UnityEngine;

/// <summary>
/// 소셜 그래프 미니게임의 핵심 상태 관리 및 타이머/추첨 로직을 구동하는 매니저 클래스입니다.
/// 기획자가 자유롭게 조절하는 '상대 가중치(Relative Weight)' 시스템을 기반으로 확률 추첨을 수행합니다.
/// </summary>
[DisallowMultipleComponent]
public class GraphGameManager : MonoBehaviour
{
    [Header("기획 설정 데이터")]
    [Tooltip("정지 시간 후보군과 상대 가중치가 설정된 ScriptableObject를 할당합니다.")]
    [SerializeField] private GraphGameTableData tableData;

    [Tooltip("이번 판의 기본 골드 보상량입니다.")]
    [SerializeField] private int baseReward = 100;

    [Tooltip("배수 상승 곡선 가속도 계수입니다.")]
    [SerializeField] private float curveSpeed = 0.1f;

    /// <summary>
    /// 이번 판의 기본 골드 보상량에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public int BaseReward => baseReward;

    [Header("실시간 게임 상태 (디버그/모니터링용)")]
    [Tooltip("현재 미니게임의 진행 상태입니다.")]
    [SerializeField] private GraphGameState currentState = GraphGameState.Ready;

    [Tooltip("현재 라운드가 시작된 이후 경과한 시간(초 단위)입니다.")]
    [SerializeField] private float currentTimer = 0f;

    [Tooltip("이번 라운드에 상대 가중치 추첨을 통해 확정된 정지 목표 시간(초 단위)입니다.")]
    [SerializeField] private float targetStopTime = 0f;

    [Tooltip("현재 실시간 상승 중인 배수 값입니다.")]
    [SerializeField] private float currentMultiplier = 1.0f;

    [Header("시도 횟수 제한 설정")]
    [Tooltip("현재 남아 있는 고블린(시도 횟수)의 수량입니다.")]
    [SerializeField] private int remainingAttempts = 3;

    /// <summary>
    /// 현재 게임의 진행 상태에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public GraphGameState CurrentState => currentState;

    /// <summary>
    /// 현재 흐른 시간에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public float CurrentTimer => currentTimer;

    /// <summary>
    /// 이번 라운드에 추첨된 정지 시간에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public float TargetStopTime => targetStopTime;

    /// <summary>
    /// 현재 실시간 상승 중인 배수 값에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public float CurrentMultiplier => currentMultiplier;

    /// <summary>
    /// 현재 남아있는 시도 횟수(고블린 수)에 대한 외부 Getter 프로퍼티입니다.
    /// </summary>
    public int RemainingAttempts => remainingAttempts;

    /// <summary>
    /// 게임 상태가 변화할 때 씬의 비주얼 연출 및 UI가 감지하여 유기적으로 동기화할 수 있도록 하는 전역 이벤트입니다.
    /// </summary>
    public static event System.Action<GraphGameState> OnStateChanged;

    /// <summary>
    /// GraphGameManager의 전역 싱글톤 인스턴스 프로퍼티입니다.
    /// </summary>
    public static GraphGameManager Instance { get; private set; }

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

    private void Start()
    {
        // 게임 시작 시 초기 상태 보장
        ResetGame();
    }

    private void Update()
    {
        // Running 상태일 때만 타이머가 작동하며 정지 시간에 도달하는지 실시간 감시합니다.
        if (currentState == GraphGameState.Running)
        {
            currentTimer += Time.deltaTime;

            // [핵심 보상 공식] 실시간 배수 연산: 1.0배에서 시작하여 시간 경과에 따라 Pow 곡선 상승
            currentMultiplier = 1.0f + Mathf.Pow(currentTimer, 1.5f) * curveSpeed;

            // 실시간으로 배수가 상승하는 시뮬레이션을 위해 프레임별 경과를 로그로 보고 싶다면 주석 해제하여 사용
            // Debug.Log($"[GraphGameManager] 상승 중... 현재 시간: {currentTimer:F2}초 / 배수: {currentMultiplier:F2}x / 목표 시간: {targetStopTime:F2}초");

            if (currentTimer >= targetStopTime)
            {
                // 타이머를 targetStopTime으로 보정하고 Busted 처리합니다.
                currentTimer = targetStopTime;
                
                // Busted 시에는 배수를 0배(들킴)로 리셋 처리
                currentMultiplier = 0f;
                TriggerBusted();
            }
        }
    }

    /// <summary>
    /// 상태 전이를 일관되고 확실하게 제어하며 유니티 콘솔에 흐름 상태를 명확히 출력하는 전용 내부 메서드입니다.
    /// </summary>
    private void SetState(GraphGameState newState)
    {
        if (currentState == newState) return;

        GraphGameState oldState = currentState;
        currentState = newState;

        // 기획자가 상태 변경 역사를 명확하게 역추적할 수 있도록 컬러 로깅
        Debug.Log($"<color=#FFF500><b>[GraphGameState] {oldState} ➡️ {newState}</b></color>");

        // 상태 전환 이벤트 전파
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// 미니게임의 새로운 라운드를 시작합니다.
    /// </summary>
    [ContextMenu("Start Round")]
    public void StartRound()
    {
        // Ready 상태에서만 라운드를 시작할 수 있습니다.
        if (currentState != GraphGameState.Ready)
        {
            Debug.LogWarning($"[GraphGameManager] 게임이 Ready 상태가 아닙니다. 현재 상태: {currentState}. StartRound를 호출하려면 먼저 ResetGame을 실행해 주세요.");
            return;
        }

        // 시도 횟수(남은 고블린 수) 부족 여부 검증 및 방어
        if (remainingAttempts <= 0)
        {
            Debug.LogError($"[GraphGameManager] 시작 불가! 남은 고블린 수(시도 횟수)가 {remainingAttempts}개로 부족합니다.");
            return;
        }

        // 데이터 예외 처리 (방어적 코딩)
        if (tableData == null)
        {
            Debug.LogError("[GraphGameManager] TableData ScriptableObject가 할당되지 않았습니다! 인스펙터를 확인해 주세요.");
            return;
        }

        if (tableData.Count == 0)
        {
            Debug.LogError("[GraphGameManager] TableData에 설정된 행(DataRows) 데이터가 없습니다! 라운드를 시작할 수 없습니다.");
            return;
        }

        int totalWeight = tableData.GetTotalWeight();
        if (totalWeight <= 0)
        {
            Debug.LogError($"[GraphGameManager] 가중치의 총합이 {totalWeight}입니다. 가중치는 최소 1 이상으로 구성되어야 합니다. 추첨을 시작할 수 없습니다.");
            return;
        }

        // 1. 상대 가중치 기반 랜덤 추첨(Weighted Random) 진행
        if (TryDrawStopTime(out float drawnTime, out GraphGameTableData.GraphGameRow drawnRow))
        {
            targetStopTime = drawnTime;
            currentTimer = 0f;
            currentMultiplier = 1.0f; // 배수 1배로 초기화
            
            // [핵심] 시작 즉시 남은 고블린(시도 횟수) 1개 차감 적용
            remainingAttempts--;
            Debug.Log($"<color=#FF7A7A><b>[GraphGameManager] 고블린 1마리 출격! (남은 고블린: {remainingAttempts}마리)</b></color>");

            // 상태를 Running으로 안전하게 전이
            SetState(GraphGameState.Running);

            // 당첨 확률 계산 (기획 확인용 디버깅 정보)
            float probabilityPercent = ((float)drawnRow.weight / totalWeight) * 100f;

            Debug.Log($"<color=#1BE468><b>[GraphGameManager] 새로운 라운드 시작!</b></color>\n" +
                      $"- 이번 판 목표 정지 시간: <color=yellow><b>{targetStopTime:F2}초</b></color> (Row Index: {drawnRow.index})\n" +
                      $"- 추첨 가중치: {drawnRow.weight} / 총 가중치 합: {totalWeight} (이론상 확률: {probabilityPercent:F2}%)");
        }
        else
        {
            Debug.LogError("[GraphGameManager] 가중치 랜덤 추첨 중 알 수 없는 문제가 발생하여 실패했습니다.");
        }
    }

    /// <summary>
    /// 유저가 배수가 터지기 전 타이머를 멈추고 현금화(탈출)를 선언하는 메서드입니다.
    /// </summary>
    [ContextMenu("Cash Out")]
    public void CashOut()
    {
        // Running 상태에서만 CashOut(현금화)이 가능합니다.
        if (currentState != GraphGameState.Running)
        {
            Debug.LogWarning($"[GraphGameManager] 실시간 진행 상태(Running)일 때만 CashOut이 가능합니다. 현재 상태: {currentState}");
            return;
        }

        // 상태를 Success로 안전하게 전이
        SetState(GraphGameState.Success);
        
        // 최종 획득 보상 계산 (기본 보상 * 탈출 성공 시점의 실시간 배수)
        int finalReward = Mathf.RoundToInt(baseReward * currentMultiplier);

        // 싱글톤 CurrencyManager를 통해 유저 골드 지급
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(finalReward);
        }
        else
        {
            Debug.LogError("[GraphGameManager] CurrencyManager 인스턴스를 찾을 수 없어 보상 지급에 실패했습니다!");
        }

        Debug.Log($"<color=#3BB2FF><b>[GraphGameManager] Cash Out 성공!</b></color>\n" +
                  $"- 안전 탈출 시점: <color=white><b>{currentTimer:F2}초</b></color> (목표 정지 시간: {targetStopTime:F2}초)\n" +
                  $"- 획득 배수: <color=yellow><b>x{currentMultiplier:F2}</b></color>\n" +
                  $"- 획득 재화: <color=orange><b>+{finalReward:N0} Gold</b></color> (기본 보상: {baseReward:N0})");
    }

    /// <summary>
    /// 다음 게임 진행을 위해 상태를 Ready로 복구하고 타이머 및 배수 정보를 초기화합니다.
    /// </summary>
    [ContextMenu("Reset Game")]
    public void ResetGame()
    {
        // 상태를 Ready로 안전하게 전이
        SetState(GraphGameState.Ready);
        
        currentTimer = 0f;
        targetStopTime = 0f;
        currentMultiplier = 1.0f;

        Debug.Log("[GraphGameManager] 게임이 초기화되었습니다. Ready 상태로 다음 판을 준비합니다.");
    }

    /// <summary>
    /// 실시간 테스트 또는 보상 획득 시 시도 횟수(고블린 수)를 안전하게 충전해 주는 헬퍼 메서드입니다.
    /// </summary>
    public void RefillAttempts(int count)
    {
        if (count <= 0) return;

        remainingAttempts += count;
        Debug.Log($"<color=#FFF500><b>[GraphGameManager] 고블린 충전 완료!</b></color> +{count}마리 (현재 고블린: {remainingAttempts}마리)");
    }

    /// <summary>
    /// 정지 시간에 도달하여 유저가 파산(실패)했음을 알리는 내부 처리 메서드입니다.
    /// </summary>
    private void TriggerBusted()
    {
        // 상태를 Busted로 안전하게 전이
        SetState(GraphGameState.Busted);

        Debug.Log($"<color=#FF4F4F><b>[GraphGameManager] BUSTED!</b></color>\n" +
                  $"- 게임 정지 도달 시간: <color=yellow><b>{targetStopTime:F2}초</b></color>에서 그래프가 폭발하였습니다. 탈출에 실패했습니다.");
    }

    /// <summary>
    /// 설정된 가중치를 기준으로 정지 시간 후보 중 하나를 추첨합니다.
    /// </summary>
    private bool TryDrawStopTime(out float drawnTime, out GraphGameTableData.GraphGameRow drawnRow)
    {
        drawnTime = 1f; // 실패 대비 기본값
        drawnRow = default;

        int totalWeight = tableData.GetTotalWeight();
        if (totalWeight <= 0)
        {
            // 가중치 합이 없는 비정상적인 상황에 대한 방어 코드: 첫 번째 유효 요소 리턴
            if (tableData.Count > 0)
            {
                drawnRow = tableData.DataRows[0];
                drawnTime = drawnRow.stopTime;
                return true;
            }
            return false;
        }

        // 1) 0부터 totalWeight 사이의 무작위 정수를 뽑습니다.
        // Unity의 Random.Range(int min, int max)는 max 값을 제외하므로, 0부터 (totalWeight - 1) 사이의 임의 정수가 반환됩니다.
        int randVal = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        // 2) 데이터를 순회하며 가중치를 누적해 나갑니다.
        for (int i = 0; i < tableData.Count; i++)
        {
            GraphGameTableData.GraphGameRow row = tableData.DataRows[i];
            
            // 가중치가 음수이거나 0 이하인 잘못된 기획 항목에 대한 방어
            int rowWeight = Mathf.Max(0, row.weight);
            if (rowWeight == 0) continue;

            accumulatedWeight += rowWeight;

            // 3) 뽑힌 무작위 정수 값이 누적 값보다 작거나 같아지는 순간의 stopTime을 targetStopTime으로 확정합니다.
            // 0-based 정수 범위 상, 누적 합과 작거나 같은지를 체크함으로써 각 범위 구간에 비례한 확률이 정확하게 보장됩니다.
            if (randVal <= accumulatedWeight)
            {
                drawnRow = row;
                drawnTime = row.stopTime;
                return true;
            }
        }

        // 아주 예외적인 누적 오류 대비 마지막 항목 리턴 안전장치
        if (tableData.Count > 0)
        {
            drawnRow = tableData.DataRows[tableData.Count - 1];
            drawnTime = drawnRow.stopTime;
            return true;
        }

        return false;
    }
}
