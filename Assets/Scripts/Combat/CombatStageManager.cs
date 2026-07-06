using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 모드 상태를 구분하는 Enum입니다.
/// IdleMode: 방치 파밍 (데미지 없는 난전 연출)
/// Transition: 맵 이동 및 전진 연출 구간
/// ChallengeMode: 보스전 (제한 시간 내 진검승부)
/// </summary>
public enum CombatMode { IdleMode, Transition, ChallengeMode }

/// <summary>
/// 방치 파밍 모드와 보스 도전 모드 간의 시퀀스 전환, 벨트스크롤 형태의 배경 스크롤 카메라 무빙,
/// 그리고 보스전의 실시간 승패(시간 초과 및 생존 여부)를 제어하는 전역 전투 흐름 매니저 싱글톤입니다.
/// </summary>
[DisallowMultipleComponent]
public class CombatStageManager : MonoBehaviour
{
    // 흐름 제어 싱글톤 인스턴스
    public static CombatStageManager Instance { get; private set; }

    [Header("전투 진행 정보")]
    [Tooltip("현재 전투의 진행 모드 상태입니다.")]
    public CombatMode currentMode = CombatMode.IdleMode;

    [Tooltip("현재 돌파 중인 스테이지 번호입니다.")]
    public int currentStage = 1;

    [Header("전투 구역 공간 및 아군 레퍼런스")]
    [Tooltip("맵 이동 시 카메라 전진 연출을 위해 직접 밀어낼 배경 공간 RectTransform입니다.")]
    [SerializeField] private RectTransform combatFieldContext;

    [Tooltip("아군 대표 히어로 고블린의 트랜스폼 레퍼런스입니다.")]
    [SerializeField] private Transform heroGoblin;

    [Header("벨트스크롤 연출 설정")]
    [Tooltip("스테이지 이동 연출 시 화면 스크롤이 진행될 시간(초)입니다.")]
    [SerializeField] private float scrollDuration = 2.0f;

    [Tooltip("보스전 전진 혹은 복귀 시 배경이 밀려날 X축 픽셀 이동 수치입니다.")]
    [SerializeField] private float scrollDistance = 800f;

    [Header("보스전 밸런싱 설정")]
    [Tooltip("보스전 진행 시 주어지는 최대 제한 시간(초)입니다.")]
    [SerializeField] private float challengeTimeLimit = 15f;

    // 실시간 보스 추적용 캐시 변수
    private BaseCombatUnit spawnedBossUnit;
    private float challengeTimer = 0f;

    // 스크롤 연출 시작 좌표 기억용
    private Vector2 initialFieldPos;
    private Vector3 initialHeroPos;
    private Coroutine activeTransitionCoroutine;

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
        }
    }

    private void Start()
    {
        // [디버그 방어용 레퍼런스 누락 검사]
        if (combatFieldContext == null)
        {
            Debug.LogError("<color=red><b>[CombatStageManager] combatFieldContext(배경판) 레퍼런스가 인스펙터에 등록되지 않았습니다! 연출 코루틴 진행 시 크래시가 발생할 수 있습니다.</b></color>");
        }
        if (heroGoblin == null)
        {
            Debug.LogError("<color=red><b>[CombatStageManager] heroGoblin(고블린) 레퍼런스가 인스펙터에 등록되지 않았습니다! 연출 코루틴 진행 시 크래시가 발생할 수 있습니다.</b></color>");
        }

        // 최초 기준 배치 좌표 기억
        if (combatFieldContext != null)
        {
            initialFieldPos = combatFieldContext.anchoredPosition;
        }
        if (heroGoblin != null)
        {
            initialHeroPos = heroGoblin.localPosition;
        }

        // 시작 시 아군 고블린을 연출용 무적 모드로 최초 지정
        SetHeroDecorationMode(true);
    }

    private void Update()
    {
        // ChallengeMode 상태일 때만 보스 처치 타이머 가동 및 실시간 승패 체크
        if (currentMode == CombatMode.ChallengeMode)
        {
            challengeTimer += Time.deltaTime;

            // [코딩 제약 조건] 1. 제한 시간 초과로 인한 패배 판정
            if (challengeTimer >= challengeTimeLimit)
            {
                Debug.Log($"<color=red><b>[CombatStageManager] 보스전 타임아웃! ({challengeTimeLimit}초 초과) 패배 처리합니다.</b></color>");
                EndChallenge(false);
                return;
            }

            // [코딩 제약 조건] 2. 아군 히어로 고블린의 실제 HP 소모에 따른 패배 판정
            BaseCombatUnit heroUnit = heroGoblin != null ? heroGoblin.GetComponent<BaseCombatUnit>() : null;
            if (heroUnit == null || heroUnit.IsDead())
            {
                Debug.Log("<color=red><b>[CombatStageManager] 아군 고블린 사망! 패배 처리합니다.</b></color>");
                EndChallenge(false);
                return;
            }

            // [코딩 제약 조건] 3. 보스 몬스터의 실제 HP 차감 사망에 따른 승리 판정
            if (spawnedBossUnit == null || spawnedBossUnit.IsDead())
            {
                Debug.Log("<color=cyan><b>[CombatStageManager] 스테이지 보스 처치 완료! 승리 처리합니다.</b></color>");
                EndChallenge(true);
                return;
            }
        }
    }

    /// <summary>
    /// 도전 버튼을 클릭했을 때 방치 모드에서 보스전으로 상태를 전이시킵니다.
    /// </summary>
    public void TriggerChallengeMode()
    {
        if (currentMode != CombatMode.IdleMode)
        {
            Debug.LogWarning("[CombatStageManager] IdleMode 상태에서만 보스 도전을 시작할 수 있습니다.");
            return;
        }

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }
        activeTransitionCoroutine = StartCoroutine(ChallengeTransitionSequence());
    }

    /// <summary>
    /// 방치 몬스터 회수 및 벨트스크롤 전진 화면 무빙을 수행하는 코루틴 시퀀스입니다.
    /// </summary>
    private IEnumerator ChallengeTransitionSequence()
    {
        currentMode = CombatMode.Transition;
        Debug.Log("<color=yellow><b>[CombatStageManager] 보스 도전 연출을 실행합니다.</b></color>");

        // [시퀀스 1]: 현재 필드의 모든 일반 몬스터 즉시 소거 회수
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ClearAllActiveEnemies();
        }

        // [시퀀스 2]: 아군 고블린이 우측으로 달리고 배경이 뒤로 밀려나는 벨트스크롤 연출
        SetHeroDecorationMode(true); // 연출 도중에는 대미지 무시

        float elapsed = 0f;
        Vector2 startFieldPos = combatFieldContext.anchoredPosition;
        Vector2 targetFieldPos = startFieldPos - new Vector2(scrollDistance, 0f); // 배경을 왼쪽으로 밀어 전진 감각 생성

        Vector3 startHeroPos = heroGoblin != null ? heroGoblin.localPosition : Vector3.zero;
        Vector3 targetHeroPos = startHeroPos + new Vector3(150f, 0f, 0f); // 고블린도 살짝 화면 우측으로 돌격 전진

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scrollDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t); // 부드러운 가감속 보간

            if (combatFieldContext != null)
            {
                combatFieldContext.anchoredPosition = Vector2.Lerp(startFieldPos, targetFieldPos, smoothT);
            }
            if (heroGoblin != null)
            {
                heroGoblin.localPosition = Vector3.Lerp(startHeroPos, targetHeroPos, smoothT);
            }

            yield return null;
        }

        if (combatFieldContext != null) combatFieldContext.anchoredPosition = targetFieldPos;
        if (heroGoblin != null) heroGoblin.localPosition = targetHeroPos;

        // [시퀀스 3]: 연출 종료 후 보스 소환 및 진검승부 개시
        SetHeroDecorationMode(false); // 보스전 시작 시 대미지 적용 (진검승부)
        
        if (EnemySpawner.Instance != null)
        {
            // 스폰된 보스의 레퍼런스를 실시간으로 바인딩하여 HP 추적 개시
            spawnedBossUnit = EnemySpawner.Instance.SpawnStageBoss();
        }

        challengeTimer = 0f;
        currentMode = CombatMode.ChallengeMode;
        Debug.Log($"[CombatStageManager] ChallengeTransitionSequence 연출 코루틴이 무사히 완료되어 보스전을 개시합니다. (현재 모드: {currentMode})");
    }

    /// <summary>
    /// 보스전의 승패 여부에 따라 다음 스테이지로 전진하거나 혹은 이전 베이스로 퇴각합니다.
    /// </summary>
    public void EndChallenge(bool isWin)
    {
        if (currentMode != CombatMode.ChallengeMode) return;

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        if (isWin)
        {
            activeTransitionCoroutine = StartCoroutine(WinSequence());
        }
        else
        {
            activeTransitionCoroutine = StartCoroutine(FailSequence());
        }
    }

    /// <summary>
    /// 승리 시: 보스를 소멸시키고, 맵을 한 번 더 전진 스크롤 시킨 후 다음 스테이지로 갱신하여 복귀합니다.
    /// </summary>
    private IEnumerator WinSequence()
    {
        currentMode = CombatMode.Transition;
        Debug.Log("<color=cyan><b>[CombatStageManager] 보스 퇴치 완료! 다음 지역으로 전진합니다.</b></color>");

        // 보스 잔당 소거
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ClearAllActiveEnemies();
        }

        // [연출] 배경판을 한번 더 밀어 맵 변경 연출
        float elapsed = 0f;
        Vector2 startFieldPos = combatFieldContext.anchoredPosition;
        Vector2 targetFieldPos = startFieldPos - new Vector2(scrollDistance, 0f);

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scrollDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (combatFieldContext != null)
            {
                combatFieldContext.anchoredPosition = Vector2.Lerp(startFieldPos, targetFieldPos, smoothT);
            }

            yield return null;
        }

        // 스테이지 레벨 업
        currentStage++;

        // 배경판 및 아군 고블린 좌표 초기 위치로 순간 이동 리셋
        if (combatFieldContext != null) combatFieldContext.anchoredPosition = initialFieldPos;
        if (heroGoblin != null) heroGoblin.localPosition = initialHeroPos;

        // 다시 평화로운 방치 파밍 모드로 복귀 (아군 무적화)
        SetHeroDecorationMode(true);
        currentMode = CombatMode.IdleMode;
        Debug.Log($"[CombatStageManager] WinSequence 연출 코루틴이 무사히 완료되어 파밍 모드로 복귀했습니다. (현재 모드: {currentMode})");
    }

    /// <summary>
    /// 실패 시: 보스를 지우고, 아군을 이전 기본 앵커 좌표로 안전 후퇴시킨 후 기존 스테이지 파밍으로 복귀합니다.
    /// </summary>
    private IEnumerator FailSequence()
    {
        currentMode = CombatMode.Transition;
        Debug.Log("<color=red><b>[CombatStageManager] 도전 실패. 이전 기지로 신속히 후퇴합니다.</b></color>");

        // 보스 해제 및 아군 무적 셋업
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ClearAllActiveEnemies();
        }
        SetHeroDecorationMode(true);

        // [연출] 배경판 원상 복구 및 아군 시작 지점으로 스무스 이동 리셋
        float elapsed = 0f;
        Vector2 startFieldPos = combatFieldContext.anchoredPosition;
        Vector3 startHeroPos = heroGoblin != null ? heroGoblin.localPosition : initialHeroPos;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scrollDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (combatFieldContext != null)
            {
                combatFieldContext.anchoredPosition = Vector2.Lerp(startFieldPos, initialFieldPos, smoothT);
            }
            if (heroGoblin != null)
            {
                heroGoblin.localPosition = Vector3.Lerp(startHeroPos, initialHeroPos, smoothT);
            }

            yield return null;
        }

        // 복귀 셋업 완료
        if (combatFieldContext != null) combatFieldContext.anchoredPosition = initialFieldPos;
        if (heroGoblin != null) heroGoblin.localPosition = initialHeroPos;

        BaseCombatUnit heroUnit = heroGoblin != null ? heroGoblin.GetComponent<BaseCombatUnit>() : null;
        if (heroUnit != null)
        {
            heroUnit.isDecorationMode = false;
        }

        currentMode = CombatMode.IdleMode;
        Debug.Log($"[CombatStageManager] FailSequence 연출 코루틴이 무사히 완료되어 파밍 모드로 복귀했습니다. (현재 모드: {currentMode})");
    }

    /// <summary>
    /// 아군 대표 히어로 유닛의 무적 상태 여부를 일괄 조절합니다.
    /// </summary>
    private void SetHeroDecorationMode(bool active)
    {
        if (heroGoblin != null)
        {
            BaseCombatUnit heroUnit = heroGoblin.GetComponent<BaseCombatUnit>();
            if (heroUnit != null)
            {
                heroUnit.isDecorationMode = active;
            }
        }
    }
}
