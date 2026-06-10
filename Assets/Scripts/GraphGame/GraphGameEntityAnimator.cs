using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소셜 그래프 미니게임 진행 상태에 연동하여 고블린의 좌우 왕복/탈출 연출 및
/// 드래곤의 상태별 다중 프레임 플립북 애니메이션(잠자기/공격)을 제어하는 비주얼 연출 매니저입니다.
/// 부모 패널의 피벗(Pivot) 치우침이나 앵커 꼬임으로 인한 편향 좌표 왕복(예: -1010 ~ -70)을 완벽 수용하도록 설계되었습니다.
/// </summary>
[DisallowMultipleComponent]
public class GraphGameEntityAnimator : MonoBehaviour
{
    [Header("UI 연출 컴포넌트 레퍼런스")]
    [Tooltip("고블린이 왕복 이동할 기준 배경 패널의 RectTransform입니다.")]
    [SerializeField] private RectTransform centerPanel;

    [Tooltip("이동을 수행할 고블린 캐릭터의 RectTransform입니다.")]
    [SerializeField] private RectTransform goblinTransform;

    [Tooltip("고블린의 실시간 활성 및 프레임 이미지를 교체할 이미지 컴포넌트입니다.")]
    [SerializeField] private Image goblinImage;

    [Tooltip("이미지 스위칭을 연출할 드래곤 이미지 컴포넌트입니다.")]
    [SerializeField] private Image dragonImage;

    [Header("Dragon Sprite Sheets")]
    [Tooltip("Ready 및 평상시 자고 있는 드래곤의 애니메이션 스프라이트 배열입니다.")]
    public Sprite[] dragonSleepingSprites;

    [Tooltip("Busted 발각 시 브레스를 뿜는 드래곤의 애니메이션 스프라이트 배열입니다.")]
    public Sprite[] dragonAttackSprites;

    [Tooltip("드래곤 애니메이션의 초당 프레임 수(FPS)입니다.")]
    public float dragonAnimationFPS = 6f;

    [Header("Goblin Settings")]
    [Tooltip("Running 진행 시 고블린의 실시간 좌우 질주 속도입니다.")]
    [SerializeField] private float moveSpeed = 400f;

    [Tooltip("고블린이 배너 끝에 도달했을 때 멈출 안전 마진 패딩 값입니다. (수동 범위 입력 시 작동 안함)")]
    [SerializeField] private float margin = 50f;

    [Tooltip("고블린이 왕복할 수동 가로 최소(좌측) 로컬 좌표입니다. (0으로 두면 자동 해상도 연산 적용)")]
    [SerializeField] private float patrolMinX = 0f;

    [Tooltip("고블린이 왕복할 수동 가로 최대(우측) 로컬 좌표입니다. (0으로 두면 자동 해상도 연산 적용)")]
    [SerializeField] private float patrolMaxX = 0f;

    [Header("Goblin Sprite Sheet Animation")]
    [Tooltip("고블린이 달릴 때 재생할 스프라이트 시트 배열입니다.")]
    public Sprite[] goblinRunSprites;

    [Tooltip("고블린 애니메이션의 초당 재생할 스프라이트 프레임 수(FPS)입니다.")]
    public float goblinAnimationFPS = 12f;

    // 고블린 실시간 상태 관리 변수군
    private bool isRunningAnimation = false;
    private float currentDirection = 1f; // 1: 오른쪽 이동, -1: 왼쪽 이동
    private float baseGoblinScaleX = 1f;   // 인스펙터에 설정된 고블린의 기본 X 스케일 크기 보존용
    
    // 고블린 프레임 애니메이션용 제어 변수군
    private int currentGoblinFrameIndex = 0;
    private float goblinFrameTimer = 0f;

    // 드래곤 프레임 애니메이션용 제어 변수군
    private Sprite[] currentDragonSprites;
    private int currentDragonFrameIndex = 0;
    private float dragonFrameTimer = 0f;

    private Coroutine currentActiveCoroutine;

    private void Start()
    {
        // 1. 방어적 예외 검증
        if (centerPanel == null || goblinTransform == null || goblinImage == null || dragonImage == null)
        {
            Debug.LogError("[GraphGameEntityAnimator] centerPanel, goblinTransform, goblinImage 또는 dragonImage 레퍼런스가 누락되었습니다!");
            enabled = false;
            return;
        }

        // 고블린 초기 세팅의 X 스케일 원형 크기를 기억하여 플립 방향 전환 시 왜곡 방지
        baseGoblinScaleX = Mathf.Abs(goblinTransform.localScale.x);

        // 2. GameManager의 상태 전이 전역 이벤트 구독
        GraphGameManager.OnStateChanged += HandleStateChanged;

        // 3. 씬 최초 진입 시 초기 셋팅 보장
        if (GraphGameManager.Instance != null)
        {
            HandleStateChanged(GraphGameManager.Instance.CurrentState);
        }
        else
        {
            HandleStateChanged(GraphGameState.Ready);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 강제 해제로 씬 메모리 릭 방어
        GraphGameManager.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        // 1. 드래곤의 애니메이션은 대기/공격에 상관없이 항상 프레임이 순환 재생되어야 합니다.
        UpdateDragonAnimation();

        // 2. Running 상태일 때만 고블린이 실시간 좌우 왕복 이동 및 달리기 애니메이션을 수행합니다.
        if (isRunningAnimation)
        {
            UpdateGoblinPatrol();
            UpdateGoblinAnimation();
        }
    }

    /// <summary>
    /// 게임 상태 전환 이벤트를 수신하여 그에 대응하는 비주얼 연출을 동기화 스위칭합니다.
    /// </summary>
    private void HandleStateChanged(GraphGameState state)
    {
        // 실행 중이던 비주얼 코루틴이 있다면 충돌 방지를 위해 즉각 중단
        StopActiveCoroutine();

        switch (state)
        {
            case GraphGameState.Ready:
                SetupReadyState();
                break;

            case GraphGameState.Running:
                SetupRunningState();
                break;

            case GraphGameState.Success:
                SetupSuccessState();
                break;

            case GraphGameState.Busted:
                SetupBustedState();
                break;
        }
    }

    /// <summary>
    /// Ready 진입 시: 고블린 위치 리셋, 고블린 스프라이트 1프레임 초기화 및 드래곤 잠자기 애니메이션 셋업
    /// </summary>
    private void SetupReadyState()
    {
        isRunningAnimation = false;

        // 고블린 애니메이션 프레임 제어 인덱스 리셋
        currentGoblinFrameIndex = 0;
        goblinFrameTimer = 0f;

        if (goblinTransform != null)
        {
            goblinTransform.gameObject.SetActive(true);
            
            // 수동 최소/최대 좌표 범위가 설정되어 있다면, 고블린의 대기 정지 중심점(X)을 두 범위의 정가운데로 정렬합니다.
            float startX = 0f;
            if (patrolMinX != 0f || patrolMaxX != 0f)
            {
                startX = (patrolMinX + patrolMaxX) / 2f;
            }

            // 고블린을 대기 좌표로 정밀 초기화 (Y값은 기존 디자인 수치 유지)
            goblinTransform.anchoredPosition = new Vector2(startX, goblinTransform.anchoredPosition.y);
            
            // 고블린 바라보는 방향 리셋 (오른쪽 보기)
            SetGoblinFacing(1f);
        }

        // 고블린 정지 상태 첫 프레임(goblinRunSprites[0]) 이미지로 강제 고정
        if (goblinImage != null && goblinRunSprites != null && goblinRunSprites.Length > 0)
        {
            goblinImage.sprite = goblinRunSprites[0];
        }

        // 드래곤 잠자기 애니메이션 프레임 등록
        SetDragonAnimationState(dragonSleepingSprites);
    }

    /// <summary>
    /// Running 진입 시: 고블린 실시간 좌우 패트롤 및 달리기 애니메이션 시작
    /// </summary>
    private void SetupRunningState()
    {
        // 시작 시 고블린을 오른쪽 방향으로 출발하도록 구성
        currentDirection = 1f;
        SetGoblinFacing(currentDirection);

        currentGoblinFrameIndex = 0;
        goblinFrameTimer = 0f;

        // Update 연산 가동 허용
        isRunningAnimation = true;

        // 드래곤은 계속 잠자고 있는 상태를 유지
        if (currentDragonSprites != dragonSleepingSprites)
        {
            SetDragonAnimationState(dragonSleepingSprites);
        }
    }

    /// <summary>
    /// Success 진입 시: 패트롤을 멈추고 고블린이 우측 화면 바깥으로 보물을 안고 내달려 사라지는 탈출 코루틴 실행
    /// </summary>
    private void SetupSuccessState()
    {
        isRunningAnimation = false;

        // 탈출 연출 코루틴 구동
        currentActiveCoroutine = StartCoroutine(SuccessEscapeCoroutine());
        
        // 드래곤은 성공 시점까지도 계속 자고 있음
        if (currentDragonSprites != dragonSleepingSprites)
        {
            SetDragonAnimationState(dragonSleepingSprites);
        }
    }

    /// <summary>
    /// Busted 진입 시: 그 즉시 동작을 정지시키고 드래곤을 브레스/공격 애니메이션으로 즉각 갱신
    /// </summary>
    private void SetupBustedState()
    {
        isRunningAnimation = false;

        // 드래곤 공격 분노 애니메이션 리스트 등록
        SetDragonAnimationState(dragonAttackSprites);
    }

    /// <summary>
    /// 수동 입력 범위(MinX ~ MaxX) 혹은 실시간 해상도를 반영하여 화면 밖 이탈이 절대로 일어나지 않도록 클램핑 왕복 질주합니다.
    /// </summary>
    private void UpdateGoblinPatrol()
    {
        float minX = 0f;
        float maxX = 0f;

        // 1. 기획자 수동 가로 좌표 범위 존재 여부에 따른 최소/최대 한계 분기 연산
        if (patrolMinX != 0f || patrolMaxX != 0f)
        {
            // 부모의 피벗이 치우친 경우 수동 로컬 좌표 범위 다이렉트 적용
            minX = patrolMinX;
            maxX = patrolMaxX;
        }
        else
        {
            // 실시간 해상도 대응을 위한 가로폭 자동 계산 작동
            float widthHalf = centerPanel.rect.width / 2f;
            minX = -widthHalf + margin;
            maxX = widthHalf - margin;
        }

        Vector2 pos = goblinTransform.anchoredPosition;

        // 방향 벡터 속도 연산
        pos.x += currentDirection * moveSpeed * Time.deltaTime;

        // 우측 한계선 도달 시 ➡️ 좌측으로 턴 및 고블린 좌측 플립
        if (pos.x >= maxX)
        {
            pos.x = maxX;
            currentDirection = -1f;
            SetGoblinFacing(currentDirection);
        }
        // 좌측 한계선 도달 시 ➡️ 우측으로 턴 및 고블린 우측 플립
        else if (pos.x <= minX)
        {
            pos.x = minX;
            currentDirection = 1f;
            SetGoblinFacing(currentDirection);
        }

        goblinTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// C# 기반의 프레임 타이머 누적 연산을 가동하여 고블린의 달리기 프레임을 실시간 교체 재생합니다.
    /// </summary>
    private void UpdateGoblinAnimation()
    {
        // 기획자가 고블린 스프라이트를 할당하지 않았을 시 방어 처리
        if (goblinRunSprites == null || goblinRunSprites.Length == 0 || goblinImage == null)
        {
            return;
        }

        if (goblinAnimationFPS <= 0f) return;

        goblinFrameTimer += Time.deltaTime;
        float timePerFrame = 1f / goblinAnimationFPS;

        if (goblinFrameTimer >= timePerFrame)
        {
            goblinFrameTimer -= timePerFrame;
            
            // 다음 프레임 번호 연산 (순환 구조)
            currentGoblinFrameIndex = (currentGoblinFrameIndex + 1) % goblinRunSprites.Length;
            goblinImage.sprite = goblinRunSprites[currentGoblinFrameIndex];
        }
    }

    /// <summary>
    /// 지정된 드래곤 스프라이트 세트를 애니메이션 타깃으로 활성화하고 프레임을 즉시 0번째로 리셋합니다.
    /// </summary>
    private void SetDragonAnimationState(Sprite[] sprites)
    {
        currentDragonSprites = sprites;
        currentDragonFrameIndex = 0;
        dragonFrameTimer = 0f;

        // 상태 전환 직후 즉각 1프레임을 그려 지연 감각을 제거
        if (dragonImage != null && currentDragonSprites != null && currentDragonSprites.Length > 0)
        {
            dragonImage.sprite = currentDragonSprites[0];
        }
    }

    /// <summary>
    /// C# 기반의 프레임 타이머 연산을 가동하여 드래곤의 상태별 애니메이션(수면/공격)을 실시간 루핑 재생합니다.
    /// </summary>
    private void UpdateDragonAnimation()
    {
        // 기획자가 드래곤 스프라이트를 할당하지 않았을 시 방어 처리
        if (currentDragonSprites == null || currentDragonSprites.Length == 0 || dragonImage == null)
        {
            return;
        }

        if (dragonAnimationFPS <= 0f) return;

        dragonFrameTimer += Time.deltaTime;
        float timePerFrame = 1f / dragonAnimationFPS;

        if (dragonFrameTimer >= timePerFrame)
        {
            // 균일한 애니메이션 프레임 재생을 위한 오차 오프셋 감산
            dragonFrameTimer -= timePerFrame;
            
            // 다음 프레임 순환 연산
            currentDragonFrameIndex = (currentDragonFrameIndex + 1) % currentDragonSprites.Length;
            dragonImage.sprite = currentDragonSprites[currentDragonFrameIndex];
        }
    }

    /// <summary>
    /// 고블린 캐릭터의 localScale을 보존 크기에 맞추어 정밀 플립 전환합니다.
    /// </summary>
    /// <param name="direction">1.0f(우측 바라보기) 또는 -1.0f(좌측 바라보기)</param>
    private void SetGoblinFacing(float direction)
    {
        if (goblinTransform == null) return;

        Vector3 localScale = goblinTransform.localScale;
        // X 스케일 부호를 방향 수치에 비례해 반전
        localScale.x = baseGoblinScaleX * Mathf.Sign(direction);
        goblinTransform.localScale = localScale;
    }

    /// <summary>
    /// 탈취 성공 시 보물을 안고 우측 캔버스 외곽 경계선 바깥으로 신속하게 뛰어 나가는 탈출 코루틴입니다.
    /// </summary>
    private IEnumerator SuccessEscapeCoroutine()
    {
        if (goblinTransform == null) yield break;

        // 탈출할 때 고블린이 우측 방향(오른쪽)을 바라보며 도망가도록 세팅
        SetGoblinFacing(1f);

        // 캔버스 가로폭 밖 한계 범위 설정 (수동 범위 설정 여부에 맞추어 안전 퇴장 거리 확보)
        float limitX = (patrolMinX != 0f || patrolMaxX != 0f) ? patrolMaxX : (centerPanel.rect.width / 2f);
        float escapeLimitX = limitX + 200f;

        while (goblinTransform.anchoredPosition.x < escapeLimitX)
        {
            Vector2 pos = goblinTransform.anchoredPosition;
            // 탈출 성공 시에는 더 빠르고 가벼운 발걸음으로 질주하도록 속도 가산 연출
            pos.x += moveSpeed * 1.5f * Time.deltaTime;
            goblinTransform.anchoredPosition = pos;

            // 탈출하는 도중에도 달리기 달리는 플립북 애니메이션이 매끄럽게 흐르도록 프레임 갱신 연계
            UpdateGoblinAnimation();
            
            yield return null;
        }

        // 화면 밖으로 완전히 탈출 성공하면 고블린 오브젝트를 보이지 않도록 숨김
        goblinTransform.gameObject.SetActive(false);
        Debug.Log("[GraphGameEntityAnimator] 고블린이 보물을 탈취해 기지 밖으로 탈출 성공하여 숨김 처리되었습니다.");
    }

    /// <summary>
    /// 실행 중인 연출용 코루틴 스택을 안전하게 강제 초기화 수거합니다.
    /// </summary>
    private void StopActiveCoroutine()
    {
        if (currentActiveCoroutine != null)
        {
            StopCoroutine(currentActiveCoroutine);
            currentActiveCoroutine = null;
        }
    }
}
