using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방치 자동 전투(IdleMode) 진행 시 좌우 화면 경계에서 Y축 레인을 반영하여 몬스터를 무한 스폰시키고,
/// 보스전 돌입 시 진검승부용 보스 유닛을 소환해 주는 적 스폰 매니저 클래스입니다.
/// UGUI 캔버스 좌표계 스케일 꼬임을 완전히 해소한 픽셀 이동이 적용됩니다.
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

    [Header("스폰 주기 설정")]
    [Tooltip("방치 파밍 모드 시 일반 몬스터가 생성될 간격 주기(초 단위)입니다.")]
    [SerializeField] private float spawnInterval = 2.0f;

    private float spawnTimer = 0f;

    // 현재 씬에 생성되어 활동 중인 적 오브젝트 리스트 (일괄 소거용)
    private readonly List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        // 싱글톤 이니셜라이즈
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (CombatStageManager.Instance == null)
        {
            // 매 프레임 찍히는 것을 예방하기 위해 주기적으로 또는 최초 경고만 로깅
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("[EnemySpawner] 씬 내에 활성화된 CombatStageManager.Instance를 찾을 수 없어 몬스터 스폰 타이머가 일시정지 중입니다.");
            }
            return;
        }

        // [코딩 제약 조건] 방치 파밍 모드(IdleMode)일 때만 일반 몬스터 주기적 스폰
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
    /// 화면의 왼쪽 혹은 오른쪽 외곽 중 임의 한 곳을 선정해 Y축 레인 오프셋을 더해 잡몹을 무적 데코 모드로 생성합니다.
    /// </summary>
    private void SpawnNormalEnemy()
    {
        // [디버그 방어 로그 강화]
        if (normalEnemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] normalEnemyPrefab(잡몹 프리팹)이 인스펙터에 등록되지 않아 스폰을 차단합니다!");
            return;
        }
        if (spawnParent == null)
        {
            Debug.LogError("[EnemySpawner] spawnParent(스폰 부모 캔버스)가 인스펙터에 등록되지 않아 스폰을 차단합니다!");
            return;
        }
        if (battleBackground == null)
        {
            Debug.LogError("[EnemySpawner] battleBackground(배경 판)가 인스펙터에 등록되지 않아 스폰을 차단합니다!");
            return;
        }
        if (leftSpawnPoint == null || rightSpawnPoint == null)
        {
            Debug.LogError("[EnemySpawner] leftSpawnPoint 또는 rightSpawnPoint가 지정되지 않아 스폰 위치를 잡지 못했습니다!");
            return;
        }

        // 1. 무작위 스폰 포인트 선택 (좌/우 분기)
        bool isLeft = Random.value > 0.5f;
        RectTransform selectedPoint = isLeft ? leftSpawnPoint : rightSpawnPoint;
        if (selectedPoint == null) return;

        // [월드 좌표계 기반 절대 스폰 위치 연산]: 스크롤이동 시의 로컬 좌표 편향 오류를 해결하기 위해 월드 좌표계 활용
        Vector3[] corners = new Vector3[4];
        battleBackground.GetWorldCorners(corners);

        float spawnWorldMinY = corners[0].y + yPadding;
        float spawnWorldMaxY = corners[1].y - yPadding;
        float randomWorldY = Random.Range(spawnWorldMinY, spawnWorldMaxY);

        // 스폰 포인트 X축 역시 월드 좌표(transform.position.x) 기준
        float spawnWorldX = isLeft ? leftSpawnPoint.position.x : rightSpawnPoint.position.x;

        // [UGUI 프리팹 스케일 꼬임 방지 Instantiate 옵션 적용]
        GameObject enemy = Instantiate(normalEnemyPrefab, spawnParent, false);
        
        // 월드 좌표 덮어쓰기 적용 (UI 로컬 앵커 꼬임 완전 차단)
        enemy.transform.position = new Vector3(spawnWorldX, randomWorldY, enemy.transform.position.z);
        enemy.transform.localScale = Vector3.one; // 크기 1, 1, 1 고정

        // 3. 전투 AI 스탯 설정
        BaseCombatUnit unit = enemy.GetComponent<BaseCombatUnit>();
        if (unit != null)
        {
            unit.isEnemy = true;
            
            // [버그 해결]: 아군(고블린)은 방치 모드 동안 무적으로 유지되지만, 적들은 아군의 공격을 받아 체력이 깎이고 사망할 수 있도록 무적(isDecorationMode)을 해제합니다.
            unit.isDecorationMode = false;

            // [전역 타겟팅 연동] 전투 매니저 적 목록 리스트에 즉시 직접 추가
            if (CombatManager.Instance != null)
            {
                if (!CombatManager.Instance.enemyUnits.Contains(unit))
                {
                    CombatManager.Instance.enemyUnits.Add(unit);
                }
            }
        }

        activeEnemies.Add(enemy);
    }

    /// <summary>
    /// 시네마틱 카메라 연출을 위해 배경 이미지(BattleBackground)의 가장 오른쪽 끝 영역에 보스를 로컬 좌표 기준으로 소환합니다.
    /// </summary>
    public BaseCombatUnit SpawnStageBoss()
    {
        if (bossEnemyPrefab == null || spawnParent == null || battleBackground == null) return null;

        Debug.Log("[EnemySpawner] 시네마틱 연출을 위해 보스를 배경 판 우측 경계선에 소환합니다!");

        // [UGUI 프리팹 스케일 꼬임 방지 Instantiate 옵션 적용]
        GameObject enemy = Instantiate(bossEnemyPrefab, spawnParent, false);
        RectTransform bossRect = enemy.GetComponent<RectTransform>();
        
        // battleBackground의 오른쪽 끝(xMax) 좌표를 구하여 약간의 여백(150f)을 두고 배치 (Y축은 0f 중앙 고정)
        float bossSpawnX = battleBackground.rect.xMax - 150f;
        bossRect.anchoredPosition = new Vector2(bossSpawnX, 0f);
        bossRect.localScale = Vector3.one; // 크기 1, 1, 1 고정

        // 2. 보스 진검승부 모드 활성화 (isDecorationMode = false)
        BaseCombatUnit bossUnit = enemy.GetComponent<BaseCombatUnit>();
        if (bossUnit != null)
        {
            bossUnit.isEnemy = true;
            bossUnit.isDecorationMode = false; // 아군 고블린과 진짜로 생명력을 깎으며 싸움

            // [전역 타겟팅 연동] 전투 매니저 적 목록 리스트에 즉시 직접 추가
            if (CombatManager.Instance != null)
            {
                if (!CombatManager.Instance.enemyUnits.Contains(bossUnit))
                {
                    CombatManager.Instance.enemyUnits.Add(bossUnit);
                }
            }
        }

        activeEnemies.Add(enemy);
        return bossUnit;
    }

    /// <summary>
    /// 전장 필드에 소환되어 있는 모든 몬스터(일반/보스 전체)를 즉각 소거 및 비활성화하여 풀로 회수합니다.
    /// </summary>
    public void ClearAllActiveEnemies()
    {
        // 역순 순회하며 메모리 파괴 진행
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
            {
                // 오브젝트 파괴 시 BaseCombatUnit.OnDestroy()에 의해 CombatManager 풀에서 자동 제외됨
                Destroy(activeEnemies[i]);
            }
        }
        activeEnemies.Clear();
        Debug.Log("[EnemySpawner] 필드 상의 모든 적이 비활성화 소거되었습니다.");
    }
}
