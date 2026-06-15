using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소셜 그래프 미니게임의 긴장감을 더하기 위해, 시간이 흐를수록 배경 팡파레 이미지를 서서히 페이드인 시키고
/// 회전 속도를 누적 가속화하는 텐션 연출용 백그라운드 스크립트입니다.
/// </summary>
[DisallowMultipleComponent]
public class GraphGameBgJuiceController : MonoBehaviour
{
    [Header("UI 연출 레퍼런스")]
    [Tooltip("회전 및 페이딩 연출을 적용할 배경 팡파레 Image 컴포넌트입니다.")]
    [SerializeField] private Image fanfareImage;

    [Header("Tension Settings")]
    [Tooltip("배경 가시성을 방해하지 않는 선에서 최대로 보일 투명도(Alpha) 값입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.3f;

    [Tooltip("최대 투명도(maxAlpha)에 도달하기까지 경과할 기준 시간(초)입니다.")]
    [SerializeField] private float fadeDuration = 10f;

    [Tooltip("라운드 시작 시점의 최초 배경 회전 속도 (도/초)입니다.")]
    [SerializeField] private float baseRotationSpeed = 10f;

    [Tooltip("시간이 지나 가속되더라도 넘어가지 않을 최대 회전 속도 한계치 (도/초)입니다.")]
    [SerializeField] private float maxRotationSpeed = 360f;

    [Tooltip("초당 회전 속도가 얼마나 누적 가속화될지 조절하는 가속도 계수입니다.")]
    [SerializeField] private float rotationAcceleration = 15f;

    private bool isRunning = false;

    private void Start()
    {
        // 1. 방어 코드
        if (fanfareImage == null)
        {
            Debug.LogError("[GraphGameBgJuiceController] fanfareImage 컴포넌트 레퍼런스가 누락되었습니다!");
            enabled = false;
            return;
        }

        // 2. GameManager의 전역 상태 이벤트 구독
        GraphGameManager.OnStateChanged += HandleStateChanged;

        // 3. 씬 진입 시 초기 동기화 보장
        if (GraphGameManager.Instance != null)
        {
            HandleStateChanged(GraphGameManager.Instance.CurrentState);
        }
        else
        {
            ResetFanfare();
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시의 이벤트 구독 해제
        GraphGameManager.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (GraphGameManager.Instance == null) return;

        float currentTimer = GraphGameManager.Instance.CurrentTimer;

        // 1. 실시간 페이드인 연산 (0 나누기 방지를 위해 Max 적용)
        float targetFadeDuration = Mathf.Max(0.001f, fadeDuration);
        float currentAlpha = Mathf.Min(maxAlpha, (currentTimer / targetFadeDuration) * maxAlpha);
        
        Color c = fanfareImage.color;
        c.a = currentAlpha;
        fanfareImage.color = c;

        // 2. 실시간 회전 속도 계산 및 프레임 독립 회전 적용
        float currentSpeed = Mathf.Min(maxRotationSpeed, baseRotationSpeed + (currentTimer * rotationAcceleration));
        fanfareImage.rectTransform.Rotate(0f, 0f, currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 게임 상태 전환에 따라 팡파레 연출 작동을 제어합니다.
    /// </summary>
    private void HandleStateChanged(GraphGameState state)
    {
        switch (state)
        {
            case GraphGameState.Ready:
                ResetFanfare();
                break;

            case GraphGameState.Running:
                // 회전 및 페이딩 작동 개시
                isRunning = true;
                break;

            case GraphGameState.Success:
            case GraphGameState.Busted:
                // 성공 및 폭발 시점의 상태를 그대로 락(Freeze)
                isRunning = false;
                break;
        }
    }

    /// <summary>
    /// 팡파레 연출을 초기 투명도(0) 및 기본 정방향 회전(0도)으로 복구합니다.
    /// </summary>
    private void ResetFanfare()
    {
        isRunning = false;

        if (fanfareImage != null)
        {
            Color c = fanfareImage.color;
            c.a = 0f;
            fanfareImage.color = c;

            fanfareImage.rectTransform.localRotation = Quaternion.identity;
        }
    }
}
