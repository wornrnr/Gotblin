using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방치 자동 전투(IdleMode) 진행 시 좌우 화면 경계에서 Y축 레인을 반영하여 몬스터를 스폰시키고,
/// 보스전 돌입 시 진검승부용 보스 유닛을 소환해 주는 적 스폰 매니저 클래스입니다.
/// 매번 유닛을 Instantiate/Destroy하지 않고 재사용하는 오브젝트 풀링(Object Pooling) 시스템이 도입되었습니다.
/// </summary>
[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    // 스포너 싱글톤 인스턴스
    public static EnemySpawner Instance { get; private set; }

    [Header("스폰 프리팹 설정")]
    [Tooltip("방치 모드에서 무한히 스폰되어 나올 일반 잡몹 몬스터 프리팹입니다.")]
    [SerializeField] private GameObject normalEnemyPrefab;

    [Tooltip("도전 모드 진입 시 소환될 강력한 보스 몬스터 프리팹입니다.")]
    [SerializeField] private GameObject bossEnemyPrefab;

    [Header("스폰 배치 공간 셋업")]
    [Tooltip("스폰된 몬스터들이 배치될 부모 캔버스 RectTransform 패널입니다 (보통 combatFieldContext).")]
    [SerializeField] private RectTransform spawnParent;

    [Tooltip("화면 왼쪽 외곽의 스폰 기준점 앵커 좌표입니다.")]
    [SerializeField] private RectTransform leftSpawnPoint;

    [Tooltip("화면 오른쪽 외곽의 스폰 기준점 앵커 좌표입니다.")]
    [SerializeField] private RectTransform rightSpawnPoint;

    [Header("Y축 레인 및 화면 영역 셋업")]
    [Tooltip("Y축 랜덤 스폰 범위를 계산할 배경 판 RectTransform입니다.")]
    [SerializeField] private RectTransform battleBackground;

    [Tooltip("배경 판 위아래 테두리에서 이 수치만큼 안쪽 영역에서만 스폰되도록 마진을 둡니다.")]
    [SerializeField] private float yPadding = 50f;

    [Header("오브젝트 풀링 설정")]
    [Tooltip("게임 시작 시 미리 대기실에 생성해둘 일반 몬스터 수량입니다.")]
    [SerializeField] private int initialPoolSize = 20;

    [Header("스폰 주기 설정")]
    [Tooltip("방치 파밍 모드 시 일반 몬스터가 생성될 간격 주기(초 단위)입니다.")]
    [SerializeField] private float spawnInterval = 2.0f;

    private float spawnTimer = 0f;

    // 일반 몬스터 프리워밍용 오브젝트 풀 큐
    private readonly Queue<GameObject> enemyPool = new Queue<GameObject>();

    // 현재 전장에 활동 중인 모든 적 게임오브젝트 리스트 (일괄 소거/반납용)
    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        // 싱글톤 이니셜라이즈
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (normalEnemyPrefab == null || spawnParent == null) return;

        // [최적화 - 프리워밍]: 대기 풀에 비활성화된 일반 잡몹들을 미리 생성하여 보관
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject enemy = Instantiate(normalEnemyPrefab, spawnParent, false);
            enemy.SetActive(false);
            enemyPool.Enqueue(enemy);
        }
    }

    private void Update()
    {
        if (CombatStageManager.Instance == null)
        {
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("[EnemySpawner] 씬 내에 활성화된 CombatStageManager.Instance를 찾을 수 없어 몬스터 스폰 타이머가 일시정지 중입니다.");
            }
            return;
        }

        // 방치 파밍 모드(IdleMode)일 때만 일반 몬스터 주기적 스폰
        if (CombatStageManager.Instance.currentMode == CombatMode.IdleMode)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnNormalEnemy();
            }
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    /// <summary>
    /// 오브젝트 풀에서 잡몹을 안전하게 인계받아 활성화시키고 컴포넌트 상태를 완벽히 리셋해 반환합니다.
    /// </summary>
    public GameObject GetEnemyFromPool()
    {
        GameObject enemy;

        if (enemyPool.Count > 0)
        {
            enemy = enemyPool.Dequeue();
            if (enemy == null)
            {
                // 에디터 씬 강제 삭제 등의 특수 경우 예외 방어용 생성
                enemy = Instantiate(normalEnemyPrefab, spawnParent, false);
            }
            else
            {
                enemy.SetActive(true);
            }
        }
        else
        {
            // 순간적으로 가용 수량이 부족할 경우 동적 확장
            enemy = Instantiate(normalEnemyPrefab, spawnParent, false);
        }

        // [초기화 청소]: 재사용 유닛의 스탯 및 넉백/피격 연출 상태를 완전히 태초 상태로 복구
        BaseCombatUnit unitScript = enemy.GetComponent<BaseCombatUnit>();
        if (unitScript != null)
        {
            unitScript.ResetUnitStateForReuse();
        }

        return enemy;
    }

    /// <summary>
    /// 전장에서 해제된 몬스터를 안전하게 풀 대기실로 회수하여 보관합니다.
    /// </summary>
    public void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy == null) return;

        BaseCombatUnit unit = enemy.GetComponent<BaseCombatUnit>();
        if (unit != null)
        {
            // 전투 매니저 리스트 풀에서 제거 진행
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.UnregisterUnit(unit);
            }

            // [보스 예외 필터링]: 보스 몬스터는 풀링 대상이 아니므로 풀에 들이지 않고 완전 제거 처리
            if (unit.isBoss)
            {
                Destroy(enemy);
                return;
            }
        }

        enemy.SetActive(false);
        enemyPool.Enqueue(enemy);
    }

    /// <summary>
    /// 화면의 왼쪽 혹은 오른쪽 외곽 중 임의 한 곳을 선정해 오브젝트 풀을 활용하여 잡몹을 무적 데코 모드로 생성합니다.
    /// </summary>
    private void SpawnNormalEnemy()
    {
        if (normalEnemyPrefab == null || spawnParent == null || battleBackground == null) return;
        if (leftSpawnPoint == null || rightSpawnPoint == null) return;

        // 1. 무작위 스폰 포인트 선택 (좌/우 분기)
        bool isLeft = Random.value > 0.5f;
        RectTransform selectedPoint = isLeft ? leftSpawnPoint : rightSpawnPoint;
        if (selectedPoint == null) return;

        // [월드 좌표계 기반 절대 스폰 위치 연산]
        Vector3[] corners = new Vector3[4];
        battleBackground.GetWorldCorners(corners);

        float spawnWorldMinY = corners[0].y + yPadding;
        float spawnWorldMaxY = corners[1].y - yPadding;
        float randomWorldY = Random.Range(spawnWorldMinY, spawnWorldMaxY);

        float spawnWorldX = isLeft ? leftSpawnPoint.position.x : rightSpawnPoint.position.x;

        // 2. [풀링 연동]: Instantiate 대신 풀을 통해 객체 획득
        GameObject enemy = GetEnemyFromPool();
        
        // 위치 지정 및 로컬 스케일 리셋
        enemy.transform.position = new Vector3(spawnWorldX, randomWorldY, enemy.transform.position.z);
        enemy.transform.localScale = Vector3.one;

        // 3. 전투 AI 스탯 설정
        BaseCombatUnit unit = enemy.GetComponent<BaseCombatUnit>();
        if (unit != null)
        {
            unit.isEnemy = true;
            
            // 일반 몬스터이므로 무적 해제 (체력 닳아서 사망/반납 가능)
            unit.isDecorationMode = false;

            // [전역 타겟팅 연동] 전투 매니저 리스트에 등록
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.RegisterUnit(unit);
            }
        }

        activeEnemies.Add(enemy);
    }

    /// <summary>
    /// 우측 스폰 포인트 영역에 Y축 레인을 적용해 진검승부(isDecorationMode = false)용 보스 몬스터를 소환합니다.
    /// </summary>
    public BaseCombatUnit SpawnStageBoss()
    {
        if (bossEnemyPrefab == null || spawnParent == null || battleBackground == null) return null;

        Debug.Log("[EnemySpawner] 시네마틱 연출을 위해 보스를 배경 판 우측 경계선에 소환합니다!");

        // [UGUI 프리팹 스케일 꼬임 방지 Instantiate 옵션 적용] (보스는 풀링 비대상)
        GameObject enemy = Instantiate(bossEnemyPrefab, spawnParent, false);
        RectTransform bossRect = enemy.GetComponent<RectTransform>();
        
        float bossSpawnX = battleBackground.rect.xMax - 150f;
        bossRect.anchoredPosition = new Vector2(bossSpawnX, 0f);
        bossRect.localScale = Vector3.one;

        BaseCombatUnit bossUnit = enemy.GetComponent<BaseCombatUnit>();
        if (bossUnit != null)
        {
            bossUnit.isEnemy = true;
            bossUnit.isDecorationMode = false;

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.RegisterUnit(bossUnit);
            }
        }

        activeEnemies.Add(enemy);
        return bossUnit;
    }

    /// <summary>
    /// 전장 필드에 소환되어 있는 모든 몬스터를 Destroy가 아닌 풀로 회수하여 안전하게 비활성화 소거합니다.
    /// </summary>
    public void ClearAllActiveEnemies()
    {
        // 역순 순회하며 풀로 안전 반납 진행
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                // [풀링 연동]: Destroy 대신 ReturnEnemyToPool 호출
                ReturnEnemyToPool(activeEnemies[i]);
            }
        }
        activeEnemies.Clear();
        Debug.Log("[EnemySpawner] 필드 상의 모든 적이 풀로 안전하게 반납 및 비활성화되었습니다.");
    }
}
