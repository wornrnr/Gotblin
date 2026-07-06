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
        // 1. 자신의 RectTransform 레퍼런스 확정 획득
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 체력 최댓값 복구 및 대기 상태 셋업
        currentHP = maxHP;
        currentState = UnitState.Idle;

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

        // 2. 실시간 최단 거리 적 검색 (개선된 싱글 아규먼트 API 연동)
        BaseCombatUnit previousTarget = currentTarget;
        if (CombatManager.Instance != null)
        {
            currentTarget = CombatManager.Instance.GetClosestTarget(this);
        }

        // [동적 오프셋 갱신]: 새로운 적인 타겟으로 변경될 때마다 타겟 주변 360도 방향 무작위 오프셋 재할당
        if (currentTarget != previousTarget && currentTarget != null)
        {
            Vector2 randomPos = Random.insideUnitCircle * (attackRange * 0.8f);
            attackPositionOffset = new Vector3(randomPos.x, randomPos.y, 0f);
        }

        // 3. FSM 상태 전이 판단 (실시간 예외/무결성 Null Check 포함)
        if (currentTarget == null || currentTarget.IsDead())
        {
            currentState = UnitState.Idle;
            currentTarget = null; // 죽은 타겟 참조 정리
        }
        else
        {
            // [부모 계층 좌표 불일치 해결] 두 유닛의 캔버스 상 부모가 달라도 정확히 거리를 잴 수 있도록 월드 공간 거리 측정
            // [포위망 오프셋 반영]: 타겟의 정중앙이 아닌, 타겟 주변의 고유 포위 목표 좌표를 최종 목적지로 계산
            Vector3 targetDest = currentTarget.transform.position + attackPositionOffset;
            float distance = Vector3.Distance(transform.position, targetDest);

            // 오프셋 목적지 기준 15픽셀 내 도달 시 멈춰 서서 공격 가동
            if (distance > 15f)
            {
                // 공격 사정거리보다 멀리 있으면 추적 상태
                currentState = UnitState.Chasing;
            }
            else
            {
                // 공격 사정거리 이내에 진입하면 공격 대기 상태
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
    }

    /// <summary>
    /// 타겟 주변의 포위망 오프셋 목표 지점을 향해 X, Y축 전방위 입체 질주를 수행합니다. (Z축 뒤틀림 방어 내장)
    /// </summary>
    private void MoveTowardsTarget()
    {
        if (currentTarget == null) return;

        Vector3 myPos = transform.position;
        // 타겟 중심 좌표에 고유의 포위 오프셋을 더한 최종 목적지 좌표 구동
        Vector3 targetDest = currentTarget.transform.position + attackPositionOffset;

        // [부모 계층 좌표 불일치 해결] 월드 포지션 기준 사방 정규화 벡터 계산
        Vector2 direction = ((Vector2)(targetDest - myPos)).normalized;

        // 월드 좌표계를 기준으로 직접 이동을 적용하되, Z축 뒤틀림으로 인한 UI 렌더링 꼬임을 완전히 방지
        Vector3 nextPos = transform.position + (Vector3)(direction * moveSpeed * Time.deltaTime);
        nextPos.z = 0f; // Z축 강제 고정
        transform.position = nextPos;

        // X축 이동 방향 기준 스프라이트 localScale 좌우 반전 플립 처리
        if (direction.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction.x > 0f ? 1f : -1f);
            transform.localScale = scale;
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
            Debug.Log($"<color=cyan><b>[CombatUnit]</b></color> <color=yellow>{gameObject.name}</color>이(가) 적 <color=red>{currentTarget.gameObject.name}</color>을(를) 타격! (피해량: {attackDamage})");
            currentTarget.TakeDamage(attackDamage);
        }
    }

    /// <summary>
    /// 외부로부터 공격 피해를 수신하여 체력을 감소시킵니다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (currentState == UnitState.Dead) return;

        // [코딩 제약 조건] 연출 전용 가짜 난전 모드인 경우 피해를 0(무시) 처리
        if (isDecorationMode)
        {
            return;
        }

        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"[{gameObject.name}] 피격 발생! (-{amount} HP) / 현재 체력: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 유닛 사망 처리를 수행하고 전투 감시 리스트에서 즉시 해제한 후 파괴합니다.
    /// </summary>
    private void Die()
    {
        if (currentState == UnitState.Dead) return;

        currentState = UnitState.Dead;
        currentHP = 0;

        Debug.Log($"<color=red><b>[{gameObject.name}]</b></color> 사망하여 전장 및 메모리에서 제거됩니다.");

        // [코딩 제약 조건] 전투 관리자 풀 리스트에서 즉시 제거(Remove) 수행
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

        // 사망 후 유닛 오브젝트 완전 파괴 (Null 포인터 예외 원천 예방)
        Destroy(gameObject);
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
