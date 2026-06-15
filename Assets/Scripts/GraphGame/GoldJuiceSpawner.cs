using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 고블린의 실시간 발밑 위치를 추적하며, 진행 시간에 비례해 점차 스폰 간격을 가속화하여
/// 골드 파티클을 뿜어내는 가비지 프리(GC-Free) 오브젝트 풀링 기반 스폰 매니저입니다.
/// </summary>
[DisallowMultipleComponent]
public class GoldJuiceSpawner : MonoBehaviour
{
    [Header("UI 대상 타깃 및 배치 설정")]
    [Tooltip("실시간으로 위치를 추적할 고블린 캐릭터의 RectTransform입니다.")]
    [SerializeField] private RectTransform goblinTransform;

    [Tooltip("UI_GoldParticle 컴포넌트가 부착되어 있는 골드 UI 원본 프리팹입니다.")]
    [SerializeField] private GameObject goldPrefab;

    [Tooltip("스폰된 골드 파티클들을 구조적으로 관리할 UI 최상위 부모 패널 RectTransform입니다.")]
    [SerializeField] private RectTransform effectContainer;

    [Header("스폰 속도 및 가속 설정")]
    [Tooltip("초기(0초) 골드 생성 주기 간격(초)입니다. 낮을수록 자주 터집니다.")]
    [SerializeField] private float baseSpawnInterval = 0.5f;

    [Tooltip("골드 생성 주기가 빨라질 수 있는 최소 한계값(초)입니다.")]
    [SerializeField] private float minSpawnInterval = 0.05f;

    [Tooltip("시간 경과에 따라 스폰 간격이 빨라지는 가속 계수입니다.")]
    [SerializeField] private float intensityScale = 0.05f;

    [Header("스폰 위치 세부 보정 (오프셋)")]
    [Tooltip("고블린의 Pivot 좌표를 기준으로 골드 프리팹의 중심/높이를 보정할 X, Y 오프셋 값입니다.")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    // 가비지 컬렉터(GC) 부담을 배제한 유니티 6 내장 ObjectPool 인스턴스
    private ObjectPool<UI_GoldParticle> pool;
    
    // 현재 필드상에서 활동 중인 파티클들의 리스트 (상태 변경 시 일괄 반환용)
    private readonly List<UI_GoldParticle> activeParticles = new List<UI_GoldParticle>();

    private float spawnTimer = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        // 1. 유니티 내장 ObjectPool 안전 초기화
        pool = new ObjectPool<UI_GoldParticle>(
            createFunc: CreateParticle,
            actionOnGet: OnGetParticle,
            actionOnRelease: OnReleaseParticle,
            actionOnDestroy: OnDestroyParticle,
            collectionCheck: true,
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    private void Start()
    {
        // 2. 런타임 오류 방지를 위한 방어 코드 검증
        if (goblinTransform == null || goldPrefab == null || effectContainer == null)
        {
            Debug.LogError("[GoldJuiceSpawner] 필수 바인딩 레퍼런스가 인스펙터에서 누락되었습니다!");
            enabled = false;
            return;
        }

        // 3. GameManager의 진행 상태 이벤트에 스폰 제어기 바인딩
        GraphGameManager.OnStateChanged += HandleStateChanged;

        // 4. 싱글톤 시작 시 즉시 상태 매핑 동기화
        if (GraphGameManager.Instance != null)
        {
            HandleStateChanged(GraphGameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 해제를 통한 메모리 누수 원천 차단
        GraphGameManager.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        if (!isRunning) return;
        if (GraphGameManager.Instance == null) return;

        // [핵심 요구사항]: 진행 시간에 따라 화려함(스폰 빈도) 가속화 연산
        float currentTimer = GraphGameManager.Instance.CurrentTimer;
        float currentInterval = Mathf.Max(minSpawnInterval, baseSpawnInterval - (currentTimer * intensityScale));

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;
            SpawnGoldParticle();
        }
    }

    /// <summary>
    /// 오브젝트 풀을 통해 골드를 꺼내와 고블린 위치 기준으로 발사 연출을 구동합니다.
    /// </summary>
    private void SpawnGoldParticle()
    {
        if (goblinTransform == null || pool == null) return;

        // 고블린의 현재 로컬 앵커 좌표에 오프셋 값을 보정하여 적용
        Vector2 spawnPos = goblinTransform.anchoredPosition + spawnOffset;

        UI_GoldParticle particle = pool.Get();
        if (particle != null)
        {
            // 발진시키며 스스로 반환할 때의 Callback Action을 주입합니다.
            particle.Launch(spawnPos, ReleaseParticleToPool);
        }
    }

    /// <summary>
    /// 개별 파티클이 수명이 끝나 반환 요청을 보낼 때 작동하는 공용 릴리즈 메서드입니다.
    /// </summary>
    private void ReleaseParticleToPool(UI_GoldParticle particle)
    {
        if (pool != null && particle != null)
        {
            pool.Release(particle);
        }
    }

    /// <summary>
    /// 미니게임 상태 변경을 감지하여 타이머를 리셋하거나 화면상에 튀어 있는 골드들을 안전하게 풀로 전부 소생시킵니다.
    /// </summary>
    private void HandleStateChanged(GraphGameState state)
    {
        if (state == GraphGameState.Running)
        {
            isRunning = true;
            spawnTimer = 0f;
        }
        else
        {
            // Ready, Success, Busted 등 Running이 아닐 때는 연출을 즉각 중지하고 활성 파티클들을 회수합니다.
            isRunning = false;
            ClearAllActiveParticles();
        }
    }

    /// <summary>
    /// 현재 화면에 보여지고 활성화되어 있는 모든 골드 파티클을 일괄적으로 풀에 안전 회수시킵니다.
    /// </summary>
    private void ClearAllActiveParticles()
    {
        // 리스트 역순으로 돌리며 OnReleaseParticle 콜백의 Remove 연산 시 발생하는 컬렉션 변경 꼬임 방지
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            if (activeParticles[i] != null)
            {
                // 강제 풀 반환 구동
                activeParticles[i].ReturnToPool();
            }
        }
        activeParticles.Clear();
    }

    #region ObjectPool 바인딩 메서드군

    private UI_GoldParticle CreateParticle()
    {
        GameObject go = Instantiate(goldPrefab, effectContainer);
        UI_GoldParticle particle = go.GetComponent<UI_GoldParticle>();
        if (particle == null)
        {
            particle = go.AddComponent<UI_GoldParticle>();
        }
        return particle;
    }

    private void OnGetParticle(UI_GoldParticle particle)
    {
        particle.gameObject.SetActive(true);
        if (!activeParticles.Contains(particle))
        {
            activeParticles.Add(particle);
        }
    }

    private void OnReleaseParticle(UI_GoldParticle particle)
    {
        particle.gameObject.SetActive(false);
        activeParticles.Remove(particle);
    }

    private void OnDestroyParticle(UI_GoldParticle particle)
    {
        if (particle != null)
        {
            Destroy(particle.gameObject);
        }
    }

    #endregion
}
