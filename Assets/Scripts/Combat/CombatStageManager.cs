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

    [Tooltip("카메라 Clamp 범위를 연산하기 위한 배경 이미지 판 RectTransform입니다.")]
    public RectTransform battleBackground;

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
        // 1. [카메라 팔로우 및 Clamp 제약 기능]: 트랜지션(카메라 강제 연출 중)이 아닐 때만 영웅 추적을 수행
        if (currentMode != CombatMode.Transition && heroGoblin != null && combatFieldContext != null)
        {
            RectTransform heroRect = heroGoblin.GetComponent<RectTransform>();
            if (heroRect != null)
            {
                // 영웅 위치에 따른 기본 카메라 목표 좌표 계산
                float targetCameraX = -heroRect.anchoredPosition.x;

                // [기획 규칙 추가]: 카메라가 배경 이미지의 좌측/우측 끝을 벗어나지 않도록 한계선(Clamp) 계산
                if (battleBackground != null)
                {
                    float halfScreenWidth = Screen.width * 0.5f;

                    // 배경이 가로로 넓다고 가정할 때, 카메라(중앙)가 갈 수 있는 최소/최대 X 제약
                    float minCameraX = -(battleBackground.rect.width * 0.5f) + halfScreenWidth;
                    float maxCameraX = (battleBackground.rect.width * 0.5f) - halfScreenWidth;

                    // UGUI 계산식 피벗 대칭 보정 Clamp 적용
                    targetCameraX = Mathf.Clamp(targetCameraX, minCameraX, maxCameraX);
                }

                // Y축은 기존 마당판의 높이를 유지하고 X축만 추적
                Vector2 targetPos = new Vector2(targetCameraX, combatFieldContext.anchoredPosition.y);
                
                // Time.deltaTime을 활용해 카메라가 홱홱 튀지 않고 부드럽게(Lerp) 따라가도록 보정
                combatFieldContext.anchoredPosition = Vector2.Lerp(combatFieldContext.anchoredPosition, targetPos, Time.deltaTime * 5f);
            }
        }

        // 2. 기존 보스전 승패 판정 감시: ChallengeMode 상태일 때만 보스 처치 타이머 가동 및 실시간 승패 체크
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
    /// 도전 버튼을 클릭했을 때 방치 모드에서 보스전 진입 연출(Transition)로 전이시킵니다.
    /// </summary>
    public void TriggerChallengeMode()
    {
        if (currentMode == CombatMode.Transition) return;
        
        currentMode = CombatMode.Transition;
        
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }
        activeTransitionCoroutine = StartCoroutine(BossAppearanceSequence());
    }

    /// <summary>
    /// 시네마틱 카메라 연출: 보스 먼저 클로즈업 ➡️ 1초 대기 ➡️ 아군 고블린으로 카메라 복귀 ➡️ 진검승부 개시
    /// </summary>
    private IEnumerator BossAppearanceSequence()
    {
        Debug.Log("<color=yellow><b>[CombatStageManager] 시네마틱 보스 등장 연출을 개시합니다.</b></color>");

        // 1. 기존 방치 모드의 일반 몬스터 즉시 싹쓸이
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ClearAllActiveEnemies();
        }

        // 연출 도중 아군 고블린의 생명력 보존을 위해 무적화 적용
        SetHeroDecorationMode(true);

        // 2. 보스 즉시 스폰 (참조값 받아오기)
        BaseCombatUnit bossUnit = EnemySpawner.Instance != null ? EnemySpawner.Instance.SpawnStageBoss() : null;
        spawnedBossUnit = bossUnit; // 보스전 승패 감지용 실시간 바인딩 완료

        if (bossUnit == null || heroGoblin == null)
        {
            Debug.LogError("[CombatStageManager] 보스 유닛 혹은 아군 고블린 레퍼런스가 없어 연출을 취소하고 즉시 전투를 시작합니다.");
            currentMode = CombatMode.ChallengeMode;
            yield break;
        }

        RectTransform bossRect = bossUnit.GetComponent<RectTransform>();
        RectTransform heroRect = heroGoblin.GetComponent<RectTransform>();

        if (bossRect == null || heroRect == null)
        {
            currentMode = CombatMode.ChallengeMode;
            yield break;
        }

        // 3. 카메라 이동: 보스를 향해 부드럽게 패닝 (보스가 화면 우측에 적당히 보이도록 스크린 오프셋 조절)
        float targetXForBoss = -bossRect.anchoredPosition.x + (Screen.width * 0.2f);
        yield return StartCoroutine(PanCameraTo(targetXForBoss, 1.0f));

        // 4. 보스를 비춘 상태로 잠시 대기 (시각적 긴장감 연출)
        yield return new WaitForSeconds(1.0f);

        // 5. 카메라 이동: 다시 히어로 고블린을 향해 패닝
        float targetXForHero = -heroRect.anchoredPosition.x;
        yield return StartCoroutine(PanCameraTo(targetXForHero, 0.8f));

        // 6. 시퀀스 종료 및 진짜 진검승부 돌입
        SetHeroDecorationMode(false); // 고블린 무적 상태 해제
        challengeTimer = 0f;
        currentMode = CombatMode.ChallengeMode;
        
        Debug.Log($"[CombatStageManager] 시네마틱 연출이 정상 완료되어 보스전을 개시합니다. (현재 모드: {currentMode})");
    }

    /// <summary>
    /// 부드러운 카메라(배경판) 이동을 담당하는 공통 코루틴 함수 (SmoothStep 가감속 활용)
    /// </summary>
    private IEnumerator PanCameraTo(float targetX, float duration)
    {
        float time = 0f;
        Vector2 startPos = combatFieldContext.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, combatFieldContext.anchoredPosition.y);

        while (time < duration)
        {
            time += Time.deltaTime;
            // SmoothStep을 사용하여 시작과 끝이 부드러운 카메라 가감속 연출
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            combatFieldContext.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        combatFieldContext.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 보스 몬스터 사망 즉시 영웅 추적을 멈추고 승리 시퀀스로 전이하는 메서드입니다.
    /// </summary>
    public void OnBossKilled()
    {
        // 카메라가 더 이상 움직이지 않도록 즉시 연출(Transition) 상태로 묶어버립니다.
        currentMode = CombatMode.Transition;
        
        Debug.Log("[시스템] 스테이지 보스 처치 완료! 카메라를 현 위치에 고정하고 승리 연출을 전개합니다.");

        // 승리 퇴치 연출 코루틴을 즉각 가동시킵니다.
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }
        activeTransitionCoroutine = StartCoroutine(WinSequence());
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
    /// 승리 시: 영웅을 화면 우측 너머로 걸어나가 퇴장하게 하고, 좌표를 초기화해 다음 스테이지 방치 모드로 진입합니다.
    /// </summary>
    private IEnumerator WinSequence()
    {
        Debug.Log("<color=cyan><b>[CombatStageManager] 스테이지 클리어! 다음 구역으로 영웅이 행진 퇴장합니다.</b></color>");

        // 1. 영웅 고블린에게 우측 화면 밖 퇴장 걷기 명령 하달
        BaseCombatUnit heroUnit = heroGoblin != null ? heroGoblin.GetComponent<BaseCombatUnit>() : null;
        if (heroUnit != null)
        {
            heroUnit.MoveToRightExit();
        }

        // 씬 내 남아있는 모든 적 잔당 일괄 삭제 소거
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ClearAllActiveEnemies();
        }

        // 2. 영웅이 화면 우측 경계면 밖으로 완전 퇴장하는 모습을 2.5초간 대기 감상
        yield return new WaitForSeconds(2.5f);

        // 3. [중앙 위치 초기화] 
        // 카메라 추적 버그를 막기 위해 아직 currentMode는 Transition(락 상태)을 유지합니다.
        if (heroUnit != null)
        {
            // 영웅의 행동 제어를 풀고, 캔버스 부모판의 정중앙(0,0)으로 강제 순간이동 (Vector3.zero 전달 시 내부에서 anchoredPosition.zero 적용)
            heroUnit.ResetToInitialPosition(Vector3.zero); 

            RectTransform heroRect = heroGoblin.GetComponent<RectTransform>();
            if (heroRect != null)
            {
                heroRect.anchoredPosition = Vector2.zero; 
            }
        }
        
        // 배경 마당판도 정중앙(0,0)으로 강제 순간이동시켜 영웅과 카메라 시점을 중앙에 일치시킵니다.
        if (combatFieldContext != null)
        {
            combatFieldContext.anchoredPosition = Vector2.zero;
        }

        // 스테이지 카운트 증가 돌파
        currentStage++;
        Debug.Log($"[시스템] 새 스테이지 {currentStage} 맵 중앙 배치 완료. 환경 정비를 위해 1초간 대기합니다.");

        // 4. [스폰 킬 방지 리스크 케어] 
        // 유저가 새 스테이지 시작을 인지할 수 있도록 1초간 정적 상태를 유지 (이동/스폰 모두 대기)
        yield return new WaitForSeconds(1.0f);

        Debug.Log("[시스템] 전투를 재개합니다.");

        // 다시 히어로를 연출 무적화 상태로 셋업하고 방치 파밍 모드로 복구
        SetHeroDecorationMode(true);
        currentMode = CombatMode.IdleMode;
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
