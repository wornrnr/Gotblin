using UnityEngine;

/// <summary>
/// 전투 유닛의 기본 상태 모델(대기, 추적, 공격, 사망)을 나타내는 Enum입니다.
/// </summary>
public enum UnitState { Idle, Chasing, Attacking, Dead }

/// <summary>
/// 플레이어 고블린 유닛과 적 몬스터 유닛이 공통으로 상속받아 동작하는
/// 수학적 2D 캔버스 anchoredPosition 거리 연산 기반 자동 전투 AI 및 FSM 기초 컴포넌트입니다.
/// 물리 충돌 연산 없이 UGUI 화면 내 픽셀 해상도 독립적인 이동을 보장합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class BaseCombatUnit : MonoBehaviour
{
    [Header("소속 팀 설정")]
    [Tooltip("true 이면 몬스터(적군) 진영이며, false 이면 고블린(아군 플레이어) 진영입니다.")]
    public bool isEnemy;

    [Tooltip("이 유닛이 스테이지 보스인지 여부입니다. 인스펙터에서 보스 프리팹은 체크해 주세요.")]
    public bool isBoss;

    [Tooltip("유닛 고유의 덩치 크기(충돌 반경) 최소 대치 거리입니다. (일반몹: 50, 보스: 120 권장)")]
    public float bodyRadius = 50f;

    [Tooltip("전투 진행 중 사망이나 체력 차감이 일어나지 않는 가짜 연출 전용 유닛인지 여부입니다.")]
    public bool isDecorationMode = false;

    [Header("전투 능력치 스탯")]
    [Tooltip("유닛이 가질 수 있는 최대 체력(HP) 값입니다.")]
    public int maxHP = 100;

    [Tooltip("유닛의 현재 실시간 체력(HP) 값입니다. (CombatManager 리스트 검증을 위해 퍼블릭 개방)")]
    public int currentHP;

    [Tooltip("유닛이 쿨타임마다 타겟에게 입힐 피해량(데미지)입니다.")]
    public int attackDamage = 10;

    [Tooltip("타겟을 공격하기 위해 근접해야 하는 수학적 최소 거리(사정거리)입니다.")]
    public float attackRange = 50f;

    [Tooltip("공격을 한 번 수행한 뒤 다시 수행하기까지 대기할 쿨타임 주기(초 단위)입니다.")]
    public float attackCooldown = 1.2f;

    [Tooltip("공격 모션 발동 후 실제 무기가 휘둘러져 타격이 들어가는 지연 시간(초 단위)입니다.")]
    public float attackHitDelay = 0.25f;

    [Tooltip("타겟을 추적할 때 적용될 평면 질주 속도입니다.")]
    public float moveSpeed = 100f;

    [Header("실시간 AI 상태")]
    [Tooltip("현재 이 유닛이 취하고 있는 AI 상태 머신 정보입니다.")]
    [SerializeField] private UnitState currentState = UnitState.Idle;

    private RectTransform rectTransform;
    private BaseCombatUnit currentTarget;
    private float attackTimer = 0f;

    // 포위 연출 시 겹침 방지를 위해 지정되는 고유의 타겟 타격 오프셋 벡터
    private Vector3 attackPositionOffset;

    // 보스 처치 후 오른쪽 퇴장 걷기 연출 작동 제어용 플래그
    private bool isVictoryWalking = false;

    // [애니메이션 동기화용] 비주얼 컨트롤러 캐시
    private HeroVisualController visualController;

    // [타격감 연출] 피격 이펙트 제어용 멤버 변수군
    private Coroutine hitEffectCoroutine;
    private Color originalColor = Color.white;
    private Vector3 originalScale = Vector3.one;
    private UnityEngine.UI.Image unitImage; // 스프라이트 컬러 변경용 (Image 컴포넌트)

    /// <summary>
    /// 보스 퇴치 후 영웅에게 전방위 AI를 종료하고 강제로 우측 화면 퇴장 명령을 내립니다.
    /// </summary>
    public void MoveToRightExit()
    {
        isVictoryWalking = true;
    }

    /// <summary>
    /// 오브젝트 풀에서 유닛을 꺼내 재사용할 때 호출되는 상태 초기화 및 연출 찌꺼기 청소 함수입니다.
    /// </summary>
    public void ResetUnitStateForReuse()
    {
        // 1. 체력 완충
        currentHP = maxHP;
        
        // 2. 타겟팅 및 상태 비활성 초기화
        currentTarget = null;
        isVictoryWalking = false;

        // 3. [피격 찌꺼기 정리]: 넉백 코루틴 중단 및 크기, 컬러 원상복구
        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = null;
        }
        
        transform.localScale = originalScale;
        
        if (unitImage == null)
        {
            unitImage = GetComponent<UnityEngine.UI.Image>();
        }
        
        if (unitImage != null)
        {
            unitImage.color = originalColor;
        }

        currentState = UnitState.Idle;

        // [전역 타겟팅 연동 재등록]
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RegisterUnit(this);
        }
    }

    /// <summary>
    /// 퇴장이 완료된 영웅을 초기 전장 시작 지점으로 복귀 및 체력 완충 리셋시킵니다.
    /// Vector3.zero(또는 zero 벡터)가 주어지면 UGUI 로컬 anchoredPosition 기준으로 Vector2.zero를 직접 대입합니다.
    /// </summary>
    public void ResetToInitialPosition(Vector3 destination)
    {
        isVictoryWalking = false;

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (destination == Vector3.zero && rectTransform != null)
        {
            // Vector3.zero가 주어질 경우, UGUI 로컬 좌표계인 anchoredPosition 기준으로 Vector2.zero를 직접 주입
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            transform.position = destination;
        }

        currentHP = maxHP;
        currentState = UnitState.Idle;

        // [전역 타겟팅 연동 재등록]: 사망 등으로 풀에서 언레지스터되었던 유닛을 다시 CombatManager 리스트에 등록
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RegisterUnit(this);
        }
    }

    // 외부 연동 및 상대 픽셀 거리 측정을 위한 RectTransform 노출 프로퍼티
    public RectTransform MyRect => rectTransform;

    // 외부 조회용 능력치 프로퍼티
    public int AttackDamage => attackDamage;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 1. 자신의 RectTransform 및 VisualController 레퍼런스 확정 획득
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        visualController = GetComponent<HeroVisualController>();

        // 체력 최댓값 복구 및 대기 상태 셋업
        currentHP = maxHP;
        currentState = UnitState.Idle;

        // [타격감 연출] 초기값 획득 및 백업
        unitImage = GetComponent<UnityEngine.UI.Image>();
        if (unitImage != null)
        {
            originalColor = unitImage.color;
        }
        originalScale = transform.localScale;

        // [포위망 산개 연출]: 사거리(attackRange)의 80% 반경 내에서 2D 무작위 위치를 지정하여 겹침 방지
        Vector2 randomPos = Random.insideUnitCircle * (attackRange * 0.8f);
        attackPositionOffset = new Vector3(randomPos.x, randomPos.y, 0f);

        // 2. [전역 타겟팅 연동] 전투 매니저 유닛 진영 풀에 직접 추가 등록
        if (CombatManager.Instance != null)
        {
            if (!isEnemy)
            {
                if (!CombatManager.Instance.playerUnits.Contains(this))
                {
                    CombatManager.Instance.playerUnits.Add(this);
                }
            }
            else
            {
                if (!CombatManager.Instance.enemyUnits.Contains(this))
                {
                    CombatManager.Instance.enemyUnits.Add(this);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 씬 전환이나 오브젝트 파괴 시 전투 풀 리스트 파손을 방지하기 위한 등록 해제
        if (CombatManager.Instance != null)
        {
            if (isEnemy)
            {
                CombatManager.Instance.enemyUnits.Remove(this);
            }
            else
            {
                CombatManager.Instance.playerUnits.Remove(this);
            }
        }
    }

    private void Update()
    {
        // 1. 사망 판정 시 추가 AI 연산 즉각 제외
        if (currentState == UnitState.Dead) return;

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        // [기획 규칙 추가] 승리 퇴장 연출 중에는 적인 타겟 유무와 무관하게 오른쪽으로 전진합니다.
        if (isVictoryWalking)
        {
            // 우측 방향 벡터 이동 (Z축 고정 방어 내장)
            Vector3 nextPos = transform.position + Vector3.right * moveSpeed * Time.deltaTime;
            nextPos.z = 0f;
            transform.position = nextPos;

            // 우측 걷기이므로 X 스케일 플립 처리도 정상 방향 유지
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;

            currentState = UnitState.Chasing; // 걷기 모션 FSM 강제
            return;
        }

        // [기획 규칙 추가] 카메라 연출(Transition) 중에는 아군 유닛의 AI 작동을 일시정지시킵니다.
        if (!isEnemy && CombatStageManager.Instance != null && 
            CombatStageManager.Instance.currentMode == CombatMode.Transition)
        {
            // 연출 시간 동안 제자리에 멈춰 서서 대기합니다.
            currentState = UnitState.Idle;
            return;
        }

        // 2. 적 탐색 (현재 타겟이 없거나 이미 처치된 경우에만 새로운 최단 거리 적 검색)
        if (currentTarget == null || currentTarget.IsDead() || !currentTarget.gameObject.activeInHierarchy)
        {
            BaseCombatUnit previousTarget = currentTarget;
            if (CombatManager.Instance != null)
            {
                currentTarget = CombatManager.Instance.GetClosestTarget(this);
            }

            // [동적 오프셋 갱신 및 쿨타임 초기화]: 새로운 적인 타겟으로 변경될 때마다 오프셋 할당 및 공격 타이머 리셋
            if (currentTarget != previousTarget && currentTarget != null)
            {
                Vector2 randomPos = Random.insideUnitCircle * (attackRange * 0.8f);
                attackPositionOffset = new Vector3(randomPos.x, randomPos.y, 0f);
                attackTimer = 0f;
            }
        }

        // [모듈형 비주얼 뒤집기 연동] 적이 내 왼쪽에 있다면 왼쪽을 바라보고, 오른쪽에 있다면 오른쪽을 바라봄
        if (currentTarget != null)
        {
            HeroVisualController visualController = GetComponent<HeroVisualController>();
            if (visualController != null)
            {
                bool isTargetLeft = currentTarget.transform.position.x < transform.position.x;
                visualController.SetFacingDirection(isTargetLeft);
            }
        }

        // 3. FSM 상태 전이 판단 (실시간 예외/무결성 Null Check 포함)
        if (currentTarget == null || currentTarget.IsDead())
        {
            currentState = UnitState.Idle;
            currentTarget = null; // 죽은 타겟 참조 정리
            attackTimer = 0f; // 타겟 소멸 시 쿨타임 타이머 즉시 리셋
        }
        else
        {
            // [부모 계층 좌표 불일치 해결] 두 유닛의 캔버스 상 부모가 달라도 정확히 거리를 잴 수 있도록 월드 공간 거리 측정
            // [포위망 오프셋 반영]: 타겟의 정중앙이 아닌, 타겟 주변의 고유 포위 목표 좌표를 최종 목적지로 계산
            Vector3 targetDest = currentTarget.transform.position + attackPositionOffset;
            
            float targetRadius = currentTarget.bodyRadius;
            float minKeepDistance = this.bodyRadius + targetRadius;
            
            // 타겟 중심과의 절대 월드 거리 측정
            float distanceToTargetCenter = Vector3.Distance(transform.position, currentTarget.transform.position);

            // [사거리 & 덩치 겹침 방지 결합]: 최소 필수 대치 간격보다 멀고, 사정거리(attackRange)보다도 멀 때만 추적(Chasing)합니다.
            if (distanceToTargetCenter > minKeepDistance && distanceToTargetCenter > attackRange)
            {
                // 아직 사거리 밖이고 목적지에도 도달하지 못했다면 추적 상태
                currentState = UnitState.Chasing;
            }
            else
            {
                // 덩치 경계선에 닿았거나 사거리 내 진입했다면 대치 및 공격 상태(Attacking)로 돌입 (제자리 정지)
                currentState = UnitState.Attacking;
            }
        }

        // 4. 상태별 구체적인 행동 구동 분기
        switch (currentState)
        {
            case UnitState.Idle:
                attackTimer = 0f;
                break;

            case UnitState.Chasing:
                attackTimer = 0f;
                MoveTowardsTarget();
                break;

            case UnitState.Attacking:
                ExecuteAttackLogic();
                break;
        }

        // 5. [애니메이션 동기화] 이동 중인지 여부를 visualController에 실시간 전달
        if (visualController != null)
        {
            bool isMovingNow = (currentState == UnitState.Chasing || isVictoryWalking);
            visualController.SetMoveAnimation(isMovingNow);
        }

        // [배경 영역 범위 제한] 매 프레임 위치가 배경판 경계 바깥으로 벗어나지 않도록 고정
        ClampPositionToBackground();
    }

    private void LateUpdate()
    {
        // 렌더링 최종 프레임 직전 유닛 좌표가 battleBackground 경계 영역 밖으로 벗어나는 것을 원천 차단
        ClampPositionToBackground();
    }

    /// <summary>
    /// 유닛의 현재 좌표가 battleBackground(배경 판) 영역 밖으로 벗어나지 않도록 강제로 경계선 내부로 제약(Clamp)합니다.
    /// </summary>
    public void ClampPositionToBackground()
    {
        if (CombatStageManager.Instance == null || CombatStageManager.Instance.battleBackground == null) return;

        RectTransform bgRect = CombatStageManager.Instance.battleBackground;
        Vector3[] corners = new Vector3[4];
        bgRect.GetWorldCorners(corners);

        // 여백(bodyRadius)을 감안하여 유닛의 중심점이 배경 판 경계를 벗어나지 않게 설정
        float minX = corners[0].x + bodyRadius;
        float maxX = corners[2].x - bodyRadius;
        float minY = corners[0].y + bodyRadius;
        float maxY = corners[1].y - bodyRadius;

        if (minX > maxX) { float temp = minX; minX = maxX; maxX = temp; }
        if (minY > maxY) { float temp = minY; minY = maxY; maxY = temp; }

        Vector3 currentPos = transform.position;
        float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);

        if (currentPos.x != clampedX || currentPos.y != clampedY)
        {
            currentPos.x = clampedX;
            currentPos.y = clampedY;
            currentPos.z = 0f;
            transform.position = currentPos;
        }
    }

    /// <summary>
    /// 상대방의 덩치 반경(bodyRadius)과 포위 오프셋을 종합하여, 겹치지 않는 경계선 목적지까지만 전방위 입체 질주를 수행합니다.
    /// </summary>
    private void MoveTowardsTarget()
    {
        if (currentTarget == null) return;

        float targetRadius = currentTarget.bodyRadius;
        float minKeepDistance = this.bodyRadius + targetRadius;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        // 상대방과의 겹침 최소 거리에 산개 오프셋을 더한 안전 목적지(destination) 산출
        Vector3 offsetDir = attackPositionOffset != Vector3.zero ? attackPositionOffset.normalized : (myPos - targetPos).normalized;
        Vector3 destination = targetPos + (offsetDir * minKeepDistance);

        // 내 현재 위치에서 안전 목적지까지의 방향 지향 벡터
        Vector3 moveDir = (destination - myPos);
        moveDir.z = 0; // Z축 고정

        if (moveDir.sqrMagnitude > 1f)
        {
            Vector3 direction = moveDir.normalized;

            // 월드 좌표계를 기준으로 안전 범위까지만 이동
            Vector3 nextPos = transform.position + (Vector3)(direction * moveSpeed * Time.deltaTime);
            nextPos.z = 0f; // Z축 렌더링 순서 보장
            transform.position = nextPos;

            // X축 이동 방향 기준 스프라이트 localScale 좌우 반전 플립 처리 (모듈형 비주얼 컨트롤러가 없는 유닛만 개별 가동)
            if (direction.x != 0f && GetComponent<HeroVisualController>() == null)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction.x > 0f ? 1f : -1f);
                transform.localScale = scale;
            }
        }
    }

    /// <summary>
    /// 공격 쿨타임을 누적하고, 주기가 완료되면 타겟에 피해를 입힙니다.
    /// </summary>
    private void ExecuteAttackLogic()
    {
        if (currentTarget == null || currentTarget.IsDead()) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            
            // [애니메이션 동기화] 무기를 휘두르는 타격 발생 순간 Attack 트리거 가동
            if (visualController != null)
            {
                visualController.TriggerAttackAnimation();
            }

            // [타격 시점 동기화] attackHitDelay(기본 0.25초) 지연 후 무기가 적에게 맞는 타이밍에 TakeDamage 수신
            StartCoroutine(PerformDelayedDamage(currentTarget, attackDamage, attackHitDelay));
        }
    }

    /// <summary>
    /// 무기 모션 궤적(휘두르는 프레임) 타이밍에 맞춰 타격 피해를 전달하는 지연 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator PerformDelayedDamage(BaseCombatUnit target, int damage, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 지연 시간 후 타깃과 나 자신의 상태 무결성 검증 (사망, 파괴, 사망한 적 예방)
        if (target != null && !target.IsDead() && !IsDead() && currentState != UnitState.Dead)
        {
            Debug.Log($"<color=cyan><b>[CombatUnit]</b></color> <color=yellow>{gameObject.name}</color>이(가) 적 <color=red>{target.gameObject.name}</color>을(를) 휘둘러 타격! (피해량: {damage})");
            target.TakeDamage(damage, transform.position);
        }
    }

    /// <summary>
    /// 외부로부터 공격 피해를 수신하여 체력을 감소시킵니다. (피격 넉백 연출 포함)
    /// </summary>
    /// <param name="amount">데미지량</param>
    /// <param name="attackerPosition">나를 가격한 상대방의 위치 좌표</param>
    public void TakeDamage(int amount, Vector3 attackerPosition)
    {
        if (currentState == UnitState.Dead) return;

        // [코딩 제약 조건] 연출 전용 가짜 난전 모드인 경우 피해를 0(무시) 처리
        if (isDecorationMode)
        {
            return;
        }

        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"[{gameObject.name}] 피격 발생! (-{amount} HP) / 현재 체력: {currentHP}/{maxHP}");

        // [피해량 텍스트 연출 트리거]: 내가 적(isEnemy=true)이면 공격자는 히어로(isHeroAttacking=true), 내가 히어로이면 공격자는 적(isHeroAttacking=false)
        if (DamageTextManager.Instance != null)
        {
            bool isHeroAttacking = isEnemy; // 피격자가 적이면 타격한 주체는 히어로 고블린
            DamageTextManager.Instance.ShowDamageText(amount, isHeroAttacking, transform.position);
        }

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        // [타격감 연출 추가] 생존해 있다면 3종 피격 피드백 실행
        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
            // 연속 피격 시 컬러가 누적되어 꼬이지 않도록 원상복구 후 재시작
            if (unitImage != null) unitImage.color = originalColor;
        }

        hitEffectCoroutine = StartCoroutine(HitFeedbackSequence(attackerPosition));
    }

    /// <summary>
    /// 피격 3종 피드백 (0.15초 붉은 플래시, 스케일 움찔 펄스, 탄성 넉백 복원) 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator HitFeedbackSequence(Vector3 attackerPos)
    {
        if (unitImage == null) unitImage = GetComponent<UnityEngine.UI.Image>();

        float duration = 0.15f; // 연출 시간
        float elapsed = 0f;

        // 1. 피격 컬러 붉은색 플래시 시작
        if (unitImage != null)
        {
            unitImage.color = new Color(1f, 0.3f, 0.3f, 1f);
        }

        Vector3 startPosition = transform.position;
        Vector3 currentScale = transform.localScale; // 피격 발생 직전 유닛이 바라보던 localScale 방향 기억
        Vector3 knockbackDirection = (transform.position - attackerPos).normalized;
        knockbackDirection.z = 0;

        float knockbackDist = 20f; // 넉백될 최대 픽셀 거리

        // [경계선 방어벽 추출]
        float minX = float.MinValue, maxX = float.MaxValue;
        float minY = float.MinValue, maxY = float.MaxValue;

        if (CombatStageManager.Instance != null && CombatStageManager.Instance.battleBackground != null)
        {
            Vector3[] bgCorners = new Vector3[4];
            CombatStageManager.Instance.battleBackground.GetWorldCorners(bgCorners);
            
            // 여백(bodyRadius)을 고려하여 유닛의 중심점이 배경 밖으로 나가지 않도록 경계선 설정
            minX = bgCorners[0].x + bodyRadius;
            maxX = bgCorners[2].x - bodyRadius;
            minY = bgCorners[0].y + bodyRadius;
            maxY = bgCorners[1].y - bodyRadius;

            // min/max 뒤틀림 예방 안전장치
            if (minX > maxX) { float temp = minX; minX = maxX; maxX = temp; }
            if (minY > maxY) { float temp = minY; minY = maxY; maxY = temp; }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // 1) 움찔 스케일 연출: Sin 곡선을 타며 현재 바라보던 방향 스케일(currentScale) 부호를 보존하며 펄스
            float scaleCurve = Mathf.Sin(percent * Mathf.PI);
            transform.localScale = currentScale * (1f + (scaleCurve * 0.15f));

            // 2) 탄성 넉백 연출: 밀렸다가 다시 원위치(startPosition)로 돌아오는 탄성 운동
            float motionCurve = Mathf.Sin(percent * Mathf.PI);
            Vector3 targetPosition = startPosition + (knockbackDirection * knockbackDist * motionCurve);

            // [기획 규칙 추가] 계산된 넉백 위치가 배경화면을 절대 뚫고 나가지 못하도록 고정
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

            transform.position = targetPosition;

            yield return null;
        }

        // 연출 시간 완료 후 피격 직전 바라보던 부호 방향(currentScale)으로 최종 복원
        transform.localScale = currentScale;
        transform.position = startPosition;
        if (unitImage != null)
        {
            unitImage.color = originalColor;
        }
        hitEffectCoroutine = null;
    }

    /// <summary>
    /// 유닛 사망 처리를 수행하고 전투 감시 리스트에서 즉시 해제한 후 파괴 또는 풀로 반납합니다.
    /// </summary>
    private void Die()
    {
        if (currentState == UnitState.Dead) return;

        currentState = UnitState.Dead;
        currentHP = 0;

        Debug.Log($"<color=red><b>[{gameObject.name}]</b></color> 사망하여 전장에서 해제됩니다.");

        // 내가 스테이지 보스였다면 사망 즉시 매니저에게 카메라 추적 락 신호 보고
        if (isBoss && CombatStageManager.Instance != null)
        {
            CombatStageManager.Instance.OnBossKilled();
        }

        // 전투 관리자 풀 리스트에서 즉시 제거 수행
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.UnregisterUnit(this);
        }

        // [풀링 반납 연동]: 일반 몬스터(isEnemy && !isBoss)라면 Destroy 하지 않고 풀에 안전 반납
        if (isEnemy && !isBoss)
        {
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.ReturnEnemyToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else if (isEnemy && isBoss)
        {
            // 보스 몬스터는 기존 방식대로 씬에서 완전 파괴(Destroy) 처리
            Destroy(gameObject);
        }
        else
        {
            // [아군 히어로 고블린 (!isEnemy)]: 현재 전투 모드(ChallengeMode vs IdleMode)를 식별하여 올바른 패배/재시작 시퀀스 호출
            if (CombatStageManager.Instance != null)
            {
                if (CombatStageManager.Instance.currentMode == CombatMode.ChallengeMode)
                {
                    CombatStageManager.Instance.EndChallenge(false);
                }
                else
                {
                    CombatStageManager.Instance.OnHeroDiedInIdleMode();
                }
            }
        }
    }

    /// <summary>
    /// RectTransform 보유 여부에 맞게 anchoredPosition 또는 일반 position 좌표를 반환합니다.
    /// </summary>
    public Vector2 GetPosition()
    {
        if (rectTransform != null)
        {
            return rectTransform.anchoredPosition;
        }
        return transform.position;
    }

    /// <summary>
    /// 유닛의 사망 여부를 확인하는 조회 메서드입니다.
    /// </summary>
    public bool IsDead()
    {
        return currentState == UnitState.Dead || currentHP <= 0;
    }
}
